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

    /// <summary>
    /// 멈춘 걸 푼다. <b>timeScale 을 켜는 자리를 한 군데로 모은다</b> —
    /// 빠져나가는 길이 넷(ESC 로 로비 · R 로 다시 · 아무 키로 취소 · 컴포넌트가 꺼짐)이라
    /// 한 군데라도 빠뜨리면 <b>게임이 멈춘 채로 남는다.</b> 그건 버그 중에 제일 무섭다.
    /// </summary>
    void Resume()
    {
        confirmQuit = false;
        RacePause.Set(false);
    }

    // 씬을 옮기거나 이 HUD 가 꺼질 때도 반드시 푼다 — 로비로 나갔는데 로비가 얼어 있으면 안 된다.
    void OnDisable() { RacePause.Set(false); RacePause.Clear(); }

    bool InKart => modeSwitcher != null && modeSwitcher.InKart;

    void Update()
    {
        var k = Keyboard.current;

        // ★ 획득 연출이 떠 있는 동안에는 <b>다른 키가 하나도 안 먹는다.</b>
        //   여기서 ENTER 가 «다시 하기» 로 새면 연출을 보기도 전에 판이 다시 시작한다.
        if (ItemReveal.Open)
        {
            if (k != null && k.anyKey.wasPressedThisFrame) ItemReveal.Dismiss();
            KartInput.Clear();
            return;
        }

        if (k == null) return;

        // ★ 브리핑 카드가 떠 있으면 <b>그 카드만</b> 듣는다. 다른 키가 같이 먹으면
        // 카드를 닫으려다 이펙트가 꺼지거나 조작법이 열린다 —
        // "큰 패널은 한 번에 한 장" 을 입력 쪽에도 적용한 것(2026-09-18).
        if (RaceBriefing.Open)
        {
            if (k.anyKey.wasPressedThisFrame) RaceBriefing.Dismiss();
            return;
        }

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        // TAB 으로 내려서 걷기. <b>개발용이다</b> — 플레이어가 걸어다니는 건 캠퍼스 씬(F4)이고,
        // 레이스 도중에 내리는 건 이상하다는 유저 판단(2026-09-17). 점검할 때만 쓴다.
        if (debugKeys && k.tabKey.wasPressedThisFrame && modeSwitcher != null && modeSwitcher.HasKart)
        {
            modeSwitcher.Toggle();
            Toast.Show(modeSwitcher.InKart ? "카트에 탔다" : "내려서 걷는다   TAB 다시 타기");
        }

        // 화면 효과 끄기/켜기. 세기를 줄이는 것과 <b>끌 수 있는 것</b>은 다른 문제야 —
        // 멀미를 타면 아무리 연해도 거슬린다. 접근성 설정이라고 보는 게 맞다.
        if (k.vKey.wasPressedThisFrame)
        {
            ScreenEffects.Toggle();
            Toast.Show(ScreenEffects.On ? "이펙트 켬" : "이펙트 끔");
        }

        // ESC — 망한 판을 빠져나갈 길. <b>게임을 끄는 게 아니라 로비로</b> 간다.
        // 한 번에 나가면 잘 달리던 판을 실수로 날린다. 두 번 눌러야 나가고, 다른 키를 누르면 취소된다.
        //
        // ★ <b>진짜 일시정지다</b>(2026-09-18 유저: *"ESC 누르면 속력이 0으로 줄어드는데
        // 일시정지 같은 느낌이니까 속력 그대로, 시간도 계속 흐르게 두지 말고"*).
        // 패널을 띄워 놓고 게임이 계속 돌면 <b>고민하는 동안 판이 망가진다.</b>
        //
        // ★★ <c>Time.timeScale = 0</c> 으로 했다가 <b>카트가 트랙 밑으로 빠졌다</b>(유저 제보).
        // 이 카트는 레이캐스트 서스펜션이 매 FixedUpdate 마다 밀어 올려서 떠 있는 거라,
        // 물리를 세우면 <b>받쳐주던 힘도 같이 멈춘다.</b> <see cref="RacePause"/> 를 쓴다 —
        // 카트를 키네마틱으로 재워서 떨어질 수가 없게 하고, 풀 때 속도를 그대로 돌려준다.
        if (k.escapeKey.wasPressedThisFrame)
        {
            if (confirmQuit) { Resume(); SceneNavigator.LoadByIndex(0); return; }
            confirmQuit = true;
            RacePause.Set(true);
        }
        else if (confirmQuit && k.rKey.wasPressedThisFrame)
        {
            Resume();
            // 2026-09-17 유저: *"다른 애들이 뛰쳐나가서 기분이 안 좋은 사람들을 위해
            // 다시 1:3 레이싱을 하는 패널이 있으면 좋겠다."* 출발을 망쳤을 때
            // <b>로비를 거쳐 돌아오는 것 말고</b> 그 자리에서 다시 할 길이 필요하다.
            confirmQuit = false;
            if (tracker != null) tracker.ResetRace();
        }
        else if (confirmQuit && k.anyKey.wasPressedThisFrame)
        {
            Resume();
        }

        // 완주했을 때뿐 아니라 <b>임무가 글러버린 순간부터</b> 다시 시작할 수 있다.
        // 실패한 줄 알면서 두 바퀴를 마저 도는 건 아무 의미가 없다.
        // 화면에는 "ENTER 닫음" 이라고 적혀 있고, 실제로 카드가 닫히면서 판이 처음으로 돌아간다.
        bool canRestart = tracker != null && (tracker.Finished || (mission != null && mission.Failed));
        if (canRestart && KartInput.RestartPressed)
        {
            KartInput.Clear();

            // ★★ 2026-10-01 유저: *"미션 하나 깨고 로비로 안 돌아온다. 바로 다음 레이스
            //   카드가 나온다."* <b>맞다 — 돌아갈 길이 ESC 밖에 없었다.</b>
            //   임무를 깨서 <b>새 이야기가 풀렸는데</b> ENTER 가 다음 판을 시작해 버리면,
            //   이야기는 영영 안 보고 레이스만 아홉 판 하게 된다.
            //   이야기가 기다리고 있으면 ENTER 는 <b>중앙홀로</b> 간다.
            if (StoryWaiting()) { SceneNavigator.LoadByIndex(0); return; }

            recordSent = false;
            recordRank = 0;
            tracker.ResetRace();
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

        if (InKart) DrawCountdown(w, h);
        DrawToast(w, h);
        DrawCorner(h);
        if (InKart) DrawMiniMap(w, h);
        if (showControls) DrawControls(w, h);
        // ESC 를 누르면 <b>그 패널만</b> 보여준다. 실패·완주 패널이 뒤에 그대로 있으면
        // 두 장이 겹쳐서 어느 쪽 글씨인지 알 수가 없다(2026-09-18 유저 제보).
        // 브리핑이 제일 앞이다 — 이게 떠 있는 동안은 아직 아무 판도 시작 안 했다.
        if (RaceBriefing.Open) DrawBriefing(w, h);
        // ★ 획득 연출이 완주 패널보다 앞이다 — 결승선을 넘은 그 순간에 뜨니까,
        //   뒤에 성적표가 같이 보이면 «찾았다» 가 «몇 등이더라» 에 묻힌다.
        else if (ItemReveal.Open) DrawReveal(w, h);
        else if (confirmQuit) DrawQuitAsk(w, h);
        else if (InKart && tracker != null && tracker.Finished) DrawFinish(w, h);
        else if (InKart && mission != null && mission.Failed) DrawFailed(w, h);

        Hud.End();
    }

    /// <summary>
    /// <b>증거 하나를 찾았다</b> — 암전 → 전시실 → 빛줄기(<see cref="ItemReveal"/>).
    ///
    /// ★ 번쩍이는 연출을 <b>걷어냈다</b>(2026-10-02). 결승선 앞에서 빛이 터지는 건
    /// «여기서 뭔가 일어났다» 지만, 수집품은 트랙이 아니라 <b>전시실에 쌓이는 것</b>이다.
    /// 짧은 암전으로 <b>장소가 바뀐다</b>는 신호를 주고, 그 물건이 들어간 자리를 보여준다.
    ///
    /// 빛줄기는 <b>위에서 아래로 내려오는 사다리꼴</b>이다. 가로로 퍼지는 광선은
    /// «폭발» 이지만, 세로로 내려오는 빛은 <b>«저기로 쏟아진다»</b> 가 된다.
    /// 먼지 알갱이가 그 안에서 천천히 떠다니면 빛에 <b>부피</b>가 생긴다.
    /// </summary>
    void DrawReveal(float w, float h)
    {
        float dark = ItemReveal.Dark;
        float stage = ItemReveal.Stage;
        float shaft = ItemReveal.Shaft;
        float settle = ItemReveal.Settle;

        // 1) 암전 — 달리던 화면을 0.2초에 덮는다
        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.02f, 0.02f, 0.03f, dark));
        if (stage <= 0.001f) return;

        // 2) 전시실. 그림이 없으면 짙은 남색 방으로 대신한다 —
        //    연출이 통째로 사라지면 «고장» 으로 보인다.
        var art = ItemReveal.Backdrop;
        var keepColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, stage);
        if (art != null)
            GUI.DrawTexture(new Rect(0f, 0f, w, h), art, ScaleMode.ScaleAndCrop);
        else
            Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.09f, 0.11f, 0.17f, 1f));
        GUI.color = keepColor;

        // 전시실은 어두운 방이다 — 빛줄기가 살려면 주변이 눌려 있어야 한다
        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.03f, 0.03f, 0.05f, 0.42f * stage));

        float cx = w * ItemReveal.BeamX;
        float cy = h * ItemReveal.BeamY;
        if (shaft <= 0.001f) return;

        // 3) 빛줄기 — 천장에서 진열장까지, 아래로 갈수록 넓어지는 사다리꼴.
        //    가로 띠를 쌓아 만든다. 세로로 기울어진 사각형을 IMGUI 로는 못 그리니까.
        const int bands = 34;
        float reach = cy * Mathf.SmoothStep(0f, 1f, shaft);
        for (int i = 0; i < bands; i++)
        {
            float t = (i + 0.5f) / bands;
            float y = t * reach;
            if (y > cy) break;

            float half = Mathf.Lerp(w * 0.028f, w * 0.085f, t);
            float a = Mathf.Lerp(0.30f, 0.07f, t) * shaft;
            Hud.Fill(new Rect(cx - half, y, half * 2f, reach / bands + 1f),
                     new Color(1f, 0.97f, 0.86f, a));
        }

        // 4) 바닥에 고이는 빛 — 줄기만 있으면 «어디에» 쏟아지는지 안 보인다
        for (int i = 5; i >= 1; i--)
        {
            float r = w * 0.035f * i * Mathf.SmoothStep(0f, 1f, shaft);
            Hud.Fill(new Rect(cx - r, cy - r * 0.26f, r * 2f, r * 0.52f),
                     new Color(1f, 0.96f, 0.84f, 0.09f * shaft));
        }

        // 5) 먼지 — 빛 안에서만 보인다. 이게 있어야 빛에 부피가 생긴다
        for (int i = 0; i < 18; i++)
        {
            float seed = i * 37.7f;
            float t = Mathf.Repeat(ItemReveal.Elapsed * 0.09f + i * 0.137f, 1f);
            float y = t * cy;
            if (y > reach) continue;

            float spread = Mathf.Lerp(w * 0.024f, w * 0.075f, t);
            float x = cx + Mathf.Sin(seed + ItemReveal.Elapsed * 0.5f) * spread;
            float s = 1.6f + Mathf.Sin(seed * 1.7f) * 0.8f;
            Hud.Fill(new Rect(x, y, s, s), new Color(1f, 0.98f, 0.9f, 0.45f * shaft));
        }

        if (settle <= 0.001f) return;

        // 6) 이름 — 빛 속에 선다. 판을 깔지 않는다. 나무 판을 깔면 그 순간
        //    «UI 가 떴다» 가 되고, 지금은 <b>장면</b>을 보여주는 중이다.
        var fade = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, settle);
        float lift = (1f - settle) * 18f;

        Glow(new Rect(0f, cy + 28f + lift, w, 24f), "증거를 찾았다",
             Hud.Resize(Hud.Label, 15, TextAnchor.MiddleCenter), new Color(1f, 0.93f, 0.78f));

        Glow(new Rect(0f, cy + 52f + lift, w, 44f), ItemReveal.ItemName,
             Hud.Resize(Hud.Title, 32, TextAnchor.MiddleCenter), Color.white);

        Glow(new Rect(0f, cy + 98f + lift, w, 24f),
             $"전시실 {ItemReveal.CaseNumber}번   ·   수집품 {ItemReveal.Have} / {ItemReveal.Total}",
             Hud.Resize(Hud.Text, 16, TextAnchor.MiddleCenter), Hud.Brass);

        if (ItemReveal.CanSkip)
            Glow(new Rect(0f, h - 46f, w, 20f), "아무 키나 누르면 넘어간다",
                 Hud.Resize(Hud.Tiny, 12, TextAnchor.MiddleCenter), new Color(0.82f, 0.78f, 0.70f));

        GUI.color = fade;
    }

    /// <summary>
    /// 그림 위에 글을 얹을 때. <b>판 없이 쓰려면 글자에 테두리가 있어야 한다</b> —
    /// 밝은 데 밝은 글씨가 얹히면 그 부분만 사라진다. 나무 판을 깔면 읽히긴 하지만
    /// 그 순간 «UI 가 떴다» 가 되고, 지금은 <b>장면</b>을 보여주는 중이다.
    ///
    /// IMGUI 에 글자 그림자가 없어서 <b>여덟 방향으로 어둡게 깔고 그 위에</b> 그린다.
    /// 네 방향만 깔면 대각선에서 테가 끊긴다.
    /// </summary>
    static void Glow(Rect r, string text, GUIStyle style, Color colour)
    {
        var keep = style.normal.textColor;
        style.normal.textColor = new Color(0.03f, 0.03f, 0.05f, 0.75f * GUI.color.a);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                GUI.Label(new Rect(r.x + dx * 1.6f, r.y + dy * 1.6f, r.width, r.height), text, style);
            }

        style.normal.textColor = colour;
        GUI.Label(r, text, style);
        style.normal.textColor = keep;
    }

    /// <summary>
    /// <b>출발 전 브리핑.</b> 이 판이 몇 번째이고, 무엇을 하고, <b>왜</b> 하는지.
    ///
    /// 레이스 중 HUD 에서 "왜" 를 빼 놓은 이유가 있다 — 힐끗 보는 계기판에 설명이 끼면
    /// 읽는 데 시간이 걸린다(2026-09-17). 그 문장들이 사라진 게 아니라 <b>여기가 제자리</b>야:
    /// <b>달리는 중에는 계기판, 출발 전에는 이야기.</b>
    ///
    /// 유저가 <see cref="StoryScript"/> 에 대사를 쓰면 아래 한 줄이 저절로 붙는다.
    /// 지금은 비어 있고, <b>비어 있으면 그 줄을 아예 안 그린다</b> — 빈칸이 남으면
    /// 카드가 미완성으로 보인다.
    /// </summary>
    void DrawBriefing(float w, float h)
    {
        var card = new Rect(w * 0.5f - 270f, h * 0.5f - 160f, 540f, 320f);
        Hud.Panel(card);
        Rect inner = Hud.Inner(card);

        // 몇 번째 판인가. <b>끝이 있는 여정</b>으로 읽혀야 다음 판을 돌 이유가 생긴다.
        GUI.Label(new Rect(inner.x + 10f, inner.y + 4f, 200f, 24f),
                  RaceBriefing.Stage, Hud.Resize(Hud.Title, 17, TextAnchor.MiddleLeft));
        GUI.Label(new Rect(inner.xMax - 210f, inner.y + 6f, 200f, 22f),
                  RaceBriefing.Progress, Hud.Resize(Hud.Label, 13, TextAnchor.MiddleRight));

        Hud.Rule(inner.x + 10f, inner.y + 32f, inner.width - 20f);

        // 임무 이름 — 두 줄까지 접힌다. "담장 안 긁고 시간 안에 완주하기" 가 제일 길다.
        var title = Hud.Resize(Hud.Title, 26, TextAnchor.UpperCenter);
        title.wordWrap = true;
        GUI.Label(new Rect(inner.x + 14f, inner.y + 46f, inner.width - 28f, 70f),
                  RaceBriefing.TitleFor(mission), title);

        // 왜 하는가. 이 한 줄이 아홉 판을 아홉 장면으로 만든다.
        var why = Hud.Resize(Hud.Text, 16, TextAnchor.UpperCenter);
        why.wordWrap = true;
        why.normal.textColor = Hud.InkSoft;
        GUI.Label(new Rect(inner.x + 20f, inner.y + 118f, inner.width - 40f, 46f),
                  RaceBriefing.Why, why);

        Hud.Rule(inner.x + 10f, inner.y + 166f, inner.width - 20f);

        // 상품(결승은 «걸린 것»). 값은 라벨 <b>아래 줄 통째로</b> — 이름이 길어져도 안 부딪힌다.
        string stake = RaceBriefing.Stake;
        if (!string.IsNullOrEmpty(stake))
        {
            GUI.Label(new Rect(inner.x + 14f, inner.y + 178f, 200f, 20f),
                      RaceBriefing.StakeLabel, Hud.Resize(Hud.Label, 13, TextAnchor.UpperLeft));

            var value = Hud.Resize(Hud.Text, 17, TextAnchor.UpperLeft);
            value.normal.textColor = Hud.Ink;
            GUI.Label(new Rect(inner.x + 14f, inner.y + 197f, inner.width - 28f, 24f), stake, value);
        }

        // 대사가 들어오면 여기. 없으면 아무 것도 안 그린다.
        string line = RaceBriefing.Line;
        if (!string.IsNullOrEmpty(line))
        {
            var say = Hud.Resize(Hud.Text, 15, TextAnchor.UpperLeft);
            say.wordWrap = true;
            say.normal.textColor = Hud.InkSoft;
            GUI.Label(new Rect(inner.x + 14f, inner.y + 228f, inner.width - 28f, 44f), line, say);
        }

        // ★ "아무 키" 는 게임 밖 말투다 — 실제로 누를 키를 적는다(2026-09-18).
        // 동작은 그대로 아무 키나 먹고, <b>적어 두는 것만</b> 하나로 골랐다.
        var go = Hud.Resize(Hud.Text, 15, TextAnchor.MiddleCenter);
        go.normal.textColor = Hud.InkSoft;
        GUI.Label(new Rect(inner.x, inner.yMax - 30f, inner.width, 24f), "SPACE   출발", go);
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

    /// <summary>수집품 패널이 어디서 끝나는지. 지도가 이 아래로 간다.</summary>
    float CollectionBottom()
    {
        if (!InKart) return 152f;
        if (mission != null && mission.AllDone) return 152f;
        return 152f + (mission == null ? 78f : 184f);
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
        // 여덟 개를 다 모으면 <b>패널을 아예 안 띄운다.</b> 걸린 임무가 없는데 체크 칸이
        // 남아 있으면 "아직 뭘 더 해야 하나" 로 읽힌다 — 그때는 그냥 달리는 판이야.
        if (mission != null && mission.AllDone) return;

        var p = new Rect(16f, 152f, 208f, mission == null ? 78f : 184f);
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

        // 설명 줄은 뺐다(2026-09-17 유저: "마지막 실사다 문구는 뭐야, 없애줘").
        // 레이스 중에 힐끗 보는 화면이라 <b>지금 뭘 해야 하는지</b>면 충분하고,
        // 왜 하는지는 이야기가 할 일이다. RaceVoice.Why 는 남겨둔다 — 브리핑에 쓸 자리야.
    }

    // ---- 순위 ----
    void DrawRankPanel(float w)
    {
        // ★ 혼자 달리는 판에서는 <b>아예 안 그린다.</b> «1대 중 1위» 는 아무 말도 안 하는데,
        //   패널은 y 16~104 를 차지해서 바로 밑의 코스 지도와 <b>겹쳤다</b>
        //   (2026-10-01 유저: "지도랑 뭐 하나 겹쳐져 있어"). 지도가 나중에 그려져서
        //   위 10px 만 삐져나와 있었고, 그게 «판이 두 장 겹친» 것으로 보였다.
        //   임무 1~8 은 전부 혼자 달리니 이 패널은 결승·자유 주행에서만 뜬다.
        if (standings == null || standings.RacerCount <= 1) return;

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
                  kart.IsBoosting ? "태엽 작동!" : (kart.IsDrifting ? "감는 중" : "SHIFT 로 태엽 감기"), state);
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

    // ---- 3 · 2 · 1 · 출발! ----
    /// <summary>
    /// 화면 <b>한가운데</b>에 크게. 구석에 작게 띄우면 출발선을 보고 있는 동안 못 본다.
    /// 숫자가 <b>커졌다 작아지는</b> 게 중요해 — 3 에서 2 로 바뀌는 걸 놓치면
    /// 카운트가 있으나 마나다.
    /// </summary>
    void DrawCountdown(float w, float h)
    {
        string label = RaceCountdown.Label;
        if (string.IsNullOrEmpty(label)) return;

        bool go = label == "출발!";

        // ★ 2026-09-18 유저(두 번째): *"임무가 하나 추가될 때마다 321 패널 뒤에 있어서
        // 잘 안 보여."* 자리를 옮겨 봤자 <b>판이 있는 한</b> 뭔가는 가린다 —
        // 임무 칸은 판이 길어지면 아래로 자라고, 카운트는 세로 가운데에 있으니까.
        //
        // <b>판을 없앤다.</b> 카운트는 3초짜리고 그동안 카트가 아예 안 움직이니
        // 화면 한가운데를 통째로 써도 아무 손해가 없다. 대신 종이 판이 없으면 밝은 노면에서
        // 묻히니까 <b>글자 뒤에 그림자를 깔아</b> 읽히게 한다 — 판보다 싸고 아무것도 안 가린다.
        var style = Hud.Resize(Hud.Title, Mathf.RoundToInt((go ? 64 : 120) * RaceCountdown.Pop),
                               TextAnchor.MiddleCenter);

        var area = new Rect(0f, h * 0.5f - 80f, w, 160f);

        // 그림자 넉 장 — 한 장만 깔면 한쪽만 읽히고, 넉 장이면 어느 배경에서도 테두리가 생긴다
        var shade = Hud.Resize(Hud.Title, style.fontSize, TextAnchor.MiddleCenter);
        shade.normal.textColor = new Color(0.12f, 0.09f, 0.07f, 0.75f);
        for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -1; dy <= 1; dy += 2)
                GUI.Label(new Rect(area.x + dx * 3f, area.y + dy * 3f, area.width, area.height), label, shade);

        style.normal.textColor = go ? Hud.Brass : new Color(0.98f, 0.95f, 0.88f);
        GUI.Label(area, label, style);
    }

    // ---- 주운 물건 안내 ----
    void DrawToast(float w, float h)
    {
        if (!Toast.Visible) return;

        // 2026-09-18 유저: *"실패 패널 뜨고 바로 위에 '세 번 실패' 패널이 겹쳐서 뜬다."*
        // 실패 사유는 <b>가운데 큰 패널에 이미 적혀 있다.</b> 같은 말을 두 군데에 띄우면
        // 겹칠 뿐 아니라 어느 쪽을 읽어야 할지도 모른다. 큰 패널이 뜨면 알림 줄은 접는다.
        bool bigPanel = InKart && (confirmQuit
                                || (tracker != null && tracker.Finished)
                                || (mission != null && mission.Failed));
        if (bigPanel) return;

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
        var box = new Rect(w * 0.5f - 200f, h * 0.5f - 157f, 400f, 314f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 16f, box.width, 26f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 46f, box.width - 40f);

        string[,] rows =
        {
            { "화살표 · WASD", "운전" },
            { "SHIFT", "꺾으면서 꾹 — 놓으면 태엽 작동" },
            { "SPACE", "톡 누르면 폴짝 (호핑)" },
            { "R", "제자리로 되돌리기" },
            { "ENTER", "이 판 다시 하기" },
            { "TAB", "내려서 걷기 (개발용)" },
            { "V", "이펙트 끄기 / 켜기" },
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
        // 2026-09-18 유저가 종이에 그려서 준 모양 그대로 — <b>제목 한 줄, 그 아래 고를 것 셋.</b>
        // 전에는 셋을 한 줄에 가운뎃점으로 이어 붙였는데, 그러면 <b>고르는 화면이 아니라
        // 안내문</b>으로 읽힌다. 줄을 나누고 키를 왼쪽에 세로로 맞추면 메뉴가 된다.
        var box = new Rect(w * 0.5f - 170f, h * 0.5f - 92f, 340f, 184f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 16f, box.width, 32f), "나가기",
                  Hud.Resize(Hud.Title, 24));

        Hud.Rule(box.x + 26f, box.y + 54f, box.width - 52f);

        // 키는 왼쪽 칸, 뜻은 오른쪽 칸. 칸을 갈라 두면 글자가 길어져도 안 부딪힌다.
        var keyStyle = Hud.Resize(Hud.Value, 17, TextAnchor.MiddleLeft);
        var whatStyle = Hud.Resize(Hud.Text, 15, TextAnchor.MiddleLeft);

        string[,] rows =
        {
            { "R", "이 판 다시 하기" },
            { "ESC", "로비로" },
            // "아무 키" 는 게임 밖 말투다. 실제로 누를 키를 적는다 —
            // 유저: *"아무키 대신 플레이어가 알아들을 수 있는 글씨 써도 좋고."*
            { "SPACE", "계속 달리기" },
        };

        for (int i = 0; i < 3; i++)
        {
            float y = box.y + 68f + i * 34f;
            GUI.Label(new Rect(box.x + 40f, y, 64f, 24f), rows[i, 0], keyStyle);
            GUI.Label(new Rect(box.x + 112f, y, box.width - 140f, 24f), rows[i, 1], whatStyle);
        }
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
        // 셋을 가로로 놓으면 칸 하나가 (폭−40)/3 밖에 안 된다. 360 일 때 107px 인데
        // "1:51.17" 이 26px 로 그리면 100px 이라 옆 칸과 딱 붙는다(2026-09-17 유저 제보).
        // 패널을 넓히고 숫자를 줄였다 — 둘 다 해야 여유가 생긴다.
        // 캐릭터마다 한 줄이라 최대 네 줄 — 세 줄 시절(292)보다 한 줄(19px) 더 필요하다.
        var box = new Rect(w * 0.5f - 208f, h * 0.5f - 156f, 416f, 311f);
        Hud.Panel(box);

        // 결승선을 넘은 <b>그 판에 한 번만</b> 기록을 낸다. OnGUI 는 매 프레임 도니까
        // 여기서 바로 부르면 같은 기록이 수십 번 쌓인다.
        if (!recordSent)
        {
            recordSent = true;
            recordRank = RaceRecords.Submit(tracker.TotalTime, tracker.BestLapTime,
                                            standings != null ? standings.PlayerPlace : 1,
                                            standings != null ? standings.RacerCount : 1,
                                            GameSelection.SelectedCastId);
        }

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
        var value = Hud.Resize(Hud.Value, 21, TextAnchor.MiddleCenter);
        float cell = (box.width - 48f) / 3f;

        for (int i = 0; i < 3; i++)
        {
            float x = box.x + 24f + i * cell;
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

        DrawRecords(box);

        bool story = StoryWaiting();
        var foot = Hud.Resize(Hud.Label, 14, TextAnchor.MiddleCenter);
        if (story) foot.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(box.x, box.y + box.height - 30f, box.width, 22f),
                  story ? "ENTER — 중앙홀로   ·   새 이야기가 기다린다" : "ENTER 를 누르면 다시 시작",
                  foot);
    }

    /// <summary>
    /// 아직 안 본 이야기가 있나. 있으면 완주 뒤 ENTER 가 <b>다시 하기</b>가 아니라
    /// <b>중앙홀로</b>가 된다 — 이야기는 로비 안에서 도니까(StoryStage).
    /// </summary>
    static bool StoryWaiting()
    {
        string id = StoryScript.CurrentScene();
        return !string.IsNullOrEmpty(id) && !StoryProgress.HasSeen(id);
    }

    bool recordSent;
    int recordRank;

    /// <summary>
    /// <b>잘 달린 기록 세 줄.</b> 2026-09-18 유저: *"전에 한 것보다 이번이 더 잘 나왔네?
    /// 하고 느낄 수 있게끔."*
    ///
    /// 이번 판 숫자만 보여주면 <b>잘한 건지 못한 건지 알 방법이 없다.</b> 기준이 없으면
    /// 두 번째 판을 돌 이유도 없어 — 레이싱에서 다시 달리게 만드는 건 상대가 아니라
    /// <b>어제의 나</b>다. 이번 기록이 표에 올랐으면 그 줄을 금색으로 짚어준다.
    /// </summary>
    void DrawRecords(Rect box)
    {
        // ★ <b>줄마다 다른 사람</b>이어야 비교가 된다. 전체 상위 셋을 뽑으면 한 번 잘 달린
        // 캐릭터가 세 줄을 다 먹어서, 다른 카트로 달린 사람은 자기 이름을 못 본다.
        var best = RaceRecords.BestPerCast();
        if (best.Count == 0) return;

        float y = box.y + 168f;
        Hud.Rule(box.x + 26f, y - 8f, box.width - 52f);

        GUI.Label(new Rect(box.x, y, box.width, 18f),
                  recordRank > 0 ? $"기록 경신 — {recordRank}위!" : "잘 달린 기록",
                  Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter));

        var line = Hud.Resize(Hud.Text, 13, TextAnchor.MiddleLeft);
        var mine = Hud.Resize(Hud.Value, 13, TextAnchor.MiddleLeft);
        mine.normal.textColor = Hud.Brass;

        for (int i = 0; i < best.Count; i++)
        {
            var r = best[i];
            // 줄이 캐릭터마다 하나라 «몇 위 줄» 로는 못 짚는다 — <b>내가 고른 캐릭터</b>의 줄을 짚는다.
            bool isMine = !string.IsNullOrEmpty(r.castId)
                          && r.castId == GameSelection.SelectedCastId;
            float row = y + 20f + i * 19f;

            // ★ <b>내 줄에는 캐릭터 이름을 안 쓴다</b>(2026-09-18 유저 판단).
            // 남의 기록은 «누구의 기록인지» 가 필요하니까 이름을 쓰고, 내 줄은 «나» 다.
            // 유저 걱정: *"이름 정해진 캐릭터로 플레이하면 걔가 된 것 같고 기분 나쁠까."*
            // 기록표는 <b>카트를 가리키는 표</b>지 플레이어를 부르는 자리가 아니라서,
            // 내 줄만 «나» 로 두면 비교 기능은 그대로 살고 그 껄끄러움만 사라진다.
            string who = isMine ? "나" : Cast.NameOf(r.castId);
            string place = r.racers > 1 ? $"{r.place}위 / {r.racers}대" : "혼자";

            GUI.Label(new Rect(box.x + 34f, row, 26f, 18f), $"{i + 1}", isMine ? mine : line);
            GUI.Label(new Rect(box.x + 60f, row, 96f, 18f),
                      LapTracker.FormatTime(r.total), isMine ? mine : line);
            GUI.Label(new Rect(box.x + 162f, row, 96f, 18f),
                      $"랩 {LapTracker.FormatTime(r.bestLap)}", isMine ? mine : line);
            GUI.Label(new Rect(box.x + 262f, row, 130f, 18f),
                      string.IsNullOrEmpty(who) ? place : $"{who} · {place}", isMine ? mine : line);
        }
    }

    // ---- 왼쪽 아래 코스 지도 ----
    /// <summary>
    /// 3인칭 백뷰에는 <b>뒤를 볼 방법이 없다.</b> 추월당하는 걸 모르면 순위가 있어도 긴장이 안 생긴다.
    /// 백미러를 그리는 것보다 지도가 싸고 잘 읽힌다 — 코스 모양까지 같이 외워지니까.
    /// 코스 선은 <see cref="MiniMap"/> 이 텍스처로 한 번만 구워 둔다.
    /// </summary>
    void DrawMiniMap(float w, float h)
    {
        if (map == null || map.Texture == null || standings == null) return;

        // ★★ 2026-09-22 유저: *"개발업자랑 정치인이랑 대결하고 끝나서 승리 확인 이후에
        // 지도가 작게 표시된다. 이럴 거면 처음부터 넣든가."* <b>맞는 지적이고 내 버그다.</b>
        //
        // 지도를 <b>왼쪽 열</b>에 두고 «수집품 패널 아래부터 남는 만큼» 으로 잡았는데,
        // 그 패널이 세로로 길어서 남는 자리가 늘 70px 미만이었다 — 그래서 <b>임무 판 내내
        // 접혀 있었다.</b> 결승을 이기면 수집품 패널이 사라지니까(«자유 주행에는 안 띄운다»)
        // 그때야 자리가 생겨 나타난 것이다.
        //
        // <b>오른쪽 열은 통째로 비어 있다</b> — 위에 순위판(y 16~104), 아래에 속도계
        // (y h−124~h−16), 그 사이가 전부 빈칸이다. 거기로 옮기면 수집품 패널과 <b>영영
        // 안 만난다.</b> 자리를 다투게 두지 말고 <b>다른 열로 보내는 것</b>이 답이었다.
        float top = (standings.RacerCount > 1 ? 104f : 16f) + 10f;
        float side = Mathf.Min(132f, h - 134f - top);
        if (side < 70f) return;

        var box = new Rect(w - 16f - side, top, side, side);
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
