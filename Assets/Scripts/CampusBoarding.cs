using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>문에 못질한 폐쇄 판자.</b> 수집품 하나를 모을 때마다 한 동씩 걷힌다.
///
/// 2026-09-18 유저: *"이미 8개 다 모았는데 학생회실이 나무판자로 가려져 있네.
/// 다 모으기 전에는 판자 유지하고, 다 모으면 빼게 해줘."* 그리고
/// *"전시실은 덮개로 전후 연출이 되어 있는데 캠퍼스만의 전용 연출이 없을까."*
///
/// 두 요구가 같은 답이다. 전시실의 <b>먼지 덮개</b>가 진열장 여덟 개를 한 장씩 벗듯이,
/// 캠퍼스는 <b>문에 박힌 판자</b>를 한 동씩 뗀다. 같은 문법을 두 방에 쓰면
/// 플레이어가 한 번만 배우면 되고, 방마다 다른 물건이라 따라 한 티도 안 난다.
///
/// 현판의 빨간 딱지(<see cref="BuildingSign.SetClosed"/>)와 <b>짝이다</b> —
/// 딱지는 멀리서 보는 신호, 판자는 문 앞까지 걸어왔을 때 보이는 신호.
/// 거리가 다르면 다른 물건이어야 한다(현판·안내판과 같은 판단).
///
/// <b>콜라이더가 없다.</b> 판자가 진짜로 막으면 건물 열셋을 못 들어가 보고,
/// 그건 점검에 방해가 된다. 막는 건 <see cref="HingedDoor"/> 가 이미 한다.
/// </summary>
public class CampusBoarding : MonoBehaviour
{
    // ==================================================================
    //  스스로 씬에 들어온다
    // ==================================================================
    /// <summary>
    /// ★★ 2026-10-06 유저: *"초반 레이싱할 때 건물에 모든 판자가 안 세워져 있는데,
    /// 무조건 관장이랑 맞장뜨기 전까지는 나무 패널 유지해 줘."*
    ///
    /// <b>판자가 «걷힌» 게 아니라 아예 없었다.</b> 이 컴포넌트를 붙여 주는 건
    /// <c>CampusSceneBuilder</c> 뿐이라 <b>캠퍼스 씬에만</b> 있었는데, 캠퍼스 건물은
    /// <b>트랙 씬에서도</b> 지어진다(<c>CampusBuilder.buildOnAwake</c>) — 거기엔 아무도 안 붙였다.
    /// 그래서 레이스 중에 보이는 건물은 열세 동 전부 맨 문이었다.
    ///
    /// 스스로 들어오게 고쳤다. 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를
    /// 다섯 번 겪은 뒤의 기본형이고, 이러면 <b>트랙 씬을 안 건드려도</b> 들어온다.
    /// <see cref="onlyAfterFinal"/> 가 기본 켜짐이라 <b>결승을 이기기 전에는 하나도 안 걷힌다.</b>
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
        // 캠퍼스가 없는 씬(로비·전시실)에는 붙일 문이 없다
        if (Object.FindFirstObjectByType<CampusBuilder>() == null) return;

        // 캠퍼스 씬에는 리그에 이미 붙어 있다 — 두 개면 판자가 두 겹이 된다.
        // 꺼져 있을 수도 있으니 <b>Include</b> 로 찾는다(이 프로젝트에서 세 번 걸린 함정).
        if (Object.FindObjectsByType<CampusBoarding>(FindObjectsInactive.Include,
                                                     FindObjectsSortMode.None).Length > 0) return;

        new GameObject("CampusBoarding").AddComponent<CampusBoarding>();
    }

    [Tooltip("판자를 안 붙일 건물. 행정동은 없어지지 않는다 — 그게 농담이야")]
    public string neverClosed = "웅지관";

    [Tooltip("판자 색")]
    public Color plankColor = new Color32(0x7A, 0x5A, 0x3A, 0xFF);
    [Tooltip("못 자국 색")]
    public Color nailColor = new Color32(0x4A, 0x40, 0x38, 0xFF);

    /// <summary>
    /// ★★ 2026-10-02 유저: *"개발업자랑 시의원이랑 레이스 완주 못했을 때는
    /// <b>절대</b> 나무판자로 막혀 있게 해줘. 근데 화장실은 열어줘."*
    ///
    /// 전에는 «수집품 하나에 한 동씩 걷힌다» 였고, 미니게임이 있는 세 동은
    /// 아예 안 박았다(2026-09-22). 이제 <b>결승을 이기기 전에는 전부 박힌다</b> —
    /// 철거가 결정된 곳이니 그게 설정에 맞고, 다 이겼을 때 한꺼번에 걷히는 게
    /// 「지켜냈다」 를 제일 크게 만든다.
    ///
    /// ⚠ <b>대가가 있다</b>: 미니게임 셋(급식·안전훈련·따라그리기)이 결승 전까지 못 들어간다.
    /// 되돌리려면 <see cref="onlyAfterFinal"/> 을 끄면 수집품에 따라 한 동씩 걷히는
    /// 예전 방식으로 돌아간다.
    /// </summary>
    [Tooltip("켜면 결승을 이기기 전까지 전부 막힌다. 끄면 수집품마다 한 동씩 걷힌다")]
    public bool onlyAfterFinal = true;

    /// <summary>
    /// 절대 안 막는 곳. 화장실은 곰과 대화하는 자리라 늘 열려 있다.
    ///
    /// ★★ 2026-10-06 <b>웅지관(행정동)을 넣었다.</b> 열세 동이 전부 판자로 막혀 있는데
    /// <b>행정동 문만 멀쩡히 열린다</b> — 아무도 말 안 해주는 신호다.
    /// 「폐과 딱지가 안 붙는 유일한 건물」(<see cref="CampusMood.neverClosed"/>)과 같은 말을
    /// 하고 있고, 플레이어는 0/8 에 캠퍼스를 한 바퀴 돌면서 이걸 먼저 본다.
    /// </summary>
    [Tooltip("절대 안 막는 곳. 화장실은 대화 자리, 웅지관은 복선이다")]
    public string[] alwaysOpen = { "화장실", "웅지관" };

    readonly List<Transform> boards = new();
    readonly List<Transform> notices = new();
    int lastCount = -1;
    bool lastCleared;

    /// <summary>
    /// 이 문에 판자를 박나. <b>건물 정문만</b> — 화장실 칸막이 문(«왼쪽 칸»)은 아니다.
    /// <see cref="HingedDoor.boardable"/> 이 꺼져 있어도 <b>미니게임이 있는 동이면 박는다</b> —
    /// 그 예외는 미니게임을 열어 두려고 넣은 것인데, 이제 결승 전에는 전부 막는 게 맞다.
    /// </summary>
    bool ShouldBoard(HingedDoor door)
    {
        if (door == null || string.IsNullOrEmpty(door.label)) return false;

        foreach (var open in alwaysOpen)
            if (door.label == open) return false;

        bool minigame = door.GetComponentInParent<MinigameSpot>() != null
                     || (door.transform.parent != null &&
                         door.transform.parent.GetComponentInChildren<MinigameSpot>(true) != null);

        return door.boardable || minigame;
    }

    void Start()
    {
        // 현판이 곧 건물 목록이다. 이름 순으로 정렬해야 판자가 매번 같은 순서로 걷힌다 —
        // 안 그러면 다시 켤 때마다 다른 건물이 열려서 "내가 저길 열었다" 가 안 남는다.
        var doors = new List<HingedDoor>();
        foreach (var door in FindObjectsByType<HingedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ShouldBoard(door)) doors.Add(door);

        doors.Sort((a, b) => string.CompareOrdinal(a.label, b.label));

        foreach (var door in doors)
        {
            boards.Add(MakeBoards(door));
            notices.Add(MakeNotice(door));
        }
        Refresh();
    }

    void Update()
    {
        if (lastCount == CollectionState.Count && lastCleared == GrandFinal.Cleared) return;
        Refresh();
    }

    bool first = true;

    void Refresh()
    {
        lastCount = CollectionState.Count;
        lastCleared = GrandFinal.Cleared;

        // 결승을 이기기 전에는 하나도 안 걷힌다. 이기면 전부 — 한꺼번에 걷히는 게
        // 「지켜냈다」 를 제일 크게 만든다. 걷히는 순서는 Reveal 이 어긋나게 놓는다.
        float t = onlyAfterFinal
            ? (GrandFinal.Cleared ? 1f : 0f)
            : (ExhibitCatalogue.Count <= 0 ? 0f
               : Mathf.Clamp01(CollectionState.Count / (float)ExhibitCatalogue.Count));

        // 살아남은 건물 수 = 전체 × 진행도.
        int open = Mathf.RoundToInt(boards.Count * t);

        for (int i = 0; i < boards.Count; i++)
        {
            if (boards[i] == null) continue;
            bool nailed = i >= open;

            // ★ 씬을 열 때는 스냅한다. 들어가자마자 판자가 우수수 떨어지면 연출이 아니라
            // <b>로딩이 덜 된 것</b>으로 보인다. 그 뒤부터는 <b>걷히는 자리에서부터</b>
            // 순서대로 — 한 프레임에 다 걷히면 «설정 변경» 이다.
            if (first) Reveal.Snap(boards[i], nailed);
            else Reveal.Play(boards[i], nailed, Mathf.Abs(i - open) * Reveal.Step, 0.7f);

            // 출입금지 표지판은 판자와 같이 움직인다 — 판자가 걷혔는데 «출입금지» 가
            // 서 있으면 <b>어느 쪽이 맞는지</b> 플레이어가 알 수가 없다.
            if (i < notices.Count && notices[i] != null)
            {
                if (first) Reveal.Snap(notices[i], nailed);
                else Reveal.Play(notices[i], nailed, Mathf.Abs(i - open) * Reveal.Step, 0.7f);
            }
        }

        first = false;
    }

    /// <summary>
    /// 문에 X 자로 두 장, 가로로 한 장. <b>세 장이 최소</b>다 —
    /// 한 장이면 선반으로 보이고, X 자만 있으면 장식 무늬로 보인다.
    /// </summary>
    Transform MakeBoards(HingedDoor door)
    {
        // 문짝에서 문 크기를 뽑는다. 빌더가 아는 치수를 다시 받아 적으면 문을 고칠 때 또 틀린다.
        // ★ 2026-09-18 유저: *"재주관 안내판에 나무판자랑 글씨가 겹쳐 보인다."*
        // 판자를 문짝 폭 ×2.1 로 잡았더니 <b>문 구멍보다 넓어져</b> 옆에 선 안내판까지 덮었다.
        // 안내판은 문 중심에서 `문폭×0.5 + 1.5m` 에 있고 판 반폭이 1.3m 라,
        // 판자 반폭이 `문폭×0.55` 를 넘으면 그대로 부딪힌다.
        //
        // <b>판자는 문 구멍 안에만 있어야 한다.</b> 문을 막는 물건이지 벽을 막는 물건이 아니야.
        float w = 3.2f, h = 4.2f;
        Transform leaf = LeafOf(door);
        if (leaf != null)
        {
            w = Mathf.Abs(leaf.localScale.x) * 1.88f;
            h = Mathf.Abs(leaf.localScale.y);
        }

        var root = new GameObject("Boarding").transform;
        root.SetParent(door.transform, false);
        root.localPosition = new Vector3(0f, 0f, 0.22f);   // 문짝 앞으로. 같은 자리면 번쩍거린다

        float diagonal = Mathf.Sqrt(w * w + h * h) * 0.98f;
        float angle = Mathf.Atan2(h, w) * Mathf.Rad2Deg;

        Plank(root, "Cross_A", new Vector3(0f, h * 0.5f, 0f), angle, diagonal);
        Plank(root, "Cross_B", new Vector3(0f, h * 0.5f, 0.06f), -angle, diagonal);
        Plank(root, "Bar", new Vector3(0f, h * 0.34f, 0.12f), 0f, w * 0.96f);

        return root;
    }

    /// <summary>
    /// ★ <b>통(<c>LeafRoot_s</c>)의 스케일은 1 이다. 크기는 그 안의 문짝(<c>Leaf_s</c>)에 있다.</b>
    ///
    /// 통에서 읽으면 판자가 <b>1.88 × 1.0</b> 으로 나온다 — 실제 문짝은 2.06 × 4.46 이라,
    /// 문 구멍(3.9 × 4.5)을 막기는커녕 <b>무릎 높이(y 0.5)에 작은 X 자</b>가 떠 있게 된다.
    /// <see cref="HingedDoor"/> 는 2026-09-18 에 같은 함정을 고쳤는데 여기는 안 고쳐져 있었다.
    ///
    /// <b>계층을 한 겹 넣으면 그 크기를 읽던 코드를 전부 훑어라</b> — 컴파일도 되고
    /// 예외도 안 나고 조용히 엉뚱한 숫자가 나온다.
    /// </summary>
    static Transform LeafOf(HingedDoor door)
    {
        if (door.leaves == null) return null;

        // 통 안의 문짝이 먼저. 통은 늘 스케일 1 이라 못 믿는다.
        foreach (var root in door.leaves)
        {
            if (root == null) continue;
            for (int c = 0; c < root.childCount; c++)
                if (root.GetChild(c).name.StartsWith("Leaf_")) return root.GetChild(c);
        }

        // 통을 도입하기 전에 구운 씬 — 문짝이 바로 꽂혀 있다.
        foreach (var root in door.leaves)
            if (root != null && root.childCount == 0) return root;

        return null;   // 못 찾으면 기본값(3.2 × 4.2)을 쓴다. 틀린 숫자보다 낫다.
    }

    /// <summary>
    /// 문 옆에 세우는 <b>붉은 출입금지 표지판.</b> 2026-10-02 유저 요청 —
    /// *"레이싱 게임 전에 출입금지 적혀 있는 빨간 안내판을 건물마다 세워줘."*
    ///
    /// 판자만 있으면 <b>«수리 중»</b> 으로도 읽힌다. 도로 표지판처럼 생긴 붉은 판이
    /// 같이 서 있으면 «행정이 막아 놓았다» 가 되고, 그게 이 이야기의 내용이다.
    ///
    /// 자리는 <b>문 옆</b>이다 — 문 정면 한가운데는 지나다니는 길이라 비워 둔다
    /// (「문 앞에 뭘 놓을 때는 x 를 0 으로 두지 마라」 — 2026-09-18 곰생회관 게시판).
    /// 안내판(문폭×0.5 + 1.5m)과도 반대쪽에 세워서 서로 안 가린다.
    /// </summary>
    Transform MakeNotice(HingedDoor door)
    {
        if (door == null) return null;

        float w = 3.2f;
        Transform leaf = LeafOf(door);
        if (leaf != null) w = Mathf.Abs(leaf.localScale.x) * 2f;

        var root = new GameObject("EntryNotice").transform;
        root.SetParent(door.transform, false);
        // 안내판은 +x 쪽(문폭×0.5 + 1.5)에 있으니 표지판은 −x 쪽으로
        root.localPosition = new Vector3(-(w * 0.5f + 1.1f), 0f, 1.5f);

        Bar(root, "Post", new Vector3(0f, 1.0f, 0f), new Vector3(0.10f, 2.0f, 0.10f),
            new Color32(0x9A, 0x9A, 0x96, 0xFF));
        Bar(root, "Foot", new Vector3(0f, 0.05f, 0f), new Vector3(0.44f, 0.10f, 0.44f),
            new Color32(0x6E, 0x6E, 0x6A, 0xFF));

        // ★★ 2026-10-02 유저: *"출입금지 표지판이 대부분 뒤집혔고 글자도 간판 이상으로 오버다."*
        //   두 가지를 틀렸다. 첫째, <b>층을 거꾸로 쌓았다</b> — 테두리를 판 앞에 두고
        //   글자를 판 <b>뒤</b>에 뒀다. 문의 +Z 가 보는 사람 쪽이니 숫자가 커질수록 앞이다.
        //   (「층은 뒤에서 앞으로 쌓는다 · 글자는 언제나 제일 앞」 — 2026-09-18)
        Bar(root, "Rim", new Vector3(0f, 1.95f, -0.02f), new Vector3(1.06f, 0.82f, 0.05f),
            new Color32(0xF3, 0xEC, 0xDC, 0xFF));
        Bar(root, "Plate", new Vector3(0f, 1.95f, 0.01f), new Vector3(0.96f, 0.72f, 0.06f),
            new Color32(0xC4, 0x45, 0x3E, 0xFF));

        // 글자. 현판과 같은 폰트·같은 셰이더를 쓴다 — 기본 폰트 재질은 ZTest Always 라
        // <b>벽 뒤에서도 보인다</b>(2026-09-17 에 겪은 것).
        var font = Resources.Load<Font>("HudFont");
        if (font != null)
        {
            var label = new GameObject("Text").AddComponent<TextMesh>();
            label.transform.SetParent(root, false);
            //   ★★ 돌리는 것과 자리를 <b>따로따로 고치다 두 번 틀렸다.</b>
            //   처음엔 180도 + 판 뒤 → 안 보임. 다음엔 0도 + 판 앞 → 거울상("지금입출").
            //   <b>둘 다 필요했다</b>: TextMesh 는 제 −Z 쪽에서 바로 읽히니 180도를 돌리고,
            //   보이려면 판보다 앞(+Z)에 있어야 한다.
            //
            //   <b>한 번에 하나씩 바꾸면 두 조건이 동시에 맞는 자리를 못 찾는다.</b>
            //   돌림과 자리는 같이 정해야 한다.
            label.transform.localPosition = new Vector3(0f, 1.95f, 0.07f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.font = font;
            label.text = "출입금지";
            label.fontSize = 120;

            // 글자 높이 0.17m. 네 글자면 가로 약 0.68m 라 판(0.96m) 안에 들어온다.
            // 전에는 0.42m 라 네 글자가 1.68m — <b>판보다 두 배 가까이 길었다.</b>
            label.characterSize = 0.17f * 10f / 120f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color32(0xF3, 0xEC, 0xDC, 0xFF);
            label.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
        }

        return root;
    }

    void Bar(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Strip(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
    }

    void Plank(Transform parent, string name, Vector3 at, float tilt, float length)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Strip(go.GetComponent<Collider>());

        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        go.transform.localScale = new Vector3(length, 0.34f, 0.09f);
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(plankColor);

        // 못 자국 둘. 이게 있어야 "붙여 놓은 판" 이 아니라 "박아 놓은 판" 으로 읽힌다.
        for (int s = -1; s <= 1; s += 2)
        {
            var nail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nail.name = "Nail";
            Strip(nail.GetComponent<Collider>());
            nail.transform.SetParent(go.transform, false);
            // 부모가 길쭉하게 눌린 상자라 자식도 같이 눌린다. 나누어 되돌린다.
            nail.transform.localPosition = new Vector3(s * 0.42f, 0f, -0.6f);
            nail.transform.localScale = new Vector3(0.1f / length, 0.3f, 0.7f);
            nail.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(nailColor);
        }
    }

    /// <summary>에디터에는 다음 프레임이 없어서 <c>Destroy</c> 가 그 자리에서 안 없앤다.</summary>
    static void Strip(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}
