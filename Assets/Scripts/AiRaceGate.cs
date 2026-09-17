using UnityEngine;

/// <summary>
/// <b>AI 카트는 마지막 판에만 나온다.</b>
///
/// 2026-09-17 유저: *"처음부터 다시 모으는 상태에서 레이싱하면 초반부터 AI 와 겨루게 된다.
/// 원래 8개 다 모으고 나서 마지막이 AI 카트 3대와 함께 달리는 거잖아."* 맞다.
///
/// 처음부터 AI 가 있으면 <b>배우는 판이 사라진다.</b> 1판은 뭘 하는 게임인지 익히는 자리인데
/// 옆에서 세 대가 달리면 조작을 익힐 겨를이 없고, 마지막 판의 "드디어 겨룬다" 도 없어진다.
///
/// <b>카트는 씬에 그대로 두고 꺼두기만 한다.</b> 씬을 굽는 시점에는 몇 판째인지 알 수가 없고
/// (수집 기록은 PlayerPrefs 에 있다), 지웠다 다시 만들면 출발 위치·실력·차선을 또 계산해야 한다.
/// </summary>
public class AiRaceGate : MonoBehaviour
{
    [Tooltip("AI 카트들이 담긴 부모. 비워두면 KartAi 를 씬에서 찾는다")]
    public Transform aiKarts;

    [Tooltip("켜면 몇 판째든 AI 가 나온다 — 달리는 걸 확인할 때만")]
    public bool alwaysOn;

    /// <summary>
    /// AI 가 나와야 하나. <b>`KartAi` 도 이걸 본다</b> — 게이트가 씬에 없거나 순서가 밀려도
    /// 카트가 스스로 꺼질 수 있게. 새 컴포넌트에만 기대면 옛날에 구운 씬에서 안 먹는다
    /// (이 프로젝트에서 세 번 겪었다).
    /// </summary>
    public static bool ShouldRace => DebugAlways || MissionManager.FinalRace;

    /// <summary>`alwaysOn` 이 켜진 게이트가 하나라도 있으면 true. 정적으로 들고 있는다.</summary>
    public static bool DebugAlways { get; private set; }

    void Awake() => DebugAlways = alwaysOn;

    void Start() => Apply();

    /// <summary>레이스를 다시 시작할 때도 맞춰준다 — 그 사이에 마지막 판이 됐을 수 있다.</summary>
    public void Apply()
    {
        DebugAlways = alwaysOn;
        bool race = ShouldRace;

        if (aiKarts != null)
        {
            foreach (Transform kart in aiKarts) kart.gameObject.SetActive(race);
        }
        else
        {
            foreach (var ai in FindObjectsByType<KartAi>(FindObjectsSortMode.None))
                ai.gameObject.SetActive(race);
        }

        // 순위판은 <b>지금 달리는 카트만</b> 세야 한다. 꺼진 카트를 세면 "4대 중 1위" 라고
        // 나오는데 화면에는 나 혼자다.
        var standings = FindFirstObjectByType<RaceStandings>();
        if (standings != null) standings.Recount();
    }
}
