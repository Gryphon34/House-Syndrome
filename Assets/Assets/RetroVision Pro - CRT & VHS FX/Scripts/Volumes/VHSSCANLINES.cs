using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{

    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id5")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/VHS Scanlines")]

public class VHSSCANLINES : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Effect fade.")]
    public ClampedFloatParameter Fade = new ClampedFloatParameter(0f, 0f, 1f, true);
    [Tooltip("Lines color.")]
    public ColorParameter ScanLinesColor = new ColorParameter(new Color());
    [Tooltip("Amount of scanlines.")]
    public NoInterpFloatParameter ScanlineDensity = new NoInterpFloatParameter(0.58f);
    [Tooltip("Lines speed.")]
    public NoInterpClampedFloatParameter Speed = new NoInterpClampedFloatParameter(1, 0, 5);
    [Tooltip("Enable horizontal lines.")]
    public BoolParameter EnableHorizontal = new BoolParameter(true);
    [Tooltip("Jitter Distortion.")]
    public NoInterpClampedFloatParameter JitterDivisor = new NoInterpClampedFloatParameter(0f, 0f, 0.5f);
    [Tooltip("Fisheye Spherical Distortion.")]
    public NoInterpClampedFloatParameter FisheyeSpherical = new NoInterpClampedFloatParameter(0.1f, -2, 2);
    [Tooltip("FisheyeBarrel Distortion.")]
    public NoInterpClampedFloatParameter FisheyeBarrel = new NoInterpClampedFloatParameter(0, -2, 2);
    [Tooltip("Scale Fisheye size.")]
    public NoInterpClampedFloatParameter FisheyeScale = new NoInterpClampedFloatParameter(1,0,2);
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