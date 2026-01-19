using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id20")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/NTSC Codec")]
public class NTSCCODEC : VolumeComponent, IPostProcessComponent
{
    // Blending / intensity
    [Tooltip("Overall blend with the original image. 0 = off, 1 = fully encoded/decoded.")]
    public ClampedFloatParameter Amount = new ClampedFloatParameter(0f, 0f, 1f, true);

    // Line timing / phase flavour
    [Tooltip("Line scan-time offset (advanced). Small +/- values change subcarrier interaction and artifact strength.")]
    public NoInterpClampedFloatParameter LineScanTimeOffset = new NoInterpClampedFloatParameter(4f, -20f, 20f);

    [Tooltip("Line phase randomness (X = amount 0..1, Y = seed). X controls how much per-line phase varies; Y is a seed.")]
    public Vector2Parameter LinePhaseRandomness = new Vector2Parameter(new Vector2(0.25f, 0f));

    // Signal gains
    [Tooltip("Luma gain (brightness of Y after decode). Use 1.0 for neutral.")]
    public NoInterpClampedFloatParameter LumaGain = new NoInterpClampedFloatParameter(-0.1f, -20f, 20f);

    [Tooltip("Chroma saturation (I/Q amplitude). Higher values increase rainbowing/cross-color.")]
    public NoInterpClampedFloatParameter ChromaSaturation = new NoInterpClampedFloatParameter(0f, -20f, 20f);

    // Horizontal prefilter
    [Tooltip("Horizontal luma prefilter width. Higher = softer, fewer artifacts; lower = sharper, more artifacts.")]
    public NoInterpClampedFloatParameter LumaBlurWidth = new NoInterpClampedFloatParameter(1f, 0f, 2f);

    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);


    [Space]
    [Tooltip("Use Global Post Processing Settings to toggle post-processing in Scene View/cameras. Leave OFF if driving effects per-layer.")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => Amount.value > 0;
    public bool IsTileCompatible() => false;
}
}
