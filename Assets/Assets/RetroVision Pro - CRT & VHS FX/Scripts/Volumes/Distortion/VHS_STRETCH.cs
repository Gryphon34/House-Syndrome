using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    public enum _StretchMode
{
    SubtleTwoBand = 0,        // gentle, light stretch
    ClassicThreeBand = 1,     // balanced VHS-style stretch
    HeavyFiveBand = 2,        // dense, worn tape look
    TightFourBand = 3,        // thin, frequent lines
    SlowTwoBandSweep = 4,     // slow broad sweeps
    FastThreeBandSweep = 5,   // quick, lively sweeps
    SlowThreeBand = 6,        // steady, slow movement
    ScanlineThreeBand = 7,    // emphasis along scanlines
    LightTwoBand = 8,         // barely-there stretch
    StrongSixBand = 9,        // aggressive layered stretch
}
[Serializable]
public sealed class stretchModeParameter : VolumeParameter<_StretchMode> { };
[HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id10")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/VHS Stretch")]
public class VHS_STRETCH : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter Fade = new (0f, 0f, 1f);
    public NoInterpClampedFloatParameter speed = new NoInterpClampedFloatParameter(1f, 0f, 5f);
    [Space]
    public stretchModeParameter StretchMode =  new stretchModeParameter();

    [Tooltip("Stretch Resolution.")]
    public NoInterpFloatParameter stretchResolution = new NoInterpFloatParameter(240f);
    [Space]
    [Tooltip("Time.unscaledTime .")]
    public BoolParameter unscaledTime = new BoolParameter(false);
    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);
    [Space]
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in scene view or via camera setup. THIS SETTING SHOULD BE TURNED OFF FOR EFFECTS, IN CASE OF USING THEM FOR SEPARATE LAYERS")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);


    public bool IsActive() => Fade.value > 0;

    public bool IsTileCompatible() => false;
}
}