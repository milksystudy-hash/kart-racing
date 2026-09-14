using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비에서 가장 가까운 캐릭터 자리를 찾아 E 로 고르게 한다.
/// 화면에 뜨는 안내문도 여기서 만들어서 LobbyHUD 로 넘긴다.
/// </summary>
public class LobbySelector : MonoBehaviour
{
    public Transform player;
    [Tooltip("이 거리 안에 들어와야 고를 수 있다")]
    public float reachDistance = 3.2f;

    [Tooltip("비워두면 씬에서 알아서 다 찾는다")]
    public CharacterStand[] stands;

    public CharacterStand Nearest { get; private set; }
    public CharacterStand Chosen { get; private set; }

    void Awake()
    {
        if (stands == null || stands.Length == 0)
            stands = FindObjectsByType<CharacterStand>(FindObjectsSortMode.None);

        System.Array.Sort(stands, (a, b) => a.index.CompareTo(b.index));

        if (player == null)
        {
            var fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null) player = fpc.transform;
        }

        // 로비로 돌아왔을 때 아까 고른 캐릭터를 그대로 유지한다
        if (GameSelection.HasSelection) ApplyChoice(FindByIndex(GameSelection.SelectedIndex), silent: true);
    }

    void Update()
    {
        if (player == null) return;

        Nearest = FindNearest();

        if (Nearest != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ApplyChoice(Nearest, silent: false);
    }

    CharacterStand FindNearest()
    {
        CharacterStand best = null;
        float bestSqr = reachDistance * reachDistance;

        foreach (var stand in stands)
        {
            if (stand == null) continue;
            float sqr = (stand.transform.position - player.position).sqrMagnitude;
            if (sqr <= bestSqr) { bestSqr = sqr; best = stand; }
        }
        return best;
    }

    CharacterStand FindByIndex(int index)
    {
        foreach (var stand in stands)
            if (stand != null && stand.index == index) return stand;
        return null;
    }

    void ApplyChoice(CharacterStand stand, bool silent)
    {
        if (stand == null) return;

        foreach (var s in stands)
            if (s != null) s.SetSelected(s == stand);

        Chosen = stand;
        GameSelection.Select(stand.index, stand.Label);

        if (!silent) Debug.Log($"[Lobby] 캐릭터 선택: {stand.Label}");
    }

    /// <summary>HUD 가 그대로 띄울 안내문. 상황에 따라 문장이 바뀐다.</summary>
    public string Prompt
    {
        get
        {
            if (Nearest == null) return "";
            if (Nearest.IsEmpty) return $"{Nearest.Label}  —  준비 중";
            return Chosen == Nearest ? $"{Nearest.Label}  —  선택됨"
                                     : $"[E]  {Nearest.Label}  고르기";
        }
    }
}
