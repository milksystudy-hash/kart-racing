using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 표면 마감. 전시실이 그럴듯해 보이고 트랙이 안 그런 이유가 여기 있었다(2026-09-16).
///
/// 전시실은 마감을 7가지로 나눠 쓰는데, 트랙과 캠퍼스는 <b>전부 같은 무광</b>이었다.
/// 기와도 물도 잔디도 똑같이 번들거리면 눈이 재질을 구분 못 하고, 그러면 전부
/// 같은 플라스틱으로 보인다 — 그게 "유니티로 만든 티" 의 정체야.
///
/// <b>표는 여기 한 군데에만 있다.</b> 에디터(TestSceneBuilder)도 런타임(CampusBuilder)도
/// 이 값을 읽는다. 두 군데에 두면 반드시 어긋난다.
/// </summary>
public enum Finish { 무광, 나무, 석재, 광택, 금속, 유리, 발광 }

public static class Surface
{
    /// <summary>거칠기의 반대. 높을수록 번들거린다.</summary>
    public static float Smoothness(this Finish f) => f switch
    {
        Finish.나무 => 0.30f,   // 기름 먹인 목재 — 약하게 번들거린다
        Finish.석재 => 0.18f,   // 다듬은 돌·기와
        Finish.광택 => 0.40f,   // 닦은 바닥, 연못 물. 더 올리면 비스듬히 볼 때 어두운 하늘을 그대로 비춘다
        Finish.금속 => 0.55f,
        Finish.유리 => 0.95f,
        _ => 0.08f,
    };

    public static float Metallic(this Finish f) => f == Finish.금속 ? 0.85f : 0f;

    /// <summary>지금 렌더 파이프라인에 맞는 단색 셰이더.</summary>
    public static Shader Lit()
    {
        Shader s = null;
        if (GraphicsSettings.defaultRenderPipeline != null)
            s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Diffuse");
        return s;
    }
}

/// <summary>
/// 런타임에 만드는 단색 머티리얼. 같은 색·같은 마감은 한 번만 만들어서 돌려 쓴다.
///
/// 마감을 인자로 안 받아도 되게 <see cref="ByColor"/> 표를 둔다 — 팔레트 색이 곧 재질이라
/// 색만 보고 마감을 정할 수 있고, 그 덕에 캠퍼스의 Block() 호출 수백 개를 안 고쳐도 된다.
/// 새 재질을 쓰고 싶으면 이 표에 색 한 줄만 추가하면 돼.
/// </summary>
public static class FlatMaterial
{
    /// <summary>팔레트 색 → 마감. 여기 없는 색은 전부 무광.</summary>
    static readonly Dictionary<uint, Finish> ByColor = new()
    {
        // ---- 돌·기와 ----
        { 0xA8A49Au, Finish.석재 },   // 담장 돌
        { 0xA8A498u, Finish.석재 },   // 트랙 가드레일 돌담
        { 0x6E7A72u, Finish.석재 },   // 담장 기와
        { 0x4E7A70u, Finish.석재 },   // 청기와 지붕 · 기둥 머리
        { 0x8E9A8Cu, Finish.석재 },   // 이끼 낀 돌
        { 0x9A9A96u, Finish.석재 },   // 바위
        { 0xC6C0B2u, Finish.석재 },   // 광장 포석
        { 0x9A8E80u, Finish.석재 },   // 발바닥 문양

        // ---- 나무 ----
        { 0x7A583Eu, Finish.나무 },   // 나무 울타리 · 난간
        { 0x6B4A33u, Finish.나무 },   // 기둥
        { 0x6B4F3Au, Finish.나무 },   // 소나무 줄기

        // ---- 물 ----
        { 0x6FA0A8u, Finish.광택 },   // 연못

        // ---- 빛나는 것 ----
        { 0xF0C070u, Finish.발광 },   // 창문
        { 0xF5C069u, Finish.발광 },   // 석등
        { 0xFFD13Cu, Finish.발광 },   // 가속 발판 화살표
    };

    static readonly Dictionary<(Color, Finish), Material> cache = new();

    public static Material Get(Color color) => Get(color, FinishFor(color));

    public static Material Get(Color color, Finish finish)
    {
        if (cache.TryGetValue((color, finish), out var hit) && hit != null) return hit;

        var mat = new Material(Surface.Lit())
        {
            name = finish == Finish.무광
                ? $"Flat_{ColorUtility.ToHtmlStringRGB(color)}"
                : $"Flat_{ColorUtility.ToHtmlStringRGB(color)}_{finish}",
        };

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", finish.Smoothness());
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", finish.Smoothness());
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", finish.Metallic());

        if (finish == Finish.발광)
        {
            // 전시실(×2.2)보다 약하게. 거긴 어두운 실내라 확 타올라야 하지만,
            // 여긴 한낮 야외라 같은 값을 쓰면 창문이 하얗게 날아간다.
            // 눈부시면 이 숫자 하나만 내리면 돼 — 창문·석등·발판 화살표가 같이 따라온다.
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 1.25f);
        }

        cache[(color, finish)] = mat;
        return mat;
    }

    /// <summary>색을 0xRRGGBB 정수로. 구조체를 그대로 키로 쓰면 해시가 미덥지 않다.</summary>
    static uint Key(Color c)
    {
        var b = (Color32)c;
        return ((uint)b.r << 16) | ((uint)b.g << 8) | b.b;
    }

    static Finish FinishFor(Color color)
        => ByColor.TryGetValue(Key(color), out var f) ? f : Finish.무광;
}
