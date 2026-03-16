using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id12")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/Tape Distortion")]
public class TAPE_DISTORTION : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter Fade = new (0f, 0f, 1f);
    [Tooltip("Distortion amplitude per band. 0 = none, 1 = strong warp.")]
    public NoInterpClampedFloatParameter tapeIntensity = new (.5f, 0f, 1f);

    [Tooltip("Temporal speed of band movement. 0 = static, higher = faster drift.")]
    public NoInterpClampedFloatParameter tapeSpeed = new (1f, 0f, 3f);

    [Tooltip("Softness of band edges. Higher values blend distortion into neighbors.")]
    public NoInterpClampedFloatParameter TapeFeather = new (.1f, 0f, 8f);

    [Tooltip("Relative band thickness (vertical). 0 = thin line, 1 = full screen height.")]
    public NoInterpClampedFloatParameter TapeWidth = new (.05f, 0f, 1f);

    [Tooltip("Band count over screen height. Higher = more, thinner waves.")]
    public NoInterpClampedFloatParameter TapeFrequency = new (2f, 0f, 8f);

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

    public bool IsActive() => Fade.value > 0;

    public bool IsTileCompatible() => false;
}
}