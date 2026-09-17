using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1인칭 이동. 네가 주인공의 눈이 돼서 맵을 걸어 다니는 용도.
///
/// 걷기는 CharacterController 를 쓴다 — Rigidbody 보다 벽에 안 끼고,
/// 계단·경사도 알아서 올라간다. 초보가 만질 게 적어.
///
/// F 를 누르면 자유 비행으로 바뀌어서 벽을 통과하고 위아래로 날 수 있다.
/// 맵을 위에서 내려다보거나 구석을 확인할 때 이게 제일 편해.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("걷기")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.1f;
    public float gravity = -22f;

    [Header("시점")]
    [Tooltip("마우스 감도. 너무 빠르면 0.05 정도로 낮춰")]
    public float lookSensitivity = 0.11f;
    [Tooltip("위아래로 꺾을 수 있는 최대 각도")]
    public float pitchLimit = 88f;
    [Tooltip("눈 위치. 비워두면 자식에서 카메라를 찾아 쓴다")]
    public Transform cameraPivot;

    [Header("추락 복구")]
    [Tooltip("이 높이보다 아래로 내려가면 시작 위치로 되돌린다")]
    public float fallResetY = -15f;

    [Header("자유 비행 — F 키")]
    // 2026-09-17 유저: "담장을 못 넘어서 캠퍼스를 자세히 못 보겠다. 임시 사각형이니까
    // 물체 다 통과하게 해줘." <b>개발용 유령</b>이다 — 플레이어한테는 안 간다.
    // 담장·벽은 그대로 두고(그게 맞는 설정이니까) 점검할 때만 통과한다.
    [Tooltip("켜면 벽을 통과한다. 개발용 — G 로 켜고 끈다")]
    public bool ghost;

    public bool flyMode;
    public float flySpeed = 12f;
    [Tooltip("비행 중 Shift 를 누르면 몇 배 빨라지는지")]
    public float flySprintMultiplier = 3f;
    [Tooltip("Space 를 안 누를 때 저절로 내려오는 속도. 0 이면 공중에 멈춰 있는다")]
    public float flySinkSpeed = 3.5f;
    [Tooltip("바닥에 닿으면 비행을 자동으로 푼다")]
    public bool landAutomatically = true;

    /// <summary>카트를 타고 있는 동안처럼, 조작을 잠시 꺼야 할 때 false 로.</summary>
    public bool ControlEnabled { get; set; } = true;

    public bool IsFlying => flyMode;

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    Vector3 spawnPosition;
    float spawnYaw;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) cameraPivot = cam.transform;
        }

        pitch = cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f;
        if (pitch > 180f) pitch -= 360f;

        spawnPosition = transform.position;
        spawnYaw = transform.eulerAngles.y;
    }

    void OnEnable() => CursorLock.Lock();

    void Update()
    {
        // 바닥이 뚫려 있든 맵 밖으로 걸어나가든, 끝없이 떨어지지는 않게.
        // 조작이 꺼져 있어도 복구는 되도록 맨 앞에서 검사한다.
        if (transform.position.y < fallResetY)
        {
            Teleport(spawnPosition, spawnYaw);
            return;
        }

        if (!ControlEnabled) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            ToggleFly();

        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            ToggleGhost();

        CursorLock.HandleEscapeAndClick();

        if (CursorLock.IsLocked) HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        if (Mouse.current == null || cameraPivot == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;

        // 좌우는 몸통을, 위아래는 머리(카메라)만 돌린다
        transform.Rotate(0f, delta.x, 0f, Space.Self);

        pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = 0f, z = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    z += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  z -= 1f;

        bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        Vector3 wish = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);

        // <b>유령</b>은 CharacterController 를 아예 끄고 좌표를 직접 옮긴다.
        // Move() 를 쓰면 컨트롤러가 살아 있어서 벽에 걸린다 — 통과하려면 이 방법뿐이야.
        if (ghost)
        {
            float rise = 0f;
            if (keyboard.spaceKey.isPressed) rise += 1f;
            if (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed) rise -= 1f;

            float ghostSpeed = flySpeed * (sprint ? flySprintMultiplier : 1f);
            transform.position += (wish * ghostSpeed + Vector3.up * (rise * ghostSpeed)) * Time.deltaTime;
            verticalVelocity = 0f;
            return;
        }

        if (flyMode)
        {
            float up = 0f;
            if (keyboard.spaceKey.isPressed) up += 1f;
            if (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed) up -= 1f;

            float speed = flySpeed * (sprint ? flySprintMultiplier : 1f);
            float vertical = up * speed;

            // 위로 올리는 키를 안 누르면 저절로 가라앉는다.
            // 이게 없으면 한 번 뜬 뒤로 계속 떠 있어서 "내려오질 않는다" 가 된다.
            if (Mathf.Approximately(up, 0f)) vertical = -flySinkSpeed;

            controller.Move((wish * speed + Vector3.up * vertical) * Time.deltaTime);
            verticalVelocity = 0f;

            // 바닥에 닿으면 알아서 비행이 풀린다 — 뛰었다가 착지하는 느낌으로
            if (landAutomatically && up <= 0f && controller.isGrounded) flyMode = false;
            return;
        }

        // 땅에 붙어 있을 때 살짝 아래로 눌러줘야 경사에서 안 튄다
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = wish * (sprint ? sprintSpeed : walkSpeed);
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    public void ToggleFly()
    {
        flyMode = !flyMode;
        verticalVelocity = 0f;
    }

    /// <summary>벽 통과. 개발용이라 플레이어 빌드에서는 쓸 일이 없다.</summary>
    public void ToggleGhost()
    {
        ghost = !ghost;
        controller.enabled = !ghost;
        verticalVelocity = 0f;
        if (ghost) flyMode = false;
    }

    /// <summary>카트에서 내릴 때처럼, 특정 위치로 순간이동시킬 때.</summary>
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        controller.enabled = !ghost;   // 유령 중이면 꺼진 채로 둔다
        verticalVelocity = 0f;
    }
}

/// <summary>
/// 마우스 커서 잠금을 한 군데서 관리한다.
/// 에디터에서 커서가 갇혀서 못 빠져나오는 게 초보한테 제일 당황스러운 일이라,
/// Esc 로 항상 풀리게 해뒀다.
/// </summary>
public static class CursorLock
{
    public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

    public static void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Esc = 커서 풀기, 화면 클릭 = 다시 잠그기.</summary>
    public static void HandleEscapeAndClick()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Unlock();
        else if (!IsLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Lock();
    }
}
