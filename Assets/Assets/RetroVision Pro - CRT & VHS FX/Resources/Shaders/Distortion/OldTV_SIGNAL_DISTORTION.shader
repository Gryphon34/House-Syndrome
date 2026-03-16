    // -------------------------------------------------------------------------
    // Simulates analog time-base errors:
    //  (1) Per-scanline horizontal wobble (time-base jitter)
    //  (2) Small per-segment jerks within a line (tracking/tearing)
    //  (3) Vertical sine ripple mapped to horizontal shift (power ripple)
    // -------------------------------------------------------------------------
Shader "RetroVisionPro/CRT&VHSFX/OldTV_SignalDistortion" 
{
    Properties
    {
        _BlitTexture ("CTexture", 2D) = "white" {}
    }
    HLSLINCLUDE
    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D(_Mask);
    SAMPLER(sampler_Mask);
    // Mask fade
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;
    // ---------------- User parameters ----------------
    half _Blend;           // Final mix 0..1 (0 = original, 1 = fully distorted)
    half _PlaySpeed;       // Global animation speed multiplier
    half _LineJitterPx;    // Per-scanline horizontal wobble amplitude (pixels)
    half _ChunkJitterPx;   // Extra horizontal jerk per segment (pixels)
    half _ChunksPerRow;    // Number of horizontal segments per scanline
    half _SineRipplePx;    // Sine ripple amplitude mapped to horizontal shift (pixels)

    // Helper: convert pixel delta to UV delta (resolution-aware)
    inline float2 PxToUV(float2 px) { return px / _ScreenParams.xy; }

    // Cheap 1D value noise (smoothstep curve) for stable “analog” motions
    float SmoothNoise1D(float x)
    {
        float i = floor(x), f = frac(x);
        float a = frac(sin(i       * 43758.5453) * 12517.27);
        float b = frac(sin((i+1.0) * 43758.5453) * 12517.27);
        f = f*f*(3.0 - 2.0*f);
        return lerp(a, b, f);
    }

    // Fragment: compute per-row/segment horizontal offset and sample
    half4 Frag(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float2 uv   = i.texcoord;
        float2 res  = _ScreenParams.xy;           // (width, height) in pixels
        float  time = _Time.y * _PlaySpeed;       // global animation time

        // Row index in pixels (scanline number)
        float rowPx = uv.y * res.y;

        // (1) Time-base jitter: slow horizontal wobble per scanline
        float dx = (SmoothNoise1D(rowPx * 0.035 + time * 0.7) - 0.5) * (_LineJitterPx / res.x);

        // (2) Segment jerk: divide each row into N segments with independent small offsets
        float segCount = max(1.0, _ChunksPerRow);
        float segId    = floor(uv.x * segCount);
        float segNoise = (SmoothNoise1D(segId * 3.17 + floor(time * 2.0) + rowPx * 0.002) - 0.5);
        dx += segNoise * (_ChunkJitterPx / res.x);

        // (3) Sine ripple: vertical sine mapped to horizontal displacement
        dx += (_SineRipplePx / res.x) * sin(TWO_PI * (uv.y + time));

        // Apply horizontal distortion
        float2 uvDistorted = uv + float2(dx, 0.0);

        // Sample warped and original, then blend
        float3 warped = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvDistorted).rgb;
        float3 src    = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                _Blend *= maskVal;
            }
        return float4(lerp(src, warped, _Blend), 1.0);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "OldTV_SignalDistortion"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}