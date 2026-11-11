// The dream sky: a purple-to-pink gradient, twinkling stars, drifting nebula bands and a crescent moon.
// The moon sits where the main directional light of the scene comes from, so shadows always agree with it.
Shader "FeverDream/Dream Sky"
{
    Properties
    {
        [Header(Gradient)]
        [HDR] _ZenithColor ("Zenith", Color) = (0.035, 0.03, 0.16, 1)
        [HDR] _MidColor ("Mid Sky", Color) = (0.22, 0.12, 0.52, 1)
        [HDR] _HorizonColor ("Horizon", Color) = (0.85, 0.32, 0.7, 1)
        [HDR] _GroundColor ("Below Horizon", Color) = (0.05, 0.03, 0.14, 1)
        _MidHeight ("Mid Sky Height", Range(0.05, 0.9)) = 0.32
        _HorizonBand ("Horizon Band Height", Range(0.01, 0.5)) = 0.1
        _HorizonGlow ("Horizon Glow", Range(0, 1.5)) = 0.35
        _GroundSoftness ("Below Horizon Softness", Range(0.01, 1.0)) = 0.28

        [Header(Stars)]
        _StarDensity ("Density", Range(10, 120)) = 45
        _StarAmount ("Amount", Range(0, 0.4)) = 0.14
        [HDR] _StarColor ("Colour", Color) = (1.5, 1.6, 2.2, 1)
        _StarTwinkle ("Twinkle Speed", Range(0, 8)) = 2.2

        [Header(Moon)]
        [HDR] _MoonColor ("Colour", Color) = (2.0, 2.1, 2.8, 1)
        _MoonSize ("Radius (degrees)", Range(0.5, 20)) = 5.5
        _MoonCrescent ("Crescent (0 = full)", Range(0, 1)) = 0.75
        [HDR] _MoonHaloColor ("Halo Colour", Color) = (0.4, 0.38, 1.0, 1)
        _MoonHaloPower ("Halo Tightness", Range(2, 400)) = 28

        [Header(Nebula)]
        [HDR] _NebulaColorA ("Colour A", Color) = (0.95, 0.22, 0.72, 1)
        [HDR] _NebulaColorB ("Colour B", Color) = (0.18, 0.7, 1.0, 1)
        _NebulaScale ("Scale", Range(0.2, 6)) = 1.6
        _NebulaAmount ("Amount", Range(0, 1.5)) = 0.45
        _NebulaSpeed ("Drift Speed", Range(0, 0.2)) = 0.012

        _Exposure ("Exposure", Range(0, 4)) = 1
    }

    SubShader
    {
        // Same tags as the built-in Skybox shaders. No "RenderPipeline" tag on purpose: the engine also draws this
        // material outside the camera loop (environment reflection, material preview).
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _MidColor;
                float4 _HorizonColor;
                float4 _GroundColor;
                float _MidHeight;
                float _HorizonBand;
                float _HorizonGlow;
                float _GroundSoftness;
                float _StarDensity;
                float _StarAmount;
                float4 _StarColor;
                float _StarTwinkle;
                float4 _MoonColor;
                float _MoonSize;
                float _MoonCrescent;
                float4 _MoonHaloColor;
                float _MoonHaloPower;
                float4 _NebulaColorA;
                float4 _NebulaColorB;
                float _NebulaScale;
                float _NebulaAmount;
                float _NebulaSpeed;
                float _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                // zyx, not yzx: with yzx the result is the same for (x, y, z) and (y, x, z) and the star field
                // repeats mirrored across a diagonal of the sky.
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1.0, 0.0)), f.x),
                            lerp(Hash21(i + float2(0.0, 1.0)), Hash21(i + float2(1.0, 1.0)), f.x), f.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;
                for (int octave = 0; octave < 3; octave++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return sum / 0.875;
            }

            // At most one star per cell of a grid laid through the sky sphere. Offset plus size stays inside the
            // cell, so a star is never cut at a cell border.
            float3 Stars(float3 dir)
            {
                float3 p = dir * _StarDensity;
                float3 cell = floor(p);
                float3 local = frac(p) - 0.5;
                float pick = Hash31(cell);
                float3 offset = float3(Hash31(cell + 3.1), Hash31(cell + 7.7), Hash31(cell + 13.3)) * 0.6 - 0.3;
                float size = lerp(0.05, 0.16, Hash31(cell + 21.9));
                float star = smoothstep(size, 0.0, length(local - offset)) * step(1.0 - _StarAmount, pick);
                float twinkle = 0.65 + 0.35 * sin(_Time.y * _StarTwinkle * (0.5 + pick) + pick * 40.0);
                // A few stars lean pink, most stay cool.
                float3 tint = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.6, 0.9), step(0.8, Hash31(cell + 31.7)));
                return _StarColor.rgb * tint * (star * twinkle);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // The sky mesh is centred on the camera, so its position is the view direction.
                // Left unnormalised here; normalising per vertex would skew the interpolation.
                output.viewDirWS = TransformObjectToWorldDir(input.positionOS.xyz, false);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 dir = normalize(input.viewDirWS);
                float height = dir.y;
                float up = saturate(height);

                // Gradient: a thin glowing band at the horizon, violet above it, deep indigo overhead.
                float3 color = lerp(_HorizonColor.rgb, _MidColor.rgb, smoothstep(0.0, _HorizonBand + _MidHeight, up));
                color = lerp(color, _ZenithColor.rgb, smoothstep(_MidHeight, 1.0, up));
                color += _HorizonColor.rgb * (_HorizonGlow * exp(-abs(height) / max(_HorizonBand, 0.001)));

                // Nebula bands, drifting slowly. The projection stretches them toward the horizon like a cloud deck.
                float skyMask = smoothstep(0.02, 0.3, height);
                float2 plane = dir.xz / (up + 0.4) * _NebulaScale + _Time.y * _NebulaSpeed;
                float bands = smoothstep(0.48, 0.82, Fbm(plane));
                float3 nebula = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, Fbm(plane * 0.6 + 31.0));
                color += nebula * (bands * _NebulaAmount * skyMask);

                color += Stars(dir) * (skyMask * (1.0 - bands * 0.7));

                // Moon, in the direction the main light comes from. Without a main light URP reports a black light
                // pointing along +Z; the moon is left out then instead of sitting on the horizon.
                float3 toLight = _MainLightPosition.xyz;
                float hasMainLight = step(0.0001, dot(toLight, toLight))
                    * step(0.000001, dot(_MainLightColor.rgb, _MainLightColor.rgb));
                toLight = hasMainLight > 0.5 ? normalize(toLight) : float3(0.0, 1.0, 0.0);
                float cosAngle = dot(dir, toLight);
                float moonCos = cos(radians(_MoonSize));
                float edge = (1.0 - moonCos) * 0.04 + 0.00002;
                float disc = smoothstep(moonCos - edge, moonCos + edge, cosAngle);

                // The crescent is the disc minus a second disc, shifted sideways and a little upward.
                float3 side = normalize(cross(toLight, float3(0.0, 1.0, 0.0)) + float3(0.0001, 0.0, 0.0));
                float3 rise = cross(side, toLight);
                float shift = tan(radians(_MoonSize)) * lerp(2.2, 0.35, _MoonCrescent);
                float3 biteDir = normalize(toLight + (side * 0.8 + rise * 0.6) * shift);
                float bite = smoothstep(moonCos - edge, moonCos + edge, dot(dir, biteDir));
                float moon = saturate(disc - bite);
                float halo = pow(saturate(cosAngle), _MoonHaloPower);
                color += (_MoonColor.rgb * moon + _MoonHaloColor.rgb * halo) * hasMainLight;

                // Below the horizon: the void under the level.
                color = lerp(color, _GroundColor.rgb, smoothstep(0.0, _GroundSoftness, -height));

                // A little noise hides banding in the smooth gradient.
                color += (Hash21(input.positionCS.xy + _Time.y) - 0.5) / 255.0;

                // No fog and no clamp: HDR values reach the colour target for bloom and tonemapping.
                return half4(half3(max(color, 0.0) * _Exposure), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
