Shader "RetroVisionPro/CRT&VHSFX/FilmGrainShader"
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

		#define MOD3 float3(443.8975,397.2973, 491.1871)

		float hash12(float2 p) 
	{
		float3 p3 = frac(float3(p.xyx) * MOD3);
		p3 += dot(p3, p3.yzx + 19.19);
		return frac(p3.x * p3.z * p3.y);
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

	float filmGrainAmount = 16.0;

		float FilmGrain(float2 uv, float time) 
		{
			float nr = hash12(uv + 0.07 * frac(time));
			return nr * nr * nr;
		}


	float4 Frag(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float t = _Time.y;
		float2 UV = i.texcoord.xy;
                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, i.texcoord).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                filmGrainAmount *= maskVal;
            }

		half3 col = half3(0.0,0.0,0.0);
		half3 signal = half3(0.0,0.0,0.0);

		col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).rgb;
		signal = rgb2yiq(col);

		float bg = FilmGrain((UV - 0.5) * 0.5, t);
		signal.x += bg * filmGrainAmount ;

		col = yiq2rgb(signal);

		return half4(col, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, UV).a);
	}

	ENDHLSL

	SubShader
	{
		Pass
		{
			Name "#FilmGrainShader#"
			Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}
	}
	Fallback Off
}