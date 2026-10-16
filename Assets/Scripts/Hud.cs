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
    /// <summary>
    /// ★★ 2026-10-16 — <b>720 → 640.</b> 강사님: *"글씨가 여전히 작다."*
    ///   화면 높이를 이 값으로 나눠 배율을 잡으니 <b>이 숫자를 줄이면 전부 커진다.</b>
    ///   1080p 에서 1.50 → <b>1.69</b>(+12.5%). 칸도 글자도 같은 비율로 커져서
    ///   <b>넘침이 새로 생기지 않는다</b> — 글자만 키우면 칸을 뚫는다.
    /// </summary>
    public const float DesignHeight = 640f;

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

        float s = Mathf.Clamp(Screen.height / DesignHeight, 0.8f, 2.0f);
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

        // ★★ 2026-10-12 — <b>그림이 있으면 그걸 쓴다.</b> 없으면 예전처럼 단색 한 점으로
        //   떨어진다 — 파일을 빼도 HUD 가 안 깨진다.
        //   이 두 줄이 <see cref="Panel"/> 을 지나는 <b>모든 패널</b>을 한꺼번에 바꾼다 —
        //   레이스·로비·캐퍼스·조작법·완주·단서 카드까지.
        WoodTex     = Resources.Load<Texture2D>("UI/Wood")  ?? Solid(Wood);
        WoodDarkTex = Solid(WoodDark);
        PaperTex    = Resources.Load<Texture2D>("UI/Paper") ?? Solid(Paper);
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

    // ── 나무 장난감 리모컨 ─────────────────────────────────────────────────────
    //
    //  ★★ 2026-10-08 — 수연이 만든 그림(<c>Resources/UI/Remote.png</c>)을 그대로 쓴다.
    //  2026-09-16 에 정한 방향이 <b>「박물관 기념품점의 나무 장난감 리모컨」</b> 이었는데,
    //  여태 그걸 <see cref="Panel"/>(나무 상자 + 종이 라벨)로 <b>흉내만</b> 내고 있었다.
    //
    //  ★ 그림이 없으면 <see cref="Panel"/> 로 떨어진다 — 파일을 빼도 HUD 가 안 깨진다.

    static Texture2D remoteTex;
    static bool remoteTried;

    /// <summary>리모컨 그림. 없으면 <c>null</c>.</summary>
    public static Texture2D RemoteTex
    {
        get
        {
            if (!remoteTried) { remoteTried = true; remoteTex = Resources.Load<Texture2D>("UI/Remote"); }
            return remoteTex;
        }
    }

    /// <summary>
    /// 리모컨을 그린다. <b>그림이 정사각형이라 <c>r</c> 도 정사각으로 줘라</b> —
    /// 가로로 늘이면 나무결이 늘어나서 바로 티가 난다.
    /// </summary>
    public static void Remote(Rect r)
    {
        if (RemoteTex != null) GUI.DrawTexture(r, RemoteTex, ScaleMode.StretchToFill, true);
        else Panel(r);
    }

    //  아래 넷은 <b>그림에서 재서</b> 적은 값이다(1254px 기준). 그림을 새로 받으면 다시 재라 —
    //  <c>scratchpad/panel.py</c> 가 크림색 구간을 훑어서 뱉는다.
    const float PaperX0 = 0.155f, PaperX1 = 0.855f;
    const float PaperY0 = 0.495f, PaperY1 = 0.800f;


    static Texture2D stickTex;
    static bool stickTried;

    /// <summary>조종 레버 그림. 없으면 <c>null</c> — 리모컨만 그려진다.</summary>
    public static Texture2D StickTex
    {
        get
        {
            if (!stickTried) { stickTried = true; stickTex = Resources.Load<Texture2D>("UI/RemoteStick"); }
            return stickTex;
        }
    }

    /// <summary>
    /// 레버의 <b>경첩</b> 위치(그림 위에서부터의 비율). 그림을 훑어서 재었다 —
    /// 머리(폭 573~605px) → 경첩(165~190) → 손잡이(115) 로 줄어드는 자리가 0.42 다.
    /// <b>여기를 축으로 돌려야 레버로 보인다</b> — 밑동을 잡으면 부채질하는 것 같다.
    /// </summary>
    public const float StickHinge = 0.42f;

    /// <summary>리모컨의 <b>종이판 안쪽</b>. 글씨는 전부 여기 안에만 — 나무 테를 넘으면 잘려 보인다.</summary>
    public static Rect RemotePaper(Rect r) => new Rect(
        r.x + r.width * PaperX0, r.y + r.height * PaperY0,
        r.width * (PaperX1 - PaperX0), r.height * (PaperY1 - PaperY0));

    /// <summary>초록(왼쪽) · 빨강(오른쪽) 버튼 자리. 불을 얹으면 «눌렸다» 로 보인다.</summary>
    public static Rect RemoteButton(Rect r, bool right)
    {
        float cx = right ? 0.662f : 0.337f;
        const float cy = 0.757f, bw = 0.118f, bh = 0.064f;
        return new Rect(r.x + r.width * (cx - bw * 0.5f), r.y + r.height * (cy - bh * 0.5f),
                        r.width * bw, r.height * bh);
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

    // ── 곤 발바닥 버튼·손글씨 종이·곤 귀 ─────────────────────────────────
    //
    //  ★★ 2026-10-12 — 세 HUD 가 <b>같은 칩과 같은 카드</b>를 그린다. 여기 한 군데에
    //  두면 한 번 고쳐 셋이 같이 바뀜다 — 팔레트를 <c>Hud</c> 에 모아 둔 것과 같은 이유다.
    //  ★ 그림이 없으면 전부 예전 모양으로 떨어진다.

    static Texture2D pawDownTex;
    static Texture2D pawTex, earsTex, noteTex;
    static bool uiTried;

    static void LoadUi()
    {
        if (uiTried) return;
        uiTried = true;
        pawTex  = Resources.Load<Texture2D>("UI/Paw");
        pawDownTex = Resources.Load<Texture2D>("UI/Paw_Pressed");
        pawDownTex = Resources.Load<Texture2D>("UI/Paw_Pressed");
        earsTex = Resources.Load<Texture2D>("UI/Ears");
        noteTex = Resources.Load<Texture2D>("UI/Note");
    }

    public static Texture2D PawTex  { get { LoadUi(); return pawTex;  } }
    public static Texture2D EarsTex { get { LoadUi(); return earsTex; } }
    public static Texture2D NoteTex { get { LoadUi(); return noteTex; } }

    /// <summary>
    /// 조작법 칩. 곤 발바닥이 있으면 그걸 앞에 두고, 없으면 「H」 글자만.
    /// <b>마우스로도 눌린다</b> — 누른 것을 <c>true</c> 로 돌려준다.
    /// </summary>
    /// <summary>
    /// ★★ <b>곰 발바닥 버튼</b>(2026-10-12). 수연: *"나무 판이 아니라 곰발바닥 자체를
    /// UI 아이콘으로. 기껏 귀엽게 만들어 놨는데."* <b>맞는 말이다</b> — 발바닥을 네모 판
    /// 안에 넣으면 그건 «발바닥 아이콘이 붙은 버튼» 이지 «발바닥 버튼» 이 아니다.
    ///
    /// <b>판을 통째로 없앴다.</b> 남은 건 발바닥 하나와 글자뿐이다.
    ///
    /// ★ 글자가 읽히는 건 <b>종이 테두리</b> 덕이다. 판이 없으면 크림색 벽에서도 어두운
    ///   노면에서도 글자가 묻히는데, <b>글자 둘레에 종이색을 여덟 번 깔면</b> 어느 배경에서도
    ///   테두리가 생긴다. 카운트다운 숫자가 쓰는 방법과 같고(그쪽은 반대로 어두운 그림자),
    ///   <b>판보다 싸고 아무것도 안 가린다.</b> 본문 색은 <see cref="Ink"/> 그대로라
    ///   7.7:1 대비 규칙도 안 깨진다.
    ///
    /// ★ 눌린 그림은 <b>같은 정사각 칸에 그대로 그린다.</b> 그린 사람이 128×128 안에
    ///   이미 납작하게(세로 81 → 54) 그리고 아래로 내려 그려 놨다 —
    ///   비율을 내가 다시 계산하면 그 의도가 지워진다. <b>칸은 정사각 하나면 된다.</b>
    /// </summary>
    public static bool PawButton(Rect r, string label, int size = 14)
    {
        LoadUi();

        bool over = MouseOver(r);
        var m = UnityEngine.InputSystem.Mouse.current;
        bool held = over && m != null && m.leftButton.isPressed;
        float sink = held ? 2f : 0f;

        float s = r.height;
        float lx = r.x + s + 2f;

        // ★ 글자 <b>뒤에만</b> 종이를 깐다. 칸 전체를 덮으면 그게 판이고,
        //   글자만큼만 깔면 <b>발바닥이 꼬리표를 물고 있는</b> 모양이 된다.
        //   재 봤다(스크래치패드 <c>paw_ab.png</c>): 종이 없이 테두리만 두르면
        //   크림 벽에서는 멀쩡한데 <b>어두운 노면에서 글자가 뭉갠다.</b>
        var face = Resize(Text, size, TextAnchor.MiddleLeft);
        float tw = face.CalcSize(new GUIContent(label)).x;
        // 꼬리표를 발바닥 <b>밑으로 밀어 넣는다</b>. 떨어뜨려 놓으면 물건이 둘로 보이고,
        //   겹쳐 놓으면 발이 꼬리표를 물고 있는 하나가 된다.
        float tagX = r.x + s * 0.58f;
        var tag = new Rect(tagX, r.y + r.height * 0.5f - 13f + sink, lx + tw + 8f - tagX, 26f);
        GUI.color = new Color(1f, 1f, 1f, 0.96f);
        GUI.DrawTexture(tag, PaperTex);
        GUI.color = Color.white;

        // 발바닥은 꼬리표 <b>위에</b> 그린다 — 앞에 있어야 «발이 들고 있다» 로 읽힌다.
        var tex = held && pawDownTex != null ? pawDownTex : pawTex;
        float grow = held ? 1f : (over ? 1.08f : 1f);
        float off = s * (grow - 1f) * 0.5f;
        var paw = new Rect(r.x - off, r.y - off + sink, s * grow, s * grow);
        if (tex != null) GUI.DrawTexture(paw, tex, ScaleMode.ScaleToFit, true);

        face.normal.textColor = held ? Brass : Ink;
        GUI.Label(new Rect(lx, tag.y, tag.width, tag.height), label, face);

        // ★★ 2026-10-12 — <b>OnGUI 는 한 프레임에 두 번 돈다</b>(Layout · Repaint).
        //   <c>wasPressedThisFrame</c> 은 그 프레임 내내 참이라 <b>true 가 두 번</b> 나오고,
        //   받는 쪽이 토글이면 켰다 껐다 해서 <b>아무 일도 안 일어난다.</b>
        //   수연: *"로비에서 클릭해도 걸어다니기가 안 된다."* — 이게 그거다.
        //   그리는 패스에서만 한 번 돌려준다.
        return over && m != null && m.leftButton.wasPressedThisFrame
               && Event.current != null && Event.current.type == EventType.Repaint;
    }

    /// <summary>마우스가 이 칸 위에 있나. <b>화면 좌표를 가상 1280x720 으로 내린다</b> — 태엽 회전축과 같은 함정.</summary>
    public static bool MouseOver(Rect r)
    {
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m == null) return false;
        Vector2 v = m.position.ReadValue();
        return r.Contains(new Vector2(v.x / ScaleFactor, (Screen.height - v.y) / ScaleFactor));
    }

    /// <summary>
    /// 판 없이 읽히는 글자. 종이색을 여덟 방향으로 깔아 <b>글자마다 테두리</b>를 만든다.
    /// 한 장만 깔면 한쪽만 읽히고, 여덟이면 어느 배경에서도 떨어져 보인다.
    /// </summary>
    public static void HaloLabel(Rect r, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var halo = Resize(Text, size, anchor);
        halo.normal.textColor = new Color(Paper.r, Paper.g, Paper.b, 0.92f);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                GUI.Label(new Rect(r.x + dx * 2f, r.y + dy * 2f, r.width, r.height), text, halo);
            }

        var ink = Resize(Text, size, anchor);
        ink.normal.textColor = color;
        GUI.Label(r, text, ink);
    }

    /// <summary>
    /// ★ 이 글이 들어가려면 칸이 얼마나 넓어야 하나. <b>칸 크기를 손으로 적지 마라</b> —
    /// 글이 길어지면 조용히 잘린다(HUD 에서 이미 여러 번 겪었다).
    /// </summary>
    public static float PawWidth(string label, int size, float height)
        => height + 2f + Resize(Text, size).CalcSize(new GUIContent(label)).x + 10f;

    public static bool PawChip(Rect r, string label = "조작법") => PawButton(r, label, 13);

    /// <summary>
    /// 손글씨 종이 카드. 그림이 있으면 한지 한 장, 없으면 <see cref="Panel"/>.
    /// 같이 <b>곤 귀</b>를 언어 — 카드 위에 귀가 빼꿜 나오면 «고문» 이 된다.
    /// </summary>
    public static void NoteCard(Rect r)
    {
        LoadUi();
        if (earsTex != null)
        {
            float ew = r.width * 0.30f, eh = ew * 0.5f;
            GUI.DrawTexture(new Rect(r.center.x - ew * 0.5f, r.y - eh * 0.62f, ew, eh),
                            earsTex, ScaleMode.ScaleToFit, true);
        }
        if (noteTex != null) GUI.DrawTexture(r, noteTex, ScaleMode.StretchToFill, true);
        else Panel(r);
    }

    /// <summary>종이 카드 안쪽. 한지 테두리가 있어 <see cref="Inner"/> 보다 좀 더 물러난다.</summary>
    public static Rect NoteInner(Rect r) =>
        noteTex != null ? new Rect(r.x + r.width * 0.055f, r.y + r.height * 0.055f,
                                   r.width * 0.89f, r.height * 0.89f)
                        : Inner(r);

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
