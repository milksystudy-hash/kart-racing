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

    void OnGUI()
    {
        if (!ready) BuildStyles();
        float w = Screen.width, h = Screen.height;

        DrawProgress();
        DrawHoverName(w, h);
        DrawOpenedPanel(w, h);
        DrawBackPrompt(w, h);

        // 화면 아래 조작 안내줄은 뺐다(2026-09-17 유저). 마우스로 끌어보면 바로 아는 조작이라
        // 줄글로 설명할 게 아니었고, 전시실은 "가만히 보는 방" 이라 글자가 적을수록 낫다.
        // 개발용 단축키는 사라진 게 아니라 여전히 듣는다 — 목록은 프로젝트 루트의
        // 개발자_단축키.md 에 있다.
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
