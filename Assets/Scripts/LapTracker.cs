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

        RaceCountdown.Begin();   // 씬에 들어올 때도 카운트부터
    }

    void Update()
    {
        if (progress == null) return;

        // 카운트 중에는 시계도 안 간다. 안 그러면 제한시간 판이 3초를 손해 본다.
        if (RaceCountdown.Blocked) return;

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
        }

        if (kart != null && kart.transform.position.y < killPlaneY)
            RespawnAtLastCheckpoint();
    }

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
        RaceCountdown.Begin();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--.--";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:0}:{s:00.00}";
    }
}
