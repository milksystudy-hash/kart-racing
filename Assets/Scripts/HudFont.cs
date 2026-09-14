using UnityEngine;

/// <summary>
/// 임시 HUD 용 폰트 해결기.
///
/// 인스펙터에 폰트를 꽂아 두면 그걸 쓰고, 비어 있으면 OS 에 깔린 한글 폰트를 잡아 쓴다.
/// 그래서 아무것도 꽂지 않아도 화면에 한글이 나온다 — 꽂으면 그 폰트로 바뀐다.
///
/// 진짜 UI(TextMeshPro 캔버스)로 갈아엎을 때 HUD 들과 같이 버릴 스크립트야.
/// </summary>
public static class HudFont
{
    static Font fallback;
    static bool tried;

    // 앞에서부터 찾아서 먼저 있는 걸 쓴다. 한국어 윈도우면 맑은 고딕은 항상 있다.
    static readonly string[] OsCandidates =
    {
        "Paperlogy",          // 프로젝트 폰트를 윈도우에도 설치해 뒀다면 이게 잡힌다
        "Malgun Gothic",
        "맑은 고딕",
        "NanumGothic",
        "Gulim",
        "Arial Unicode MS",
    };

    /// <summary>꽂아 둔 폰트가 있으면 그것, 없으면 OS 한글 폰트.</summary>
    public static Font Resolve(Font preferred)
    {
        if (preferred != null) return preferred;

        if (!tried)
        {
            tried = true;
            fallback = Font.CreateDynamicFontFromOSFont(OsCandidates, 16);
        }
        return fallback;
    }

    /// <summary>스타일에 폰트를 입힌다. 폰트가 없으면 유니티 기본값을 그대로 둔다.</summary>
    public static GUIStyle With(GUIStyle style, Font font)
    {
        if (font != null) style.font = font;
        return style;
    }
}
