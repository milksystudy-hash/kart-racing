using UnityEngine;

/// <summary>
/// 레이스 중에 플레이어가 읽는 <b>말</b>을 한 군데 모았다.
///
/// 전에는 판정 코드가 문장을 직접 만들었고, 그래서 "벽에 3번 부딪혔다 / 무충돌로 시간 안에"
/// 같은 로봇 말투가 나왔다. 상태를 그대로 읽어주는 건 정보지 말이 아니야.
///
/// 한 번 시비 거는 말투로 써봤다가 유저가 <b>담백한 쪽으로 되돌렸다</b>(2026-09-16).
/// HUD 는 레이스 중에 힐끗 보는 거라 농담이 끼면 읽는 데 시간이 걸린다 — 맞는 판단이야.
/// 이야기의 말투(다크코미디)는 StoryScript 쪽에서 살린다. 여기는 계기판이지 대사가 아니다.
///
/// 그래도 <b>문장은 여기에만 둔다</b>. 판정 코드가 문장을 만들기 시작하면 다시 로봇 말투가 된다.
/// </summary>
public static class RaceVoice
{
    // ------------------------------------------------------------------
    //  임무 이름 — 짧아야 한다
    // ------------------------------------------------------------------
    /// <summary>
    /// 임무 이름. HUD 의 임무 칸은 <b>두 줄까지 자동으로 접힌다</b>(TestHUD 의 wordWrap) —
    /// "부딪힘 없이 시간제한에 맞춰 도착하기" 처럼 길어도 안 겹친다. 세 줄 넘어가면 잘리니 거기까지만.
    /// 전에는 접기가 없어서 "무충돌로 시간 안에" 가 라벨을 파고들어 "무충룰" 로 보였다.
    /// </summary>
    public static string Title(MissionManager.Goal goal, MissionManager m) => goal switch
    {
        MissionManager.Goal.발판전부 => "발판 다 밟기",
        MissionManager.Goal.무충돌   => "안 긁고 완주",
        MissionManager.Goal.제한시간 => $"{Mathf.RoundToInt(m.timeLimit)}초 컷",
        MissionManager.Goal.태엽    => $"태엽 {m.driftBoostsNeeded}번",
        MissionManager.Goal.무발판   => "발판 없이",
        MissionManager.Goal.빠른랩   => $"한 바퀴 {Mathf.RoundToInt(m.lapLimit)}초",
        MissionManager.Goal.완벽    => "부딪힘 없이 시간제한에 맞춰 도착하기",
        _                           => "세 바퀴 완주",
    };

    // ------------------------------------------------------------------
    //  실패 사유 — 뭘 고쳐야 하는지가 한눈에 보여야 한다
    // ------------------------------------------------------------------
    public static string WallHit(int hits) => $"벽 {hits}번 부딪힘";

    public static string OutOfTime() => "시간 초과";

    public static string SteppedOnPad() => "가속 발판을 밟음";

    public static string MissedPads(int got, int total) => $"발판 {got}/{total} 밟음";

    public static string NotEnoughDrift(int got, int need) => $"태엽 {got}/{need}번";

    public static string NoFastLap() => "목표 랩타임 못 냄";

    public static string Generic() => "조건 미달";

    // ------------------------------------------------------------------
    //  화면 아래 안내
    // ------------------------------------------------------------------
    /// <summary>키 이름만. 설명을 붙이면 카드가 빽빽해지고, 어차피 한 번 보면 안다.</summary>
    public static string RetryHint() => "ENTER";
    public static string QuitHint() => "ESC  그만두기";

    public static string Failed() => "임무 실패";
    public static string Cleared() => "성공";

    public static string Reward(string itemName, int got, int total)
        => $"{itemName} 획득   ({got} / {total})";
}
