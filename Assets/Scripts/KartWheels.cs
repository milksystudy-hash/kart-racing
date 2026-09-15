using UnityEngine;

/// <summary>
/// 바퀴를 굴리고 앞바퀴를 꺾는다. 핸들도 같이 돌린다.
///
/// 달리는데 바퀴가 안 도는 건 눈에 바로 걸린다 — 속도감이 통째로 죽어버려서,
/// 잘 만든 모델이 오히려 장난감처럼 보인다.
///
/// 애니메이션 클립이 아니라 <b>코드로 Transform 을 돌린다</b>. 클립은 경로를 기억해서
/// 모델을 갈아끼우면 깨지지만, 이건 인스펙터에 꽂힌 오브젝트만 보니까 안 깨진다.
/// (CLAUDE.md 의 "회색 상자 교체 네 규칙" 중 네 번째)
///
/// 바퀴를 <b>카트와 축이 맞는 빈 오브젝트(Pivot)</b> 안에 넣어두고 쓴다. 그래야 모델이
/// 어떤 방향으로 만들어졌든 상관없이 X 는 축, Y 는 조향이 된다 — 빌더가 그 껍데기를 만들어준다.
/// </summary>
public class KartWheels : MonoBehaviour
{
    [Header("연결")]
    public KartController kart;

    [Tooltip("앞바퀴를 감싼 껍데기. 이게 좌우로 꺾인다")]
    public Transform[] steerPivots;

    [Tooltip("굴러갈 바퀴 넷. 껍데기가 아니라 바퀴 자체")]
    public Transform[] spinWheels;

    [Tooltip("운전대. 없으면 비워둬도 된다")]
    public Transform steeringWheel;

    [Header("값")]
    [Tooltip("앞바퀴가 최대로 꺾이는 각도")]
    public float maxSteerAngle = 26f;

    [Tooltip("바퀴 반지름(m). 이 값으로 구르는 속도를 계산한다")]
    public float wheelRadius = 0.17f;

    [Tooltip("운전대가 도는 각도. 앞바퀴보다 크게 돌아야 자연스럽다")]
    public float steeringWheelAngle = 110f;

    [Tooltip("꺾임이 따라오는 속도. 낮을수록 느긋하다")]
    public float steerSmoothing = 12f;

    Quaternion[] spinBase;
    Quaternion steeringBase;
    float spinDegrees;
    float steer01;

    void Awake()
    {
        if (kart == null) kart = GetComponent<KartController>();

        // 바퀴가 원래 어떻게 놓여 있었는지 기억해둔다. 굴릴 때 이걸 기준으로 돌린다.
        spinBase = new Quaternion[spinWheels != null ? spinWheels.Length : 0];
        for (int i = 0; i < spinBase.Length; i++)
            if (spinWheels[i] != null) spinBase[i] = spinWheels[i].localRotation;

        if (steeringWheel != null) steeringBase = steeringWheel.localRotation;
    }

    void LateUpdate()
    {
        if (kart == null) return;

        float dt = Time.deltaTime;

        // ---- 굴리기 ----
        // 바퀴 둘레를 한 바퀴 돌 때 360도. 속도가 곧 회전 속도가 된다.
        float circumference = 2f * Mathf.PI * Mathf.Max(0.01f, wheelRadius);
        float metresPerSecond = kart.SpeedKph / 3.6f;
        spinDegrees += (metresPerSecond / circumference) * 360f * dt;
        spinDegrees = Mathf.Repeat(spinDegrees, 360f);

        for (int i = 0; i < spinBase.Length; i++)
        {
            if (spinWheels[i] == null) continue;
            // 껍데기 기준 X 가 바퀴 축이다 (빌더가 카트와 축을 맞춰서 만들어 둔다)
            spinWheels[i].localRotation = spinBase[i] * Quaternion.Euler(spinDegrees, 0f, 0f);
        }

        // ---- 꺾기 ----
        steer01 = Mathf.Lerp(steer01, kart.SteerInput, 1f - Mathf.Exp(-steerSmoothing * dt));

        if (steerPivots != null)
            foreach (var pivot in steerPivots)
                if (pivot != null)
                    pivot.localRotation = Quaternion.Euler(0f, steer01 * maxSteerAngle, 0f);

        if (steeringWheel != null)
            steeringWheel.localRotation = steeringBase * Quaternion.Euler(0f, 0f, -steer01 * steeringWheelAngle);
    }
}
