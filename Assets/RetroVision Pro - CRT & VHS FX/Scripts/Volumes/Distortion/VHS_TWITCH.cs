using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace RetroVisionPro
{
    public enum _TwitchMode
{
    [InspectorName("Light Tracking (OK)")]
    Light_Tracking_OK = 0,          // gentle wobble, like good tracking

    [InspectorName("Classic Tracking Error")]
    Classic_Tracking_Error = 1,     // typical VHS tracking shake

    [InspectorName("Worn Tape – Heavy Tracking")]
    Worn_Tape_Heavy_Tracking = 2,   // frequent tracking issues

    [InspectorName("Head Misalignment Lines")]
    Head_Misalign_Lines = 3,        // thin, frequent horizontal line jitter

    [InspectorName("Pause / Jog Jitter")]
    Pause_Jitter = 4,               // quick left–right nudges

    [InspectorName("Tape Wrinkle Ripples")]
    Tape_Wrinkle_Ripples = 5,       // layered ripple artifacts

    [InspectorName("Vertical Hold Drift")]
    Vertical_Hold_Drift = 6,        // slow up–down slip

    [InspectorName("Scanline Ripple")]
    Scanline_Ripple = 7,            // wobble near a moving scanline

    [InspectorName("Mostly Clean Playback")]
    Mostly_Clean_Playback = 8,      // very light movement

    [InspectorName("Severe Tracking Fault")]
    Severe_Tracking_Fault = 9       // strong mixed faults
}

[Serializable]
public sealed class twitchhModeParameter : VolumeParameter<_TwitchMode> { };

[HelpURL("https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/#id9")]
[VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Distortion/VHS Twitch")]
public class VHS_TWITCH : VolumeComponent, IPostProcessComponent
{
    [InspectorName("Preset")]
    [Tooltip("Choose a VHS tracking artifact preset.")]
    public twitchhModeParameter TwitchMode = new twitchhModeParameter();

    [InspectorName("Fade")]
    [Tooltip("Final mix with the original image (0 = off, 1 = full effect).")]
    public ClampedFloatParameter _Amount = new (0f, 0f, 1f);

    [InspectorName("Burst Interval (sec)")]
    [Tooltip("Time between active twitch bursts. Larger value = less frequent bursts.")]
    public NoInterpClampedFloatParameter _BurstInterval = new (1f, 0f, 25f);

    [InspectorName("Horizontal Amplitude")]
    [Tooltip("Strength of left–right (horizontal) shake.")]
    public NoInterpClampedFloatParameter _AmpH = new (1f, 0f, 2f);

    [InspectorName("Vertical Amplitude")]
    [Tooltip("Strength of up–down (vertical) slip.")]
    public NoInterpClampedFloatParameter _AmpV = new (1f, 0f, 1f);

    [InspectorName("Speed")]
    [Tooltip("Global speed multiplier for all twitch bands.")]
    public NoInterpClampedFloatParameter _Speed = new (1f, 0f, 5f);

    [Space]
    [InspectorName("Use Unscaled Time")]
    [Tooltip("Use Time.unscaledTime (ignores timeScale/slow-motion).")]
    public BoolParameter unscaledTime = new BoolParameter(false);

    [Space]
    [Tooltip("Mask texture")]
    public TextureParameter mask = new TextureParameter(null);
    public maskChannelModeParameter maskChannel = new maskChannelModeParameter();
    [Tooltip("Value to adjust mask edge thickness.")]
    public NoInterpClampedFloatParameter maskEdgeFineTuning = new(.15f, 0.000001f, 1f);

    [Space]
    [InspectorName("Global PP Toggle (Info)")]
    [Tooltip("Use Project Settings → Post-processing or Camera settings to enable/disable PP in Scene/Game view. Turn this OFF if you drive effects on separate layers.")]
    public BoolParameter GlobalPostProcessingSettings = new BoolParameter(false);

    public bool IsActive() => _Amount.value > 0;
    public bool IsTileCompatible() => false;
}
}
