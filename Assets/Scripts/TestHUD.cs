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
    public RaceStandings standings;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    // 나무 판에 종이 라벨을 붙인 장난감 리모컨 느낌.
    // 무인 모형 카트를 옆에서 조종한다는 설정이라(2026-09-16), 화면이 곧 그 리모컨이다.
    // 색은 로비·전시실과 같은 목재/크림색을 쓴다 — 화면과 방이 같은 재료로 보이게.
    static readonly Color Wood      = new Color32(0x6B, 0x4A, 0x33, 0xF2);
    static readonly Color WoodLight = new Color32(0x8A, 0x6A, 0x48, 0xFF);
    static readonly Color Paper     = new Color32(0xEF, 0xE7, 0xD6, 0xFF);
    static readonly Color PaperEdge = new Color32(0xD6, 0xC9, 0xB0, 0xFF);
    static readonly Color Ink       = new Color32(0x3A, 0x2C, 0x22, 0xFF);
    static readonly Color InkSoft   = new Color32(0x7A, 0x66, 0x54, 0xFF);

    Texture2D woodTex, woodLightTex, paperTex, paperEdgeTex;
    Texture2D barBgTex, barFillTex, accentTex, pickupTex;
    GUIStyle bigStyle, labelStyle, valueStyle, hintStyle, centerHint;
    bool stylesReady;

    bool InKart => modeSwitcher != null && modeSwitcher.InKart;

    void Awake()
    {
        woodTex      = Solid(Wood);
        woodLightTex = Solid(WoodLight);
        paperTex     = Solid(Paper);
        paperEdgeTex = Solid(PaperEdge);

        barBgTex   = Solid(new Color32(0xD6, 0xC9, 0xB0, 0xFF));   // 종이에 눌린 홈
        barFillTex = Solid(new Color32(0x8A, 0x6A, 0x48, 0xFF));   // 감긴 태엽
        accentTex  = Solid(new Color32(0xC9, 0x8A, 0x3C, 0xFF));   // 놋쇠 — 터질 때
        pickupTex  = Solid(new Color32(0xC4, 0x45, 0x3E, 0xFF));   // 박물관 리본 빨강
    }

    /// <summary>
    /// 나무 판 한 장에 종이 라벨을 붙인다. 패널은 전부 이걸로 그린다.
    ///
    /// 나무 테두리를 바깥에 남기는 게 핵심 — 종이가 판 위에 "붙어 있는" 것처럼 보인다.
    /// 반투명 검은 사각형이 "유니티 기본 UI" 로 읽히던 걸 이걸로 바꾼다.
    /// </summary>
    void Panel(Rect r)
    {
        GUI.DrawTexture(r, woodTex);                                   // 나무 판
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2f), woodLightTex); // 위쪽 결 — 빛 받는 면

        var paper = new Rect(r.x + 6f, r.y + 6f, r.width - 12f, r.height - 12f);
        GUI.DrawTexture(paper, paperEdgeTex);
        GUI.DrawTexture(new Rect(paper.x, paper.y, paper.width - 1f, paper.height - 1f), paperTex);
    }

    [Header("테스트 도우미")]
    [Tooltip("켜두면 F7 로 다음 장, F8 로 첫 장. 임무 판정이 붙으면 필요 없어진다")]
    public bool chapterDebugKeys = true;

    void Update()
    {
        if (tracker != null && tracker.Finished && KartInput.RestartPressed)
        {
            tracker.ResetRace();
            KartInput.Clear();
        }

        if (!chapterDebugKeys || UnityEngine.InputSystem.Keyboard.current == null) return;

        // 아직 임무 판정이 없어서 장이 저절로 안 넘어간다. 손으로 넘겨보는 용도.
        if (UnityEngine.InputSystem.Keyboard.current.f7Key.wasPressedThisFrame)
        {
            StoryProgress.AdvanceChapter();
            SceneNavigator.Reload();
        }
        if (UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
        {
            StoryProgress.CurrentChapter = 1;
            SceneNavigator.Reload();
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
        bigStyle.normal.textColor = Ink;

        labelStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 12 }, font);
        labelStyle.normal.textColor = InkSoft;

        valueStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 20, fontStyle = FontStyle.Bold }, font);
        valueStyle.normal.textColor = Ink;

        hintStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 13 }, font);
        hintStyle.normal.textColor = InkSoft;

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
        Panel(new Rect(16, 16, 250, 62));

        GUI.Label(new Rect(30, 22, 230, 16), "씬", labelStyle);
        GUI.Label(new Rect(30, 34, 230, 22), SceneNavigator.CurrentSceneName, valueStyle);

        string mode = InKart ? "카트"
                    : (player != null && player.IsFlying ? "비행" : "도보");
        GUI.Label(new Rect(196, 22, 60, 16), "상태", labelStyle);
        GUI.Label(new Rect(196, 36, 60, 20),
                  mode, new GUIStyle(labelStyle) { fontSize = 14, fontStyle = FontStyle.Bold });

        // 지금 몇 장인지 — 어떤 수집품이 나올지를 이게 정한다
        Panel(new Rect(16, 84, 250, 40));
        GUI.Label(new Rect(30, 88, 230, 16), "진행 중", labelStyle);
        GUI.Label(new Rect(30, 102, 230, 18),
                  StoryProgress.CurrentName, new GUIStyle(labelStyle) { fontSize = 13 });
    }

    void DrawRacePanels(float w, float h)
    {
        if (tracker != null)
        {
            Panel(new Rect(16, 132, 240, 108));
            GUI.Label(new Rect(30, 138, 200, 16), "랩", labelStyle);
            GUI.Label(new Rect(30, 152, 200, 26),
                      $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}", valueStyle);

            GUI.Label(new Rect(30, 182, 100, 16), "현재 랩", labelStyle);
            GUI.Label(new Rect(30, 196, 100, 26), LapTracker.FormatTime(tracker.LapTime), valueStyle);

            GUI.Label(new Rect(140, 182, 110, 16), "최고 랩", labelStyle);
            GUI.Label(new Rect(140, 196, 110, 26), LapTracker.FormatTime(tracker.BestLapTime), valueStyle);
        }

        // 등수 — 필수 조건은 아니지만 달리는 내내 보인다
        if (standings != null && standings.RacerCount > 0)
        {
            Panel(new Rect(w - 150, 16, 134, 66));
            GUI.Label(new Rect(w - 136, 22, 110, 16), "순위", labelStyle);
            GUI.Label(new Rect(w - 136, 36, 110, 34),
                      $"{RaceStandings.PlaceLabel(standings.PlayerPlace)}",
                      new GUIStyle(valueStyle) { fontSize = 26 });
            GUI.Label(new Rect(w - 62, 46, 46, 20), $"/ {standings.RacerCount}", labelStyle);
        }

        if (kart == null) return;

        // 속도계와 태엽은 리모컨 아래쪽에 붙은 판 하나에 같이 올린다
        Panel(new Rect(w - 228, h - 122, 212, 106));

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
        keys += "     F1·F2 씬 이동";
        if (chapterDebugKeys) keys += "     F7 다음 장 / F8 첫 장";

        // 조작 안내는 바닥에 깔린 종이 띠 위에
        GUI.DrawTexture(new Rect(0, h - 30, w, 26), woodTex);
        GUI.Label(new Rect(0, h - 28, w, 22), keys, centerHint);
    }

    /// <summary>수집품을 주우면 잠깐 뜨는 안내. 전광등만으로는 뭘 주웠는지 모르니까.</summary>
    void DrawPickupToast(float w, float h)
    {
        const float showSeconds = 3f;
        float age = Time.time - ExhibitPickup.LastMessageTime;
        if (age > showSeconds || string.IsNullOrEmpty(ExhibitPickup.LastMessage)) return;

        var box = new Rect(w * 0.5f - 210, h * 0.16f, 420, 46);
        Panel(box);

        // 노란 띠 — 전광등과 같은 색이라 둘이 한 사건으로 읽힌다
        GUI.DrawTexture(new Rect(box.x, box.y, 5f, box.height), pickupTex);

        var style = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
        GUI.Label(box, ExhibitPickup.LastMessage, style);
    }

    void DrawCrosshair(float w, float h)
    {
        if (InKart) return;
        // 1인칭일 때 화면 중앙에 작은 점 — 어디를 보고 있는지 알기 쉬우라고
        GUI.DrawTexture(new Rect(w * 0.5f - 2f, h * 0.5f - 2f, 4f, 4f), woodLightTex);
    }

    void DrawFinish(float w, float h)
    {
        var box = new Rect(w * 0.5f - 170, h * 0.5f - 86, 340, 172);
        Panel(box);

        var center = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        GUI.Label(new Rect(box.x, box.y + 18, box.width, 34), "완주!", center);

        var sub = new GUIStyle(valueStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUI.Label(new Rect(box.x, box.y + 58, box.width, 24),
                  $"총 시간   {LapTracker.FormatTime(tracker.TotalTime)}", sub);
        GUI.Label(new Rect(box.x, box.y + 80, box.width, 24),
                  $"최고 랩   {LapTracker.FormatTime(tracker.BestLapTime)}", sub);

        // 선택 임무 — 1위는 보너스다. 못 해도 이야기는 진행된다.
        if (standings != null && standings.RacerCount > 1)
        {
            bool first = standings.PlayerFinishedFirst;
            var line = new GUIStyle(centerHint) { fontSize = 14 };
            line.normal.textColor = first ? new Color32(0xB0, 0x6A, 0x14, 0xFF) : InkSoft;
            GUI.Label(new Rect(box.x, box.y + 102, box.width, 20),
                      first ? "◆ 선택 임무 달성 — 1위로 완주" : "선택 임무 — 1위로 완주 (미달성)", line);
        }

        GUI.Label(new Rect(box.x, box.y + 136, box.width, 20), "ENTER 를 누르면 다시 시작", centerHint);
    }
}
