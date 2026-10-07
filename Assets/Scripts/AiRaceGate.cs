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

    /// <summary>
    /// <b>이 순번의 AI 가 이번 판에 달리나.</b> 결승에서는 상대가 개발업자·시의원 <b>둘뿐</b>이라
    /// 셋째는 끈다 — 관계없는 형제가 한 대 껴 있으면 «둘과 맞붙는다» 가 «셋이 뒤섞인다» 가 된다.
    /// 자유 주행에서는 셋 다 나온다(같이 달릴 상대가 많은 게 낫다).
    /// </summary>
    public static bool SlotRaces(int slot) =>
        DebugAlways || !GrandFinal.Available || slot < GrandFinal.Rivals.Length;

    /// <summary>`alwaysOn` 이 켜진 게이트가 하나라도 있으면 true. 정적으로 들고 있는다.</summary>
    public static bool DebugAlways { get; private set; }

    void Awake() => DebugAlways = alwaysOn;

    void Start() => Apply();

    bool lastRace;
    bool haveLast;

    /// <summary>
    /// ★★ 2026-10-06 유저: *"게임 다 끝내고 자유 플레이 하러 들어가면 첫 판에 AI 가 없고,
    /// 나갔다가 다시 들어가면 그제서야 AI 자유 주행이 된다."*
    ///
    /// <b>«나갔다 오면 된다» 는 언제나 «첫 프레임 순서» 문제다.</b> <see cref="Apply"/> 가
    /// <c>Start</c> 와 재시작에서만 돌아서, 그 시점에 <c>ShouldRace</c> 가 아직 false 로
    /// 읽히면 <b>그 판 내내 다시 묻지 않았다.</b> 이제 <b>답이 바뀌면 바로 다시 맞춘다</b> —
    /// 매 프레임 세는 게 아니라 <b>달라졌을 때만</b>.
    /// </summary>
    void Update()
    {
        bool race = ShouldRace;
        if (haveLast && race == lastRace) return;
        haveLast = true; lastRace = race;
        Apply();
    }

    /// <summary>
    /// 게이트가 <b>없는 씬</b>에서도 스스로 들어온다. 「새 컴포넌트로 고치면 씬을 다시
    /// 구워야만 고쳐진다」를 다섯 번 겪은 뒤의 기본형이야.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;
        Place();
    }

    static void OnLoaded(UnityEngine.SceneManagement.Scene s,
                         UnityEngine.SceneManagement.LoadSceneMode m) => Place();

    static void Place()
    {
        // 꺼진 카트까지 봐야 한다 — 한 번 꺼두면 기본 검색으로는 영영 안 잡힌다
        if (FindObjectsByType<KartAi>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0) return;
        if (FindObjectsByType<AiRaceGate>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) return;

        new GameObject("AiRaceGate").AddComponent<AiRaceGate>();
    }

    /// <summary>레이스를 다시 시작할 때도 맞춰준다 — 그 사이에 마지막 판이 됐을 수 있다.</summary>
    public void Apply()
    {
        DebugAlways = alwaysOn;
        bool race = ShouldRace;

        if (aiKarts != null)
        {
            foreach (Transform kart in aiKarts)
            {
                var skin = kart.GetComponent<KartSkin>();
                int slot = skin != null ? skin.aiSlot : 0;
                kart.gameObject.SetActive(race && SlotRaces(slot));
                // ★ 판이 바뀌면 <b>누가 상대인지도 바뀐다.</b> Awake 는 씬을 열 때 한 번뿐이라
                // 결승을 이기고 자유 주행이 돼도 악당 카트를 그대로 입고 있었다.
                if (skin != null && kart.gameObject.activeSelf) skin.Reskin();
            }
        }
        else
        {
            // <b>꺼진 오브젝트까지 찾아야 한다.</b> FindObjectsByType 의 기본값은 꺼진 것을
            // 건너뛴다 — 그래서 한 번 꺼두면 <b>같은 씬에서 영영 다시 못 켠다.</b>
            // 유저가 4번 판에서 자재를 못 본 이유가 이거야(2026-09-17): 3번 판에서 껐는데
            // 4번 판이 되어 켜려고 보니 목록이 비어 있었다. 씬을 나갔다 오면 고쳐지던 것도 같은 이유.
            foreach (var ai in FindObjectsByType<KartAi>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var skin = ai.GetComponent<KartSkin>();
                int slot = skin != null ? skin.aiSlot : 0;
                ai.gameObject.SetActive(race && SlotRaces(slot));
                if (skin != null && ai.gameObject.activeSelf) skin.Reskin();
            }
        }

        // 순위판은 <b>지금 달리는 카트만</b> 세야 한다. 꺼진 카트를 세면 "4대 중 1위" 라고
        // 나오는데 화면에는 나 혼자다.
        var standings = FindFirstObjectByType<RaceStandings>();
        if (standings != null) standings.Recount();
    }
}
