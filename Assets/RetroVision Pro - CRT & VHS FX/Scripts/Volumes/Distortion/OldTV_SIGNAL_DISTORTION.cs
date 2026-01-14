using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id2")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/OldTV_SignalDistortion")]
public class OldTV_SIGNAL_DISTORTION : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Effect Amount")]
    public ClampedFloatParameter Amount = new (0f, 0f, 1f);
    [Tooltip("Effect Speed")]
    public NoInterpClampedFloatParameter Speed = new (1f, 0f, 6f);
    [Space]
    [Tooltip("LineJitter Amount")]
    public NoInterpClampedFloatParameter LineJitterAmount = new (.5f, 0f, 6f);
    [Space]
    [Tooltip("Segment Jitter Amount")]
    public NoInterpClampedFloatParameter ChunkJitterAmount = new (1f, 0f, 6f);
    [Tooltip("Segments Per Row")]
    public NoInterpClampedFloatParameter ChunkJitterSegments = new (1f, 1f, 64f);
    [Space]
    [Tooltip("Sine Ripple Amplitude")]
    public NoInterpClampedFloatParameter SineRippleAmount = new (1f, 0f, 4f);
    [Tooltip("Sine Ripple Speed")]
    public NoInterpClampedFloatParameter SineRippleSpeed = new (.06f, 0f, 5f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);
    [Space]
    [Tooltip("Time.unscaledTime .")]
    public BoolParameter unscaledTime = new BoolParameter(false);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);
    public bool IsActive() => Amount.value > 0;

    public bool IsTileCompatible() => false;
}
}