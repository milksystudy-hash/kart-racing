using UnityEngine;

/// <summary>
/// 로비용 임시 화면 표시. 고른 캐릭터, 눈앞의 안내문, 출발문 카운트다운.
/// 진짜 UI 를 만들 때 통째로 버릴 스크립트야.
///
/// 폰트: uiFont 를 비워 두면 OS 한글 폰트를 자동으로 잡는다(HudFont 참고).
/// </summary>
public class LobbyHUD : MonoBehaviour
{
    public LobbySelector selector;
    public StartGate gate;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    Texture2D panelTex, dimTex, accentTex;
    GUIStyle titleStyle, labelStyle, valueStyle, promptStyle, hintStyle;
    bool ready;

    void Awake()
    {
        panelTex  = Solid(new Color(0.09f, 0.10f, 0.08f, 0.72f));
        dimTex    = Solid(new Color(1f, 1f, 1f, 0.16f));
        accentTex = Solid(new Color(0.94f, 0.71f, 0.29f, 0.95f));
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

        hintStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 13, alignment = TextAnchor.MiddleCenter }, font);
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

        ready = true;
    }

    void OnGUI()
    {
        if (!ready) BuildStyles();
        float w = Screen.width, h = Screen.height;

        // ---------- 좌상단: 고른 캐릭터 ----------
        GUI.DrawTexture(new Rect(16, 16, 230, 74), panelTex);
        GUI.Label(new Rect(30, 22, 200, 26), "로비", titleStyle);
        GUI.Label(new Rect(30, 50, 200, 14), "드라이버", labelStyle);
        GUI.Label(new Rect(30, 62, 200, 24),
                  GameSelection.HasSelection ? GameSelection.SelectedName : "선택 안 됨", valueStyle);

        // ---------- 화면 중앙 아래: 눈앞의 안내문 ----------
        if (selector != null && !string.IsNullOrEmpty(selector.Prompt))
        {
            var box = new Rect(w * 0.5f - 190, h * 0.62f, 380, 38);
            GUI.DrawTexture(box, panelTex);
            GUI.Label(box, selector.Prompt, promptStyle);
        }

        // ---------- 출발문 ----------
        if (gate != null && (gate.Hovered || gate.CountingDown))
        {
            var box = new Rect(w * 0.5f - 160, h * 0.30f, 320, 76);
            GUI.DrawTexture(box, panelTex);

            if (gate.Blocked)
            {
                GUI.Label(new Rect(box.x, box.y + 14, box.width, 24), "먼저 드라이버를 고르세요", promptStyle);
                GUI.Label(new Rect(box.x, box.y + 44, box.width, 20),
                          "받침대 위 캐릭터를 클릭하세요", hintStyle);
            }
            else if (gate.CountingDown)
            {
                GUI.Label(new Rect(box.x, box.y + 12, box.width, 26), "레이스 시작", promptStyle);

                float fill = 1f - Mathf.Clamp01(gate.Remaining / Mathf.Max(0.01f, gate.countdownSeconds));
                var bar = new Rect(box.x + 40, box.y + 48, box.width - 80, 10);
                GUI.DrawTexture(bar, dimTex);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), accentTex);
            }
            else
            {
                GUI.Label(new Rect(box.x, box.y + 14, box.width, 24), "출발문", promptStyle);
                GUI.Label(new Rect(box.x, box.y + 44, box.width, 20),
                          "클릭하면 레이스가 시작됩니다", hintStyle);
            }
        }

        // ---------- 하단: 조작법 ----------
        GUI.Label(new Rect(0, h - 26, w, 20),
                  "마우스 끌기 둘러보기     휠 확대·축소     클릭 선택     F1·F2 씬 이동",
                  hintStyle);
    }
}
