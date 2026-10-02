using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전시실용 임시 화면 표시. 수집 현황, 눈앞의 전시품 이름, 클릭했을 때의 설명.
/// 진짜 UI 를 만들 때 통째로 버릴 스크립트야.
/// </summary>
public class GalleryHUD : MonoBehaviour
{
    public GallerySelector selector;

    [Header("폰트 (비워 두면 프로젝트 한글 폰트를 쓴다)")]
    public Font uiFont;

    [Header("테스트 도우미")]
    [Tooltip("켜두면 F9 로 전부 수집, F10 으로 전부 초기화. 제출 전에 꺼")]
    public bool debugKeys = true;

    Texture2D panelTex;
    GUIStyle titleStyle, labelStyle, valueStyle, promptStyle, bodyStyle;
    bool ready;

    void Awake()
    {
        panelTex = Solid(new Color(0.09f, 0.10f, 0.08f, 0.78f));
    }

    void Update()
    {
        TickViewer();

        var keys = Keyboard.current;
        if (keys == null) return;

        // 로비로 돌아가기. 전시실은 궤도 카메라라 걸어가서 여는 문이 아니다 — 어디서든 E.
        if (keys.eKey.wasPressedThisFrame && SceneDoor.Nearest != null)
        {
            SceneDoor.Nearest.Enter();
            return;
        }

        if (!debugKeys || selector == null) return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            foreach (var c in selector.cases)
                if (c != null) CollectionState.Collect(c.itemId);
            selector.RecountCollected();
        }
        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            CollectionState.ClearAll();
            selector.RecountCollected();
        }
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void BuildStyles()
    {
        var font = HudFont.Resolve(uiFont);

        titleStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 22, fontStyle = FontStyle.Bold }, font);
        titleStyle.normal.textColor = Color.white;

        labelStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 12 }, font);
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);

        valueStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 18, fontStyle = FontStyle.Bold }, font);
        valueStyle.normal.textColor = Color.white;

        promptStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }, font);
        promptStyle.normal.textColor = Color.white;

        bodyStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true }, font);
        bodyStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        ready = true;
    }

    void OnDisable()
    {
        if (viewer != null) { Destroy(viewer); viewer = null; }
    }

    void OnGUI()
    {
        if (!ready) BuildStyles();
        float w = Screen.width, h = Screen.height;

        DrawProgress();
        DrawViewer(w, h);
        DrawHoverName(w, h);
        DrawOpenedPanel(w, h);
        DrawBackPrompt(w, h);

        // 화면 아래 조작 안내줄은 뺐다(2026-09-17 유저). 마우스로 끌어보면 바로 아는 조작이라
        // 줄글로 설명할 게 아니었고, 전시실은 "가만히 보는 방" 이라 글자가 적을수록 낫다.
        // 개발용 단축키는 사라진 게 아니라 여전히 듣는다 — 목록은 프로젝트 루트의
        // 개발자_단축키.md 에 있다.
    }

    ExhibitViewer viewer;

    /// <summary>
    /// <b>고른 전시품을 돌려보는 패널.</b> 2026-10-02 유저 요청 —
    /// *"설명은 아주 좋은데 좀 허전하다. 선택한 아이템을 빙글빙글 돌리는 패널을 왼쪽에."*
    ///
    /// 진열장 안의 물건은 유리 너머 8 m 라 아무리 잘 만들어도 안 보인다.
    /// 여기서는 화면 가득 차게 보이고 <b>끌어서 돌린다</b> — 손을 떼면 저절로 돈다.
    ///
    /// ★ <see cref="ExhibitViewer"/> 를 <b>실행 중에 만든다.</b> 씬에 저장되는 게 없어서
    /// 전시실을 다시 굽지 않아도 들어온다.
    /// </summary>
    /// <summary>패널 자리. <b>Update 와 OnGUI 가 같은 함수를 쓴다</b> — 두 군데서 계산하면 어긋난다.</summary>
    static Rect ViewerBox => new Rect(16f, 104f, 268f, 268f);

    static Rect ViewerInner
    {
        get { var b = ViewerBox; return new Rect(b.x + 10, b.y + 10, b.width - 20, b.height - 44); }
    }

    /// <summary>
    /// ★★ 뷰어가 <b>실제로 일하는 자리</b>. 마우스를 읽고 무대를 돌리고 카메라를 찍는다.
    ///
    /// 2026-10-02 유저: *"진열장을 누르면 게임이 정지되고 BGM 도 멈춘다."*
    /// 이걸 <see cref="OnGUI"/> 에서 하고 있었다 — <b>OnGUI 는 한 프레임에 여러 번 돈다.</b>
    /// 거기서 <c>Camera.Render()</c> 를 부르면 그리는 도중에 또 그리기를 시작하는 꼴이고,
    /// <c>Instantiate</c>·<c>Destroy</c> 도 같이 여러 번 돈다.
    /// <b>OnGUI 는 그리기만 한다.</b>
    /// </summary>
    void TickViewer()
    {
        if (viewer == null) viewer = gameObject.AddComponent<ExhibitViewer>();
        viewer.Show(selector != null ? selector.Opened : null);
        viewer.Handle(ViewerInner);
    }

    /// <summary>
    /// 전시품 패널 — <b>진열장을 클릭했을 때만</b> 뜬다.
    ///
    /// 2026-10-02 유저: *"평소에는 전시실이 아무것도 없다가, 진열장을 클릭하면
    /// 아이템이랑 설명이 동시에 뜨는 구조를 원해."*
    ///
    /// 전에는 빈 상자가 늘 왼쪽에 떠 있었다 — 안에 아무 것도 없는 액자는
    /// <b>«아직 안 만든 자리»</b> 로 보인다. 아무 것도 안 고른 상태의 기본값은
    /// «빈 패널» 이 아니라 <b>«패널 없음»</b> 이어야 한다.
    /// </summary>
    void DrawViewer(float w, float h)
    {
        var opened = selector != null ? selector.Opened : null;
        if (opened == null) { DrawMemo(w, h); return; }

        var box = ViewerBox;
        GUI.DrawTexture(box, panelTex);

        if (viewer != null && viewer.Ready)
            GUI.DrawTexture(ViewerInner, viewer.Image, ScaleMode.ScaleToFit);

        GUI.Label(new Rect(box.x + 14, box.yMax - 32, box.width - 28, 22),
                  opened.IsCollected ? "끌어서 돌려보기" : "아직 찾지 못한 전시품", labelStyle);
    }

    /// <summary>
    /// <b>임시 메모장.</b> 유저 요청(2026-10-02) — *"메모장 UI 를 작게 하나 만들어서
    /// 전시실 설명을 적고, 진열장을 클릭하면 아이템과 설명을 볼 수 있다고 나타나면 좋겠어.
    /// UI 는 나중에 내가 검토할게."*
    ///
    /// 방이 하는 일을 한 줄로 말하고, 다음에 뭘 누르면 되는지를 말한다.
    /// 그림 한 장 없이 <b>«여긴 뭐 하는 방이고 뭘 하면 되는지»</b> 가 전해지면 된 거야.
    /// </summary>
    void DrawMemo(float w, float h)
    {
        var box = new Rect(16f, 104f, 268f, 150f);
        GUI.DrawTexture(box, panelTex);

        float x = box.x + 18f, iw = box.width - 36f;
        GUI.Label(new Rect(x, box.y + 14f, iw, 24f), "전시실", valueStyle);

        GUI.Label(new Rect(x, box.y + 46f, iw, 20f), "철거를 막은 증거 여덟 점이", bodyStyle);
        GUI.Label(new Rect(x, box.y + 66f, iw, 20f), "여기 한 칸씩 모입니다.", bodyStyle);

        GUI.Label(new Rect(x, box.y + 100f, iw, 20f), "진열장을 클릭하면", labelStyle);
        GUI.Label(new Rect(x, box.y + 120f, iw, 20f), "전시품과 설명을 볼 수 있습니다.", labelStyle);
    }

    void DrawProgress()
    {
        // 제목과 숫자를 같은 줄 양끝에. 예전엔 세로로 쌓았는데 한글 폰트가 22px 에서
        // 줄높이를 넘겨서 글자가 겹쳤다.
        // 진행 막대도 뺐다(2026-09-17 유저) — 숫자가 이미 "3 / 8" 이라고 말하는데 막대가
        // 같은 말을 한 번 더 한다. 불이 켜지는 연출이 진짜 진행도 표시야.
        const float x = 16f, y = 16f, w = 256f, h = 72f;
        GUI.DrawTexture(new Rect(x, y, w, h), panelTex);

        int got = selector != null ? selector.CollectedCount : 0;
        int total = selector != null ? Mathf.Max(1, selector.TotalCount) : 1;

        GUI.Label(new Rect(x + 16, y + 10, 140, 32), "전시실", titleStyle);

        var countStyle = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(x + 150, y + 12, w - 166, 28), $"{got} / {total}", countStyle);

        GUI.Label(new Rect(x + 16, y + 46, w - 32, 18), "모은 전시품", labelStyle);
    }

    /// <summary>화면 아래 한 줄. 나가는 길이 없으면 방이 아니라 상자다.</summary>
    void DrawBackPrompt(float w, float h)
    {
        if (SceneDoor.Nearest == null) return;

        var box = new Rect(w * 0.5f - 90f, h - 56f, 180f, 30f);
        GUI.DrawTexture(box, panelTex);
        GUI.Label(box, "E   중앙홀로", promptStyle);
    }

    void DrawHoverName(float w, float h)
    {
        var hovered = selector != null ? selector.Hovered : null;
        if (hovered == null || hovered == (selector != null ? selector.Opened : null)) return;

        string text = hovered.IsCollected ? $"{hovered.Label}  —  클릭해서 보기"
                                          : $"???  —  아직 찾지 못함";
        var box = new Rect(w * 0.5f - 190, h * 0.66f, 380, 36);
        GUI.DrawTexture(box, panelTex);
        GUI.Label(box, text, promptStyle);
    }

    void DrawOpenedPanel(float w, float h)
    {
        var opened = selector != null ? selector.Opened : null;
        if (opened == null) return;

        var box = new Rect(w - 380, 120, 356, 200);
        GUI.DrawTexture(box, panelTex);

        if (!opened.IsCollected)
        {
            GUI.Label(new Rect(box.x + 20, box.y + 20, box.width - 40, 26), "???", valueStyle);
            GUI.Label(new Rect(box.x + 20, box.y + 54, box.width - 40, 100),
                      "아직 찾지 못한 전시품입니다.\n레이스에서 수집하면 여기 진열됩니다.", bodyStyle);
            if (!string.IsNullOrEmpty(opened.chapter))
                GUI.Label(new Rect(box.x + 20, box.y + 160, box.width - 40, 20),
                          $"출처  {opened.chapter}", labelStyle);
            return;
        }

        GUI.Label(new Rect(box.x + 20, box.y + 18, box.width - 40, 28), opened.Label, valueStyle);
        if (!string.IsNullOrEmpty(opened.chapter))
            GUI.Label(new Rect(box.x + 20, box.y + 46, box.width - 40, 18), opened.chapter, labelStyle);
        GUI.Label(new Rect(box.x + 20, box.y + 70, box.width - 40, 110), opened.description, bodyStyle);
    }
}
