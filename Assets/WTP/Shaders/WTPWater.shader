// Shader air buatan sendiri untuk WTP AR (URP).
// _Clarity 0 = air keruh coklat (lumpur/abu), 1 = air jernih biru.
// Script WTPUnit mengubah _Clarity per unit lewat MaterialPropertyBlock,
// jadi satu material "Water" bisa tampil beda-beda di tiap unit.
Shader "WTP/Water"
{
    Properties
    {
        _DirtyColor ("Warna Keruh", Color) = (0.45, 0.33, 0.18, 0.95)
        _CleanColor ("Warna Jernih", Color) = (0.1, 0.5, 0.95, 0.8)
        _Clarity ("Kejernihan", Range(0, 1)) = 1
        _WaveScale ("Skala Riak", Float) = 6
        _WaveSpeed ("Kecepatan Riak", Float) = 1.5
        _WaveStrength ("Kekuatan Riak", Range(0, 2)) = 1
        _Glossiness ("Kilau", Range(8, 256)) = 64
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DirtyColor;
                half4 _CleanColor;
                half _Clarity;
                float _WaveScale;
                float _WaveSpeed;
                half _WaveStrength;
                half _Glossiness;
            CBUFFER_END

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
                float3 positionOS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                // Riak dihitung dari posisi lokal (meter model), supaya polanya
                // tetap sama walaupun WTP diperbesar/diperkecil di AR.
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half clarity = saturate(_Clarity);
                half4 baseColor = lerp(_DirtyColor, _CleanColor, clarity);

                // Riak: gabungan dua gelombang sinus yang bergerak dengan waktu.
                float2 p = i.positionOS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float wave = sin(p.x * 1.7 + t) * sin(p.y * 1.3 - t * 1.2)
                           + 0.5 * sin((p.x + p.y) * 2.3 + t * 0.7);

                // Normal "palsu" dari turunan gelombang, supaya pantulan cahaya ikut beriak.
                float3 bend = float3(cos(p.x * 1.7 + t), 0, cos(p.y * 1.3 - t * 1.2)) * 0.15 * _WaveStrength;
                float3 n = normalize(normalize(i.normalWS) + bend);

                Light light = GetMainLight();
                float3 viewDir = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 halfDir = normalize(light.direction + viewDir);

                half diffuse = saturate(dot(n, light.direction));
                // Air jernih lebih berkilau, air keruh kusam.
                half specular = pow(saturate(dot(n, halfDir)), _Glossiness) * lerp(0.05, 0.5, clarity);
                half fresnel = pow(1.0 - saturate(dot(n, viewDir)), 3.0);

                half3 color = baseColor.rgb * (SampleSH(n) * 0.6 + light.color * diffuse * 0.7)
                            + specular * light.color;
                // Air keruh diberi noda gelap yang ikut bergerak (kesan lumpur/abu).
                color -= (1.0 - clarity) * (0.5 + 0.5 * wave) * 0.12;

                half alpha = saturate(baseColor.a + fresnel * 0.2 + specular);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
