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
    [Tooltip("판자를 안 붙일 건물. 행정동은 없어지지 않는다 — 그게 농담이야")]
    public string neverClosed = "웅지관";

    [Tooltip("판자 색")]
    public Color plankColor = new Color32(0x7A, 0x5A, 0x3A, 0xFF);
    [Tooltip("못 자국 색")]
    public Color nailColor = new Color32(0x4A, 0x40, 0x38, 0xFF);

    readonly List<Transform> boards = new();
    int lastCount = -1;

    void Start()
    {
        // 현판이 곧 건물 목록이다. 이름 순으로 정렬해야 판자가 매번 같은 순서로 걷힌다 —
        // 안 그러면 다시 켤 때마다 다른 건물이 열려서 "내가 저길 열었다" 가 안 남는다.
        var doors = new List<HingedDoor>();
        foreach (var door in FindObjectsByType<HingedDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (door.boardable && !string.IsNullOrEmpty(door.label) && door.label != neverClosed)
                doors.Add(door);

        doors.Sort((a, b) => string.CompareOrdinal(a.label, b.label));

        foreach (var door in doors) boards.Add(MakeBoards(door));
        Refresh();
    }

    void Update()
    {
        if (lastCount == CollectionState.Count) return;
        Refresh();
    }

    bool first = true;

    void Refresh()
    {
        lastCount = CollectionState.Count;

        float t = ExhibitCatalogue.Count <= 0 ? 0f
                : Mathf.Clamp01(CollectionState.Count / (float)ExhibitCatalogue.Count);

        // 살아남은 건물 수 = 전체 × 진행도. 8/8 이면 전부 걷힌다.
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
