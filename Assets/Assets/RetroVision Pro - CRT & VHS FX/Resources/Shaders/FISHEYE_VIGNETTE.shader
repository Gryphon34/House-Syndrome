Shader "RetroVisionPro/CRT&VHSFX/FISHEYE_VIGNETTE"
{
    Properties
    {
        _BlitTexture("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Params
        half  _FisheyeSize;

        half  _CutoffX;
        half  _CutoffY;
        half  _CutoffFadeX;
        half  _CutoffFadeY;

        half  _ChromaFringe;            // 0 disables extra samples

        // Map single strength to internal size and bend
        inline void MapStrength(out half size, out half bend)
        {
            bend = 1.2h * _FisheyeSize;              // curvature
            size = 1.0h + 0.51h * (_FisheyeSize);    // slight zoom compensation
        }

        // Fisheye mapping with aspect correction
        float2 FisheyeUV(float2 uv, half size, half bend)
        {
            float4 ts = 1/_ScreenParams;      // x=1/w, y=1/h, z=w, w=h
            float aspect = ts.z / ts.w;              // w/h

            float2 c = float2(0.5, 0.5);             // optical center
            float2 d = uv - c;
            d.x *= aspect;

            float r2 = dot(d, d);

            // Radial barrel: r' = r * (1 + k1 r^2 + k2 r^4)
            float k1 = 0.35 * bend;
            float k2 = 0.10 * bend * bend;
            float gain = rcp(max(size, 1e-4));

            float scale = (1.0 + k1 * r2 + k2 * r2 * r2) * gain;
            d *= scale;

            d.x *= rcp(max(aspect, 1e-4));
            return c + d;
        }

// VR-safe: use ddx/ddy(p), fallback to finite differences in uv-space
float BorderMask(float2 uv, float2 p, half size, half bend)
{
    float4 ts = 1/_ScreenParams; // x=1/w, y=1/h

    // primary derivatives
    float px_dd = length(ddx(p));
    float py_dd = length(ddy(p));

    // finite-difference fallback (per-eye, XR-safe)
    float2 dpdx = FisheyeUV(uv + float2(ts.x, 0), size, bend) - p;
    float2 dpdy = FisheyeUV(uv + float2(0, ts.y), size, bend) - p;
    float px_fd = length(dpdx);
    float py_fd = length(dpdy);

    // pick robust per-axis scale
    float px = max(px_dd, px_fd);
    float py = max(py_dd, py_fd);
    px = max(px, 1e-6);
    py = max(py, 1e-6);

    // distances to nearest edge in distorted space
    float dx = min(p.x, 1.0 - p.x);
    float dy = min(p.y, 1.0 - p.y);

    // widths in same units
    float hardX = px * _CutoffX;
    float softX = px * _CutoffFadeX;
    float hardY = py * _CutoffY;
    float softY = py * _CutoffFadeY;

    float fx = (dx <= hardX) ? 0.0 : saturate((dx - hardX) / max(softX, 1e-6));
    float fy = (dy <= hardY) ? 0.0 : saturate((dy - hardY) / max(softY, 1e-6));
    return min(fx, fy);
}


        // Main fragment
        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;

            half size, bend;
            MapStrength(size, bend);

            float2 p = FisheyeUV(uv, size, bend);
            float fade = BorderMask(uv, p, size, bend); // было: BorderMask(p)

            float3 col;
            if (_ChromaFringe <= 1e-4)
            {
                col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, p).rgb;
            }
            else
            {
                // Small per-channel bend delta for chromatic aberration
                half cb = _ChromaFringe * 0.1h;
                float2 pR = FisheyeUV(uv, size, bend * (1.0h + cb));
                float2 pG = p;
                float2 pB = FisheyeUV(uv, size, bend * (1.0h - cb));
                col.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pR).r;
                col.g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pG).g;
                col.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pB).b;
            }

            col *= fade;
            return float4(col, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "FISHEYE_VIGNETTE"
            HLSLPROGRAM
                #pragma vertex   Vert      // from Blit.hlsl
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
