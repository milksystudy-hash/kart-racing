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

    [Header("3인칭 — 고른 캐릭터 캡슐이 보이게")]
    [Tooltip("몸에서 카메라까지. 0 이면 1인칭으로 돌아간다")]
    public float viewDistance = 3.1f;

    [Tooltip("카메라가 도는 중심 높이. 캡슐 키 1.15m 바로 위")]
    public float viewHeight = 1.25f;

    [Tooltip("카메라가 뚫지 말아야 할 것 — 벽·건물")]
    // 레이어 2(Ignore Raycast)는 뺀다. 카트가 거기 있어서(CLAUDE.md) 트랙에서 내려 걸을 때
    // 카메라가 제 카트에 걸려 코앞까지 당겨진다.
    public LayerMask viewBlockers = ~(1 << 2);

    [Header("추락 복구")]
    [Tooltip("이 높이보다 아래로 내려가면 시작 위치로 되돌린다")]
    public float fallResetY = -15f;

    [Header("자유 비행 — F 키")]
    // 2026-09-17 유저: "담장을 못 넘어서 캠퍼스를 자세히 못 보겠다. 임시 사각형이니까
    // 물체 다 통과하게 해줘." <b>개발용 유령</b>이다 — 플레이어한테는 안 간다.
    // 담장·벽은 그대로 두고(그게 맞는 설정이니까) 점검할 때만 통과한다.
    [Tooltip("켜면 벽을 통과한다. 개발용 — G 로 켜고 끈다")]
    public bool ghost;

    /// <summary>화면에 상태를 띄우려고 열어둔다.</summary>
    public bool IsGhost => ghost;

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

        // ★ 걷는 몸은 여태 <b>그림이 하나도 없었다</b>. 고른 캐릭터의 캡슐을 세운다 —
        //   스스로 붙으니 씬을 다시 구울 필요가 없다.
        VisitorBody.Ensure(gameObject);
        PlaceCamera();

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
        // 마우스를 안 움직여도 카메라는 제자리에 있어야 한다 — 커서가 풀려 있을 때도,
        // 벽 뒤로 걸어 들어갈 때도. HandleLook 안에만 두면 둘 다 놓친다.
        PlaceCamera();
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

    /// <summary>
    /// 3인칭 — 몸 뒤로 뺀다. <b>몸이 보여야 «내가 고른 캐릭터» 라는 게 읽힌다.</b>
    ///
    /// ★ 거리·각도 판정(<see cref="Reach"/>)은 <b>카메라가 아니라 몸</b>을 본다
    /// (빌더가 <c>visitor</c> 에 Player 루트를 꽂는다). 그래서 카메라를 뒤로 빼도
    /// 문·곰·수도꼭지가 잡히는 거리는 하나도 안 바뀐다 — 그게 이 변경이 안전한 이유야.
    ///
    /// 벽에 닿으면 그만큼 당겨 온다. <b>유령 모드에서는 안 당긴다</b> — 벽을 통과해
    /// 점검하는 게 그 모드의 목적인데 카메라만 벽 앞에 서면 아무 것도 못 본다.
    /// </summary>
    void PlaceCamera()
    {
        if (cameraPivot == null) return;

        if (viewDistance <= 0.01f)
        {
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);   // 1인칭 — 눈높이
            return;
        }

        Vector3 pivot = new Vector3(0f, viewHeight, 0f);
        Vector3 back = cameraPivot.localRotation * Vector3.back;
        float d = viewDistance;

        if (!ghost)
        {
            // 시작점이 CharacterController 안이라 제 몸에는 안 걸린다(캡슐에는 콜라이더가 없다)
            Vector3 from = transform.TransformPoint(pivot);
            Vector3 dir = transform.TransformDirection(back);
            if (Physics.SphereCast(from, 0.22f, dir, out var hit, viewDistance,
                                   viewBlockers, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.45f, hit.distance - 0.08f);
        }

        cameraPivot.localPosition = pivot + back * d;
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
        Toast.Show(flyMode ? "날기 켬 (SPACE 위 · C 아래)" : "날기 끔");
    }

    /// <summary>
    /// 벽 통과. 개발용이라 플레이어 빌드에서는 쓸 일이 없다.
    ///
    /// <b>켠 걸 화면에 알린다.</b> 안 알리면 눌렀는데 안 눌린 건지, 눌렸는데 안 통과하는 건지
    /// 구분이 안 된다 — 실제로 유저가 "벽을 못 넘겠다" 고 했을 때 키는 이미 있었고
    /// 화면 어디에도 안 적혀 있던 게 문제였다(2026-09-17).
    /// </summary>
    public void ToggleGhost()
    {
        ghost = !ghost;

        // Awake 전에 불릴 수 있다(에디터 검사, 다른 스크립트의 Awake). 없으면 그때 찾는다.
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = !ghost;

        verticalVelocity = 0f;
        if (ghost) flyMode = false;

        Toast.Show(ghost ? "유령 켬 — 벽 통과 (SPACE 위 · C 아래)" : "유령 끔");
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
