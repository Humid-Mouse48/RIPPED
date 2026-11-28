Shader "RIPPED/EpicLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor ("Shade Color", Color) = (0.3, 0.25, 0.45, 1)
        _RimColor ("Rim Color", Color) = (1, 0.75, 0.45, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.35
        _Bands ("Light Bands", Range(1, 6)) = 3
        _BandBlend ("Band Blend", Range(0, 1)) = 0.6
        _StrataColor ("Strata Color", Color) = (1, 0.8, 0.6, 1)
        _StrataFreq ("Strata Frequency", Float) = 1.2
        _StrataStrength ("Strata Strength", Range(0, 1)) = 0
        _GrainScale ("Grain Scale", Float) = 3
        _GrainStrength ("Grain Strength", Range(0, 0.5)) = 0.08
        _GradientHeight ("Gradient Height", Float) = 14
        _GroundDarken ("Ground Darken", Range(0, 1)) = 0.7
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadeColor;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half _Bands;
            half _BandBlend;
            half4 _StrataColor;
            float _StrataFreq;
            half _StrataStrength;
            float _GrainScale;
            half _GrainStrength;
            float _GradientHeight;
            half _GroundDarken;
            half4 _EmissionColor;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));

                float3 baseColor = _BaseColor.rgb;

                float3 cell = floor(input.positionWS * _GrainScale);
                float grain = frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453) - 0.5;
                baseColor *= 1.0 + grain * _GrainStrength * 2.0;

                float strata = step(0.5, sin(input.positionWS.y * _StrataFreq) * 0.5 + 0.5);
                baseColor = lerp(baseColor, baseColor * _StrataColor.rgb, strata * _StrataStrength);

                float heightT = saturate(input.positionWS.y / max(_GradientHeight, 0.01));
                float vertical = 1.0 - abs(normal.y);
                baseColor *= lerp(1.0, lerp(_GroundDarken, 1.0, heightT), vertical);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);

                float ndl = saturate(dot(normal, light.direction));
                float lit = ndl * light.shadowAttenuation * light.distanceAttenuation;
                float banded = floor(lit * _Bands + 0.5) / _Bands;
                lit = lerp(lit, banded, _BandBlend);

                float3 ambient = SampleSH(normal);
                float3 color = lerp(_ShadeColor.rgb * baseColor, baseColor * light.color, lit);
                color += baseColor * ambient * 0.35;

                float rim = pow(1.0 - saturate(dot(normal, viewDir)), _RimPower);
                color += _RimColor.rgb * rim * _RimStrength * (0.3 + lit * 0.7);

                color += _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
