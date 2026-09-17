using UnityEngine;

/// <summary>
/// 길에 널브러진 <b>철거 자재</b>. 드럼통과 파이프 — 치면 날아가고 <b>한 대 세진다.</b>
///
/// 2026-09-17 유저: *"멈추지 않고 완주는 너무 쉽다. 차라리 그 판에만 방해물이 나뒹굴고
/// 그걸 뚫고 나가면 모를까."* 맞는 지적이야 — 멈추지 않는 건 <b>안 하면 되는 일</b>이라
/// 어렵지가 않다. 피해야 할 게 길 위에 있어야 판이 어려워진다.
///
/// 벽과 다른 점이 둘이다:
/// <b>날아간다</b>(벽은 안 움직인다) — 그래서 치고 지나갈 수는 있고, 대신 속도를 잃는다.
/// <b>매 바퀴 되살아난다</b> — 첫 바퀴에 다 치워버리면 두세 바퀴가 그냥 완주가 된다.
///
/// 임무가 끝나면 씬에서 <b>꺼진다</b>(<see cref="DebrisGate"/>). 매 판 널려 있으면
/// 그 판만의 성격이 사라진다.
/// </summary>
public class RoadDebris : MonoBehaviour
{
    [Tooltip("이 속도(㎞/h) 아래로 스치면 안 센다 — 굴러온 게 닿는 것까지 세면 억울하다")]
    public float minSpeedKph = 14f;

    /// <summary>사람이 친 횟수. 임무가 이걸 본다.</summary>
    public static int Hits { get; private set; }

    public static void ResetHits() => Hits = 0;

    static RoadDebris[] all;

    public static int CountInScene() => All().Length;

    /// <summary>바퀴가 넘어갈 때 제자리로. 안 그러면 첫 바퀴에 다 치워진다.</summary>
    public static void RestoreAll()
    {
        foreach (var piece in All()) if (piece != null) piece.Restore();
    }

    static RoadDebris[] All()
    {
        if (all == null || all.Length == 0 || all[0] == null)
            all = FindObjectsByType<RoadDebris>(FindObjectsSortMode.None);
        return all;
    }

    Vector3 home;
    Quaternion homeRotation;
    Rigidbody body;
    float lastHitAt = -99f;

    void Awake()
    {
        home = transform.position;
        homeRotation = transform.rotation;

        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();

        // 가볍게. 무거우면 카트가 벽을 받은 것처럼 멈춰 서고, 그건 장애물이 아니라 벽이다.
        body.mass = 6f;
        body.linearDamping = 0.6f;
        body.angularDamping = 0.4f;
    }

    void Restore()
    {
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(home, homeRotation);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastHitAt < 0.5f) return;

        var kart = collision.rigidbody != null ? collision.rigidbody.GetComponent<KartController>() : null;
        if (kart == null || kart.GetComponent<PlayerKart>() == null) return;
        if (Mathf.Abs(kart.SpeedKph) < minSpeedKph) return;

        lastHitAt = Time.time;
        Hits++;
    }
}
