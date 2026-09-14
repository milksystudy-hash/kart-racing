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
        if (progress != null)
        {
            progress.totalCheckpoints = checkpointCount;
            progress.totalLaps = totalLaps;
            watchedLap = progress.Lap;
        }
    }

    void Update()
    {
        if (progress == null) return;

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

    void RespawnAtLastCheckpoint()
    {
        if (kart == null) return;

        var last = progress != null ? progress.LastPassed : null;
        if (last != null && last.respawnPoint != null)
            kart.RespawnAt(last.respawnPoint.position + Vector3.up * 0.6f, last.respawnPoint.rotation);
        else
            kart.Respawn();
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

        var standings = FindFirstObjectByType<RaceStandings>();
        if (standings != null) standings.ResetRace();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--.--";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:0}:{s:00.00}";
    }
}
