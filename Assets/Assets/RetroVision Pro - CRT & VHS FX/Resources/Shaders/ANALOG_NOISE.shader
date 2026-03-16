// ============================================================================
// 
// Blends an animated noise/pattern texture over the source to mimic
// analog TV snow / moving bars. Supports optional masking.
// Controls (uniforms seen in this file):
//  - _Fade         : overall effect strength (0..1) multiplied by mask if enabled
//  - TimeX         : time input (drives animation)
//  - barHeight     : frequency of sine bars (how many bars fit in screen)
//  - barSpeed      : how fast bars scroll
//  - edgeCutOff    : phase/threshold to start bars
//  - cut           : extra threshold for final blend
//  - _OffsetNoiseX/Y: UV offsets for the noise/pattern
//  - tileX/tileY   : tiling of the noise/pattern
//  - angle         : rotation of the pattern (radians)
//  - horizontal    : if >0, bars run horizontally; else vertically
// ============================================================================

Shader "RetroVisionPro/CRT&VHSFX/ANALOG_NOISE"
{
    Properties
    {
        _BlitTexture("CTexture", 2D) = "white" {} // Source color buffer
    }

    HLSLINCLUDE

    // Core URP math-helper library
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    // Blit.hlsl provides Vert, Attributes, Varyings, and _BlitTexture + sampler_LinearClamp
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Fullscreen triangle vertex payload (URP post-process style)
        struct Attributes1
        {
            uint  vertexID : SV_VertexID;  // index for full-screen triangle
            float3 vertex  : POSITION;     // not used (kept for compatibility)
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings1
        {
            float4 positionCS      : SV_POSITION; // clip-space position
            float2 texcoord        : TEXCOORD0;   // main UV for sampling source
            float2 texcoordStereo  : TEXCOORD1;   // extra UV used for pattern
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // --- Textures/Samplers -------------------------------------------------
        TEXTURE2D(_Mask);           // Optional mask (R or A based on ALPHA_CHANNEL)
        SAMPLER(sampler_Mask);
        TEXTURE2D(_Pattern);        // Noise/pattern texture
        SAMPLER(sampler_Pattern);

        #pragma shader_feature ALPHA_CHANNEL

        // --- User controls / uniforms ------------------------------------------
        float _FadeMultiplier;  // if >0, multiply fade by mask coverage
        float MaskThreshold;
        float _Intensity;       // (unused here but kept if driven externally)
        float TimeX;            // time parameter for animation
        half  _Fade;            // base blend amount (0..1)

        // Bar animation shaping
        half barHeight   = 6.;   // how many sine bars across field
        half barOffset   = 0.6;  // (unused here)
        half barSpeed    = 2.6;  // how fast bars move
        half barOverflow = 1.2;  // (unused here)

        // Edge thresholds / gating
        half edgeCutOff;         // baseline for bar threshold
        half cut;                // extra cut for final smoothstep blend

        // Pattern UV controls
        half  _OffsetNoiseX;
        half  _OffsetNoiseY;
        half4 _BlitTexture_ST;       // standard tiling/offset (unused here)
        half  tileX = 0;         // pattern tiling (x)
        half  tileY = 0;         // pattern tiling (y)
        half  angle;             // rotation in radians

        // 1 = horizontal bars (vary along Y), 0 = vertical bars (vary along X)
        uint  horizontal;

        // --- Vertex: builds fullscreen triangle + pattern UVs -------------------
        Varyings1 Vert1(Attributes1 input)
        {
            Varyings1 output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Fullscreen triangle position/UV
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord   = GetFullScreenTriangleTexCoord(input.vertexID);

            // Build a 2x2 rotation matrix from 'angle'
            float cosAngle = cos(angle);
            float sinAngle = sin(angle);
            float2x2 rot   = float2x2(cosAngle, -sinAngle,
                                      sinAngle,  cosAngle);

            // NOTE: Rotating positionCS.xy (clip space) is unusual.
            // Often you'd rotate a 0..1 UV around a pivot instead.
            // Here we keep the original logic.
            float2 uvCS  = output.positionCS.xy;
            float2 uvRot = mul(rot, uvCS);

            // Compose pattern UV:
            //   uvRot + baseUV + offset, then apply tiling.
            // WARNING: The comma operator below means only the LAST expression
            // (_ScreenSize.zw * float2(tileY, tileX)) is used in the addition.
            // If the intention was to add both, replace the comma with '+'
            // and parenthesize accordingly.
            output.texcoordStereo =
                uvRot
                + output.texcoord
                + float2(_OffsetNoiseX - 0.2f, _OffsetNoiseY), _ScreenSize.zw * float2(tileY, tileX);

            // Finally multiply by tiling. If tileX/tileY are 0, result is 0.
            // Likely you want them >= 1 to see the pattern.
            output.texcoordStereo *= float2(tileY, tileX);

            return output;
        }

        // --- Fragment: blends animated pattern over the source ------------------
        float4 CustomPostProcess(Varyings1 input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            // Base UV for sampling source
            half2 uv      = input.texcoord;
            float4 src    = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            float4 pat    = SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, input.texcoordStereo.xy);
            float4 col    = src;

            // Choose axis: if horizontal>0 bars vary along Y, else along X
            float axis    = (horizontal > 0) ? input.texcoord.y : input.texcoord.x;

            // Build moving sine-bar field
            // 'edgeCutOff' shifts the baseline, barHeight controls frequency,
            // TimeX * barSpeed scrolls it over time.
            float bar     = floor(edgeCutOff + sin(axis * barHeight + TimeX * barSpeed) * 50);
            // Map to 0..1 and clamp
            float f       = clamp(bar * 0.03, 0, 1);

                    // Mask
            if (_FadeMultiplier > 0.0)
            {
            #if ALPHA_CHANNEL
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).a;
            #else
                float m = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv).r;
            #endif
                float maskVal = smoothstep(MaskThreshold - 0.05, MaskThreshold + 0.05, m);
                _Fade *= maskVal;
            }

            // First mix: where f is high, show more pattern
            col = lerp(pat, col, f);

            // Second mix: fade from original to the result using smooth threshold
            // smoothstep(col.r - cut, 0, 1) uses red channel as gate; lower 'cut' → more effect
            col = lerp(src, col, smoothstep(col.r - cut, 0, 1) * _Fade);

            // Preserve source alpha
            return float4(col.rgb, src.a);
        }

    ENDHLSL

    SubShader
    {
        Pass
        {
            // Fullscreen post-process: no culling/z
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex   Vert1
            ENDHLSL
        }
    }
    Fallback Off
}
