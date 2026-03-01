Shader "RetroVisionPro/CRT&VHSFX/Warp"
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

	float2 warp = float2(1.0 / 32.0, 1.0 / 24.0);
	float scale;
	float fade;

	float2 Warp(float2 pos)
	{
		float2 h = pos - float2(0.5, 0.5);
		float r2 = dot(h, h);
		float f = 1.0 + r2 * (warp.x + warp.y * sqrt(r2));
		return f * scale * h + 0.5;
	}
	float2 Warp1(float2 pos)
	{
		pos = pos * 2.0 - 1.0;
		pos *= float2(1.0 + (pos.y * pos.y) * warp.x, 1.0 + (pos.x * pos.x) * warp.y);
		return pos * scale + 0.5;
	}

		float4 Frag0(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,i.texcoord  );
		float2 fragCoord = i.texcoord.xy * _ScreenParams.xy;
		float2 pos = Warp1(fragCoord.xy / _ScreenParams.xy);

		float4 col2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pos  );


		return lerp(col,col2,fade);
	}

		float4 Frag(Varyings i) : SV_Target
	{
		        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

		float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,i.texcoord  );
		float2 fragCoord = i.texcoord.xy * _ScreenParams.xy;
		float2 pos = Warp(fragCoord.xy / _ScreenParams.xy);

		 float4 col2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pos  );

		return lerp(col,col2,fade);
	}

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "#NAME#"

		Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment Frag0
                #pragma vertex Vert
            ENDHLSL
        }
			Pass
		{
			Name "#NAME#"

		Cull Off ZWrite Off ZTest Always

			HLSLPROGRAM
				#pragma fragment Frag
				#pragma vertex Vert
			ENDHLSL
		}
    }
    Fallback Off
}