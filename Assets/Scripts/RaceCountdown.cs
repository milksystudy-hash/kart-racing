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

    /// <summary>
    /// ★★ 2026-10-06 <b>마지막 안전장치.</b> 레이스에 들어온 지 이만큼 지나면
    /// 카드든 대사든 숫자든 <b>무조건 풀린다.</b>
    ///
    /// 정상 흐름은 티키타카(최대 9초) + 숫자(3초) = 12초라 넉넉하고,
    /// 그보다 오래 막혀 있다는 건 <b>어딘가 고장났다는 뜻</b>이다. 그때 플레이어가
    /// 할 수 있는 게 아무것도 없으면 안 된다 — 문에 바닥값을 준 것, 급식 줄에 4초를 둔 것과
    /// 같은 판단이야. <b>못 하는 게 제일 나쁘다.</b>
    /// </summary>
    const float HardStop = 22f;

    static float armedAt = -99f;

    /// <summary>레이스에 들어온 시각. <see cref="LapTracker"/> 가 찍는다.</summary>
    public static void Arm() => armedAt = Time.unscaledTime;

    static bool GaveUp => Time.unscaledTime - armedAt > HardStop;

    /// <summary>
    /// ★★ 2026-10-07 — <b>«시작했나» 와 «보였나» 는 다른 값이다.</b>
    ///
    /// 수연: *"스토리 보고 다시 들어오면 3·2·1 이 안 뜨고 소리만 나고, 그 뒤에 또 한 번 뜬다.
    /// 모든 라운드가 그렇다."* 원인은 <see cref="Shown"/> 이 <b>시작하지도 않은 카운트를
    /// 시작시켜 버린 것</b>이다 — <c>DrawCountdown</c> 이 맨 윗줄에서 조건 없이 부르니까,
    /// 씬이 열리고 <b>브리핑 카드가 떠 있는 첫 프레임</b>에 이미 시계가 돌기 시작했다.
    /// 숫자는 카드 뒤에 가려 안 보이고 <b>소리만</b> 났고, 카드를 닫은 뒤 <see cref="Begin"/>
    /// 이 제대로 불리면서 <b>같은 카운트를 한 번 더</b> 했다.
    ///
    /// 그래서 플래그를 둘로 나눈다 — <c>running</c>(시작 선언) · <c>drawn</c>(첫 렌더).
    /// <b>«보였다» 는 시작한 뒤에만 의미가 있다.</b>
    /// </summary>
    static bool running;

    // ★ unscaled — <see cref="RaceBriefing"/> 와 같은 이유. timeScale 이 0이면 숫자가 안 센다
    public static void Begin()
    {
        running = true;
        startedAt = -99f;      // 아직 안 센다 — 화면에 처음 그려지는 프레임이 0초다
        drawn = false;
        beganAt = Time.unscaledTime;
        // ★ 22초 안전장치의 기준도 <b>여기서</b> 다시 잡는다. 카드와 티키타카를 천천히 읽으면
        //   씬을 연 시각 기준으로는 이미 22초가 지나 <b>카운트 없이 출발</b>해 버린다.
        Arm();
    }

    static bool drawn;

    /// <summary><see cref="Begin"/> 을 부른 시각. 화면이 영영 안 그리면 여기서 재서 그냥 센다.</summary>
    static float beganAt = -99f;

    /// <summary>
    /// 화면이 숫자를 못 그릴 때의 바닥값(초). HUD 가 꺼져 있거나 <c>-nographics</c> 로 돌리면
    /// <see cref="Shown"/> 이 영영 안 불려 <b>22초 안전장치가 걸릴 때까지 카트가 묶인다.</b>
    /// 1.5초면 로딩 끊김은 다 지나가고 사람 눈에는 안 띈다 —
    /// 문 바닥값 · 브리핑 25초 · 급식 4초와 같은 판단이야. <b>못 하는 게 제일 나쁘다.</b>
    /// </summary>
    const float ShowStop = 1.5f;

    /// <summary>
    /// 화면에 <b>처음 그려지는 프레임</b>에 시계를 다시 맞춘다.
    ///
    /// ★★ 2026-10-06 유저: *"마지막 개발업자 레이싱에 321 안 세고 1에 그냥 시작하는데
    /// 이거 버그야?"* <b>버그 맞다.</b> <see cref="Begin"/> 과 첫 렌더 사이에 <b>프레임이
    /// 길게 멈춘다</b> — 결승은 그 순간에 악당 카트 둘을 처음 켜고(새 FBX · 새 셰이더 변종),
    /// 관중 26마리와 비행선·헬기를 한꺼번에 읽는다. 셰이더 컴파일은 몇 초씩 걸리는데
    /// <c>Time.unscaledTime</c> 은 그동안에도 흐르니 <b>3 과 2 가 멈춰 있는 화면 뒤로 지나간다.</b>
    /// 그래서 결승에서만 «1» 부터 보였다.
    ///
    /// 고치는 자리는 «언제 세기 시작하나» 가 아니라 <b>«언제 보이기 시작하나»</b> 다.
    /// 보이는 첫 프레임을 0초로 삼으면 어떤 로딩 지연에도 3 · 2 · 1 이 전부 보인다.
    /// </summary>
    public static void Shown()
    {
        // ★ <b>시작 선언이 없으면 아무 일도 안 한다.</b> 이 한 줄이 «소리만 나고 또 한 번» 의 답이야.
        if (!running || drawn) return;
        drawn = true;
        startedAt = Time.unscaledTime;
    }

    /// <summary>
    /// <b>길게 멈춘 프레임은 안 센다.</b> <see cref="Shown"/> 만으로는 모자랐다 —
    /// 끊김이 <b>카운트 도중에</b> 오면(결승은 악당 카트 둘의 셰이더를 그때 굽는다)
    /// 이미 세기 시작한 뒤라 3 과 2 를 그대로 뺏긴다.
    ///
    /// 한 프레임이 0.25초를 넘으면 그건 «시간이 흐른 것» 이 아니라 <b>«화면이 멈춰 있던 것»</b>
    /// 이라, 그만큼 시작 시각을 뒤로 민다. 그러면 <b>숫자 셋이 전부 보인다.</b>
    /// 매 프레임 <see cref="RaceBriefing.Tick"/> 가 부른다.
    /// </summary>
    public static void Tick()
    {
        // 화면이 못 그려도 1.5초 뒤엔 그냥 센다
        if (running && !drawn)
        {
            if (Time.unscaledTime - beganAt > ShowStop) Shown();
            return;
        }

        if (startedAt < 0f || Elapsed > Seconds + GoSeconds) return;
        float dt = Time.unscaledDeltaTime;
        if (dt > 0.25f) startedAt += dt - 1f / 60f;
    }

    /// <summary>검사용 — 카운트를 건너뛴다.</summary>
    public static void Skip()
    {
        running = true;
        drawn = true;
        startedAt = Time.unscaledTime - 99f;   // 이미 다 끝난 것으로 친다
    }

    /// <summary>
    /// 흐른 시간. <b>세 가지 상태가 있다</b> — 아직 시작 선언이 없거나(−999),
    /// 선언은 했는데 화면에 아직 안 떴거나(−1), 세는 중이거나.
    /// 앞의 둘은 <c>Label</c> 이 빈 문자열이고 <c>Blocked</c> 는 참이다
    /// (카트는 묶여 있고 숫자는 안 보인다 — 브리핑 카드가 떠 있는 동안의 모습 그대로다).
    /// </summary>
    static float Elapsed => !running ? -999f
                          : !drawn ? -1f
                          : Time.unscaledTime - startedAt;

    /// <summary>
    /// 아직 못 움직이나.
    ///
    /// ★ <b>브리핑 카드가 떠 있는 동안도 묶는다.</b> 여기 한 줄로 막아야
    /// 이미 이걸 보고 있는 <b>네 군데</b>(플레이어 입력 · AI 의 Drive · 랩 시계 · 카트 물리)가
    /// 한꺼번에 따라온다 — 막는 곳을 따로 늘리면 반드시 한 군데를 빠뜨린다
    /// (<c>MissionManager.AllDone</c> 에 결승 조건을 넣었더니 HUD 네 군데가 같이 따라온 것과 같다).
    /// </summary>
    /// <summary>
    /// 카트가 아직 못 움직이는가. <b>카드 → 티키타카 → 숫자</b> 셋 다 여기서 막는다 —
    /// 막는 곳을 늘리면 반드시 한 군데를 빠뜨린다(이미 넷이 이 값 하나만 본다).
    /// </summary>
    public static bool Blocked =>
        !GaveUp && (RaceBriefing.Open || RaceBriefing.Chatting || Elapsed < Seconds);

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
