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
    public float SpeedKph => Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;
    public bool IsGrounded { get; private set; }
    public bool IsDrifting { get; private set; }
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

    /// <summary>
    /// 껍데기 마찰을 0 으로 둔 대가로 <b>벽이 공짜가 됐다</b> — 벽에 기대 풀악셀로 코너를
    /// 통과할 수 있어서 라인을 고를 이유가 사라진다. 마찰을 되살리면 다시 벽에 걸리니까,
    /// 걸리지는 않되 <b>속도만 깎는</b> 방식으로 대가를 되돌려준다.
    ///
    /// 바닥은 건드리지 않는다 — 접촉면이 위를 보면(그러니까 노면이면) 그냥 넘어간다.
    /// </summary>
    void OnCollisionEnter(Collision collision) => ScrubOnWall(collision, impact: true);
    void OnCollisionStay(Collision collision) => ScrubOnWall(collision, impact: false);

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
        }
        else if (wallScrubPerSecond > 0f)
        {
            // 붙어서 달리는 동안 계속 깎인다. 한 번 부딪히는 것보다 오래 비비는 게 더 손해다.
            rb.linearVelocity = Vector3.MoveTowards(velocity, Vector3.zero,
                                                    wallScrubPerSecond * Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        // Update 에서 입력을 읽고, FixedUpdate 에서 물리에 적용한다.
        KartInput.Tick(Time.deltaTime);
        throttleInput = KartInput.Throttle;
        steerInput = KartInput.Steer;
        driftHeld = KartInput.Drift;
        if (KartInput.DriftPressed) hopQueued = true;   // 물리는 FixedUpdate 에서 처리한다

        if (KartInput.RespawnPressed) Respawn();

        UpdateVisualLean();
    }

    void FixedUpdate()
    {
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
        // 빠를수록 조향각을 줄인다
        float speedCut = Mathf.Lerp(1f, 1f - highSpeedSteerCut,
                                    Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(0.1f, maxSpeed)));
        float direction = forwardSpeed < -0.1f ? -1f : 1f;   // 후진 중엔 조향이 반대
        float multiplier = IsDrifting ? driftSteerMultiplier : 1f;

        float yaw = steerInput * steerDegreesPerSecond * rollingFactor * speedCut * direction * multiplier * dt;
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
        IsDrifting = driftHeld && IsGrounded && forwardSpeed > 5f && Mathf.Abs(steerInput) > 0.2f;

        if (IsDrifting)
        {
            BoostCharge = Mathf.Min(BoostCharge + boostChargePerSecond * dt, boostChargeMax);
        }
        else if (wasDrifting && BoostCharge > 1f)
        {
            // 드리프트를 놓는 순간 모아둔 만큼 부스트가 터진다
            boostAmount = BoostCharge;
            boostTimer = boostDuration;
            BoostCharge = 0f;
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

    /// <summary>뒤집히거나 코스 밖으로 떨어졌을 때. R 키로도 부를 수 있다.</summary>
    public void Respawn()
    {
        RespawnAt(spawnPosition, spawnRotation);
    }

    public void RespawnAt(Vector3 position, Quaternion rotation)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        BoostCharge = 0f;
        boostTimer = 0f;
        boostAmount = 0f;
    }
}
