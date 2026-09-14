using UnityEngine;

/// <summary>
/// 테스트용 화면 표시. 지금 어느 씬인지, 걷는 중인지 타는 중인지, 어떤 키를 쓸 수 있는지.
/// OnGUI 로 그려서 캔버스 세팅이 필요 없다 — 진짜 UI 를 만들 때 통째로 버릴 스크립트야.
///
/// 폰트: uiFont 를 비워 두면 OS 한글 폰트를 자동으로 잡는다(HudFont 참고).
/// Paperlogy 로 보고 싶으면 인스펙터의 uiFont 에 Paperlogy-7Bold 를 끌어다 놓으면 된다.
/// </summary>
public class TestHUD : MonoBehaviour
{
    [Header("연결 (없으면 해당 정보는 안 뜬다)")]
    public PlayerModeSwitcher modeSwitcher;
    public FirstPersonController player;
    public KartController kart;
    public LapTracker tracker;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    Texture2D panelTex, barBgTex, barFillTex, accentTex, pickupTex;
    GUIStyle bigStyle, labelStyle, valueStyle, hintStyle, centerHint;
    bool stylesReady;

    bool InKart => modeSwitcher != null && modeSwitcher.InKart;

    void Awake()
    {
        panelTex   = Solid(new Color(0.09f, 0.10f, 0.08f, 0.72f));
        barBgTex   = Solid(new Color(1f, 1f, 1f, 0.16f));
        barFillTex = Solid(new Color(0.61f, 0.77f, 0.54f, 0.95f));
        accentTex  = Solid(new Color(0.85f, 0.55f, 0.42f, 0.95f));
        pickupTex  = Solid(new Color(1f, 0.86f, 0.25f, 0.95f));
    }

    void Update()
    {
        if (tracker != null && tracker.Finished && KartInput.RestartPressed)
        {
            tracker.ResetRace();
            KartInput.Clear();
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

        bigStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 46, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight }, font);
        bigStyle.normal.textColor = Color.white;

        labelStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 12 }, font);
        labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);

        valueStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 20, fontStyle = FontStyle.Bold }, font);
        valueStyle.normal.textColor = Color.white;

        hintStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 13 }, font);
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

        centerHint = new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter };

        stylesReady = true;
    }

    void OnGUI()
    {
        if (!stylesReady) BuildStyles();

        float w = Screen.width, h = Screen.height;

        DrawStatusPanel();
        if (InKart) DrawRacePanels(w, h);
        DrawPickupToast(w, h);
        DrawControlsLine(w, h);
        DrawCrosshair(w, h);
        if (tracker != null && tracker.Finished && InKart) DrawFinish(w, h);
    }

    void DrawStatusPanel()
    {
        GUI.DrawTexture(new Rect(16, 16, 250, 62), panelTex);

        GUI.Label(new Rect(30, 22, 230, 16), "씬", labelStyle);
        GUI.Label(new Rect(30, 34, 230, 22), SceneNavigator.CurrentSceneName, valueStyle);

        string mode = InKart ? "카트"
                    : (player != null && player.IsFlying ? "비행" : "도보");
        GUI.Label(new Rect(196, 22, 60, 16), "상태", labelStyle);
        GUI.Label(new Rect(196, 36, 60, 20),
                  mode, new GUIStyle(labelStyle) { fontSize = 14, fontStyle = FontStyle.Bold });
    }

    void DrawRacePanels(float w, float h)
    {
        if (tracker != null)
        {
            GUI.DrawTexture(new Rect(16, 88, 240, 108), panelTex);
            GUI.Label(new Rect(30, 94, 200, 16), "랩", labelStyle);
            GUI.Label(new Rect(30, 108, 200, 26),
                      $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}", valueStyle);

            GUI.Label(new Rect(30, 138, 100, 16), "현재 랩", labelStyle);
            GUI.Label(new Rect(30, 152, 100, 26), LapTracker.FormatTime(tracker.LapTime), valueStyle);

            GUI.Label(new Rect(140, 138, 110, 16), "최고 랩", labelStyle);
            GUI.Label(new Rect(140, 152, 110, 26), LapTracker.FormatTime(tracker.BestLapTime), valueStyle);
        }

        if (kart == null) return;

        int kph = Mathf.Abs(Mathf.RoundToInt(kart.SpeedKph));
        GUI.Label(new Rect(w - 210, h - 110, 180, 60), kph.ToString(), bigStyle);
        GUI.Label(new Rect(w - 62, h - 62, 60, 20), "km/h", labelStyle);

        float charge01 = kart.boostChargeMax > 0f ? kart.BoostCharge / kart.boostChargeMax : 0f;
        var bar = new Rect(w - 210, h - 44, 180, 10);
        GUI.DrawTexture(bar, barBgTex);
        if (charge01 > 0.01f)
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * charge01, bar.height),
                            charge01 > 0.85f ? accentTex : barFillTex);
        if (kart.IsBoosting)
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * kart.BoostRemaining01, bar.height), accentTex);

        GUI.Label(new Rect(w - 210, h - 30, 180, 16),
                  kart.IsBoosting ? "부스트!" : (kart.IsDrifting ? "드리프트" : "드리프트 충전"), labelStyle);
    }

    void DrawControlsLine(float w, float h)
    {
        string keys;
        if (InKart)
        {
            keys = "화살표·WASD 운전     SPACE 톡 호핑 / 꾹 드리프트     R 제자리로";
        }
        else if (player != null && player.IsFlying)
        {
            // 비행 중에 빠져나오는 법을 제일 앞에 둔다. 모르면 공중에 갇힌 것처럼 느껴진다.
            keys = "◆ 비행 중 — F 를 누르면 착지     WASD 이동     SPACE 위 / CTRL 아래";
        }
        else
        {
            keys = "WASD 이동     마우스 시선     SHIFT 달리기     SPACE 점프     F 비행";
        }
        keys += "     F1·F2 씬 이동     ESC 커서";

        GUI.Label(new Rect(0, h - 26, w, 20), keys, centerHint);
    }

    /// <summary>수집품을 주우면 잠깐 뜨는 안내. 전광등만으로는 뭘 주웠는지 모르니까.</summary>
    void DrawPickupToast(float w, float h)
    {
        const float showSeconds = 3f;
        float age = Time.time - ExhibitPickup.LastMessageTime;
        if (age > showSeconds || string.IsNullOrEmpty(ExhibitPickup.LastMessage)) return;

        var box = new Rect(w * 0.5f - 210, h * 0.16f, 420, 46);
        GUI.DrawTexture(box, panelTex);

        // 노란 띠 — 전광등과 같은 색이라 둘이 한 사건으로 읽힌다
        GUI.DrawTexture(new Rect(box.x, box.y, 5f, box.height), pickupTex);

        var style = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
        GUI.Label(box, ExhibitPickup.LastMessage, style);
    }

    void DrawCrosshair(float w, float h)
    {
        if (InKart) return;
        // 1인칭일 때 화면 중앙에 작은 점 — 어디를 보고 있는지 알기 쉬우라고
        GUI.DrawTexture(new Rect(w * 0.5f - 2f, h * 0.5f - 2f, 4f, 4f), barBgTex);
    }

    void DrawFinish(float w, float h)
    {
        var box = new Rect(w * 0.5f - 170, h * 0.5f - 70, 340, 140);
        GUI.DrawTexture(box, panelTex);

        var center = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        GUI.Label(new Rect(box.x, box.y + 18, box.width, 34), "완주!", center);

        var sub = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUI.Label(new Rect(box.x, box.y + 58, box.width, 24),
                  $"총 시간   {LapTracker.FormatTime(tracker.TotalTime)}", sub);
        GUI.Label(new Rect(box.x, box.y + 80, box.width, 24),
                  $"최고 랩   {LapTracker.FormatTime(tracker.BestLapTime)}", sub);

        GUI.Label(new Rect(box.x, box.y + 108, box.width, 20), "ENTER 를 누르면 다시 시작", centerHint);
    }
}
