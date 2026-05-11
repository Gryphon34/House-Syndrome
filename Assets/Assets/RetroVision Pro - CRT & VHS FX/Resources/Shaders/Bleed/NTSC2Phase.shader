Shader "RetroVisionPro/CRT&VHSFX/Bleed2PHASE"
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

    // ===== Textures & uniforms =====
    TEXTURE2D(_Mask);     SAMPLER(sampler_Mask);

    float _FadeMultiplier;                  // if > 0, gate effect by mask
    #pragma shader_feature ALPHA_CHANNEL    // choose A or R from mask
    float MaskThreshold;
    float M_Bleed_Amount;

    // ----- RGB <-> YIQ helpers -----
    half3 RGBtoYIQ(half3 rgb)
    {
        return half3(
            0.2989 * rgb.x + 0.5959 * rgb.y + 0.2115 * rgb.z,
            0.5870 * rgb.x - 0.2744 * rgb.y - 0.5229 * rgb.z,
            0.1140 * rgb.x - 0.3216 * rgb.y + 0.3114 * rgb.z
        );
    }

    half3 YIQtoRGB(half3 yiq)
    {
        return half3(
            1.0000 * yiq.x + 1.0000 * yiq.y + 1.0000 * yiq.z,
            0.9560 * yiq.x - 0.2720 * yiq.y - 1.1060 * yiq.z,
            0.6210 * yiq.x - 0.6474 * yiq.y + 1.7046 * yiq.z
        );
    }

    inline half3 SampleYIQ(float2 uv)
    {
        return RGBtoYIQ(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb);
    }

    inline half3 SampleYIQAtOffset(float2 baseUV, float stepUV_X, float offsetPx)
    {
        // Match original alignment (base - 0.5*step) + offset*step
        float2 uv = baseUV + float2((offsetPx - 0.5) * stepUV_X, 0.0);
        return SampleYIQ(uv);
    }

    // ===== Fragment: horizontal Y/C bleed via fixed FIR kernels =====
    float4 CustomPostProcessNTSC2Phase(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float2 uv           = i.texcoord;
        float  stepUV_X     = 1.0 / _ScreenParams.x; // 1 px in UV (X)

            // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                M_Bleed_Amount *= maskVal;
            }

        stepUV_X *= M_Bleed_Amount;

        // Precomputed FIR kernels (kept values, renamed for clarity)
        float LumaKernel[33];
        LumaKernel[0]  = -0.000174844; LumaKernel[1]  = -0.000205844; LumaKernel[2]  = -0.000149453; LumaKernel[3]  = -0.000051693; LumaKernel[4]  =  0.000000000;
        LumaKernel[5]  = -0.000066171; LumaKernel[6]  = -0.000245058; LumaKernel[7]  = -0.000432928; LumaKernel[8]  = -0.000472644; LumaKernel[9]  = -0.000252236;
        LumaKernel[10] =  0.000198929; LumaKernel[11] =  0.000687058; LumaKernel[12] =  0.000944112; LumaKernel[13] =  0.000803467; LumaKernel[14] =  0.000363199;
        LumaKernel[15] =  0.000013422; LumaKernel[16] =  0.000253402; LumaKernel[17] =  0.001339461; LumaKernel[18] =  0.002932972; LumaKernel[19] =  0.003983485;
        LumaKernel[20] =  0.003026683; LumaKernel[21] = -0.001102056; LumaKernel[22] = -0.008373026; LumaKernel[23] = -0.016897700; LumaKernel[24] = -0.022914480;
        LumaKernel[25] = -0.021642347; LumaKernel[26] = -0.008863273; LumaKernel[27] =  0.017271957; LumaKernel[28] =  0.054921920; LumaKernel[29] =  0.098342579;
        LumaKernel[30] =  0.139044281; LumaKernel[31] =  0.168055832; LumaKernel[32] =  0.178571429;

        float ChromaKernel[33];
        ChromaKernel[0]  = 0.001384762; ChromaKernel[1]  = 0.001678312; ChromaKernel[2]  = 0.002021715; ChromaKernel[3]  = 0.002420562; ChromaKernel[4]  = 0.002880460;
        ChromaKernel[5]  = 0.003406879; ChromaKernel[6]  = 0.004004985; ChromaKernel[7]  = 0.004679445; ChromaKernel[8]  = 0.005434218; ChromaKernel[9]  = 0.006272332;
        ChromaKernel[10] = 0.007195654; ChromaKernel[11] = 0.008204665; ChromaKernel[12] = 0.009298238; ChromaKernel[13] = 0.010473450; ChromaKernel[14] = 0.011725413;
        ChromaKernel[15] = 0.013047155; ChromaKernel[16] = 0.014429548; ChromaKernel[17] = 0.015861306; ChromaKernel[18] = 0.017329037; ChromaKernel[19] = 0.018817382;
        ChromaKernel[20] = 0.020309220; ChromaKernel[21] = 0.021785952; ChromaKernel[22] = 0.023227857; ChromaKernel[23] = 0.024614500; ChromaKernel[24] = 0.025925203;
        ChromaKernel[25] = 0.027139546; ChromaKernel[26] = 0.028237893; ChromaKernel[27] = 0.029201910; ChromaKernel[28] = 0.030015081; ChromaKernel[29] = 0.030663170;
        ChromaKernel[30] = 0.031134640; ChromaKernel[31] = 0.031420995; ChromaKernel[32] = 0.031517031;

        // Accumulators
        half3 sumYIQ = half3(0,0,0);
        half3 sumW   = half3(0,0,0);

        // Symmetric pairs around center
        [unroll]
        for (int ii = 0; ii < 29; ++ii)
        {
            float offA = (float)ii - (float)29;
            float offB = (float)29 - (float)ii;

            half3 yiqA = SampleYIQAtOffset(uv, stepUV_X, offA);
            half3 yiqB = SampleYIQAtOffset(uv, stepUV_X, offB);

            half3 w = half3(LumaKernel[ii + 3], ChromaKernel[ii], ChromaKernel[ii]);
            sumYIQ += 0.5 * (yiqA + yiqB) * w;
            sumW   += w;
        }

        // Center tap
        half3 wCenter  = half3(LumaKernel[29], ChromaKernel[29], ChromaKernel[29]);
        half3 yiqCenter= SampleYIQAtOffset(uv, stepUV_X, 0.0);
        sumYIQ += yiqCenter * wCenter;
        sumW   += wCenter;

        // Normalize, convert back, preserve original alpha
        half3 yiq = sumYIQ / max(sumW, half3(1e-6,1e-6,1e-6));
        float3 rgb = YIQtoRGB(yiq);
        float  a   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).a;

        return float4(rgb, a);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment CustomPostProcessNTSC2Phase
            ENDHLSL
        }
    }
    Fallback Off
}
