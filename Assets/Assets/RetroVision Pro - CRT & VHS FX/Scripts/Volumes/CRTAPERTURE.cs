using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id6")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/CRT Aperture")]
public class CRTAPERTURE : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter Fade = new ClampedFloatParameter(0, 0, 1, true);
    [Tooltip("Glow Halation.")]
    public NoInterpClampedFloatParameter HaloStrength = new NoInterpClampedFloatParameter(5f, 0f, 5f);
    [Tooltip("Glow Difusion.")]
    public NoInterpClampedFloatParameter GlowDiffusion = new NoInterpClampedFloatParameter(2f, 0f, 2f);
    [Tooltip("Mask Colors.")]
    public NoInterpClampedFloatParameter MaskColorCount = new NoInterpClampedFloatParameter(5f, 0f, 5f);
    [Tooltip("Mask Strength.")]
    public NoInterpClampedFloatParameter MaskStrength = new NoInterpClampedFloatParameter(1f, 0f, 1f);
    [Tooltip("Gamma Input.")]
    public NoInterpClampedFloatParameter InputGamma = new NoInterpClampedFloatParameter(5f, 0f, 5f);
    [Tooltip("Gamma Output.")]
    public NoInterpClampedFloatParameter OutputGamma = new NoInterpClampedFloatParameter(5f, 0f, 5f);
    [Tooltip("Brightness.")]
    public NoInterpClampedFloatParameter OutputBrightness = new NoInterpClampedFloatParameter(0.26f, 0f, 2.5f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);

    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => Fade.value > 0;

    public bool IsTileCompatible() => false;
}
}