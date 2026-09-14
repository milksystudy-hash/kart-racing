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
    [Tooltip("카트 기준 카메라 위치. y=높이, z=뒤로 물러난 거리")]
    public Vector3 offset = new Vector3(0f, 2.8f, -5.6f);
    [Tooltip("카트보다 조금 앞을 본다")]
    public float lookAhead = 4f;
    public float lookHeight = 1.1f;

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
