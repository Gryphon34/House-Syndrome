using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RetroVisionPro
{
    public sealed class AnalogFrameFeedbackFX : ScriptableRendererFeature
    {
        [SerializeField] private Shader m_Shader;
        [SerializeField, HideInInspector] private Material m_Material;
        private ArtefactsPass m_Pass;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            m_Shader = m_Shader ? m_Shader : Shader.Find("RetroVisionPro/CRT&VHSFX/AnalogFrameFeedbackURP");
            if (m_Shader == null) return;

            m_Material = new Material(m_Shader);
            m_Pass = new ArtefactsPass(m_Material) { renderPassEvent = Event };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData rd)
        {
            var vol = VolumeManager.instance.stack?.GetComponent<AnalogFrameFeedback>();
            if (vol == null || !vol.IsActive()) return;

            if (!rd.cameraData.postProcessEnabled)
                return;

            if (rd.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
#if UNITY_EDITOR
            if (m_Material != null)
            {
                if (EditorApplication.isPlaying) Destroy(m_Material);
                else DestroyImmediate(m_Material);
            }
#else
            if (m_Material != null) Destroy(m_Material);
#endif
            m_Pass?.Dispose();
        }

        // ======== PASS (RenderGraph) ========
        private sealed class ArtefactsPass : ScriptableRenderPass
        {
            // Properties
            static readonly int _FeedbackThreshID = Shader.PropertyToID("feedbackThresh");
            static readonly int _FeedbackAmountID = Shader.PropertyToID("feedbackAmount");
            static readonly int _FeedbackFadeID = Shader.PropertyToID("feedbackFade");
            static readonly int _FeedbackColorID = Shader.PropertyToID("feedbackColor");
            static readonly int _SpreadXID = Shader.PropertyToID("SpreadX");
            static readonly int _FeedbackTexID = Shader.PropertyToID("_FeedbackTex");
            static readonly int _LastTexID = Shader.PropertyToID("_LastTex");

            private readonly Material m_Mat;
            private RTHandle m_History; // previous-frame color

            public ArtefactsPass(Material mat)
            {
                m_Mat = mat;
                // We read cameraColor, write a new cameraColor, and keep history.
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            // Data blocks for RG lambdas
            private class BuildData
            {
                public Material mat;
                public TextureHandle src;    // cameraColor
                public TextureHandle prev;   // history as TextureHandle
                public AnalogFrameFeedback vol;
            }
            private class CompositeData
            {
                public Material mat;
                public TextureHandle src;       // cameraColor
                public TextureHandle feedback;  // from build
                public AnalogFrameFeedback vol;
            }
            private class CopyData
            {
                public TextureHandle src;
                public RTHandle history;
            }

            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frame)
            {
                var vol = VolumeManager.instance.stack.GetComponent<AnalogFrameFeedback>();
                if (vol == null || !vol.IsActive()) { ReleaseHistory(); return; }

                var res = frame.Get<UniversalResourceData>();
                var cam = frame.Get<UniversalCameraData>();

                EnsureHistory(cam.cameraTargetDescriptor);

                // PASS A: Build feedback (src + prev -> feedback) with shader pass 0
                TextureHandle feedback;
                {
                    using var builder = rg.AddRasterRenderPass<BuildData>("AnalogFrameFeedback: Build", out var data);
                    builder.AllowGlobalStateModification(true);
                    data.mat = m_Mat;
                    data.vol = vol;
                    data.src = res.cameraColor;
                    data.prev = rg.ImportTexture(m_History);

                    builder.UseTexture(data.src, AccessFlags.Read);
                    builder.UseTexture(data.prev, AccessFlags.Read);

                    var desc = rg.GetTextureDesc(res.cameraColor);
                    desc.name = "_AFF_Feedback";
                    desc.clearBuffer = false;
                    feedback = rg.CreateTexture(desc);
                    builder.SetRenderAttachment(feedback, 0, AccessFlags.Write);

                    builder.SetRenderFunc((BuildData d, RasterGraphContext ctx) =>
                    {
                        ApplyVolume(d.mat, d.vol);
                        ctx.cmd.SetGlobalTexture(_LastTexID, d.prev);
                        // Pass 0 writes artefact buffer
                        Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 0);
                    });
                }

                // PASS B: Composite (src + feedback -> temp) with shader pass 1
                TextureHandle temp;
                {
                    using var builder = rg.AddRasterRenderPass<CompositeData>("AnalogFrameFeedback: Composite", out var data);
                    builder.AllowGlobalStateModification(true);
                    data.mat = m_Mat;
                    data.vol = vol;
                    data.src = res.cameraColor;

                    builder.UseTexture(data.src, AccessFlags.Read);
                    builder.UseTexture(feedback, AccessFlags.Read);

                    var desc = rg.GetTextureDesc(res.cameraColor);
                    desc.name = "_AFF_Temp";
                    desc.clearBuffer = false;
                    temp = rg.CreateTexture(desc);
                    builder.SetRenderAttachment(temp, 0, AccessFlags.Write);

                    builder.SetRenderFunc((CompositeData d, RasterGraphContext ctx) =>
                    {
                        // If debug -> show feedback only
                        if (d.vol.debugArtefacts.value)
                        {
                            // Copy feedback to output
                            ctx.cmd.SetGlobalTexture(_FeedbackTexID, feedback);
                            Blitter.BlitTexture(ctx.cmd, feedback, new Vector4(1, 1, 0, 0), 0f, false);
                        }
                        else
                        {
                            d.mat.SetTexture(_FeedbackTexID, feedback);
                            // Pass 1 composites onto src
                            Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), d.mat, 1);
                        }
                    });
                }

                // Make the temp the new camera color
                res.cameraColor = temp;

                // PASS C: Update history (temp -> m_History)
                {
                    using var builder = rg.AddRasterRenderPass<CopyData>("AnalogFrameFeedback: History", out var data);
                    data.src = temp;
                    data.history = m_History;
                    builder.UseTexture(data.src, AccessFlags.Read);

                    var dst = rg.ImportTexture(m_History);
                    builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                    builder.SetRenderFunc((CopyData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.src, new Vector4(1, 1, 0, 0), 0f, false);
                    });
                }
            }

            private void EnsureHistory(in RenderTextureDescriptor camDesc)
            {
                var desc = camDesc;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                desc.enableRandomWrite = false;

                RenderingUtils.ReAllocateHandleIfNeeded(
                    ref m_History,
                    in desc,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    anisoLevel: 1,
                    mipMapBias: 0f,
                    name: "_AFF_History");
            }

            private static void ApplyVolume(Material mat, AnalogFrameFeedback v)
            {
                mat.SetFloat(_FeedbackThreshID, v.cutOff.value);
                mat.SetFloat(_FeedbackAmountID, v.amount.value);
                mat.SetFloat(_FeedbackFadeID, v.fade.value);
                mat.SetColor(_FeedbackColorID, v.color.value);
                if (mat.HasProperty(_SpreadXID)) mat.SetFloat(_SpreadXID, v.SpreadX.value);
            }

            public void Dispose() => ReleaseHistory();

            private void ReleaseHistory()
            {
                if (m_History != null)
                {
                    RTHandles.Release(m_History);
                    m_History = null;
                }
            }
        }
    }
}
