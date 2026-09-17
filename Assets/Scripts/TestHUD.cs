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
/// <b>좌표는 전부 1280×720 기준</b>이다. <see cref="Hud.Begin"/> 이 실제 해상도에 맞춰
/// 늘려주니까 여기서는 화면 크기를 신경 쓰지 마. 색·글꼴·판 그리기도 전부 Hud 에 있다.
/// 글씨를 넣을 때는 <see cref="Hud.Inner"/> 안쪽에만 — 판 테두리에 걸치면 잘려 보인다.
///
/// 개발용 정보(씬 이름, 진행 중인 장)는 debugKeys 가 켜져 있을 때만, 그것도 왼쪽 아래 구석에.
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
    public MiniMap map;
    public MissionManager mission;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    [Header("개발용")]
    [Tooltip("켜두면 왼쪽 아래에 씬 이름과 진행 중인 장이 보이고 F7/F8 로 장을 넘긴다. 제출 전에 꺼")]
    public bool debugKeys = true;

    bool showControls;
    bool confirmQuit;

    bool InKart => modeSwitcher != null && modeSwitcher.InKart;

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        // 화면 효과 끄기/켜기. 세기를 줄이는 것과 <b>끌 수 있는 것</b>은 다른 문제야 —
        // 멀미를 타면 아무리 연해도 거슬린다. 접근성 설정이라고 보는 게 맞다.
        if (k.vKey.wasPressedThisFrame)
        {
            ScreenEffects.Toggle();
            Toast.Show(ScreenEffects.On ? "화면 효과 켬" : "화면 효과 끔");
        }

        // ESC — 망한 판을 빠져나갈 길. <b>게임을 끄는 게 아니라 로비로</b> 간다.
        // 한 번에 나가면 잘 달리던 판을 실수로 날린다. 두 번 눌러야 나가고, 다른 키를 누르면 취소된다.
        if (k.escapeKey.wasPressedThisFrame)
        {
            if (confirmQuit) { SceneNavigator.LoadByIndex(0); return; }
            confirmQuit = true;
        }
        else if (confirmQuit && k.anyKey.wasPressedThisFrame)
        {
            confirmQuit = false;
        }

        // 완주했을 때뿐 아니라 <b>임무가 글러버린 순간부터</b> 다시 시작할 수 있다.
        // 실패한 줄 알면서 두 바퀴를 마저 도는 건 아무 의미가 없다.
        // 화면에는 "ENTER 닫음" 이라고 적혀 있고, 실제로 카드가 닫히면서 판이 처음으로 돌아간다.
        bool canRestart = tracker != null && (tracker.Finished || (mission != null && mission.Failed));
        if (canRestart && KartInput.RestartPressed)
        {
            tracker.ResetRace();
            KartInput.Clear();
        }

        if (!debugKeys) return;

        // 아직 임무 판정이 없어서 장이 저절로 안 넘어간다. 손으로 넘겨보는 용도.
        if (k.f7Key.wasPressedThisFrame) { StoryProgress.AdvanceChapter(); SceneNavigator.Reload(); }
        if (k.f8Key.wasPressedThisFrame) { StoryProgress.CurrentChapter = 1; SceneNavigator.Reload(); }
    }

    void OnGUI()
    {
        Rect screen = Hud.Begin(uiFont);
        float w = screen.width, h = screen.height;

        if (InKart)
        {
            DrawLapPanel();
            DrawCollectionPanel();
            DrawRankPanel(w);
            DrawSpeedPanel(w, h);
        }

        DrawToast(w, h);
        DrawCorner(h);
        if (InKart) DrawMiniMap(h);
        if (showControls) DrawControls(w, h);
        if (confirmQuit) DrawQuitAsk(w, h);
        if (InKart && tracker != null && tracker.Finished) DrawFinish(w, h);
        else if (InKart && mission != null && mission.Failed) DrawFailed(w, h);

        Hud.End();
    }

    // ---- 랩과 시간 ----
    void DrawLapPanel()
    {
        if (tracker == null) return;

        var p = new Rect(16f, 16f, 188f, 128f);
        Hud.Panel(p);

        float x = p.x + 14f;
        GUI.Label(new Rect(x, p.y + 12f, 100f, 18f), "랩", Hud.Label);
        GUI.Label(new Rect(x, p.y + 28f, 150f, 32f),
                  $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}", Hud.Value);

        Hud.Rule(x, p.y + 68f, p.width - 28f);

        // 라벨은 왼쪽 칸, 시간은 오른쪽 칸. 칸을 갈라 두면 시간이 길어져도 글자가 안 겹친다.
        var time = Hud.Resize(Hud.Value, 17, TextAnchor.MiddleRight);
        GUI.Label(new Rect(x, p.y + 74f, 44f, 20f), "현재", Hud.Label);
        GUI.Label(new Rect(p.x + 62f, p.y + 74f, p.width - 76f, 20f),
                  LapTracker.FormatTime(tracker.LapTime), time);

        GUI.Label(new Rect(x, p.y + 94f, 44f, 20f), "최고", Hud.Label);
        GUI.Label(new Rect(p.x + 62f, p.y + 94f, p.width - 76f, 20f),
                  LapTracker.FormatTime(tracker.BestLapTime), time);
    }

    // ---- 수집품 체크리스트 + 이번 판 임무 ----
    /// <summary>
    /// 장 이름("제1장 사라진 관람객")보다 <b>몇 개 모았는지</b>가 화면에 있어야 한다는
    /// 유저 판단(2026-09-16). 맞는 말이야 — 장 이름은 지금 뭘 해야 하는지를 안 알려준다.
    ///
    /// 여덟 칸이 전시실 진열장 여덟 개와 <b>같은 순서</b>다. 하나 주우면 그 자리가 채워지니까,
    /// 빈 칸을 보면 아직 뭐가 남았는지가 바로 읽힌다.
    /// </summary>
    void DrawCollectionPanel()
    {
        // 라벨과 값을 한 줄에 좌우로 놓으면 이름이 길 때 부딪힌다 — "무충돌로 시간 안에" 가
        // 라벨을 파고들어 "무충룰" 로 보였다(2026-09-16). 값은 아래 줄에 통째로 놓는다.
        var p = new Rect(16f, 152f, 208f, mission == null ? 78f : (mission.AllDone ? 100f : 222f));
        Hud.Panel(p);

        float x = p.x + 14f;
        int got = CollectionState.Count, total = ExhibitCatalogue.Count;

        GUI.Label(new Rect(x, p.y + 10f, 110f, 18f), "수집품", Hud.Label);
        GUI.Label(new Rect(p.x + 62f, p.y + 8f, p.width - 76f, 22f), $"{got} / {total}",
                  Hud.Resize(Hud.Value, 18, TextAnchor.MiddleRight));

        // 체크 칸 여덟 개
        const float box = 17f, gap = 4f;
        for (int i = 0; i < total; i++)
        {
            var cell = new Rect(x + i * (box + gap), p.y + 34f, box, box);
            bool has = CollectionState.Has(ExhibitCatalogue.All[i].id);

            GUI.DrawTexture(cell, Hud.WoodDarkTex);
            if (!has) continue;

            // 채운 칸 안에 체크 표시 — 색만 다르면 색약인 사람이 못 가린다
            GUI.DrawTexture(new Rect(cell.x + 1f, cell.y + 1f, box - 2f, box - 2f), Hud.BrassTex);
            GUI.Label(cell, "v", Hud.Resize(Hud.Title, 13));
        }

        if (mission == null) return;

        Hud.Rule(x, p.y + 60f, p.width - 28f);

        if (mission.AllDone)
        {
            GUI.Label(new Rect(x, p.y + 68f, p.width - 28f, 20f), RaceVoice.AllCollected(),
                      Hud.Resize(Hud.Value, 15));
            return;
        }

        // 이번 판에 뭐가 걸렸는지. "다음 건 어떻게 모으냐" 에 대한 답이 화면에 있어야 한다.
        float full = p.width - 28f;
        var tiny = Hud.Resize(Hud.Label, 12);

        GUI.Label(new Rect(x, p.y + 64f, full, 16f), "상품", tiny);
        GUI.Label(new Rect(x, p.y + 78f, full, 18f), mission.RewardName, Hud.Resize(Hud.Text, 14));

        GUI.Label(new Rect(x, p.y + 100f, full, 16f), "임무", tiny);
        // 이름이 길면 두 줄로 접는다. 접기가 없으면 라벨을 파고들어 "무충룰" 처럼 보인다.
        var wrap = Hud.Resize(Hud.Text, 14);
        wrap.wordWrap = true;
        GUI.Label(new Rect(x, p.y + 114f, full, 40f), mission.Title, wrap);

        // 진행도 한 줄 통째로. 임무 이름 옆에 붙이면 "무사고 + 시간" 처럼 둘 다 긴 경우 부딪힌다.
        var state = Hud.Resize(Hud.Value, 16);
        if (mission.Failed) state.normal.textColor = Hud.Ribbon;
        else if (mission.Cleared) state.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(x, p.y + 156f, full, 20f),
                  mission.Failed ? RaceVoice.Failed() : mission.Progress, state);

        // 왜 이걸 하는지. 조건만 적으면 조작 설명이 되고 박물관과 아무 상관이 없어진다.
        var why = Hud.Resize(Hud.Label, 11);
        why.wordWrap = true;
        GUI.Label(new Rect(x, p.y + 180f, full, 34f), RaceVoice.Why(mission.goal), why);
    }

    // ---- 순위 ----
    void DrawRankPanel(float w)
    {
        if (standings == null || standings.RacerCount <= 0) return;

        var p = new Rect(w - 120f, 16f, 104f, 88f);
        Hud.Panel(p);

        float x = p.x + 14f;
        GUI.Label(new Rect(x, p.y + 12f, 80f, 18f), "순위", Hud.Label);
        GUI.Label(new Rect(x, p.y + 30f, 76f, 34f),
                  RaceStandings.PlaceLabel(standings.PlayerPlace), Hud.Resize(Hud.Value, 28));
        GUI.Label(new Rect(x, p.y + 64f, 76f, 18f), $"{standings.RacerCount}대 중", Hud.Tiny);
    }

    // ---- 속도와 태엽 ----
    void DrawSpeedPanel(float w, float h)
    {
        if (kart == null) return;

        var p = new Rect(w - 206f, h - 124f, 190f, 108f);
        Hud.Panel(p);

        int kph = Mathf.Abs(Mathf.RoundToInt(kart.SpeedKph));
        GUI.Label(new Rect(p.x + 10f, p.y + 8f, 112f, 46f), kph.ToString(), Hud.Big);
        GUI.Label(new Rect(p.x + 128f, p.y + 28f, 50f, 20f), "km/h", Hud.Label);

        // 태엽 — 드리프트로 감고, 놓으면 풀리며 튀어나간다
        float charge01 = kart.boostChargeMax > 0f ? kart.BoostCharge / kart.boostChargeMax : 0f;
        float fill = kart.IsBoosting ? kart.BoostRemaining01 : charge01;
        bool hot = kart.IsBoosting || charge01 > 0.85f;

        // 돌아가는 태엽 열쇠. "태엽" 이라는 낱말을 몰라도 감기고 풀리는 게 눈에 보인다.
        WindKey(new Rect(p.x + 14f, p.y + 58f, 28f, 28f), fill * 1.5f,
                hot ? Hud.BrassTex : Hud.WoodDarkTex);

        var bar = new Rect(p.x + 50f, p.y + 60f, 126f, 11f);
        GUI.DrawTexture(bar, Hud.WoodDarkTex);
        if (fill > 0.01f)
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height),
                            hot ? Hud.BrassTex : Hud.PaperTex);

        var state = Hud.Resize(Hud.Label, 13);
        if (hot) state.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(bar.x, p.y + 75f, 126f, 18f),
                  kart.IsBoosting ? "풀린다!" : (kart.IsDrifting ? "감는 중" : "SHIFT 로 감기"), state);
    }

    /// <summary>
    /// 감기는 태엽 열쇠. 십자 막대를 돌려서 그린다 — 글자를 못 읽어도 상태가 보인다.
    /// turns 는 몇 바퀴 감겼는지. 0 이면 제자리, 1.5 면 한 바퀴 반.
    /// </summary>
    void WindKey(Rect r, float turns, Texture2D tex)
    {
        // 텍스처는 Hud 가 미리 만들어 둔 걸 받는다. OnGUI 는 매 프레임 도니까 여기서 만들면 계속 쌓인다.
        var pivot = r.center;
        var saved = GUI.matrix;   // Hud.Begin 이 넣어둔 화면 배율. 끝나고 이걸 그대로 되돌린다

        // 중심점은 <b>화면 좌표</b>로 넘겨야 한다. 여긴 1280x720 가상 좌표라 배율을 곱한다 —
        // 안 곱하면 회전 중심이 배율만큼 어긋나서 태엽이 화면 밖으로 날아간다.
        GUIUtility.RotateAroundPivot(turns * 360f, pivot * Hud.ScaleFactor);

        float arm = r.width * 0.5f;
        GUI.DrawTexture(new Rect(pivot.x - arm, pivot.y - 3f, r.width, 6f), tex);          // 가로 막대
        GUI.DrawTexture(new Rect(pivot.x - 3f, pivot.y - arm * 0.6f, 6f, arm * 1.2f), tex); // 세로 축

        GUI.matrix = saved;
    }

    // ---- 주운 물건 안내 ----
    void DrawToast(float w, float h)
    {
        if (!Toast.Visible) return;

        var box = new Rect(w * 0.5f - 200f, h * 0.16f, 400f, 56f);
        Hud.Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 16f), Hud.RibbonTex);
        GUI.Label(box, Toast.Message, Hud.Resize(Hud.Title, 17));
    }

    // ---- 왼쪽 아래 구석: 조작법 힌트 + 개발용 ----
    void DrawCorner(float h)
    {
        if (debugKeys)
        {
            // 가운데 위에 두면 랩 패널과 부딪힌다. 구석이 제자리야.
            var dev = new Rect(16f, h - 54f, 236f, 22f);
            Hud.Chip(dev);
            GUI.Label(new Rect(dev.x + 8f, dev.y + 4f, dev.width - 16f, 16f),
                      $"{SceneNavigator.CurrentSceneName} · {StoryProgress.CurrentName} · F7/F8", Hud.Tiny);
        }

        if (showControls) return;
        var chip = new Rect(16f, h - 28f, 88f, 22f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 9f, chip.y + 3f, 78f, 18f), "H  조작법", Hud.Resize(Hud.Text, 13));
    }

    void DrawControls(float w, float h)
    {
        var box = new Rect(w * 0.5f - 200f, h * 0.5f - 144f, 400f, 288f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 16f, box.width, 26f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 46f, box.width - 40f);

        string[,] rows =
        {
            { "화살표 · WASD", "운전" },
            { "SHIFT", "꾹 누르고 꺾으면 태엽이 감긴다" },
            { "SPACE", "톡 누르면 폴짝 (호핑)" },
            { "R", "제자리로 되돌리기" },
            { "ENTER", "이 판 다시 하기" },
            { "V", "화면 효과 끄기 / 켜기" },
            { "ESC", "두 번 누르면 로비로" },
            { "H", "이 창 닫기" },
        };

        var key = Hud.Resize(Hud.Text, 14);
        key.fontStyle = FontStyle.Bold;
        var desc = Hud.Resize(Hud.Label, 14);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = box.y + 56f + i * 26f;
            GUI.Label(new Rect(box.x + 24f, y, 120f, 22f), rows[i, 0], key);
            GUI.Label(new Rect(box.x + 150f, y, box.width - 172f, 22f), rows[i, 1], desc);
        }
    }


    // ---- ESC 로 그만두기 ----
    /// <summary>
    /// <b>게임을 끄지 않는다. 로비로 간다.</b> 빌드한 게임에서 ESC 가 곧장 종료되면
    /// 잘못 눌렀을 때 되돌릴 방법이 없다 — 로비로 보내면 언제든 다시 들어올 수 있어.
    ///
    /// 모은 수집품은 PlayerPrefs 에 이미 저장돼 있어서 나가도 안 날아간다.
    /// 다만 <b>진행 중이던 판은 처음부터</b>다 — 상품은 결승선을 넘어야 주니까.
    /// </summary>
    void DrawQuitAsk(float w, float h)
    {
        var box = new Rect(w * 0.5f - 150f, h * 0.5f - 58f, 300f, 116f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 18f, box.width, 28f), "그만둘까",
                  Hud.Resize(Hud.Title, 22));
        GUI.Label(new Rect(box.x, box.y + 50f, box.width, 20f), "모은 건 그대로 남는다",
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter));

        Hud.Rule(box.x + 24f, box.y + 78f, box.width - 48f);
        GUI.Label(new Rect(box.x, box.y + 84f, box.width, 22f), "ESC 로비로   ·   아무 키나 계속",
                  Hud.Resize(Hud.Text, 13, TextAnchor.MiddleCenter));
    }
    // ---- 임무 실패 (레이스 도중) ----
    /// <summary>
    /// 글러버린 순간 화면 가운데에 띄운다. 구석 패널에 "실패" 두 글자만 뜨면 못 본다 —
    /// 유저가 "안 뜨는 것 같다" 고 한 게 그거였어.
    /// <b>다시 해도 이미 모은 수집품은 그대로다.</b> 이 판만 다시 하는 거야.
    /// </summary>
    void DrawFailed(float w, float h)
    {
        var box = new Rect(w * 0.5f - 170f, h * 0.5f - 74f, 340f, 148f);
        Hud.Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 16f), Hud.RibbonTex);

        var head = Hud.Resize(Hud.Title, 26);
        head.normal.textColor = Hud.Ribbon;
        GUI.Label(new Rect(box.x, box.y + 18f, box.width, 32f), RaceVoice.Failed(), head);

        GUI.Label(new Rect(box.x, box.y + 54f, box.width, 22f), mission.FailReason,
                  Hud.Resize(Hud.Text, 15, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x, box.y + 78f, box.width, 20f), mission.Title,
                  Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter));

        Hud.Rule(box.x + 24f, box.y + 104f, box.width - 48f);
        GUI.Label(new Rect(box.x, box.y + 112f, box.width, 22f),
                  RaceVoice.RetryHint() + "   ·   " + RaceVoice.QuitHint(),
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));
    }

    // ---- 완주 ----
    /// <summary>
    /// 결과는 <b>숫자 셋</b>뿐이다 — 총 시간 · 최고 랩 · 순위. 유저(2026-09-17):
    /// *"자유주행 기록을 줄여봐 대신에 그냥 완주랑 총시간, 최고랩, 순위만 크게."*
    /// 맞는 판단이야. 결승선을 넘은 직후에 읽고 싶은 건 <b>내가 얼마나 잘했나</b>이고,
    /// 거기에 훈수가 붙으면 성적표가 아니라 잔소리가 된다.
    /// </summary>
    void DrawFinish(float w, float h)
    {
        var box = new Rect(w * 0.5f - 180f, h * 0.5f - 112f, 360f, 224f);
        Hud.Panel(box);

        // 다 모았으면 걸린 임무가 없다. 판정을 그대로 돌리면 "임무 실패 — 세 바퀴 완주" 가 뜬다.
        bool freeRun = mission != null && mission.AllDone;
        bool ok = mission == null || freeRun || mission.Cleared;

        var head = Hud.Resize(Hud.Title, 34);
        head.normal.textColor = ok ? Hud.Ink : Hud.Ribbon;
        GUI.Label(new Rect(box.x, box.y + 14f, box.width, 42f), ok ? "완주!" : RaceVoice.Failed(), head);

        Hud.Rule(box.x + 26f, box.y + 60f, box.width - 52f);

        // 셋을 나란히. 세로로 쌓으면 어느 게 중요한지 알 수가 없다.
        string[,] cells =
        {
            { "총 시간", LapTracker.FormatTime(tracker.TotalTime) },
            { "최고 랩", LapTracker.FormatTime(tracker.BestLapTime) },
            { "순위", standings != null && standings.PlayerPlace > 0
                      ? $"{standings.PlayerPlace} / {standings.RacerCount}" : "—" },
        };

        var label = Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter);
        var value = Hud.Resize(Hud.Value, 26, TextAnchor.MiddleCenter);
        float cell = (box.width - 40f) / 3f;

        for (int i = 0; i < 3; i++)
        {
            float x = box.x + 20f + i * cell;
            GUI.Label(new Rect(x, box.y + 74f, cell, 18f), cells[i, 0], label);
            GUI.Label(new Rect(x, box.y + 92f, cell, 34f), cells[i, 1], value);
        }

        // 임무 결과는 한 줄. 다 모았으면 걸린 게 없으니 아예 안 띄운다.
        if (mission != null && !freeRun)
        {
            var line = Hud.Resize(Hud.Label, 15, TextAnchor.MiddleCenter);
            line.normal.textColor = mission.Cleared ? Hud.Brass : Hud.Ribbon;
            GUI.Label(new Rect(box.x, box.y + 142f, box.width, 24f),
                      mission.Cleared ? $"◆ 임무 달성 — {mission.Title}"
                                      : $"임무 실패 — {mission.Title}", line);
        }

        GUI.Label(new Rect(box.x, box.y + 182f, box.width, 22f), "ENTER 를 누르면 다시 시작",
                  Hud.Resize(Hud.Label, 14, TextAnchor.MiddleCenter));
    }

    // ---- 왼쪽 아래 코스 지도 ----
    /// <summary>
    /// 3인칭 백뷰에는 <b>뒤를 볼 방법이 없다.</b> 추월당하는 걸 모르면 순위가 있어도 긴장이 안 생긴다.
    /// 백미러를 그리는 것보다 지도가 싸고 잘 읽힌다 — 코스 모양까지 같이 외워지니까.
    /// 코스 선은 <see cref="MiniMap"/> 이 텍스처로 한 번만 구워 둔다.
    /// </summary>
    void DrawMiniMap(float h)
    {
        if (map == null || map.Texture == null || standings == null) return;

        const float side = 132f;
        var box = new Rect(16f, h - side - 52f, side, side);
        Hud.Panel(box);

        var inner = Hud.Inner(box);
        GUI.DrawTexture(inner, map.Texture, ScaleMode.ScaleToFit);

        foreach (var racer in standings.racers)
        {
            if (racer == null) continue;

            Vector2 at = map.ToMap(racer.transform.position);
            if (at.x < 0f || at.x > 1f || at.y < 0f || at.y > 1f) continue;

            bool me = racer == standings.playerRacer;
            float dot = me ? 9f : 7f;

            // 지도는 y 가 위로 가는데 화면은 아래로 간다. 뒤집어야 코스 모양과 맞는다.
            var spot = new Rect(inner.x + at.x * inner.width - dot * 0.5f,
                                inner.y + (1f - at.y) * inner.height - dot * 0.5f, dot, dot);

            // 내 점만 테두리를 두른다. 색만으로는 네 개 중 어느 게 나인지 헷갈린다.
            if (me) GUI.DrawTexture(new Rect(spot.x - 2f, spot.y - 2f, dot + 4f, dot + 4f), Hud.WoodDarkTex);

            var skin = racer.GetComponent<KartSkin>();
            GUI.color = skin != null ? Cast.ColorOf(skin.CurrentCastId) : Color.white;
            GUI.DrawTexture(spot, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
