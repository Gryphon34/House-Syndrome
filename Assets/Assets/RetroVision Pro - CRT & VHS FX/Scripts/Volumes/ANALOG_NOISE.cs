using UnityEngine;
using UnityEngine.Rendering;
using System;
namespace RetroVisionPro
{
    [Serializable]
public sealed class maskChannelModeParameter : VolumeParameter<maskChannelMode> { };
[HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id8")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Analog Noise")]
public class ANALOG_NOISE : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Effect Fade.")]
    public ClampedFloatParameter fade = new ClampedFloatParameter(0f, 0f, 1f, true);
    [Tooltip("Option enables static noise (without movement).")]
    public BoolParameter staticNoise = new BoolParameter(false);
    [Tooltip("Horizontal/Vertical Noise lines.")]
    public BoolParameter Horizontal = new BoolParameter(true);
    [Range(0f, 60f), Tooltip("Noise bar width.")]
    public NoInterpClampedFloatParameter barWidth = new NoInterpClampedFloatParameter(21f, 0f, 60f);
    [Range(0f, 60f), Tooltip("Noise tiling.")]
    public NoInterpVector2Parameter tile = new NoInterpVector2Parameter(new Vector2(1, 1));
    [Range(0f, 1f), Tooltip("Noise texture angle.")]
    public NoInterpClampedFloatParameter textureAngle = new NoInterpClampedFloatParameter(1f, 0f, 1f);
    [Range(0f, 100f), Tooltip("Noise bar edges cutoff.")]
    public NoInterpClampedFloatParameter edgeCutOff = new NoInterpClampedFloatParameter(0f, 0f, 100f);
    [Range(-1f, 1f), Tooltip("Noise cutoff.")]
    public NoInterpClampedFloatParameter CutOff = new NoInterpClampedFloatParameter(1f, -1f, 1f);
    [Range(-10f, 10f), Tooltip("Noise bars speed.")]
    public NoInterpClampedFloatParameter barSpeed = new NoInterpClampedFloatParameter(1f, -60f, 60f);
    [Tooltip("Noise texture.")]
    public TextureParameter texture = new TextureParameter(null);
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

    public bool IsActive() => fade.value > 0;

    public bool IsTileCompatible() => false;
}
}