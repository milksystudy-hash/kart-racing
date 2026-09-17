using UnityEngine;

/// <summary>
/// <b>3 · 2 · 1 · 출발!</b> 레이스가 시작될 때 잠깐 모두를 묶어 둔다.
///
/// 2026-09-17 유저: *"엔터 누르고 새로 시작하면 카트들이 우수수 쏟아져 내리니까
/// 마리오카트처럼 3, 2, 1 출발 이런 식으로 해야 하지 않을까. 너무 파쿠리인가."*
/// <b>파쿠리가 아니다.</b> 출발 카운트는 1974년 스피드 레이스부터 거의 모든 레이싱
/// 게임에 있고, 특정 게임의 발명이 아니야. 이게 하는 일은 셋이다:
/// 다시 시작한 게 <b>눈에 보이고</b>, 넷이 <b>같은 선에서</b> 출발하고,
/// 되돌려 놓은 카트가 바닥에 내려앉을 시간을 번다(안 그러면 진짜로 쏟아진다).
///
/// MonoBehaviour 가 아니다 — 씬에 올릴 것도, 저장할 것도 없다.
/// 막는 곳은 <b>값을 넣는 자리 두 군데</b>(플레이어 입력 · <c>Drive</c>)라
/// 실행 순서에 안 휘둘린다.
/// </summary>
public static class RaceCountdown
{
    /// <summary>묶어 두는 시간(초). 3 · 2 · 1 이 한 번씩 뜬다.</summary>
    public const float Seconds = 3f;

    /// <summary>"출발!" 이 남아 있는 시간.</summary>
    const float GoSeconds = 0.9f;

    static float startedAt = -99f;

    public static void Begin() => startedAt = Time.time;

    /// <summary>검사용 — 카운트를 건너뛴다.</summary>
    public static void Skip() => startedAt = -99f;

    static float Elapsed => Time.time - startedAt;

    /// <summary>아직 못 움직이나.</summary>
    public static bool Blocked => Elapsed < Seconds;

    /// <summary>화면 가운데에 띄울 글자. 띄울 게 없으면 빈 문자열.</summary>
    public static string Label
    {
        get
        {
            float t = Elapsed;
            if (t < 0f || t > Seconds + GoSeconds) return "";
            if (t >= Seconds) return "출발!";
            return Mathf.CeilToInt(Seconds - t).ToString();
        }
    }

    /// <summary>글자가 커졌다 작아지는 배율. 숫자가 그냥 바뀌면 바뀐 걸 놓친다.</summary>
    public static float Pop
    {
        get
        {
            float t = Elapsed;
            if (t < 0f) return 1f;
            float into = t >= Seconds ? t - Seconds : t % 1f;   // 이 숫자가 뜬 지 얼마나 됐나
            return 1f + 0.45f * Mathf.Clamp01(1f - into * 4f);
        }
    }
}
