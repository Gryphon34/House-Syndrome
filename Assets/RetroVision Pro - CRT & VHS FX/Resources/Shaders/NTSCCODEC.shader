    // NTSCCodec (composite YIQ encode/decode sim)
    // Encodes RGB to YIQ, modulates chroma on an NTSC subcarrier (Fsc),
    // applies simple horizontal filtering (luma notch/LPF, chroma LPF),
    // then decodes back to RGB.
Shader "RetroVisionPro/CRT&VHSFX/NTSCCodec"
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
        TEXTURE2D(_Mask);
    SAMPLER(sampler_Mask);
    float  MaskThreshold;  

        // Mask fade
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL


    // --- Constants / matrices for RGB<->YIQ and subcarrier ---
    #define Pi2 6.283185307f
    static const float3 YTransform = float3(0.299, 0.587, 0.114);
    static const float3 ITransform = float3(0.595716, -0.274453, -0.321263);
    static const float3 QTransform = float3(0.211456, -0.522591, 0.311135);
    #define MinC   (-0.10f)   // composite normalization min
    #define CRange (3.2366f)  // composite normalization range
    static const float Fsc = 3.579545f; // NTSC subcarrier (MHz), used relatively

    // --- User-driven uniforms (set from script/volume) ---
    float   T;               // (unused here; keep for parity if you use externally)
    half    val1;            // small scan-time tweak (adds to base line scan time)
    half    fade;            // final blend 0..1 with base RGB
    float   _BrightnessY;    // luma gain (post decode)
    float   _ChromaSat;      // chroma saturation scale (I/Q amplitude)
    float   _RandomizeVal;   // phase randomization vs. line index (adds grunginess)
    float   _YBlur;          // horizontal prefilter reach for Y sampling (higher = softer)

    // RGB -> YIQ sample with chroma saturation scaling
    inline float3 SampleYIQ(float2 uv)
    {
        float3 rgb = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        float  Y = dot(rgb, YTransform);
        float  I = dot(rgb, ITransform) * _ChromaSat;
        float  Q = dot(rgb, QTransform) * _ChromaSat;
        return float3(Y,I,Q);
    }

    // Encode 4 horizontal samples to composite quickly (vectorized by 4)
    inline float4 Composite4_Horiz_Fast(float4 xUV, float yUV, float scanTime, float lineIdx)
    {
        float screenW = _ScreenParams.x;

        // angular frequency of subcarrier scaled by scan time (per pixel)
        float omega   = Pi2 * Fsc * scanTime / screenW;

        // pixel positions and phase for each of the 4 taps
        float4 px     = xUV * screenW;
        float4 phase  = (px + 0.5 * lineIdx) * omega; // +0.5*lineIdx simulates line parity

        // fetch YIQ at each tap
        float3 yiq0 = SampleYIQ(float2(xUV.x, yUV));
        float3 yiq1 = SampleYIQ(float2(xUV.y, yUV));
        float3 yiq2 = SampleYIQ(float2(xUV.z, yUV));
        float3 yiq3 = SampleYIQ(float2(xUV.w, yUV));

        float4 Y = float4(yiq0.x, yiq1.x, yiq2.x, yiq3.x);
        float4 I = float4(yiq0.y, yiq1.y, yiq2.y, yiq3.y);
        float4 Q = float4(yiq0.z, yiq1.z, yiq2.z, yiq3.z);

        // composite = Y + I*cos(phase) + Q*sin(phase)
        float4 encoded = Y + I * cos(phase) + Q * sin(phase);

        // normalize to a friendlier range for filtering math
        return (encoded - MinC) / CRange;
    }

    // Main codec: encode to composite domain, apply simple line-based filters, decode back
    float4 NTSCCodec(float2 UV)
    {
        // Reference/scaling to keep behavior stable across resolutions
        float2 refRes = float2(1000, 555.91000);
        float2 kRes   = _ScreenParams.xy / refRes.xy;

        // Approximate active line time (in "pixels") + tweak (val1)
        float scanTimeBase = 52.6 + val1;
        float scanTime     = scanTimeBase * kRes.x;

        // Base RGB (for fading/mix)
        float4 baseCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV);

        // Precompute normalized rates used by simple sinc-like FIRs
        float screenW = _ScreenParams.x;
        float timePerPixel     = scanTime / screenW;
        float yNotchEdgeLoNorm = (Fsc - 3.0) * timePerPixel; // Y notch lower edge
        float yNotchEdgeHiNorm = (Fsc + 3.0) * timePerPixel; // Y notch upper edge
        float yLumaLpNorm      =  6.0       * timePerPixel;  // Y low-pass
        float iChromLpNorm     =  1.2       * timePerPixel;  // I low-pass
        float qChromLpNorm     =  0.6       * timePerPixel;  // Q low-pass

        // Hamming window spacing in taps
        float piLen = 0.0766242;

        // Pseudo line index for phase randomization (adds natural variance)
        float linesForPhase = refRes.y;
        float lineIdx = floor(UV.y * linesForPhase * _RandomizeVal);

        float2 invRes = 1.0 / _ScreenParams.xy;

        // Accumulators for filtered Y/I/Q (vectorized across 4 taps)
        float4 Yaccum = 0, Iaccum = 0, Qaccum = 0;

        // Horizontal FIR (unrolled by 4 taps each iteration, from -8..+7)
        [unroll(4)]
        for (float n = -8.0; n < 8.0; n += 4.0)
        {
            // Tap offsets (4 at a time), with small sub-pixel bias for stability
            float4 sampleOffsets4 = n * 0.5 + float4(0.1, 1.1, 2.1, 3.1);

            // Horizontal coordinate with adjustable blur reach (_YBlur)
            float4 coordX = UV.x + invRes.x * sampleOffsets4 * 1.682 * _YBlur;
            float  coordY = UV.y;

            // Encode to normalized composite for these 4 taps
            float4 C = Composite4_Horiz_Fast(coordX, coordY, scanTime, lineIdx);

            // Back to composite magnitude domain for filtering
            C = C * CRange + MinC;

            // Phase for demodulation of I/Q at each tap
            float4 px4 = coordX * screenW;
            float  omega = Pi2 * Fsc * scanTime / screenW;
            float4 wt  = (px4 + 0.5 * lineIdx) * omega;

            float4 sWt, cWt; sincos(wt, sWt, cWt);

            // Simple windowed-sinc approximations for Y notch + LPF
            float4 yNotchLoSinc = sin(Pi2 * yNotchEdgeLoNorm * sampleOffsets4) / (Pi2 * yNotchEdgeLoNorm * sampleOffsets4);
            float4 yNotchHiSinc = sin(Pi2 * yNotchEdgeHiNorm * sampleOffsets4) / (Pi2 * yNotchEdgeHiNorm * sampleOffsets4);
            float4 yLumaLpSinc  = sin(Pi2 * yLumaLpNorm      * sampleOffsets4) / (Pi2 * yLumaLpNorm      * sampleOffsets4);

            float4 idealY  = 2.0 * (yNotchEdgeLoNorm * yNotchLoSinc - yNotchEdgeHiNorm * yNotchHiSinc + yLumaLpNorm * yLumaLpSinc);
            float4 filterY = (0.54 + 0.46 * cos(piLen * sampleOffsets4)) * idealY; // Hamming window

            // Chrominance LPFs for I/Q (narrower than Y)
            float4 sincI = sin(Pi2 * iChromLpNorm * sampleOffsets4) / (Pi2 * iChromLpNorm * sampleOffsets4);
            float4 filterI = (0.54 + 0.46 * cos(piLen * sampleOffsets4)) * (2.0 * iChromLpNorm * sincI);

            float4 sincQ = sin(Pi2 * qChromLpNorm * sampleOffsets4) / (Pi2 * qChromLpNorm * sampleOffsets4);
            float4 filterQ = (0.54 + 0.46 * cos(piLen * sampleOffsets4)) * (2.0 * qChromLpNorm * sincQ);

            // Accumulate filtered Y and demodulated I/Q
            Yaccum += C * filterY;
            Iaccum += C * cWt * filterI; // I ~ cos demod
            Qaccum += C * sWt * filterQ; // Q ~ sin demod
        }

        // Collapse 4-wide accumulators and apply gains
        float Y = dot(Yaccum, 1.0.xxxx) * _BrightnessY;
        float I = dot(Iaccum, 1.0.xxxx) * 2.0;
        float Q = dot(Qaccum, 1.0.xxxx) * 2.0;

        // Decode back to RGB from YIQ (BT.601-ish)
        float3 yiq = float3(Y, I, Q);
        float3 rgb = float3(
            dot(yiq, float3(1.0,  0.956,  0.621)),
            dot(yiq, float3(1.0, -0.272, -0.647)),
            dot(yiq, float3(1.0, -1.106,  1.703))
        );

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, UV).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, UV).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                fade *= maskVal;
            }
        // Final blend with original
        return lerp(baseCol, float4(rgb, baseCol.a), fade);
    }

    float4 Frag0(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        return NTSCCodec(i.texcoord);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#NTSCCodec#"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma target 3.5
                #pragma vertex   Vert
                #pragma fragment Frag0
            ENDHLSL
        }
    }
    Fallback Off
}