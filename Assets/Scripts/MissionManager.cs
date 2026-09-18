using UnityEngine;

/// <summary>
/// 이번 레이스의 임무 하나. <b>깨면 수집품 하나를 준다.</b>
///
/// 2026-09-16 에 방식을 바꿨다. 전에는 트랙에 떨어진 물건을 <b>주워서</b> 모았는데,
/// 유저 지적대로 그건 레이싱의 본질을 흐린다 — 물건을 주우러 가려고 일부러 느리게 달리고,
/// 심하면 뒤로 돌아가야 한다. 그리고 한 판에 두 개씩 주워지니 8개가 네 판이면 끝났다.
///
/// 지금은 <b>한 판 = 임무 하나 = 수집품 하나</b>다. 8개니까 여덟 판.
/// 한 판이 1분 30초쯤이니 레이스만 12분, 기획서 §1.2 의 15~25분 안에 들어온다.
/// 잘 달리는 것 말고 다른 걸 할 필요가 없어졌고, 플레이 시간은 오히려 늘었어.
///
/// <b>판정 단위는 레이스 한 판(3바퀴) 전체다.</b> 바퀴마다 끊어서 매기면
/// 첫 바퀴에 실패한 사람이 남은 두 바퀴를 의미 없이 돌아야 한다.
///
/// 이 스크립트는 <b>판정과 보상만</b> 한다. 화면 표시는 TestHUD, 바퀴 세기는 LapTracker,
/// 발판은 BoostPad 가 맡는다 — 기획서 §9.1 의 "한 스크립트 한 가지 일".
/// </summary>
public class MissionManager : MonoBehaviour
{
    /// <summary>
    /// 수집품 여덟 개니까 임무도 <b>여덟 개 전부 다르다</b>(2026-09-16 유저).
    /// 같은 트랙을 여덟 번 도는데 매번 신경 쓰는 게 달라진다 — 기획서 §4.1 이 노린 게 이거야.
    /// AI 카트와 겨루는 "1위" 임무는 AI 주행이 들어온 뒤에 아홉 번째로 붙인다.
    /// </summary>
    public enum Goal
    {
        완주,        // 3바퀴를 끝내기만 하면 된다
        발판전부,    // 가속 발판을 매 바퀴 하나도 빠뜨리지 않고
        무충돌,      // 벽에 세게 부딪히지 않고
        제한시간,    // 정해진 시간 안에 완주
        장애물,      // 길에 널린 철거 자재를 치지 않고 완주
        전시품,      // 창고에 처박힌 곰인형을 전시실로 나른다 — <b>플러스형</b>
        광고판,      // 골든베어 입간판을 전부 들이받아 부수기
        완벽,        // 무충돌 + 제한시간 동시. 마지막 판
    }

    [Header("연결")]
    public LapTracker tracker;
    public KartController kart;

    [Header("규칙")]
    [Tooltip("무충돌 임무에서 봐주는 충돌 횟수. 0 이면 한 번도 안 된다")]
    public int allowedHits = 2;

    // 한 바퀴가 464 -> 535m 로 15% 길어져서(2026-09-16 직선 추가) 전부 비례로 옮겼다.
    // 난이도는 그대로 둔 값이야 — 실제 랩타임을 재면 그때 조인다.
    [Tooltip("제한시간 임무의 제한(초). 코스가 535m 로 늘어난 뒤 값")]
    public float timeLimit = 121f;

    [Tooltip("빠른랩 임무에서 한 바퀴를 몇 초 안에. 535m 기준 (지금은 안 쓴다 — 광고판으로 바뀜)")]
    public float lapLimit = 38f;

    [Tooltip("태엽 임무에서 태엽을 몇 번 터뜨려야 하는지 (지금은 안 쓴다 — 무정차로 바뀜)")]
    public int driftBoostsNeeded = 6;

    [Tooltip("장애물 임무에서 봐주는 충돌 횟수")]
    public int allowedDebris = 2;

    [Tooltip("발판전부 임무에서 밟아야 하는 최소 개수. 0 이면 전부")]
    public int padsNeeded = 4;

    [Tooltip("전시품 임무에서 실어야 하는 최소 개수. 0 이면 전부")]
    public int cargoNeeded = 6;

    [Tooltip("완벽 임무의 제한(초). 무충돌까지 같이 지켜야 한다")]
    public float perfectTimeLimit = 133f;

    public bool Failed { get; private set; }
    public bool Cleared { get; private set; }

    /// <summary>왜 실패했는지 한 줄. 화면에 그대로 띄운다 — "실패" 만 뜨면 뭘 고쳐야 할지 모른다.</summary>
    public string FailReason { get; private set; } = "";

    /// <summary>이번 판에 걸린 수집품. 다 모았으면 빈 문자열.</summary>
    public string RewardId { get; private set; } = "";
    public string RewardName => string.IsNullOrEmpty(RewardId) ? "" : ExhibitCatalogue.NameOf(RewardId);
    public bool AllDone => string.IsNullOrEmpty(RewardId);

    /// <summary>
    /// 마지막 판인가. <b>AI 카트는 여기서만 나온다</b>(2026-09-17 유저:
    /// *"원래 8개 다 모으고 나서 마지막이 AI 카트 3대와 함께 달리는 거잖아."*).
    ///
    /// 처음부터 AI 와 겨루면 <b>배우는 판이 사라진다</b> — 1판은 뭘 하는 게임인지 익히는
    /// 자리인데 옆에서 세 대가 달리면 조작을 익힐 겨를이 없다.
    /// 다 모은 뒤 자유 주행에서도 남겨둔다 — 그때는 같이 달릴 상대가 있는 게 낫다.
    /// </summary>
    /// <summary>지금 판이 전시품 판인가. 곰인형들이 스스로 이걸 본다.</summary>
    public static bool WantsCargo =>
        !string.IsNullOrEmpty(NextReward()) && CurrentGoal == Goal.전시품;

    /// <summary>지금 판이 장애물 판인가. 자재들이 스스로 이걸 본다.</summary>
    public static bool WantsDebris =>
        !string.IsNullOrEmpty(NextReward()) && CurrentGoal == Goal.장애물;

    /// <summary>
    /// 지금 판이 광고판 판인가. 유저: *"그 이후 임무에도 골든베어가 붙어 있더라."*
    /// 자재와 같은 규칙이야 — <b>그 판에만</b> 세운다. 판마다 널려 있으면 그 판만의 성격이 없어진다.
    /// </summary>
    public static bool WantsAdSigns =>
        !string.IsNullOrEmpty(NextReward()) && CurrentGoal == Goal.광고판;

    /// <summary>
    /// <b>여덟 판을 전부 깬 뒤인가.</b> AI 카트는 여기서만 나온다.
    ///
    /// 2026-09-17 유저(세 번째): *"임무 1번부터 8번까지는 선택한 캐릭터 혼자 달리게 하고,
    /// 8번까지 다 깨면 그제서야 AI 카트 3대와 경주하는 걸로."*
    ///
    /// 전에는 <c>Count >= Count − 1</c>(7개)이라 <b>8번째 판에 이미 AI 가 나왔다.</b>
    /// 8번째도 임무 판이다 — 임무를 도는 동안에는 언제나 혼자야.
    /// AI 는 임무가 다 끝난 뒤의 <b>자유 주행</b>에서만 나온다.
    /// </summary>
    public static bool FinalRace =>
        ExhibitCatalogue.Count > 0 && CollectionState.Count >= ExhibitCatalogue.Count;

    public Goal goal { get; private set; }

    int padsTotal;
    int signsTotal;
    /// <summary>이번 바퀴가 시작될 때까지 부순 개수. 화면에는 이걸 뺀 값이 뜬다.</summary>
    int lapBaseBreaks;
    int lastLapSeen = 1;
    bool rewarded;

    void Start()
    {
        padsTotal = BoostPad.CountInScene();
        signsTotal = AdBoard.CountInScene();
        Restart();
    }

    /// <summary>
    /// 아직 안 모은 것 중 목록에서 제일 앞의 것. 이게 이번 판의 상품이다.
    /// 목록 순서가 곧 진행 순서라서, 어디까지 왔는지를 따로 저장할 필요가 없다.
    /// </summary>
    public static string NextReward()
    {
        foreach (var item in ExhibitCatalogue.All)
            if (!CollectionState.Has(item.id)) return item.id;
        return "";
    }

    /// <summary>
    /// 수집품마다 임무가 다르다. 네 가지를 돌려 쓰니까 여덟 판이 전부 다른 판이 된다 —
    /// 트랙은 하나뿐인데 여덟 번 다르게 달리게 되는 게 기획서 §4.1 의 노림수야.
    /// 첫 판은 무조건 완주로 시작한다. 처음부터 조건을 걸면 뭘 하는 게임인지 모른다.
    /// </summary>
    /// <summary>
    /// 지금 판의 임무. <b>MissionManager 없이도 알 수 있다</b> — 수집 기록만 보면 되니까.
    ///
    /// 게이트 컴포넌트가 씬에 없거나 Start 순서가 밀려도 물건들이 스스로 판단할 수 있게
    /// 열어둔다. 새 컴포넌트에만 기대면 <b>옛날에 구운 씬에서 안 먹는다</b> —
    /// 이 프로젝트에서 네 번 겪었다(카트 중복 · 벽 부딪힘 · AI · 장애물).
    /// </summary>
    public static Goal CurrentGoal => GoalForReward(NextReward());

    public static Goal GoalForReward(string id)
    {
        // 목록 순서 그대로 임무 순서다. 첫 판은 완주 — 처음부터 조건을 걸면 뭘 하는 게임인지 모른다.
        for (int i = 0; i < ExhibitCatalogue.All.Length; i++)
            if (ExhibitCatalogue.All[i].id == id) return (Goal)Mathf.Min(i, 7);
        return Goal.완주;
    }

    void Update()
    {
        if (tracker == null || kart == null || AllDone) return;

        // 씬을 안 다시 굽고 카트만 바꾸면 발판 수가 0으로 남는다. 그때 조용히 통과되면 안 된다.
        if (goal == Goal.발판전부 && padsTotal == 0) padsTotal = BoostPad.CountInScene();
        if (goal == Goal.광고판 && signsTotal == 0) signsTotal = AdBoard.CountInScene();

        if (tracker.CurrentLap != lastLapSeen)
        {
            lastLapSeen = tracker.CurrentLap;
            // 한 바퀴가 끝날 때마다 발판은 다시 밟아야 한다 — 3바퀴 내내 챙기라는 뜻이야
            if (goal == Goal.발판전부) BoostPad.ClearTaken();
            // 첫 바퀴에 다 치워버리면 두세 바퀴가 그냥 완주가 된다
            if (goal == Goal.장애물) RoadDebris.RestoreAll();
            // 간판도 바퀴마다 다시 선다. <b>부순 누적 개수는 안 지운다</b> — 그래서
            // 3바퀴에 8×3 = 24개를 부숴야 하고, 첫 바퀴에 몰아 부수는 게 의미가 없어진다.
            if (goal == Goal.광고판) { AdBoard.RestoreAll(); lapBaseBreaks = AdBoard.Breaks; }
        }

        if (Failed || Cleared) return;

        // 판이 끝나기 전에 이미 글러버린 것들은 그 자리에서 알려준다.
        // 실패한 줄 모르고 두 바퀴를 더 도는 게 제일 허탈하다.
        if ((goal == Goal.무충돌 || goal == Goal.완벽) && kart.WallHits > allowedHits)
            Fail(RaceVoice.WallHit(kart.WallHits));
        if (goal == Goal.제한시간 && tracker.TotalTime > timeLimit) Fail(RaceVoice.OutOfTime());

        // 장애물 — 정해진 횟수를 넘겨 치면 그 자리에서 끝
        if (goal == Goal.장애물 && RoadDebris.Hits > allowedDebris)
            Fail(RaceVoice.HitDebris(RoadDebris.Hits));
        if (goal == Goal.완벽 && tracker.TotalTime > perfectTimeLimit) Fail(RaceVoice.OutOfTime());

        if (Failed || !tracker.Finished) return;

        Cleared = goal switch
        {
            // <b>전부</b>가 아니라 <b>목표치</b>다(2026-09-18). 하나 놓쳤다고 실패하면
            // 그 판은 감점제가 되고, 감점제만 여덟 판이면 게임이 안 신난다.
            Goal.발판전부 => padsTotal > 0 && BoostPad.TakenCount() >= PadQuota,
            Goal.무충돌   => kart.WallHits <= allowedHits,
            Goal.제한시간 => tracker.TotalTime <= timeLimit,
            Goal.장애물   => RoadDebris.Hits <= allowedDebris,
            Goal.전시품   => CargoQuota > 0 && ExhibitCargo.Loaded >= CargoQuota,
            Goal.광고판   => SignQuota > 0 && AdBoard.Breaks >= SignQuota,
            Goal.완벽    => kart.WallHits <= allowedHits && tracker.TotalTime <= perfectTimeLimit,
            _             => true,
        };

        if (Cleared) GiveReward();
        else Fail(goal switch
        {
            Goal.발판전부 => RaceVoice.MissedPads(BoostPad.TakenCount(), PadQuota),
            Goal.전시품   => RaceVoice.MissedCargo(ExhibitCargo.Loaded, CargoQuota),
            Goal.장애물   => RaceVoice.HitDebris(RoadDebris.Hits),
            Goal.광고판   => RaceVoice.MissedSigns(AdBoard.Breaks, SignQuota),
            _             => RaceVoice.Generic(),
        });
    }

    void Fail(string reason)
    {
        if (Failed) return;
        Failed = true;
        FailReason = reason;
        Toast.Show(reason);
    }

    /// <summary>임무를 깬 그 순간 수집품이 들어온다. 트랙을 되돌아갈 일이 없다.</summary>
    void GiveReward()
    {
        if (rewarded || string.IsNullOrEmpty(RewardId)) return;
        rewarded = true;

        string name = RewardName;
        int chapterBefore = ChapterOf(RewardId);

        CollectionState.Collect(RewardId);
        Toast.Show(RaceVoice.Reward(name, CollectionState.Count, ExhibitCatalogue.Count));

        // 이 장의 수집품을 다 모았으면 다음 장으로. 이게 있어서 F7 로 손수 넘길 필요가 없어졌다.
        if (ChapterComplete(chapterBefore) && StoryProgress.CurrentChapter <= chapterBefore)
            StoryProgress.AdvanceChapter();
    }

    static int ChapterOf(string id)
    {
        foreach (var item in ExhibitCatalogue.All)
            if (item.id == id) return item.chapterIndex;
        return 0;
    }

    static bool ChapterComplete(int chapter)
    {
        foreach (var item in ExhibitCatalogue.All)
            if (item.chapterIndex == chapter && !CollectionState.Has(item.id)) return false;
        return true;
    }

    /// <summary>레이스를 다시 시작할 때 LapTracker 가 불러준다.</summary>
    public void Restart()
    {
        RewardId = NextReward();
        goal = GoalForReward(RewardId);

        Failed = false;
        Cleared = false;
        FailReason = "";
        rewarded = false;
        lastLapSeen = 1;
        lapBaseBreaks = 0;

        BoostPad.ClearTaken();
        ExhibitCargo.ResetAll();
        AdBoard.ResetAll();
        RoadDebris.ResetHits();
        RoadDebris.RestoreAll();
        if (kart != null) kart.ResetWallHits();

        // 방금 여덟 번째를 받았으면 이 판부터 AI 가 나온다
        var gate = FindFirstObjectByType<AiRaceGate>();
        if (gate != null) gate.Apply();

        var debris = FindFirstObjectByType<DebrisGate>();
        if (debris != null) debris.Apply();

        var ads = FindFirstObjectByType<AdSignGate>();
        if (ads != null) ads.Apply();

        var crowd = FindFirstObjectByType<CargoGate>();
        if (crowd != null) crowd.Apply();

        // 게이트가 간판을 켜고 끈 다음에 세야 맞다. 꺼져 있으면 0 이고, 그러면
        // 광고판 임무가 아니라는 뜻이라 어차피 안 쓴다.
        signsTotal = AdBoard.CountInScene();
    }

    // ---- 화면에 띄울 글 ----
    /// <summary>글은 RaceVoice 에 있다. 판정 코드가 문장을 만들면 로봇 말투가 된다.</summary>
    public string Title => RaceVoice.Title(goal, this);

    public string Progress => goal switch
    {
        Goal.발판전부 => $"{BoostPad.TakenCount()} / {PadQuota}",
        Goal.무충돌   => ChanceLabel,
        Goal.제한시간 => Remaining(timeLimit),
        Goal.장애물   => $"기회 {Chances(allowedDebris, RoadDebris.Hits)}",
        Goal.전시품   => $"{ExhibitCargo.Loaded} / {CargoQuota}",
        // 2026-09-17 유저: *"처음부터 0/24 를 띄우면 플레이어가 부담을 느낀다.
        // 1랩에 0/8, 2랩에도 0/8 로."* 맞다 — 지금 이 바퀴에 <b>몇 개 남았는지</b>가
        // 운전에 필요한 숫자고, 24 는 판이 끝나야 의미가 있는 숫자야.
        Goal.광고판   => $"{AdBoard.Breaks - lapBaseBreaks} / {signsTotal}",
        // 두 조건을 다 보여줘야 하는데 칸이 좁다. "남은 기회 2" 대신 "2회" 로 줄인다.
        Goal.완벽    => $"{Chances(allowedHits, WallHits)}회 · {Remaining(perfectTimeLimit)}",
        _             => tracker != null ? $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}" : "",
    };

    // 칸이 54px 라 "남은 기회 2"(77px)는 넘친다. 짧게.
    string ChanceLabel => allowedHits > 0 ? $"기회 {Chances(allowedHits, WallHits)}"
                                          : (WallHits > 0 ? "0" : "깨끗");

    /// <summary>
    /// <b>남은 기회는 봐주는 횟수 + 1 이다.</b> 2026-09-18 유저:
    /// *"자재 3번 치면 실패라고 뜨는데 기회 2라고 적으면 안 되지 않아?
    /// 0 됐는데 하면 플레이어가 헷갈리잖아. 기회 1이 마지막 기회로 인식돼야
    /// 마지막에 쳤을 때 '아 실패구나' 가 된다."*
    ///
    /// 맞다. 판정은 <c>Hits &gt; allowed</c> 라 <b>allowed + 1 번째에 실패</b>한다.
    /// 그런데 화면에는 `allowed − Hits` 를 띄워서 <b>0 이 뜬 채로 한 번 더 칠 수 있었다.</b>
    /// 남은 횟수를 세는 칸이 0 인데 아직 안 죽으면 그 숫자는 거짓말이야.
    ///
    /// 이제 봐주는 횟수 2 → 처음에 <b>기회 3</b>, 한 번 치면 2, 두 번이면 1,
    /// <b>세 번째에 0 과 동시에 실패</b>. 숫자와 사건이 같은 순간에 일어난다.
    /// </summary>
    static int Chances(int allowed, int used) => Mathf.Max(0, allowed + 1 - used);

    string Remaining(float limit) =>
        tracker != null ? LapTracker.FormatTime(Mathf.Max(0f, limit - tracker.TotalTime)) : "";

    int WallHits => kart != null ? kart.WallHits : 0;

    /// <summary>광고판 임무에서 부숴야 하는 총 개수 = 간판 수 × 바퀴 수.</summary>
    int SignQuota => signsTotal * Mathf.Max(1, tracker != null ? tracker.totalLaps : 1);

    /// <summary>발판 목표치. 씬에 있는 수보다 크게 잡히지 않는다.</summary>
    int PadQuota => padsNeeded <= 0 ? padsTotal : Mathf.Min(padsNeeded, padsTotal);

    /// <summary>전시품 목표치. 놓쳐도 되는 여유가 있어야 <b>모으는 재미</b>가 된다.</summary>
    int CargoQuota
    {
        get
        {
            int there = ExhibitCargo.CountInScene();
            return cargoNeeded <= 0 ? there : Mathf.Min(cargoNeeded, there);
        }
    }
}
