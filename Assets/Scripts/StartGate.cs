using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비의 출발문. 마우스로 클릭하면 카운트다운이 돌고 레이스 씬으로 넘어간다.
/// (엔터 키로도 된다.)
///
/// 캐릭터를 안 골랐으면 막힌다 — 그래야 잠긴 자리를 고른 채로 출발하는 일이 없다.
/// 카운트다운 중에 다시 클릭하면 취소된다.
/// </summary>
public class StartGate : MonoBehaviour
{
    [Header("넘어갈 씬")]
    [Tooltip("빌드 설정에 등록돼 있어야 한다")]
    public string targetScene = "Track";

    [Header("동작")]
    public float countdownSeconds = 1.5f;
    [Tooltip("캐릭터를 안 골랐으면 못 들어가게 할지")]
    public bool requireSelection = true;

    [Header("보는 카메라")]
    public Camera lobbyCamera;
    public LobbyOrbitCamera orbit;

    [Header("연출")]
    public Renderer highlightRenderer;
    public Color idleColor  = new Color32(0x9C, 0x8A, 0x66, 0xFF);
    public Color hoverColor = new Color32(0xF0, 0xB5, 0x4A, 0xFF);

    public bool Hovered { get; private set; }
    public bool CountingDown { get; private set; }
    public float Remaining { get; private set; }
    public bool Blocked => requireSelection && !GameSelection.HasSelection;

    bool loading;
    bool appliedHover;

    void Awake()
    {
        if (lobbyCamera == null) lobbyCamera = Camera.main;
        if (orbit == null && lobbyCamera != null) orbit = lobbyCamera.GetComponent<LobbyOrbitCamera>();
        Remaining = countdownSeconds;
        ApplyColor();
    }

    void Update()
    {
        if (loading) return;

        // ★ 이야기가 도는 동안·첫 화면이 떠 있는 동안·획득 연출 중에는 <b>아무 것도 안 듣는다.</b>
        //   출발 키(ENTER·클릭)가 대사를 넘기는 키와 같아서, 안 막으면
        //   <b>대사를 읽다가 레이스가 시작된다</b>(2026-10-02 유저 제보).
        if (StoryStage.Talking || TitleScreen.Up)
        {
            Hovered = false;
            if (appliedHover) { appliedHover = false; ApplyColor(); }
            return;
        }

        Hovered = (orbit != null && orbit.IsDragging) ? false : PointerIsOnGate();
        if (Hovered != appliedHover) { appliedHover = Hovered; ApplyColor(); }

        if (WasActivated()) Toggle();

        if (!CountingDown) return;

        Remaining -= Time.deltaTime;
        if (Remaining <= 0f) Go();
    }

    bool WasActivated()
    {
        bool enterPressed = Keyboard.current != null &&
                            (Keyboard.current.enterKey.wasPressedThisFrame ||
                             Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        if (enterPressed) return true;

        bool clicked = orbit != null
            ? orbit.ClickedWithoutDragging
            : (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

        return clicked && Hovered;
    }

    void Toggle()
    {
        if (Blocked) return;

        CountingDown = !CountingDown;
        Remaining = countdownSeconds;
    }

    bool PointerIsOnGate()
    {
        if (lobbyCamera == null || Mouse.current == null) return false;

        Vector2 screen = Mouse.current.position.ReadValue();
        if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
            return false;

        Ray ray = lobbyCamera.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Collide))
            return false;

        return hit.collider.GetComponentInParent<StartGate>() == this;
    }

    void ApplyColor()
    {
        if (highlightRenderer == null) return;

        Color c = Blocked ? idleColor : (Hovered ? hoverColor : idleColor);
        var mat = highlightRenderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }

    void Go()
    {
        loading = true;
        CursorLock.Unlock();

        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning($"[StartGate] '{targetScene}' 씬을 빌드 설정에서 못 찾았어. " +
                             "File → Build Profiles 에서 씬 목록을 확인해줘.");
            loading = false;
            CountingDown = false;
            Remaining = countdownSeconds;
        }
    }
}
