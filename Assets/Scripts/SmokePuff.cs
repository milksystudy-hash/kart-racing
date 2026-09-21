using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// <b>연기·김 알갱이 재질 한 군데.</b> 곰밥마당의 김과 카트의 배기 김이 같이 쓴다.
///
/// ★★ <b>알갱이가 «흰 네모» 로 보이던 진짜 이유</b> (2026-09-21, 유저: *"수증기가 너무 네모 픽셀"*):
///
/// <list type="number">
/// <item><b>텍스처가 없었다.</b> URP 파티클 셰이더에 맵을 안 주면 쿼드 전체가 그대로 칠해진다.</item>
/// <item>★ <b>투명이 안 켜져 있었다.</b> <c>SetFloat("_Surface", 1)</c> 은 <b>속성만</b> 바꾼다 —
///   URP 는 렌더링할 때 <b>셰이더 키워드</b>(<c>_SURFACE_TYPE_TRANSPARENT</c>)와
///   <c>_SrcBlend</c>/<c>_DstBlend</c>/<c>_ZWrite</c> 를 본다. 그걸 안 건드리면
///   <b>불투명으로 그려져서 알파가 통째로 무시된다.</b> 텍스처를 넣어도 네모가 남는 게 이것 때문이야.</item>
/// </list>
///
/// 두 번째가 핵심이다. 첫 번째만 고치고 «왜 아직도 네모지» 로 한 번 헤맸다.
///
/// > <b>URP 재질을 코드로 만들 때 `SetFloat` 만으로 투명이 되지 않는다.</b>
/// > 키워드와 블렌드 상태를 같이 켜라. 인스펙터에서 드롭다운을 바꾸면 에디터가 그걸 대신 해준다.
/// </summary>
public static class SmokePuff
{
    static Material shared;
    static Texture2D dot;

    /// <summary>알갱이 하나짜리 재질. 없으면 null — 부르는 쪽이 조용히 포기하면 된다.</summary>
    public static Material Material()
    {
        if (shared != null) return shared;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        shared = new UnityEngine.Material(shader)
        {
            name = "SmokePuff",
            hideFlags = HideFlags.HideAndDontSave,
        };

        var tex = SoftDot();
        // 셰이더마다 이름이 다르다. 있는 것만 꽂는다.
        if (shared.HasProperty("_BaseMap")) shared.SetTexture("_BaseMap", tex);
        if (shared.HasProperty("_MainTex")) shared.SetTexture("_MainTex", tex);
        shared.mainTexture = tex;

        if (urp) MakeTransparent(shared);
        shared.renderQueue = (int)RenderQueue.Transparent;

        return shared;
    }

    /// <summary>
    /// ★ URP 를 <b>진짜로</b> 투명하게 만든다. 속성 넷 + 키워드 셋을 같이 건드려야 한다 —
    /// 하나라도 빠지면 알파가 무시되고 쿼드가 통째로 칠해진다.
    /// </summary>
    static void MakeTransparent(Material m)
    {
        m.SetFloat("_Surface", 1f);                                   // 0 불투명 · 1 투명
        m.SetFloat("_Blend", 0f);                                     // 0 알파 블렌드
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);                                    // 연기끼리 서로 가리면 안 된다

        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHAMODULATE_ON");
    }

    /// <summary>
    /// 가운데가 진하고 가장자리로 갈수록 사라지는 동그라미를 <b>코드로 굽는다.</b>
    /// 새 이미지 파일이 0개라 저장소에 뭘 더 넣지 않아도 된다.
    ///
    /// 감쇠를 <b>세제곱</b>으로 준다 — 선형이면 가장자리에 옅은 테두리가 남아서
    /// 여러 장이 겹칠 때 <b>동그라미 자국</b>이 보인다. 연기는 경계가 없어야 연기다.
    /// </summary>
    static Texture2D SoftDot()
    {
        if (dot != null) return dot;

        const int n = 128;   // 64 면 가까이서 볼 때 가장자리 계단이 보인다
        dot = new Texture2D(n, n, TextureFormat.RGBA32, true)
        {
            name = "SoftPuff",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            anisoLevel = 2,
        };

        var pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                float a = Mathf.Clamp01(1f - r);
                a = a * a * a;                     // 가장자리를 길게 흘린다
                a = Mathf.Clamp01(a * 1.35f);      // 가운데는 다시 진하게

                pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

        dot.SetPixels32(pixels);
        dot.Apply(true);
        return dot;
    }
}
