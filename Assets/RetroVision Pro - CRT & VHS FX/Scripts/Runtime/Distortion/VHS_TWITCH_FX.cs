using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;
namespace RetroVisionPro
{
    public class VHS_TWITCH_FX : ScriptableRendererFeature
    {
        [SerializeField]
        private Shader m_Shader;
        private Material material;
        VHS_TWITCH_Pass RetroVision_RenderPass;
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            m_Shader = Shader.Find("RetroVisionPro/CRT&VHSFX/Distortion/VHS_TWITCH");
            if (m_Shader == null)
            {
                Debug.Log("Shader not found");

                return;
            }
            material = new Material(m_Shader);
            RetroVision_RenderPass = new VHS_TWITCH_Pass(material);

            RetroVision_RenderPass.renderPassEvent = Event;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            VHS_TWITCH myVolume = VolumeManager.instance.stack?.GetComponent<VHS_TWITCH>();
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

        public class VHS_TWITCH_Pass : ScriptableRenderPass
        {
            static readonly int _Mask = Shader.PropertyToID("_Mask");
            static readonly int _FadeMultiplier = Shader.PropertyToID("_FadeMultiplier");
            static readonly int MaskThreshold = Shader.PropertyToID("MaskThreshold");

            private Material material;

            public VHS_TWITCH_Pass(Material material)
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
                var volumeComponent = VolumeManager.instance.stack.GetComponent<VHS_TWITCH>();

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

                material.SetFloat("time_", TimeX);

                material.SetFloat("_BurstInterval", volumeComponent._BurstInterval.value);
                material.SetFloat("_Speed", volumeComponent._Speed.value);
                material.SetFloat("_AmpH", volumeComponent._AmpH.value);
                material.SetFloat("_AmpV", volumeComponent._AmpV.value);
                material.SetFloat("_Amount", volumeComponent._Amount.value);

                ApplyPreset(material, ((int)volumeComponent.TwitchMode.value));

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
            public enum Preset
            {
                Light_Tracking_OK = 0,      // gentle wobble, like good tracking on a VHS deck
                Classic_Tracking_Error = 1, // typical VHS tracking shake at edges
                Worn_Tape_Heavy_Tracking = 2, // old cassette feel, frequent tracking issues
                Head_Misalign_Lines = 3,    // thin, frequent horizontal line twitch (head misalignment)
                Pause_Jitter = 4,           // quick left–right nudges like pause/jog
                Tape_Wrinkle_Ripples = 5,   // layered ripples from a wrinkled tape
                Vertical_Hold_Drift = 6,    // slow up–down slip like bad vertical hold
                Scanline_Ripple = 7,        // wobble centered around a moving scanline
                Mostly_Clean_Playback = 8,  // very light movement, almost clean
                Severe_Tracking_Fault = 9   // strong multi-layer tracking problems
            }


            // (hFreq, hDuty, vFreq, vDuty) per layer — 2..6 layers each
            public static readonly Vector4[][] Presets = new Vector4[][]
            {
        // 0 Light_Tracking_OK — gentle, stable playback
        new[] {
            new Vector4(0.70f, 0.35f, 0.30f, 0.35f),
            new Vector4(0.90f, 0.35f, 0.40f, 0.35f),
        },

        // 1 Classic_Tracking_Error — typical VHS tracking shake
        new[] {
            new Vector4(1.00f, 0.35f, 0.50f, 0.35f),
            new Vector4(1.20f, 0.35f, 0.45f, 0.35f),
            new Vector4(0.80f, 0.35f, 0.30f, 0.35f),
        },

        // 2 Worn_Tape_Heavy_Tracking — frequent issues, older cassette
        new[] {
            new Vector4(0.90f, 0.35f, 0.60f, 0.35f),
            new Vector4(1.80f, 0.35f, 0.15f, 0.35f),
            new Vector4(0.80f, 0.35f, 0.80f, 0.35f),
            new Vector4(1.40f, 0.35f, 0.30f, 0.35f),
            new Vector4(0.60f, 0.35f, 0.10f, 0.35f),
        },

        // 3 Head_Misalign_Lines — thin, frequent horizontal line jitter
        new[] {
            new Vector4(2.20f, 0.35f, 0.20f, 0.35f),
            new Vector4(1.80f, 0.35f, 0.15f, 0.35f),
            new Vector4(1.40f, 0.35f, 0.00f, 0.35f),
            new Vector4(1.10f, 0.35f, 0.00f, 0.35f),
        },

        // 4 Pause_Jitter — quick left/right nudges like pause/jog
        new[] {
            new Vector4(3.20f, 0.35f, 0.00f, 0.35f),
            new Vector4(2.40f, 0.35f, 0.10f, 0.35f),
        },

        // 5 Tape_Wrinkle_Ripples — layered ripple from wrinkled tape
        new[] {
            new Vector4(0.70f, 0.35f, 0.50f, 0.35f),
            new Vector4(1.10f, 0.35f, 0.35f, 0.35f),
            new Vector4(1.50f, 0.35f, 0.25f, 0.35f),
            new Vector4(0.90f, 0.35f, 0.80f, 0.35f),
            new Vector4(1.70f, 0.35f, 0.20f, 0.35f),
            new Vector4(0.60f, 0.35f, 0.10f, 0.35f),
        },

        // 6 Vertical_Hold_Drift — slow, steady vertical slip
        new[] {
            new Vector4(0.30f, 0.35f, 0.25f, 0.35f),
            new Vector4(0.40f, 0.35f, 0.35f, 0.35f),
            new Vector4(0.20f, 0.35f, 0.15f, 0.35f),
        },

        // 7 Scanline_Ripple — wobble around a moving scanline
        new[] {
            new Vector4(1.10f, 0.35f, 0.60f, 0.35f),
            new Vector4(1.40f, 0.35f, 0.80f, 0.35f),
            new Vector4(1.60f, 0.35f, 0.90f, 0.35f),
        },

        // 8 Mostly_Clean_Playback — almost clean picture
        new[] {
            new Vector4(0.50f, 0.35f, 0.20f, 0.35f),
            new Vector4(0.70f, 0.35f, 0.30f, 0.35f),
        },

        // 9 Severe_Tracking_Fault — strong, mixed problems
        new[] {
            new Vector4(2.00f, 0.35f, 0.90f, 0.35f),
            new Vector4(3.00f, 0.35f, 0.00f, 0.35f),
            new Vector4(1.60f, 0.35f, 0.70f, 0.35f),
            new Vector4(1.20f, 0.35f, 0.50f, 0.35f),
            new Vector4(0.80f, 0.35f, 0.30f, 0.35f),
            new Vector4(0.60f, 0.35f, 0.10f, 0.35f),
        },
            };
            public static void ApplyPreset(Material mat, int presetIndex)
            {
                presetIndex = Mathf.Clamp(presetIndex, 0, Presets.Length - 1);
                var src = Presets[presetIndex];

                var arr = new Vector4[6];
                int n = Mathf.Min(src.Length, 6);
                for (int i = 0; i < n; i++) arr[i] = src[i];
                for (int i = n; i < 6; i++) arr[i] = Vector4.zero;

                mat.SetVectorArray("_TwitchBands", arr);
            }
        }
    }
}