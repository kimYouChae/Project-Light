Shader "Project Light/Searchlight Shadow Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            StructuredBuffer<float4> _ShadowVertices;
            int _ShadowVertexCount;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings MaskVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                float3 positionWS = TransformObjectToWorld(
                    UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS.xy;
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            float Cross2D(float2 a, float2 b)
            {
                return a.x * b.y - a.y * b.x;
            }

            bool InsideTriangle(float2 samplePosition, float2 a, float2 b, float2 c)
            {
                if (Cross2D(b - a, c - a) == 0.0)
                {
                    return false;
                }

                float ab = Cross2D(b - a, samplePosition - a);
                float bc = Cross2D(c - b, samplePosition - b);
                float ca = Cross2D(a - c, samplePosition - c);
                return (ab >= 0.0 && bc >= 0.0 && ca >= 0.0)
                    || (ab <= 0.0 && bc <= 0.0 && ca <= 0.0);
            }

            half4 MaskFragment(Varyings input) : SV_Target
            {
                [loop]
                for (int index = 0; index < _ShadowVertexCount; index += 3)
                {
                    if (InsideTriangle(input.positionWS,
                        _ShadowVertices[index].xy,
                        _ShadowVertices[index + 1].xy,
                        _ShadowVertices[index + 2].xy))
                    {
                        discard;
                    }
                }

                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            }
            ENDHLSL
        }
    }
}
