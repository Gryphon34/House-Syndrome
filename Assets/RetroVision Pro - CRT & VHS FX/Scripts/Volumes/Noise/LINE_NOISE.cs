using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id16")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Noise/Line Noise")]
public class LINE_NOISE : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter LineNoiseFade = new ClampedFloatParameter(0f, 0f, 2f);
    [Tooltip("Line Noise Amount")]
    public NoInterpClampedFloatParameter LineNoiseAmount = new (.1f, 0f, .2f);
    [Tooltip("Line Noise Speed")]
    public NoInterpClampedFloatParameter LineNoiseSpeed = new (1f, 0f, 10f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);

    [Tooltip("Time.unscaledTime.")]
	public BoolParameter unscaledTime = new BoolParameter(false);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);


    public bool IsActive() => LineNoiseFade.value > 0;

    public bool IsTileCompatible() => false;
}
}