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

    /// <summary>
    /// ★ 2026-10-12 — <b>드론이 떨굴 슬래브에 맞은 것도 «자재» 다.</b>
    /// 수연: *"드론이 떨어뜨리는 자재도 자재로 쳐야 하는데 기회 횟수가 안 줄어들어."*
    /// 맞는 지적이다 — 화면에서는 둘 다 «길에 떨어진 철거 자재» 인데
    /// 한쪽만 세면 플레이어는 규칙을 못 배운다.
    /// (슬래브는 콜라이더가 없어서 <see cref="OnCollisionEnter"/> 를 안 타다 —
    ///  떨어지는 순간 거리로 판정한다. <c>SkyDrop.Land</c> 가 이걸 부른다.)
    /// </summary>
    public static void CountSkyHit() => Hits++;

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

        // ★★ 2026-10-14 (두 번째) — <b>바닥 맞추기를 여기서 한 번만 한다.</b>
        //   어제는 <see cref="Restore"/> 안에서 바닥을 쟀는데, 그러면 되돌린 자리와
        //   <see cref="home"/> 이 서로 달라진다. 아래 <see cref="FixedUpdate"/> 는
        //   <b>home 에서 얼마나 벗어났나</b>로 판단하니, 둘이 1.2m 넘게 차이 나면
        //   되돌리자마자 또 «벗어났다» 가 되어 <b>영원히 되돌리기만 반복</b>한다 —
        //   그게 「떨어지다가 다시 위로 챱 붙는다」의 정체다.
        //   기준점 자체를 바닥에 맞춰 두면 둘이 같은 값이라 안 싸운다.
        // ★★★ 2026-10-15 (세 번째) — <b>바닥을 재던 광선이 제 몸을 맞고 있었다.</b>
        //
        //   수연이 «자재가 공중에 떠 있다» 를 세 번 신고했고, 세 번 다 이것이었다.
        //   위에서 아래로 쏘면 <b>제 윗면이 제일 먼저</b> 걸린다. 그 높이를 «바닥» 으로
        //   읽고 거기에 반높이를 또 더하니 <b>제 키만큼 솟는다.</b> 되돌릴 때마다
        //   그 자리로 돌아가니 영영 공중이다.
        //
        //   ★ 레이어로도 안 걸러졌다 — 아래 <c>layer = 2</c>(Ignore Raycast)가
        //     <b>이 줄보다 나중</b>이라, 재는 시점에는 아직 Default 였다.
        //     순서를 바꾸는 것만으로도 되지만, 그러면 «순서를 지켜야 맞는 코드» 가 된다.
        //     <see cref="Ground"/> 가 <b>자기 자신을 빼고</b> 재니 순서에 안 흔들린다.
        //
        //   ★ 올리는 양도 반높이가 아니라 <b>원점에서 메시 맨 밑까지</b>다 —
        //     원점이 가운데인 자재와 바닥인 자재가 섞여 있어도 둘 다 맞는다.
        gameObject.layer = 2;      // 재기 전에 먼저 — 광선이든 서스펜션이든 안 걸린다

        homeRotation = transform.rotation;
        Settle(transform.position);

        // ★★ 2026-10-08 — <b>카트가 자재를 타고 넘어가던 것.</b> 수연: *"자재 넘으면 바로 몸이
        //   통과되니까 못 넘어가게 막아야 할 듯."* 통과한 게 아니라 <b>올라탄 것</b>이었다.
        //
        //   이 카트는 바퀴가 아니라 <b>레이캐스트 서스펜션</b>으로 떠 있다(groundMask).
        //   자재가 기본 레이어라 그 광선에 걸리고, 그러면 서스펜션이 <b>자재를 바닥으로 보고</b>
        //   카트를 그 높이까지 밀어 올린다 — 드럼통 위로 스르륵 올라가 버린다.
        //
        //   <b>Ignore Raycast(2)로 보내면 광선만 안 맞고 충돌은 그대로</b>다.
        //   그래서 올라타는 대신 <b>부딪혀서 막힌다</b> — 수연이 원한 그 동작이야.
        //   카트 자신이 레이어 2 에 있는 것과 같은 이유고, 같은 해결이다.
        //   (★ 실제로 거는 자리는 위로 옮겼다 — 바닥을 재기 <b>전</b>이어야 한다.)

        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();

        // ★ <b>가볍고 덜 끌리면 날아간다.</b> 20m/s 로 받힌 6kg 짜리는 벽 메시 안으로 깊이
        //   파고들고, 유니티는 파묻힌 물체를 <b>초당 10까지만</b> 밀어내서(기본 디페네트레이션
        //   속도) 그게 «공중에 박힌 것» 으로 보인다. 무겁고 끈적하게 바꿔 아예 안 날아가게 한다.
        body.mass = 26f;
        body.linearDamping = 1.5f;
        body.angularDamping = 1.6f;

        // 깊이 파고드는 것 자체를 막는다 — 자재는 느리지만 <b>카트가 빠르다.</b>
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>
    /// ★ <b>자재는 멀리 못 간다.</b> 치면 밀려나되 날아가지는 않는다 —
    /// 날아간 자재는 <b>코스 밖이나 벽 속</b>에서 끝나고, 그게 「공중에 박힌 애」였다.
    ///
    /// 세 가지를 같이 본다: 너무 빠르면 <b>속도를 깎고</b>, 집에서 너무 멀거나
    /// 너무 높이 뜨면 <b>제자리로 되돌린다.</b> 되돌리는 건 바퀴마다 어차피 하는 일이라
    /// (<see cref="RestoreAll"/>) 플레이어 눈에는 「굴러가다 멈췄다」로 보인다.
    /// </summary>
    void FixedUpdate()
    {
        if (body == null || body.isKinematic) return;

        Vector3 v = body.linearVelocity;
        if (v.sqrMagnitude > MaxSpeed * MaxSpeed)
            body.linearVelocity = v.normalized * MaxSpeed;

        Vector3 d = transform.position - home;
        if (d.y > StrayUp || new Vector2(d.x, d.z).sqrMagnitude > StrayFlat * StrayFlat)
            Restore();
    }

    /// <summary>자재가 낼 수 있는 최고 속도(m/s). 이보다 빠르면 날아간 것이다.</summary>
    const float MaxSpeed = 7f;

    /// <summary>제자리에서 이만큼 뜨면 돌려보낸다. 드럼통 키가 0.9 라 1.2 면 «들렸다» 가 확실하다.</summary>
    const float StrayUp = 1.2f;

    /// <summary>제자리에서 이만큼 밀려나면 돌려보낸다. 길 폭이 7~12m 라 6m 면 이미 코스 밖이다.</summary>
    const float StrayFlat = 6f;

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
    /// ★★ 2026-10-14 수연: *"모든 자재들이 아예 바닥에 없고 다 위쪽으로 떠 있어."*
    ///
    /// <see cref="home"/> 은 <b>빌더가 놓은 자리</b>를 Awake 에 적어 둔 값이다.
    /// 그 자리가 길 표면보다 조금 위면, 처음에는 중력으로 내려앉지만
    /// <b>되돌릴 때마다 다시 그 높이로 올라간다</b> — 바퀴가 넘어갈 때마다 한 번씩
    /// (<see cref="RestoreAll"/>) 떠오르는 셈이라 결국 전부 공중에 선다.
    ///
    /// 그래서 <b>되돌릴 때 바닥을 재서</b> 내려놓는다. 코스를 고치거나 자재를 옮겨도
    /// 다시 안 틀린다 — 「좌표로 찍지 말고 재라」 와 같은 판단이야.
    /// 바닥을 못 찾으면 적어 둔 자리를 그대로 쓴다(없는 것보다 낫다).
    /// </summary>
    bool grounded;

    /// <summary>바닥에 앉히고 그 자리를 <see cref="home"/> 으로 삼는다.</summary>
    void Settle(Vector3 want)
    {
        grounded = Ground.FloorUnder(want, transform, out _);
        home = grounded ? Ground.Sit(transform, want) : want;
        transform.position = home;
    }

    /// <summary>
    /// ★ 코스는 <see cref="TrackBuilder"/> 가 <b>Awake 에</b> 짓는다. 자재가 먼저 깨면
    /// 그 밑에 아직 길이 없어서 바닥을 못 찾는다 — 그러면 빌더가 놓은 높이 그대로 뜬다.
    /// <c>Start</c> 는 <b>모든 Awake 뒤</b>라 거기서 한 번 더 잰다.
    /// </summary>
    void Start()
    {
        if (!grounded) Settle(home);
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

        // ★ <b>치고 지나가면 속도를 잃는다.</b> 전에는 자재가 가벼워서 «툭 밀리고 끝» 이었다 —
        //   그러면 피할 이유가 숫자(기회 깎임)뿐이고, <b>손에는 아무 일도 안 일어난다.</b>
        //   물기둥과 같은 방식으로 깎는다(날려 보내지 않는다 — 코스 밖으로 날아가면 벌이 된다).
        kart.Douse(0.62f, 0f);
    }
}
