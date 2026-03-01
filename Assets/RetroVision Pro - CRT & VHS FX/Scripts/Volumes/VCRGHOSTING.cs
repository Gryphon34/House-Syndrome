using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id3")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/VCR Ghosting")]
public class VCRGHOSTING : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Overall blend 0..1 (0 = original, 1 = full ghosted sum)")]
    public ClampedFloatParameter _Amount = new ClampedFloatParameter(0f, 0f, 1f);
    [Tooltip("Horizontal blur radius for echoes, in pixels (0 = sharp)")]
    public NoInterpClampedFloatParameter _BlurGhostPx = new (1f, 0f, 3f);
    [Tooltip("Multiplier for echo horizontal offsets")]
    public NoInterpClampedFloatParameter GhostOffset = new (.1f, 0f, 3f);
    [Tooltip("Echo tilt in pixels per scanline")]
    public NoInterpClampedFloatParameter _Tilt = new (-0.05f, -.5f, .5f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);
    public bool IsActive() => _Amount.value > 0;

    public bool IsTileCompatible() => false;
}
}