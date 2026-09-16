using UnityEngine;

/// <summary>
/// 환웅박물관 야외 캠퍼스를 코드로 세운다 — 담장, 정문, 본관, 전시동, 연못, 중앙 광장.
///
/// 레퍼런스 그림(곰인형 박물관 조감도)의 배치를 따른다:
///   북쪽에 한옥 정문, 남쪽에 곰 얼굴이 달린 본관, 동쪽에 연못과 돌다리,
///   서쪽에 전시동 몇 채, 한가운데 발바닥 문양이 새겨진 광장.
///
/// 트랙(TrackBuilder)과는 따로 논다 — 이 스크립트는 길을 만들지 않고, TrackBuilder 는
/// 건물을 만들지 않는다. 책임을 하나씩 나눠 둬야 한쪽을 고쳐도 다른 쪽이 안 깨진다.
///
/// 전부 회색 상자다. 나중에 진짜 모델이 오면 이 오브젝트를 끄고 그 자리에 넣으면 돼.
/// </summary>
public class CampusBuilder : MonoBehaviour
{
    [Header("만들기")]
    [Tooltip("끄면 캠퍼스가 아예 안 생긴다. 트랙만 보고 싶을 때")]
    public bool buildOnAwake = true;

    [Header("담장 크기")]
    public float wallHalfX = 116f;
    public float wallHalfZ = 108f;
    public float wallHeight = 4.2f;

    [Header("장식 개수")]
    [Range(0, 80)] public int pineCount = 46;
    [Range(0, 40)] public int rockCount = 22;
    [Range(0, 30)] public int lanternCount = 16;
    [Tooltip("같은 숫자를 넣으면 항상 같은 배치가 나온다")]
    public int scatterSeed = 20260914;

    // ---- 레퍼런스 그림에서 뽑은 색 ----
    static readonly Color ColGrass    = new Color32(0xC4, 0xBC, 0x92, 0xFF);
    // 잔디를 한 가지 색으로 쫙 깔면 아무리 넓어도 "바닥판 한 장" 으로 보인다.
    static readonly Color ColGrassDry = new Color32(0xB6, 0xAE, 0x84, 0xFF);
    static readonly Color ColGrassWet = new Color32(0xA6, 0xB0, 0x7E, 0xFF);
    static readonly Color ColBush     = new Color32(0x7E, 0x94, 0x62, 0xFF);
    static readonly Color ColStoneWall= new Color32(0xA8, 0xA4, 0x9A, 0xFF);
    static readonly Color ColWallTile = new Color32(0x6E, 0x7A, 0x72, 0xFF);
    static readonly Color ColWoodRail = new Color32(0x7A, 0x58, 0x3E, 0xFF);
    static readonly Color ColCream    = new Color32(0xEF, 0xE7, 0xD6, 0xFF);
    static readonly Color ColMint     = new Color32(0xB4, 0xCD, 0xBC, 0xFF);
    static readonly Color ColRoof     = new Color32(0x4E, 0x7A, 0x70, 0xFF);
    static readonly Color ColWood     = new Color32(0x6B, 0x4A, 0x33, 0xFF);
    static readonly Color ColWindow   = new Color32(0xF0, 0xC0, 0x70, 0xFF);
    static readonly Color ColBearFur  = new Color32(0xA5, 0x75, 0x4A, 0xFF);
    static readonly Color ColBearFace = new Color32(0xE6, 0xDA, 0xC4, 0xFF);
    static readonly Color ColBearDark = new Color32(0x4A, 0x33, 0x26, 0xFF);
    static readonly Color ColRibbon   = new Color32(0xC4, 0x45, 0x3E, 0xFF);
    static readonly Color ColPineLeaf = new Color32(0x5B, 0x7A, 0x4E, 0xFF);
    static readonly Color ColPineTrunk= new Color32(0x6B, 0x4F, 0x3A, 0xFF);
    static readonly Color ColRock     = new Color32(0x9A, 0x9A, 0x96, 0xFF);
    static readonly Color ColWater    = new Color32(0x6F, 0xA0, 0xA8, 0xFF);
    static readonly Color ColPlaza    = new Color32(0xC6, 0xC0, 0xB2, 0xFF);
    static readonly Color ColPaw      = new Color32(0x9A, 0x8E, 0x80, 0xFF);
    static readonly Color ColLantern  = new Color32(0xF5, 0xC0, 0x69, 0xFF);

    Transform built;

    void Awake()
    {
        if (buildOnAwake) Build();
    }

    public void Build()
    {
        if (built != null) Destroy(built.gameObject);
        built = new GameObject("~Campus").transform;
        built.SetParent(transform, false);

        BuildGround();
        ScatterGroundPatches();
        BuildPerimeterWall();
        BuildPlaza();
        BuildPond(new Vector3(30f, 0f, 24f));

        BuildMainHall(new Vector3(0f, 0f, -100f), 180f);      // 곰 본관 — 캠퍼스 안쪽을 본다
        BuildGate(new Vector3(0f, 0f, 100f));                 // 한옥 정문
        BuildTicketBooth(new Vector3(-26f, 0f, -88f), 150f);

        Hanok(built, "Annex_W1", new Vector3(-94f, 0f, -22f),  75f, 22f, 14f, 9f);
        Hanok(built, "Annex_W2", new Vector3(-90f, 0f,  42f),  95f, 18f, 12f, 8f);
        Hanok(built, "Annex_NE", new Vector3( 74f, 0f,  80f), 205f, 20f, 13f, 9f);
        Hanok(built, "Annex_E1", new Vector3( 96f, 0f, -12f), 275f, 21f, 13f, 9f);
        Hanok(built, "Annex_SE", new Vector3( 82f, 0f, -78f), 320f, 17f, 12f, 8f);

        ScatterNature();
        ScatterLanterns();
        BuildRoof();
    }

    // ==================================================================
    //  바닥 · 담장
    // ==================================================================
    void BuildGround()
    {
        // 노면(y=0)보다 살짝 낮게 깔아서 z-파이팅을 피한다
        var go = Block(built, "Grass", new Vector3(0f, -1.05f, 0f), Quaternion.identity,
                       new Vector3(wallHalfX * 2.6f, 2f, wallHalfZ * 2.6f), ColGrass);
        go.isStatic = true;
    }

    /// <summary>돌담 위에 기와를 얹고, 안쪽으로 나무 난간을 세운다.</summary>
    void BuildPerimeterWall()
    {
        var root = new GameObject("PerimeterWall").transform;
        root.SetParent(built, false);

        (Vector3 pos, Vector3 size)[] sides =
        {
            (new Vector3(0f, 0f, -wallHalfZ), new Vector3(wallHalfX * 2f, 0f, 2.2f)),
            (new Vector3(0f, 0f,  wallHalfZ), new Vector3(wallHalfX * 2f, 0f, 2.2f)),
            (new Vector3(-wallHalfX, 0f, 0f), new Vector3(2.2f, 0f, wallHalfZ * 2f)),
            (new Vector3( wallHalfX, 0f, 0f), new Vector3(2.2f, 0f, wallHalfZ * 2f)),
        };

        for (int i = 0; i < sides.Length; i++)
        {
            var (pos, size) = sides[i];
            Block(root, $"Wall_{i}", pos + Vector3.up * (wallHeight * 0.5f), Quaternion.identity,
                  new Vector3(size.x, wallHeight, size.z), ColStoneWall);
            Block(root, $"WallTile_{i}", pos + Vector3.up * (wallHeight + 0.2f), Quaternion.identity,
                  new Vector3(size.x + 1.2f, 0.4f, size.z + 1.2f), ColWallTile, noCollider: true);
        }
    }

    // ==================================================================
    //  중앙 광장 — 발바닥 문양
    // ==================================================================
    void BuildPlaza()
    {
        var root = new GameObject("Plaza").transform;
        root.SetParent(built, false);

        Disc(root, "Paving", new Vector3(0f, 0.04f, 0f), new Vector3(52f, 0.04f, 52f), ColPlaza);
        Disc(root, "Ring",   new Vector3(0f, 0.06f, 0f), new Vector3(22f, 0.03f, 22f), ColStoneWall);
        Disc(root, "Inner",  new Vector3(0f, 0.08f, 0f), new Vector3(18f, 0.03f, 18f), ColPlaza);

        // 발바닥 — 발볼 하나 + 발가락 네 개 (로비 바닥, 바깥 광장과 같은 문양)
        Disc(root, "PawPad", new Vector3(0f, 0.10f, -1.1f), new Vector3(6.4f, 0.03f, 5.4f), ColPaw);
        float[] toeX = { -3.6f, -1.25f, 1.25f, 3.6f };
        float[] toeZ = { 2.6f, 4.0f, 4.0f, 2.6f };
        for (int i = 0; i < 4; i++)
            Disc(root, $"PawToe_{i}", new Vector3(toeX[i], 0.10f, toeZ[i]),
                 new Vector3(2.4f, 0.03f, 2.4f), ColPaw);
    }

    // ==================================================================
    //  연못과 돌다리
    // ==================================================================
    void BuildPond(Vector3 center)
    {
        var root = new GameObject("Pond").transform;
        root.SetParent(built, false);
        root.position = center;

        Disc(root, "Bank",  new Vector3(0f, 0.05f, 0f), new Vector3(30f, 0.05f, 22f), ColStoneWall);
        Disc(root, "Water", new Vector3(0f, 0.02f, 0f), new Vector3(26f, 0.04f, 18f), ColWater);

        // 아치 돌다리 — 계단식 상자 다섯 개로 곡선을 흉내낸다
        var bridge = new GameObject("StoneBridge").transform;
        bridge.SetParent(root, false);
        float[] arch = { 0.5f, 1.1f, 1.45f, 1.1f, 0.5f };
        for (int i = 0; i < arch.Length; i++)
        {
            float x = (i - 2) * 4.2f;
            Block(bridge, $"Deck_{i}", new Vector3(x, arch[i], 0f), Quaternion.identity,
                  new Vector3(4.4f, 0.5f, 5.2f), ColStoneWall);
            Block(bridge, $"RailL_{i}", new Vector3(x, arch[i] + 0.7f, -2.4f), Quaternion.identity,
                  new Vector3(4.4f, 0.9f, 0.35f), ColStoneWall, noCollider: true);
            Block(bridge, $"RailR_{i}", new Vector3(x, arch[i] + 0.7f,  2.4f), Quaternion.identity,
                  new Vector3(4.4f, 0.9f, 0.35f), ColStoneWall, noCollider: true);
        }
    }

    // ==================================================================
    //  건물
    // ==================================================================
    /// <summary>크림 벽 + 민트 허리 패널 + 청록 기와 지붕. 캠퍼스의 기본 한옥 한 채.</summary>
    GameObject Hanok(Transform parent, string name, Vector3 position, float yaw,
                     float width, float depth, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var t = go.transform;

        Block(t, "Body", new Vector3(0f, height * 0.5f, 0f), Quaternion.identity,
              new Vector3(width, height, depth), ColCream);
        Block(t, "Skirt", new Vector3(0f, 1.3f, 0f), Quaternion.identity,
              new Vector3(width + 0.2f, 2.6f, depth + 0.2f), ColMint, noCollider: true);

        // 처마가 벽보다 넉넉히 나오는 게 한옥 지붕의 인상
        Block(t, "Eaves", new Vector3(0f, height + 0.35f, 0f), Quaternion.identity,
              new Vector3(width + 4.5f, 0.7f, depth + 4.5f), ColRoof, noCollider: true);
        Block(t, "Roof", new Vector3(0f, height + 1.3f, 0f), Quaternion.identity,
              new Vector3(width + 1.6f, 1.3f, depth + 1.6f), ColRoof, noCollider: true);
        Block(t, "Ridge", new Vector3(0f, height + 2.0f, 0f), Quaternion.identity,
              new Vector3(width * 0.7f, 0.5f, depth * 0.35f), ColRoof, noCollider: true);

        // 모서리 기둥
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"Post_{sx}_{sz}", new Vector3(sx * width * 0.48f, height * 0.5f, sz * depth * 0.48f),
                      Quaternion.identity, new Vector3(0.7f, height, 0.7f), ColWood, noCollider: true);

        // 정면 문과 창 — +Z 쪽이 앞이다
        float front = depth * 0.5f + 0.1f;
        Block(t, "Door", new Vector3(0f, 1.9f, front), Quaternion.identity,
              new Vector3(3.4f, 3.8f, 0.3f), ColWood, noCollider: true);
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"Window_{i}", new Vector3(i * width * 0.28f, 2.6f, front), Quaternion.identity,
                  new Vector3(2.6f, 2.2f, 0.25f), ColWindow, noCollider: true);

        return go;
    }

    /// <summary>본관 — 정면 박공에 곰 얼굴과 빨간 리본이 달려 있다.</summary>
    void BuildMainHall(Vector3 position, float yaw)
    {
        var hall = Hanok(built, "MainHall", position, yaw, 36f, 22f, 13f);
        var t = hall.transform;
        float front = 22f * 0.5f + 0.4f;

        // 곰 얼굴
        var face = new GameObject("BearEmblem").transform;
        face.SetParent(t, false);
        face.localPosition = new Vector3(0f, 11.2f, front);

        Ball(face, "Head",   Vector3.zero,                       new Vector3(7.2f, 6.4f, 1.2f), ColBearFace);
        Ball(face, "Ear_L",  new Vector3(-3.2f, 3.2f, -0.1f),    new Vector3(2.6f, 2.6f, 1.0f), ColBearFur);
        Ball(face, "Ear_R",  new Vector3( 3.2f, 3.2f, -0.1f),    new Vector3(2.6f, 2.6f, 1.0f), ColBearFur);
        Ball(face, "Muzzle", new Vector3(0f, -1.1f, 0.5f),       new Vector3(3.2f, 2.2f, 1.0f), ColBearFace);
        Ball(face, "Nose",   new Vector3(0f, -0.5f, 1.0f),       new Vector3(1.0f, 0.7f, 0.6f), ColBearDark);
        Ball(face, "Eye_L",  new Vector3(-1.8f, 0.9f, 0.8f),     new Vector3(0.8f, 0.9f, 0.5f), ColBearDark);
        Ball(face, "Eye_R",  new Vector3( 1.8f, 0.9f, 0.8f),     new Vector3(0.8f, 0.9f, 0.5f), ColBearDark);
        Ball(face, "Bow_L",  new Vector3(-1.9f, -3.4f, 0.6f),    new Vector3(2.4f, 1.7f, 0.8f), ColRibbon);
        Ball(face, "Bow_R",  new Vector3( 1.9f, -3.4f, 0.6f),    new Vector3(2.4f, 1.7f, 0.8f), ColRibbon);
        Ball(face, "BowKnot",new Vector3(0f, -3.4f, 0.8f),       new Vector3(1.2f, 1.2f, 0.8f), ColRibbon);

        // 박공 양옆의 둥근 동판
        for (int i = -1; i <= 1; i += 2)
            Disc(t, $"Medallion_{i}", new Vector3(i * 12.5f, 12.2f, front),
                 new Vector3(4.4f, 0.3f, 4.4f), ColBearFur, rotation: Quaternion.Euler(90f, 0f, 0f));

        // 정면 돌계단
        for (int i = 0; i < 3; i++)
            Block(t, $"Step_{i}", new Vector3(0f, 0.25f + i * 0.3f, front + 3.4f - i * 1.1f),
                  Quaternion.identity, new Vector3(12f, 0.6f, 3.2f - i * 0.4f), ColStoneWall);
    }

    /// <summary>한옥 정문 — 기둥 넷에 청록 기와를 얹은 삼문.</summary>
    void BuildGate(Vector3 position)
    {
        var root = new GameObject("MainGate").transform;
        root.SetParent(built, false);
        root.position = position;

        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                Block(root, $"Post_{i}_{j}", new Vector3(i * 7f, 4.5f, j * 2.2f), Quaternion.identity,
                      new Vector3(1.5f, 9f, 1.5f), ColWood);

        Block(root, "Lintel", new Vector3(0f, 9.3f, 0f), Quaternion.identity,
              new Vector3(17f, 1.2f, 6.4f), ColWood, noCollider: true);
        Block(root, "Eaves", new Vector3(0f, 10.2f, 0f), Quaternion.identity,
              new Vector3(22f, 1.0f, 10f), ColRoof, noCollider: true);
        Block(root, "Roof", new Vector3(0f, 11.2f, 0f), Quaternion.identity,
              new Vector3(18f, 1.2f, 7.5f), ColRoof, noCollider: true);
        Block(root, "Plaque", new Vector3(0f, 8.2f, 3.3f), Quaternion.identity,
              new Vector3(5.5f, 1.6f, 0.3f), ColMint, noCollider: true);
    }

    void BuildTicketBooth(Vector3 position, float yaw)
    {
        var root = new GameObject("TicketBooth").transform;
        root.SetParent(built, false);
        root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        Block(root, "Body", new Vector3(0f, 2.1f, 0f), Quaternion.identity,
              new Vector3(5f, 4.2f, 5f), ColCream);
        Block(root, "Window", new Vector3(0f, 2.6f, 2.6f), Quaternion.identity,
              new Vector3(3.4f, 2f, 0.3f), ColWindow, noCollider: true);
        Block(root, "Eaves", new Vector3(0f, 4.5f, 0f), Quaternion.identity,
              new Vector3(7.6f, 0.6f, 7.6f), ColRoof, noCollider: true);
        Block(root, "Roof", new Vector3(0f, 5.2f, 0f), Quaternion.identity,
              new Vector3(5.4f, 1f, 5.4f), ColRoof, noCollider: true);
    }

    // ==================================================================
    //  장식 — 소나무 · 바위 · 석등
    // ==================================================================
    void ScatterNature()
    {
        var trees = new GameObject("Pines").transform; trees.SetParent(built, false);
        var rocks = new GameObject("Rocks").transform; rocks.SetParent(built, false);
        var random = new System.Random(scatterSeed);

        for (int i = 0; i < pineCount; i++)
        {
            if (!TryFindDecorSpot(random, out Vector3 spot)) continue;
            float scale = 0.8f + (float)random.NextDouble() * 0.9f;

            var pine = new GameObject($"Pine_{i:00}").transform;
            pine.SetParent(trees, false);
            pine.position = spot;
            pine.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            Block(pine, "Trunk", new Vector3(0f, 1.6f * scale, 0f), Quaternion.identity,
                  new Vector3(0.7f * scale, 3.2f * scale, 0.7f * scale), ColPineTrunk, noCollider: true);
            // 소나무는 위로 뾰족한 게 아니라 옆으로 퍼지는 모양이라 넓적한 덩어리 세 개
            Ball(pine, "Canopy_A", new Vector3(0f, 3.9f * scale, 0f),
                 new Vector3(6.4f * scale, 2.0f * scale, 6.4f * scale), ColPineLeaf);
            Ball(pine, "Canopy_B", new Vector3(-1.5f * scale, 4.9f * scale, 0.8f * scale),
                 new Vector3(4.2f * scale, 1.6f * scale, 4.2f * scale), ColPineLeaf);
            Ball(pine, "Canopy_C", new Vector3(1.6f * scale, 5.1f * scale, -0.6f * scale),
                 new Vector3(3.6f * scale, 1.4f * scale, 3.6f * scale), ColPineLeaf);
        }

        for (int i = 0; i < rockCount; i++)
        {
            if (!TryFindDecorSpot(random, out Vector3 spot)) continue;
            float scale = 1.1f + (float)random.NextDouble() * 1.8f;

            Ball(rocks, $"Rock_{i:00}", spot + Vector3.up * (scale * 0.35f),
                 new Vector3(scale * 1.7f, scale * 0.9f, scale * 1.4f), ColRock,
                 rotation: Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
        }
    }

    /// <summary>
    /// 잔디 위에 큰 얼룩을 몇 장 깐다. 색 차이는 아주 약하게 준다 — 세게 주면 얼룩이
    /// 따로 놀고, 약하게 주면 "넓은 풀밭" 으로 읽힌다. 진짜 잔디밭이 그래서 안 밋밋한 거야.
    ///
    /// 콜라이더 없는 납작한 원판이라 카트는 그 위를 그냥 지나간다.
    /// </summary>
    void ScatterGroundPatches()
    {
        var root = new GameObject("GroundPatches").transform;
        root.SetParent(built, false);

        var random = new System.Random(scatterSeed + 7);
        for (int i = 0; i < 26; i++)
        {
            float x = ((float)random.NextDouble() * 2f - 1f) * wallHalfX * 1.15f;
            float z = ((float)random.NextDouble() * 2f - 1f) * wallHalfZ * 1.15f;
            float r = 22f + (float)random.NextDouble() * 36f;

            // 노면(y=0)보다 낮게. 잔디 윗면이 y=-0.05 라 그 사이에 얇게 끼워 넣는다.
            Disc(root, $"Patch_{i:00}", new Vector3(x, -0.03f, z),
                 new Vector3(r, 0.01f, r * (0.6f + (float)random.NextDouble() * 0.7f)),
                 i % 2 == 0 ? ColGrassDry : ColGrassWet,
                 Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
        }
    }

    /// <summary>
    /// 캠퍼스 전체를 한옥 지붕으로 덮는다. 야외가 아니라 <b>박물관 대청</b> 안이 되는 거야 —
    /// 무인 모형 카트가 실내 대회를 도는 설정이라 오히려 이쪽이 이야기에 맞는다.
    ///
    /// 기둥은 담장 선에만 세운다. 트랙은 반경 66m 안쪽을 도니까 카트가 기둥에 부딪힐 일이 없다.
    /// </summary>
    void BuildRoof()
    {
        float width = wallHalfX * 2.3f, depth = wallHalfZ * 2.3f;
        const float ceiling = 26f;

        HanokRoof.Build(built, Vector3.zero, width, depth, ceiling, FlatMaterial.Get);
        BuildUpperWalls(width, depth, ceiling);

        // 지붕을 받치는 기둥 — 담장 바깥쪽 모서리에만. 없으면 지붕이 공중에 떠 보인다.
        var posts = new GameObject("RoofPosts").transform;
        posts.SetParent(built, false);

        float halfW = width * 0.5f, halfD = depth * 0.5f;
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            float x = Mathf.Lerp(-halfW, halfW, t);
            float z = Mathf.Lerp(-halfD, halfD, t);
            RoofPost(posts, $"PostN_{i}", new Vector3(x, 0f, -halfD), ceiling);
            RoofPost(posts, $"PostS_{i}", new Vector3(x, 0f,  halfD), ceiling);
            if (i == 0 || i == 4) continue;   // 모서리는 위에서 이미 세웠다
            RoofPost(posts, $"PostW_{i}", new Vector3(-halfW, 0f, z), ceiling);
            RoofPost(posts, $"PostE_{i}", new Vector3( halfW, 0f, z), ceiling);
        }
    }


    /// <summary>
    /// 담장 꼭대기(4.2m)와 지붕(26m) 사이를 막는 윗벽. 이게 없으면 지평선 쪽에 트인 띠가
    /// 남아서, 지붕을 덮어도 그 틈으로 배경색이 하늘처럼 보인다.
    ///
    /// 통짜 회벽이면 절벽 같아서, 위쪽에 발광 창을 한 줄 넣어 높이를 읽히게 했다.
    /// </summary>
    void BuildUpperWalls(float width, float depth, float ceiling)
    {
        var root = new GameObject("UpperWalls").transform;
        root.SetParent(built, false);

        float halfW = width * 0.5f, halfD = depth * 0.5f;
        float bottom = wallHeight;                 // 담장 꼭대기
        float h = ceiling - bottom;
        float midY = bottom + h * 0.5f;

        (Vector3 pos, Vector3 size, bool alongX)[] sides =
        {
            (new Vector3(0f, midY, -halfD), new Vector3(width, h, 1.6f), true),
            (new Vector3(0f, midY,  halfD), new Vector3(width, h, 1.6f), true),
            (new Vector3(-halfW, midY, 0f), new Vector3(1.6f, h, depth), false),
            (new Vector3( halfW, midY, 0f), new Vector3(1.6f, h, depth), false),
        };

        for (int i = 0; i < sides.Length; i++)
        {
            var (pos, size, alongX) = sides[i];
            Block(root, $"Upper_{i}", pos, Quaternion.identity, size, ColCream, noCollider: true);

            // 창 한 줄 — 지붕 바로 아래에
            float winY = ceiling - h * 0.22f;
            int count = 9;
            for (int j = 0; j < count; j++)
            {
                float t = (j + 0.5f) / count;
                Vector3 c = alongX
                    ? new Vector3(Mathf.Lerp(-halfW, halfW, t), winY, pos.z)
                    : new Vector3(pos.x, winY, Mathf.Lerp(-halfD, halfD, t));
                Vector3 s = alongX ? new Vector3(width / count * 0.45f, h * 0.16f, 1.9f)
                                   : new Vector3(1.9f, h * 0.16f, depth / count * 0.45f);
                Block(root, $"UpperWin_{i}_{j}", c, Quaternion.identity, s, ColWindow, noCollider: true);
            }
        }
    }
    void RoofPost(Transform parent, string name, Vector3 position, float ceiling)
    {
        Block(parent, name, position + Vector3.up * (ceiling * 0.5f), Quaternion.identity,
              new Vector3(2.4f, ceiling, 2.4f), ColWood, noCollider: true);
        Block(parent, name + "_Base", position + Vector3.up * 0.7f, Quaternion.identity,
              new Vector3(3.6f, 1.4f, 3.6f), ColStoneWall, noCollider: true);
    }

    /// <summary>석등을 도로 바깥쪽 가장자리를 따라 세운다.</summary>
    void ScatterLanterns()
    {
        var root = new GameObject("StoneLanterns").transform;
        root.SetParent(built, false);

        var track = GetComponentInParent<TrackBuilder>() ?? FindFirstObjectByType<TrackBuilder>();
        if (track == null) return;

        for (int i = 0; i < lanternCount; i++)
        {
            float t = (float)i / lanternCount;
            Vector3 on = track.PointOnPath(t);
            Vector3 side = Vector3.Cross(Vector3.up, track.TangentOnPath(t));
            Vector3 spot = on + side * (track.WidthOnPath(t) * 0.5f + 3.2f);

            var go = new GameObject($"Lantern_{i:00}").transform;
            go.SetParent(root, false);
            go.position = spot;

            Block(go, "Base",    new Vector3(0f, 0.2f, 0f), Quaternion.identity,
                  new Vector3(1.1f, 0.4f, 1.1f), ColStoneWall, noCollider: true);
            Block(go, "Shaft",   new Vector3(0f, 1.3f, 0f), Quaternion.identity,
                  new Vector3(0.4f, 1.8f, 0.4f), ColStoneWall, noCollider: true);
            Block(go, "Housing", new Vector3(0f, 2.6f, 0f), Quaternion.identity,
                  new Vector3(0.9f, 0.8f, 0.9f), ColLantern, noCollider: true);
            Block(go, "Cap",     new Vector3(0f, 3.2f, 0f), Quaternion.identity,
                  new Vector3(1.4f, 0.3f, 1.4f), ColRoof, noCollider: true);
        }
    }

    /// <summary>도로와 건물을 피해서 장식 놓을 자리를 찾는다. 못 찾으면 그냥 건너뛴다.</summary>
    bool TryFindDecorSpot(System.Random random, out Vector3 spot)
    {
        var track = GetComponentInParent<TrackBuilder>() ?? FindFirstObjectByType<TrackBuilder>();

        for (int attempt = 0; attempt < 24; attempt++)
        {
            float x = (float)(random.NextDouble() * 2.0 - 1.0) * (wallHalfX - 8f);
            float z = (float)(random.NextDouble() * 2.0 - 1.0) * (wallHalfZ - 8f);
            spot = new Vector3(x, 0f, z);

            // 광장 위에는 안 놓는다
            if (spot.sqrMagnitude < 30f * 30f) continue;

            // 도로에서 충분히 떨어져 있어야 한다
            if (track != null && DistanceToTrack(track, spot) < 9f) continue;

            return true;
        }
        spot = Vector3.zero;
        return false;
    }

    static float DistanceToTrack(TrackBuilder track, Vector3 point)
    {
        float best = float.MaxValue;
        const int samples = 96;
        for (int i = 0; i < samples; i++)
        {
            Vector3 on = track.PointOnPath((float)i / samples);
            float d = Vector3.Distance(new Vector3(on.x, 0f, on.z), new Vector3(point.x, 0f, point.z))
                    - track.WidthOnPath((float)i / samples) * 0.5f;
            if (d < best) best = d;
        }
        return best;
    }

    // ==================================================================
    //  도우미
    // ==================================================================
    GameObject Block(Transform parent, string name, Vector3 localPosition, Quaternion localRotation,
                     Vector3 scale, Color color, bool noCollider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
        go.isStatic = true;
        if (noCollider) Destroy(go.GetComponent<Collider>());
        return go;
    }

    GameObject Ball(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color,
                    Quaternion? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
        go.isStatic = true;
        Destroy(go.GetComponent<Collider>());   // 장식은 충돌 없이 — 카트가 걸리면 답답하다
        return go;
    }

    /// <summary>원기둥 모양에 콜라이더 없음. 평평한 바닥 문양 전용.</summary>
    GameObject Disc(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color,
                    Quaternion? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
        go.isStatic = true;
        return go;
    }
}
