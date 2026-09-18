using UnityEngine;

/// <summary>
/// 카트 한 대가 코스를 얼마나 갔는지. 플레이어 카트와 AI 카트 모두 이걸 하나씩 달고 다닌다.
///
/// 순위는 이 값을 비교해서 정한다 — 체크포인트를 몇 개 지났는지가 먼저고,
/// 같으면 다음 체크포인트에 누가 더 가까운지로 가른다.
///
/// **맵이 바뀌어도 이 코드는 안 고친다.** 체크포인트 오브젝트가 어디 놓이든 번호만 맞으면
/// 그대로 동작하니까, 네가 직접 만든 트랙으로 갈아끼워도 순위 계산은 따라온다.
/// </summary>
public class RaceProgress : MonoBehaviour
{
    [Header("규칙")]
    public int totalCheckpoints = 12;
    public int totalLaps = 3;

    [Tooltip("화면에 표시할 이름. 비워두면 오브젝트 이름을 쓴다")]
    public string racerName = "";

    public int Lap { get; private set; } = 1;
    public int NextCheckpoint { get; private set; } = 1;
    /// <summary>출발선부터 지금까지 지난 체크포인트 총 개수. 랩을 넘어가도 계속 쌓인다.</summary>
    public int CheckpointsPassed { get; private set; }
    public bool Finished { get; private set; }

    /// <summary>
    /// <b>몇 번째로 결승선을 넘었나</b>(1부터). 아직 안 끝났으면 0.
    ///
    /// 2026-09-18 유저: *"내가 1등이었는데 뒤에 있던 AI 가 나를 밟으니 알림판이 2등으로 바뀌었다."*
    /// 순위를 <b>매 프레임 위치로</b> 매기니까 완주한 뒤에도 계속 흔들렸다.
    /// 결승선을 넘은 <b>순서</b>는 그때 정해지는 값이지 나중에 바뀌는 값이 아니야.
    /// </summary>
    public int FinishOrder { get; private set; }

    static int finishedSoFar;
    public Checkpoint LastPassed { get; private set; }

    public string DisplayName => string.IsNullOrEmpty(racerName) ? name : racerName;

    /// <summary>순위를 매길 때 쓰는 값. 클수록 앞서 있다.</summary>
    public float RankScore
    {
        get
        {
            float score = CheckpointsPassed;

            // 같은 체크포인트를 지난 카트끼리는 다음 관문에 가까운 쪽이 앞선다.
            // 0~1 사이 값이라 체크포인트 하나를 넘어서지 않는다.
            var next = Checkpoint.ByIndex(NextCheckpoint);
            if (next != null)
            {
                float distance = Vector3.Distance(transform.position, next.transform.position);
                score += 1f / (1f + distance);
            }
            return score;
        }
    }

    /// <summary>체크포인트가 순서대로 통과됐을 때만 부른다. 역주행이나 지름길은 여기 안 온다.</summary>
    public void PassCheckpoint(int index, Checkpoint checkpoint)
    {
        if (Finished) return;
        if (index != NextCheckpoint) return;   // 순서가 안 맞으면 무시

        LastPassed = checkpoint;
        CheckpointsPassed++;
        NextCheckpoint = (NextCheckpoint + 1) % Mathf.Max(1, totalCheckpoints);

        // 0번(결승선)을 제대로 밟고 지나갔다면 한 바퀴 완주
        if (index != 0) return;

        if (Lap >= totalLaps)
        {
            Finished = true;
            FinishOrder = ++finishedSoFar;   // 넘은 순서대로 1, 2, 3...
        }
        else Lap++;
    }

    public void ResetRace()
    {
        // 한 대라도 처음으로 돌아가면 순서를 다시 센다. 첫 카트가 부를 때 0 이 되고
        // 나머지는 이미 0 이라 영향이 없다.
        finishedSoFar = 0;
        FinishOrder = 0;
        Lap = 1;
        NextCheckpoint = 1;
        CheckpointsPassed = 0;
        Finished = false;
        LastPassed = null;
    }
}
