using UnityEngine;

/// <summary>
/// 레이스 중에 플레이어가 읽는 <b>말</b>을 한 군데 모았다.
///
/// 전에는 판정 코드가 문장을 직접 만들었고, 그래서 "벽에 3번 부딪혔다 / 무충돌로 시간 안에"
/// 같은 로봇 말투가 나왔다. 상태를 그대로 읽어주는 건 정보지 말이 아니야.
///
/// 말투는 이 게임 것 — <b>다크코미디, 약간 시비 거는 투</b>. 곰들이 옆에서 리모컨 들고
/// 지켜보는 상황이니까, 화면의 목소리는 심판이라기보다 <b>지켜보던 놈</b>에 가깝다.
///
/// <b>여기 있는 건 견본이야.</b> 대사는 원래 유저가 쓴다(<see cref="StoryScript"/> 와 같은 원칙).
/// 마음에 안 드는 줄은 이 파일에서 바로 고치면 되고, 판정 코드는 손댈 필요 없다.
/// </summary>
public static class RaceVoice
{
    // ------------------------------------------------------------------
    //  임무 이름 — 짧아야 한다
    // ------------------------------------------------------------------
    /// <summary>
    /// HUD 칸이 좁아서 <b>여덟 글자를 넘기면 옆 글자와 부딪힌다.</b>
    /// 실제로 "무충돌로 시간 안에" 가 라벨과 겹쳐서 "무충룰" 로 보였다(2026-09-16).
    /// 새 임무를 넣을 때도 이 길이를 지켜.
    /// </summary>
    public static string Title(MissionManager.Goal goal, MissionManager m) => goal switch
    {
        MissionManager.Goal.발판전부 => "발판 다 밟기",
        MissionManager.Goal.무충돌   => "안 긁고 완주",
        MissionManager.Goal.제한시간 => $"{Mathf.RoundToInt(m.timeLimit)}초 컷",
        MissionManager.Goal.태엽    => $"태엽 {m.driftBoostsNeeded}번",
        MissionManager.Goal.무발판   => "발판 없이",
        MissionManager.Goal.빠른랩   => $"한 바퀴 {Mathf.RoundToInt(m.lapLimit)}초",
        MissionManager.Goal.완벽    => "무사고 + 시간",
        _                           => "세 바퀴 완주",
    };

    // ------------------------------------------------------------------
    //  실패했을 때 — 왜 실패했는지가 아니라, 무슨 일이 있었는지
    // ------------------------------------------------------------------
    public static string WallHit(int hits) => Pick(new[]
    {
        $"벽이 {hits}번 이겼다.",
        "저 벽, 아까부터 거기 있었어.",
        $"{hits}번. 카트는 아무 잘못 없다.",
        "벽을 세는 게임이 아니야.",
    });

    public static string OutOfTime() => Pick(new[]
    {
        "시계가 먼저 들어왔다.",
        "느긋했네. 아주.",
        "구경하면서 달렸구나.",
    });

    public static string SteppedOnPad() => Pick(new[]
    {
        "밟지 말랬잖아.",
        "발이 먼저 나갔네.",
        "그 파란 거, 피하라고 있는 거였어.",
    });

    public static string MissedPads(int got, int total) => Pick(new[]
    {
        $"{total}개 중 {got}개. 하나가 그렇게 멀디?",
        "한 바퀴라도 제대로 돌면 되는데.",
        $"{got}/{total}. 아쉬운 쪽은 너다.",
    });

    public static string NotEnoughDrift(int got, int need) => Pick(new[]
    {
        $"태엽 {got}번. {need}번이랬는데.",
        "드리프트가 무서우면 저렇게 된다.",
        "SHIFT 는 장식이 아니야.",
    });

    public static string NoFastLap() => Pick(new[]
    {
        "세 바퀴 다 비슷하게 느렸다.",
        "한 바퀴만 잘하면 되는 거였는데.",
        "어느 바퀴도 특별하지 않았어.",
    });

    public static string Generic() => Pick(new[]
    {
        "안 됐다.",
        "조건은 조건이야.",
    });

    // ------------------------------------------------------------------
    //  화면 아래 안내
    // ------------------------------------------------------------------
    /// <summary>다시 하라는 말. <b>모은 걸 안 뺏는다는 게 제일 중요한 정보다</b> — 그게 있어야 다시 눌러본다.</summary>
    public static string RetryHint() => "ENTER — 다시. 모은 건 그대로 둔다";

    public static string Failed() => "망했다";
    public static string Cleared() => "됐다";

    public static string Reward(string itemName, int got, int total)
        => $"{itemName} 챙겼다   ({got} / {total})";

    static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];
}
