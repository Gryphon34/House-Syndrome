Shader "RetroVisionPro/CRT&VHSFX/CRT_APERTURE"
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

    #define FIX(c) max(abs(c), 1e-5)
    #define saturate(c) clamp(c, 0.0, 1.0)

    // Textures / samplers
    TEXTURE2D(_Mask);
    SAMPLER(sampler_Mask);
    // Mask fade
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;
    float HaloStrength;        
    float GlowDiffusion;      
    float TriadColorCount;     
    float MaskStrength;        
    float InputGamma;          
    float OutputGamma;         
    float OutputBrightness;    
    float BlendAmount;         

    // ==== Helpers (renamed) ====
    float Mod(float x, float y) { return x - y * floor(x / y); }

    float  Fract(float  x) { return x - floor(x); }
    float2 Fract(float2 x) { return x - floor(x); }
    float4 Fract(float4 x) { return x - floor(x); }

    float3 SampleMainTexWithInputGamma(float2 uv)
    {
        // Apply input gamma curve per original
        float3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
        return pow(abs(c), float3(InputGamma, InputGamma, InputGamma)).xyz;
    }

    float3x3 GetColorMatrix3x3(float2 uv, float2 dx)
    {
        return float3x3(
            SampleMainTexWithInputGamma(uv - dx),
            SampleMainTexWithInputGamma(uv),
            SampleMainTexWithInputGamma(uv + dx)
        );
    }

    float3 Blur1D(float3x3 m, float dist, float radius)
    {
        float3 x = float3(dist - 1.0, dist, dist + 1.0) / radius;
        float3 w = exp2(x * x * -1.0);
        return (m[0] * w.x + m[1] * w.y + m[2] * w.z) / (w.x + w.y + w.z);
    }

    float3 FilterGaussian3x3(float2 uv, float2 texSize)
    {
        float2 dx = float2(1.0 / texSize.x, 0.0);
        float2 dy = float2(0.0, 1.0 / texSize.y);
        float2 pix = uv * texSize;
        float2 baseUV = (floor(pix) + 0.5) / texSize;
        float2 dist = (Fract(pix) - 0.5) * -1.0;

        float3x3 row0 = GetColorMatrix3x3(baseUV - dy, dx);
        float3x3 row1 = GetColorMatrix3x3(baseUV,      dx);
        float3x3 row2 = GetColorMatrix3x3(baseUV + dy, dx);

        float3x3 col = float3x3(
            Blur1D(row0, dist.x, 0.5),
            Blur1D(row1, dist.x, 0.5),
            Blur1D(row2, dist.x, 0.5)
        );
        return Blur1D(col, dist.y, 0.5);
    }

    float3 FilterLanczosX(float2 uv, float2 texSize, float sharp)
    {
        texSize.x *= sharp;

        float2 dx = float2(1.0 / texSize.x, 0.0);
        float2 pix   = uv * texSize - float2(0.5, 0.0);
        float2 base  = (floor(pix) + float2(0.5, 0.001)) / texSize;
        float2 dist  = Fract(pix);

        float4 coef = PI * float4(dist.x + 1.0, dist.x, dist.x - 1.0, dist.x - 2.0);
        coef = FIX(coef);
        coef = 2.0 * sin(coef) * sin(coef / 2.0) / (coef * coef);
        coef /= dot(coef, float4(1.0, 1.0, 1.0, 1.0));

        float4 c1 = float4(SampleMainTexWithInputGamma(base),      1.0);
        float4 c2 = float4(SampleMainTexWithInputGamma(base + dx), 1.0);

        // Preserve original quirky construction
        float4x4 m0 = mul(coef.x, float4x4(c1, c1, c2, c2));
        float4x4 m1 = mul(coef.y, float4x4(c1, c1, c2, c2));
        float4x4 m2 = mul(coef.z, float4x4(c1, c1, c2, c2));

        return float3(m0[0].x, m0[0].y, m0[0].z);
    }

    float3 Mix3(float3 a, float3 b, float3 t) { return a * (1 - t) + b * t; }

    float3 GetApertureMaskWeight(float x)
    {
        float i = Mod(floor(x), TriadColorCount);
        if (i == 0.0)
            return Mix3(float3(1.0, 0.0, 1.0), float3(1.0, 0.0, 0.0), float3(TriadColorCount - 2.0, TriadColorCount - 2.0, TriadColorCount - 2.0));
        else if (i == 1.0)
            return float3(0.0, 1.0, 0.0);
        else
            return float3(0.0, 0.0, 1.0);
    }

    // ==== Fragment ====
    float4 FragCRTAperture(Varyings i) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

        float2 uv = i.texcoord;
        float4 srcRGBA = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

        float3 colGlow  = FilterGaussian3x3(uv, _ScreenParams.xy);
        float3 colSoft  = FilterLanczosX(uv, _ScreenParams.xy, 1.0);
        float3 colSharp = FilterLanczosX(uv, _ScreenParams.xy, 3.0);

        float3 col = sqrt(colSharp * colSoft);

        colGlow = saturate(colGlow - col);
        col += colGlow * colGlow * HaloStrength;

        // Aperture mask coloring
        col = Mix3(col, col * GetApertureMaskWeight(uv.x) * TriadColorCount, float3(MaskStrength, MaskStrength, MaskStrength));

        col += colGlow * GlowDiffusion;

        // Output gamma and brightness
        col = pow(abs(col * OutputBrightness), float3(1.0 / OutputGamma, 1.0 / OutputGamma, 1.0 / OutputGamma));

        float fadeMul = 1.0;
                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                fadeMul *= maskVal;
            }

        float blend = fadeMul * BlendAmount;
        return lerp(srcRGBA, float4(col, srcRGBA.a), blend);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#CRT_APERTURE#"
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
                #pragma vertex   Vert
                #pragma fragment FragCRTAperture
            ENDHLSL
        }
    }
    Fallback Off
}
