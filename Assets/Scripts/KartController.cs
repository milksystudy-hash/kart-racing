using UnityEngine;

/// <summary>
/// 아케이드 카트 조작. 마리오카트/소닉레이싱 계열의 "무겁지 않은" 느낌을 목표로 함.
/// 실제 바퀴 물리(WheelCollider)를 안 쓰고, 바닥에 레이캐스트를 쏴서 스프링으로 띄우는 방식.
/// 초보가 튜닝하기 쉽고 잘 안 뒤집힌다.
///
/// 조작: 방향키 / WASD = 가속·조향,  스페이스바 = 드리프트(놓으면 부스트)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("주행 — 숫자 만져보면서 감 잡으면 돼")]
    [Tooltip("최고 속도 (m/s). 17 이면 대략 시속 61km. 실내 트랙이라 이 정도가 무난하다")]
    public float maxSpeed = 17f;
    public float maxReverseSpeed = 8f;
    [Tooltip("가속력. 높을수록 출발이 빠릿함")]
    public float acceleration = 22f;
    [Tooltip("반대 방향키를 눌렀을 때 감속력")]
    public float braking = 40f;
    [Tooltip("아무 키도 안 눌렀을 때 저절로 느려지는 정도")]
    public float coastDrag = 1.2f;

    [Header("조향")]
    [Tooltip("초당 몇 도까지 돌 수 있는지")]
    public float steerDegreesPerSecond = 130f;
    [Range(0f, 0.9f)]
    [Tooltip("빠를수록 덜 꺾이게. 0이면 고속에서도 제자리 회전")]
    public float highSpeedSteerCut = 0.30f;

    [Header("드리프트 (스페이스바)")]
    public float driftSteerMultiplier = 1.7f;
    [Tooltip("옆으로 미끄러지지 않게 잡아주는 힘. 높을수록 레일 위 느낌")]
    public float gripNormal = 16f;
    [Tooltip("드리프트 중 접지력. 낮을수록 많이 미끄러짐")]
    public float gripWhileDrifting = 3.5f;
    public float boostChargePerSecond = 4f;
    public float boostChargeMax = 8f;

    [Tooltip("짧게 감아도 최소 이만큼은 나간다. 최고 속도에 더해지는 값(m/s)")]
    public float driftBoostFloor = 3f;
    [Tooltip("드리프트를 놓았을 때 부스트가 지속되는 시간(초)")]
    public float boostDuration = 1.3f;

    [Header("호핑 — 드리프트 키를 톡 누르면 통통")]
    [Tooltip("튀어오르는 세기. 3.2 면 약 0.5m 높이")]
    public float hopVelocity = 3.2f;
    [Tooltip("튀어오른 뒤 이 시간 동안은 서스펜션이 쉰다. 안 그러면 스프링이 바로 눌러버린다")]
    public float hopAirTime = 0.35f;
    [Tooltip("연속으로 통통 튀는 걸 막는 최소 간격")]
    public float hopCooldown = 0.25f;

    [Header("서스펜션 (바닥에서 띄우는 높이)")]
    public float rideHeight = 0.38f;
    public float springStrength = 140f;
    public float springDamper = 14f;
    public float extraGravity = 22f;
    public LayerMask groundMask = ~0;

    [Header("연출")]
    [Tooltip("코너에서 기울일 자식 오브젝트. 비워두면 안 기울어짐")]
    public Transform visual;
    public float visualLeanDegrees = 7f;
    public float visualLeanWhileDrifting = 16f;

    // --- HUD 와 다른 스크립트가 읽어가는 값들 ---
    /// <summary>
    /// ★ <b>멈춰 있는 동안의 속도.</b> <see cref="RacePause"/> 가 리지드바디를 키네마틱으로
    /// 재우는데, 유니티는 그 순간 <b>속도를 0 으로 지운다.</b> 그래서 ESC 를 누르면
    /// 속도계가 0 으로 떨어져 보였다(2026-09-18 유저). 실제 속도는 안 잃었고
    /// 풀면 그대로 돌아오는데, <b>화면만 거짓말</b>을 한 거야.
    /// 재우기 직전 값을 적어 두고 멈춘 동안에는 그걸 보여준다.
    /// </summary>
    Vector3 pausedVelocity;

    public void RememberVelocityForPause(Vector3 v) => pausedVelocity = v;

    /// <summary>속도를 읽는 곳은 전부 여기를 거친다 — 속도계·김·화면효과·카메라가 다 같은 값을 본다.</summary>
    public Vector3 CurrentVelocity =>
        RacePause.On ? pausedVelocity : (rb != null ? rb.linearVelocity : Vector3.zero);

    public float SpeedKph => Vector3.Dot(CurrentVelocity, transform.forward) * 3.6f;
    /// <summary>-1(좌) ~ +1(우). 바퀴와 운전대를 돌릴 때 KartWheels 가 읽는다.</summary>
    public float SteerInput => steerInput;
    public bool IsGrounded { get; private set; }
    public bool IsDrifting { get; private set; }
    /// <summary>
    /// 이번 판에 벽에 세게 부딪힌 횟수. 무충돌 임무가 이걸 본다.
    ///
    /// <b>벽은 한 덩어리가 아니라 여러 조각이다</b>(구간별 리본). 벽을 따라 쭉 긁으면
    /// 조각을 넘을 때마다 OnCollisionEnter 가 또 오기 때문에, 한 번 긁은 게 서너 번으로 세진다.
    /// 그래서 <see cref="WallHitCooldown"/> 안에 들어온 건 같은 접촉으로 친다.
    /// </summary>
    public int WallHits { get; private set; }

    /// <summary>이 시간 안에 또 부딪힌 건 같은 접촉으로 본다(초).</summary>
    const float WallHitCooldown = 0.7f;

    float lastWallHitAt = -99f;

    /// <summary>이번 판에 드리프트로 모은 태엽을 몇 번 터뜨렸는지.</summary>
    public int DriftBoosts { get; private set; }

    public float BoostCharge { get; private set; }
    public bool IsBoosting => boostTimer > 0f;
    public float BoostRemaining01 => boostDuration > 0f ? Mathf.Clamp01(boostTimer / boostDuration) : 0f;

    Rigidbody rb;
    float throttleInput, steerInput;
    bool driftHeld, hopQueued;
    float hopTimer, hopCooldownTimer;
    float boostTimer, boostAmount;
    Vector3 spawnPosition;
    Quaternion spawnRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // 무게중심을 낮추면 벽에 부딪혀도 덜 튄다
        rb.centerOfMass = new Vector3(0f, -0.2f, 0f);

        ApplySlipperyShell();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    /// <summary>
    /// 카트 껍데기를 미끄럽게 만든다. 벽에 스치면 걸려서 멈추는 걸 막는 장치야.
    ///
    /// 유니티 기본 마찰이면 상자끼리 닿는 순간 붙잡혀서, 벽을 따라 미끄러지는 게 아니라
    /// 벽에 달라붙는다. 마찰을 0 으로 두고 "둘 중 낮은 쪽을 쓴다(Minimum)" 로 맞추면
    /// 상대가 뭐든 미끄러진다. 아케이드 레이싱에서 흔히 쓰는 방법.
    ///
    /// 노면 접지력은 이것과 무관하다 — 그건 ApplyGrip() 이 따로 계산한다.
    /// </summary>
    void ApplySlipperyShell()
    {
        var slide = new PhysicsMaterial("KartSlide")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0.05f,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };

        foreach (var collider in GetComponentsInChildren<Collider>())
            if (!collider.isTrigger) collider.sharedMaterial = slide;
    }

    // ------------------------------------------------------------------
    //  벽에 부딪히면 손해를 본다
    // ------------------------------------------------------------------
    [Header("벽 충돌")]
    [Tooltip("정면으로 박았을 때 깎이는 속도 비율. 0 이면 벽이 공짜가 된다")]
    [Range(0f, 1f)] public float wallImpactLoss = 0.45f;

    [Tooltip("벽에 비비며 달릴 때 초당 잃는 속도(m/s). 벽을 타고 코너를 도는 걸 막는다")]
    public float wallScrubPerSecond = 5f;

    /// <summary>벽에 비벼도 이 속도 아래로는 안 깎는다. 벽에서 빠져나올 힘은 남겨둬야 한다.</summary>
    const float WallScrubFloor = 3f;

    /// <summary>
    /// 껍데기 마찰을 0 으로 둔 대가로 <b>벽이 공짜가 됐다</b> — 벽에 기대 풀악셀로 코너를
    /// 통과할 수 있어서 라인을 고를 이유가 사라진다. 마찰을 되살리면 다시 벽에 걸리니까,
    /// 걸리지는 않되 <b>속도만 깎는</b> 방식으로 대가를 되돌려준다.
    ///
    /// 바닥은 건드리지 않는다 — 접촉면이 위를 보면(그러니까 노면이면) 그냥 넘어간다.
    /// </summary>
    [Header("카트끼리 부딪히기")]
    // 2026-09-17 유저: "팽이들끼리 부딪히는 것처럼 크게 튕겼으면. 그렇다고 너무 튕기지는 말고."
    // 세기를 1.15 -> 1.7, 최소 2.2 -> 3.2 로 올렸다. 더 올리면 코스 밖으로 날아가서
    // <b>부딪힌 쪽이 레이스를 포기하게</b> 된다 — 그건 재미가 아니라 벌이야.
    [Tooltip("튕겨나가는 세기. 0 이면 안 튕긴다")]
    public float bumpPush = 1.7f;

    [Tooltip("가만히 있다 받혀도 이만큼은 튕긴다(m/s)")]
    public float bumpMinimum = 3.2f;

    [Tooltip("부딪힌 뒤 조종이 덜 먹는 시간(초). 길면 억울하다")]
    public float bumpStun = 0.3f;

    [Tooltip("부딪힐 때 팽이처럼 도는 세기(도/초)")]
    public float bumpSpin = 230f;

    const float BumpCooldown = 0.25f;
    float lastBumpAt = -99f;
    float bumpedUntil = -99f;
    float spinRate;

    public float Mass => rb != null ? rb.mass : 1f;
    public Vector3 Velocity => CurrentVelocity;

    /// <summary>방금 다른 카트에 받혔나. HUD 나 이펙트가 쓸 수 있게 열어둔다.</summary>
    public bool IsBumped => Time.time < bumpedUntil;

    void OnCollisionEnter(Collision collision)
    {
        var other = OtherKart(collision);
        if (other != null) { Bump(collision, other); return; }
        ScrubOnWall(collision, impact: true);
    }

    void OnCollisionStay(Collision collision)
    {
        // 카트끼리는 벽 처리를 타면 안 된다. 그러면 <b>속도가 매 프레임 지워져서</b>
        // 둘이 붙은 채 서로 밀기만 하고 아무도 못 빠져나간다(2026-09-17 유저 제보).
        // 벽 부딪힘 횟수에도 잘못 세졌다.
        if (OtherKart(collision) != null) return;
        ScrubOnWall(collision, impact: false);
    }

    static KartController OtherKart(Collision collision)
        => collision.rigidbody != null ? collision.rigidbody.GetComponent<KartController>() : null;

    /// <summary>
    /// 카트끼리 <b>탁 튕긴다.</b> 유저: *"탑블레이드 팽이처럼 부딪혀야 재밌잖아."*
    ///
    /// 물리 엔진에 맡기면 안 튕긴다 — 서스펜션이 매 프레임 속도를 다시 쓰고,
    /// 양쪽이 서로를 향해 구동력을 넣고 있어서 <b>밀기 싸움</b>이 된다. 그래서 충돌 순간에
    /// 속도를 직접 바꿔준다(ForceMode.VelocityChange — 질량과 무관하게 딱 그만큼 튄다).
    ///
    /// <b>무게가 여기서 처음으로 의미를 가진다.</b> 전에는 제원표의 중량이 사실상 장식이었어
    /// (구동력이 ForceMode.Acceleration 이라 질량을 무시한다). 이제 가벼운 카트가 더 많이 튄다 —
    /// 세진(11.7)이 시우(14.2)를 받으면 세진이 더 날아간다.
    ///
    /// 양쪽 카트가 각자 이 함수를 돌려서 <b>서로 반대 방향으로</b> 튄다. 한쪽만 계산하면
    /// 누가 먼저 충돌을 받았느냐에 따라 결과가 달라진다.
    /// </summary>
    void Bump(Collision collision, KartController other)
    {
        if (rb == null || collision.contactCount == 0) return;
        if (Time.time - lastBumpAt < BumpCooldown) return;
        lastBumpAt = Time.time;

        Vector3 normal = collision.GetContact(0).normal;   // 상대 -> 나
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.01f) return;
        normal.Normalize();

        // 서로 다가가던 속도. 나란히 스치면 작고, 정면으로 받으면 크다.
        float closing = Vector3.Dot(other.Velocity - rb.linearVelocity, normal);
        float strength = Mathf.Max(bumpMinimum, closing) * bumpPush;

        // 1 이면 동급. 내가 가벼울수록 커진다.
        float ratio = 2f * other.Mass / Mathf.Max(0.1f, Mass + other.Mass);

        rb.AddForce(normal * (strength * ratio), ForceMode.VelocityChange);

        // 팽이처럼 한 번 돌아간다. 조향이 MoveRotation 이라 토크는 안 먹어서 직접 돌린다.
        spinRate = Mathf.Sign(Vector3.Dot(Vector3.Cross(normal, transform.forward), Vector3.up))
                 * bumpSpin * ratio * Mathf.Clamp01(strength / 8f);

        bumpedUntil = Time.time + bumpStun * ratio;
        CancelBoost();   // 받히면 부스트는 날아간다. 안 그러면 밀려나면서도 앞으로 간다
    }

    void ScrubOnWall(Collision collision, bool impact)
    {
        if (rb == null || collision.contactCount == 0) return;

        Vector3 normal = collision.GetContact(0).normal;
        if (Mathf.Abs(normal.y) > 0.6f) return;   // 바닥이나 천장 — 벽이 아니다

        Vector3 velocity = rb.linearVelocity;
        float into = Vector3.Dot(velocity, -normal);   // 벽을 향해 파고드는 속도

        if (impact)
        {
            if (into < 1.5f) return;   // 스치기만 한 건 봐준다
            float severity = Mathf.Clamp01(into / Mathf.Max(1f, maxSpeed));
            rb.linearVelocity = velocity * (1f - wallImpactLoss * severity);

            // 속도가 깎일 만큼 박은 것만, 그리고 <b>한 접촉당 한 번만</b> 센다.
            if (Time.time - lastWallHitAt < WallHitCooldown) return;
            lastWallHitAt = Time.time;
            WallHits++;
        }
        else if (wallScrubPerSecond > 0f)
        {
            // 붙어서 달리는 동안 깎인다. 다만 **0 까지는 안 깎는다** —
            // 0 으로 보내면 벽에 박힌 채 악셀을 밟아도 매 프레임 속도가 지워져서 영영 못 빠져나온다.
            // (부스터 위에서 벽에 박히면 안 움직이던 게 이거였다.)
            float speed = velocity.magnitude;
            if (speed <= WallScrubFloor)
            {
                // 벽에 눌려서 더는 못 느려지는데 부스트가 계속 돌면, 앞으로 미는 힘 때문에
                // 후진도 조향도 안 먹는다. "발판 위에서 벽에 박히면 게이지만 줄고 못 빠져나간다" 가 이거였다.
                CancelBoost();
                return;
            }

            float drop = Mathf.Min(wallScrubPerSecond * Time.fixedDeltaTime, speed - WallScrubFloor);
            rb.linearVelocity = velocity * ((speed - drop) / speed);
        }
    }

    /// <summary>
    /// 끄면 키보드를 안 읽는다. <b>AI 카트가 이걸 끄고 <see cref="Drive"/> 로 몰아</b>(2026-09-16).
    /// 조종하는 주체만 다르고 물리/서스펜션/드리프트는 플레이어 것과 <b>똑같은 코드</b>를 쓴다 —
    /// 그래야 AI 가 사람이 못 하는 움직임을 하지 않는다.
    /// </summary>
    public bool acceptPlayerInput = true;

    /// <summary>바깥에서 카트를 몬다. 값은 다음 Update 까지 유지된다.</summary>
    public void Drive(float throttle, float steer, bool drift, bool hop = false)
    {
        // 출발 카운트 중에는 아무도 못 움직인다. <b>값을 넣는 자리에서</b> 막아야
        // 실행 순서에 안 휘둘린다 — AI 의 Update 가 이 Update 뒤에 돌 수도 있으니까.
        if (RaceCountdown.Blocked) { throttle = 0f; drift = false; hop = false; }

        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        driftHeld = drift;
        if (hop) hopQueued = true;
    }

    void Update()
    {
        if (acceptPlayerInput)
        {
            // Update 에서 입력을 읽고, FixedUpdate 에서 물리에 적용한다.
            KartInput.Tick(Time.deltaTime);
            throttleInput = KartInput.Throttle;
            steerInput = KartInput.Steer;
            driftHeld = KartInput.Drift;
            if (KartInput.HopPressed) hopQueued = true;   // 물리는 FixedUpdate 에서 처리한다

            // R 은 <b>구조 요청</b>이지 재시작이 아니다 — 지나온 체크포인트로 돌아간다.
            if (KartInput.RespawnPressed) RespawnToCourse();

            if (RaceCountdown.Blocked) { throttleInput = 0f; driftHeld = false; hopQueued = false; }
        }

        UpdateVisualLean();
    }

    void FixedUpdate()
    {
        // 멈춰 있는 동안은 물리를 아예 안 돌린다. 리지드바디가 키네마틱이라
        // 여기서 힘을 줘도 안 먹지만, 드리프트 게이지 같은 <b>타이머까지 돌면</b>
        // 멈춰 놓고 태엽이 감긴다.
        if (RacePause.On) return;

        float dt = Time.fixedDeltaTime;
        TryHop(dt);
        ApplySuspension(dt);
        ApplyDrive(dt);
        ApplySteering(dt);
        ApplyGrip();
        UpdateDriftAndBoost(dt);

        // 공중에서 붕 뜨는 느낌을 줄이려고 중력을 조금 더 준다
        if (!IsGrounded) rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }

    /// <summary>
    /// 드리프트 키를 톡 누르면 카트가 짧게 튀어오른다 — 마리오 카트의 그 호핑.
    /// 같은 키를 꾹 누른 채 꺾으면 드리프트로 이어지니까 키를 하나 더 쓸 필요가 없다.
    /// </summary>
    void TryHop(float dt)
    {
        // 타이머는 공중이든 땅이든 무조건 흐르게 한다.
        // 예전엔 ApplySuspension 안에서 줄였는데, 튀어올라서 공중에 뜨면 그 함수가 먼저
        // 빠져나가버려서 타이머가 멈췄다 — 착지한 뒤에도 한동안 서스펜션이 죽어 있었다.
        if (hopTimer > 0f) hopTimer -= dt;
        if (hopCooldownTimer > 0f) hopCooldownTimer -= dt;

        bool canHop = hopQueued && IsGrounded && hopTimer <= 0f && hopCooldownTimer <= 0f;
        hopQueued = false;
        if (!canHop) return;

        // 위로 향하는 속도를 갈아끼운다. 더하면 이미 뜨고 있을 때 너무 높이 솟는다.
        Vector3 v = rb.linearVelocity;
        v.y = hopVelocity;
        rb.linearVelocity = v;

        hopTimer = hopAirTime;
        hopCooldownTimer = hopAirTime + hopCooldown;
    }

    void ApplySuspension(float dt)
    {
        // 카트 중심보다 조금 위에서 아래로 쏜다 (바닥에 박혀 있어도 감지되게)
        const float rayStartUp = 0.5f;
        Vector3 origin = rb.position + Vector3.up * rayStartUp;
        float maxDistance = rayStartUp + rideHeight + 0.5f;

        IsGrounded = Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                                     maxDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (!IsGrounded) return;

        // 호핑 중엔 스프링을 잠시 쉬게 한다. 안 그러면 튀어오르자마자 도로 눌러버려서
        // 통통 튀는 게 아니라 부르르 떠는 것처럼 보인다. (타이머는 TryHop 에서 흐른다)
        if (hopTimer > 0f) return;

        float restDistance = rayStartUp + rideHeight;
        float compression = restDistance - hit.distance;      // + 면 너무 낮다 → 밀어올린다
        float verticalSpeed = Vector3.Dot(rb.linearVelocity, Vector3.up);
        float force = compression * springStrength - verticalSpeed * springDamper;

        rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
    }

    void ApplyDrive(float dt)
    {
        if (!IsGrounded) return;

        Vector3 forward = transform.forward;
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, forward);

        if (Mathf.Abs(throttleInput) > 0.01f)
        {
            // 달리는 방향과 반대로 누르면 가속이 아니라 브레이크다
            bool opposing = Mathf.Abs(forwardSpeed) > 0.5f &&
                            Mathf.Sign(throttleInput) != Mathf.Sign(forwardSpeed);
            float power = opposing ? braking : acceleration;
            rb.AddForce(forward * (throttleInput * power), ForceMode.Acceleration);
        }
        else
        {
            // 손 뗐을 때 서서히 멈춤
            rb.AddForce(forward * (-forwardSpeed * coastDrag), ForceMode.Acceleration);
        }

        if (IsBoosting)
            rb.AddForce(forward * (boostAmount * 6f), ForceMode.Acceleration);

        // 최고속 제한 (부스트 중에는 그만큼 더 나감)
        float limit = forwardSpeed >= 0f ? maxSpeed + boostAmount : maxReverseSpeed;
        float over = Mathf.Abs(forwardSpeed) - limit;
        if (over > 0f)
            rb.AddForce(forward * (-Mathf.Sign(forwardSpeed) * over * 12f), ForceMode.Acceleration);
    }

    void ApplySteering(float dt)
    {
        if (!IsGrounded) return;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // 거의 멈춰 있으면 안 돌아가야 자연스럽다 (제자리 회전 방지)
        float rollingFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);

        // 다만 벽에 정면으로 박히면 속도가 0 이 되고, 그러면 조향도 0 이라 영영 못 빠져나온다.
        // (부스터 위에서 벽에 붙으면 게이지만 줄고 아무것도 안 되던 게 이거였다.)
        // 악셀이나 후진을 밟고 있는 동안에는 최소한의 조향을 남겨둔다 — 손으로 비집고 나올 수 있게.
        if (Mathf.Abs(throttleInput) > 0.1f)
            rollingFactor = Mathf.Max(rollingFactor, 0.35f);
        // 빠를수록 조향각을 줄인다
        float speedCut = Mathf.Lerp(1f, 1f - highSpeedSteerCut,
                                    Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.1f, maxSpeed)));
        float direction = forwardSpeed < -0.1f ? -1f : 1f;   // 후진 중엔 조향이 반대
        float multiplier = IsDrifting ? driftSteerMultiplier : 1f;

        float yaw = steerInput * steerDegreesPerSecond * rollingFactor * speedCut * direction * multiplier * dt;

        // 받힌 직후에는 조종이 덜 먹고, 팽이처럼 돌던 게 남아 있다
        if (IsBumped) yaw *= 0.35f;
        if (Mathf.Abs(spinRate) > 0.5f)
        {
            yaw += spinRate * dt;
            spinRate = Mathf.Lerp(spinRate, 0f, 1f - Mathf.Exp(-6f * dt));
        }
        else spinRate = 0f;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
    }

    void ApplyGrip()
    {
        if (!IsGrounded) return;

        // 옆으로 흐르는 속도를 깎아낸다. 이 값이 곧 "미끄러지는 정도"다.
        Vector3 right = transform.right;
        float lateralSpeed = Vector3.Dot(rb.linearVelocity, right);
        float grip = IsDrifting ? gripWhileDrifting : gripNormal;
        rb.AddForce(right * (-lateralSpeed * grip), ForceMode.Acceleration);
    }

    /// <summary>레이스를 다시 시작할 때 임무 판정이 불러준다.</summary>
    public void ResetWallHits()
    {
        WallHits = 0;
        DriftBoosts = 0;
        lastWallHitAt = -99f;
    }

    /// <summary>돌던 부스트를 즉시 끊는다. 벽에 눌려 못 움직일 때 빠져나갈 길을 터준다.</summary>
    public void CancelBoost()
    {
        boostTimer = 0f;
        boostAmount = 0f;
    }

    /// <summary>
    /// 바깥에서 부스트를 먹인다 — 트랙의 가속 발판(BoostPad) 같은 것.
    /// 이미 부스트 중이면 더 센 쪽과 더 긴 쪽을 남긴다. 발판을 연달아 밟아도 끊기지 않게.
    /// </summary>
    public void ApplyBoost(float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f) return;

        boostAmount = Mathf.Max(boostAmount, amount);
        boostTimer = Mathf.Max(boostTimer, duration);
        BoostCharge = 0f;   // 모으던 드리프트 게이지는 여기서 정리한다
    }

    void UpdateDriftAndBoost(float dt)
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        bool wasDrifting = IsDrifting;
        // 5m/s 는 너무 높았다 — 코너에 들어가려고 속도를 줄이면 그 순간 태엽이 안 감겼다.
        // 2026-09-17 유저: "SHIFT 를 잘 못 다루겠다. 평소엔 자주 부딪혀서 안 쓸 것 같다."
        // 문턱이 높았다 — 3.5m/s 는 코너 진입에서 브레이크를 밟으면 바로 밑으로 떨어지고,
        // 조향 0.18 은 완만한 코너에서 안 걸린다. <b>감기다 끊기는 게 제일 나쁘다</b>:
        // 감기는 줄 알고 잡고 있었는데 아무 일도 안 일어나면 그 키를 다시 안 쓴다.
        IsDrifting = driftHeld && IsGrounded && forwardSpeed > 2.2f && Mathf.Abs(steerInput) > 0.10f;

        if (IsDrifting)
        {
            BoostCharge = Mathf.Min(BoostCharge + boostChargePerSecond * dt, boostChargeMax);
        }
        else if (wasDrifting && BoostCharge > 1f)
        {
            // 드리프트를 놓는 순간 모아둔 만큼 부스트가 터진다.
            //
            // <b>바닥값을 준다.</b> 모은 게 1.2 쯤이면 최고 속도가 7% 오르고 끝나서
            // "감아도 달라지는 게 없다" 로 느껴진다(2026-09-17 유저: "기능이 더 나아지는 것
            // 같지도 않다"). 짧게 감아도 <b>확실히 느껴지는</b> 만큼은 나가야 다음에 또 쓴다.
            boostAmount = Mathf.Max(BoostCharge, driftBoostFloor);
            boostTimer = boostDuration;
            BoostCharge = 0f;
            DriftBoosts++;   // "태엽 N번 터뜨리기" 임무가 이걸 본다
        }
        else if (!IsDrifting)
        {
            BoostCharge = Mathf.MoveTowards(BoostCharge, 0f, boostChargePerSecond * 2f * dt);
        }

        if (boostTimer > 0f)
        {
            boostTimer -= dt;
            if (boostTimer <= 0f) boostAmount = 0f;
        }
    }

    void UpdateVisualLean()
    {
        if (visual == null) return;

        float target = -steerInput * (IsDrifting ? visualLeanWhileDrifting : visualLeanDegrees);
        Quaternion goal = Quaternion.Euler(0f, 0f, target);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, goal, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    /// <summary>
    /// <b>제 출발 자리로.</b> 판을 다시 시작할 때만 쓴다(<see cref="RaceStandings.ResetRace"/>).
    /// 레이스 중에 이걸 부르면 출발선까지 끌려간다 — 그건 구조가 아니라 벌이야.
    /// </summary>
    public void Respawn()
    {
        RespawnAt(spawnPosition, spawnRotation);
    }

    /// <summary>
    /// <b>R 키 — 지나온 체크포인트로 돌아간다.</b>
    ///
    /// 2026-09-18 유저: *"AI 세 대랑 달리다 멈춰서 추월당한 뒤 R 을 누르면 나만 출발선으로
    /// 가고 AI 는 가던 길 그대로던데 이게 맞아?"* 안 맞다. R 은 <b>뒤집히거나 낀 걸 빼주는
    /// 구조 요청</b>이고, 그 대가는 «그 자리에서 조금 뒤로» 여야 한다. 출발선으로 보내면
    /// 한 바퀴를 통째로 날리는 거라 <b>차라리 낀 채로 버티는 게 이득</b>이 되고,
    /// 그러면 키가 있으나 마나야.
    ///
    /// 코스 밖으로 떨어졌을 때(<see cref="LapTracker"/> 의 killPlane)와 <b>같은 자리</b>로 간다 —
    /// 같은 사고를 두 가지로 처리하면 플레이어가 규칙을 못 배운다.
    /// 체크포인트를 아직 하나도 안 지났으면(출발 직후) 출발 자리가 곧 마지막 체크포인트다.
    /// </summary>
    public void RespawnToCourse()
    {
        var progress = GetComponent<RaceProgress>();
        var last = progress != null ? progress.LastPassed : null;

        if (last != null && last.respawnPoint != null)
            RespawnAt(last.respawnPoint.position + Vector3.up * 0.6f, last.respawnPoint.rotation);
        else
            RespawnAt(spawnPosition, spawnRotation);
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        // 에디터(배치모드)에서는 Awake 가 안 돌아서 rb 가 비어 있다. 검사 스크립트가
        // 부를 수 있으니 막아둔다 — 실행 중에는 항상 있다.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }
        transform.SetPositionAndRotation(position, rotation);
        BoostCharge = 0f;
        boostTimer = 0f;
        boostAmount = 0f;
    }
}
