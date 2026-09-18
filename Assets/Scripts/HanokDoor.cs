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
                                   float plaqueHeight = 0f, bool openable = false)
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
        //
        // ★ <b>문짝 하나가 여섯 조각이다</b>(판·한지·세로살 셋·가로살 둘·문고리).
        // 전에는 이걸 전부 문 루트에 형제로 달았고, <see cref="HingedDoor"/> 는
        // 판(`Leaf_s`) <b>하나만</b> 옮겼다 — 판만 미끄러지고 한지와 살은 제자리에 남으니
        // 눈에는 <b>아무 일도 안 일어난 것처럼</b> 보였다(2026-09-18 유저:
        // *"문 열고 닫기는 되는데 애니메이션이 없다"*).
        //
        // <b>빈 통에 묶는다.</b> `LeafRoot_s` 를 옮기면 여섯 조각이 같이 간다 —
        // 조각을 하나씩 옮기는 코드를 쓰지 않아도 되고, 나중에 조각을 더해도 저절로 따라온다.
        //
        // ★★ <b>움직이는 문짝은 static 이면 안 된다.</b> 2026-09-18, 문이 안 열린 <b>여섯 번째</b>
        // 이자 진짜 원인. <see cref="Piece"/> 가 모든 조각에 `isStatic = true` 를 달았는데,
        // 유니티는 씬을 열 때 static 렌더러들을 <b>하나의 메시로 합쳐 버린다</b>(정적 배칭).
        // 합쳐진 뒤에는 개별 오브젝트의 트랜스폼을 옮겨도 <b>그려지는 자리가 안 바뀐다</b> —
        // 트랜스폼은 실제로 움직이니까 <b>로그도 배치모드 검사도 전부 통과하는데</b>
        // 화면만 그대로였다. 앞의 다섯 번을 헛짚은 이유가 이거야.
        //
        // 그래서 <paramref name="openable"/> 인 문만 문짝을 non-static 으로 둔다. 안 열리는 문
        // (로비·전시실)은 그대로 배칭에 맡긴다 — 공짜로 얻던 드로우콜 절약을 다 버릴 이유가 없다.
        // ★ <b>문짝이 구멍보다 좁아서 닫혀 있어도 틈이 보였다</b>(2026-09-18 유저:
        // *"문 열지도 않았는데 이미 살짝 열린 상태로 모델링되어 있다"*).
        // 전에는 `half − 0.04` 라 두 짝을 합쳐도 문폭보다 <b>8cm 좁았고</b>, 벽 구멍은
        // 그보다 더 넓었다(`Hollow` 쪽 주석 참고). 이제 `half + 0.06` 으로
        // <b>문설주에 6cm 물리게</b> 한다 — 겹치면 안 보이고, 딱 맞추면 같은 평면이라 지지직거린다.
        float leaf = half + 0.06f;
        for (int s = -1; s <= 1; s += 2)
        {
            float cx = s * leaf * 0.5f;

            var swing = new GameObject($"LeafRoot_{s}");
            swing.transform.SetParent(root.transform, false);
            swing.transform.localPosition = new Vector3(cx, 0f, 0f);

            // 이 아래는 <b>문짝 기준 좌표</b>라 x 가 0 이다. 통이 cx 를 들고 있으니까.
            Piece(swing, $"Leaf_{s}", new Vector3(0f, height * 0.5f + 0.07f, 0.02f),
                  new Vector3(leaf, height - 0.14f, 0.1f), Leaf, material);

            // 한지 — 안에 불이 켜져 있다는 신호. 이것 하나로 "들어갈 수 있는 곳" 이 된다
            Piece(swing, $"Paper_{s}", new Vector3(0f, height * 0.55f + 0.07f, 0.075f),
                  new Vector3(leaf - 0.22f, height * 0.62f, 0.03f), Paper, material);

            // 격자살 — 세로 셋, 가로 둘. 한지 앞에 얹혀야 창살로 보인다
            for (int v = -1; v <= 1; v++)
                Piece(swing, $"SlatV_{s}_{v}", new Vector3(v * leaf * 0.27f, height * 0.55f + 0.07f, 0.1f),
                      new Vector3(0.055f, height * 0.62f, 0.03f), Slat, material);

            for (int h = 0; h < 2; h++)
                Piece(swing, $"SlatH_{s}_{h}", new Vector3(0f, height * (0.4f + h * 0.3f) + 0.07f, 0.1f),
                      new Vector3(leaf - 0.22f, 0.055f, 0.03f), Slat, material);

            // 문고리 — 두 짝이 만나는 쪽 <b>자기 문짝 위</b>에.
            //
            // ★ 전에는 `-s * 0.12f - cx` 라 <b>부호가 반대</b>였다. 그러면 문고리가 제 문짝을
            // 넘어 <b>맞은편 문짝 위</b>에 얹힌다 — 닫혀 있을 때는 거기 문짝이 있으니 안 보이다가,
            // <b>문이 열리면 둘 다 가운데 빈 구멍에 떠 있다</b>(2026-09-18 유저:
            // "문 열 때 노란 경첩이 떠 있어"). 열어 봐야만 드러나는 자리 버그야.
            //
            // 문짝은 통 기준으로 −leaf/2 ~ +leaf/2 이고, 가운데를 보는 안쪽 끝이
            // `-s * leaf * 0.5` 다. 거기서 제 문짝 쪽으로 0.14 들여놓는다.
            // z 도 0.12 → 0.085 로 당겼다. 문짝 앞면이 0.07 이라 0.12 면 5cm 떠 있었다.
            Piece(swing, $"Handle_{s}", new Vector3(s * (0.14f - leaf * 0.5f), height * 0.42f, 0.085f),
                  new Vector3(0.1f, 0.22f, 0.06f), Handle, material);

            // ★★ 여기서 통째로 static 을 벗긴다. 조각마다 인자로 넘기면 <b>나중에 조각을
            // 하나 더했을 때 그것만 static 으로 남아</b> 제자리에 붙어 버린다 —
            // 한 조각만 안 움직여도 문이 부서져 보이니까 <b>통 아래 전부</b>를 훑는다.
            if (openable)
                foreach (var kid in swing.GetComponentsInChildren<Transform>(true))
                    kid.gameObject.isStatic = false;
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
