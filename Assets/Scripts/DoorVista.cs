using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ 2026-10-06 유저: *"로비 씬 기준으로 캠퍼스와 전시실에 E 를 누르면 문이 열리면서
/// 문 안에 있는 벽이 나온다. 전시실이면 전시실 배경과 이어지게, 캠퍼스면 캠퍼스 밖 배경에
/// 맞춰 줘. 지금은 문은 열리는데 해당 간판에 맞는 배경이 아니라 맨 벽이잖아."*
///
/// <b>맞는 지적이고, 문이 열리는 연출이 지금 손해를 보고 있었다.</b> 문을 여는 동작은
/// «저 너머로 이어진다» 를 보여주려고 넣은 건데, 열고 나니 벽이면 <b>열지 말걸 그랬다</b> 가 된다.
///
/// 두 가지를 한다:
/// <list type="number">
/// <item>벽에 <b>구멍을 낸다</b> — 로비 벽은 30m 짜리 상자 하나라 통째로 끌 수가 없다.
///       원래 상자의 <b>렌더러만 끄고</b> 문 자리를 비운 조각 셋으로 다시 그린다.
///       ★ <b>콜라이더는 그대로 둔다.</b> 이 문은 걸어서 통과하는 문이 아니라
///       <c>E</c> 로 씬을 옮기는 문이다 — 구멍으로 걸어 나가면 허공에 떨어진다.</item>
/// <item>구멍 너머에 <b>그 씬다운 방</b>을 짓는다. 전시실 쪽은 전시실, 캠퍼스 쪽은 바깥.</item>
/// </list>
///
/// ★ <b>전부 런타임에 짓는다.</b> 로비를 다시 굽지 않아도 들어오고, 씬 파일이 안 커진다
/// (<see cref="DeskClock"/> · <c>HanokRoof</c> 와 같은 방식).
/// </summary>
public static class DoorVista
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Build();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Build();

    static void Build()
    {
        // 로비에서만. 캠퍼스 문은 진짜로 걸어 들어가는 문이라 이런 장치가 필요 없다
        if (SceneManager.GetActiveScene().buildIndex != 0) return;
        if (Object.FindFirstObjectByType<VistaRoot>() != null) return;

        var doors = Object.FindObjectsByType<SceneDoor>(FindObjectsInactive.Include,
                                                        FindObjectsSortMode.None);
        if (doors.Length == 0) return;

        var root = new GameObject("DoorVistas").AddComponent<VistaRoot>().transform;

        foreach (var door in doors)
        {
            // 문 바깥쪽. 문패가 홀 안을 보게 서 있어서 <b>+Z 가 안쪽</b>이다 —
            // 글자를 180도 돌려 단 것과 같은 사실이야(2026-09-17).
            Vector3 out3 = -door.transform.forward;

            var wall = FindWall(door.transform.position, out3);
            if (wall == null) continue;

            Carve(root, wall, door.transform.position, out3, 1.95f, 4.6f);
            Carve(root, FindPanel(wall), door.transform.position, out3, 1.95f, 4.6f);

            // ★★ 2026-10-06 유저: *"캠퍼스 옆 판자에 «곰밥마당 · 별관» 이라고 한 거 없애.
            //   저기 가면 곰밥마당이 바로 나오는 것도 아니니까."* <b>맞다 — 문패가 곧 지도인데
            //   거짓 지도면 없느니만 못하다.</b> 빌더는 «철거 예정 구역» 으로 고쳐 뒀지만
            //   로비는 <b>구워서 저장하는 씬</b>이라 옛 글자가 씬에 박혀 있다 — 여기서 걷어낸다.
            //   (현판 «캠퍼스 / 전시실» 은 그대로 둔다. <c>name == "Plaque"</c> 로 가린다.)
            foreach (var sign in door.GetComponentsInChildren<BuildingSign>(true))
                if (sign.name != "Plaque") sign.gameObject.SetActive(false);

            if (door.sceneIndex == 2) Gallery(root, door.transform.position, out3);
            else                      Outside(root, door.transform.position, out3);
        }
    }

    // ────────────────────────────────────────────────── 벽에 구멍
    /// <summary>문 바깥쪽에서 제일 가까운 벽 상자. 이름이 아니라 <b>자리로</b> 찾는다.</summary>
    static Transform FindWall(Vector3 at, Vector3 dir)
    {
        Transform best = null;
        float bestD = float.MaxValue;

        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.name.StartsWith("Wall")) continue;
            if (r.name.EndsWith("_Panel")) continue;

            // ★★ <b>바운즈 «중심» 까지 재면 안 된다.</b> 로비 벽은 30m 짜리 상자 하나라
            //   문에서 중심까지가 7m 로 나왔고, 그래서 «가까운 벽 없음» 으로 걸러졌다 —
            //   유저가 «문 너머 배경이 그대로다» 라고 한 게 이거다.
            //   <b>상자까지의 거리</b>(SqrDistance)를 재야 «문이 붙어 있는 벽» 이 잡힌다.
            Vector3 v = r.bounds.center - at;
            if (Vector3.Dot(v, dir) <= 0f) continue;          // 바깥쪽에 있는 것만

            float d = r.bounds.SqrDistance(at);
            if (d < bestD) { bestD = d; best = r.transform; }
        }
        return bestD < 9f ? best : null;                      // 3m 안(제곱이라 9)
    }

    static Transform FindPanel(Transform wall)
    {
        if (wall == null || wall.parent == null) return null;
        var t = wall.parent.Find(wall.name + "_Panel");
        return t;
    }

    /// <summary>
    /// 상자 하나를 <b>구멍 뚫린 조각 셋</b>으로 다시 그린다.
    /// 원래 상자는 렌더러만 끈다 — <c>isStatic</c> 이라 <b>옮기거나 크기를 바꿔도 화면이 안 바뀐다</b>
    /// (정적 배칭. 이 프로젝트에서 문을 여섯 번 고치고서야 잡은 함정이야).
    /// </summary>
    static void Carve(Transform root, Transform wall, Vector3 doorAt, Vector3 outward,
                      float halfHole, float holeTop)
    {
        if (wall == null) return;

        var rend = wall.GetComponent<MeshRenderer>();
        if (rend == null || !rend.enabled) return;

        Vector3 c = wall.position, s = wall.lossyScale;
        var mat = rend.sharedMaterial;
        rend.enabled = false;

        // 벽이 x 로 긴가 z 로 긴가. 축에 나란한 상자라 큰 쪽이 곧 «길이» 다
        bool alongX = s.x > s.z;
        float len = alongX ? s.x : s.z;
        float along = alongX ? doorAt.x : doorAt.z;
        float mid = alongX ? c.x : c.z;

        float lo = mid - len * 0.5f, hi = mid + len * 0.5f;
        float a = Mathf.Clamp(along - halfHole, lo, hi);
        float b = Mathf.Clamp(along + halfHole, lo, hi);

        float bottom = c.y - s.y * 0.5f;
        float top = c.y + s.y * 0.5f;
        float cut = Mathf.Min(top, bottom + holeTop);

        Piece(root, wall.name + "_L", Mid(c, alongX, (lo + a) * 0.5f, c.y),
              Size(s, alongX, a - lo, s.y), mat);
        Piece(root, wall.name + "_R", Mid(c, alongX, (b + hi) * 0.5f, c.y),
              Size(s, alongX, hi - b, s.y), mat);
        Piece(root, wall.name + "_Top", Mid(c, alongX, (a + b) * 0.5f, (cut + top) * 0.5f),
              Size(s, alongX, b - a, top - cut), mat);
    }

    static Vector3 Mid(Vector3 c, bool alongX, float along, float y)
        => alongX ? new Vector3(along, y, c.z) : new Vector3(c.x, y, along);

    static Vector3 Size(Vector3 s, bool alongX, float along, float h)
        => alongX ? new Vector3(Mathf.Max(0f, along), h, s.z)
                  : new Vector3(s.x, h, Mathf.Max(0f, along));

    // ────────────────────────────────────────────────── 문 너머의 방
    static readonly Color Cream = new Color32(0xEF, 0xE7, 0xD6, 0xFF);
    static readonly Color Stone = new Color32(0xA8, 0xA4, 0x9A, 0xFF);
    static readonly Color Floor = new Color32(0xC6, 0xC0, 0xB2, 0xFF);
    static readonly Color Wood = new Color32(0x6B, 0x4A, 0x33, 0xFF);
    static readonly Color Tile = new Color32(0x4E, 0x7A, 0x70, 0xFF);
    static readonly Color Glass = new Color32(0x6F, 0xA0, 0xA8, 0xFF);
    static readonly Color Ribbon = new Color32(0xC4, 0x45, 0x3E, 0xFF);
    static readonly Color Leaf = new Color32(0x4E, 0x6B, 0x45, 0xFF);
    /// <summary>★ 이 둘은 <see cref="Surfaces"/> 표에서 <b>발광</b>이다 — 조명 없이 밝게 보인다.</summary>
    static readonly Color Lamp = new Color32(0xFF, 0xF6, 0xDC, 0xFF);
    static readonly Color Window = new Color32(0xF0, 0xC0, 0x70, 0xFF);

    /// <summary>전시실 쪽 — 석재 바닥 · 붉은 카펫 · 진열장 둘 · 천장 띠.</summary>
    static void Gallery(Transform root, Vector3 door, Vector3 outward)
    {
        var a = Alcove(root, "전시실_너머", door, outward, 7.5f, 5.4f, 4.9f, Floor, Cream);

        Box(a, "Carpet", new Vector3(0f, 0.03f, 3.4f), new Vector3(2.6f, 0.06f, 6.6f), Ribbon);

        for (int s = -1; s <= 1; s += 2)
        {
            Box(a, $"Case_{s}", new Vector3(s * 1.95f, 0.75f, 2.9f), new Vector3(1.1f, 1.5f, 1.1f), Glass);
            Box(a, $"Plinth_{s}", new Vector3(s * 1.95f, 0.2f, 2.9f), new Vector3(1.3f, 0.4f, 1.3f), Wood);
            Box(a, $"Case2_{s}", new Vector3(s * 1.95f, 0.75f, 5.6f), new Vector3(1.1f, 1.5f, 1.1f), Glass);
            Box(a, $"Plinth2_{s}", new Vector3(s * 1.95f, 0.2f, 5.6f), new Vector3(1.3f, 0.4f, 1.3f), Wood);
        }

        // 천장 띠 — 발광이라 조명 없이도 «켜진 방» 으로 읽힌다
        Box(a, "Cove", new Vector3(0f, 4.55f, 3.6f), new Vector3(2.0f, 0.12f, 6.8f), Lamp);
        Box(a, "Rail", new Vector3(0f, 2.9f, 7.42f), new Vector3(5.2f, 0.14f, 0.1f), Wood);
        // 안쪽 끝 벽에 액자 셋 — 복도가 «계속 이어진다» 로 보이게
        for (int i = -1; i <= 1; i++)
            Box(a, $"Art_{i}", new Vector3(i * 1.5f, 2.4f, 7.38f), new Vector3(1.0f, 1.3f, 0.08f), Wood);
    }

    /// <summary>캠퍼스 쪽 — 돌길 · 낮은 담장 · 소나무 · 멀리 건물과 밝은 빛.</summary>
    static void Outside(Transform root, Vector3 door, Vector3 outward)
    {
        var a = Alcove(root, "캠퍼스_너머", door, outward, 11f, 7.5f, 6.2f, Stone, Cream, lid: false);

        Box(a, "Path", new Vector3(0f, 0.03f, 5f), new Vector3(3.0f, 0.06f, 10f), Floor);

        for (int s = -1; s <= 1; s += 2)
        {
            Box(a, $"Fence_{s}", new Vector3(s * 2.6f, 0.7f, 5f), new Vector3(0.3f, 1.4f, 9f), Stone);
            Box(a, $"Cap_{s}", new Vector3(s * 2.6f, 1.46f, 5f), new Vector3(0.44f, 0.12f, 9f), Tile);

            Box(a, $"Trunk_{s}", new Vector3(s * 3.1f, 1.5f, 7.6f), new Vector3(0.34f, 3f, 0.34f), Wood);
            Box(a, $"Crown_{s}", new Vector3(s * 3.1f, 3.4f, 7.6f), new Vector3(2.4f, 0.9f, 2.4f), Leaf);
        }

        // 멀리 보이는 별관 — 몸통 · 처마 · 창. 실루엣만으로 «캠퍼스가 계속된다» 가 된다
        Box(a, "Far", new Vector3(0f, 2.2f, 10.2f), new Vector3(6.4f, 4.4f, 1.2f), Cream);
        Box(a, "FarEave", new Vector3(0f, 4.5f, 10.1f), new Vector3(7.6f, 0.5f, 1.8f), Tile);
        for (int i = -1; i <= 1; i++)
            Box(a, $"FarWin_{i}", new Vector3(i * 1.9f, 2.3f, 9.58f), new Vector3(1.0f, 1.2f, 0.1f), Window);

        // 바깥 빛 — 하늘을 안 그리는 게임이라(지붕으로 덮었다) <b>빛으로</b> 바깥을 말한다
        Box(a, "Daylight", new Vector3(0f, 5.4f, 5f), new Vector3(6.6f, 0.14f, 9.6f), Lamp);
    }

    /// <summary>
    /// 문 바깥에 상자 방 하나. 반환값은 <b>그 방의 로컬 좌표계</b>라서,
    /// 안의 물건은 «문에서 몇 m 앞, 좌우로 몇 m» 로만 적으면 된다 — 문이 어느 벽에 붙어
    /// 있든 같은 숫자가 통한다(<c>HanokDoor</c> 가 부모 기준 좌표를 쓰는 것과 같은 이유).
    /// </summary>
    static Transform Alcove(Transform root, string name, Vector3 door, Vector3 outward,
                            float depth, float width, float height, Color floor, Color wall,
                            bool lid = true)
    {
        var a = new GameObject(name).transform;
        a.SetParent(root, false);
        a.position = new Vector3(door.x, 0f, door.z);
        a.rotation = Quaternion.LookRotation(new Vector3(outward.x, 0f, outward.z), Vector3.up);

        float half = width * 0.5f, mid = depth * 0.5f;

        Box(a, "Floor", new Vector3(0f, -0.05f, mid), new Vector3(width, 0.1f, depth), floor);
        Box(a, "Left", new Vector3(-half, height * 0.5f, mid), new Vector3(0.3f, height, depth), wall);
        Box(a, "Right", new Vector3(half, height * 0.5f, mid), new Vector3(0.3f, height, depth), wall);
        Box(a, "Back", new Vector3(0f, height * 0.5f, depth), new Vector3(width, height, 0.3f), wall);
        if (lid) Box(a, "Ceiling", new Vector3(0f, height, mid), new Vector3(width, 0.2f, depth), Wood);
        return a;
    }

    static void Box(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());   // ★ 전부 장식. 걸어 나갈 수 있으면 안 된다
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = FlatMaterial.Get(color);
    }

    static void Piece(Transform parent, string name, Vector3 at, Vector3 size, Material mat)
    {
        if (size.x <= 0.001f || size.y <= 0.001f || size.z <= 0.001f) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());   // 막는 건 원래 벽의 콜라이더가 계속 한다
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}

/// <summary>두 번 짓지 않으려고 두는 표식.</summary>
public class VistaRoot : MonoBehaviour { }
