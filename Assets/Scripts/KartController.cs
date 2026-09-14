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
    [Tooltip("최고 속도 (m/s). 22 정도가 시속 80km 느낌")]
    public float maxSpeed = 22f;
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
    public float highSpeedSteerCut = 0.45f;

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
    bool driftHeld;
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

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Update()
    {
        // Update 에서 입력을 읽고, FixedUpdate 에서 물리에 적용한다.
        KartInput.Tick(Time.deltaTime);
        throttleInput = KartInput.Throttle;
        steerInput = KartInput.Steer;
        driftHeld = KartInput.Drift;

        if (KartInput.RespawnPressed) Respawn();

        UpdateVisualLean();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        ApplySuspension(dt);
        ApplyDrive(dt);
        ApplySteering(dt);
        ApplyGrip();
        UpdateDriftAndBoost(dt);

        // 공중에서 붕 뜨는 느낌을 줄이려고 중력을 조금 더 준다
        if (!IsGrounded) rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
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
