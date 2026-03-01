using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    public enum _AnimMode
{
    NoAnimation,
    SmoothLFO,
    SteppedPulses,
    RampSnap,
    RandomBursts
}

[Serializable]
public sealed class AnimModeParameter : VolumeParameter<_AnimMode> { };
[HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id7")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/VHS Tape Rewind")]
public class VHS_TAPE_REWIND : VolumeComponent, IPostProcessComponent
{
    [InspectorName("Blend")]
    [Tooltip("Final mix with the original image. 0 = off, 1 = full effect.")]
    public ClampedFloatParameter fade = new ClampedFloatParameter(0f, 0, 1, true);
    [InspectorName("Animation Mode")]
    [Tooltip("0 NoAnimation • 1 Smooth LFO • 2 Stepped pulses (smoothed) • 3 Ramp + Snap • 4 Random bursts (dense).")]
    public AnimModeParameter AnimationMode = new AnimModeParameter();
    [InspectorName("Intensity")]
    [Tooltip("Displacement amount.")]
    public NoInterpClampedFloatParameter intencity = new NoInterpClampedFloatParameter(0.8f, 0, 5);

    [InspectorName("Region Feather")]
    [Tooltip("Soft top edge thickness as a fraction of screen height. Try 0.05–0.15.")]
    public NoInterpClampedFloatParameter RegionFeather = new NoInterpClampedFloatParameter(0.167f, 0, 1);

    [InspectorName("Region Height")]
    [Tooltip("Effect height from bottom (0–1). 0 = bottom only, 1 = full height.")]
    public NoInterpClampedFloatParameter RegionHeight = new NoInterpClampedFloatParameter(0.337f, 0, 1);

    [InspectorName("Animation Speed")]
    [Tooltip("Speed for modes 1–4. 0 = still. Typical 0.6–1.5.")]
    public NoInterpClampedFloatParameter AnimSpeed = new NoInterpClampedFloatParameter(4f, 0, 5);
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
    [Tooltip("Use Global Post Processing Settings to enable or disable Post Processing in Scene View or via camera. Turn OFF when driving effects per-layer.")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => fade.value > 0;
    public bool IsTileCompatible() => false;
}
}
