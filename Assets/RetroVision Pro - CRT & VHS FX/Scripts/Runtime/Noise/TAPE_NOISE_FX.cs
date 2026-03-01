using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
namespace RetroVisionPro
{
    public class TAPE_NOISE_FX : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader m_Shader;
        private Material material;
        TAPE_NOISE_Pass RetroVision_RenderPass;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            m_Shader = Shader.Find("RetroVisionPro/CRT&VHSFX/TapeNoiseShader");
            if (m_Shader == null)
            {
                return;
            }
            material = new Material(m_Shader);
            RetroVision_RenderPass = new TAPE_NOISE_Pass(material);

            RetroVision_RenderPass.renderPassEvent = Event;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            TAPE_NOISE myVolume = VolumeManager.instance.stack?.GetComponent<TAPE_NOISE>();
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

        public class TAPE_NOISE_Pass : ScriptableRenderPass
        {
            static readonly int _Mask = Shader.PropertyToID("_Mask");
            static readonly int _FadeMultiplier = Shader.PropertyToID("_FadeMultiplier");
            static readonly int MaskThreshold = Shader.PropertyToID("MaskThreshold");

            private Material material;

            public TAPE_NOISE_Pass(Material material)
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
                var volumeComponent = VolumeManager.instance.stack.GetComponent<TAPE_NOISE>();

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

                material.SetFloat("_TimeOverride", TimeX);

                material.SetFloat("M_TapeJitter", volumeComponent.TapeJitter.value);
                material.SetFloat("M_Fade", volumeComponent.Fade.value);
                material.SetFloat("_TapeLineChromaGain", volumeComponent.TapeLineColorAmount.value);
                material.SetFloat("_TapeLineHueShift", volumeComponent.TapeLineHueShift.value);
                material.SetFloat("_TapeLineRainbowScale", volumeComponent._TapeLineRainbowScale.value);
                material.SetFloat("_TapeLineRainbowMode", (int)volumeComponent.ColorMode.value > 0 ? 1 : 0);

                ApplyPreset(material, (int)volumeComponent.TapePreset.value, volumeComponent.TapeJitter.value, volumeComponent);

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
            void ApplyPreset(Material m, int preset, float tapeJitter, TAPE_NOISE volumeComponent)
            {
                // defaults
                float tLA = 1 - volumeComponent.tapeLinesAmount.value, nLN = 500 * volumeComponent.VerticalResolution.value, nQX = 10f, TNF = volumeComponent.TapeNoiseFade.value, TNA = 1 - volumeComponent.TapeNoiseAmount.value, tNS = volumeComponent.Speed.value;
                int W = 20, TL = 20; float Dist = 4f; float seed = .2f;


                // classic coherent branches on CPU
                switch (preset)
                {
                    case 1: tLA /= 0.85f; nLN += 240; nQX += 1.00f; TNF *= 0.35f; TNA /= 0.92f; tNS *= 0.60f; W = 8; Dist = 1.5f; TL = 8; seed = 0.10f; break;
                    case 2: tLA /= 0.60f; nLN += 220; nQX += 0.90f; TNF *= 0.60f; TNA /= 0.80f; tNS *= 1.00f; W = 16; Dist = 2.5f; TL = 14; seed = 0.25f; break;
                    case 3: tLA /= 0.50f; nLN += 200; nQX += 0.75f; TNF *= 0.80f; TNA /= 0.70f; tNS *= 0.30f; W = 24; Dist = 4.0f; TL = 6; seed = 0.35f; break;
                    case 4: tLA /= 0.45f; nLN += 180; nQX += 0.60f; TNF *= 1.00f; TNA /= 0.60f; tNS *= 1.80f; W = 28; Dist = 6.0f; TL = 20; seed = 0.7f; break;
                    case 5: tLA /= 0.70f; nLN += 160; nQX += 0.50f; TNF *= 0.55f; TNA /= 0.85f; tNS *= 0.90f; W = 12; Dist = 2.0f; TL = 10; seed = 0.5f; break;
                    case 6: tLA /= 0.40f; nLN += 240; nQX += 1.00f; TNF *= 1.20f; TNA /= 0.50f; tNS *= 2.20f; W = 8; Dist = 0.5f; TL = 4; seed = 0.95f; break;
                }

                // clamp to shader caps
                W = Mathf.Min(W, 32);
                TL = Mathf.Min(TL, 24);

                m.SetFloat("_TapeLinesAmount", tLA);
                m.SetFloat("_TapeNoiseFade", TNF);
                m.SetFloat("_TapeNoiseAmount", TNA);
                m.SetFloat("_TapeNoiseSpeed", tNS);
                m.SetFloat("_NoiseLinesNum", nLN);
                m.SetFloat("_NoiseQuantizeX", nQX);
                m.SetFloat("_DistAmount", Dist);
                m.SetInt("_DistWidth", W);
                m.SetInt("_TailLength", TL);
                m.SetFloat("_PresetSeed", seed);
            }
        }
    }
}


