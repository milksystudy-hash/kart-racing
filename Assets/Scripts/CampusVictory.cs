using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// <b>레이싱을 다 끝내면 캠퍼스에서 «점령의 흔적» 이 걷힌다.</b>
///
/// 유저(2026-09-28): *"레이싱 게임 클리어하기 전에 권대호 동상이나 그런 거는 있다가,
/// 레이싱 끝나고 나면 동상과 노란색·보라색 골든베어 칸막이를 제거하도록 해줘.
/// 그리고 클리어 전이랑 후랑 연출 좀 신경써줘 — 우리가 만든 건 너무 아마추어 같아."*
///
/// 걷히는 것 둘 다 <b>«저놈들이 여기 들어와 있다» 를 눈으로 말하던 물건</b>이다:
/// 금색·자홍 광고판(§4.4 가 시킨 «일부러 안 어울리는 색»)과 명예 후원자 동상.
/// 그래서 이 둘이 없어지는 것만으로 캠퍼스가 <b>되찾아진 것</b>으로 읽힌다.
///
/// <b>기단은 남긴다.</b> 통째로 지우면 «원래 없었다» 가 되는데, 빈 받침대가 남으면
/// <b>«있었는데 내렸다»</b> 가 된다 — 없어진 것을 보여주는 게 이긴 걸 보여주는 방법이야.
/// 그 위에 곰인형 하나가 앉는다. 그게 이 박물관이 되찾은 자리라는 뜻이고,
/// 이 게임에서 제일 싸게 웃긴 한 장면이다.
///
/// ★★ <b>바뀐 것은 한 번은 보여준다.</b> 결승은 트랙에서 끝나니까, 그냥 두면 플레이어는
/// <b>이미 다 바뀌어 있는 캠퍼스</b>에 걸어 들어온다 — 그러면 아무 일도 안 일어난 것과 같다.
/// 마지막으로 본 상태를 저장해 뒀다가, <b>달라졌으면 옛 모습으로 시작해서 눈앞에서 걷어낸다.</b>
/// 두 번째부터는 스냅이다. 이게 상용 게임이 하는 일이고 <see cref="Reveal"/> 가 그 방법이야.
/// </summary>
[DefaultExecutionOrder(45)]
public class CampusVictory : MonoBehaviour
{
    const string SeenKey = "Racing.CampusWon";
    const string RootName = "CampusVictory";

    static readonly Color Fur = new Color32(0xA5, 0x75, 0x4A, 0xFF);
    static readonly Color Face = new Color32(0xE6, 0xDA, 0xC4, 0xFF);
    static readonly Color Dark = new Color32(0x4A, 0x33, 0x26, 0xFF);
    static readonly Color Ribbon = new Color32(0xC4, 0x45, 0x3E, 0xFF);

    // ---- 들어오기 — 씬을 다시 굽지 않아도 붙는다 ----------------------------
    // 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 이 프로젝트에서 다섯 번 겪었다.
    // 동상과 같은 방식으로 <b>스스로 들어온다.</b>

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Install();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

    static void Install()
    {
        // 연출이 게임을 망가뜨리면 안 된다 — 실패하면 조용히 포기한다(동상과 같은 판단).
        try
        {
            var boards = GameObject.Find("AdBoards");
            var statue = FindFirstObjectByType<CampusStatue>(FindObjectsInactive.Include);
            if (boards == null && statue == null) return;          // 캠퍼스·트랙이 아니다
            if (GameObject.Find(RootName) != null) return;         // 이미 있다

            var go = new GameObject(RootName);
            go.SetActive(false);   // AddComponent 는 그 자리에서 Awake 를 돌린다
            go.AddComponent<CampusVictory>();
            go.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[캠퍼스] 승리 연출을 못 붙였지만 게임은 계속된다: {e}");
        }
    }

    // ---- 상태 ---------------------------------------------------------------

    readonly List<Transform> ads = new List<Transform>();   // 금색·자홍 광고판
    readonly List<Transform> gone = new List<Transform>();  // 동상에서 내려가는 것
    Transform bear;                                         // 기단에 올라앉는 곰인형
    bool shownWon;

    /// <summary>여덟 판 + 결승까지 끝났나. 이 한 값이 캠퍼스의 «전 / 후» 를 정한다.</summary>
    static bool Won => GrandFinal.FreeRun;

    void Start()
    {
        Gather();

        bool won = Won;
        bool seen = PlayerPrefs.GetInt(SeenKey, 0) == 1;

        if (won == seen)
        {
            // 지난번과 같은 상태 — 아무 일도 없었다는 듯이 박는다
            shownWon = won;
            Apply(won, animate: false);
        }
        else
        {
            // ★ 달라졌다. <b>옛 모습으로 시작해서</b> 눈앞에서 바뀐다
            shownWon = !won;
            Apply(!won, animate: false);
            Apply(won, animate: true);
            shownWon = won;
            PlayerPrefs.SetInt(SeenKey, won ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ★ <b>F8 — 전후를 눈으로 확인하는 키.</b> 결승은 트랙에서 끝나니까 이게 없으면
    /// 연출을 한 번 보려고 <b>아홉 판을 다시 달려야 한다.</b> 화면 왼쪽 아래에 적어 뒀다 —
    /// 「키가 있어도 화면에 없으면 없는 것」(이 프로젝트에서 세 번 겪었다).
    /// <b>제출 전에 끌 것</b>(`TestHUD.debugKeys` 와 같이).
    /// </summary>
    public bool debugKeys = true;

    void Update()
    {
        var k = Keyboard.current;
        // ★★ 2026-10-06 유저: *"F8 은 개발업자·시의원이 나오기 전까지 꺼 둬라.
        //   플레이어가 못 찾게."* 맞는 요구다 — 전에는 F8 한 번에 <b>수집품 여덟 개를
        //   통째로 채우고</b> 결승까지 깬 상태가 돼서, 모르고 눌러도 <b>게임이 끝나 버렸다.</b>
        //
        //   이제 <b>여덟 개를 진짜로 다 모은 뒤에만</b> 듣는다. 그 시점이 곧 악당 둘이
        //   이야기에 들어오는 자리라, <b>건너뛸 게 남아 있지 않다</b> — 전후를 눈으로
        //   보는 용도는 그대로 살고 지름길만 사라진다.
        //   (점검할 때는 로비에서 F9 로 수집품을 채운 뒤 F8 — 두 단계라 우연히 못 누른다.)
        if (Dev.Enabled && debugKeys && GrandFinal.AllCollected && k != null && k.f8Key.wasPressedThisFrame)
        {
            if (GrandFinal.FreeRun)
            {
                GrandFinal.Reset();   // 수집품은 그대로 두고 «결승 전» 으로만
            }
            else
            {
                var ids = new List<string>();
                foreach (var e in ExhibitCatalogue.All) ids.Add(e.id);
                CollectionState.CollectAll(ids);
                GrandFinal.MarkCleared();
            }
        }

        // 로비의 F9 / F10 으로 수집 기록을 뒤집으면 여기서도 바로 보인다
        if (Won == shownWon) return;

        shownWon = Won;
        Apply(shownWon, animate: true);
        PlayerPrefs.SetInt(SeenKey, shownWon ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ---- 무엇이 바뀌나 -------------------------------------------------------

    void Gather()
    {
        var boards = GameObject.Find("AdBoards");
        if (boards != null)
            foreach (Transform kid in boards.transform)
                if (kid.name.StartsWith("Ad_")) ads.Add(kid);

        // 같은 판이 매번 같은 순서로 내려가야 «한 줄로 철거된다» 로 읽힌다
        ads.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        var statue = FindFirstObjectByType<CampusStatue>(FindObjectsInactive.Include);
        if (statue == null) return;

        // ★ <b>명판은 안 내린다.</b> 유저 설정: 시의원이 철거를 포기한 뒤에도 «자기 발자취를
        // 남겨야겠다» 며 동상을 그대로 둔다 — 다만 <b>근육 버전이 진짜 체형으로 바뀐다.</b>
        // 그가 물러났다는 걸 말로 설명하지 않고 <b>동상 하나로</b> 말하는 자리야.
        foreach (string n in new[] { "Figure_Muscle", "Figure" })
        {
            var part = statue.transform.Find(n);
            if (part != null) gone.Add(part);
        }

        bear = statue.transform.Find("Figure_Real");   // 없으면(옛 씬·FBX 없음) 그냥 사라지기만 한다
    }

    /// <summary>
    /// 순서가 전부다. 광고판이 <b>한 줄로 하나씩</b> 내려가고, 그게 끝난 뒤에
    /// 동상이 내려가고, 마지막에 곰인형이 떨어진다 — <b>2.6초짜리 한 문장</b>이야.
    /// 같은 프레임에 다 바꾸면 그건 문장이 아니라 설정값 변경이다.
    /// </summary>
    void Apply(bool won, bool animate)
    {
        if (won)
        {
            for (int i = 0; i < ads.Count; i++) Set(ads[i], false, animate, 0.15f + i * 0.22f, 2.4f);
            foreach (var g in gone) Set(g, false, animate, 1.55f, 1.6f);
            Set(bear, true, animate, 2.15f, 2.6f);
        }
        else
        {
            Set(bear, false, animate, 0f, 2.6f);
            foreach (var g in gone) Set(g, true, animate, 0.25f, 1.6f);
            for (int i = 0; i < ads.Count; i++) Set(ads[i], true, animate, 0.5f + i * 0.18f, 2.4f);
        }
    }

    static void Set(Transform t, bool visible, bool animate, float delay, float sink)
    {
        if (t == null) return;
        if (animate) Reveal.Play(t, visible, delay, sink);
        else Reveal.Snap(t, visible);
    }

    // ---- 기단에 앉는 곰인형 --------------------------------------------------

    /// <summary>
    /// 기단 꼭대기(`Cap` 윗면 y 1.59)에 앉는 작은 곰. <b>서 있지 않고 앉아 있다</b> —
    /// 서 있으면 «새 동상» 이라 같은 농담을 반대편에서 반복하는 꼴이 된다.
    /// 앉아 있으면 <b>기념물이 아니라 그냥 여기 사는 애</b>가 된다.
    /// </summary>
    Transform MakeBear(Transform statue)
    {
        var root = new GameObject("VictoryBear").transform;
        root.SetParent(statue, false);
        root.localPosition = new Vector3(0f, 1.59f, 0f);
        root.localRotation = Quaternion.Euler(0f, 18f, 0f);   // 살짝 틀어야 «놓인» 게 아니라 «앉은» 거다

        Ball(root, "Hip", new Vector3(0f, 0.20f, 0f), new Vector3(0.52f, 0.38f, 0.46f), Fur);
        Ball(root, "Chest", new Vector3(0f, 0.44f, 0.02f), new Vector3(0.44f, 0.38f, 0.40f), Fur);
        Ball(root, "Head", new Vector3(0f, 0.74f, 0.03f), new Vector3(0.42f, 0.40f, 0.40f), Fur);
        Ball(root, "Muzzle", new Vector3(0f, 0.70f, 0.20f), new Vector3(0.20f, 0.15f, 0.14f), Face);
        Ball(root, "Nose", new Vector3(0f, 0.73f, 0.26f), new Vector3(0.07f, 0.05f, 0.05f), Dark);

        for (int s = -1; s <= 1; s += 2)
        {
            Ball(root, $"Ear_{s}", new Vector3(s * 0.16f, 0.93f, 0.01f), new Vector3(0.17f, 0.17f, 0.10f), Fur);
            Ball(root, $"Eye_{s}", new Vector3(s * 0.09f, 0.79f, 0.19f), new Vector3(0.05f, 0.06f, 0.04f), Dark);
            // 다리는 앞으로 뻗는다 — 기단 끝에 걸터앉은 모양
            Ball(root, $"Leg_{s}", new Vector3(s * 0.17f, 0.12f, 0.26f), new Vector3(0.20f, 0.18f, 0.46f), Fur);
            Ball(root, $"Arm_{s}", new Vector3(s * 0.28f, 0.42f, 0.08f), new Vector3(0.16f, 0.30f, 0.18f), Fur);
        }

        // 빨간 리본 — 본관 박공의 곰과 같은 표식이라 «우리 쪽» 이라는 게 설명 없이 읽힌다
        Ball(root, "Bow", new Vector3(0f, 0.60f, 0.16f), new Vector3(0.26f, 0.13f, 0.10f), Ribbon);

        root.gameObject.SetActive(false);
        return root;
    }

    static void Ball(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;

        // 눌린 캡슐·구 콜라이더는 커다란 구로 부푼다(CLAUDE.md) — 장식이니 전부 뗀다
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color, Finish.무광);
    }
}
