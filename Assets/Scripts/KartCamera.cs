using UnityEngine;

/// <summary>
/// 카트를 따라다니는 3인칭 카메라.
/// 카트의 좌우 기울기는 따라가지 않고 방향(Y축)만 따라가서 화면이 안 흔들린다.
/// 속도가 붙으면 시야각이 넓어져서 빨라 보인다 — 레이싱 게임의 기본 트릭.
/// </summary>
public class KartCamera : MonoBehaviour
{
    [Header("따라갈 대상")]
    public Transform target;
    public KartController kart;

    [Header("위치")]
    // 5.6m 뒤 · 2.8m 위는 카트(길이 1.5m)를 화면의 10% 도 안 되게 만든다 — 차가 장난감처럼 보인다.
    // 3.9m 뒤 · 1.95m 위면 카트가 화면에서 두 배 가까이 커지고, 낮은 시점이라 속도감도 붙는다.
    [Tooltip("카트 기준 카메라 위치. y=높이, z=뒤로 물러난 거리")]
    public Vector3 offset = new Vector3(0f, 1.75f, -3.4f);
    [Tooltip("카트보다 조금 앞을 본다. 카메라가 가까워진 만큼 더 멀리 봐야 코너가 미리 보인다")]
    public float lookAhead = 5f;
    public float lookHeight = 0.6f;

    [Header("따라오는 속도")]
    public float positionSmoothing = 7f;
    public float rotationSmoothing = 9f;

    [Header("속도감 — 시야각")]
    public float baseFov = 60f;
    public float topSpeedFov = 76f;
    public float boostFovKick = 6f;

    [Header("속도감 — 흔들림")]
    // 시야각만으로는 부족했다. 화면이 <b>가만히</b> 있으면 22m/s 도 8m/s 처럼 보인다.
    // 손떨림 같은 잡음이 아니라 노면 진동처럼 보여야 해서 펄린 노이즈를 쓴다.
    // 2026-09-17 유저: "달릴 때마다 지진 온 것 같다. 뭐 부딪히거나 해야 진동이 실감 난다."
    // 맞는 말이야 — 늘 떨고 있으면 그건 진동이 아니라 <b>화면 상태</b>가 된다. 떨림은
    // 사건일 때만 의미가 있어서 <b>주행 진동은 껐다.</b> 속도감은 시야각과 물러나기가 맡는다.
    [Tooltip("최고 속도에서 카메라가 떠는 폭(m). 0 이면 안 떤다 — 기본은 0")]
    public float shakeAtTopSpeed = 0f;
    [Tooltip("부스트가 터지는 순간의 떨림(m)")]
    public float boostShake = 0.035f;
    [Tooltip("벽에 부딪힌 순간의 충격(m). 0.35초쯤에 걸쳐 잦아든다")]
    public float hitShake = 0.30f;

    [Header("속도감 — 물러나기")]
    // 빨라질수록 카메라가 뒤로 처진다. 카트가 <b>화면에서 작아지면서</b> 달아나는 것처럼 보인다.
    [Tooltip("최고 속도에서 뒤로 더 물러나는 거리(m)")]
    public float pullBack = 0.85f;
    [Tooltip("최고 속도에서 낮아지는 높이(m). 낮을수록 지면이 빨리 흐른다")]
    public float crouch = 0.22f;

    Camera cam;
    float yaw;
    float hitImpulse;
    int lastWallHits;
    float noiseSeed;

    void Awake()
    {
        cam = GetComponent<Camera>();
        noiseSeed = Random.Range(0f, 100f);

        // 화면 효과는 따로 떼어놨다(SpeedRush). 여기서 붙여주면 <b>씬을 다시 굽지 않아도</b>
        // 바로 들어온다 — 카메라는 이미 씬에 있으니까.
        if (GetComponent<SpeedRush>() == null)
            gameObject.AddComponent<SpeedRush>().kart = kart;
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            transform.position = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ★ <b>멈춘 동안에는 카메라도 멈춘다</b>(2026-09-18 유저: *"ESC 누르면 화면이 덜덜 떨려"*).
        // 카트는 물리로 재워 놨는데 카메라는 `LateUpdate` 라 계속 돌았고, 흔들림·물러나기·
        // 시야각이 전부 <b>0 이 된 속도</b>를 보고 원위치로 되돌아오려다 매 프레임 떨었다.
        // 멈춤은 «그림도 멈추는 것» 이어야 일시정지로 읽힌다.
        if (RacePause.On) return;

        // 카트의 Y 회전만 부드럽게 따라간다
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime));
        Quaternion flatRotation = Quaternion.Euler(0f, yaw, 0f);

        float speed01 = Speed01;

        // 빠를수록 뒤로, 그리고 조금 낮게
        Vector3 moved = offset + new Vector3(0f, -crouch * speed01, -pullBack * speed01);

        Vector3 desired = target.position + flatRotation * moved;
        transform.position = Vector3.Lerp(transform.position, desired,
                                          1f - Mathf.Exp(-positionSmoothing * Time.deltaTime));

        Vector3 lookTarget = target.position
                           + Vector3.up * lookHeight
                           + flatRotation * Vector3.forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        // 흔들림은 <b>맨 마지막에 더한다.</b> 목표 위치에 섞으면 부드럽게 만드는 lerp 가
        // 떨림을 먹어버려서 아무 일도 안 일어난다.
        transform.position += Shake(speed01);

        UpdateFov();
    }

    float Speed01 => kart == null ? 0f
        : Mathf.Clamp01(Mathf.Abs(kart.SpeedKph) / Mathf.Max(1f, kart.maxSpeed * 3.6f));

    /// <summary>노면 진동 + 부스트 + 충돌 충격을 합친 한 프레임치 흔들림.</summary>
    Vector3 Shake(float speed01)
    {
        if (kart == null) return Vector3.zero;

        // 벽에 새로 부딪혔나. WallHits 는 실제로 속도를 깎은 충돌만 센다.
        if (kart.WallHits != lastWallHits)
        {
            if (kart.WallHits > lastWallHits) hitImpulse = hitShake;
            lastWallHits = kart.WallHits;
        }
        hitImpulse = Mathf.Lerp(hitImpulse, 0f, 1f - Mathf.Exp(-9f * Time.deltaTime));

        float amount = shakeAtTopSpeed * speed01 * speed01     // 제곱 — 느릴 때는 거의 안 떨게
                     + (kart.IsBoosting ? boostShake : 0f)
                     + hitImpulse;
        if (amount < 0.0005f) return Vector3.zero;

        // 펄린 노이즈는 연속이라 진동으로 보인다. Random 을 쓰면 화면 잡음처럼 보여.
        float t = Time.time * 26f + noiseSeed;
        return transform.right * ((Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * amount)
             + transform.up    * ((Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * amount);
    }

    void UpdateFov()
    {
        if (cam == null || kart == null) return;

        float speed01 = Mathf.Clamp01(Mathf.Abs(kart.SpeedKph) / (kart.maxSpeed * 3.6f));
        float goal = Mathf.Lerp(baseFov, topSpeedFov, speed01);
        if (kart.IsBoosting) goal += boostFovKick;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, goal, 1f - Mathf.Exp(-6f * Time.deltaTime));
    }
}
