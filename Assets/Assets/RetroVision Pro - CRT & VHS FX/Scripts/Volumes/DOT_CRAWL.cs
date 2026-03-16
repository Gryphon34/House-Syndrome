using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id1")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Dot Crawl effect")]

public class DOT_CRAWL : VolumeComponent, IPostProcessComponent
{
	[Space]
    [Tooltip("Effect Amount")]
	public ClampedFloatParameter Amount = new (0f, 0f, 1f);
    [Tooltip("How easily edges trigger the effect.")]
    public NoInterpClampedFloatParameter EdgeSensitivity = new (1f, 0, 1f);
    [Tooltip("Dot Scale.")]
    public NoInterpClampedFloatParameter DotSize = new (1f, 1f, 2f);
    [Tooltip("How fast the checkerboard crawls.")]
    public NoInterpClampedFloatParameter AnimationSpeed = new (1f, 0, 3f);
    [Tooltip("Edge detector modes (0: Central, 1: Sobel, 2: Prewitt, 3: Scharr, 4: Roberts, 5: ChromaDiff)")]
    public NoInterpClampedIntParameter EdgeDetectionMode = new (1, 0, 5);
    [Tooltip("Shift dots a bit away from the edge (in pixels), like real hardware often does.")]
    public NoInterpClampedFloatParameter DotOffsetPx = new (1,-8, 8);
    [Tooltip("Softens dot harshness")]
    public NoInterpClampedFloatParameter DotFeatherPx = new (2, 0, 2);
    public NoInterpClampedFloatParameter _BleedPower = new (1, 0, 4);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);

    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => Amount.value > 0;

    public bool IsTileCompatible() => false;
}
}