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

        // ---- 로비/전시실에서만 쓰는 색 ----
        { 0x8A6A48u, Finish.나무 },   // 바닥 굽도리
        { 0xA8784Cu, Finish.나무 },   // 밝은 목재
        { 0xB0ACA0u, Finish.석재 },   // 받침대 · 석재
        { 0x1B222Eu, Finish.광택 },   // 광고 화면

        // ---- 물 ----
        { 0x6FA0A8u, Finish.광택 },   // 연못

        // ---- 지붕 ----
        { 0xC9B99Au, Finish.나무 },   // 반자널

        // ---- 빛나는 것 ----
        { 0xF0C070u, Finish.발광 },   // 창문
        { 0xF5C069u, Finish.발광 },   // 석등
        { 0xFFD13Cu, Finish.발광 },   // 가속 발판 화살표
        { 0xFFF6DCu, Finish.발광 },   // 천창
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

    static readonly Dictionary<(uint, int), Material> clear = new();

    /// <summary>
    /// <b>비치는 재질.</b> 물처럼 뒤가 보여야 하는 것에만 쓴다.
    ///
    /// ★ <see cref="Get(Color, Finish)"/> 는 <c>Finish.유리</c> 를 줘도 <b>불투명</b>이다 —
    /// 런타임 쪽은 매끈함·금속감만 만지고 투명 설정을 안 한다(에디터 <c>MaterialAsset</c> 만 한다).
    /// 그래서 물을 유리로 달라고 하면 <b>하늘색 판때기</b>가 나온다. 화장실 거울을 불투명
    /// 크림색 판으로 만들었다가 «무슨 원리인지 모르겠다» 를 들은 그 자리야(2026-09-22).
    ///
    /// URP 는 투명을 <b>여섯 군데를 같이</b> 맞춰야 켜진다 — 하나만 빠져도 조용히 불투명이다.
    /// </summary>
    public static Material Water(Color rgb, float alpha)
    {
        int a = Mathf.RoundToInt(Mathf.Clamp01(alpha) * 100f);
        if (clear.TryGetValue((Key(rgb), a), out var hit) && hit != null) return hit;

        var c = new Color(rgb.r, rgb.g, rgb.b, a / 100f);
        var mat = new Material(Surface.Lit()) { name = $"Clear_{ColorUtility.ToHtmlStringRGB(rgb)}_{a}" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);     // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);         // Alpha
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        clear[(Key(rgb), a)] = mat;
        return mat;
    }

    /// <summary>색을 0xRRGGBB 정수로. 구조체를 그대로 키로 쓰면 해시가 미덥지 않다.</summary>
    static uint Key(Color c)
    {
        var b = (Color32)c;
        return ((uint)b.r << 16) | ((uint)b.g << 8) | b.b;
    }

    /// <summary>이 색이 무슨 재질인지. 표에 없으면 무광.</summary>
    public static Finish FinishFor(Color color)
        => ByColor.TryGetValue(Key(color), out var f) ? f : Finish.무광;
}
