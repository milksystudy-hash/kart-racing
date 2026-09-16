using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 레이스 화면 표시 — 나무 판에 종이 라벨을 붙인 장난감 리모컨.
///
/// 무인 모형 카트를 옆에서 조종한다는 설정(2026-09-16)이라 화면 자체가 그 리모컨이다.
/// 색은 로비·전시실과 같은 목재/크림을 써서 화면과 방이 같은 재료로 읽히게 했다.
///
/// 플레이어에게 보여줄 것만 화면에 둔다:
///   · 랩과 시간 · 순위 · 속도와 태엽 · 주운 물건 안내
/// 조작법은 <b>H</b> 를 누를 때만 뜬다. 화면 아래 띠로 늘 깔아두면 답답하고,
/// 한 번 외우면 다시 볼 일도 없다.
///
/// 개발용 정보(씬 이름, 진행 중인 장)는 debugKeys 가 켜져 있을 때만 나온다.
/// OnGUI 로 그려서 캔버스 세팅이 필요 없다 — 진짜 UI 로 갈 때 통째로 버릴 스크립트야.
/// </summary>
public class TestHUD : MonoBehaviour
{
    [Header("연결 (없으면 해당 정보는 안 뜬다)")]
    public PlayerModeSwitcher modeSwitcher;
    public FirstPersonController player;
    public KartController kart;
    public LapTracker tracker;
    public RaceStandings standings;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    [Header("개발용")]
    [Tooltip("켜두면 씬 이름과 진행 중인 장이 보이고 F7/F8 로 장을 넘긴다. 제출 전에 꺼")]
    public bool debugKeys = true;

    // ---- 색 ----
    // 밝고 따뜻하게. 어두운 판에 흰 글자는 눈이 피로하고 "유니티 기본" 으로 읽힌다.
    static readonly Color Wood     = new Color32(0xA5, 0x76, 0x4B, 0xFF);   // 판 테두리
    static readonly Color WoodDark = new Color32(0x7A, 0x55, 0x35, 0xFF);   // 아래 그림자 결
    static readonly Color Paper    = new Color32(0xFF, 0xF6, 0xE5, 0xFF);   // 종이 라벨
    static readonly Color Ink      = new Color32(0x4A, 0x37, 0x28, 0xFF);
    static readonly Color InkSoft  = new Color32(0x9A, 0x82, 0x6C, 0xFF);
    static readonly Color Brass    = new Color32(0xE0, 0x9B, 0x2E, 0xFF);   // 태엽이 터질 때
    static readonly Color Ribbon   = new Color32(0xC4, 0x45, 0x3E, 0xFF);

    Texture2D woodTex, woodDarkTex, paperTex, brassTex, ribbonTex;
    GUIStyle label, value, big, title, hint;
    bool ready;
    bool showControls;

    bool InKart => modeSwitcher != null && modeSwitcher.InKart;

    void Awake()
    {
        woodTex     = Solid(Wood);
        woodDarkTex = Solid(WoodDark);
        paperTex    = Solid(Paper);
        brassTex    = Solid(Brass);
        ribbonTex   = Solid(Ribbon);
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        if (tracker != null && tracker.Finished && KartInput.RestartPressed)
        {
            tracker.ResetRace();
            KartInput.Clear();
        }

        if (!debugKeys) return;

        // 아직 임무 판정이 없어서 장이 저절로 안 넘어간다. 손으로 넘겨보는 용도.
        if (k.f7Key.wasPressedThisFrame) { StoryProgress.AdvanceChapter(); SceneNavigator.Reload(); }
        if (k.f8Key.wasPressedThisFrame) { StoryProgress.CurrentChapter = 1; SceneNavigator.Reload(); }
    }

    // ------------------------------------------------------------------
    //  나무 판 + 종이 라벨
    // ------------------------------------------------------------------
    /// <summary>
    /// 나무 테두리를 바깥에 남기는 게 핵심이다 — 종이가 판 위에 붙어 있는 것처럼 보인다.
    /// 아래쪽에만 어두운 결을 한 줄 깔면 판이 살짝 두꺼워 보인다.
    /// </summary>
    void Panel(Rect r)
    {
        GUI.DrawTexture(r, woodTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 4f, r.width, 4f), woodDarkTex);
        GUI.DrawTexture(new Rect(r.x + 7f, r.y + 7f, r.width - 14f, r.height - 16f), paperTex);
    }

    void BuildStyles()
    {
        var f = HudFont.Resolve(uiFont);

        label = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 14 }, f);
        label.normal.textColor = InkSoft;

        value = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 26, fontStyle = FontStyle.Bold }, f);
        value.normal.textColor = Ink;

        big = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight }, f);
        big.normal.textColor = Ink;

        title = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }, f);
        title.normal.textColor = Ink;

        hint = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 15 }, f);
        hint.normal.textColor = InkSoft;

        ready = true;
    }

    void OnGUI()
    {
        if (!ready) BuildStyles();

        float w = Screen.width, h = Screen.height;

        if (InKart)
        {
            DrawLapPanel();
            DrawRankPanel(w);
            DrawSpeedPanel(w, h);
        }
        if (debugKeys) DrawDevPanel(w);

        DrawToast(w, h);
        DrawHelpHint(w, h);
        if (showControls) DrawControls(w, h);
        if (tracker != null && tracker.Finished && InKart) DrawFinish(w, h);
    }

    // ---- 랩과 시간 ----
    void DrawLapPanel()
    {
        if (tracker == null) return;

        Panel(new Rect(18, 18, 250, 150));

        GUI.Label(new Rect(34, 28, 220, 20), "랩", label);
        GUI.Label(new Rect(34, 46, 220, 34),
                  $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}", value);

        GUI.Label(new Rect(34, 86, 220, 20), "현재", label);
        GUI.Label(new Rect(34, 104, 220, 30), LapTracker.FormatTime(tracker.LapTime),
                  new GUIStyle(value) { fontSize = 22 });

        GUI.Label(new Rect(150, 86, 100, 20), "최고", label);
        GUI.Label(new Rect(150, 104, 100, 30), LapTracker.FormatTime(tracker.BestLapTime),
                  new GUIStyle(value) { fontSize = 22 });
    }

    // ---- 순위 ----
    void DrawRankPanel(float w)
    {
        if (standings == null || standings.RacerCount <= 0) return;

        Panel(new Rect(w - 168, 18, 150, 96));
        GUI.Label(new Rect(w - 152, 28, 120, 20), "순위", label);
        GUI.Label(new Rect(w - 152, 46, 120, 44),
                  RaceStandings.PlaceLabel(standings.PlayerPlace),
                  new GUIStyle(value) { fontSize = 34 });
        GUI.Label(new Rect(w - 72, 62, 50, 24), $"/ {standings.RacerCount}", label);
    }

    // ---- 속도와 태엽 ----
    void DrawSpeedPanel(float w, float h)
    {
        if (kart == null) return;

        var panel = new Rect(w - 250, h - 140, 232, 122);
        Panel(panel);

        int kph = Mathf.Abs(Mathf.RoundToInt(kart.SpeedKph));
        GUI.Label(new Rect(panel.x + 16, panel.y + 12, 150, 58), kph.ToString(), big);
        GUI.Label(new Rect(panel.x + 172, panel.y + 42, 50, 24), "km/h", label);

        // 태엽 — 드리프트로 감고, 놓으면 풀리며 튀어나간다
        var bar = new Rect(panel.x + 18, panel.y + 80, 196, 12);
        GUI.DrawTexture(bar, woodDarkTex);

        float charge01 = kart.boostChargeMax > 0f ? kart.BoostCharge / kart.boostChargeMax : 0f;
        if (kart.IsBoosting)
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * kart.BoostRemaining01, bar.height), brassTex);
        else if (charge01 > 0.01f)
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * charge01, bar.height),
                            charge01 > 0.85f ? brassTex : woodTex);

        GUI.Label(new Rect(panel.x + 18, panel.y + 94, 200, 20),
                  kart.IsBoosting ? "태엽이 풀린다!" : (kart.IsDrifting ? "태엽 감는 중" : "태엽"), label);
    }

    // ---- 개발용 ----
    void DrawDevPanel(float w)
    {
        Panel(new Rect(w * 0.5f - 150, 12, 300, 52));
        GUI.Label(new Rect(w * 0.5f - 134, 22, 268, 20),
                  $"{SceneNavigator.CurrentSceneName}   ·   {StoryProgress.CurrentName}", hint);
        GUI.Label(new Rect(w * 0.5f - 134, 40, 268, 18), "F7 다음 장 · F8 첫 장 (개발용)",
                  new GUIStyle(hint) { fontSize = 12 });
    }

    // ---- 주운 물건 안내 ----
    void DrawToast(float w, float h)
    {
        const float seconds = 3f;
        if (Time.time - ExhibitPickup.LastMessageTime > seconds ||
            string.IsNullOrEmpty(ExhibitPickup.LastMessage)) return;

        var box = new Rect(w * 0.5f - 230, h * 0.17f, 460, 62);
        Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 16f), ribbonTex);
        GUI.Label(box, ExhibitPickup.LastMessage, title);
    }

    // ---- 조작법 ----
    void DrawHelpHint(float w, float h)
    {
        if (showControls) return;
        GUI.Label(new Rect(18, h - 34, 200, 22), "H  조작법", hint);
    }

    void DrawControls(float w, float h)
    {
        var box = new Rect(w * 0.5f - 210, h * 0.5f - 110, 420, 220);
        Panel(box);

        GUI.Label(new Rect(box.x, box.y + 18, box.width, 26), "조작법", title);

        string[,] rows =
        {
            { "화살표 · WASD", "운전" },
            { "SPACE", "톡 누르면 호핑 / 꾹 누르면 드리프트" },
            { "R", "제자리로 되돌리기" },
            { "ENTER", "완주 후 다시 시작" },
            { "H", "이 창 닫기" },
        };

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = box.y + 58 + i * 30;
            GUI.Label(new Rect(box.x + 28, y, 150, 24), rows[i, 0],
                      new GUIStyle(hint) { fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(box.x + 178, y, 224, 24), rows[i, 1], hint);
        }
    }

    // ---- 완주 ----
    void DrawFinish(float w, float h)
    {
        var box = new Rect(w * 0.5f - 190, h * 0.5f - 110, 380, 220);
        Panel(box);

        GUI.Label(new Rect(box.x, box.y + 22, box.width, 40), "완주!",
                  new GUIStyle(title) { fontSize = 34 });

        var centre = new GUIStyle(value) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(box.x, box.y + 76, box.width, 28),
                  $"총 시간   {LapTracker.FormatTime(tracker.TotalTime)}", centre);
        GUI.Label(new Rect(box.x, box.y + 106, box.width, 28),
                  $"최고 랩   {LapTracker.FormatTime(tracker.BestLapTime)}", centre);

        if (standings != null && standings.RacerCount > 1)
        {
            bool first = standings.PlayerFinishedFirst;
            var line = new GUIStyle(hint) { alignment = TextAnchor.MiddleCenter };
            line.normal.textColor = first ? Brass : InkSoft;
            GUI.Label(new Rect(box.x, box.y + 142, box.width, 24),
                      first ? "◆ 선택 임무 달성 — 1위로 완주" : "선택 임무 — 1위로 완주 (미달성)", line);
        }

        GUI.Label(new Rect(box.x, box.y + 176, box.width, 24), "ENTER 를 누르면 다시 시작",
                  new GUIStyle(hint) { alignment = TextAnchor.MiddleCenter });
    }
}
