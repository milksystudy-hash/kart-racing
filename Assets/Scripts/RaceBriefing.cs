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
    const float Timeout = 25f;

    static bool open;
    static float openedAt = -99f;

    /// <summary>카드가 떠 있나. 이 동안에는 카트도 시계도 멈춘다.</summary>
    public static bool Open => open && Time.time - openedAt < Timeout;

    /// <summary>
    /// 마지막으로 보여준 임무. <b>같은 판을 다시 할 때는 안 띄운다</b> —
    /// 실패하고 ENTER 로 다시 하는 게 이 게임에서 제일 흔한 동작인데, 그때마다 읽은 카드가
    /// 또 뜨면 그건 안내가 아니라 장애물이 된다.
    /// </summary>
    static int lastShown = -999;

    /// <summary>레이스를 시작할 자리에서 부른다. 카드를 띄우거나, 바로 카운트다운으로 넘긴다.</summary>
    public static void Begin()
    {
        int goal = (int)MissionManager.CurrentGoal;

        if (goal == lastShown)
        {
            RaceCountdown.Begin();
            return;
        }

        lastShown = goal;
        open = true;
        openedAt = Time.time;
        // 카운트다운은 <b>카드를 닫을 때</b> 시작한다. 지금 켜면 읽는 동안 숫자가 다 지나간다.
    }

    /// <summary>아무 키나. 닫으면서 3 · 2 · 1 이 시작된다.</summary>
    public static void Dismiss()
    {
        if (!Open) return;
        open = false;
        RaceCountdown.Begin();
    }

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
