using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비를 둘러보는 카메라. 걸어다니지 않고 가운데를 중심으로 빙 돈다.
///
///   마우스 끌기 = 회전,  휠 = 확대·축소,  그냥 클릭 = 선택
///
/// "끌었나 그냥 눌렀나" 를 여기서 구분해서 LobbySelector 에 알려준다.
/// 안 그러면 카메라를 돌리려고 끌 때마다 캐릭터가 같이 선택돼 버린다.
/// </summary>
public class LobbyOrbitCamera : MonoBehaviour
{
    [Header("바라볼 중심")]
    public Transform pivot;
    [Tooltip("pivot 이 없을 때 쓸 좌표")]
    public Vector3 fallbackPivot = new Vector3(0f, 1.2f, 0f);

    [Header("거리")]
    public float distance = 15f;
    public float minDistance = 7f;
    public float maxDistance = 26f;
    public float zoomSensitivity = 1.6f;

    [Header("각도")]
    public float yaw = 0f;
    public float pitch = 16f;
    public float minPitch = -2f;
    public float maxPitch = 55f;
    public float dragSensitivity = 0.22f;

    [Header("부드러움")]
    public float smoothing = 12f;

    [Header("가만히 둘 때")]
    [Tooltip("손 떼고 이만큼 지나면 천천히 돌기 시작한다. 0 이면 안 돈다")]
    public float idleDelay = 5f;
    public float idleSpinSpeed = 3.5f;

    /// <summary>이번 프레임에 '끌지 않고 그냥 클릭' 이 일어났는지.</summary>
    public bool ClickedWithoutDragging { get; private set; }

    /// <summary>지금 카메라를 돌리는 중인지. 돌리는 동안은 마우스오버 반응을 끈다.</summary>
    public bool IsDragging { get; private set; }

    const float DragThresholdPixels = 6f;

    Vector2 pressPosition;
    float dragDistance;
    float idleTimer;
    float smoothedYaw, smoothedPitch, smoothedDistance;

    void Awake()
    {
        smoothedYaw = yaw;
        smoothedPitch = pitch;
        smoothedDistance = distance;
        Apply(instant: true);
    }

    void OnEnable()
    {
        // 로비는 메뉴 화면이니까 커서가 보여야 한다
        CursorLock.Unlock();
    }

    void Update()
    {
        ClickedWithoutDragging = false;

        HandleMouse();
        HandleIdleSpin();
        Apply(instant: false);
    }

    void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressPosition = mouse.position.ReadValue();
            dragDistance = 0f;
            IsDragging = false;
            idleTimer = 0f;
        }

        if (mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            dragDistance += delta.magnitude;

            if (dragDistance > DragThresholdPixels)
            {
                IsDragging = true;
                yaw += delta.x * dragSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * dragSensitivity, minPitch, maxPitch);
            }
            idleTimer = 0f;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            // 거의 안 움직였으면 회전이 아니라 클릭으로 친다
            ClickedWithoutDragging = dragDistance <= DragThresholdPixels;
            IsDragging = false;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * zoomSensitivity,
                                   minDistance, maxDistance);
            idleTimer = 0f;
        }
    }

    void HandleIdleSpin()
    {
        if (idleDelay <= 0f) return;

        idleTimer += Time.deltaTime;
        if (idleTimer > idleDelay)
            yaw += idleSpinSpeed * Time.deltaTime;
    }

    void Apply(bool instant)
    {
        float t = instant ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedYaw = Mathf.LerpAngle(smoothedYaw, yaw, t);
        smoothedPitch = Mathf.Lerp(smoothedPitch, pitch, t);
        smoothedDistance = Mathf.Lerp(smoothedDistance, distance, t);

        Vector3 target = pivot != null ? pivot.position : fallbackPivot;
        Quaternion rotation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0f);

        transform.position = target + rotation * new Vector3(0f, 0f, -smoothedDistance);
        transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
    }
}
