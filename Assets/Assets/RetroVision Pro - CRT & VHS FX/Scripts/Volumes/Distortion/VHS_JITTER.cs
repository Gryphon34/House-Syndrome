using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id11")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/VHS Jitter")]
public class VHS_JITTER : VolumeComponent, IPostProcessComponent
{
    [Range(0f, 5f), Tooltip("Amount of horizontal interlacing.")]
    public ClampedFloatParameter jitterHorizontalAmount = new ClampedFloatParameter(0f, 0f, 10f, true);
    public ClampedFloatParameter jitterHorizontalSpeed = new ClampedFloatParameter(0f, 0f, 1f, true);
    [Space]
    [Range(0f, 15f), Tooltip("Amount of shake.")]
    public ClampedFloatParameter jitterVerticalAmount = new ClampedFloatParameter(0f, 0f, 15f, true);
    [Range(0f, 15f), Tooltip("Speed of vertical shake. ")]
    public NoInterpClampedFloatParameter jitterVerticalSpeed = new NoInterpClampedFloatParameter(1f, 0f, 15f);
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


    public bool IsActive() => jitterHorizontalAmount.value > 0 || jitterVerticalAmount.value > 0;

    public bool IsTileCompatible() => false;
}
}