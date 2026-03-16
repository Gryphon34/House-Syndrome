Shader "RetroVisionPro/CRT&VHSFX/SignalNoiseShader"
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
	float _FadeMultiplier;
	#pragma shader_feature ALPHA_CHANNEL
	float MaskThreshold;

	#pragma shader_feature VHS_YIQNOISE_ON
	float signalNoisePower = 1.0f;
	float time_ = 1.0f;
	float signalNoiseAmount = 1.0f;
	#define MOD3 float3(443.8975,397.2973, 491.1871)



	half3 Blend_Screen(half3 a, half3 b) { return 1.0 - (1.0 - a) * (1.0 - b); }


		float2 hash22(float2 p)
		{
			float3 p3 = frac(float3(p.xyx) * MOD3);
			p3 += dot(p3.zxy, p3.yzx + 19.19);
			return frac(float2((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y));
		}
		float2 NoiseBW(float2 uv, float time, float contrast)
		{
			time = frac(time);
			float2 grain = hash22(uv + 0.07 * time);
			contrast = 1.0 / (10.0 * contrast);
			return pow(grain, contrast);
		}


	half3 rgb2yiq(half3 c) 
	{
		return half3(
			(0.2989 * c.x + 0.5959 * c.y + 0.2115 * c.z),
			(0.5870 * c.x - 0.2744 * c.y - 0.5229 * c.z),
			(0.1140 * c.x - 0.3216 * c.y + 0.3114 * c.z)
			);
	};

	half3 yiq2rgb(half3 c) 
	{
		return half3(
			(1.0 * c.x + 1.0 * c.y + 1.0 * c.z),
			(0.956 * c.x - 0.2720 * c.y - 1.1060 * c.z),
			(0.6210 * c.x - 0.6474 * c.y + 1.7046 * c.z)
			);
	};


	float4 Frag(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float2 UV = i.texcoord;
		float cuttOff = 1;
                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                cuttOff *= maskVal;
            }
		half3 col = half3(0.0,0.0,0.0);
		half3 signal = half3(0.0,0.0,0.0);

		col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).rgb;
		signal = rgb2yiq(col);

		float2 noise = NoiseBW(UV,time_, 1.0 - signalNoisePower);
		signal.y += (noise.x * 2.0 - 1.0) * signalNoiseAmount* cuttOff * signal.x;
		signal.z += (noise.y * 2.0 - 1.0) * signalNoiseAmount * cuttOff * signal.x;

		col = yiq2rgb(signal);

		return half4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).a);
	}

	ENDHLSL

	SubShader
	{
		Pass
		{
			Name "#SignalNoiseShader#"
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}
	}
	Fallback Off
}