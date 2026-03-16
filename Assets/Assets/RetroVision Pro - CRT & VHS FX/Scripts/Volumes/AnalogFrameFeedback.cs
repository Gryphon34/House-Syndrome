using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RetroVisionPro
{
    [Serializable, VolumeComponentMenu("RetroVision Pro/CRT & VHS FX/Analog Frame Feedback")]
    public class AnalogFrameFeedback : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Amplifies the input amount after cutoff.")]
        public ClampedFloatParameter amount = new(0f, 0f, 3f);
        [Tooltip("Brightness threshold of input.")]
        public ClampedFloatParameter cutOff = new(0.5f, 0f, 1f);


        [Tooltip("Controls how fast artefacts fade.")]
        public ClampedFloatParameter fade = new(0.5f, 0f, 1f);
        public ClampedFloatParameter SpreadX = new(0.5f, 0f, 5f);

        [Tooltip("Artefacts color.")]
        public ColorParameter color = new(Color.white, true, true, true);

        [Tooltip("Render artefacts only.")]
        public BoolParameter debugArtefacts = new(false);

        public bool IsActive() => active && amount.value > 1e-4f;
    }
}
