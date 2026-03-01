using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id13")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Noise/Signal Noise")]
public class SIGNAL_NOISE : VolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);
	[Space]
	[Tooltip("Signal Noise Power")]
	public ClampedFloatParameter SignalNoisePower = new ClampedFloatParameter(0.9f, 0.5f, 0.97f);
	[Tooltip("Signal Noise Amount")]
	public ClampedFloatParameter SignalNoiseAmount = new ClampedFloatParameter(1f, 0f, 2f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);


    public bool IsActive() => (bool)enable;

    public bool IsTileCompatible() => false;
}
}