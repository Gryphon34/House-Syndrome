using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    public enum TapePresets
{
    CleanBroadcast = 0,
    VCR = 1,
    Pause = 2,
    WrinkledSegment = 3,
    LongPlayEP = 4,
    DirtyHeadsBursts = 5
}
public enum ColorModes { Color, Rainbow }

[Serializable] public sealed class TapePresetsParameter : VolumeParameter<TapePresets> { }
[Serializable] public sealed class ColorModeParameter : VolumeParameter<ColorModes> { }

[HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id14")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Noise/Tape Noise")]
public class TAPE_NOISE : VolumeComponent, IPostProcessComponent
{
    [Header("Main")]
    [Tooltip("Overall mix. 0 = off, 1 = full effect.")]
    public ClampedFloatParameter Fade = new(0f, 0f, 1f);

    [Tooltip("Pre-tuned parameter sets for typical VHS looks.")]
    public TapePresetsParameter TapePreset = new();

    [Tooltip("Virtual vertical resolution (0–1). Higher = Lower Resolution.")]
    public NoInterpClampedFloatParameter VerticalResolution = new(1f, 0f, 1f);

    [Tooltip("Animation rate of noise. Negative = reverse.")]
    public NoInterpClampedFloatParameter Speed = new(0.5f, -1.5f, 5f);

    [Header("Jitter")]
    [Tooltip("Mechanical jitter amount. Increases chance of tape-line hits.")]
    public NoInterpClampedFloatParameter TapeJitter = new(0f, 0f, 5f);

    [Header("Noise")]
    [Tooltip("Brightness of noise-lines. 0 = dim, 1 = bright, >1 = very bright.")]
    public NoInterpClampedFloatParameter TapeNoiseFade = new(1f, 0f, 1.5f);

    [Tooltip("Noise-line trigger threshold. Higher = more, Lower = fewer noise-lines.")]
    public NoInterpClampedFloatParameter TapeNoiseAmount = new(1f, 0f, 1f);

    [Header("Tape Lines")]
    [Tooltip("Density of tape-lines per frame. Higher = more hits.")]
    public NoInterpClampedFloatParameter tapeLinesAmount = new(0.8f, 0f, 1f);

    [Tooltip("Color mode for tape-lines: Color = fixed hue, Rainbow = per-scanline hue.")]
    public ColorModeParameter ColorMode = new();

    [Tooltip("Chroma amount for tape-lines (saturation in YIQ).")]
    public ClampedFloatParameter TapeLineColorAmount = new(.02f, 0f, .5f);

    [Tooltip("Hue of tape-lines (radians). Used in Color mode and as base phase in Rainbow.")]
    public ClampedFloatParameter TapeLineHueShift = new(6f, -10f, 10f);

    [Tooltip("Rainbow frequency along scanlines. 1 = ~one hue cycle per virtual frame height.")]
    public ClampedFloatParameter _TapeLineRainbowScale = new(1f, 0f, 100f);

    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);

    [Space]
    [Tooltip("Use unscaled time (ignores Time.timeScale).")]
    public BoolParameter unscaledTime = new(false);

    [Space]
    [Tooltip("Respect global Post Processing toggles. Turn OFF for per-layer control.")]
    public BoolParameter GlobalPostProcessingSettings = new(false);

    public bool IsActive() => Fade.value > 0f;
    public bool IsTileCompatible() => false;
}
}
