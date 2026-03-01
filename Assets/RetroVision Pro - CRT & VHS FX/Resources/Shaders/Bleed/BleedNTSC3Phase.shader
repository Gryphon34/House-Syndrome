Shader "RetroVisionPro/CRT&VHSFX/Bleed3PHASE"
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

    TEXTURE2D(_Mask);    SAMPLER(sampler_Mask);

    float _FadeMultiplier;                  // if > 0, gate effect by mask
    #pragma shader_feature ALPHA_CHANNEL    // choose A or R from mask
    float MaskThreshold;
    float M_Bleed_Amount;

    // ===== Color space helpers (YIQ) =====
    half3 RGBtoYIQ(half3 c)
    {
        // Y, I, Q (approx NTSC axes)
        return half3(
            0.2989 * c.x + 0.5959 * c.y + 0.2115 * c.z,
            0.5870 * c.x - 0.2744 * c.y - 0.5229 * c.z,
            0.1140 * c.x - 0.3216 * c.y + 0.3114 * c.z
        );
    }

    half3 YIQtoRGB(half3 c)
    {
        // Inverse matrix matching the original shader’s choice
        return half3(
            1.0000 * c.x + 1.0000 * c.y + 1.0000 * c.z,
            0.9560 * c.x - 0.2720 * c.y - 1.1060 * c.z,
            0.6210 * c.x - 0.6474 * c.y + 1.7046 * c.z
        );
    }

    // Sample source and convert to YIQ
    half3 SampleYIQ(float2 uv)
    {
        float3 rgb = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        return RGBtoYIQ(rgb);
    }

    // ===== Fragment (Horizontal Y/C bleed via fixed FIR kernels) =====
    float4 Bleed3PhaseFrag(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float2 uv = i.texcoord;

        // 1 px in UV horizontally
        float pixelStepU = 1.0 / _ScreenParams.x;

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

        pixelStepU *= M_Bleed_Amount;

        // Align center like the original macro (uv - 0.5*pixelStep)
        float2 uvCenter = uv - float2(0.5 * pixelStepU, 0.0);

        // Precomputed FIR kernels (kept values, clearer names)
        float LumaKernel[25];
        LumaKernel[0] = -0.000012020; LumaKernel[1] = -0.000022146; LumaKernel[2] = -0.000013155; LumaKernel[3] = -0.000012020; LumaKernel[4] = -0.000049979;
        LumaKernel[5] = -0.000113940; LumaKernel[6] = -0.000122150; LumaKernel[7] = -0.000005612; LumaKernel[8] =  0.000170516; LumaKernel[9] =  0.000237199;
        LumaKernel[10]=  0.000169640; LumaKernel[11]=  0.000285688; LumaKernel[12]=  0.000984574; LumaKernel[13]=  0.002018683; LumaKernel[14]=  0.002002275;
        LumaKernel[15]= -0.000909882; LumaKernel[16]= -0.007049081; LumaKernel[17]= -0.013222860; LumaKernel[18]= -0.012606931; LumaKernel[19]=  0.002460860;
        LumaKernel[20]=  0.035868225; LumaKernel[21]=  0.084016453; LumaKernel[22]=  0.135563500; LumaKernel[23]=  0.175261268; LumaKernel[24]=  0.190176552;

        float ChromaKernel[25];
        ChromaKernel[0] = -0.000118847; ChromaKernel[1] = -0.000271306; ChromaKernel[2] = -0.000502642; ChromaKernel[3] = -0.000930833; ChromaKernel[4] = -0.001451013;
        ChromaKernel[5] = -0.002064744; ChromaKernel[6] = -0.002700432; ChromaKernel[7] = -0.003241276; ChromaKernel[8] = -0.003524948; ChromaKernel[9] = -0.003350284;
        ChromaKernel[10]= -0.002491729; ChromaKernel[11]= -0.000721149; ChromaKernel[12]=  0.002164659; ChromaKernel[13]=  0.006313635; ChromaKernel[14]=  0.011789103;
        ChromaKernel[15]=  0.018545660; ChromaKernel[16]=  0.026414396; ChromaKernel[17]=  0.035100710; ChromaKernel[18]=  0.044196567; ChromaKernel[19]=  0.053207202;
        ChromaKernel[20]=  0.061590275; ChromaKernel[21]=  0.068803602; ChromaKernel[22]=  0.074356193; ChromaKernel[23]=  0.077856564; ChromaKernel[24]=  0.079052396;

        // Accumulators (per Y/I/Q channel)
        half3 accumYIQ = half3(0,0,0);
        half3 accumW   = half3(0,0,0);

        // Pair-symmetric taps around center: k = 0..20 (center handled later)
        [unroll]
        for (int k = 0; k < 21; ++k)
        {
            float offA = (float)k - 21.0;
            float offB = 21.0 - (float)k;

            half3 yiqA = SampleYIQ(uvCenter + float2(offA * pixelStepU, 0.0));
            half3 yiqB = SampleYIQ(uvCenter + float2(offB * pixelStepU, 0.0));

            half3 tapWeights = half3(LumaKernel[k + 3], ChromaKernel[k], ChromaKernel[k]);

            // Sum both sides with the same weights (keeps original look/logic)
            accumYIQ += 0.5 * (yiqA + yiqB) * tapWeights;
            accumW   += tapWeights; // note: matching the original normalization approach
        }

        // Center tap
        half3 centerW = half3(LumaKernel[21], ChromaKernel[21], ChromaKernel[21]);
        float4 centerRGB = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter);

        accumYIQ += RGBtoYIQ(centerRGB.rgb) * centerW;
        accumW   += centerW;

        // Normalize and convert back to RGB
        half3 yiqFiltered = accumYIQ / max(accumW, half3(1e-6,1e-6,1e-6));
        float3 rgbOut     = YIQtoRGB(yiqFiltered);

        return float4(rgbOut,centerRGB.a);
    }
ENDHLSL
    SubShader
    {
        Pass
        {
            Name "Bleed3Phase"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment Bleed3PhaseFrag
            ENDHLSL
        }
    }
    Fallback Off
}
