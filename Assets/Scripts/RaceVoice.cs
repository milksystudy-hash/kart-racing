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
        MissionManager.Goal.발판전부 => "순찰 등 전부 켜기",
        MissionManager.Goal.무충돌   => "담장 긁지 않기",
        MissionManager.Goal.제한시간 => $"{Mathf.RoundToInt(m.timeLimit)}초 안에 실사 끝내기",
        MissionManager.Goal.태엽    => $"태엽 {m.driftBoostsNeeded}번 감기",
        MissionManager.Goal.무발판   => "발판 밟지 않고 조용히",
        MissionManager.Goal.광고판   => "골든베어 광고 철거",
        MissionManager.Goal.완벽    => "담장 안 긁고 시간 안에 실사 끝내기",
        _                           => "캠퍼스 세 바퀴 돌기",
    };

    /// <summary>
    /// 임무 한 줄 설명. <b>왜 이걸 하는지</b>를 이야기로 붙인다 — 조건만 적으면
    /// "발판 다 밟기" 처럼 게임 조작 설명이 되고, 박물관과 아무 상관이 없어진다.
    /// (2026-09-17 유저 · 강사님 피드백. 임시 문구라 <see cref="StoryScript"/> 쪽과 같이 다듬을 것)
    /// </summary>
    public static string Why(MissionManager.Goal goal) => goal switch
    {
        MissionManager.Goal.발판전부 => "밤에 불 꺼진 캠퍼스는 폐가로 찍힌다",
        MissionManager.Goal.무충돌   => "담장 흠집도 철거 사유로 적힌다",
        MissionManager.Goal.제한시간 => "실사단이 오래 머물수록 트집이 늘어난다",
        MissionManager.Goal.태엽    => "태엽 소리가 나야 아직 돌아가는 곳이다",
        MissionManager.Goal.무발판   => "전기 쓴 기록이 남으면 예산 낭비로 잡힌다",
        MissionManager.Goal.광고판   => "리조트 광고가 먼저 와서 서 있다",
        MissionManager.Goal.완벽    => "마지막 실사다",
        _                           => "아직 운영 중이라는 걸 보여야 한다",
    };

    // ------------------------------------------------------------------
    //  실패 사유 — 뭘 고쳐야 하는지가 한눈에 보여야 한다
    // ------------------------------------------------------------------
    public static string WallHit(int hits) => $"벽 {hits}번 부딪힘";

    public static string OutOfTime() => "시간 초과";

    public static string SteppedOnPad() => "가속 발판을 밟음";

    public static string MissedPads(int got, int total) => $"발판 {got}/{total} 밟음";

    public static string NotEnoughDrift(int got, int need) => $"태엽 {got}/{need}번";

    public static string MissedSigns(int got, int total) => $"광고판 {got}/{total} 부숨";

    public static string Generic() => "조건 미달";

    // ------------------------------------------------------------------
    //  화면 아래 안내
    // ------------------------------------------------------------------
    /// <summary>키 이름만. 설명을 붙이면 카드가 빽빽해지고, 어차피 한 번 보면 안다.</summary>
    public static string RetryHint() => "ENTER 닫음";
    public static string QuitHint() => "ESC 그만두기";

    public static string Failed() => "임무 실패";
    public static string Cleared() => "성공";

    /// <summary>
    /// 수집품을 여덟 개 다 모은 뒤의 레이스. <b>임무가 없으니 실패도 없다.</b>
    /// 전에는 상품이 없는 상태에서도 판정을 그대로 돌려서 "임무 실패 — 세 바퀴 완주" 가 떴다.
    /// </summary>
    public static string FreeRun() => "자유 주행 — 기록을 줄여 봐";

    public static string Reward(string itemName, int got, int total)
        => $"{itemName} 획득   ({got} / {total})";
}
