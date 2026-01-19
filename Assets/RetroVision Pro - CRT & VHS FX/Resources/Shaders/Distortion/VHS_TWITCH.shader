Shader "RetroVisionPro/CRT&VHSFX/Distortion/VHS_TWITCH"
{
	Properties
	{
		_BlitTexture("CTexture", 2D) = "white" {}
	}

    HLSLINCLUDE
    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // Optional mask to gate the effect
	TEXTURE2D(_Mask);
	SAMPLER(sampler_Mask);
	float _FadeMultiplier;
	#pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;
    // Main texture
    float _Intensity;

    // Line counters (can be overridden externally)
	float screenLinesNum = 240.0;
	float noiseLinesNum = 240.0;
	float noiseQuantizeX = 1.0;

    // Runtime scratch values (set externally or derived)
	float time_ = 0.0;
	float SLN = 0.0;
	float SLN_Noise = 0.0;
	float ONE_X = 0.0;
	float ONE_Y = 0.0;

    // ---------- Twitch band controls ----------
    #define MAX_TWITCH_BANDS 6
    // Each band: (hFreq, hDuty 0..1, vFreq, vDuty 0..1)
    // Duty values are present but not used; gating uses fixed duty for simplicity.
    float4 _TwitchBands[MAX_TWITCH_BANDS];

    // User controls
    float _Amount, _BurstInterval, _Speed, _AmpH, _AmpV;

    // Fixed shaping constants (kept small to avoid user overload)
    static const float kDuty     = 0.35; // on-time inside each gate cycle
    static const float kFeather  = 0.05; // gate edge smoothing
    static const float kRowFocus = 80.0; // active scanline window width
    static const float kScanRate = 0.25; // vertical scan window speed

    // RGB <-> YIQ helpers (kept for potential color-domain effects)
	half3 rgb2yiq(half3 c) {
		return half3(
			(0.2989 * c.x + 0.5959 * c.y + 0.2115 * c.z),
			(0.5870 * c.x - 0.2744 * c.y - 0.5229 * c.z),
			(0.1140 * c.x - 0.3216 * c.y + 0.3114 * c.z)
		);
	}

	half3 yiq2rgb(half3 c) {
		return half3(
			(1.0 * c.x + 1.0 * c.y + 1.0 * c.z),
			(0.956 * c.x - 0.2720 * c.y - 1.1060 * c.z),
			(0.6210 * c.x - 0.6474 * c.y + 1.7046 * c.z)
		);
	}

    // Global burst gate over a repeating interval (seconds)
	float masterGate(float t, float intervalSec)
	{
        float ph = frac(t / max(1e-3, intervalSec));
        float a  = smoothstep(0.0, kFeather, ph);
        float b  = 1.0 - smoothstep(kDuty, kDuty + kFeather, ph);
        return saturate(a * b);
	}

    // Per-band gate using frequency; duty is fixed by constants above
	float onOffSmooth(float freq, float t)
	{
        float ph = frac(t * max(freq, 1e-4));
        float a  = smoothstep(0.0, kFeather, ph);
        float b  = 1.0 - smoothstep(kDuty, kDuty + kFeather, ph);
        return saturate(a * b);
	}

    // Vertical twitch: wraps UV.y with a time-varying offset
	float2 ApplyVerticalTwitchBand(float2 uv, float time, float vFreq, float ampV)
	{
        if (vFreq <= 0.0) return uv;
        float gate  = onOffSmooth(vFreq, time);
        float shift = (0.4 * ampV) * gate *
                      (sin(time)*sin(time*20.0) + (0.5 + 0.1*sin(time*200.0)*cos(time)));
        uv.y = frac(uv.y + shift);
        return uv;
	}

    // Horizontal twitch: offsets UV.x near a moving scanline window
	float2 ApplyHorizontalTwitchBand(float2 uv, float time, float hFreq, float ampH)
	{
        if (hFreq <= 0.0) return uv;
        float scanlineCenter = frac(time * kScanRate);
        float dy = uv.y - scanlineCenter;
        float rowFalloff = 1.0 / (1.0 + kRowFocus * dy * dy); // roll-off from active scanline
        float gate = onOffSmooth(hFreq, time);
        float offset =
            (sin(uv.y * 10.0 + time) / 50.0) *
            gate * (1.0 + cos(time * 80.0)) *
            rowFalloff * ampH;
        uv.x += offset;
        return uv;
	}

	// Linear repeat sampler for the main texture (declared for completeness)
	SamplerState sampler_linear_repeat;

    // Main fragment shader
	float4 Frag0(Varyings i) : SV_Target
	{
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float t = time_;
        float2 p = i.texcoord;

        // Initialize line counts and pixel size if not set
        if (screenLinesNum == 0.0) screenLinesNum = _ScreenParams.y;
        SLN = screenLinesNum;
        SLN_Noise = noiseLinesNum;
        if (SLN_Noise == 0 || SLN_Noise > SLN) SLN_Noise = SLN;

        ONE_X = 1.0 / _ScreenParams.x;
        ONE_Y = 1.0 / _ScreenParams.y;

        // --- Preset-driven twitch bands (2–6 entries). Minimal user controls.
        float tScaled = t * _Speed;

        // Global burst gate for the whole packet of bands
        float master = masterGate(t, _BurstInterval);
        if (master > 1e-3)
        {
            [unroll]
            for (int it=0; it<MAX_TWITCH_BANDS; ++it)
            {
                float4 b = _TwitchBands[it]; // (hFreq, hDuty, vFreq, vDuty) — duty ignored
                if (b.x<=0.0 && b.z<=0.0) break;

                // Frequencies come from the preset; amplitudes come from sliders
                p = ApplyVerticalTwitchBand  (p, tScaled, b.z, _AmpV * master);
                p = ApplyHorizontalTwitchBand(p, tScaled, b.x, _AmpH * master);
            }
        }

        // Fetch color with displaced UV and original color for blending
        half3 col   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).rgb;
        half4 colIn = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

        float fd = 1;
                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                fd *= maskVal;
            }

        // Final user-facing blend controlled by _Amount (0..1)
        return lerp(colIn, float4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).a), fd * _Amount);
	}

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#VHS_TWITCH#"
			Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment Frag0
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
