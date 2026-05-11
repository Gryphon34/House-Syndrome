using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id15")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Noise/Filmgrain")]
public class FILMGRAIN : VolumeComponent, IPostProcessComponent
{
	[Tooltip("Granularity Amount")]
	public ClampedFloatParameter GranularityAmount = new ClampedFloatParameter(0f, 0f, 0.5f);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);
    public bool IsActive() => GranularityAmount.value > 0;

    public bool IsTileCompatible() => false;
}
}