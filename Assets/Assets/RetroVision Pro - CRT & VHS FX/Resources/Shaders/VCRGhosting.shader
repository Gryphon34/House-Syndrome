    // -----------------------------------------------------------------------------
    // VCRGhosting
    // Simulates multi-path/ghost echoes (pre/post-echo) seen on analog TV/VCR:
    // several faint, horizontally shifted copies of the image summed back over
    // the original. Optional horizontal blur per echo and a per-scanline tilt
    // -----------------------------------------------------------------------------
Shader "RetroVisionPro/CRT&VHSFX/VCRGhosting"
{
    Properties
    {
        _BlitTexture ("Source", 2D) = "white" {}
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
        // ===== User parameters (set from Volume/Material) =====
        float _BlurGhostPx;   // Horizontal blur radius for echoes, in pixels (0 = sharp)
        float _Amount;        // Overall blend 0..1 (0 = original, 1 = full ghosted sum)
        float _GhostOffset;   // Global multiplier for echo horizontal offsets (scales all taps)
        float _Tilt;          // Echo tilt in pixels per scanline (positive = more delay lower in frame)

        // -----------------------------------------------------------------------------
        // Blur3_H: small, cheap 3-tap horizontal blur centered at uvCenter.
        // Uses a fixed Gaussian-like kernel: [side = 0.60653, center = 1.0].
        // Blur radius is expressed in pixels via _BlurGhostPx and converted to UV.
        // If radius ~ 0, returns a single sample (no blur).
        // -----------------------------------------------------------------------------
        float3 Blur3_H(float2 uvCenter)
        {
            float sPx = max(0.0, _BlurGhostPx);
            if (sPx <= 1e-4)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter).rgb; // no blur
            }

            // Convert pixel radius to UV step along X
            float stepUV = sPx / _ScreenParams.x;
            float2 duv   = float2(stepUV, 0);

            // Fixed 3-tap weights (Gaussian with σ≈1): center=1, sides≈0.60653
            const float wC = 1.0;
            const float wS = 0.6065306597;
            const float norm = rcp(wC + 2.0 * wS); // normalize to keep energy

            float3 c0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter).rgb;
            float3 cL = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter - duv).rgb;
            float3 cR = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter + duv).rgb;

            return (c0*wC + (cL + cR)*wS) * norm;
        }

        // -----------------------------------------------------------------------------
        // SampleGhost: returns one ghost/echo sample:
        //  - offsetPx: horizontal echo offset in pixels (positive = to the right).
        //  - gain:     echo amplitude (0..~0.5 typical). If <= 0, returns 0.
        //  - tiltPxPerLine: additional offset that grows linearly with scanline index
        //                   (simulates multi-path/geometry tilt across the frame).
        // The echo can be optionally blurred using Blur3_H to mimic band-limited paths.
        // -----------------------------------------------------------------------------
        float3 SampleGhost(float2 uv, float offsetPx, float gain, float tiltPxPerLine)
        {
            if (gain <= 0.0) return 0.0.xxx;

            // Convert vertical UV to a scanline index (in pixels)
            float line1 = uv.y * _ScreenParams.y;

            // Total horizontal UV shift = (base offset + per-line tilt * line) / width
            float dxUv = (offsetPx + tiltPxPerLine * line1) / _ScreenParams.x;

            // Shift the sampling position to produce the echo
            float2 uvEcho = uv + float2(dxUv, 0.0);

            // Optionally blur the echo a little (band-limited ghosting)
            float3 c = Blur3_H(uvEcho);

            return c * gain;
        }

        // -----------------------------------------------------------------------------
        // Fragment: accumulate 4 right-hand echoes with different delays and gains,
        // scaled by _GhostOffset, each with the same tilt. Then blend with original
        // using _Amount.
        // -----------------------------------------------------------------------------
        half4 Frag(Varyings i) : SV_Target
        {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            float2 uv = i.texcoord;

            // Base image
            float4 C0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

            // Accumulator (start from original to make blending intuitive)
            float3 C = C0;

            // Four echoes: distances (in px) and gains chosen to look VCR-like.
            // Multiply offsets by _GhostOffset so the user can push/pull the spread.
            C += SampleGhost(uv, 12 * _GhostOffset, 0.20, _Tilt);
            C += SampleGhost(uv, 24 * _GhostOffset, 0.12, _Tilt);
            C += SampleGhost(uv, 40 * _GhostOffset, 0.07, _Tilt);
            C += SampleGhost(uv, 64 * _GhostOffset, 0.04, _Tilt);
            C *= 0.75;

                                // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                _Amount *= maskVal;
            }
            // Final mix: _Amount = 0 → original, 1 → fully ghosted sum
            float3 outCol = lerp(C0, C, _Amount);
            return float4(outCol, 1.0);
        }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#VCRGhosting#"
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment Frag
                #pragma vertex   Vert
            ENDHLSL
        }
    }
    Fallback Off
}
