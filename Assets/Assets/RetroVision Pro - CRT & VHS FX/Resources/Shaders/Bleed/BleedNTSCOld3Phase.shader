Shader "RetroVisionPro/CRT&VHSFX/BleedOld3Phase"
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

    TEXTURE2D(_Mask);          SAMPLER(sampler_Mask);      // Optional mask

    float _FadeMultiplier;   // If >0, multiply effect by mask
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;
    float M_Bleed_Amount;
    // ================= Color space helpers =================
    // RGB -> YIQ (approx NTSC)
    half3 RGBtoYIQ(half3 rgb)
    {
        return half3(
            0.2989 * rgb.x + 0.5959 * rgb.y + 0.2115 * rgb.z,
            0.5870 * rgb.x - 0.2744 * rgb.y - 0.5229 * rgb.z,
            0.1140 * rgb.x - 0.3216 * rgb.y + 0.3114 * rgb.z
        );
    }

    // YIQ -> RGB
    half3 YIQtoRGB(half3 yiq)
    {
        return half3(
            1.0000 * yiq.x + 1.0000 * yiq.y + 1.0000 * yiq.z,
            0.9560 * yiq.x - 0.2720 * yiq.y - 1.1060 * yiq.z,
            0.6210 * yiq.x - 0.6474 * yiq.y + 1.7046 * yiq.z
        );
    }

    // Sample source and convert to YIQ
    half3 SampleYIQ(float2 uv)
    {
        float3 rgb = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        return RGBtoYIQ(rgb);
    }

    // ===================== Fragment =======================
    // Horizontal Y/C bleed using fixed FIR kernels (luma/chroma)
    float4 Frag_BleedOld3Phase(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float2 uv            = i.texcoord;
        float  pixelStepUV_X = 1.0 / _ScreenParams.x; // 1 pixel in UV (X)

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

        pixelStepUV_X *= M_Bleed_Amount;

        // Tap alignment (original macro used uv - 0.5*step)
        float2 uvCenter = uv - float2(0.5 * pixelStepUV_X, 0.0);

        // Precomputed FIR kernels (kept values, renamed for clarity)
        float LumaKernel[25];
        LumaKernel[0] = -0.000071070; LumaKernel[1] = -0.000032816; LumaKernel[2] =  0.000128784; LumaKernel[3] =  0.000134711; LumaKernel[4] = -0.000226705;
        LumaKernel[5] = -0.000777988; LumaKernel[6] = -0.000997809; LumaKernel[7] = -0.000522802; LumaKernel[8] =  0.000344691; LumaKernel[9] =  0.000768930;
        LumaKernel[10]=  0.000275591; LumaKernel[11]= -0.000373434; LumaKernel[12]=  0.000522796; LumaKernel[13]=  0.003813817; LumaKernel[14]=  0.007502825;
        LumaKernel[15]=  0.006786001; LumaKernel[16]= -0.002636726; LumaKernel[17]= -0.019461182; LumaKernel[18]= -0.033792479; LumaKernel[19]= -0.029921972;
        LumaKernel[20]=  0.005032552; LumaKernel[21]=  0.071226466; LumaKernel[22]=  0.151755921; LumaKernel[23]=  0.218166470; LumaKernel[24]=  0.243902439;

        float ChromaKernel[25];
        ChromaKernel[0] = 0.001845562; ChromaKernel[1] = 0.002381606; ChromaKernel[2] = 0.003040177; ChromaKernel[3] = 0.003838976; ChromaKernel[4] = 0.004795341;
        ChromaKernel[5] = 0.005925312; ChromaKernel[6] = 0.007242534; ChromaKernel[7] = 0.008757043; ChromaKernel[8] = 0.010473987; ChromaKernel[9] = 0.012392365;
        ChromaKernel[10]= 0.014503872; ChromaKernel[11]= 0.016791957; ChromaKernel[12]= 0.019231195; ChromaKernel[13]= 0.021787070; ChromaKernel[14]= 0.024416251;
        ChromaKernel[15]= 0.027067414; ChromaKernel[16]= 0.029682613; ChromaKernel[17]= 0.032199202; ChromaKernel[18]= 0.034552198; ChromaKernel[19]= 0.036677005;
        ChromaKernel[20]= 0.038512317; ChromaKernel[21]= 0.040003044; ChromaKernel[22]= 0.041103048; ChromaKernel[23]= 0.041777517; ChromaKernel[24]= 0.042004791;

        // Accumulators
        half3 accumYIQ = half3(0,0,0);
        half3 accumW   = half3(0,0,0);

        // Symmetric pair accumulation (equivalent to original macro loop)
        [unroll]
        for (int k = 0; k < 21; ++k)
        {
            float offA = (float)k - 21.0;
            float offB = 21.0 - (float)k;

            half3 yiqA = SampleYIQ(uvCenter + float2(offA * pixelStepUV_X, 0.0));
            half3 yiqB = SampleYIQ(uvCenter + float2(offB * pixelStepUV_X, 0.0));

            half3 tapW = half3(LumaKernel[k + 3], ChromaKernel[k], ChromaKernel[k]);

            accumYIQ += 0.5 *(yiqA + yiqB) * tapW;
            accumW   += tapW;
        }

        // Center tap
        half3 centerW  = half3(LumaKernel[21], ChromaKernel[21], ChromaKernel[21]);
        float4 centerRGB = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvCenter);

        accumYIQ += RGBtoYIQ(centerRGB.rgb) * centerW;
        accumW   += centerW;

        // Normalize and convert back to RGB
        half3 yiqFiltered = accumYIQ / max(accumW, half3(1e-6,1e-6,1e-6));
        float3 rgbOut     = YIQtoRGB(yiqFiltered);

        return float4(rgbOut, centerRGB.a);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma fragment Frag_BleedOld3Phase
                #pragma vertex   Vert
            ENDHLSL
        }
    }
    Fallback Off
}
