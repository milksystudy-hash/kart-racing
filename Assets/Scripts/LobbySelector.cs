using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비에서 마우스로 캐릭터를 고른다.
///
/// 카메라에서 커서 방향으로 광선을 쏴서 어느 받침대를 가리키는지 알아낸다.
/// 카메라를 돌리려고 끄는 중에는 반응하지 않는다 — 그 판단은 LobbyOrbitCamera 가 해준다.
/// </summary>
public class LobbySelector : MonoBehaviour
{
    public Camera lobbyCamera;
    public LobbyOrbitCamera orbit;

    [Tooltip("비워두면 씬에서 알아서 다 찾는다")]
    public CharacterStand[] stands;

    [Tooltip("클릭 판정에 쓸 레이어. 기본은 전부")]
    public LayerMask pickMask = ~0;

    /// <summary>지금 커서가 가리키는 받침대.</summary>
    public CharacterStand Hovered { get; private set; }
    /// <summary>선택된 받침대.</summary>
    public CharacterStand Chosen { get; private set; }

    void Awake()
    {
        if (stands == null || stands.Length == 0)
            stands = FindObjectsByType<CharacterStand>(FindObjectsSortMode.None);
        System.Array.Sort(stands, (a, b) => a.index.CompareTo(b.index));

        if (lobbyCamera == null) lobbyCamera = Camera.main;
        if (orbit == null && lobbyCamera != null) orbit = lobbyCamera.GetComponent<LobbyOrbitCamera>();

        // 레이스에서 돌아왔을 때 아까 고른 캐릭터를 그대로 유지한다
        if (GameSelection.HasSelection) Apply(FindByIndex(GameSelection.SelectedIndex), silent: true);
    }

    void Update()
    {
        Hovered = (orbit != null && orbit.IsDragging) ? null : StandUnderCursor();

        foreach (var stand in stands)
            if (stand != null) stand.SetHighlighted(stand == Hovered && stand != Chosen);

        bool clicked = orbit != null
            ? orbit.ClickedWithoutDragging
            : (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame);

        if (clicked && Hovered != null && Hovered.Selectable) Apply(Hovered, silent: false);
    }

    CharacterStand StandUnderCursor()
    {
        if (lobbyCamera == null || Mouse.current == null) return null;

        Vector2 screen = Mouse.current.position.ReadValue();
        // 커서가 창 밖으로 나가면 무시
        if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
            return null;

        Ray ray = lobbyCamera.ScreenPointToRay(screen);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, pickMask, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<CharacterStand>();
    }

    CharacterStand FindByIndex(int index)
    {
        foreach (var stand in stands)
            if (stand != null && stand.index == index) return stand;
        return null;
    }

    void Apply(CharacterStand stand, bool silent)
    {
        if (stand == null || !stand.Selectable) return;

        foreach (var s in stands)
            if (s != null) s.SetSelected(s == stand);

        Chosen = stand;
        GameSelection.Select(stand.index, stand.Label, stand.CastId);

        if (!silent) Debug.Log($"[Lobby] 캐릭터 선택: {stand.Label}");
    }

    /// <summary>HUD 가 그대로 띄울 안내문. 상황에 따라 문장이 바뀐다.</summary>
    public string Prompt
    {
        get
        {
            if (Hovered == null) return "";
            if (!Hovered.Selectable) return $"{Hovered.Label}  —  잠김";
            return Chosen == Hovered ? $"{Hovered.Label}  —  선택됨"
                                     : $"{Hovered.Label}  —  클릭해서 고르기";
        }
    }
}
