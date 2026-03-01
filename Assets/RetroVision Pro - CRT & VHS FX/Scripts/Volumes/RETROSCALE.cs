using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{

    [HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id4")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/RetroScale")]
public class RETROSCALE : VolumeComponent, IPostProcessComponent
{
    public BoolParameter enable = new BoolParameter(false);
    [InspectorName("CRT 4:3 Mode")]
    [Tooltip("Enforce a 4:3 gate before sampling. Adds pillar- or letter-boxing on widescreens. Off keeps native aspect.")]
    public BoolParameter CRT43Mode = new BoolParameter(false);
    [Tooltip("“Internal effect resolution. 1 = native, 0.5 = half res. Lower = more blocky pixels.")]
    public ClampedFloatParameter RenderScale = new ClampedFloatParameter(0.5f,0,1);
    [Tooltip("Retro chroma subsampling amount. 0 = full RGB, 1 = strong color bleed (≈4:2:2/4:1:1). Luma stays sharp; color softens.")]
    public ClampedFloatParameter ChromaCompression = new ClampedFloatParameter(1,0,1);
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