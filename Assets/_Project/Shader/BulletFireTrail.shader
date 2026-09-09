Shader "Custom/URP/BulletFireTrail"
{
    Properties
    {
        _MainTex ("Detail Texture (optional, white = no effect)", 2D) = "white" {}
        _CoreColor ("Core Color (near bullet)", Color) = (1, 0.95, 0.6, 1)
        _MidColor ("Mid Flame Color", Color) = (1, 0.45, 0.08, 1)
        _TipColor ("Tail / Ember Color", Color) = (0.55, 0.05, 0.03, 1)
        _Intensity ("Fire Intensity", Range(0, 1)) = 0
        _Heat ("White-Hot Amount", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 6
        _ScrollSpeed ("Scroll Speed", Float) = 3
        _FlickerSpeed ("Flicker Speed", Float) = 8
        _EdgeSharpness ("Edge Sharpness", Range(1, 20)) = 6
        _Brightness ("Brightness", Float) = 1.5
        _FlipU ("Flip Trail Direction (0 or 1)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CoreColor;
                float4 _MidColor;
                float4 _TipColor;
                float _Intensity;
                float _Heat;
                float _NoiseScale;
                float _ScrollSpeed;
                float _FlickerSpeed;
                float _EdgeSharpness;
                float _Brightness;
                float _FlipU;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    value += amp * valueNoise(p);
                    p *= 2.02;
                    amp *= 0.5;
                }
                return value;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                uv.x = lerp(uv.x, 1.0 - uv.x, _FlipU);

                // 0 at the trail's centerline, 1 at its edges
                float edge = abs(uv.y - 0.5) * 2.0;

                // scroll noise backward along the trail so flame drifts and flickers toward the tail
                float2 noiseUV = float2(uv.x * _NoiseScale - _Time.y * _ScrollSpeed, uv.y * _NoiseScale);
                noiseUV += _Time.y * _FlickerSpeed * 0.1;
                float flicker = fbm(noiseUV);

                // flame gets thinner and weaker toward the tail end of the trail
                float taper = saturate(1.0 - uv.x);
                float mask = flicker * taper - edge * 0.85;
                mask = saturate(mask * _EdgeSharpness);

                // posterize into a few bands for a chunky, pixel-art flame edge instead of a smooth blob
                const float bands = 4.0;
                mask = floor(mask * bands) / bands;

                // hot near the bullet (uv.x = 0), cooling off toward the tail
                float heatAmount = saturate(1.0 - uv.x * 1.3 + flicker * 0.3);
                half4 flameColor = lerp(_TipColor, _MidColor, saturate(heatAmount * 1.6));
                flameColor = lerp(flameColor, _CoreColor, saturate((heatAmount - 0.6) * 2.5));
                flameColor = lerp(flameColor, half4(1, 1, 0.92, 1), saturate(_Heat * heatAmount * heatAmount));

                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                half4 col;
                col.rgb = flameColor.rgb * _Brightness * texSample.rgb;
                col.a = mask * _Intensity * IN.color.a * texSample.a;

                return col;
            }
            ENDHLSL
        }
    }
}
