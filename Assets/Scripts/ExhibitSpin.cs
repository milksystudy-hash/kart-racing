using UnityEngine;

/// <summary>
/// 진열장 안의 전시품이 <b>천천히 돌면서 떠 있는다.</b>
///
/// 2026-10-02 유저: *"코인이 안에 박힌 것 같은데. 좀 크기도 키우고 위로 띄우고
/// 빙글빙글 돌려야 하지 않을까."* 맞다 — 유리 안에 가만히 놓인 물건은
/// <b>방 장식</b>으로 읽히고, 떠서 도는 물건은 <b>«이게 그 증거다»</b> 가 된다.
/// 어두운 방이라 더 그렇다: 움직이는 건 그것 하나뿐이니 눈이 거기로 간다.
///
/// ★ <b>회전을 대입하지 않고 곱한다.</b> 임포트한 FBX 는 축 변환(−90° X)을
/// 루트 회전으로 들고 오는데, <c>localRotation</c> 을 덮어쓰면 <b>모델이 눕는다</b> —
/// 이 프로젝트에서 곰으로 한 번 겪었다(2026-09-21).
/// </summary>
public class ExhibitSpin : MonoBehaviour
{
    [Tooltip("초당 몇 도. 빠르면 «돌아가는 장식», 느려야 «전시 중» 으로 보인다")]
    public float degreesPerSecond = 20f;

    [Tooltip("위아래로 떠다니는 폭(m). 0 이면 가만히 떠 있는다")]
    public float bob = 0.022f;

    [Tooltip("떠다니는 주기. 회전과 주기가 같으면 기계처럼 보여서 일부러 어긋나게 둔다")]
    public float bobSpeed = 0.85f;

    Quaternion baseRotation;
    Vector3 home;
    float phase;

    /// <summary>드래그로 돌려볼 때 더해지는 각도(<see cref="ExhibitViewer"/>).</summary>
    public float extraYaw;

    void Awake()
    {
        baseRotation = transform.localRotation;
        home = transform.localPosition;
        phase = Mathf.Repeat(transform.position.x * 1.7f + transform.position.z * 2.3f, 6.28f);
    }

    void Update()
    {
        float yaw = Time.time * degreesPerSecond + extraYaw;
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * baseRotation;

        if (bob > 0.0001f)
            transform.localPosition = home + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bob);
    }
}
