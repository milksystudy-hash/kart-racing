// 로비 받침대 위 종이 스탠디용.
//
// ★★ 2026-10-16 제작자: *"애들이 뭐 카드가 됐어. 회색 상자는 왜 만들어."*
//
// URP/Unlit 머티리얼을 코드로 만들면서 <b>투명(_Surface 1)과 알파컷(_AlphaClip)을
// 동시에 켜 놨던 게 원인이다.</b> 둘 중 하나로 돌게 돼 있는 셰이더라 섞인 상태가 되고,
// 그러면 그림 둘레의 투명한 자리가 <b>텍스처의 RGB 그대로</b> 그려진다 —
// 시우 스탠디가 파란 상자로, 이감이 검은 상자로 보인 게 그것이다.
//
// 키워드를 더 맞춰 보는 대신 <b>전용 셰이더 한 장</b>으로 간다. clip() 이 무조건 돌고,
// URP 머티리얼 설정에 기대는 데가 한 군데도 없다 — 같은 문제가 다시 날 자리가 없다.
//
// <b>잘라내기(alpha cutout)지 반투명이 아니다.</b> 종이 스탠디는 잘라낸 판이고,
// 그래야 깊이도 제대로 써서 정렬 문제가 없다.
//
// ★ Fallback 을 달지 마라 — "GUI/Text Shader" 폴백이 2026-10-16 에
//   빌드를 통째로 막았다(HideFlags.DontSave 에셋을 빌드가 끌어온다).
Shader "Racing/Standee"
{
    Properties
    {
        _MainTex ("Art", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }

        Lighting Off
        Cull Off        // 한 장으로 앞뒤 다 보인다 — 쿼드를 두 장 세울 필요가 없다
        ZWrite On
        ZTest LEqual

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
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(c.a - _Cutoff);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }
    }
}
