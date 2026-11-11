// Walls, floors and other blockout surfaces of the dream world.
// Everything is computed from the world position, so it needs no UVs, keeps the same scale on stretched cubes
// and survives static batching (which throws the object space away).
// Lit with the URP simple (Blinn-Phong) model. The passes mirror Shaders/SimpleLit.shader of URP 17.3 minus the
// texture, alpha, lightmap and decal paths.
Shader "FeverDream/World Surface"
{
    Properties
    {
        [Header(Colour)]
        [MainColor] _BaseColor ("Base Colour", Color) = (0.42, 0.34, 0.72, 1)
        _SecondColor ("Mottle Colour", Color) = (0.30, 0.25, 0.62, 1)
        _MottleScale ("Mottle Scale (per metre)", Range(0.02, 2)) = 0.22
        _MottleAmount ("Mottle Amount", Range(0, 1)) = 0.6

        [Header(Pattern)]
        [Enum(None,0,Grid,1,Checker,2,Stripes,3)] _Pattern ("Pattern", Float) = 0
        _PatternColor ("Pattern Colour", Color) = (0.62, 0.52, 0.95, 1)
        _PatternSize ("Pattern Size (metres)", Range(0.1, 8)) = 1
        _PatternAngle ("Pattern Angle (degrees)", Range(0, 180)) = 0
        _PatternWidth ("Line Width or Stripe Fill", Range(0.01, 0.9)) = 0.06
        _PatternStrength ("Pattern Strength", Range(0, 1)) = 0.5
        [HDR] _GlowColor ("Pattern Glow", Color) = (0, 0, 0, 1)
        _GlowPulse ("Glow Pulse Speed", Range(0, 6)) = 0

        [Header(Light Response)]
        [HDR] _RimColor ("Rim Colour", Color) = (0.1, 0.12, 0.3, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _SpecularColor ("Specular Colour", Color) = (0, 0, 0, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }

    // Shared by every pass below.
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // SRP Batcher: every material property is declared here and only here, and every pass sees this exact
        // block. Each entry needs a matching line in Properties.
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _SecondColor;
            float _MottleScale;
            half _MottleAmount;
            float _Pattern;
            half4 _PatternColor;
            float _PatternSize;
            float _PatternAngle;
            float _PatternWidth;
            half _PatternStrength;
            half4 _GlowColor;
            float _GlowPulse;
            half4 _RimColor;
            half _RimPower;
            half4 _SpecularColor;
            half _Smoothness;
        CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ForwardLitVertex
            #pragma fragment ForwardLitFragment

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Defines
            // Blinn-Phong specular is compiled in only when this is defined before Lighting.hlsl
            // (see CalculateBlinnPhong). A black Specular Colour switches the highlight off per material.
            #define _SPECULAR_COLOR

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half4 fogFactorAndVertexLight : TEXCOORD2; // x: fogFactor, yzw: vertex light
                #else
                    half  fogFactor : TEXCOORD2;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD3;
                #endif

                half3 vertexSH : TEXCOORD4;

                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1.0, 0.0)), f.x),
                            lerp(Hash21(i + float2(0.0, 1.0)), Hash21(i + float2(1.0, 1.0)), f.x), f.y);
            }

            // Flat world-space coordinates in metres, projected along the world axis the surface faces most (box
            // mapping). Faces square to the axes are exact; a wall turned 45 degrees stretches the pattern by 1.41 at
            // most. The coordinate is always a plain world coordinate: an axis built from the normal would be
            // multiplied by the distance to the world origin and tear the pattern apart on curved meshes.
            float2 SurfaceUV(float3 positionWS, float3 normalWS)
            {
                float3 facing = abs(normalWS);
                float2 wallUV = facing.x > facing.z ? positionWS.zy : positionWS.xy;
                return facing.y > 0.7 ? positionWS.xz : wallUV;
            }

            // Box-filtered patterns (after Inigo Quilez). The pixel footprint w is averaged analytically, so
            // lines do not shimmer when the camera moves and fade to their mean colour in the distance.
            float FilteredChecker(float2 p, float2 w)
            {
                float2 i = 2.0 * (abs(frac((p - 0.5 * w) * 0.5) - 0.5) - abs(frac((p + 0.5 * w) * 0.5) - 0.5)) / w;
                return 0.5 - 0.5 * i.x * i.y;
            }

            float FilteredGrid(float2 p, float2 w, float lineWidth)
            {
                float n = 1.0 / lineWidth;
                float2 a = p + 0.5 * w;
                float2 b = p - 0.5 * w;
                float2 i = (floor(a) + min(frac(a) * n, 1.0) - floor(b) - min(frac(b) * n, 1.0)) / (n * w);
                return 1.0 - (1.0 - i.x) * (1.0 - i.y);
            }

            float FilteredStripes(float x, float w, float fill)
            {
                float a = x + 0.5 * w;
                float b = x - 0.5 * w;
                return (floor(a) * fill + min(frac(a), fill) - floor(b) * fill - min(frac(b), fill)) / w;
            }

            // 0 = bare surface, 1 = pattern.
            float PatternMask(float2 uv)
            {
                float angleSin, angleCos;
                sincos(radians(_PatternAngle), angleSin, angleCos);
                float2 p = uv / max(_PatternSize, 0.01);
                p = float2(angleCos * p.x + angleSin * p.y, angleCos * p.y - angleSin * p.x);
                // The derivative is taken before the branches; inside them the compiler would have to flatten.
                float2 w = fwidth(p) + 0.003;

                float mask = 0.0;
                if (_Pattern > 2.5)
                    mask = FilteredStripes(p.x, w.x, _PatternWidth);
                else if (_Pattern > 1.5)
                    mask = FilteredChecker(p, w);
                else if (_Pattern > 0.5)
                    mask = FilteredGrid(p, w, _PatternWidth);
                return saturate(mask);
            }

            void InitializeInputData(Varyings input, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;

                inputData.normalWS = input.normalWS;
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
                inputData.viewDirectionWS = SafeNormalize(viewDirWS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                #else
                    inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.fogFactor);
                    inputData.vertexLighting = half3(0, 0, 0);
                #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                // Ambient from the SH probe. These two lines are what SAMPLE_GI and
                // SAMPLE_SHADOWMASK expand to without lightmaps or probe volumes.
                inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
                inputData.shadowMask = unity_ProbesOcclusion;

                #if defined(DEBUG_DISPLAY)
                    inputData.vertexSH = input.vertexSH;
                #endif
            }

            void InitializeSurfaceData(InputData inputData, out SurfaceData surfaceData)
            {
                surfaceData = (SurfaceData)0;

                float2 uv = SurfaceUV(inputData.positionWS, inputData.normalWS);

                // Two octaves of soft noise break up the flat colour of large blockout faces.
                float mottle = ValueNoise(uv * _MottleScale) * 0.65 + ValueNoise(uv * _MottleScale * 2.7 + 13.7) * 0.35;
                half3 albedo = lerp(_BaseColor.rgb, _SecondColor.rgb, smoothstep(0.25, 0.75, mottle) * _MottleAmount);

                float pattern = PatternMask(uv);
                albedo = lerp(albedo, _PatternColor.rgb, pattern * _PatternStrength);

                float pulse = lerp(1.0, 0.7 + 0.3 * sin(_Time.y * _GlowPulse), step(0.001, _GlowPulse));
                half rim = pow(1.0 - saturate(dot(inputData.normalWS, inputData.viewDirectionWS)), _RimPower);

                surfaceData.albedo = albedo;
                surfaceData.alpha = 1.0;
                surfaceData.specular = _SpecularColor.rgb;
                surfaceData.smoothness = _Smoothness;
                surfaceData.metallic = 0.0;              // unused by Blinn-Phong
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = _GlowColor.rgb * (pattern * pulse) + _RimColor.rgb * rim;
            }

            Varyings ForwardLitVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

            #if defined(_FOG_FRAGMENT)
                half fogFactor = 0;
            #else
                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
            #endif

                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);

                // Same call OUTPUT_SH4 makes without lightmaps or probe volumes.
                output.vertexSH = SampleProbeSHVertex(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS));

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                    output.fogFactor = fogFactor;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }

            void ForwardLitFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, inputData);

                SurfaceData surfaceData;
                InitializeSurfaceData(inputData, surfaceData);

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, false);

                outColor = color;

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // Tells directional and punctual light shadows apart; they apply the normal bias differently.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            // Set by ShadowUtils.SetupShadowCasterConstantBuffer. Pipeline globals, not material
            // properties, so they stay outside UnityPerMaterial.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                positionCS = ApplyShadowClamping(positionCS);
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            struct Attributes
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                return input.positionCS.z;
            }
            ENDHLSL
        }

        // Feeds _CameraNormalsTexture, which the ambient occlusion pass of this project reads.
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #if defined(LOD_FADE_CROSSFADE)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normal     : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal);
                output.normalWS = half3(NormalizeNormalPerVertex(normalInput.normalWS));

                return output;
            }

            void DepthNormalsFragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                half3 normalWS = input.normalWS;
                normalWS = NormalizeNormalPerPixel(normalWS);
                outNormalWS = half4(normalWS, 0.0);

                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
