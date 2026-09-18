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
            // 꺼진 것도 담는다. 안 그러면 꺼놓은 판 다음에 목록이 비어서 되살릴 수가 없다.
            all = FindObjectsByType<RoadDebris>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return all;
    }

    Vector3 home;
    Quaternion homeRotation;
    Rigidbody body;
    bool counted;

    void Awake()
    {
        // 게이트가 없는 씬(옛날에 구운 것)에서도 스스로 꺼진다.
        if (!MissionManager.WantsDebris && FindFirstObjectByType<DebrisGate>() == null)
        {
            gameObject.SetActive(false);
            return;
        }

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
        counted = false;   // 바퀴가 넘어가면 이 자재는 다시 셀 수 있다

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(home, homeRotation);
    }

    /// <summary>
    /// <b>한 자재는 한 바퀴에 한 번만 세진다.</b> 2026-09-18 유저:
    /// *"한 개를 밀치고 쭉 이어 나가는데 숫자가 자동으로 깎이는 게 이상한데."*
    ///
    /// 맞는 지적이다. 전에는 <b>0.5초마다 다시</b> 셌다 — 드럼통을 앞에 끼고 밀면서
    /// 달리면 <b>한 번 친 실수가 세 번 네 번으로 불어났다.</b> 그건 플레이어가 한 행동이
    /// 아니라 물리 엔진이 센 숫자야.
    ///
    /// 세는 기준은 <b>"몇 개를 건드렸나"</b> 여야 한다. "몇 번 닿았나" 가 아니라.
    /// 벽 부딪힘을 0.7초 쿨다운으로 묶은 것과 같은 판단이고, 여기는 물건마다 따로 있으니
    /// 시간이 아니라 <b>물건 한 개당 한 번</b>으로 묶는 게 더 정확하다.
    ///
    /// 밀려서 굴러온 게 나중에 다시 닿아도 안 세진다 — 그게 제일 억울한 경우였어.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (counted) return;

        var kart = collision.rigidbody != null ? collision.rigidbody.GetComponent<KartController>() : null;
        if (kart == null || kart.GetComponent<PlayerKart>() == null) return;
        if (Mathf.Abs(kart.SpeedKph) < minSpeedKph) return;

        counted = true;
        Hits++;
    }
}
