using UnityEngine;

/// <summary>
/// 이번 레이스의 임무 하나. <b>실패할 수 있게 만드는 게 전부야.</b>
///
/// 지금까지 레이스가 심심했던 이유는 코스가 짧아서가 아니라 <b>질 수가 없어서</b>였다.
/// 벽에 박아도 끝은 나니까 잘 달릴 이유가 없었어. 임무가 붙으면 코스는 그대로인데
/// 세 번 다르게 달리게 된다 — 그게 기획서 §4.1 의 "트랙 하나를 챕터마다 다시 쓴다" 야.
///
/// <b>판정 단위는 레이스 한 판(3바퀴) 전체다.</b> 바퀴마다 끊어서 매기면
/// 첫 바퀴에 실패한 사람이 남은 두 바퀴를 의미 없이 돌아야 한다. 판이 끝나야 결과가 나오는 게
/// 다시 하기도 쉽고 이야기 단위와도 맞아.
///
/// 이 스크립트는 <b>판정만</b> 한다. 화면 표시는 TestHUD, 바퀴 세기는 LapTracker,
/// 발판은 BoostPad 가 맡는다 — 기획서 §9.1 의 "한 스크립트 한 가지 일".
/// </summary>
public class MissionManager : MonoBehaviour
{
    public enum Goal
    {
        완주,        // 3바퀴를 끝내기만 하면 된다 — 첫 장의 연습용
        발판전부,    // 가속 발판을 하나도 빠뜨리지 않고 완주
        무충돌,      // 벽에 세게 부딪히지 않고 완주
    }

    [Header("연결")]
    public LapTracker tracker;
    public KartController kart;

    [Header("규칙")]
    [Tooltip("비워두면 진행 중인 장에 맞는 임무가 자동으로 정해진다")]
    public bool followChapter = true;
    public Goal goal = Goal.완주;

    [Tooltip("무충돌 임무에서 봐주는 충돌 횟수. 0 이면 한 번도 안 된다")]
    public int allowedHits = 2;

    public bool Failed { get; private set; }
    public bool Cleared { get; private set; }

    int padsTotal;
    int lastLapSeen = 1;

    void Start()
    {
        if (followChapter) goal = GoalForChapter(StoryProgress.CurrentChapter);
        padsTotal = BoostPad.CountInScene();
        Restart();
    }

    /// <summary>
    /// 장이 올라갈수록 어려워진다. 기획서 §4.1 대로 <b>트랙은 그대로 두고 조건만 바꾼다.</b>
    /// </summary>
    public static Goal GoalForChapter(int chapter) => chapter switch
    {
        <= 1 => Goal.완주,
        2 => Goal.발판전부,
        _ => Goal.무충돌,
    };

    void Update()
    {
        if (tracker == null || kart == null) return;

        // 씬을 안 다시 굽고 카트만 바꾸면 발판 수가 0으로 남는다. 그때 조용히 통과되면 안 된다.
        if (goal == Goal.발판전부 && padsTotal == 0) padsTotal = BoostPad.CountInScene();

        if (tracker.CurrentLap != lastLapSeen)
        {
            lastLapSeen = tracker.CurrentLap;
            // 한 바퀴가 끝날 때마다 발판은 다시 밟아야 한다 — 3바퀴 내내 챙기라는 뜻이야
            if (goal == Goal.발판전부) BoostPad.ClearTaken();
        }

        if (Failed || Cleared) return;

        if (goal == Goal.무충돌 && kart.WallHits > allowedHits) Failed = true;

        if (!tracker.Finished) return;

        Cleared = goal switch
        {
            Goal.발판전부 => BoostPad.TakenCount() >= padsTotal && padsTotal > 0,
            Goal.무충돌   => kart.WallHits <= allowedHits,
            _             => true,
        };
        Failed = !Cleared;
    }

    /// <summary>레이스를 다시 시작할 때 LapTracker 가 불러준다.</summary>
    public void Restart()
    {
        Failed = false;
        Cleared = false;
        lastLapSeen = 1;
        BoostPad.ClearTaken();
        if (kart != null) kart.ResetWallHits();
    }

    // ---- 화면에 띄울 글 ----
    public string Title => goal switch
    {
        Goal.발판전부 => "발판 전부 밟기",
        Goal.무충돌   => "벽에 안 부딪히기",
        _             => "3바퀴 완주",
    };

    public string Progress => goal switch
    {
        Goal.발판전부 => $"{BoostPad.TakenCount()} / {padsTotal}",
        Goal.무충돌   => allowedHits > 0 ? $"남은 기회 {Mathf.Max(0, allowedHits - WallHits)}"
                                         : (WallHits > 0 ? "실패" : "깨끗"),
        _             => tracker != null ? $"{Mathf.Min(tracker.CurrentLap, tracker.totalLaps)} / {tracker.totalLaps}" : "",
    };

    int WallHits => kart != null ? kart.WallHits : 0;
}
