using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{

    [VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Fisheye")]

public class FISHEYE_VIGNETTE: VolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);
    [Tooltip("Zoom factor through the lens. 0 = natural.")]
    public ClampedFloatParameter FisheyeSize = new(.6f, 0f, 2.0f);

    [Tooltip("Hard crop at left/right in pixels.")]
    public ClampedFloatParameter CutoffX = new(50f, 0f, 128f);

    [Tooltip("Hard crop at top/bottom in pixels.")]
    public ClampedFloatParameter CutoffY = new(60f, 0f, 128f);

    [Tooltip("Soft fade width at left/right in pixels.")]
    public ClampedFloatParameter CutoffFadeX = new(100f, 0f, 256f);

    [Tooltip("Soft fade width at top/bottom in pixels.")]
    public ClampedFloatParameter CutoffFadeY = new(40f, 0f, 256f);

    [Tooltip("Per-channel bend for subtle chromatic aberration.")]
    public ClampedFloatParameter ChromaFringe = new(0f, 0f, 1f); [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => enable.value;

    public bool IsTileCompatible() => false;
}
}