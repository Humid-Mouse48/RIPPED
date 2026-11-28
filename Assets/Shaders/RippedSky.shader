Shader "RIPPED/Sky"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.25, 0.52, 0.91, 1)
        _HorizonColor ("Horizon Color", Color) = (0.81, 0.9, 1, 1)
        _GroundColor ("Ground Color", Color) = (0.66, 0.54, 0.36, 1)
        _SunColor ("Sun Color", Color) = (1, 0.86, 0.6, 1)
        _SunDirection ("Sun Direction", Vector) = (0.4, 0.6, 0.5, 0)
        _SunSize ("Sun Size", Range(0.9, 0.9999)) = 0.9985
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _SunColor;
                float4 _SunDirection;
                half _SunSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float h = dir.y;

                float3 color = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.55));
                color = lerp(color, _GroundColor.rgb, saturate(-h * 4.0));

                float sun = saturate(dot(dir, normalize(_SunDirection.xyz)));
                float disc = smoothstep(_SunSize, 1.0, sun);
                float glow = pow(sun, 24.0) * 0.35;
                color += _SunColor.rgb * (disc + glow);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
