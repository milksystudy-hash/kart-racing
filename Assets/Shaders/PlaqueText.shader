// 현판 글씨용 셰이더.
//
// 유니티 기본 "GUI/Text Shader" 는 <b>ZTest Always</b> 라 깊이 검사를 아예 안 한다 —
// 원래 화면 위 UI 를 그리라고 만든 거라 그게 맞지만, 월드에 세운 TextMesh 에 쓰면
// <b>벽 뒤에 있어도 그대로 보인다</b>(2026-09-17 유저: "오브젝트를 통과해서 보인다").
//
// 바꾼 건 한 줄 — ZTest LEqual. 나머지는 기본 셰이더와 같다:
// 글자 모양은 폰트 아틀라스의 <b>알파 채널에만</b> 들어 있어서, 색은 정점 색에서 가져오고
// 텍스처에서는 알파만 곱한다. URP/Unlit 같은 걸로 대신하면 RGB 가 0 이라 글자가 까맣게 나온다.
Shader "Racing/PlaqueText"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

        Lighting Off
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 col = input.color;
                col.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "GUI/Text Shader"
}
