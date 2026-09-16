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
    [Tooltip("1 이면 카트 성능을 다 쓴다. 낮추면 느긋해진다")]
    [Range(0.5f, 1f)] public float skill = 0.85f;

    [Tooltip("몇 미터 앞을 보고 핸들을 꺾을지. 짧으면 코너를 못 돌고, 길면 코너를 잘라먹는다")]
    public float lookAhead = 14f;

    [Tooltip("코너가 급하면 미리 속도를 줄인다. 0 이면 안 줄인다")]
    [Range(0f, 1f)] public float cornerCaution = 0.7f;

    [Tooltip("이 각도보다 급하게 꺾이면 드리프트를 건다(도)")]
    public float driftAngle = 32f;

    KartController kart;
    float progress;          // 코스에서 지금 어디쯤인지 (0~1)
    float stuckFor;

    void Awake()
    {
        kart = GetComponent<KartController>();
        kart.acceptPlayerInput = false;   // 키보드를 안 읽는다. 이게 없으면 플레이어와 같이 움직인다

        if (track == null) track = FindFirstObjectByType<TrackBuilder>();
        if (track == null)
            Debug.LogWarning("[AI] 트랙을 못 찾았어. TrackBuilder 를 인스펙터에 꽂아줘.", this);
    }

    void Start() => progress = NearestT(transform.position, 0f, 1f, 60);

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

        // 벽에 붙어서 못 나가면 후진해서 뺀다. 안 그러면 한 대가 영영 거기 있는다.
        if (Mathf.Abs(kart.SpeedKph) < 2f) stuckFor += Time.deltaTime;
        else stuckFor = 0f;

        if (stuckFor > 1.2f)
        {
            kart.Drive(-1f, -steer, false);
            if (stuckFor > 2.4f) stuckFor = 0f;
            return;
        }

        kart.Drive(throttle, steer, drift);
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
