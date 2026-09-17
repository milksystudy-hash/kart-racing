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
    static readonly Color ColMapleRed = new Color32(0xB5, 0x5A, 0x3C, 0xFF);
    static readonly Color ColMapleGold= new Color32(0xC9, 0x93, 0x3E, 0xFF);
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

        doorFronts.Clear();
        footprints.Clear();

        BuildGround();
        ScatterGroundPatches();
        BuildPlaza();
        BuildPond(new Vector3(30f, 0f, 24f));

        // <b>yaw 0 이 캠퍼스 안쪽이다.</b> Hanok 의 정면은 +Z 인데 180 을 주면 −Z 를 보게 되고,
        // 본관은 남쪽 끝에 있어서 그게 곧 담장 바깥이다. 문도 곰 얼굴도 밖을 보고 있었다
        // (2026-09-17, 문 뚫고 들어가는 검사에서 잡혔다).
        BuildMainHall(new Vector3(0f, 0f, -107f), 0f);       // 곰 본관 — 캠퍼스 안쪽을 본다
        // 정면을 안쪽으로 돌리니 계단과 처마가 코스 쪽으로 나와서 −102 에서는 0.4m 밖에 안 남았다.
        // −107 로 물렸다. 남쪽 담장(−108)을 건물이 가로지르는데, 담장에 물린 건물은
        // 원래 있는 구조(문간채)라 어색하지 않다 — 이 건물은 처음부터 담장을 넘고 있었다.
        BuildGate(new Vector3(0f, 0f, 100f));                 // 한옥 정문
        BuildTicketBooth(new Vector3(-26f, 0f, -88f), 150f);

        BuildCampusHalls();

        // <b>담장은 건물 다음에.</b> 건물이 어디 있는지 알아야 비켜갈 수 있다.
        BuildPerimeterWall();

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

        // <b>담장은 건물을 비켜간다.</b> 큰 건물은 코스와 담장 사이에 안 들어가서 담장을
        // 넘을 수밖에 없는데, 안에 들어갈 수 있게 되고 나서는 <b>담장이 방을 가로지른다</b>
        // (2026-09-17, 본관과 곰밥마당이 그랬다). 건물마다 손으로 구멍을 내면 건물을 옮길
        // 때마다 또 틀리니까, <b>담장이 알아서 끊기게</b> 만든다. 담장에 물린 건물은
        // 문간채라 어색하지도 않다.
        Side(root, "S", -wallHalfZ, alongX: true);
        Side(root, "N",  wallHalfZ, alongX: true);
        Side(root, "W", -wallHalfX, alongX: false);
        Side(root, "E",  wallHalfX, alongX: false);
    }

    /// <summary>담장 한 변. 건물 발자국이 걸치는 구간을 빼고 남은 토막만 세운다.</summary>
    void Side(Transform root, string tag, float fixedCoord, bool alongX)
    {
        float half = alongX ? wallHalfX : wallHalfZ;

        // 이 변을 가로지르는 건물들의 구간을 모은다
        var cuts = new System.Collections.Generic.List<(float from, float to)>();
        foreach (var box in footprints)
        {
            float across = alongX ? box.center.z : box.center.x;
            float reach = alongX ? box.extents.z : box.extents.x;
            if (Mathf.Abs(across - fixedCoord) > reach) continue;   // 이 변에 안 닿는다

            float mid = alongX ? box.center.x : box.center.z;
            float span = alongX ? box.extents.x : box.extents.z;
            cuts.Add((mid - span, mid + span));
        }
        cuts.Sort((a, b) => a.from.CompareTo(b.from));

        // 남은 토막을 세운다
        float cursor = -half;
        int piece = 0;

        foreach (var (from, to) in cuts)
        {
            if (to <= cursor) continue;
            if (from > cursor) Pane(root, $"Wall_{tag}{piece++}", cursor, Mathf.Min(from, half), fixedCoord, alongX);
            cursor = Mathf.Max(cursor, to);
            if (cursor >= half) break;
        }
        if (cursor < half) Pane(root, $"Wall_{tag}{piece}", cursor, half, fixedCoord, alongX);
    }

    void Pane(Transform root, string name, float from, float to, float fixedCoord, bool alongX)
    {
        float length = to - from;
        if (length < 1f) return;

        float mid = (from + to) * 0.5f;
        Vector3 pos = alongX ? new Vector3(mid, 0f, fixedCoord) : new Vector3(fixedCoord, 0f, mid);
        Vector3 size = alongX ? new Vector3(length, 0f, 2.2f) : new Vector3(2.2f, 0f, length);

        Block(root, name, pos + Vector3.up * (wallHeight * 0.5f), Quaternion.identity,
              new Vector3(size.x, wallHeight, size.z), ColStoneWall);
        Block(root, name + "_Tile", pos + Vector3.up * (wallHeight + 0.2f), Quaternion.identity,
              new Vector3(size.x + 1.2f, 0.4f, size.z + 1.2f), ColWallTile, noCollider: true);
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
    /// <summary>
    /// 캠퍼스 건물 열셋. <b>설정: 박물관은 폐교한 곰 전문대 부지에 들어섰고 간판만 남았다</b>
    /// (2026-09-17). 그래서 철거는 이 자리의 <b>두 번째 죽음</b>이고, 한옥 캠퍼스에
    /// 학과 건물이 왜 서 있는지도 설명이 된다. 기획서의 박물관 설정은 하나도 안 건드린다.
    ///
    /// 이름은 유저가 지었고 <b>곰이 스스로를 까는 구조</b>라서 세다 —
    /// 공예과가 "곰손관"(손재주 없는 손), 연구동이 "곰머리관"(미련한 머리),
    /// 방송동이 "웅성관"(웅성거림). 자학이 제일 센 농담이야.
    ///
    /// <b>자리는 트랙 바깥 고리에만 잡는다.</b> 코스가 x −78~70 · z −78~74 를 돌아서
    /// 그 안쪽은 광장과 연못이 이미 쓰고 있다. 겹치는지는 눈이 아니라 재서 확인했다
    /// (미러 프로젝트 `Editor/_Campus.cs` — 코스 양 끝까지 포함해서 검사).
    /// </summary>
    void BuildCampusHalls()
    {
        // (이름, 학과, x, z, yaw, 폭, 깊이, 높이, 한 줄)
        var halls = new (string name, string dept, float x, float z, float yaw,
                         float w, float d, float h, string motto)[]
        {
            ("곰손관",   "조리·제빵·공예·봉제",      -99f, -23f,  75f, 22f, 14f, 9f, "손재주는 타고나는 게 아니랍니다"),
            ("곰머리관", "인문·교육·연구·심리",      -90f,  42f,  95f, 18f, 12f, 8f, ""),
            ("곰누리관", "관광·외국어·박물관·국제문화", 74f,  80f, 205f, 20f, 13f, 9f, ""),
            ("재주관",   "미술·음악·영상·공연",       96f, -12f, 275f, 21f, 13f, 9f, "재주는 곰이 넘고 돈은 딴 놈이 번다"),
            ("곰테크관", "게임·공학·기계·카트",       94f, -86f, 320f, 17f, 12f, 8f, ""),

            ("철곰관",   "경호·체육·안전",          -102f, -65f,  55f, 19f, 13f, 8f, ""),
            ("웅성관",   "방송·언론·홍보·마케팅",     -80f,  90f, 130f, 18f, 12f, 9f, ""),
            ("곰생회관", "학생회",                    42f, 101f, 195f, 17f, 12f, 8f, ""),
            ("참잘했어요관", "시상·전시",             98f,  36f, 262f, 16f, 12f, 8f, ""),
            ("대충기념관", "기념",                    92f, -52f, 300f, 15f, 11f, 7f, "2026년 준공"),
            ("곰밥마당", "학생식당·카페·조리실습",   -73f, -107f, 10f, 30f, 19f, 9f, "곰국 없음"),
            ("곰짝박수마당", "야외 행사",             49f, -106f,  15f, 15f, 11f, 7f, ""),
            // 유저: "웅지관 오른쪽에 아무것도 없으면 화장실로 쓰자." 정비 곰이 노상 변기 얘기를
            // 하는데 정작 화장실이 없었다 — 이제 있다.
            ("화장실",   "손 씻는 곳",                30f, -100f,   0f,  8f,  7f, 6f, "정비 중"),
        };

        foreach (var hall in halls)
            Hanok(built, hall.name, new Vector3(hall.x, 0f, hall.z), hall.yaw,
                  hall.w, hall.d, hall.h, 3.6f, 4.2f, hall.dept, hall.motto);
    }

    GameObject Hanok(Transform parent, string name, Vector3 position, float yaw,
                     float width, float depth, float height,
                     float doorWidth = 3.6f, float doorHeight = 4.2f,
                     string department = "", string motto = "")
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var t = go.transform;

        // <b>속을 비운다.</b> 통짜 상자면 문이 아무리 예뻐도 들어갈 데가 없다
        // (2026-09-17 유저: "모든 건물에 E 눌러서 문 열고, 안을 모델링 해줘").
        // 벽 넉 장 + 바닥 + 천장으로 짓고 정면에 문 만큼 구멍을 낸다.
        Hollow(t, width, depth, height, doorWidth + 0.9f);
        Interior(t, name, width, depth, height);

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

        Flavour(t, name, width, depth, height);

        // 정면 문과 창 — +Z 쪽이 앞이다
        float front = depth * 0.5f + 0.1f;
        // 갈색 판자 한 장은 문으로 안 읽힌다(2026-09-17 유저). 문틀이 있어야 뚫려 보인다.
        // 현판은 <b>처마 바로 밑</b>(건물 높이 −1.1)에 건다. 문 위가 아니라 — 한옥이 그렇고,
        // 달리면서 보면 높이 걸린 쪽이 훨씬 잘 보인다. 처마는 height+0.35 에 있다.
        var door = HanokDoor.Build(t, new Vector3(0f, 0f, front), Quaternion.identity,
                                   doorWidth, doorHeight, FlatMaterial.Get,
                                   plaque: true, buildingName: name, department: department, motto: motto,
                                   plaqueHeight: height - 1.1f);

        // 문 앞 6m 를 기억해 둔다 — 나무가 문을 막지 않게
        doorFronts.Add(t.TransformPoint(new Vector3(0f, 0f, front + 6f)));

        // 담장이 이 건물을 비켜가게. 회전까지 감안해 <b>바깥으로 넉넉히</b> 잡는다 —
        // 담장이 방을 조금이라도 가로지르면 걸어서 지나갈 수가 없다.
        float reach = Mathf.Max(width, depth) * 0.5f + 3f;
        footprints.Add(new Bounds(position, new Vector3(reach * 2f, 40f, reach * 2f)));

        // 문짝 둘을 젖힐 수 있게. 문틀이 실제로 뚫려 있으니 열고 걸어 들어가면 된다.
        var hinge = door.AddComponent<HingedDoor>();
        hinge.label = name;
        hinge.leaves = new[] { door.transform.Find("Leaf_-1"), door.transform.Find("Leaf_1") };
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"Window_{i}", new Vector3(i * width * 0.28f, 2.6f, front), Quaternion.identity,
                  new Vector3(2.6f, 2.2f, 0.25f), ColWindow, noCollider: true);

        return go;
    }

    /// <summary>
    /// 벽 넉 장 + 바닥 + 천장. 정면 가운데에 <paramref name="gap"/> 만큼 구멍을 남긴다.
    ///
    /// 통짜 상자를 벽 넉 장으로 바꾸면 조각이 하나에서 여덟로 는다(건물 열셋이면 91개).
    /// 그래도 싼 편이야 — <b>안에 들어갈 수 있다는 게 건물 하나를 방 하나로 바꾼다.</b>
    /// 창도 이제 진짜로 안이 비친다.
    /// </summary>
    void Hollow(Transform t, float w, float d, float h, float gap)
    {
        const float wall = 0.6f;
        float halfW = w * 0.5f, halfD = d * 0.5f;

        Block(t, "WallBack", new Vector3(0f, h * 0.5f, -halfD + wall * 0.5f), Quaternion.identity,
              new Vector3(w, h, wall), ColCream);
        for (int s2 = -1; s2 <= 1; s2 += 2)
            Block(t, $"WallSide_{s2}", new Vector3(s2 * (halfW - wall * 0.5f), h * 0.5f, 0f),
                  Quaternion.identity, new Vector3(wall, h, d), ColCream);

        // 정면은 문 구멍을 남기고 둘로 나눈다
        float pane = (w - gap) * 0.5f;
        if (pane > 0.2f)
            for (int s2 = -1; s2 <= 1; s2 += 2)
                Block(t, $"WallFront_{s2}", new Vector3(s2 * (gap + pane) * 0.5f, h * 0.5f, halfD - wall * 0.5f),
                      Quaternion.identity, new Vector3(pane, h, wall), ColCream);

        // 문 위 인방 — 구멍이 천장까지 뚫려 있으면 건물이 잘린 것처럼 보인다
        float lintel = h - 4.6f;
        if (lintel > 0.4f)
            Block(t, "WallOverDoor", new Vector3(0f, h - lintel * 0.5f, halfD - wall * 0.5f),
                  Quaternion.identity, new Vector3(gap, lintel, wall), ColCream);

        Block(t, "InFloor", new Vector3(0f, 0.05f, 0f), Quaternion.identity,
              new Vector3(w - wall * 2f, 0.1f, d - wall * 2f), ColPlaza);
        Block(t, "InCeiling", new Vector3(0f, h - 0.2f, 0f), Quaternion.identity,
              new Vector3(w - wall * 2f, 0.4f, d - wall * 2f), ColWood, noCollider: true);

        // 허리 패널(민트)도 넉 장으로. 통짜로 두면 방 안을 가득 채운다.
        Block(t, "SkirtBack", new Vector3(0f, 1.3f, -halfD - 0.1f), Quaternion.identity,
              new Vector3(w + 0.2f, 2.6f, 0.3f), ColMint, noCollider: true);
        for (int s2 = -1; s2 <= 1; s2 += 2)
            Block(t, $"SkirtSide_{s2}", new Vector3(s2 * (halfW + 0.1f), 1.3f, 0f), Quaternion.identity,
                  new Vector3(0.3f, 2.6f, d + 0.2f), ColMint, noCollider: true);
        if (pane > 0.2f)
            for (int s2 = -1; s2 <= 1; s2 += 2)
                Block(t, $"SkirtFront_{s2}", new Vector3(s2 * (gap + pane) * 0.5f, 1.3f, halfD + 0.1f),
                      Quaternion.identity, new Vector3(pane, 2.6f, 0.3f), ColMint, noCollider: true);
    }

    /// <summary>
    /// 방 안. 관마다 <b>대여섯 개</b>만 놓는다 — 열세 방을 가득 채우면 프레임도 시간도 안 남는다.
    /// 들어갔을 때 "여기가 뭐 하는 데였구나" 만 알면 충분하고, 미니게임이 들어오면 그때 채운다.
    ///
    /// 천장등은 <b>전부 발광 재질</b>이라 조명을 안 쓴다. 방 열셋에 실시간 조명을 달면
    /// 기획서 §7.6 의 "실시간 그림자는 주요 조명 하나만" 이 무너진다.
    /// </summary>
    void Interior(Transform t, string name, float w, float d, float h)
    {
        float halfW = w * 0.5f - 0.9f, halfD = d * 0.5f - 0.9f;

        // 어느 방에나 있는 것 — 천장등 둘, 뒷벽 걸레받이
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"InLamp_{i}", new Vector3(i * w * 0.22f, h - 0.55f, 0f), Quaternion.identity,
                  new Vector3(1.6f, 0.18f, 1.6f), ColLantern, noCollider: true);

        Block(t, "InBase", new Vector3(0f, 0.28f, -halfD - 0.1f), Quaternion.identity,
              new Vector3(w - 1.2f, 0.46f, 0.18f), ColWood, noCollider: true);

        switch (name)
        {
            case "곰손관":     // 조리·제빵·공예·봉제 — 작업대와 재봉틀
                for (int i = -1; i <= 1; i++)
                    Block(t, $"InBench_{i}", new Vector3(i * 3.4f, 0.45f, halfD - 2.5f), Quaternion.identity,
                          new Vector3(2.4f, 0.9f, 1.1f), ColWoodRail, noCollider: true);
                Block(t, "InMachine", new Vector3(-halfW + 1.5f, 1.15f, 0f), Quaternion.identity,
                      new Vector3(1.1f, 0.5f, 0.6f), ColBearDark, noCollider: true);
                Block(t, "InCloth", new Vector3(halfW - 1.2f, 1.6f, 1f), Quaternion.identity,
                      new Vector3(0.5f, 2.4f, 1.6f), ColRibbon, noCollider: true);
                break;

            case "곰머리관":   // 인문·연구 — 서가
                for (int i = -1; i <= 1; i += 2)
                    for (int j = 0; j < 3; j++)
                        Block(t, $"InShelf_{i}_{j}", new Vector3(i * (halfW - 0.6f), 1.1f + j * 1.1f, j - 1f),
                              Quaternion.identity, new Vector3(0.7f, 0.18f, d - 3f),
                              j % 2 == 0 ? ColCream : ColRibbon, noCollider: true);
                Block(t, "InDesk", new Vector3(0f, 0.4f, 0f), Quaternion.identity,
                      new Vector3(3f, 0.8f, 1.4f), ColWoodRail, noCollider: true);
                break;

            case "철곰관":     // 경호·체육 — 매트와 모래주머니
                Block(t, "InMat", new Vector3(0f, 0.12f, 0f), Quaternion.identity,
                      new Vector3(w - 3f, 0.24f, d - 3f), ColBush, noCollider: true);
                for (int i = -1; i <= 1; i += 2)
                    Block(t, $"InBag_{i}", new Vector3(i * 2.6f, 1.5f, -halfD + 1.6f), Quaternion.identity,
                          new Vector3(0.6f, 2.4f, 0.6f), ColWoodRail, noCollider: true);
                break;

            case "재주관":     // 미술·음악 — 이젤과 무대
                for (int i = -1; i <= 1; i++)
                {
                    Block(t, $"InEasel_{i}", new Vector3(i * 2.6f, 1.2f, 1f), Quaternion.Euler(-12f, 0f, 0f),
                          new Vector3(1.3f, 1.6f, 0.08f), ColCream, noCollider: true);
                    Block(t, $"InEaselLeg_{i}", new Vector3(i * 2.6f, 0.4f, 1.2f), Quaternion.identity,
                          new Vector3(0.1f, 0.8f, 0.1f), ColWood, noCollider: true);
                }
                Block(t, "InStage", new Vector3(0f, 0.2f, -halfD + 1.5f), Quaternion.identity,
                      new Vector3(w - 4f, 0.4f, 2.4f), ColWoodRail, noCollider: true);
                break;

            case "곰테크관":   // 공학·카트 — 정비대와 부품
                Block(t, "InLift", new Vector3(0f, 0.55f, 0f), Quaternion.identity,
                      new Vector3(3.4f, 1.1f, 2f), ColStoneWall, noCollider: true);
                for (int i = 0; i < 4; i++)
                    Block(t, $"InTire_{i}", new Vector3(halfW - 1f, 0.35f + i * 0.35f, -2f + i * 0.2f),
                          Quaternion.identity, new Vector3(1f, 0.32f, 1f), ColBearDark, noCollider: true);
                Block(t, "InToolWall", new Vector3(-halfW + 0.3f, 2f, 0f), Quaternion.identity,
                      new Vector3(0.2f, 2f, d - 4f), ColWallTile, noCollider: true);
                break;

            case "곰누리관":   // 관광·외국어 — 지구본과 안내대
                Disc(t, "InGlobe", new Vector3(-2.5f, 1.4f, 0f), new Vector3(1.6f, 0.8f, 1.6f), ColWater);
                Block(t, "InGlobeStand", new Vector3(-2.5f, 0.4f, 0f), Quaternion.identity,
                      new Vector3(0.3f, 0.8f, 0.3f), ColWood, noCollider: true);
                Block(t, "InCounter", new Vector3(2.5f, 0.55f, 1f), Quaternion.identity,
                      new Vector3(3.4f, 1.1f, 1f), ColWoodRail, noCollider: true);
                Block(t, "InMap", new Vector3(0f, 2.4f, -halfD - 0.05f), Quaternion.identity,
                      new Vector3(w - 4f, 2f, 0.1f), ColMint, noCollider: true);
                break;

            case "웅성관":     // 방송 — 부스와 콘솔
                Block(t, "InBooth", new Vector3(-halfW + 2.2f, 1.4f, 0f), Quaternion.identity,
                      new Vector3(3.4f, 2.8f, 3.4f), ColWallTile, noCollider: true);
                Block(t, "InGlass", new Vector3(-halfW + 2.2f, 1.8f, halfD - 2.2f), Quaternion.identity,
                      new Vector3(2.6f, 1.4f, 0.08f), ColWindow, noCollider: true);
                Block(t, "InConsole", new Vector3(2f, 0.5f, 0f), Quaternion.identity,
                      new Vector3(3f, 1f, 1.2f), ColBearDark, noCollider: true);
                break;

            case "곰생회관":   // 학생회 — 긴 탁자
                Block(t, "InTable", new Vector3(0f, 0.45f, 0f), Quaternion.identity,
                      new Vector3(w - 5f, 0.9f, 1.6f), ColWoodRail, noCollider: true);
                for (int i = -2; i <= 2; i++)
                    Block(t, $"InChair_{i}", new Vector3(i * 1.6f, 0.3f, 1.6f), Quaternion.identity,
                          new Vector3(0.5f, 0.6f, 0.5f), ColWood, noCollider: true);
                break;

            case "참잘했어요관":  // 시상 — 진열장과 트로피
                Block(t, "InCase", new Vector3(0f, 1.2f, -halfD + 1f), Quaternion.identity,
                      new Vector3(w - 4f, 2.4f, 0.8f), ColWindow, noCollider: true);
                for (int i = -2; i <= 2; i++)
                    Block(t, $"InTrophy_{i}", new Vector3(i * 1.5f, 1.6f, -halfD + 1f), Quaternion.identity,
                          new Vector3(0.3f, 0.7f, 0.3f), ColLantern, noCollider: true);
                break;

            case "대충기념관":  // 이름값 — 안 푼 상자
                for (int i = 0; i < 6; i++)
                    Block(t, $"InCrate_{i}", new Vector3(-2f + (i % 3) * 2f, 0.5f + (i / 3) * 1f, i % 2 * 1.5f),
                          Quaternion.Euler(0f, i * 23f, 0f), new Vector3(1.4f, 1f, 1.4f),
                          ColWoodRail, noCollider: true);
                Block(t, "InTarp", new Vector3(0f, 1.9f, -2f), Quaternion.Euler(6f, 0f, 0f),
                      new Vector3(w - 5f, 0.1f, 3f), ColBush, noCollider: true);
                break;

            case "곰밥마당":   // 학생식당 — 한옥 급식소
                BapMadang(t, w, d, h, halfW, halfD);
                break;

            case "곰짝박수마당":  // 행사 — 접의자와 현수막
                for (int r = 0; r < 3; r++)
                    for (int c = -2; c <= 2; c++)
                        Block(t, $"InSeat_{r}_{c}", new Vector3(c * 1.3f, 0.28f, 1f + r * 1.3f),
                              Quaternion.identity, new Vector3(0.55f, 0.56f, 0.55f),
                              r % 2 == 0 ? ColWoodRail : ColWood, noCollider: true);
                Block(t, "InBanner", new Vector3(0f, 2.6f, -halfD - 0.05f), Quaternion.identity,
                      new Vector3(w - 4f, 1.4f, 0.1f), ColRibbon, noCollider: true);
                break;

            default:           // 웅지관 — 행정. 카운터와 서류함
                Block(t, "InCounter", new Vector3(0f, 0.6f, 2f), Quaternion.identity,
                      new Vector3(w - 8f, 1.2f, 1.2f), ColWoodRail, noCollider: true);
                for (int i = -2; i <= 2; i++)
                    Block(t, $"InCabinet_{i}", new Vector3(i * 2.6f, 1.1f, -halfD + 0.8f),
                          Quaternion.identity, new Vector3(1.8f, 2.2f, 0.7f), ColStoneWall, noCollider: true);
                break;
        }
    }

    /// <summary>
    /// <b>곰밥마당 — 한옥 급식소.</b> 유저가 첫 인테리어로 고른 곳(2026-09-17).
    /// *"한옥 + 곰인형 박물관 + 급식소처럼. 한국 문화 나게. 대형 박물관이라 의자가 많이 필요할지도."*
    ///
    /// 한국 급식소를 한국 급식소로 만드는 건 밥이 아니라 <b>줄 서는 동선</b>이다 —
    /// 식판 쌓인 곳 → 배식대 → 자리 → 반납대. 이 순서가 보이면 설명이 필요 없다.
    /// 그래서 이 방은 <b>한쪽 벽을 통째로 배식 줄</b>에 쓰고 나머지를 자리로 채운다.
    ///
    /// <b>곰은 안 둔다</b>(유저 지시). 곰인형 박물관이지 곰이 밥 먹는 곳이 아니야 —
    /// 대신 한옥 요소(평상·방석·주련·한지 창)와 놋그릇 색으로 한국 문화를 낸다.
    /// </summary>
    void BapMadang(Transform t, float w, float d, float h, float halfW, float halfD)
    {
        // ---- 배식 줄 : 왼쪽 벽을 따라 ----
        float lineX = -halfW + 1.2f;

        // 식판 쌓아둔 곳 — 줄의 시작
        Block(t, "InTrayStand", new Vector3(lineX, 0.45f, halfD - 2.2f), Quaternion.identity,
              new Vector3(1.6f, 0.9f, 1.4f), ColWoodRail, noCollider: true);
        for (int i = 0; i < 5; i++)
            Block(t, $"InTray_{i}", new Vector3(lineX, 0.94f + i * 0.055f, halfD - 2.2f), Quaternion.identity,
                  new Vector3(1.2f, 0.05f, 1f), ColLantern, noCollider: true);

        // 배식대 — 스테인리스 상판에 국통 넷
        Block(t, "InServe", new Vector3(lineX, 0.5f, 0f), Quaternion.identity,
              new Vector3(1.9f, 1f, d - 8f), ColStoneWall, noCollider: true);
        Block(t, "InServeTop", new Vector3(lineX, 1.03f, 0f), Quaternion.identity,
              new Vector3(2.1f, 0.08f, d - 7.6f), ColWallTile, noCollider: true);
        for (int i = -2; i <= 1; i++)
            Disc(t, $"InPot_{i + 2}", new Vector3(lineX, 1.2f, i * 2.2f + 1.1f),
                 new Vector3(1.2f, 0.28f, 1.2f), ColBearDark);

        // 위생 가림막 — 급식소에 반드시 있는 것
        Block(t, "InGuard", new Vector3(lineX + 1.1f, 1.75f, 0f), Quaternion.Euler(-18f, 0f, 0f),
              new Vector3(0.06f, 0.7f, d - 7.6f), ColWindow, noCollider: true);

        // 메뉴판 — 배식대 위 벽에
        Block(t, "InMenuBoard", new Vector3(-halfW - 0.05f, 2.9f, 0f), Quaternion.identity,
              new Vector3(0.12f, 1.6f, d - 8f), ColBearDark, noCollider: true);
        for (int i = -2; i <= 2; i++)
            Block(t, $"InMenuSlip_{i + 2}", new Vector3(-halfW + 0.05f, 3.3f - 0f, i * 1.5f),
                  Quaternion.identity, new Vector3(0.04f, 0.5f, 1.1f), ColCream, noCollider: true);

        // 반납대 — 줄의 끝. 문 가까이 둬야 나가면서 놓고 간다
        Block(t, "InReturn", new Vector3(lineX, 0.45f, -halfD + 2f), Quaternion.identity,
              new Vector3(1.8f, 0.9f, 1.8f), ColWallTile, noCollider: true);
        Block(t, "InReturnBin", new Vector3(lineX + 1.4f, 0.4f, -halfD + 2f), Quaternion.identity,
              new Vector3(0.9f, 0.8f, 0.9f), ColBearDark, noCollider: true);

        // ---- 자리 : 입식 긴 탁자 + 좌식 평상 ----
        // 대형 박물관이라 자리가 많이 필요하다는 유저 말대로 <b>서른여섯 자리</b>.
        for (int row = 0; row < 3; row++)
        {
            float z = -3.4f + row * 3.4f;
            float x = 3.2f;

            Block(t, $"InTable_{row}", new Vector3(x, 0.74f, z), Quaternion.identity,
                  new Vector3(7.2f, 0.1f, 1.1f), ColWoodRail, noCollider: true);
            for (int leg = -1; leg <= 1; leg += 2)
                Block(t, $"InTableLeg_{row}_{leg}", new Vector3(x + leg * 3.2f, 0.37f, z),
                      Quaternion.identity, new Vector3(0.14f, 0.74f, 0.9f), ColWood, noCollider: true);

            // 의자 — 한 줄에 열둘(위아래 여섯씩)
            for (int seat = -2; seat <= 3; seat++)
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 at = new Vector3(x + seat * 1.25f + 0.6f, 0.23f, z + side * 1.05f);
                    Block(t, $"InChair_{row}_{seat}_{side}", at, Quaternion.identity,
                          new Vector3(0.44f, 0.46f, 0.44f), ColWood, noCollider: true);
                    Block(t, $"InChairBack_{row}_{seat}_{side}",
                          at + new Vector3(0f, 0.42f, side * 0.2f), Quaternion.identity,
                          new Vector3(0.44f, 0.5f, 0.06f), ColWoodRail, noCollider: true);
                }
        }

        // 좌식 평상 둘 — 한옥다운 자리. 신 벗고 올라가는 마루
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 at = new Vector3(halfW - 2.6f, 0f, i * 4.2f);
            Block(t, $"InFloorSeat_{i}", at + Vector3.up * 0.22f, Quaternion.identity,
                  new Vector3(3.6f, 0.44f, 3.6f), ColWoodRail, noCollider: true);
            Block(t, $"InFloorTable_{i}", at + Vector3.up * 0.62f, Quaternion.identity,
                  new Vector3(1.7f, 0.36f, 1.7f), ColWood, noCollider: true);
            for (int c = -1; c <= 1; c += 2)
                for (int r = -1; r <= 1; r += 2)
                    Block(t, $"InCushion_{i}_{c}_{r}", at + new Vector3(c * 1.15f, 0.49f, r * 1.15f),
                          Quaternion.identity, new Vector3(0.7f, 0.1f, 0.7f),
                          c * r > 0 ? ColRibbon : ColMint, noCollider: true);
        }

        // ---- 한옥 손맛 ----
        // 주련 — 기둥에 세로로 거는 글판. 이것 하나로 방이 한옥이 된다
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"InCouplet_{i}", new Vector3(i * (halfW - 0.4f), 2.6f, halfD - 4f),
                  Quaternion.identity, new Vector3(0.1f, 2.6f, 0.5f), ColRibbon, noCollider: true);

        // 한지 창 — 뒷벽에. 안에서 보면 바깥 빛이 드는 것처럼
        for (int i = -1; i <= 1; i++)
            Block(t, $"InPaperWindow_{i + 1}", new Vector3(i * 5.5f, 3.2f, -halfD - 0.05f),
                  Quaternion.identity, new Vector3(3.4f, 2.2f, 0.12f), ColWindow, noCollider: true);

        // 물동이와 컵 — 입구 옆
        Disc(t, "InWaterJar", new Vector3(halfW - 1.4f, 0.5f, halfD - 2.4f),
             new Vector3(1.3f, 0.5f, 1.3f), ColWallTile);
        for (int i = 0; i < 4; i++)
            Block(t, $"InCup_{i}", new Vector3(halfW - 2.6f, 1.02f, halfD - 2.2f + i * 0.28f),
                  Quaternion.identity, new Vector3(0.16f, 0.2f, 0.16f), ColLantern, noCollider: true);

        // 천장 서까래 한 겹 — 급식소라도 천장이 민짜면 창고로 보인다
        for (int i = -4; i <= 4; i++)
            Block(t, $"InRafter_{i + 4}", new Vector3(i * (w * 0.1f), h - 0.75f, 0f), Quaternion.identity,
                  new Vector3(0.22f, 0.26f, d - 2f), ColWood, noCollider: true);
    }

    /// <summary>
    /// 관마다 <b>딱 그 학과로 보이게</b> 하는 물건 몇 개. 유저: *"건물 모델링이 덜 됐다.
    /// 창문은 많이 만들지 말고, 한옥 + 곰인형 박물관 컨셉이 살게."*
    ///
    /// 그래서 <b>창을 늘리지 않고 물건을 얹는다.</b> 창은 다 똑같이 생겨서 아무리 늘려도
    /// 건물이 구분이 안 되고, 한옥은 원래 벽면이 비어 있는 게 맞다. 구분은 <b>실루엣</b>이
    /// 만든다 — 굴뚝, 안테나, 무대 차양, 기울어진 기둥 같은 것들.
    ///
    /// 전부 콜라이더 없는 상자 서넛이다. 달리면서 스치는 거라 그 이상은 낭비야.
    /// </summary>
    void Flavour(Transform t, string name, float w, float d, float h)
    {
        float front = d * 0.5f + 0.1f;
        float side = w * 0.5f;

        switch (name)
        {
            case "곰손관":   // 조리·제빵·공예·봉제 — 굴뚝과 널어놓은 천
                Block(t, "Chimney", new Vector3(side * 0.55f, h + 2.4f, -d * 0.2f), Quaternion.identity,
                      new Vector3(1.5f, 3.4f, 1.5f), ColStoneWall, noCollider: true);
                Block(t, "ChimneyCap", new Vector3(side * 0.55f, h + 4.2f, -d * 0.2f), Quaternion.identity,
                      new Vector3(2.1f, 0.3f, 2.1f), ColRoof, noCollider: true);
                // 천은 <b>현판 높이를 피해서</b> 양옆으로만. 전에는 h-1.1 이라 현판을 덮었다
                // (2026-09-17 유저: "빨간 패널에 이름이 가려짐").
                for (int i = -3; i <= 3; i++)
                {
                    if (Mathf.Abs(i) < 2) continue;          // 가운데는 비운다 — 거기가 현판
                    Block(t, $"Cloth_{i + 3}", new Vector3(i * w * 0.13f, h - 2.6f, front + 1.6f),
                          Quaternion.identity, new Vector3(0.9f, 2.2f, 0.08f),
                          i % 2 == 0 ? ColRibbon : ColMint, noCollider: true);
                }
                break;

            case "곰머리관":  // 인문·연구 — 쌓아 올린 책 더미
                for (int i = 0; i < 5; i++)
                    Block(t, $"Book_{i}", new Vector3(-side + 1.6f, 0.35f + i * 0.55f, front + 2.2f),
                          Quaternion.Euler(0f, i * 17f, 0f), new Vector3(2.6f, 0.5f, 1.9f),
                          i % 2 == 0 ? ColCream : ColRibbon, noCollider: true);
                break;

            case "철곰관":   // 경호·체육 — 낮고 두껍게, 역기와 철봉
                Block(t, "BarbellBar", new Vector3(0f, 1.1f, front + 3f), Quaternion.Euler(0f, 0f, 90f),
                      new Vector3(0.22f, 5f, 0.22f), ColBearDark, noCollider: true);
                for (int s = -1; s <= 1; s += 2)
                    Block(t, $"Plate_{s}", new Vector3(s * 2.3f, 1.1f, front + 3f), Quaternion.identity,
                          new Vector3(0.4f, 1.9f, 1.9f), ColBearDark, noCollider: true);
                Block(t, "Guard", new Vector3(0f, h + 1.0f, front - 0.3f), Quaternion.identity,
                      new Vector3(w * 0.9f, 0.5f, 0.5f), ColWoodRail, noCollider: true);
                break;

            case "재주관":   // 미술·음악·영상 — 색 깃발 줄과 북
                for (int i = -3; i <= 3; i++)
                    Block(t, $"Flag_{i}", new Vector3(i * w * 0.13f, h + 1.6f, front + 0.4f),
                          Quaternion.Euler(0f, 0f, i * 5f), new Vector3(0.7f, 1.3f, 0.06f),
                          i % 3 == 0 ? ColRibbon : (i % 3 == 1 ? ColLantern : ColMint), noCollider: true);
                Disc(t, "Drum", new Vector3(side - 2.2f, 1.2f, front + 2.4f),
                     new Vector3(2.4f, 1.2f, 2.4f), ColRibbon, Quaternion.Euler(90f, 0f, 0f));
                break;

            case "곰테크관":  // 게임·공학·카트 — 셔터문과 톱니
                Block(t, "Shutter", new Vector3(-side * 0.45f, 2.2f, front), Quaternion.identity,
                      new Vector3(w * 0.34f, 4.4f, 0.2f), ColWallTile, noCollider: true);
                for (int i = 0; i < 6; i++)
                    Block(t, $"Slat_{i}", new Vector3(-side * 0.45f, 0.6f + i * 0.72f, front + 0.12f),
                          Quaternion.identity, new Vector3(w * 0.34f, 0.42f, 0.08f), ColStoneWall, noCollider: true);
                Disc(t, "Gear", new Vector3(side - 2f, h + 1.4f, front - 0.2f),
                     new Vector3(2.8f, 0.3f, 2.8f), ColWood, Quaternion.Euler(90f, 0f, 0f));
                break;

            case "곰누리관":  // 관광·외국어 — 만국기 줄
                for (int i = -5; i <= 5; i++)
                    Block(t, $"Bunting_{i}", new Vector3(i * w * 0.085f, h + 0.9f + Mathf.Abs(i) * 0.1f, front + 1.2f),
                          Quaternion.Euler(0f, 0f, 180f), new Vector3(0.55f, 0.75f, 0.05f),
                          i % 4 == 0 ? ColRibbon : (i % 4 == 1 ? ColMint : (i % 4 == 2 ? ColLantern : ColCream)),
                          noCollider: true);
                break;

            case "웅성관":   // 방송·언론 — 지붕 위 안테나와 확성기
                Block(t, "Mast", new Vector3(0f, h + 4f, -d * 0.15f), Quaternion.identity,
                      new Vector3(0.3f, 6f, 0.3f), ColBearDark, noCollider: true);
                for (int i = 0; i < 3; i++)
                    Block(t, $"Cross_{i}", new Vector3(0f, h + 5.4f + i * 0.9f, -d * 0.15f), Quaternion.identity,
                          new Vector3(3.2f - i * 0.7f, 0.16f, 0.16f), ColBearDark, noCollider: true);
                for (int s = -1; s <= 1; s += 2)
                    Block(t, $"Horn_{s}", new Vector3(s * side * 0.6f, h + 1.2f, front), Quaternion.identity,
                          new Vector3(1.1f, 1.1f, 1.4f), ColLantern, noCollider: true);
                break;

            case "곰생회관":  // 학생회 — 게시판과 현수막
                Block(t, "Board", new Vector3(0f, 1.9f, front + 2.6f), Quaternion.identity,
                      new Vector3(w * 0.7f, 3.4f, 0.24f), ColWood, noCollider: true);
                Block(t, "BoardFace", new Vector3(0f, 1.9f, front + 2.75f), Quaternion.identity,
                      new Vector3(w * 0.64f, 3f, 0.06f), ColCream, noCollider: true);
                Block(t, "Banner", new Vector3(0f, h - 1.4f, front + 0.5f), Quaternion.identity,
                      new Vector3(w * 0.86f, 1.5f, 0.08f), ColRibbon, noCollider: true);
                break;

            case "참잘했어요관":  // 시상 — 커다란 도장
                Block(t, "StampHandle", new Vector3(0f, h + 2.6f, front - 0.4f), Quaternion.identity,
                      new Vector3(0.9f, 2.4f, 0.9f), ColWood, noCollider: true);
                Disc(t, "StampHead", new Vector3(0f, h + 1.2f, front - 0.4f),
                     new Vector3(3.4f, 0.6f, 3.4f), ColRibbon);
                break;

            case "대충기념관":  // 이름값 — 비계가 그대로 있고 기둥이 기울었다
                for (int i = 0; i < 4; i++)
                    Block(t, $"Scaffold_{i}", new Vector3(-side + 0.8f + i * 1.9f, h * 0.5f, front + 0.8f),
                          Quaternion.identity, new Vector3(0.18f, h, 0.18f), ColWoodRail, noCollider: true);
                for (int i = 0; i < 3; i++)
                    Block(t, $"ScaffoldRung_{i}", new Vector3(-side + 3.6f, 1.6f + i * 2.2f, front + 0.8f),
                          Quaternion.identity, new Vector3(6f, 0.16f, 0.16f), ColWoodRail, noCollider: true);
                Block(t, "LeaningPost", new Vector3(side - 1.4f, h * 0.5f, front - 0.2f),
                      Quaternion.Euler(0f, 0f, 7f), new Vector3(0.7f, h, 0.7f), ColWood, noCollider: true);
                break;

            case "곰밥마당":  // 학생식당 — 가마솥과 평상
                Disc(t, "Cauldron", new Vector3(-side + 2.6f, 0.9f, front + 3.2f),
                     new Vector3(3.2f, 0.9f, 3.2f), ColBearDark);
                Disc(t, "CauldronLid", new Vector3(-side + 2.6f, 1.7f, front + 3.2f),
                     new Vector3(2.9f, 0.16f, 2.9f), ColStoneWall);
                for (int i = 0; i < 2; i++)
                    Block(t, $"Bench_{i}", new Vector3(side - 2.5f - i * 3.4f, 0.45f, front + 3.4f),
                          Quaternion.identity, new Vector3(2.8f, 0.3f, 2.2f), ColWoodRail, noCollider: true);
                // 차양은 <b>현판보다 낮게.</b> h-0.6 이면 현판(h-1.1) 바로 앞을 가린다
                // (2026-09-17 유저: "곰밥마당 글자가 가림판에 막힘").
                Block(t, "Awning", new Vector3(0f, h - 3.2f, front + 2.2f), Quaternion.Euler(-16f, 0f, 0f),
                      new Vector3(w * 0.8f, 0.12f, 4.4f), ColMint, noCollider: true);
                break;

            case "곰짝박수마당":  // 야외 행사 — 무대와 차양
                Block(t, "Stage", new Vector3(0f, 0.35f, front + 3.4f), Quaternion.identity,
                      new Vector3(w * 1.1f, 0.7f, 5f), ColWoodRail, noCollider: true);
                for (int s = -1; s <= 1; s += 2)
                    Block(t, $"StagePost_{s}", new Vector3(s * w * 0.5f, 2.4f, front + 5.4f),
                          Quaternion.identity, new Vector3(0.3f, 4.8f, 0.3f), ColWood, noCollider: true);
                Block(t, "Canopy", new Vector3(0f, 4.9f, front + 4.4f), Quaternion.Euler(-9f, 0f, 0f),
                      new Vector3(w * 1.15f, 0.16f, 4.2f), ColRibbon, noCollider: true);
                break;
        }
    }

    /// <summary>본관 — 정면 박공에 곰 얼굴과 빨간 리본이 달려 있다.</summary>
    void BuildMainHall(Vector3 position, float yaw)
    {
        // 본관은 제일 크고 제일 자주 보이는 건물이라 문도 크게. 별관과 같은 문이면 건물이 작아 보인다.
        // 행정동 이름만 정색한 건 일부러야 — 주변이 다 실없어야 여기가 무섭게 보인다.
        var hall = Hanok(built, "웅지관", position, yaw, 36f, 22f, 13f, 6.4f, 6.2f,
                         "리더십·경영·행정", "");
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

        // 2026-09-17 유저: "나무 모양이 다 똑같다." 한 가지를 크기만 바꿔 뿌리면
        // <b>많을수록 더 똑같아 보인다</b> — 눈이 반복을 먼저 알아채기 때문이야.
        // 세 가지를 섞는다: 소나무(옆으로 퍼짐) · 느티나무(위로 둥글게) · 단풍(작고 붉게).
        for (int i = 0; i < pineCount; i++)
        {
            if (!TryFindDecorSpot(random, out Vector3 spot)) continue;

            float scale = 0.8f + (float)random.NextDouble() * 0.9f;
            int kind = i % 5;   // 소나무 셋 · 느티 하나 · 단풍 하나

            var tree = new GameObject($"Tree_{i:00}").transform;
            tree.SetParent(trees, false);
            tree.position = spot;
            tree.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            if (kind < 3) Pine(tree, scale, random);
            else if (kind == 3) Zelkova(tree, scale, random);
            else Maple(tree, scale * 0.8f, random);
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
    /// <summary>소나무 — 옆으로 퍼지고 줄기가 굽는다. 덩어리를 <b>기울여</b> 얹어야 안 뻣뻣하다.</summary>
    void Pine(Transform t, float s, System.Random random)
    {
        float lean = (float)(random.NextDouble() * 8.0 - 4.0);

        Block(t, "Trunk", new Vector3(0f, 1.6f * s, 0f), Quaternion.Euler(0f, 0f, lean),
              new Vector3(0.6f * s, 3.2f * s, 0.6f * s), ColPineTrunk, noCollider: true);
        Block(t, "Branch", new Vector3(0.9f * s, 3.2f * s, 0.2f * s), Quaternion.Euler(0f, 24f, 62f),
              new Vector3(0.22f * s, 1.8f * s, 0.22f * s), ColPineTrunk, noCollider: true);

        Ball(t, "Canopy_A", new Vector3(0f, 3.9f * s, 0f),
             new Vector3(6.4f * s, 1.8f * s, 6.4f * s), ColPineLeaf);
        Ball(t, "Canopy_B", new Vector3(-1.5f * s, 4.9f * s, 0.8f * s),
             new Vector3(4.2f * s, 1.5f * s, 4.2f * s), ColPineLeaf);
        Ball(t, "Canopy_C", new Vector3(1.6f * s, 5.1f * s, -0.6f * s),
             new Vector3(3.6f * s, 1.3f * s, 3.6f * s), ColPineLeaf);
    }

    /// <summary>느티나무 — 줄기가 굵고 위로 둥글게. 캠퍼스 정자나무 노릇.</summary>
    void Zelkova(Transform t, float s, System.Random random)
    {
        Block(t, "Trunk", new Vector3(0f, 2.4f * s, 0f), Quaternion.identity,
              new Vector3(1.1f * s, 4.8f * s, 1.1f * s), ColPineTrunk, noCollider: true);

        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f + (float)random.NextDouble() * 30f;
            var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            Block(t, $"Limb_{i}", dir * (1.2f * s) + Vector3.up * (4.4f * s),
                  Quaternion.Euler(38f, a, 0f), new Vector3(0.3f * s, 2.2f * s, 0.3f * s),
                  ColPineTrunk, noCollider: true);
        }

        Ball(t, "Crown_A", new Vector3(0f, 7.2f * s, 0f),
             new Vector3(8.2f * s, 5.2f * s, 8.2f * s), ColBush);
        Ball(t, "Crown_B", new Vector3(-1.8f * s, 6.2f * s, 1.4f * s),
             new Vector3(5.4f * s, 3.6f * s, 5.4f * s), ColBush);
        Ball(t, "Crown_C", new Vector3(2.0f * s, 6.6f * s, -1.2f * s),
             new Vector3(4.8f * s, 3.2f * s, 4.8f * s), ColPineLeaf);
    }

    /// <summary>단풍 — 작고 붉다. 초록 사이에 몇 그루 섞이면 숲이 <b>한 덩어리로 안 뭉친다</b>.</summary>
    void Maple(Transform t, float s, System.Random random)
    {
        Block(t, "Trunk", new Vector3(0f, 1.2f * s, 0f), Quaternion.identity,
              new Vector3(0.44f * s, 2.4f * s, 0.44f * s), ColPineTrunk, noCollider: true);

        var warm = (float)random.NextDouble() < 0.5 ? ColMapleRed : ColMapleGold;

        Ball(t, "Crown_A", new Vector3(0f, 3.3f * s, 0f),
             new Vector3(4.4f * s, 3.0f * s, 4.4f * s), warm);
        Ball(t, "Crown_B", new Vector3(1.0f * s, 2.8f * s, 0.7f * s),
             new Vector3(2.8f * s, 2.0f * s, 2.8f * s), warm);
        Ball(t, "Crown_C", new Vector3(-0.9f * s, 3.0f * s, -0.6f * s),
             new Vector3(2.4f * s, 1.8f * s, 2.4f * s), ColPineLeaf);
    }

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

            // <b>문 앞은 비워둔다.</b> 들어갈 수 있는 건물이 되고 나서는 문 앞의 소나무가
            // 장식이 아니라 장애물이다 — 곰머리관 문 앞에 한 그루가 박혀 있었다(2026-09-17).
            if (TooCloseToDoor(spot)) continue;

            return true;
        }
        spot = Vector3.zero;
        return false;
    }

    /// <summary>건물 문 앞 자리. `Hanok` 이 지으면서 채운다.</summary>
    readonly System.Collections.Generic.List<Vector3> doorFronts = new System.Collections.Generic.List<Vector3>();

    /// <summary>건물이 차지한 자리. 담장이 이걸 보고 비켜간다.</summary>
    readonly System.Collections.Generic.List<Bounds> footprints = new System.Collections.Generic.List<Bounds>();

    bool TooCloseToDoor(Vector3 spot)
    {
        foreach (var door in doorFronts)
        {
            Vector3 flat = spot - door;
            flat.y = 0f;
            // 9m 로는 모자랐다 — 소나무 갓이 반경 3m 에 키가 5m 라 조금 멀리서 보면
            // 처마 밑 현판을 가린다(2026-09-17 유저: "철곰관 글자가 나무에 가려짐").
            if (flat.sqrMagnitude < 17f * 17f) return true;
        }
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
        if (noCollider) Strip(go);
        return go;
    }

    /// <summary>
    /// 콜라이더를 뗀다. <b>`Destroy` 는 에디터에서 그 자리에서 안 없앤다</b> — 다음 프레임에
    /// 지우는데 에디터 빌드에는 다음 프레임이 없다. 그래서 씬을 구워 저장하면
    /// <b>장식에 콜라이더가 전부 남아 있었다</b>(2026-09-17 발견). 나무에 부딪히고 차양에 막힌다.
    /// 플레이할 때는 Awake 에서 다시 지어져서 멀쩡했던 게 더 나빴다 — 에디터에서만 틀렸으니까.
    /// </summary>
    static void Strip(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c == null) return;
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
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
        Strip(go);   // 장식은 충돌 없이 — 카트가 걸리면 답답하다
        return go;
    }

    /// <summary>원기둥 모양에 콜라이더 없음. 평평한 바닥 문양 전용.</summary>
    GameObject Disc(Transform parent, string name, Vector3 localPosition, Vector3 scale, Color color,
                    Quaternion? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Strip(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
        go.isStatic = true;
        return go;
    }
}
