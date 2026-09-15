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

    [Header("속도감")]
    public float baseFov = 60f;
    public float topSpeedFov = 76f;
    public float boostFovKick = 6f;

    Camera cam;
    float yaw;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null)
        {
            yaw = target.eulerAngles.y;
            transform.position = target.position + Quaternion.Euler(0f, yaw, 0f) * offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 카트의 Y 회전만 부드럽게 따라간다
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime));
        Quaternion flatRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 desired = target.position + flatRotation * offset;
        transform.position = Vector3.Lerp(transform.position, desired,
                                          1f - Mathf.Exp(-positionSmoothing * Time.deltaTime));

        Vector3 lookTarget = target.position
                           + Vector3.up * lookHeight
                           + flatRotation * Vector3.forward * lookAhead;
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        UpdateFov();
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
