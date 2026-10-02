using UnityEngine;

/// <summary>
/// HUD 공통 바탕 — 나무 판에 종이 라벨을 붙인 장난감 리모컨, 그리고 화면 크기를 따라가는 좌표계.
///
/// <b>화면 크기 문제.</b> OnGUI 는 픽셀로 그린다. 그래서 1280×720 에 맞춰 놓은 판이
/// 1920×1080 에서는 작아지고, 게임 뷰를 반만 열어두면 화면을 다 덮는다. 그게 "패널이 너무 크다"
/// 의 정체였다(2026-09-16). 여기서 <see cref="Begin"/> 이 GUI.matrix 를 한 번 늘려주고,
/// HUD 들은 전부 <b>1280×720 기준 좌표</b>로만 그린다. 그러면 어느 해상도에서도 화면에서
/// 차지하는 비율이 같다.
///
/// <b>글씨 문제.</b> 연한 잉크(#A2907C)를 종이(#FDF8EC) 위에 쓰면 대비가 2.9:1 이라 읽기 힘들다.
/// 웹 접근성 기준이 4.5:1 이야. 지금 값은 <see cref="Ink"/> 7.7:1 / <see cref="InkSoft"/> 5.0:1 —
/// 색은 그대로 따뜻한데 글자는 또렷하다. <b>이 두 색 말고 다른 갈색을 글자에 쓰지 마.</b>
///
/// 진짜 UI(TextMeshPro 캔버스)로 갈아엎을 때 HUD 들과 같이 버릴 스크립트야.
/// </summary>
public static class Hud
{
    /// <summary>모든 HUD 좌표의 기준 해상도. 기획서 §7.6 의 성능 목표와 같은 숫자다.</summary>
    public const float DesignHeight = 720f;

    // ---- 색 ----
    public static readonly Color Wood     = new Color32(0xC9, 0xAC, 0x8A, 0xFF);   // 판 테두리
    public static readonly Color WoodDark = new Color32(0xAE, 0x8E, 0x6B, 0xFF);   // 아래 그림자 결
    public static readonly Color Paper    = new Color32(0xFD, 0xF8, 0xEC, 0xFF);   // 종이 라벨
    public static readonly Color Ink      = new Color32(0x4A, 0x3A, 0x2C, 0xFF);   // 본문 (대비 7.7:1)
    public static readonly Color InkSoft  = new Color32(0x7B, 0x67, 0x52, 0xFF);   // 작은 라벨 (5.0:1)
    public static readonly Color Brass    = new Color32(0xD2, 0x88, 0x18, 0xFF);   // 태엽이 터질 때
    public static readonly Color Ribbon   = new Color32(0xC4, 0x45, 0x3E, 0xFF);

    public static Texture2D WoodTex { get; private set; }
    public static Texture2D WoodDarkTex { get; private set; }
    public static Texture2D PaperTex { get; private set; }
    public static Texture2D BrassTex { get; private set; }
    public static Texture2D RibbonTex { get; private set; }

    // ---- 글자 ----
    public static GUIStyle Label { get; private set; }    // 작은 설명 라벨
    public static GUIStyle Value { get; private set; }    // 숫자
    public static GUIStyle Big { get; private set; }      // 속도계
    public static GUIStyle Title { get; private set; }    // 가운데 제목
    public static GUIStyle Text { get; private set; }     // 본문
    public static GUIStyle Tiny { get; private set; }     // 개발용

    /// <summary>지금 화면 배율. GUIUtility.RotateAroundPivot 처럼 <b>화면 좌표</b>를 받는
    /// 함수에 가상 좌표를 넘길 때 이걸 곱해야 한다. 안 그러면 회전 중심이 어긋나서
    /// 1080p 에서 태엽이 화면 밖으로 날아갔다(2026-09-16).</summary>
    public static float ScaleFactor { get; private set; } = 1f;

    static Font builtWith;
    static bool built;

    /// <summary>GUI.matrix 를 화면 크기에 맞춰 늘리고, 1280×720 기준 화면 크기를 돌려준다.</summary>
    public static Rect Begin(Font preferred)
    {
        Ensure(preferred);

        float s = Mathf.Clamp(Screen.height / DesignHeight, 0.8f, 1.8f);
        ScaleFactor = s;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        return new Rect(0f, 0f, Screen.width / s, Screen.height / s);
    }

    public static void End() => GUI.matrix = Matrix4x4.identity;

    static void Ensure(Font preferred)
    {
        var font = HudFont.Resolve(preferred);
        if (built && font == builtWith) return;
        builtWith = font;
        built = true;

        WoodTex     = Solid(Wood);
        WoodDarkTex = Solid(WoodDark);
        PaperTex    = Solid(Paper);
        BrassTex    = Solid(Brass);
        RibbonTex   = Solid(Ribbon);

        Label = Make(font, 14, FontStyle.Normal, TextAnchor.UpperLeft, InkSoft);
        Value = Make(font, 26, FontStyle.Bold,   TextAnchor.UpperLeft, Ink);
        Big   = Make(font, 38, FontStyle.Bold,   TextAnchor.MiddleRight, Ink);
        Title = Make(font, 19, FontStyle.Bold,   TextAnchor.MiddleCenter, Ink);
        Text  = Make(font, 15, FontStyle.Normal, TextAnchor.UpperLeft, Ink);
        Tiny  = Make(font, 12, FontStyle.Normal, TextAnchor.UpperLeft, InkSoft);
    }

    static GUIStyle Make(Font font, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        // GUI.skin.label 에서 복사하면 안 된다 — GUI.skin 은 OnGUI 안에서만 읽을 수 있어서
        // 검사 스크립트나 Awake 에서 부르면 그대로 터진다. 빈 스타일에서 시작하면 그 문제가 없다.
        var s = HudFont.With(new GUIStyle
        { fontSize = size, fontStyle = style, alignment = anchor, wordWrap = false }, font);
        s.normal.textColor = color;
        s.padding = new RectOffset(0, 0, 0, 0);
        return s;
    }

    /// <summary>같은 스타일에 크기·정렬만 바꿔 쓰고 싶을 때. OnGUI 안에서 불러도 싸다.</summary>
    public static GUIStyle Resize(GUIStyle basis, int size, TextAnchor? anchor = null)
    {
        var s = new GUIStyle(basis) { fontSize = size };
        if (anchor.HasValue) s.alignment = anchor.Value;
        return s;
    }

    // ------------------------------------------------------------------
    //  나무 판 + 종이 라벨
    // ------------------------------------------------------------------
    /// <summary>
    /// 나무 테두리를 바깥에 남기는 게 핵심이다 — 종이가 판 위에 붙어 있는 것처럼 보인다.
    /// 종이 안쪽 영역은 <see cref="Inner"/> 로 받아서 그 안에만 글씨를 넣어.
    /// </summary>
    public static void Panel(Rect r)
    {
        GUI.DrawTexture(r, WoodTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 4f, r.width, 4f), WoodDarkTex);
        GUI.DrawTexture(Inner(r), PaperTex);
    }

    /// <summary>한 가지 색으로 칠한다. 알파가 있는 색도 그대로 먹는다.</summary>
    public static void Fill(Rect r, Color c)
    {
        var keep = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * keep.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = keep;
    }

    /// <summary>판 안에서 종이가 덮는 영역. 글씨는 반드시 이 안에 있어야 한다.</summary>
    public static Rect Inner(Rect r) => new Rect(r.x + 7f, r.y + 7f, r.width - 14f, r.height - 16f);

    /// <summary>테두리 없는 작은 종이 쪽지. 구석 힌트용.</summary>
    public static void Chip(Rect r)
    {
        GUI.DrawTexture(r, PaperTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), WoodDarkTex);
    }

    /// <summary>종이에 그은 옅은 칸막이 줄.</summary>
    public static void Rule(float x, float y, float width)
        => GUI.DrawTexture(new Rect(x, y, width, 1f), WoodTex);

    public static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }
}
