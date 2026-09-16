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
    public enum Goal
    {
        완주,        // 3바퀴를 끝내기만 하면 된다
        발판전부,    // 가속 발판을 매 바퀴 하나도 빠뜨리지 않고 완주
        무충돌,      // 벽에 세게 부딪히지 않고 완주
        제한시간,    // 정해진 시간 안에 완주
    }

    [Header("연결")]
    public LapTracker tracker;
    public KartController kart;

    [Header("규칙")]
    [Tooltip("무충돌 임무에서 봐주는 충돌 횟수. 0 이면 한 번도 안 된다")]
    public int allowedHits = 2;

    [Tooltip("제한시간 임무의 제한(초). 3바퀴 기준 넉넉한 완주가 87초쯤이다")]
    public float timeLimit = 105f;

    public bool Failed { get; private set; }
    public bool Cleared { get; private set; }

    /// <summary>이번 판에 걸린 수집품. 다 모았으면 빈 문자열.</summary>
    public string RewardId { get; private set; } = "";
    public string RewardName => string.IsNullOrEmpty(RewardId) ? "" : ExhibitCatalogue.NameOf(RewardId);
    public bool AllDone => string.IsNullOrEmpty(RewardId);

    public Goal goal { get; private set; }

    int padsTotal;
    int lastLapSeen = 1;
    bool rewarded;

    void Start()
    {
        padsTotal = BoostPad.CountInScene();
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
    public static Goal GoalForReward(string id)
    {
        for (int i = 0; i < ExhibitCatalogue.All.Length; i++)
            if (ExhibitCatalogue.All[i].id == id) return (Goal)(i % 4);
        return Goal.완주;
    }

    void Update()
    {
        if (tracker == null || kart == null || AllDone) return;

        // 씬을 안 다시 굽고 카트만 바꾸면 발판 수가 0으로 남는다. 그때 조용히 통과되면 안 된다.
        if (goal == Goal.발판전부 && padsTotal == 0) padsTotal = BoostPad.CountInScene();

        if (tracker.CurrentLap != lastLapSeen)
        {
            lastLapSeen = tracker.CurrentLap;
            // 한 바퀴가 끝날 때마다 발판은 다시 밟아야 한다 — 3바퀴 내내 챙기라는 뜻이야
            if (goal == Goal.발판전부) BoostPad.ClearTaken();
        }

        if (Failed || Cleared) return;

        // 판이 끝나기 전에 이미 글러버린 것들은 그 자리에서 알려준다.
        // 실패한 줄 모르고 두 바퀴를 더 도는 게 제일 허탈하다.
        if (goal == Goal.무충돌 && kart.WallHits > allowedHits) Failed = true;
        if (goal == Goal.제한시간 && tracker.TotalTime > timeLimit) Failed = true;

        if (!tracker.Finished) return;

        Cleared = goal switch
        {
            Goal.발판전부 => padsTotal > 0 && BoostPad.TakenCount() >= padsTotal,
            Goal.무충돌   => kart.WallHits <= allowedHits,
            Goal.제한시간 => tracker.TotalTime <= timeLimit,
            _             => true,
        };
        Failed = !Cleared;

        if (Cleared) GiveReward();
    }

    /// <summary>임무를 깬 그 순간 수집품이 들어온다. 트랙을 되돌아갈 일이 없다.</summary>
    void GiveReward()
    {
        if (rewarded || string.IsNullOrEmpty(RewardId)) return;
        rewarded = true;

        string name = RewardName;
        int chapterBefore = ChapterOf(RewardId);

        CollectionState.Collect(RewardId);
        Toast.Show($"임무 달성 — {name} 획득  ({CollectionState.Count} / {ExhibitCatalogue.Count})");

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
        rewarded = false;
        lastLapSeen = 1;

        BoostPad.ClearTaken();
        if (kart != null) kart.ResetWallHits();
    }

    // ---- 화면에 띄울 글 ----
    public string Title => goal switch
    {
        Goal.발판전부 => "가속 발판 전부 밟기",
        Goal.무충돌   => "벽에 안 부딪히기",
        Goal.제한시간 => $"{Mathf.RoundToInt(timeLimit)}초 안에 완주",
        _             => "3바퀴 완주",
    };

    public string Progress => goal switch
    {
        Goal.발판전부 => $"{BoostPad.TakenCount()} / {padsTotal}",
        Goal.무충돌   => allowedHits > 0 ? $"남은 기회 {Mathf.Max(0, allowedHits - WallHits)}"
                                         : (WallHits > 0 ? "실패" : "깨끗"),
        Goal.제한시간 => tracker != null ? LapTracker.FormatTime(Mathf.Max(0f, timeLimit - tracker.TotalTime)) : "",
        _             => tracker != null ? $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}" : "",
    };

    int WallHits => kart != null ? kart.WallHits : 0;
}
