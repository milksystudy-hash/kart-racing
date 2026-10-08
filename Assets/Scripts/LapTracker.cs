using UnityEngine;

/// <summary>
/// 플레이어의 랩타임과 완주 판정. 랩을 세는 일 자체는 <see cref="RaceProgress"/> 가 하고,
/// 여기서는 그 값이 바뀌는 순간을 지켜보며 시간을 찍는다.
///
/// 왜 나눠뒀냐면 — AI 카트도 랩을 세야 순위가 나오는데, 시계는 플레이어에게만 필요하다.
/// 한 스크립트가 둘 다 하면 카트가 늘어날 때마다 꼬인다.
/// </summary>
public class LapTracker : MonoBehaviour
{
    [Header("연결")]
    public KartController kart;
    [Tooltip("플레이어 카트의 RaceProgress. 비워두면 kart 에서 찾는다")]
    public RaceProgress progress;

    [Header("규칙")]
    public int checkpointCount = 12;
    public int totalLaps = 3;

    [Header("추락 처리")]
    [Tooltip("이 높이보다 아래로 떨어지면 마지막 체크포인트로 되돌린다")]
    public float killPlaneY = -8f;

    // --- HUD 가 읽어가는 값 ---
    public int CurrentLap => progress != null ? progress.Lap : 1;
    public bool Finished => progress != null && progress.Finished;
    public float LapTime { get; private set; }
    public float BestLapTime { get; private set; } = -1f;
    public float LastLapTime { get; private set; } = -1f;
    public float TotalTime { get; private set; }

    int watchedLap = 1;
    bool watchedFinished;

    void Awake()
    {
        if (progress == null && kart != null) progress = kart.GetComponent<RaceProgress>();

        if (progress == null)
        {
            // 이게 없으면 랩이 영원히 1 에 머문다. 씬이 낡아서 카트에 부품이 안 붙은 경우인데,
            // 아무 말도 없으면 "코드가 고장났나" 로 보인다. 실제로 그렇게 한 번 헤맸다.
            Debug.LogWarning("[레이스] 카트에 RaceProgress 가 없어서 랩이 안 세어져. " +
                             "이 씬은 RaceProgress 가 생기기 전에 저장된 거야 — " +
                             "메뉴 Racing → 테스트 씬 두 개 다시 만들기 를 누르면 고쳐진다.", this);
            return;
        }

        progress.totalCheckpoints = checkpointCount;
        progress.totalLaps = totalLaps;
        watchedLap = progress.Lap;

        // ★★ 2026-10-06 <b>씬을 넘어 살아남는 전역값을 여기서 싹 푼다.</b>
        //
        // <c>Time.timeScale</c>(미니게임 일시정지) · <c>Physics.simulationMode</c>(레이스
        // 일시정지) · <see cref="ItemReveal"/>(획득 연출) 은 전부 <b>static 이거나 엔진 전역</b>이라
        // 씬을 갈아타도 안 풀린다. 어느 한 군데서 푸는 걸 빠뜨리면 <b>다음 레이스가
        // 멈춘 채로 시작하고</b>, 그건 «먹통» 으로 보이지 로그에는 아무것도 안 남는다.
        //
        // 푸는 자리를 늘리는 대신 <b>레이스가 시작되는 이 한 곳에서 무조건 제자리로</b> 돌린다 —
        // 「못 푸는 게 제일 나쁘다」(문 바닥값 · 브리핑 25초 · 급식 4초 안전장치와 같은 판단).
        Time.timeScale = 1f;
        RacePause.Clear();
        ItemReveal.Clear();

        // 씬에 들어올 때는 <b>브리핑부터</b>. 처음 보는 임무면 카드를 띄우고, 이미 본 임무면
        // 그대로 카운트다운으로 넘어간다 (RaceBriefing 이 알아서 고른다).
        // ★ <b>지난 판의 값을 먼저 버린다.</b> 안 그러면 2차 진입 첫 프레임에
        //   카운트다운의 바닥값이 «7초 전» 과 비교되어 즉시 터지고, 숫자가 0.7초 만에
        //   지나간 뒤 Begin() 이 그걸 지운다 — 화면에는 아무것도 안 뜬 걸로 보인다.
        RaceCountdown.Forget();
        RaceCountdown.Arm();   // 35초 안전장치의 기준 시각
        RaceBriefing.Begin();
    }

    void Update()
    {
        if (progress == null) return;

        // 카운트 중에는 시계도 안 간다. 안 그러면 제한시간 판이 3초를 손해 본다.
        // ESC 로 멈춘 동안도 마찬가지 — <b>고민하는 시간이 기록에 들어가면 안 된다.</b>
        RaceBriefing.Tick();
        if (RaceCountdown.Blocked || RacePause.On) return;

        if (!progress.Finished)
        {
            LapTime += Time.deltaTime;
            TotalTime += Time.deltaTime;
        }

        // 랩이 넘어간 순간을 잡아서 시간을 찍는다
        if (progress.Lap != watchedLap)
        {
            watchedLap = progress.Lap;
            RecordLap();
        }
        else if (progress.Finished && !watchedFinished)
        {
            watchedFinished = true;
            RecordLap();
            // ★★ 2026-10-07 수연: *"임무 실패한 채 결승선에 들어오면 결승선 소리가 나지
            //   실패 소리가 안 난다."* 맞다 — 둘이 <b>같은 프레임에 같이</b> 울렸고,
            //   3.5초짜리 팡파레가 1.6초짜리 트럼펫을 덮었다.
            //
            //   고치는 자리는 음량이 아니라 <b>«실패한 판에 팡파레를 틀지 마라»</b> 다.
            //   「잘했다」 소리를 「임무 실패」 글자 위에 깔면 그건 버그보다 나쁘다.
            //   <b>한 프레임 늦춰서</b> 판정이 끝난 뒤에 고른다 — MissionManager 의 Update 가
            //   LapTracker 보다 먼저 도는지 나중에 도는지는 정해져 있지 않다(이 프로젝트에서
            //   실행 순서로 다섯 번 틀렸다).
            finishSoundAt = Time.unscaledTime;
        }

        if (finishSoundAt > 0f && Time.unscaledTime > finishSoundAt)
        {
            finishSoundAt = -1f;
            var judge = FindFirstObjectByType<MissionManager>();
            // 실패했으면 <see cref="MissionManager.Fail"/> 이 이미 트럼펫을 울렸다.
            if (judge == null || !judge.Failed)
                Sfx.Play("Finish");  // 결승선 — 음악이 잠깐 비켜 준다(duckMusic 기본값)
        }

        if (kart != null && kart.transform.position.y < killPlaneY)
            RespawnAtLastCheckpoint();
    }

    /// <summary>결승선 소리를 낼 시각. 판정이 끝난 <b>다음 프레임</b>에 «무슨 소리» 인지 정한다.</summary>
    float finishSoundAt = -1f;

    void RecordLap()
    {
        LastLapTime = LapTime;
        if (BestLapTime < 0f || LapTime < BestLapTime) BestLapTime = LapTime;
        LapTime = 0f;
    }

    // 코스 밖으로 떨어진 것과 R 키는 <b>같은 사고</b>다 — 처리도 한 군데에 둔다
    // (`KartController.RespawnToCourse`). 둘을 따로 두면 한쪽만 고쳐놓고 놓친다.
    void RespawnAtLastCheckpoint()
    {
        if (kart != null) kart.RespawnToCourse();
    }

    public void ResetRace()
    {
        LapTime = 0f;
        TotalTime = 0f;
        BestLapTime = -1f;
        LastLapTime = -1f;
        watchedLap = 1;
        watchedFinished = false;

        if (progress != null) progress.ResetRace();
        if (kart != null) kart.Respawn();

        // ★ <b>임무를 먼저, 순위판을 나중에.</b> 임무가 게이트를 다시 맞추면서
        // AI 카트를 켠다 — 순위판을 먼저 돌리면 <b>그때 꺼져 있던 AI 는 출발선으로
        // 안 돌아가고</b> 아까 멈춘 자리에서 다시 달린다(2026-09-17 유저 제보).
        var mission = FindFirstObjectByType<MissionManager>();
        if (mission != null) mission.Restart();

        var standings = FindFirstObjectByType<RaceStandings>();
        if (standings != null) standings.ResetRace();

        // 3 · 2 · 1 · 출발! 되돌려 놓은 카트가 바닥에 내려앉을 시간이기도 하다.
        //
        // ENTER 로 <b>같은 판을 다시</b> 하는 경우라 브리핑은 대개 안 뜬다 — 방금 읽은 카드가
        // 또 나오면 안내가 아니라 장애물이다. 상품을 받아서 임무가 바뀌었을 때만 뜬다.
        RaceCountdown.Arm();   // 35초 안전장치의 기준 시각
        RaceBriefing.Begin();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--.--";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:0}:{s:00.00}";
    }
}
