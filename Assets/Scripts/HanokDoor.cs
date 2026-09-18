using System;
using UnityEngine;

/// <summary>
/// 한옥 <b>여닫이문</b> 한 짝. 세 씬이 같이 쓴다 — <see cref="HanokRoof"/> 와 같은 방식.
///
/// 2026-09-17 유저: *"다들 문이 없는데 어떻게 들어가고 나간 거야."* 맞는 말이다.
/// 박물관인데 벽이 통짜라 방들이 서로 <b>이어져 보이지 않았다.</b> 별관에는 문이 있긴 했는데
/// 갈색 판자 한 장이라 문으로 안 읽혔고, 로비와 전시실에는 아예 없었다.
///
/// 문이 방을 이어 보이게 하는 건 <b>틀</b>이다. 판자 한 장은 벽에 칠한 자국으로 보이고,
/// 문틀·문지방·상인방이 있어야 "저기가 뚫려 있다" 로 읽힌다. 그래서 판보다 틀에 조각을 더 썼다.
///
/// 열리지는 않는다. 씬 이동은 F 키와 이야기가 하고, 문은 <b>어디가 입구인지 알려주는 표지</b>야.
/// </summary>
public static class HanokDoor
{
    static readonly Color Wood    = new Color32(0x6B, 0x4A, 0x32, 0xFF);   // 문틀
    static readonly Color Leaf    = new Color32(0x8A, 0x5E, 0x3C, 0xFF);   // 문짝
    static readonly Color Paper   = new Color32(0xF2, 0xE6, 0xCC, 0xFF);   // 한지 — 안쪽 불빛
    static readonly Color Slat    = new Color32(0x5A, 0x3E, 0x2A, 0xFF);   // 살
    static readonly Color Plaque  = new Color32(0x3A, 0x2E, 0x24, 0xFF);   // 현판
    static readonly Color Handle  = new Color32(0xC9, 0xA2, 0x27, 0xFF);   // 문고리

    /// <summary>
    /// <paramref name="at"/> 는 문지방 한가운데(바닥). <paramref name="facing"/> 의 +Z 가 바깥쪽.
    /// <paramref name="plaque"/> 를 켜면 상인방 위에 현판이 붙는다 — 건물 이름을 달 자리야.
    /// </summary>
    public static GameObject Build(Transform parent, Vector3 at, Quaternion facing,
                                   float width, float height, Func<Color, Material> material,
                                   bool plaque = true, string buildingName = "",
                                   string department = "", string motto = "",
                                   float plaqueHeight = 0f)
    {
        var root = new GameObject("Door");
        root.transform.SetParent(parent, false);
        // 부모 기준 좌표다. 건물이 돌아가 있어도 "정면 한가운데" 를 그대로 적을 수 있게.
        root.transform.localPosition = at;
        root.transform.localRotation = facing;

        float half = width * 0.5f;

        // ---- 틀 : 문을 문으로 보이게 하는 건 판이 아니라 이쪽이다 ----
        for (int s = -1; s <= 1; s += 2)
            Piece(root, $"Jamb_{s}", new Vector3(s * (half + 0.14f), height * 0.5f, 0f),
                  new Vector3(0.28f, height + 0.1f, 0.62f), Wood, material);

        Piece(root, "Lintel", new Vector3(0f, height + 0.17f, 0f),
              new Vector3(width + 0.84f, 0.34f, 0.7f), Wood, material);

        // 문지방 — 이게 없으면 문짝이 바닥에 떠 있는 것처럼 보인다
        Piece(root, "Sill", new Vector3(0f, 0.07f, 0f),
              new Vector3(width + 0.5f, 0.14f, 0.72f), Wood, material);

        // ---- 문짝 둘 ----
        float leaf = half - 0.04f;
        for (int s = -1; s <= 1; s += 2)
        {
            float cx = s * leaf * 0.5f;

            Piece(root, $"Leaf_{s}", new Vector3(cx, height * 0.5f + 0.07f, 0.02f),
                  new Vector3(leaf, height - 0.14f, 0.1f), Leaf, material);

            // 한지 — 안에 불이 켜져 있다는 신호. 이것 하나로 "들어갈 수 있는 곳" 이 된다
            Piece(root, $"Paper_{s}", new Vector3(cx, height * 0.55f + 0.07f, 0.075f),
                  new Vector3(leaf - 0.22f, height * 0.62f, 0.03f), Paper, material);

            // 격자살 — 세로 셋, 가로 둘. 한지 앞에 얹혀야 창살로 보인다
            for (int v = -1; v <= 1; v++)
                Piece(root, $"SlatV_{s}_{v}", new Vector3(cx + v * leaf * 0.27f, height * 0.55f + 0.07f, 0.1f),
                      new Vector3(0.055f, height * 0.62f, 0.03f), Slat, material);

            for (int h = 0; h < 2; h++)
                Piece(root, $"SlatH_{s}_{h}", new Vector3(cx, height * (0.4f + h * 0.3f) + 0.07f, 0.1f),
                      new Vector3(leaf - 0.22f, 0.055f, 0.03f), Slat, material);

            // 문고리 — 두 짝이 만나는 쪽에
            Piece(root, $"Handle_{s}", new Vector3(-s * 0.12f, height * 0.42f, 0.12f),
                  new Vector3(0.1f, 0.22f, 0.06f), Handle, material);
        }

        // ---- 현판 : 건물 이름 ----
        if (plaque)
        {
            // 2026-09-17 유저: "건물 글자가 잘 안 보인다." 현판이 문폭의 62% 라 너무 작았다.
            // 달리면서 스쳐 보는 간판은 <b>건물에 비해 과하다 싶을 만큼</b> 커야 읽힌다.
            // 판을 <b>학과 줄까지 덮을 만큼</b> 키운다. 작은 글씨가 나무 기둥 위에 얹히면
            // 배경이 밝아서 안 읽힌다(2026-09-17 유저: "노란 글씨도 잘 안 보여").
            // 어두운 판 위에 올려야 밝은 글씨가 산다.
            // <b>한옥 현판은 처마 밑에 건다.</b> 문 바로 위에 붙이면 높은 건물에서는
            // 벽 한가운데에 뜬 채로 보인다(2026-09-17 유저: "위치가 이상해").
            // plaqueHeight 를 주면 그 높이에, 안 주면 문 바로 위에.
            float plaqueY = plaqueHeight > 0f ? plaqueHeight : height + 0.9f;

            var board = Piece(root, "Plaque", new Vector3(0f, plaqueY, 0.06f),
                              new Vector3(width * 1.15f, 1.16f, 0.16f), Plaque, material);

            // 현판 테두리 — 검은 판만 있으면 벽에 뚫린 구멍처럼 보인다
            Piece(root, "PlaqueFrame", new Vector3(0f, plaqueY, 0.03f),
                  new Vector3(width * 1.26f, 1.32f, 0.1f), Wood, material);

            // 현판을 매다는 끈 두 줄 — 처마 밑에 걸린 것처럼 보이게. 벽에 박힌 판과 다르다.
            if (plaqueHeight > 0f)
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Piece(root, $"PlaqueRope_{s2}", new Vector3(s2 * width * 0.38f, plaqueY + 0.9f, 0.05f),
                          new Vector3(0.07f, 0.65f, 0.07f), Slat, material);

            // <b>현판에는 이름만.</b> 작은 줄까지 얹으면 판이 모자라서 넘치고, 처마 밑이라
            // 높아서 작은 글씨는 어차피 안 읽힌다(2026-09-17 유저: "작은 글씨가 깨져 보인다").
            var sign = board.AddComponent<BuildingSign>();
            sign.buildingName = buildingName;
            sign.maxLine = 0.62f;
        }

        // ---- 안내판 : 학과와 한 줄은 <b>눈높이</b>에 따로 세운다 ----
        // 처마 밑 현판은 멀리서 "저 건물 이름" 을 읽는 것이고, 안내판은 문 앞에 서서
        // "여기가 뭐 하는 데" 를 읽는 것이다. 둘은 <b>보는 거리가 달라서</b> 한 판에 못 넣는다.
        if (plaque && !(string.IsNullOrEmpty(department) && string.IsNullOrEmpty(motto)))
        {
            float side = width * 0.5f + 1.5f;

            Piece(root, "BoardPost", new Vector3(side, 0.75f, 0.5f),
                  new Vector3(0.14f, 1.5f, 0.14f), Wood, material);
            Piece(root, "BoardFoot", new Vector3(side, 0.08f, 0.5f),
                  new Vector3(0.6f, 0.16f, 0.6f), Wood, material);

            // 한 줄 문구까지 넉넉히 담게 조금 키웠다(2026-09-18). 네 줄이 들어간다.
            //
            // ★ <b>판과 테두리의 z 범위가 겹치면 지지직거린다</b>(2026-09-18 유저 제보).
            // 전에는 판 0.44~0.56, 테두리 0.42~0.50 이라 <b>0.06m 가 겹쳤다</b> —
            // 두 면이 같은 깊이에 있으면 GPU 가 앞뒤를 못 정해서 픽셀마다 번갈아 찍힌다.
            // 테두리를 판 <b>완전히 뒤로</b> 뺀다: 테두리 0.36~0.44, 판 0.44~0.56. 맞닿을 뿐 안 겹친다.
            var notice = Piece(root, "Board", new Vector3(side, 1.66f, 0.5f),
                               new Vector3(2.6f, 1.25f, 0.12f), Plaque, material);
            Piece(root, "BoardFrame", new Vector3(side, 1.66f, 0.4f),
                  new Vector3(2.8f, 1.45f, 0.08f), Wood, material);

            // 작은 지붕 한 겹 — 비 가리는 시늉. 판만 서 있으면 표지판이 아니라 널빤지다
            Piece(root, "BoardRoof", new Vector3(side, 2.42f, 0.42f),
                  new Vector3(3.0f, 0.1f, 0.5f), Slat, material);

            var info = notice.AddComponent<BuildingSign>();
            info.department = department;
            info.motto = motto;
            info.maxLine = 0.3f;
        }

        return root;
    }

    /// <summary>문에는 충돌체를 안 단다 — 벽이나 건물 몸통이 이미 막고 있다.</summary>
    static GameObject Piece(GameObject parent, string name, Vector3 local, Vector3 size,
                            Color color, Func<Color, Material> material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = local;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material(color);
        go.isStatic = true;

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
            else UnityEngine.Object.DestroyImmediate(collider);
        }
        return go;
    }
}
