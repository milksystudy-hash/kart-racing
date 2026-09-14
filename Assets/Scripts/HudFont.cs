using UnityEngine;

/// <summary>
/// 임시 HUD 용 폰트 해결기.
///
/// 찾는 순서는 이렇다:
///   1. 인스펙터에 꽂아 둔 폰트
///   2. Assets/Resources/HudFont.ttf  (프로젝트에 들어 있는 Paperlogy)
///   3. OS 에 깔린 한글 폰트
///   4. 유니티 기본 폰트 (한글은 네모로 나오지만 에러는 안 난다)
///
/// 2번을 먼저 보는 이유: OS 폰트를 이름으로 찾는 방식은 못 찾았을 때
/// **null 이 아니라 "속이 빈 폰트"** 를 돌려준다. 그걸 그대로 쓰면 글자를 그릴 때마다
/// "Can't Generate Mesh, No Font Asset has been assigned" 가 터지는데,
/// OnGUI 는 매 프레임 도니까 콘솔이 수천 줄로 막혀버린다. 실제로 한 번 그렇게 됐다.
///
/// 진짜 UI(TextMeshPro 캔버스)로 갈아엎을 때 HUD 들과 같이 버릴 스크립트야.
/// </summary>
public static class HudFont
{
    /// <summary>Resources 폴더에 둔 폰트 파일 이름 (확장자 없이).</summary>
    const string ResourceName = "HudFont";

    static readonly string[] OsCandidates =
    {
        "Malgun Gothic", "맑은 고딕", "NanumGothic", "Nanum Gothic",
        "Gulim", "굴림", "Batang", "Arial Unicode MS",
    };

    static Font cached;
    static bool resolved;

    /// <summary>꽂아 둔 폰트가 있으면 그것, 없으면 프로젝트 폰트, 그것도 없으면 OS 폰트.</summary>
    public static Font Resolve(Font preferred)
    {
        if (preferred != null) return preferred;
        if (resolved) return cached;

        resolved = true;

        // 1) 프로젝트에 들어 있는 폰트 — 제일 확실하다
        cached = Resources.Load<Font>(ResourceName);
        if (IsUsable(cached)) return cached;

        // 2) OS 폰트
        cached = Font.CreateDynamicFontFromOSFont(OsCandidates, 16);
        if (IsUsable(cached)) return cached;

        // 3) 포기. null 을 돌려주면 With() 가 유니티 기본 폰트를 그대로 둔다.
        Debug.LogWarning("[HudFont] 한글 폰트를 못 찾았어. Assets/Resources/HudFont.ttf 가 있는지 확인해줘. " +
                         "당장은 유니티 기본 폰트로 그리고, 한글은 네모로 보일 수 있어.");
        cached = null;
        return null;
    }

    /// <summary>속이 빈 폰트를 걸러낸다. 이게 이 스크립트의 핵심이야.</summary>
    static bool IsUsable(Font font)
    {
        if (font == null) return false;
        // 동적 폰트는 글리프 요청이 되면 쓸 수 있는 것, 정적 폰트는 문자표가 있으면 된다.
        if (font.dynamic) return font.HasCharacter('가') || font.HasCharacter('A');
        return font.characterInfo != null && font.characterInfo.Length > 0;
    }

    /// <summary>스타일에 폰트를 입힌다. 폰트가 없으면 유니티 기본값을 그대로 둔다.</summary>
    public static GUIStyle With(GUIStyle style, Font font)
    {
        if (font != null) style.font = font;
        return style;
    }
}
