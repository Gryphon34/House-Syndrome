Shader "RetroVisionPro/CRT&VHSFX/RETROSCALE"
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
    // Mask fade
    float _FadeMultiplier;
    #pragma shader_feature ALPHA_CHANNEL
    float MaskThreshold;

    float _Scale;                   // x,y
    float  _Keep43,  _Chroma;

    float2 Gate43(float2 uv){
        float sA = _ScreenParams.x / _ScreenParams.y;
        const float tA = 4.0/3.0;
        if (sA > tA){ float xs = tA/sA; uv.x = (uv.x-0.5)*xs + 0.5; }
        else        { float ys = sA/tA; uv.y = (uv.y-0.5)*ys + 0.5; }
        return uv;
    }

    float3 YCoCg(float3 c){ return float3(c.r*0.25 + c.g*0.5 + c.b*0.25, c.r*0.5 - c.b*0.5, -c.r*0.25 + c.g*0.5 - c.b*0.25); }

    float3 RGBfromYCoCg(float3 y){ return float3(y.x + y.y - y.z, y.x + y.z, y.x - y.y - y.z); }


float3 ApplyChroma1Param(float2 uv, float2 grid, float3 rgb)
{
    if (_Chroma <= 0) return rgb;
    float s = saturate(_Chroma);

    // 2..8, чётная ширина хрома-блока
    float Wf = lerp(2.0, 8.0, s);
    int   W  = max(2, 2 * (int)round(Wf * 0.5));
    int   Q  = (int)round(s * 24.0);

    // выравнивание к началу блока W в ретро-сетке
    float2 rp = floor(uv * grid);
    rp.x -= fmod(rp.x, (float)W);
    float2 uv0 = (rp + 0.5) / grid;

    // размер активного RT (per-eye, DR-safe)
    float2 dim = _ScaledScreenParams.xy;

    // усреднение Co/Cg по W центрам текселей текущего RT (_BlitTexture)
    float2 cc = 0;
    [unroll(8)]
    for (int j = 0; j < 8; j++) {
        if (j >= W) break;
        float2 u = uv0 + float2(j,0) / grid;
        float2 t = (floor(u * dim) + 0.5) / dim;  // центр текселя в _BlitTexture
        float3 sRGB = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, t, 0).rgb;
        float3 ycc  = YCoCg(sRGB);
        cc += ycc.yz;
    }
    cc /= (float)W;
    if (Q > 0) { float q = (float)Q; cc = round(cc*q)/q; }

    // заменить только хрому
    float3 yL = YCoCg(rgb);
    yL.yz = lerp(yL.yz, cc, s);
    return RGBfromYCoCg(yL);
}



float4 Frag(Varyings i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    float2 uv = i.texcoord;
    if (_Keep43 > 0.5) uv = Gate43(uv);

    // --- Mask → blend 0..1
    float maskVal = 1.0;
    if (_FadeMultiplier > 0.0)
    {
    #if ALPHA_CHANNEL
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
    #else
        float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
    #endif
        maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
        maskVal *= saturate(_FadeMultiplier);
    }

    float targetScale = max(_Scale, 0.01);
    float s = lerp(1.0, targetScale, maskVal);

    float2 grid = max(floor(_ScreenParams.xy * s), 1.0.xx);
    float2 uvPx = round(uv * grid) / grid;

    float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvPx).rgb;
    col = ApplyChroma1Param(uv, grid, col);

    return float4(col, 1);
}


    ENDHLSL

    SubShader
    {
			Pass
		{
			Name "#RETROSCALE#"

			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}

    }
    Fallback Off
}