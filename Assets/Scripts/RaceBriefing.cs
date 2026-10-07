using UnityEngine;

/// <summary>
/// <b>출발 전 브리핑.</b> 이 판이 몇 번째이고, 무엇을 해야 하고, <b>왜</b> 하는지가
/// 카운트다운 앞에 한 장 뜬다.
///
/// 2026-09-21 유저: *"스토리 봐도 레이싱이 빨리 끝날 것 같은데 미니게임을 더 만들까."*
/// 재보니 분량은 이미 기획서 범위 안이었다(9판 × 1:27 ≈ 13분). 비어 있던 건 길이가 아니라
/// <b>판과 판 사이</b>다 — 아홉 판이 전부 같은 코스인데 그 사이에 아무도 아무 말을 안 하니까
/// "짧다" 가 아니라 "밋밋하다" 로 느껴진다. 미니게임을 더 만들어도 그건 안 메워진다.
///
/// 재료는 이미 다 있었다. <see cref="RaceVoice.Why"/> 는 2026-09-17 에 써 놓고
/// HUD 가 좁아서 뺀 문구라 여덟 개가 그대로 살아 있다. 여기가 그 문장들이 원래 있어야 할
/// 자리야 — <b>레이스 중에는 계기판, 출발 전에는 이야기.</b>
///
/// <b>MonoBehaviour 가 아니다</b> — <see cref="RaceCountdown"/> 과 같은 꼴이라
/// 씬에 올릴 것도 저장할 것도 없고, 씬을 다시 굽지 않아도 들어온다.
/// </summary>
public static class RaceBriefing
{
    /// <summary>
    /// ★ <b>안전장치.</b> 카드를 닫는 건 <see cref="TestHUD"/> 하나뿐이라, 그 컴포넌트가
    /// 없는 낡은 씬에서는 <b>아무도 닫아주지 않아 카트가 영영 묶인다.</b>
    /// 이 프로젝트에서 "새 컴포넌트에 기대면 옛 씬에서 안 먹는다" 를 네 번 겪었고,
    /// 이번 건 안 먹는 정도가 아니라 <b>게임이 멈춘다.</b>
    ///
    /// 그래서 시간이 지나면 저절로 풀린다. 풀리면 카운트다운 없이 바로 출발하는데,
    /// 그건 <b>안 움직이는 것보다 낫다</b>(문에 바닥값을 준 것과 같은 판단).
    /// </summary>
    /// <summary>
    /// ★ 2026-10-06 25초에서 <b>14초로</b> 내렸다. 이건 «안 누르면» 의 안전장치지
    /// 읽는 시간이 아니다 — 카드 한 장에 40자다. 25초를 기다리는 건 고장으로 읽힌다.
    /// </summary>
    const float Timeout = 14f;

    static bool open;
    static float openedAt = -99f;

    /// <summary>카드가 떠 있나. 이 동안에는 카트도 시계도 멈춘다.</summary>
    // ★ 전부 <b>unscaled</b> — 이건 UI 지 게임플레이가 아니고, timeScale 에 묶이면 영원히 안 끝난다
    public static bool Open => open && Time.unscaledTime - openedAt < Timeout;

    /// <summary>
    /// 마지막으로 보여준 임무. <b>같은 판을 다시 할 때는 안 띄운다</b> —
    /// 실패하고 ENTER 로 다시 하는 게 이 게임에서 제일 흔한 동작인데, 그때마다 읽은 카드가
    /// 또 뜨면 그건 안내가 아니라 장애물이 된다.
    /// </summary>
    static int lastShown = -999;

    /// <summary>
    /// <b>이번 입장이 «처음 보는 판» 인가.</b> 카운트다운 중의 티키타카가 이걸 본다 —
    /// ENTER 로 다시 할 때마다 같은 대사가 또 나오면 <b>안내가 아니라 장애물</b>이 된다
    /// (브리핑 카드를 다시 안 띄우는 것과 같은 이유).
    /// </summary>
    public static bool Fresh { get; private set; }

    // ── 티키타카 ───────────────────────────────────────────────────────
    /// <summary>한 줄이 떠 있는 시간. 24자를 읽는 데 그 정도면 넉넉하다.</summary>
    /// <summary>
    /// ★★ 2026-10-06 <b>12초로 뒀다가 그게 곧 «먹통» 이었다.</b>
    /// 플레이 모드로 재현해 보니 두 줄 × 12초 + 카운트다운 3초 = <b>최대 27초</b> 동안
    /// 카트가 안 움직인다. 플레이어는 그걸 «안전장치» 로 안 읽고 <b>고장</b>으로 읽는다.
    ///
    /// 이건 «안 누르면» 의 안전장치지 기본 속도가 아니다 — <b>4.5초</b>면 한 줄을 읽기에
    /// 충분하고, 끝까지 가만히 있어도 12초 안에 출발한다.
    /// </summary>
    const float LineSeconds = 4.5f;

    /// <summary>마지막 줄을 읽고 나서 숫자가 뜨기까지의 숨. 바로 넘어가면 쫓기는 느낌이다.</summary>
    const float ChatterTail = 0.5f;

    static float chatterAt = -99f;
    static int chatterCount;

    /// <summary>
    /// ★★ 2026-10-06 <b>티키타카는 카운트다운 «앞» 에서 끝낸다</b>
    /// (유저: *"3초에 대사하지 말고, 티키타카를 끝내고 그 다음에 321 하고 출발"*).
    ///
    /// 맞는 판단이다. 숫자를 세는 3초는 <b>출발 타이밍을 보는 시간</b>이라, 거기에 글을
    /// 얹으면 둘 다 놓친다 — 대사를 읽으면 출발이 늦고, 출발을 보면 대사를 못 읽는다.
    /// <b>카드 닫기 → 티키타카 → 3 · 2 · 1 → 출발</b> 로 한 줄에 하나씩 세운다.
    /// </summary>
    /// <summary>
    /// ★★ 2026-10-06 <b>시간으로 넘기지 않고 «눌러서» 넘긴다</b>
    /// (유저: *"자동으로 띄우지 말고 대화창처럼 다음이랑 삼각형으로 넘길 수 있게"*).
    ///
    /// 그게 읽기에도 낫지만, 무엇보다 <b>멈출 구멍이 사라진다</b> —
    /// 전에는 <c>Time.time</c> 으로 재고 있었는데 그건 <c>Time.timeScale</c> 을 타서,
    /// 어디선가 0이 되어 있으면 <b>영원히 안 끝나고 출발도 안 한다.</b>
    /// 아래 시계는 전부 <b>unscaled</b> 이고, 그건 «안 눌렀을 때» 의 안전장치로만 쓴다.
    /// </summary>
    public static bool Chatting => chatterCount > 0;

    static float ChatterTotal => LineSeconds;

    static int chatterShown;

    /// <summary>지금까지 몇 줄이 나왔나. 한꺼번에 뿌리지 않고 <b>한 줄씩</b> 쌓인다.</summary>
    public static int ChatterShown => Chatting ? Mathf.Clamp(chatterShown, 1, chatterCount) : 0;

    /// <summary>더 읽을 줄이 남았나. 화면의 «다음 ▼» 이 이걸 본다.</summary>
    public static bool ChatterHasNext => Chatting && chatterShown < chatterCount;

    /// <summary>티키타카를 건너뛴다 — 아무 키나. 못 건너뛰는 게 제일 나쁘다.</summary>
    /// <summary>한 줄 넘긴다. 마지막 줄에서 누르면 티키타카가 끝나고 3 · 2 · 1 이 시작된다.</summary>
    public static void SkipChatter()
    {
        if (!Chatting) return;

        // ★ 카드를 닫은 그 키가 <b>다음 프레임에 넘기기로 새면</b> 첫 줄을 못 읽는다.
        if (Time.unscaledTime - chatterAt < 0.35f) return;

        if (chatterShown < chatterCount)
        {
            chatterShown++;
            chatterAt = Time.unscaledTime;
            return;
        }

        if (Dev.Enabled && debugLog) Debug.Log("[출발] 티키타카 끝 → 3 · 2 · 1");
        chatterCount = 0;
        RaceCountdown.Begin();
    }

    /// <summary>
    /// 티키타카가 끝났으면 카운트다운을 시작한다. <see cref="TestHUD"/> 와
    /// <see cref="LapTracker"/> 가 매 프레임 부른다 — <b>한 군데만 부르면 그 컴포넌트가
    /// 없는 씬에서 영영 안 출발한다.</b>
    /// </summary>
    public static void Tick()
    {
        RaceCountdown.Tick();      // 끊긴 프레임은 안 센다 — 결승에서 «1» 부터 보이던 것

        // ★ 카드가 25초 안전장치로 사라졌으면 여기서 마무리한다. 안 하면 그 뒤가 통째로 안 돈다
        if (open && !Open) { Close(); return; }

        if (chatterCount == 0) return;

        // 안 누르고 가만히 있어도 <b>12초 뒤엔 저절로 넘어간다.</b> 못 넘기는 게 제일 나쁘다
        if (Time.unscaledTime - chatterAt < ChatterTotal) return;

        if (chatterShown < chatterCount) { chatterShown++; chatterAt = Time.unscaledTime; return; }

        if (Dev.Enabled && debugLog) Debug.Log("[출발] 티키타카 시간 초과 → 3 · 2 · 1");
        chatterCount = 0;
        RaceCountdown.Begin();
    }

    /// <summary>레이스를 시작할 자리에서 부른다. 카드를 띄우거나, 바로 카운트다운으로 넘긴다.</summary>
    public static void Begin()
    {
        int goal = (int)MissionManager.CurrentGoal;

        chatterCount = 0;

        // ★★ 2026-10-06 유저: *"다 끝내고 자유 플레이 들어가면 첫 판에 시우가 «놀러 온 거
        //   아니야» 하고 1판 대사가 나온다."* 맞다 — 전부 모으면 <c>NextReward()</c> 가
        //   빈 문자열이고 <c>GoalForReward("")</c> 가 <b>Goal.완주로 떨어진다.</b>
        //   임무가 끝난 뒤의 자유 주행은 <b>브리핑도 티키타카도 없는 판</b>이다.
        if (GrandFinal.FreeRun)
        {
            lastShown = -1;
            Fresh = false;
            RaceCountdown.Begin();
            return;
        }

        if (goal == lastShown)
        {
            Fresh = false;
            RaceCountdown.Begin();
            return;
        }

        lastShown = goal;
        Fresh = true;
        open = true;
        openedAt = Time.unscaledTime;
        // 카운트다운은 <b>카드를 닫을 때</b> 시작한다. 지금 켜면 읽는 동안 숫자가 다 지나간다.
    }

    /// <summary>아무 키나. 닫으면서 3 · 2 · 1 이 시작된다.</summary>
    public static void Dismiss()
    {
        if (!Open) return;
        Close();
    }

    /// <summary>
    /// 카드를 접고 <b>그 다음 단계로 넘긴다.</b> 티키타카가 있으면 그것부터, 없으면 바로 숫자로.
    ///
    /// ★★ 2026-10-06 <b>이걸 함수로 뺀 이유</b>가 곧 버그였다. 전에는 <see cref="Dismiss"/>
    /// 안에만 있었는데, <see cref="Open"/> 이 25초 안전장치로 false 가 되면 Dismiss 가
    /// <b>맨 윗줄에서 돌아가 버려서</b> <c>open</c> 은 true 로 남고
    /// <b>카운트다운이 영영 시작되지 않았다.</b> 화면에는 카드도 대사도 숫자도 없이
    /// <b>그냥 멈춰 있는 것</b>으로 보인다 — 로그에도 아무것도 안 남는다.
    /// </summary>
    static void Close()
    {
        open = false;

        var lines = StoryScript.Chatter(MissionManager.CurrentGoal);
        int n = 0;
        if (lines != null)
            foreach (var l in lines) { if (!string.IsNullOrEmpty(l)) n++; if (n >= 2) break; }

        if (n > 0)
        {
            chatterCount = n;
            chatterShown = 1;
            chatterAt = Time.unscaledTime;
            if (Dev.Enabled && debugLog) Debug.Log($"[출발] 카드 닫음 → 티키타카 {n}줄 ({ChatterTotal:0.0}초)");
            return;
        }

        if (Dev.Enabled && debugLog) Debug.Log("[출발] 카드 닫음 → 대사 없음 → 3 · 2 · 1");
        RaceCountdown.Begin();
    }

    /// <summary>제출 전에 끌 것. 출발이 막히면 <b>어디서 막혔는지</b>를 이게 알려준다.</summary>
    public static bool debugLog = true;

    /// <summary>검사용 — 카드를 건너뛴다.</summary>
    public static void Skip()
    {
        open = false;
        lastShown = -999;
    }

    // ---- 화면이 읽는 값 ----------------------------------------------------

    static MissionManager.Goal Goal => MissionManager.CurrentGoal;

    /// <summary>"제3판" 또는 "결승". 몇 번째인지가 보여야 <b>끝이 있는 여정</b>으로 읽힌다.</summary>
    public static string Stage
    {
        get
        {
            if (Goal == MissionManager.Goal.결승) return "결승";
            int i = MissionManager.IndexOf(Goal);
            return i >= 0 ? $"제{i + 1}판" : "실사";
        }
    }

    /// <summary>모두 여덟 판 중 몇 번째인가. 결승은 아홉 번째.</summary>
    public static string Progress
    {
        get
        {
            if (Goal == MissionManager.Goal.결승) return "마지막";
            int i = MissionManager.IndexOf(Goal);
            return i >= 0 ? $"{i + 1} / {ExhibitCatalogue.Count}" : "";
        }
    }

    /// <summary>
    /// 임무 이름. <see cref="RaceVoice.Title"/> 가 <b>목표치</b>(발판 n개, 곰인형 n개)를
    /// 읽어야 해서 <see cref="MissionManager"/> 인스턴스가 필요하다 - 화면이 판정과
    /// <b>같은 속성</b>을 읽게 만든 그 규칙이야(2026-09-18). 그리는 쪽이 넘겨준다.
    /// </summary>
    public static string TitleFor(MissionManager m) =>
        m == null ? "" : RaceVoice.Title(Goal, m);

    /// <summary>왜 이 판을 도는가. 이 한 줄이 아홉 판을 아홉 장면으로 만든다.</summary>
    public static string Why => RaceVoice.Why(Goal);

    /// <summary>이 판을 깨면 받는 것. 결승은 상품이 없고 <b>이기는 것</b>이 전부다.</summary>
    public static string Reward
    {
        get
        {
            if (Goal == MissionManager.Goal.결승) return "";
            string id = MissionManager.NextReward();
            return string.IsNullOrEmpty(id) ? "" : ExhibitCatalogue.NameOf(id);
        }
    }

    /// <summary>아래 줄의 라벨. 결승은 상품이 아니라 <b>걸린 것</b>이 있다.</summary>
    public static string StakeLabel => Goal == MissionManager.Goal.결승 ? "걸린 것" : "상품";

    /// <summary>
    /// 아래 줄의 값. 결승은 받는 물건이 없고 <b>이기는 것 자체</b>가 목적이라,
    /// 빈칸을 두는 대신 무엇이 걸렸는지를 적는다 - 여덟 판을 쌓아 온 이유가 여기 있어야 한다.
    /// </summary>
    public static string Stake =>
        Goal == MissionManager.Goal.결승 ? "이기면 자유 주행이 열린다" : Reward;

    /// <summary>
    /// 이 판에서 나눌 대사. <b>유저가 <see cref="StoryScript"/> 에 쓰면 여기 들어온다.</b>
    /// 지금은 비어 있고, 비어 있으면 카드가 그 줄을 아예 안 그린다 —
    /// 자리만 잡아 두는 게 아니라 <b>없을 때도 멀쩡해야</b> 쓸모가 있다.
    /// </summary>
    public static string Line => StoryScript.Briefing(Goal);
}
