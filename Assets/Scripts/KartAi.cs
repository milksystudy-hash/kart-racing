using UnityEngine;

/// <summary>
/// AI 카트 운전수. <b>트랙 곡선을 따라가기만 한다</b> — 길찾기도 NavMesh 도 없다.
///
/// 이게 싸게 되는 이유는 <see cref="TrackBuilder.PointOnPath"/> 가 이미 있어서야.
/// "지금 내가 코스의 어디쯤인지" 를 한 번 찾고, 그보다 조금 앞을 보고 핸들을 꺾으면 끝.
/// 코스 모양을 바꿔도 이 스크립트는 안 고쳐도 된다.
///
/// <b>플레이어와 같은 KartController 를 쓴다.</b> 물리도 드리프트도 서스펜션도 같은 코드야 —
/// 그래야 AI 가 사람이 못 하는 움직임을 안 하고, 카트를 손보면 AI 도 같이 따라온다.
/// AI 는 <see cref="KartController.Drive"/> 로 "악셀 얼마, 핸들 얼마" 만 말한다.
///
/// 지금은 <b>만들어만 둔 상태</b>다(2026-09-16 유저). 임무에는 아직 안 걸려 있어 —
/// 초반부터 AI 와 겨루게 하지 말라는 요청이라, 달리기는 해도 이겨야 할 이유는 아직 없다.
/// </summary>
[RequireComponent(typeof(KartController))]
public class KartAi : MonoBehaviour
{
    [Header("코스")]
    public TrackBuilder track;

    [Tooltip("코스 가운데에서 이만큼 옆으로 비켜 달린다(-1 ~ +1). 넷이 겹쳐 달리지 않게")]
    [Range(-1f, 1f)] public float lane;

    [Header("실력")]
    // 2026-09-17 유저: "전체적으로 난이도가 좀 쉬운 것 같다." 0.80~0.89 였다.
    [Tooltip("1 이면 카트 성능을 다 쓴다. 낮추면 느긋해진다")]
    [Range(0.5f, 1f)] public float skill = 0.92f;

    [Tooltip("몇 미터 앞을 보고 핸들을 꺾을지. 짧으면 코너를 못 돌고, 길면 코너를 잘라먹는다")]
    public float lookAhead = 14f;

    [Tooltip("코너가 급하면 미리 속도를 줄인다. 0 이면 안 줄인다")]
    [Range(0f, 1f)] public float cornerCaution = 0.7f;

    [Tooltip("이 각도보다 급하게 꺾이면 드리프트를 건다(도)")]
    public float driftAngle = 32f;

    [Tooltip("코스를 따라 이만큼 못 나아가면 되돌린다(초). 후진으로도 못 빠져나오는 경우")]
    public float recoverAfter = 2.5f;

    KartController kart;
    float progress;          // 코스에서 지금 어디쯤인지 (0~1)
    float lastProgress;      // 마지막으로 "나아갔다" 고 인정한 지점
    float stuckFor;

    void Awake()
    {
        // <b>씬을 다시 굽지 않아도 꺼진다.</b> AiRaceGate 는 새로 붙는 컴포넌트라
        // 옛날에 구운 씬에는 없다 — 그래서 고쳐 놓고도 2/8 에서 AI 가 따라왔다
        // (2026-09-17 유저 제보, 두 번째). 카트가 스스로도 확인한다.
        if (!MissionManager.FinalRace && FindFirstObjectByType<AiRaceGate>() == null)
        {
            gameObject.SetActive(false);
            return;
        }

        kart = GetComponent<KartController>();
        kart.acceptPlayerInput = false;   // 키보드를 안 읽는다. 이게 없으면 플레이어와 같이 움직인다

        if (track == null) track = FindFirstObjectByType<TrackBuilder>();
        if (track == null)
            Debug.LogWarning("[AI] 트랙을 못 찾았어. TrackBuilder 를 인스펙터에 꽂아줘.", this);
    }

    void Start()
    {
        progress = NearestT(transform.position, 0f, 1f, 60);
        lastProgress = progress;
    }

    void Update()
    {
        if (track == null) return;

        // 내 위치를 다시 찾는다. 앞뒤로 조금씩만 뒤져서 — 코스 전체를 매번 뒤지면 비싸고,
        // 좁은 코스에서 맞은편 구간으로 잘못 붙을 수도 있다.
        progress = NearestT(transform.position, progress - 0.02f, progress + 0.06f, 12);

        float ahead = progress + lookAhead / Mathf.Max(1f, track.LapLength);
        Vector3 target = LanePoint(ahead);

        Vector3 flat = target - transform.position;
        flat.y = 0f;

        // 목표 방향과 지금 보는 방향의 차이가 곧 핸들이다
        float angle = Vector3.SignedAngle(transform.forward, flat.normalized, Vector3.up);
        float steer = Mathf.Clamp(angle / 28f, -1f, 1f);

        // 더 앞쪽이 얼마나 꺾이는지 보고 미리 속도를 줄인다
        float bend = Vector3.Angle(track.TangentOnPath(Wrap(ahead)),
                                   track.TangentOnPath(Wrap(ahead + 0.035f)));
        float ease = 1f - Mathf.Clamp01(bend / 45f) * cornerCaution;
        float throttle = Mathf.Clamp01(skill * ease);

        bool drift = Mathf.Abs(angle) > driftAngle && kart.SpeedKph > 18f;

        // <b>속도로 보면 안 된다.</b> 벽에 대고 악셀을 밟으면 제자리에서 덜덜거리며
        // 3~5km/h 가 찍혀서 "달리는 중" 으로 판정된다 — 그래서 되돌리기가 영영 안 걸리고
        // AI 가 경기를 포기한 것처럼 보였다(2026-09-17 유저 제보, 두 번째).
        // <b>코스를 따라 실제로 나아갔는지</b>를 본다. 벽에 박혀 있으면 이 값이 안 움직인다.
        float moved = Mathf.Abs(Mathf.DeltaAngle(lastProgress * 360f, progress * 360f)) / 360f;
        if (moved * track.LapLength < 0.35f) stuckFor += Time.deltaTime;
        else { stuckFor = 0f; lastProgress = progress; }

        // <b>후진으로도 못 나오면 코스 위로 되돌린다.</b> 유저 제보(2026-09-17):
        // 코너에 밀어 넣으면 AI 가 바닥을 뒤뚱거리며 박힌 채로 끝났다. 뒤집히거나 끼면
        // 후진만으로는 영영 못 나온다. <b>AI 도 완주는 해야 한다</b> — 한 대가 코스 밖에
        // 서 있으면 순위판이 거짓말이 되고, 마지막 판에 AI 와 겨루게 될 때 그게 무너진다.
        if (stuckFor > recoverAfter) { PutBackOnTrack(); return; }

        if (stuckFor > 1.2f)
        {
            kart.Drive(-1f, -steer, false);
            return;
        }

        kart.Drive(throttle, steer, drift);
    }

    /// <summary>
    /// 코스 위 <b>조금 앞</b>으로 되돌린다. 있던 자리에 그대로 세우면 끼었던 곳에 다시 낀다.
    /// 플레이어 카트에는 이걸 안 한다 — 사람은 R 로 직접 부르고, 저절로 옮겨지면 황당하다.
    /// </summary>
    void PutBackOnTrack()
    {
        stuckFor = 0f;

        // <b>사람이 막고 있으면 되돌리지 않는다.</b> 그러면 막은 보람이 없어진다 —
        // 유저 제보(2026-09-17): 일부러 막았더니 버벅거리다 <b>앞으로 순간이동해서</b> 가버렸다.
        // 막혀서 못 가는 건 벌이 아니라 결과여야 한다.
        if (PlayerNear(8f)) return;

        // <b>제자리에</b> 세운다. 앞으로 옮기면 끼었다가 오히려 이득을 본다.
        // 끼었던 자리에 다시 끼는 건 방향을 코스 쪽으로 돌려주는 것으로 푼다.
        Vector3 at = LanePoint(progress) + Vector3.up * 0.6f;
        Vector3 forward = track.TangentOnPath(Wrap(progress));

        kart.RespawnAt(at, Quaternion.LookRotation(forward, Vector3.up));
        lastProgress = progress;
    }

    /// <summary>사람이 탄 카트가 이 거리 안에 있나.</summary>
    bool PlayerNear(float range)
    {
        foreach (var mark in FindObjectsByType<PlayerKart>(FindObjectsSortMode.None))
            if ((mark.transform.position - transform.position).sqrMagnitude < range * range) return true;
        return false;
    }

    /// <summary>코스 위 t 지점에서 내 차선만큼 옆으로 비킨 자리.</summary>
    Vector3 LanePoint(float t)
    {
        t = Wrap(t);
        Vector3 side = Vector3.Cross(Vector3.up, track.TangentOnPath(t));
        return track.transform.position + track.PointOnPath(t)
             + side * (lane * track.WidthOnPath(t) * 0.32f);
    }

    /// <summary>주어진 구간에서 나와 제일 가까운 코스 위치를 찾는다.</summary>
    float NearestT(Vector3 from, float lo, float hi, int steps)
    {
        float best = lo, bestDistance = float.MaxValue;
        for (int i = 0; i <= steps; i++)
        {
            float t = Mathf.Lerp(lo, hi, i / (float)steps);
            float d = (track.transform.position + track.PointOnPath(Wrap(t)) - from).sqrMagnitude;
            if (d >= bestDistance) continue;
            bestDistance = d;
            best = t;
        }
        return best;
    }

    static float Wrap(float t) => t - Mathf.Floor(t);
}
