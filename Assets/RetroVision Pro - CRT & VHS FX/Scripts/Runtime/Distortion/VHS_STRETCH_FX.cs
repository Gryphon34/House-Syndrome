using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
namespace RetroVisionPro
{
    public class VHS_STRETCH_FX : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader m_Shader;
        private Material material;
        VHS_STRETCH_Pass RetroVision_RenderPass;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            m_Shader = Shader.Find("RetroVisionPro/CRT&VHSFX/Distortion/VHS_STRETCH");
            if (m_Shader == null)
            {
                return;
            }
            material = new Material(m_Shader);
            RetroVision_RenderPass = new VHS_STRETCH_Pass(material);

            RetroVision_RenderPass.renderPassEvent = Event;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VHS_STRETCH myVolume = VolumeManager.instance.stack?.GetComponent<VHS_STRETCH>();
            if (myVolume == null || !myVolume.IsActive())
                return;
            if (!renderingData.cameraData.postProcessEnabled && myVolume.GlobalPostProcessingSettings.value) return;

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(RetroVision_RenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
#else
                Destroy(material);
#endif
        }

        public class VHS_STRETCH_Pass : ScriptableRenderPass
        {
            static readonly int _Mask = Shader.PropertyToID("_Mask");
            static readonly int _FadeMultiplier = Shader.PropertyToID("_FadeMultiplier");
            static readonly int MaskThreshold = Shader.PropertyToID("MaskThreshold");

            private Material material;

            public VHS_STRETCH_Pass(Material material)
            {
                this.material = material;
            }
            private void ParamSwitch(Material mat, bool paramValue, string paramName)
            {
                if (paramValue) mat.EnableKeyword(paramName);
                else mat.DisableKeyword(paramName);
            }

            private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
            {
                desc.msaaSamples = 1;

                // This avoids copying the depth buffer tied to the current descriptor as the main pass in this example does not use it
                desc.depthBufferBits = (int)DepthBits.None;

                return desc;
            }
            private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
            {
                Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
            }
            private static void ExecuteCopyColorPass(CopyPassData data, RasterGraphContext context)
            {
                ExecuteCopyColorPass(context.cmd, data.inputTexture);
            }

            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material, int pass)
            {
                Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1f, 1f, 0f, 0f), material, pass);
            }
            private static void ExecuteMainPass(PassData data, RasterGraphContext context, int pass)
            {
                ExecuteMainPass(context.cmd, data.src.IsValid() ? data.src : null, data.material, pass);
            }

            private class PassData
            {
                internal TextureHandle src;
                internal Material material;
            }
            private class CopyPassData
            {
                public TextureHandle inputTexture;
            }
            float TimeX;
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;

                // Use the Volume settings or the default settings if no Volume is set.
                var volumeComponent = VolumeManager.instance.stack.GetComponent<VHS_STRETCH>();

                if (volumeComponent.mask.value != null)
                {
                    material.SetFloat(_FadeMultiplier, 1);
                    material.SetFloat(MaskThreshold, volumeComponent.maskEdgeFineTuning.value);
                    material.SetTexture(_Mask, volumeComponent.mask.value);
                    ParamSwitch(material, volumeComponent.maskChannel.value == maskChannelMode.alphaChannel ? true : false, "ALPHA_CHANNEL");
                }
                else
                {
                    material.SetFloat(_FadeMultiplier, 0);
                }

                if (volumeComponent.unscaledTime.value) { TimeX = Time.unscaledTime; }
                else TimeX = Time.time;

                material.SetFloat("screenLinesNum", volumeComponent.stretchResolution.value);
                material.SetFloat("Fade", volumeComponent.Fade.value);
                material.SetFloat("time_", TimeX * volumeComponent.speed.value);

                ApplyPreset(material, ((int)volumeComponent.StretchMode.value));

                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                var colorCopyDescriptor = GetCopyPassTextureDescriptor(cameraData.cameraTargetDescriptor);

                TextureHandle copiedColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, colorCopyDescriptor, "_CustomPostPassColorCopy", false);

                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("CustomPostPass_CopyColor", out var passData, profilingSampler))
                {
                    passData.inputTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(resourcesData.activeColorTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(copiedColor, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyColorPass(data, context));
                }

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("CustomPostPass", out var passData, profilingSampler))
                {
                    passData.material = material;

                    passData.src = copiedColor;
                    builder.UseTexture(copiedColor, AccessFlags.Read);

                    builder.SetRenderAttachment(resourcesData.activeColorTexture, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteMainPass(data, context, 0));
                }
            }
            static readonly int StretchBandsID = Shader.PropertyToID("_StretchBands");
            public const int MaxBands = 6;
            public static void ApplyPreset(Material mat, int presetIndex)
            {
                presetIndex = Mathf.Clamp(presetIndex, 0, Presets.Length - 1);
                var src = Presets[presetIndex];

                var arr = new Vector4[MaxBands];
                int n = Mathf.Min(src.Length, MaxBands);
                for (int i = 0; i < n; i++) arr[i] = src[i];
                for (int i = n; i < MaxBands; i++) arr[i] = Vector4.zero;

                mat.SetVectorArray(StretchBandsID, arr);
            }
            public static readonly Vector4[][] Presets = new Vector4[][]
            {
        // 0 SubtleTwoBand
        new[] {
            new Vector4(14f, 0.90f,  0.20f, 0.75f),
            new Vector4(10f, 1.10f,  0.35f, 0.15f),
        },

        // 1 ClassicThreeBand
        new[] {
            new Vector4(15f, 1.00f,  0.50f, 0.00f),
            new Vector4( 8f, 1.20f,  0.45f, 0.50f),
            new Vector4(11f, 0.50f, -0.35f, 0.25f),
        },

        // 2 HeavyFiveBand
        new[] {
            new Vector4(18f, 0.50f,  1.10f, 0.50f),
            new Vector4(12f, 2.00f,  0.10f, 0.00f),
            new Vector4(20f, 0.80f,  0.65f, 0.10f),
            new Vector4( 9f, 1.60f, -0.25f, 0.40f),
            new Vector4(16f, 0.70f,  0.80f, 0.30f),
        },

        // 3 TightFourBand
        new[] {
            new Vector4(22f, 1.10f, -0.60f, 0.35f),
            new Vector4(20f, 0.80f,  0.65f, 0.10f),
            new Vector4(15f, 1.30f,  0.30f, 0.85f),
            new Vector4(12f, 0.90f,  0.50f, 0.25f),
        },

        // 4 SlowTwoBandSweep
        new[] {
            new Vector4(24f, 0.55f,  0.10f, 0.15f),
            new Vector4(18f, 0.50f,  0.20f, 0.75f),
        },

        // 5 FastThreeBandSweep
        new[] {
            new Vector4(12f, 2.20f,  0.00f, 0.00f),
            new Vector4(10f, 2.00f,  0.30f, 0.60f),
            new Vector4( 8f, 1.80f,  0.45f, 0.25f),
        },

        // 6 SlowThreeBand
        new[] {
            new Vector4(20f, 0.55f,  0.60f, 0.35f),
            new Vector4(12f, 0.50f,  0.20f, 0.75f),
            new Vector4(16f, 0.60f,  0.85f, 0.15f),
        },

        // 7 ScanlineThreeBand
        new[] {
            new Vector4(18f, 0.60f,  1.10f, 0.50f),
            new Vector4(14f, 0.80f,  0.75f, 0.25f),
            new Vector4(12f, 1.00f,  0.65f, 0.10f),
        },

        // 8 LightTwoBand
        new[] {
            new Vector4(10f, 1.20f,  0.45f, 0.50f),
            new Vector4(14f, 0.90f,  0.20f, 0.75f),
        },

        // 9 StrongSixBand
        new[] {
            new Vector4(12f, 2.20f,  0.00f, 0.00f),
            new Vector4(15f, 1.00f,  0.50f, 0.00f),
            new Vector4( 8f, 2.30f,  0.30f, 0.85f),
            new Vector4(20f, 0.80f,  0.65f, 0.10f),
            new Vector4(22f, 1.00f, -0.60f, 0.35f),
            new Vector4(11f, 0.50f, -0.35f, 0.25f),
        },
            };
        }
    }
}


