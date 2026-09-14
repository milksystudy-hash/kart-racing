using UnityEngine;

/// <summary>
/// 랩 카운트와 랩타임. 체크포인트를 순서대로 통과해야만 인정되기 때문에
/// 역주행이나 지름길로 랩을 올릴 수 없다.
/// </summary>
public class LapTracker : MonoBehaviour
{
    [Header("규칙")]
    public int checkpointCount = 8;
    public int totalLaps = 3;

    [Header("추락 처리")]
    [Tooltip("이 높이보다 아래로 떨어지면 마지막 체크포인트로 되돌린다")]
    public float killPlaneY = -8f;

    public KartController kart;

    // --- HUD 가 읽어가는 값 ---
    public int CurrentLap { get; private set; } = 1;
    public int NextCheckpoint { get; private set; } = 1;
    public float LapTime { get; private set; }
    public float BestLapTime { get; private set; } = -1f;
    public float TotalTime { get; private set; }
    public bool Finished { get; private set; }
    public float LastLapTime { get; private set; } = -1f;

    Checkpoint lastPassed;

    void Update()
    {
        if (Finished) return;

        LapTime += Time.deltaTime;
        TotalTime += Time.deltaTime;

        if (kart != null && kart.transform.position.y < killPlaneY)
            RespawnAtLastCheckpoint();
    }

    public void PassCheckpoint(int index, Checkpoint checkpoint)
    {
        if (Finished) return;

        // 순서가 안 맞으면 무시한다
        if (index != NextCheckpoint) return;

        lastPassed = checkpoint;
        NextCheckpoint = (NextCheckpoint + 1) % Mathf.Max(1, checkpointCount);

        // 0번(결승선)을 제대로 밟고 지나갔다면 한 바퀴 완주
        if (index == 0) CompleteLap();
    }

    void CompleteLap()
    {
        LastLapTime = LapTime;
        if (BestLapTime < 0f || LapTime < BestLapTime) BestLapTime = LapTime;
        LapTime = 0f;

        if (CurrentLap >= totalLaps) Finished = true;
        else CurrentLap++;
    }

    void RespawnAtLastCheckpoint()
    {
        if (kart == null) return;

        if (lastPassed != null && lastPassed.respawnPoint != null)
            kart.RespawnAt(lastPassed.respawnPoint.position + Vector3.up * 0.6f,
                           lastPassed.respawnPoint.rotation);
        else
            kart.Respawn();
    }

    public void ResetRace()
    {
        CurrentLap = 1;
        NextCheckpoint = 1;
        LapTime = 0f;
        TotalTime = 0f;
        BestLapTime = -1f;
        LastLapTime = -1f;
        Finished = false;
        lastPassed = null;
        if (kart != null) kart.Respawn();
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) return "--:--.--";
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:0}:{s:00.00}";
    }
}
