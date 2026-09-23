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
    /// <summary>씨름판 모래. 바닥(크림)보다 노랗고 탁해야 «판» 으로 따로 읽힌다.</summary>
    static readonly Color ColSandRing = new Color32(0xD6, 0xB8, 0x7E, 0xFF);
    static readonly Color ColAsphalt  = new Color32(0x56, 0x54, 0x52, 0xFF);
    // 레고 느낌 잡기(2026-09-18). 벽 <b>아래쪽만</b> 어두운 색 — 실제 건물은 아래가 때가 타고
    // 위가 바랜다. 차이를 작게 둔다: 세면 얼룩으로 보이고 약하면 그늘로 보인다.
    static readonly Color ColWallFoot = new Color32(0xD8, 0xCE, 0xB8, 0xFF);
    static readonly Color ColTrimDark = new Color32(0xB9, 0xAE, 0x98, 0xFF);
    static readonly Color ColGroove   = new Color32(0xC9, 0xBF, 0xAA, 0xFF);

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

        BuildOuterRoad();

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
    /// <summary>
    /// <b>담장 바깥의 길.</b> 2026-09-18 유저: *"캠퍼스 씬이면 적어도 주변 도로도 조금
    /// 만들어 주는 게 어떨까. 물론 도로에는 상호작용 못 하게 하고."*
    ///
    /// 담장 밖이 허허벌판이면 캠퍼스가 <b>세상에서 잘려 나온 조각</b>으로 보인다.
    /// 길 하나만 지나가도 "여기는 어느 동네 안" 이 된다 — 가장 싼 배경이야.
    /// 정문 앞(북쪽)에만 깐다. 사방에 깔면 조각 수만 늘고 보이지도 않는다.
    ///
    /// <b>콜라이더가 전부 없다.</b> 담장이 이미 막고 있고, 길은 보라고 있는 거지
    /// 걸어가라고 있는 게 아니다 — 유저 지시대로 상호작용이 없다.
    /// </summary>
    void BuildOuterRoad()
    {
        var root = new GameObject("OuterRoad").transform;
        root.SetParent(transform, false);

        const float z = 126f;        // 정문(z 100)에서 26m 밖 — 담장과 안 겹친다
        const float length = 300f;

        Block(root, "Asphalt", new Vector3(0f, 0.04f, z), Quaternion.identity,
              new Vector3(length, 0.08f, 14f), ColAsphalt, noCollider: true);

        // 중앙선 — 끊어진 흰 선. 이어진 한 줄은 길이 아니라 띠로 보인다
        for (int i = -9; i <= 9; i++)
            Block(root, $"Lane_{i + 9}", new Vector3(i * 15f, 0.09f, z), Quaternion.identity,
                  new Vector3(7f, 0.06f, 0.35f), ColCream, noCollider: true);

        // 인도 — 길과 담장 사이. 턱이 있어야 차도와 인도가 갈린다
        Block(root, "Walk", new Vector3(0f, 0.16f, z - 8.4f), Quaternion.identity,
              new Vector3(length, 0.3f, 2.8f), ColPlaza, noCollider: true);
        Block(root, "Walk_N", new Vector3(0f, 0.16f, z + 8.4f), Quaternion.identity,
              new Vector3(length, 0.3f, 2.8f), ColPlaza, noCollider: true);

        // 횡단보도 — 정문 바로 앞. 여기가 입구라는 걸 길이 말해준다
        for (int i = -4; i <= 4; i++)
            Block(root, $"Cross_{i + 4}", new Vector3(i * 1.4f, 0.09f, z), Quaternion.identity,
                  new Vector3(0.8f, 0.06f, 13f), ColCream, noCollider: true);

        // 가로등과 가로수. 길만 있으면 바닥 무늬로 보인다 — <b>서 있는 것</b>이 있어야 길이다
        for (int i = -4; i <= 4; i++)
        {
            if (i == 0) continue;
            float x = i * 34f;

            Block(root, $"Pole_{i + 4}", new Vector3(x, 2.6f, z - 9.2f), Quaternion.identity,
                  new Vector3(0.22f, 5.2f, 0.22f), ColPlaza, noCollider: true);
            Block(root, $"Lamp_{i + 4}", new Vector3(x, 5.1f, z - 8.4f), Quaternion.identity,
                  new Vector3(0.8f, 0.24f, 1.8f), ColLantern, noCollider: true);

            Block(root, $"StreetTrunk_{i + 4}", new Vector3(x + 17f, 1.7f, z + 9.6f), Quaternion.identity,
                  new Vector3(0.5f, 3.4f, 0.5f), ColPineTrunk, noCollider: true);
            Ball(root, $"StreetCrown_{i + 4}", new Vector3(x + 17f, 4.4f, z + 9.6f),
                 new Vector3(4.6f, 3.4f, 4.6f), ColBush);
        }

        // 건너편 건물 실루엣 — 상자 몇 개면 충분하다. 안개가 절반을 먹는다
        var random = new System.Random(77);
        for (int i = -3; i <= 3; i++)
        {
            float x = i * 42f + (float)random.NextDouble() * 12f - 6f;
            float bw = 16f + (float)random.NextDouble() * 14f;
            float bh = 10f + (float)random.NextDouble() * 12f;

            Block(root, $"TownBlock_{i + 3}", new Vector3(x, bh * 0.5f, z + 26f + (float)random.NextDouble() * 10f),
                  Quaternion.identity, new Vector3(bw, bh, 14f), ColStoneWall, noCollider: true);
        }
    }

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
            // ★ 2026-09-23 유저: *"곰솥관이랑 곰손관이랑 사실상 차이가 없는 것 같은데.
            // 팻말도 둘이 똑같아."* <b>맞다 — 팻말이 진짜로 겹쳐 있었다.</b>
            // 곰손관이 「조리·제빵·공예·봉제」였고 곰솥관이 「조리실습·제과제빵」이라
            // <b>앞 두 낱말이 같은 말</b>이다. 2026-09-18 에 곰밥마당에서 «조리실습» 을 떼어
            // 곰솥관을 지어 줬는데, <b>곰손관에서는 안 뗐다.</b>
            //
            // 건물을 지울 일이 아니다 — <b>내부는 이미 봉제실</b>이고(작업대·재봉틀·널린 천)
            // 조리 도구가 하나도 없다. 팻말만 몸에 안 맞는 옷을 입고 있었던 거야.
            // 그리고 <b>곰인형 박물관에서 인형을 만드는 학과는 여기 하나뿐</b>이다 —
            // 이 세계에서 제일 중요한 과가 이름을 못 갖고 있었다.
            ("곰손관",   "인형제작·공예·봉제",       -99f, -23f,  75f, 22f, 14f, 9f, "여기서 다들 태어났습니다"),
            ("곰머리관", "인문·교육·연구·심리",      -90f,  42f,  95f, 18f, 12f, 8f, ""),
            ("곰누리관", "관광·외국어·박물관·국제문화", 74f,  80f, 205f, 20f, 13f, 9f, ""),
            ("재주관",   "미술·음악·영상·공연",       96f, -12f, 275f, 21f, 13f, 9f, "재주는 곰이 넘고 돈은 딴 놈이 번다"),
            ("곰테크관", "게임·공학·기계·카트",       94f, -86f, 320f, 17f, 12f, 8f, ""),

            ("철곰관",   "경호·체육·안전",          -102f, -65f,  55f, 19f, 13f, 8f, ""),
            ("웅성관",   "방송·언론·홍보·마케팅",     -80f,  90f, 130f, 18f, 12f, 9f, ""),
            ("곰생회관", "학생회",                    42f, 101f, 195f, 17f, 12f, 8f, ""),
            ("참잘했어요관", "시상·전시",             98f,  36f, 262f, 16f, 12f, 8f, ""),
            ("대충기념관", "기념",                    92f, -52f, 300f, 15f, 11f, 7f, "2026년 준공"),
            ("곰밥마당", "학생식당·카페·배식",       -73f, -107f, 10f, 30f, 19f, 9f, "곰국 없음"),
            // 2026-09-18 유저: "곰밥마당 옆에 조리실습 공부하는 공간을 더 지어 줘."
            // 곰밥마당에 묶여 있던 "조리실습" 을 떼어 제 건물을 줬다 —
            // 급식소 옆에 조리실습실이 없는 게 오히려 이상했다(곰 전문대잖아).
            // 자리: 곰밥마당 오른쪽 끝(−58+처마) 과 본관 왼쪽 끝(−18−처마) 사이가 35m 라 들어간다.
            ("곰솥관",   "조리실습·제과제빵",        -40f, -107f,  10f, 20f, 14f, 8f, "불 조심. 곰은 더 조심"),
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
        // ★ 구멍을 `doorWidth + 0.9` 로 뚫었더니 <b>문틀이 다 못 메웠다.</b> 문설주 바깥 끝이
        // `half + 0.28` 인데 구멍 끝은 `half + 0.45` 라 <b>양옆 17cm 가 그냥 뚫려 있었다</b> —
        // 닫힌 문이 살짝 열려 보인 진짜 이유(2026-09-18). `+0.5` 면 구멍 끝이 `half + 0.25` 라
        // 문설주(1.80~2.08)가 벽을 3cm 물고 들어간다.
        Hollow(t, width, depth, height, doorWidth + 0.5f, doorHeight);
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
                                   plaqueHeight: height - 1.1f,
                                   // ★ 이 문은 <b>열린다.</b> 문짝을 static 으로 두면 유니티가
                                   // 씬을 열 때 메시를 합쳐 버려서(정적 배칭) 트랜스폼을 옮겨도
                                   // 그려지는 자리가 안 바뀐다 — 문이 안 열린 진짜 원인(2026-09-18).
                                   openable: true);

        // 문 앞 6m 를 기억해 둔다 — 나무가 문을 막지 않게
        doorFronts.Add(t.TransformPoint(new Vector3(0f, 0f, front + 6f)));

        // 담장이 이 건물을 비켜가게. 회전까지 감안해 <b>바깥으로 넉넉히</b> 잡는다 —
        // 담장이 방을 조금이라도 가로지르면 걸어서 지나갈 수가 없다.
        float reach = Mathf.Max(width, depth) * 0.5f + 3f;
        footprints.Add(new Bounds(position, new Vector3(reach * 2f, 40f, reach * 2f)));

        // 미니게임이 들어올 자리. <b>세 동에만</b> 단다 — 열세 동 전부에 "준비 중" 이 뜨면
        // 핑계가 아니라 <b>못 만든 목록</b>으로 보인다(2026-09-18).
        MinigameSpotFor(t, name, position);

        // 문짝 둘을 젖힐 수 있게. 문틀이 실제로 뚫려 있으니 열고 걸어 들어가면 된다.
        var hinge = door.AddComponent<HingedDoor>();
        hinge.label = name;

        // ★★ <b>미니게임이 있는 동에는 폐쇄 판자를 안 박는다</b>(2026-09-22).
        // 유저: *"전시실 아이템 다 모으는 중인데 곰밥마당이 리본과 간판으로 막고 있다."*
        // 판자는 «수집품 하나에 한 동씩 걷힌다» 는 연출인데(2026-09-18), 하필 <b>유일하게
        // 돌아가는 미니게임</b>이 그 뒤에 잠겨 버렸다. 급식은 <b>레이스와 레이스 사이</b>에
        // 쉬어가라고 만든 것이라, 여덟 판을 다 깨야 열리면 <b>있으나 마나</b>가 된다.
        // 남은 열 동으로도 «하나씩 걷힌다» 는 충분히 읽힌다.
        if (t.GetComponentInChildren<MinigameSpot>(true) != null) hinge.boardable = false;
        // ★ <b>문짝은 이제 통(`LeafRoot_s`) 안에 들어 있다.</b> 전처럼 `Find("Leaf_-1")` 을
        // 부르면 `Transform.Find` 가 직계 자식만 보기 때문에 <b>null 이 꽂히고</b>,
        // 씬에는 그 null 이 저장된다. 통을 직접 꽂는다.
        hinge.leaves = new[] { door.transform.Find("LeafRoot_-1"), door.transform.Find("LeafRoot_1") };
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"Window_{i}", new Vector3(i * width * 0.28f, 2.6f, front), Quaternion.identity,
                  new Vector3(2.6f, 2.2f, 0.25f), ColWindow, noCollider: true);

        return go;
    }

    /// <summary>
    /// <b>이 건물 지붕의 제일 높은 점</b>(월드 y). 지붕 위에 뭘 올릴 때 쓴다.
    ///
    /// 눈으로 어림하면 틀린다 — 곰밥마당은 높이 9 인데 기와까지 쌓이면 <b>10.95</b> 다.
    /// 처마·기와·능선이 층층이 올라가서 `height + 0.55` 같은 어림값이 안 맞아.
    /// </summary>
    static float RoofTopLocal(Transform building, float height)
    {
        // 지붕 조각을 아직 안 지었을 수도 있으니 <b>바닥값</b>을 둔다 —
        // 측정해 보면 기와까지 대략 `높이 + 2.0` 이다(곰밥마당 9 → 10.95).
        float top = height + 2.0f;

        foreach (var r in building.GetComponentsInChildren<Renderer>())
        {
            string n = r.name;
            if (!n.Contains("Eave") && !n.Contains("Roof") && !n.Contains("Tile") && !n.Contains("Ridge"))
                continue;
            top = Mathf.Max(top, r.bounds.max.y - building.position.y);
        }
        return top;
    }

    /// <summary>
    /// <b>유저가 만든 FBX 를 자리에 앉힌다.</b> 2026-09-18, 밥그릇이 첫 손님이야.
    ///
    /// <paramref name="targetWidth"/> 가 핵심이다 — 블렌더에서 몇 미터로 만들었든
    /// <b>여기서 정한 크기로 맞춰 준다.</b> 밥그릇을 4.6m 로 만들어 왔어도 0.22m 로 앉는다.
    /// 유저에게 "블렌더에서 크기를 다시 맞춰 오세요" 를 시키지 않으려고 이렇게 한다
    /// (기획서 §9.3 — 유저가 손으로 해야 하는 단계를 남기지 마라).
    ///
    /// 파일이 없으면 <b>아무 일도 안 한다.</b> 없는 걸 기다리며 방을 비워 두면 안 되고,
    /// 나중에 파일만 놓고 씬을 다시 구우면 저절로 들어온다.
    /// </summary>
    /// <summary>
    /// <b>static 이다</b> — 로비 빌더도 같은 규칙으로 유저 모델을 앉혀야 한다(2026-09-22 곰 스피커).
    /// 인스턴스 상태를 하나도 안 쓰니 옮길 것도 없었다.
    /// </summary>
    public static Transform MyModel(Transform parent, string assetPath, string name, Vector3 at, float targetWidth,
                      float maxHeight = 0f)
    {
#if UNITY_EDITOR
        // <b>임포트 설정을 먼저 맞춘다.</b> 유니티 기본값은 `materialLocation: External` 이라
        // 짝이 맞는 `.mat` 에셋이 없으면 <b>모델이 새하얗게</b> 나온다 — 카트와 곰에서 이미
        // 두 번 겪은 함정이야. 유저에게 임포터를 만지라고 시키지 않는다(기획서 §9.3).
        var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;
        if (importer != null &&
            (importer.materialLocation != UnityEditor.ModelImporterMaterialLocation.InPrefab
             || importer.importCameras || importer.importLights || importer.addCollider))
        {
            importer.materialLocation = UnityEditor.ModelImporterMaterialLocation.InPrefab;
            importer.importCameras = false;   // 블렌더 기본 내보내기가 카메라·조명을 같이 싣는다
            importer.importLights = false;
            importer.addCollider = false;
            importer.SaveAndReimport();
        }

        // ★★ <b>FBX 안에 박힌(embedded) 텍스처는 저절로 에셋이 되지 않는다</b>(2026-09-21).
        // `materialLocation = InPrefab` 만으로는 부족하다 — 재질은 생기는데 `_BaseMap` 이 비어서
        // <b>모델이 통째로 하얗거나 회색으로</b> 나온다. 곰 NPC 때 이미 한 번 겪은 함정이야
        // (CLAUDE.md 「곰이 하얗게 나오던 것」). 유저의 한복 곰도 4096² 를 파일 안에 싣고 왔다.
        if (importer != null)
        {
            string home = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string texFolder = home + "/Textures";

            if (!UnityEditor.AssetDatabase.IsValidFolder(texFolder))
                UnityEditor.AssetDatabase.CreateFolder(home, "Textures");

            // ★★ <b>«폴더가 비었나» 로 판단하면 안 된다</b>(2026-09-22).
            // 여러 모델이 <b>같은 Textures 폴더를 나눠 쓰면</b>, 첫 모델이 한 장 꺼낸 순간
            // 폴더가 안 비게 되어 <b>두 번째 모델부터는 영영 안 꺼낸다.</b> 유저 소품 넷이
            // `_BaseMap` 이 빈 채로 들어왔고, 그건 화면에서 <b>새하얀 가구</b>가 된다
            // (카트·곰에서 이미 두 번 겪은 그 증상).
            //
            // <b>이 모델의 재질에 텍스처가 붙었나</b>를 본다 — 그게 우리가 실제로 원하는 조건이다.
            bool needsTextures = false;
            var probe = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (probe != null)
                foreach (var r in probe.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                        if (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                            needsTextures = true;

            if (needsTextures && importer.ExtractTextures(texFolder))
            {
                UnityEditor.AssetDatabase.Refresh();

                // 꺼낸 뒤 <b>normal 이 이름에 든 텍스처는 타입을 NormalMap 으로</b> 바꾼다.
                // 안 그러면 모델이 파랗게 칠해진다(곰 NPC 때 그랬다).
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { texFolder }))
                {
                    string texPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var texImp = UnityEditor.AssetImporter.GetAtPath(texPath) as UnityEditor.TextureImporter;
                    if (texImp == null) continue;

                    var wanted = texPath.ToLowerInvariant().Contains("normal")
                               ? UnityEditor.TextureImporterType.NormalMap
                               : UnityEditor.TextureImporterType.Default;
                    if (texImp.textureType == wanted) continue;

                    texImp.textureType = wanted;
                    texImp.SaveAndReimport();
                }

                importer.SaveAndReimport();   // 꺼낸 텍스처를 재질에 다시 물린다
            }
        }

        var fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (fbx == null) return null;

        // ★ <b>기하가 비어 있는 FBX 를 걸러낸다.</b> 2026-09-18 유저가 준
        // `pot lid_PLUS IRON_spoon.fbx` 가 그랬다 — 4KB 에 `Objects` 가 통째로 비어서
        // 유니티에는 <b>빈 상자(트랜스폼)만</b> 떴다(유저: "박스만 나오고 아이템이 안 나와").
        // 블렌더에서 <b>「선택된 오브젝트만」을 켠 채 아무것도 안 고르고</b> 내보내면 이렇게 된다.
        // 그냥 두면 씬에 빈 오브젝트가 남아서 "배치는 됐는데 안 보인다" 로 또 헤맨다.
        if (fbx.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            Debug.LogError($"[내 모델] '{assetPath}' 안에 <b>메시가 하나도 없다.</b> " +
                           $"블렌더에서 내보낼 때 오브젝트를 고르고 다시 내보내라 " +
                           $"(File → Export → FBX, 「Limit to: Selected Objects」를 끄거나 " +
                           $"모델을 선택한 상태로). 그 자리는 비워 둔다.");
            return null;
        }

        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fbx);

        // ★★ <b>바로 언팩한다</b>(2026-09-22). 프리팹 인스턴스로 두면 이름·위치·스케일이
        // 전부 «오버라이드» 로만 저장되는데, <b>나중에 다른 모델이 리임포트를 돌리면
        // 그 오버라이드가 통째로 되돌아간다.</b>
        //
        // 실제로 그랬다: 곰 스피커 넷을 놓은 뒤 소품이 텍스처를 꺼내며
        // <c>AssetDatabase.Refresh()</c> 를 불렀고, 스피커 <b>셋이 이름은 `Bear_speaker`,
        // 위치는 (0,0,0)</b> 으로 돌아가 홀 한가운데에 겹쳐 섰다. 이름이 바뀌었으니
        // <see cref="SpeakerBounce"/> 를 붙이는 쪽도 그것들을 못 찾아 <b>귀도 안 튀었다.</b>
        //
        // 언팩하면 연결이 끊겨 리임포트가 씬을 못 건드린다. 어차피 씬을 다시 구워서 쓰는
        // 방식이라 잃을 게 없다 — 카트에서 이미 같은 결정을 했다(2026-09-18).
        UnityEditor.PrefabUtility.UnpackPrefabInstance(
            go, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);

        go.name = name;
        go.transform.SetParent(parent, false);

        // 크기를 재서 비율을 구한다. 손으로 스케일을 적으면 모델을 새로 뽑을 때마다 또 틀린다.
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
        }
        if (first) return null;

        float widest = Mathf.Max(bounds.size.x, bounds.size.z);
        float scale = widest > 0.001f ? targetWidth / widest : 1f;

        // <b>키 제한.</b> 폭만 보고 키우면 <b>거의 정육면체인 모델이 건물보다 높아진다</b> —
        // 솥뚜껑은 1.85 × 1.88 × 2.06 이라 폭 8m 로 맞추면 높이가 7.3m 가 된다(건물이 9m).
        // 간판은 커야 읽히지만 <b>지붕보다 높은 간판은 건물을 가린다.</b>
        if (maxHeight > 0.01f && bounds.size.y * scale > maxHeight)
            scale = maxHeight / bounds.size.y;

        // ★★ <b>대입이 아니라 곱한다</b>(2026-09-22). 블렌더 기본 익스포트는 모델 루트에
        // <c>Lcl Scaling = 100</c> 을 싣고 오는 경우가 있고, 그걸 <c>= one * scale</c> 로
        // 덮으면 <b>그 100 이 통째로 사라진다.</b> 위에서 잰 <c>bounds</c> 는 100 이 반영된
        // 월드 크기라 <c>scale</c> 이 1/100 로 나오는데, 거기서 100 까지 잃으니 결과가
        // <b>1만 분의 1</b> 이 된다 — 유저 소품 넷이 전부 1cm 로 나왔다.
        //
        // <b>회전과 같은 함정이다</b>(2026-09-21 곰: «localRotation 을 덮어쓰지 마라»).
        // 임포트한 FBX 의 트랜스폼은 <b>덮어쓰지 말고 곱해라</b> — 회전도, 스케일도.
        go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * scale);

        // <b>원점이 바닥이 아니어도 바닥에 앉힌다.</b> 블렌더에서 원점을 가운데 둔 모델이
        // 많고, 그걸 매번 고쳐 오라고 하는 게 이 프로젝트의 방식이 아니야.
        float bottom = (bounds.min.y - go.transform.position.y) * scale;
        go.transform.localPosition = at - new Vector3(0f, bottom, 0f);

        // 콜라이더는 안 붙인다 — 배식대 위 장식이고, 걷다가 걸리면 짐이 된다.
        foreach (var c in go.GetComponentsInChildren<Collider>())
        {
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }

        return go.transform;
#else
        return null;
#endif
    }

    /// <summary>
    /// 미니게임 자리를 다는 세 동. 나머지 열 동은 그냥 둘러보는 방이다 —
    /// 시간이 남으면 그때 늘리면 된다(유저: *"시간 남으면 더 오픈하면 되잖아"*).
    /// </summary>
    void MinigameSpotFor(Transform t, string name, Vector3 position)
    {
        string title, blurb;
        bool ready;

        switch (name)
        {
            case "곰밥마당":
                // 유저: *"어차피 급식 게임할 거면 급식소는 여기밖에 없고 밥은 나눠줘야지,
                // 공사라도 라고 한 줄 띡 적어 놓으면 될 듯한데."* 맞다 —
                // 급식소가 통째로 잠겨 있으면 <b>들어갈 이유가 없는 방</b>이 된다.
                // 2026-09-21 만들었다. ready 는 씬에 굽히는 값이라 옛 씬에서는 false 로 남는데,
                // MinigameSpot 이 제목을 보고 스스로 «만들어진 게임» 을 안다 — 여긴 표시용.
                title = "오늘의 급식"; blurb = "한 판 90초 · 점수만"; ready = true; break;
            case "곰짝박수마당":
                title = "한마당 무대"; blurb = "박자에 맞춰 손뼉을"; ready = false; break;
            case "철곰관":
                title = "곰 씨름판"; blurb = "밀어서 넘기면 이긴다"; ready = false; break;
            default:
                return;   // 나머지는 자리를 안 둔다
        }

        var go = new GameObject("MinigameSpot").transform;
        go.SetParent(t, false);
        go.localPosition = new Vector3(0f, 0f, 2.5f);   // 문 안쪽 — 들어와야 보인다

        var spot = go.gameObject.AddComponent<MinigameSpot>();
        spot.title = title;
        spot.blurb = blurb;
        spot.ready = ready;

        // 입간판 — 나무 판에 붉은 띠. "여기서 뭔가 한다" 가 멀리서도 보여야 한다.
        Block(go, "SpotPost", new Vector3(0f, 0.8f, 0f), Quaternion.identity,
              new Vector3(0.14f, 1.6f, 0.14f), ColWood, noCollider: true);
        Block(go, "SpotFoot", new Vector3(0f, 0.08f, 0f), Quaternion.identity,
              new Vector3(0.7f, 0.16f, 0.7f), ColWood, noCollider: true);
        // ★ 층을 <b>뒤에서 앞으로</b> 쌓는다. 겹치면 지지직거리고, 테두리를 글자보다
        // 앞에 두면 <b>글자가 테두리에 묻힌다</b>(2026-09-18 유저: "글씨까지 같이 사라졌는데").
        // 뒤 ← 테두리 −0.10~−0.04 · 판 −0.04~0.06 · 글자 0.15 · 띠 0.18~0.22 → 앞
        var board = Block(go, "SpotBoard", new Vector3(0f, 1.75f, 0.01f), Quaternion.identity,
                          new Vector3(1.9f, 0.9f, 0.1f), ColCream, noCollider: true);
        Block(go, "SpotFrame", new Vector3(0f, 1.75f, -0.07f), Quaternion.identity,
              new Vector3(2.1f, 1.1f, 0.06f), ColWood, noCollider: true);
        // 띠는 글자보다 앞이지만 판 아래쪽만 가로지르니 글자를 안 덮는다
        Block(go, "SpotTape", new Vector3(0f, 1.36f, 0.2f), Quaternion.Euler(0f, 0f, 9f),
              new Vector3(2.2f, 0.14f, 0.04f), ColRibbon, noCollider: true);

        // <b>글자는 판에 붙인다.</b> BuildingSign 이 판의 localScale 에서 크기를 뽑기 때문에
        // 빈 통에 붙이면 1×1 로 계산해서 글자가 판 밖으로 넘친다.
        var sign = board.AddComponent<BuildingSign>();
        sign.department = title;
        sign.motto = blurb;
        sign.maxLine = 0.26f;
    }

    /// <summary>
    /// 벽 넉 장 + 바닥 + 천장. 정면 가운데에 <paramref name="gap"/> 만큼 구멍을 남긴다.
    ///
    /// 통짜 상자를 벽 넉 장으로 바꾸면 조각이 하나에서 여덟로 는다(건물 열셋이면 91개).
    /// 그래도 싼 편이야 — <b>안에 들어갈 수 있다는 게 건물 하나를 방 하나로 바꾼다.</b>
    /// 창도 이제 진짜로 안이 비친다.
    /// </summary>
    void Hollow(Transform t, float w, float d, float h, float gap, float doorHeight = 4.2f)
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

        // 문 위 인방 — 구멍이 천장까지 뚫려 있으면 건물이 잘린 것처럼 보인다.
        // ★ 전에는 `h − 4.6` 이라 벽이 y 4.6 부터 시작했는데 문 상인방은 4.54 에서 끝난다 —
        // 그 사이 <b>6cm 가 가로로 뚫려</b> 문 위에 검은 줄이 보였다(2026-09-18).
        // 상인방이 `doorHeight + 0.34` 까지 올라오니 벽을 `doorHeight + 0.3` 부터 시작해 물린다.
        float lintel = h - (doorHeight + 0.3f);
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

        Weathering(t, w, d, h);
    }

    /// <summary>
    /// <b>레고 느낌을 잡는 세 가지 중 둘.</b> 2026-09-18 유저: *"캠퍼스 씬에서 아직 레고
    /// 냄새가 난다."* 원인은 상자가 많아서가 아니다:
    ///
    /// 1. <b>모든 면이 90도</b> — 상자는 모서리가 완벽하게 날카로워서 빛이 한 번에 꺾인다.
    ///    실제 건물은 모서리가 살짝 둥글거나 <b>홈</b>이 있다. 홈을 파면 그 줄에 그림자가
    ///    한 줄 생기고, 그 한 줄이 "면이 하나가 아니다" 를 말해준다.
    /// 2. <b>한 덩어리 = 한 색</b> — 벽 하나가 통째로 크림색이었다. 실제 벽은 <b>아래가
    ///    때가 타고 위가 바랜다.</b> 아래 0.9m 만 조금 어두운 색으로 갈면 벽이 벽이 된다.
    ///
    /// <b>기하를 늘리는 게 아니라 면을 쪼개는 것</b>이야 — 새 물건이 아니라 이미 있는
    /// 벽 위에 얇은 판을 얹는다. 전부 <c>noCollider</c> 라 걷는 데 아무 영향이 없다.
    /// </summary>
    void Weathering(Transform t, float w, float d, float h)
    {
        float halfW = w * 0.5f, halfD = d * 0.5f;
        const float foot = 0.9f;     // 때가 타는 높이 — 사람 허리 아래

        // ---- 굽도리 : 벽 아래쪽만 다른 색 ----
        Block(t, "FootBack", new Vector3(0f, foot * 0.5f, -halfD - 0.16f), Quaternion.identity,
              new Vector3(w + 0.3f, foot, 0.12f), ColWallFoot, noCollider: true);
        Block(t, "FootFront", new Vector3(0f, foot * 0.5f, halfD + 0.16f), Quaternion.identity,
              new Vector3(w + 0.3f, foot, 0.12f), ColWallFoot, noCollider: true);
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"FootSide_{s}", new Vector3(s * (halfW + 0.16f), foot * 0.5f, 0f),
                  Quaternion.identity, new Vector3(0.12f, foot, d + 0.3f), ColWallFoot, noCollider: true);

        // 굽도리 윗선 — 색만 바뀌면 칠한 자국이고, <b>턱</b>이 있어야 다른 재료로 읽힌다
        Block(t, "FootCapBack", new Vector3(0f, foot, -halfD - 0.2f), Quaternion.identity,
              new Vector3(w + 0.4f, 0.1f, 0.2f), ColTrimDark, noCollider: true);
        Block(t, "FootCapFront", new Vector3(0f, foot, halfD + 0.2f), Quaternion.identity,
              new Vector3(w + 0.4f, 0.1f, 0.2f), ColTrimDark, noCollider: true);
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"FootCapSide_{s}", new Vector3(s * (halfW + 0.2f), foot, 0f),
                  Quaternion.identity, new Vector3(0.2f, 0.1f, d + 0.4f), ColTrimDark, noCollider: true);

        // ---- 세로 홈 : 벽을 일정 간격으로 쪼갠다 ----
        // 간격은 <b>2.4m 고정</b>. 건물 폭에 비례로 두면 큰 건물의 홈이 넓어져서
        // 다시 한 덩어리로 보인다 — 실제 벽체 간격은 건물 크기와 상관없이 일정하다.
        int across = Mathf.Max(2, Mathf.RoundToInt(w / 2.4f));
        for (int i = 1; i < across; i++)
        {
            float x = -halfW + w * i / across;
            Block(t, $"GrooveFront_{i}", new Vector3(x, (h + foot) * 0.5f, halfD + 0.14f),
                  Quaternion.identity, new Vector3(0.14f, h - foot, 0.1f), ColGroove, noCollider: true);
            Block(t, $"GrooveBack_{i}", new Vector3(x, (h + foot) * 0.5f, -halfD - 0.14f),
                  Quaternion.identity, new Vector3(0.14f, h - foot, 0.1f), ColGroove, noCollider: true);
        }

        int along = Mathf.Max(2, Mathf.RoundToInt(d / 2.4f));
        for (int i = 1; i < along; i++)
        {
            float z = -halfD + d * i / along;
            for (int s = -1; s <= 1; s += 2)
                Block(t, $"GrooveSide_{s}_{i}", new Vector3(s * (halfW + 0.14f), (h + foot) * 0.5f, z),
                      Quaternion.identity, new Vector3(0.1f, h - foot, 0.14f), ColGroove, noCollider: true);
        }

        // ---- 모서리 기둥 : 네 귀퉁이를 세로로 덮는다 ----
        // 이게 제일 크게 듣는다. <b>상자의 날 선 모서리 넷</b>이 레고의 정체거든 —
        // 기둥으로 덮으면 그 선이 사라지고 면이 셋으로 갈린다.
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Block(t, $"Corner_{sx}_{sz}", new Vector3(sx * (halfW + 0.05f), h * 0.5f, sz * (halfD + 0.05f)),
                      Quaternion.identity, new Vector3(0.44f, h, 0.44f), ColWoodRail, noCollider: true);
                Block(t, $"CornerCap_{sx}_{sz}", new Vector3(sx * (halfW + 0.05f), h - 0.2f, sz * (halfD + 0.05f)),
                      Quaternion.identity, new Vector3(0.62f, 0.22f, 0.62f), ColTrimDark, noCollider: true);
                Block(t, $"CornerFoot_{sx}_{sz}", new Vector3(sx * (halfW + 0.05f), 0.16f, sz * (halfD + 0.05f)),
                      Quaternion.identity, new Vector3(0.62f, 0.32f, 0.62f), ColStoneWall, noCollider: true);
            }

        // ---- 3. 작은 물건 : 30cm 미만이 있어야 눈이 크기를 잰다 ----
        // 문 옆에만 놓는다. 사방에 흩뿌리면 조각 수만 늘고 걸어 다닐 때 안 보인다.
        float front = halfD + 0.3f;
        Block(t, "Vent", new Vector3(halfW - 1.2f, 2.3f, front), Quaternion.identity,
              new Vector3(0.6f, 0.4f, 0.1f), ColStoneWall, noCollider: true);
        for (int i = 0; i < 3; i++)
            Block(t, $"VentSlat_{i}", new Vector3(halfW - 1.2f, 2.16f + i * 0.13f, front + 0.04f),
                  Quaternion.identity, new Vector3(0.5f, 0.05f, 0.06f), ColTrimDark, noCollider: true);

        Block(t, "Socket", new Vector3(-halfW + 0.9f, 0.55f, front), Quaternion.identity,
              new Vector3(0.16f, 0.22f, 0.08f), ColCream, noCollider: true);
        Block(t, "Downpipe", new Vector3(-halfW + 0.45f, h * 0.5f, front - 0.05f), Quaternion.identity,
              new Vector3(0.22f, h, 0.22f), ColStoneWall, noCollider: true);
        Block(t, "DownpipeShoe", new Vector3(-halfW + 0.45f, 0.3f, front + 0.12f),
              Quaternion.Euler(22f, 0f, 0f), new Vector3(0.24f, 0.5f, 0.24f), ColStoneWall, noCollider: true);
        Block(t, "Notice", new Vector3(halfW - 2.4f, 1.6f, front), Quaternion.Euler(0f, 0f, 2f),
              new Vector3(0.3f, 0.42f, 0.04f), ColCream, noCollider: true);
        Block(t, "Hose", new Vector3(-halfW + 1.6f, 0.28f, front), Quaternion.identity,
              new Vector3(0.28f, 0.28f, 0.28f), ColWoodRail, noCollider: true);
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
            // ★ 2026-09-23 <b>수예부로 다시 지었다.</b> 전에는 다섯 조각(작업대 3 · 재봉틀 1 ·
            // 천 1)뿐이라 22 × 14m 방이 거의 비어 있었고, 그래서 곰솥관과 «사실상 차이가 없는»
            // 것처럼 보였다.
            //
            // <b>이 방이 곰인형 박물관에서 제일 중요한 방이다</b> — 여기서 곰들이 만들어진다.
            // 그래서 재봉실에 있을 법한 물건만 늘어놓는 게 아니라 <b>만들어지는 중인 곰</b>을
            // 놓는다. 팻말의 「여기서 다들 태어났습니다」가 그 한 장면으로 설명된다.
            case "곰손관":     // 인형제작·공예·봉제 — 수예부
            {
                float sX = w * 0.5f - 0.6f;      // 옆벽 안쪽 (Hollow 벽 두께 0.6)
                float sZb = -d * 0.5f + 0.6f;    // 뒷벽 안쪽

                // ── 재봉 작업대 넷 ── 각 대에 재봉틀·천 뭉치·실패
                // 한 대만 있으면 «누군가의 책상» 이고, 줄지어 있으면 <b>실습실</b>이 된다.
                for (int i = -1; i <= 2; i++)
                {
                    float bx = i * 4.4f - 2.2f;
                    Vector3 at = new Vector3(bx, 0f, halfD - 3.2f);

                    Block(t, $"InBench_{i}", at + Vector3.up * 0.42f, Quaternion.identity,
                          new Vector3(3.4f, 0.84f, 1.5f), ColWoodRail, noCollider: true);
                    Block(t, $"InBenchTop_{i}", at + Vector3.up * 0.88f, Quaternion.identity,
                          new Vector3(3.6f, 0.08f, 1.7f), ColWood, noCollider: true);

                    // 재봉틀 — 몸통 + 팔 + 바늘대. 상자 하나는 «기계» 로 안 읽힌다
                    Block(t, $"InMachine_{i}", at + new Vector3(-0.7f, 1.12f, 0f), Quaternion.identity,
                          new Vector3(0.9f, 0.4f, 0.45f), ColBearDark, noCollider: true);
                    Block(t, $"InMachineArm_{i}", at + new Vector3(-0.7f, 1.46f, 0f), Quaternion.identity,
                          new Vector3(0.75f, 0.28f, 0.3f), ColBearDark, noCollider: true);
                    Block(t, $"InMachineNeedle_{i}", at + new Vector3(-1.02f, 1.22f, 0f),
                          Quaternion.identity, new Vector3(0.05f, 0.22f, 0.05f),
                          ColStoneWall, noCollider: true);

                    // 천 뭉치와 실패 — 작업 중인 흔적
                    Block(t, $"InFabric_{i}", at + new Vector3(0.7f, 0.99f, 0.1f),
                          Quaternion.Euler(0f, 18f, 0f), new Vector3(1.0f, 0.14f, 0.7f),
                          i % 2 == 0 ? ColMint : ColRibbon, noCollider: true);
                    Disc(t, $"InSpool_{i}", at + new Vector3(1.3f, 1.0f, -0.4f),
                         new Vector3(0.12f, 0.16f, 0.12f), ColCream);

                    // 의자
                    Block(t, $"InStool_{i}", at + new Vector3(0f, 0.25f, 1.5f), Quaternion.identity,
                          new Vector3(0.5f, 0.5f, 0.5f), ColWood, noCollider: true);
                }

                // ── 실 벽 ── 수예부의 얼굴. 색 실패가 격자로 꽂힌 판
                // <b>색이 많은 것이 이 방의 정체다</b> — 나머지 방은 다 나무·크림·돌색이야.
                Block(t, "InThreadBoard", new Vector3(-sX + 0.25f, 2.2f, 1.5f), Quaternion.identity,
                      new Vector3(0.12f, 2.2f, 6.0f), ColWood, noCollider: true);
                var threads = new[] { ColRibbon, ColMint, ColLantern, ColMapleGold, ColWater, ColCream };
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 9; c++)
                        Disc(t, $"InThread_{r}_{c}",
                             new Vector3(-sX + 0.45f, 1.35f + r * 0.42f, -1.2f + c * 0.68f),
                             new Vector3(0.16f, 0.2f, 0.16f), threads[(r * 9 + c) % threads.Length]);

                // ── 원단 두루마리 ── 옆벽에 기대 세운다. 비스듬해야 «세워 둔 것» 이다
                for (int i = 0; i < 6; i++)
                    Block(t, $"InBolt_{i}", new Vector3(sX - 0.5f, 1.1f, -4.5f + i * 0.55f),
                          Quaternion.Euler(9f, 0f, 6f), new Vector3(0.3f, 2.2f, 0.3f),
                          i % 3 == 0 ? ColRibbon : i % 3 == 1 ? ColMint : ColCream, noCollider: true);

                // ── 솜 자루 ── 곰인형 박물관이라 <b>솜이 없으면 안 된다</b>
                for (int i = 0; i < 3; i++)
                    Block(t, $"InStuffing_{i}", new Vector3(sX - 1.6f - i * 0.1f, 0.55f, 2.4f + i * 1.1f),
                          Quaternion.Euler(0f, i * 14f, 0f), new Vector3(1.1f, 1.1f, 1.1f),
                          ColCream, noCollider: true);
                Block(t, "InStuffingOpen", new Vector3(sX - 1.6f, 1.15f, 2.4f), Quaternion.identity,
                      new Vector3(0.7f, 0.3f, 0.7f), ColWallTile, noCollider: true);

                // ── 만들다 만 곰 ── 이 방의 주인공. 몸통은 작업대에, 팔다리는 옆에 따로.
                // <b>완성품을 놓으면 «전시» 고, 조각을 놓으면 «작업 중» 이다.</b>
                Vector3 wip = new Vector3(2.2f, 0f, halfD - 3.2f);
                Block(t, "InBearBody", wip + new Vector3(0.2f, 1.22f, 0.2f),
                      Quaternion.Euler(0f, 20f, 8f), new Vector3(0.5f, 0.6f, 0.4f),
                      ColMapleGold, noCollider: true);
                for (int i = 0; i < 4; i++)
                    Block(t, $"InBearLimb_{i}", wip + new Vector3(0.9f + (i % 2) * 0.26f, 0.96f,
                                                                  -0.1f - (i / 2) * 0.24f),
                          Quaternion.Euler(0f, 30f + i * 25f, 78f), new Vector3(0.16f, 0.42f, 0.16f),
                          ColMapleGold, noCollider: true);
                for (int i = 0; i < 2; i++)
                    Disc(t, $"InBearEar_{i}", wip + new Vector3(0.05f + i * 0.3f, 0.95f, 0.55f),
                         new Vector3(0.2f, 0.06f, 0.2f), ColMapleGold);

                // 눈 단추 상자 — 칸마다 까만 단추. 작아서 «세밀한 일» 로 읽힌다
                Block(t, "InButtonBox", wip + new Vector3(-0.1f, 0.96f, -0.55f), Quaternion.identity,
                      new Vector3(0.44f, 0.09f, 0.3f), ColWood, noCollider: true);
                for (int i = 0; i < 6; i++)
                    Disc(t, $"InEyeButton_{i}", wip + new Vector3(-0.26f + (i % 3) * 0.16f, 1.02f,
                                                                  -0.62f + (i / 3) * 0.14f),
                         new Vector3(0.07f, 0.02f, 0.07f), ColBearDark);

                // ── 완성품 선반 ── 뒷벽. 다 만든 곰들이 앉아 있다
                Block(t, "InShelfDone", new Vector3(0f, 1.9f, sZb + 0.35f), Quaternion.identity,
                      new Vector3(w - 5f, 0.12f, 0.6f), ColWood, noCollider: true);
                for (int i = -3; i <= 3; i++)
                {
                    Block(t, $"InDoneBear_{i + 3}", new Vector3(i * 2.1f, 2.22f, sZb + 0.35f),
                          Quaternion.Euler(0f, i * 11f, 0f), new Vector3(0.42f, 0.52f, 0.34f),
                          i % 2 == 0 ? ColMapleGold : ColWoodRail, noCollider: true);
                    for (int e = 0; e < 2; e++)
                        Disc(t, $"InDoneEar_{i + 3}_{e}",
                             new Vector3(i * 2.1f - 0.13f + e * 0.26f, 2.5f, sZb + 0.35f),
                             new Vector3(0.17f, 0.05f, 0.17f),
                             i % 2 == 0 ? ColMapleGold : ColWoodRail);
                }

                // ── 패턴 종이 ── 벽에 핀으로 붙인 도면. 종이가 있어야 «공방» 이다
                for (int i = 0; i < 5; i++)
                    Block(t, $"InPattern_{i}", new Vector3(-4f + i * 2.0f, 3.1f, sZb + 0.08f),
                          Quaternion.Euler(0f, 0f, -4f + i * 2f), new Vector3(0.8f, 1.0f, 0.03f),
                          ColCream, noCollider: true);

                // ── 다리미대 ── 좁고 긴 것 하나가 방의 리듬을 깬다
                Block(t, "InIronBoard", new Vector3(-6.2f, 0.85f, -2.2f), Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(1.6f, 0.08f, 0.55f), ColCream, noCollider: true);
                for (int i = -1; i <= 1; i += 2)
                    Block(t, $"InIronLeg_{i}", new Vector3(-6.2f + i * 0.5f, 0.42f, -2.2f),
                          Quaternion.Euler(0f, 24f, i * 12f), new Vector3(0.08f, 0.84f, 0.08f),
                          ColStoneWall, noCollider: true);
                Block(t, "InIron", new Vector3(-5.8f, 0.96f, -2.3f), Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(0.34f, 0.16f, 0.2f), ColStoneWall, noCollider: true);

                // ── 마네킹 둘 ── 사람 키를 알려주는 물건(전시실 벤치와 같은 역할)
                for (int i = 0; i < 2; i++)
                {
                    Vector3 mAt = new Vector3(6.6f + i * 1.8f, 0f, -3.4f);
                    Block(t, $"InFormPost_{i}", mAt + Vector3.up * 0.5f, Quaternion.identity,
                          new Vector3(0.09f, 1.0f, 0.09f), ColStoneWall, noCollider: true);
                    Disc(t, $"InFormFoot_{i}", mAt + Vector3.up * 0.04f,
                         new Vector3(0.5f, 0.08f, 0.5f), ColBearDark);
                    Block(t, $"InFormBody_{i}", mAt + Vector3.up * 1.32f,
                          Quaternion.Euler(0f, i * 26f, 0f), new Vector3(0.52f, 0.72f, 0.36f),
                          i == 0 ? ColCream : ColMint, noCollider: true);
                }

                // ── 바닥에 떨어진 천 조각 ── 작은 것이 있어야 레고로 안 보인다(2026-09-17)
                for (int i = 0; i < 9; i++)
                    Block(t, $"InScrap_{i}",
                          new Vector3(-7f + i * 1.7f, 0.012f, -0.4f + (i % 3) * 1.1f),
                          Quaternion.Euler(0f, i * 37f, 0f), new Vector3(0.28f, 0.012f, 0.2f),
                          i % 3 == 0 ? ColRibbon : i % 3 == 1 ? ColMint : ColCream, noCollider: true);
                break;
            }

            case "곰머리관":   // 인문·연구 — 서가
                for (int i = -1; i <= 1; i += 2)
                    for (int j = 0; j < 3; j++)
                        Block(t, $"InShelf_{i}_{j}", new Vector3(i * (halfW - 0.6f), 1.1f + j * 1.1f, j - 1f),
                              Quaternion.identity, new Vector3(0.7f, 0.18f, d - 3f),
                              j % 2 == 0 ? ColCream : ColRibbon, noCollider: true);
                Block(t, "InDesk", new Vector3(0f, 0.4f, 0f), Quaternion.identity,
                      new Vector3(3f, 0.8f, 1.4f), ColWoodRail, noCollider: true);
                break;

            // 경호·체육. ★ 2026-09-21 유저: *"철곰관도 일단 임시로 인테리어 해놓자."*
            // 여기 미니게임 자리가 «곰 씨름판» 이라고 적혀 있는데 방에는 매트 한 장과
            // 모래주머니 둘뿐이었다 — <b>입간판이 약속한 걸 방이 안 보여주면</b>
            // «준비 중» 이 아니라 «아무것도 없다» 로 읽힌다.
            case "철곰관":
            {
                // ---- 씨름판 ----
                // 모래판은 <b>둥글어야</b> 씨름판이다. 네모난 매트는 체육관 바닥이지 씨름판이 아니야.
                // Disc 는 콜라이더를 늘 떼고 noCollider 인자를 안 받는다 — 그대로 부르면 된다.
                Disc(t, "InRing", new Vector3(0f, 0.07f, 0f),
                     new Vector3(7.4f, 0.14f, 7.4f), ColSandRing);
                Disc(t, "InRingEdge", new Vector3(0f, 0.05f, 0f),
                     new Vector3(8.0f, 0.10f, 8.0f), ColWoodRail);
                // 바깥 낙법 매트 — 판에서 밀려나는 경기니까 밖이 푹신해야 말이 된다
                Block(t, "InFallMat", new Vector3(0f, 0.03f, 0f), Quaternion.identity,
                      new Vector3(w - 3f, 0.06f, d - 3f), ColBush, noCollider: true);

                // ---- 관중석 ----
                // 씨름은 <b>보는 경기</b>다. 앉을 데가 없으면 연습실로 보인다.
                for (int s = -1; s <= 1; s += 2)
                    for (int tier = 0; tier < 2; tier++)
                    {
                        float x = s * (halfW - 1.2f - tier * 0.9f);
                        float y = 0.28f + tier * 0.42f;
                        Block(t, $"InBench_{s}_{tier}", new Vector3(x, y, 0f), Quaternion.identity,
                              new Vector3(0.9f, 0.16f, d - 5f), ColWoodRail, noCollider: true);
                        Block(t, $"InBenchLeg_{s}_{tier}", new Vector3(x, y * 0.5f, 0f), Quaternion.identity,
                              new Vector3(0.7f, y, d - 5.4f), ColWood, noCollider: true);
                    }

                // ---- 벽 쪽 ----
                // 모래주머니는 남긴다(원래 있던 것). 자리만 구석으로 물려서 판을 안 가린다.
                for (int i = -1; i <= 1; i += 2)
                    Block(t, $"InBag_{i}", new Vector3(i * 1.8f, 1.5f, -halfD + 1.3f), Quaternion.identity,
                          new Vector3(0.6f, 2.4f, 0.6f), ColWoodRail, noCollider: true);

                // 샅바 걸이 — 이 한 가지가 «씨름» 을 확정한다. 역기만 있으면 헬스장이야.
                Block(t, "InBeltRail", new Vector3(-halfW + 0.5f, 2.0f, 1.5f), Quaternion.identity,
                      new Vector3(0.08f, 0.08f, 3.2f), ColWood, noCollider: true);
                for (int i = 0; i < 4; i++)
                    Block(t, $"InBelt_{i}", new Vector3(-halfW + 0.5f, 1.55f, 0.2f + i * 0.85f),
                          Quaternion.identity, new Vector3(0.06f, 0.82f, 0.26f),
                          i % 2 == 0 ? ColRibbon : ColMint, noCollider: true);

                // 역기 — 체육관다움은 이걸로 충분하다
                Block(t, "InBarBar", new Vector3(halfW - 1.6f, 0.5f, -halfD + 2.2f),
                      Quaternion.Euler(0f, 90f, 0f), new Vector3(0.09f, 0.09f, 2.2f),
                      ColStoneWall, noCollider: true);
                for (int i = -1; i <= 1; i += 2)
                    Disc(t, $"InPlate_{i}", new Vector3(halfW - 1.6f + i * 0.95f, 0.5f, -halfD + 2.2f),
                         new Vector3(0.9f, 0.14f, 0.9f), ColBearDark,
                         Quaternion.Euler(0f, 0f, 90f));   // 원판은 세워야 역기다

                // 점수판과 우승기 — 대회가 열리는 곳이라는 신호
                Block(t, "InScoreBoard", new Vector3(0f, 2.6f, halfD - 0.4f), Quaternion.identity,
                      new Vector3(3.2f, 1.4f, 0.14f), ColBearDark, noCollider: true);
                for (int i = -1; i <= 1; i += 2)
                    Block(t, $"InScoreSlot_{i}", new Vector3(i * 0.8f, 2.6f, halfD - 0.5f),
                          Quaternion.identity, new Vector3(1.1f, 0.9f, 0.06f), ColCream, noCollider: true);
                for (int i = 0; i < 3; i++)
                    Block(t, $"InFlag_{i}", new Vector3(-2.4f + i * 2.4f, 3.1f, -halfD + 0.4f),
                          Quaternion.identity, new Vector3(0.7f, 1.1f, 0.05f),
                          i == 1 ? ColLantern : ColRibbon, noCollider: true);
                break;
            }

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

            case "화장실":     // 2026-09-18 유저: "화장실 안에 왜 이리 빛나는 거 있어. 변기도 없고."
            {
                // 천장등만 있고 아무것도 없어서 <b>발광 재질만 남아 빛나는 빈 방</b>이었다.
                // 칸막이 · 변기 · 세면대 · 거울 — 화장실을 화장실로 만드는 건 변기가 아니라 <b>칸</b>이다.
                //
                // ★★ 2026-09-21 <b>벽 두께를 빼먹고 좌표를 잡고 있었다.</b>
                // <see cref="Hollow"/> 의 벽은 <b>0.6m 두께</b>라 방 안쪽 면은 `w/2` 가 아니라
                // <b>`w/2 − 0.6`</b> 이다. 그걸 안 빼서 물통·칸막이 끝·환풍기가 <b>벽 속에 묻혀</b>
                // 있었다. 이제 안쪽 면에서부터 잰다.
                float inX  =  w * 0.5f - 0.6f;   // 옆벽 안쪽 면
                float inZb = -d * 0.5f + 0.6f;   // 뒷벽 안쪽 면
                float inZf =  d * 0.5f - 0.6f;   // 앞벽(출입문) 안쪽 면
                float ceilY = 3.3f;              // 내린 천장. 앞벽 창(y 2.6) 위로 지나간다
                HingedDoor bearStallDoor = null; // 곰이 든 칸 문 — 열릴 때마다 곰 자세가 바뀐다

                for (int i = 0; i < 2; i++)
                {
                    float cx = -inX + 1.5f + i * 2.4f;

                    // 칸막이 셋(좌·우·뒤)과 낮은 문 — 위아래가 트여 있어야 화장실 칸으로 읽힌다
                    Block(t, $"Stall_{i}_L", new Vector3(cx - 1.1f, 1.1f, inZb + 1.1f),
                          Quaternion.identity, new Vector3(0.1f, 2.0f, 2.2f), ColCream, noCollider: true);
                    Block(t, $"Stall_{i}_R", new Vector3(cx + 1.1f, 1.1f, inZb + 1.1f),
                          Quaternion.identity, new Vector3(0.1f, 2.0f, 2.2f), ColCream, noCollider: true);
                    // 칸막이 발 — 실제 화장실 칸은 바닥에서 떠 있고 다리로 받친다.
                    // 이 «떠 있음» 이 통짜 벽과 칸막이를 가르는 제일 큰 신호다.
                    for (int s2 = -1; s2 <= 1; s2 += 2)
                        Block(t, $"Stall_{i}_Leg{s2}", new Vector3(cx + s2 * 1.1f, 0.05f, inZb + 1.1f),
                              Quaternion.identity, new Vector3(0.14f, 0.1f, 0.16f), ColStoneWall, noCollider: true);

                    // ★ 2026-09-21 유저: *"화장실 칸막이 나무문은 안 열리더라."*
                    // 맞다 — 여기 문은 <b>그냥 상자였다.</b> HingedDoor 가 아예 안 붙어 있어서
                    // 열릴 방법이 없었고, 유저가 본 «문 열기» 는 <b>화장실 건물 출입문</b> 거였다.
                    //
                    // 경첩 자리에 <b>빈 통</b>을 두고 문짝을 그 안에 넣는다. 통을 돌리면
                    // 가장자리를 축으로 젖혀진다 — 문짝만 돌리면 제자리에서 팽이처럼 돈다.
                    var hingePivot = new GameObject($"Stall_{i}_Hinge").transform;
                    hingePivot.SetParent(t, false);
                    hingePivot.localPosition = new Vector3(cx - 1.0f, 1.1f, inZb + 2.2f);

                    var leaf = Block(hingePivot, $"Stall_{i}_Door", new Vector3(1.0f, 0f, 0f),
                                     Quaternion.identity, new Vector3(2.0f, 1.7f, 0.08f),
                                     ColWood, noCollider: true);
                    // ★ <b>움직일 물건에 isStatic 을 달면 정적 배칭에 합쳐져서 화면이 안 바뀐다.</b>
                    // 이 프로젝트에서 캠퍼스 문이 안 열린 진짜 원인이 이거였다(2026-09-18, 여섯 번째).
                    leaf.isStatic = false;
                    hingePivot.gameObject.isStatic = false;

                    // 문짝 장식 — 가로살 둘과 손잡이. 민짜 판은 문이 아니라 «세워둔 합판» 이다
                    for (int r2 = 0; r2 < 2; r2++)
                    {
                        var rail = Block(hingePivot, $"Stall_{i}_Rail{r2}",
                                         new Vector3(1.0f, -0.45f + r2 * 0.9f, -0.055f),
                                         Quaternion.identity, new Vector3(1.94f, 0.1f, 0.03f),
                                         ColWoodRail, noCollider: true);
                        rail.isStatic = false;
                    }
                    var knob = Block(hingePivot, $"Stall_{i}_Knob", new Vector3(1.78f, 0f, -0.07f),
                                     Quaternion.identity, new Vector3(0.09f, 0.09f, 0.08f),
                                     ColMapleGold, noCollider: true);   // ★ ColLantern 은 발광이다
                    knob.isStatic = false;

                    // ★ 왼쪽 칸(푸세식)은 <b>막혀서 수리 중</b>이다 — 정비 곰 대사의 그 칸.
                    // 오른쪽 칸만 «사용 중» 표시가 있으면 왼쪽은 아무 말도 안 하는 칸이 된다.
                    if (i == 1)
                    {
                        Block(hingePivot, "SignFixing", new Vector3(1.0f, 0.5f, -0.07f),
                              Quaternion.Euler(0f, 0f, -6f), new Vector3(0.52f, 0.22f, 0.02f),
                              ColMapleGold, noCollider: true).isStatic = false;   // ★ 발광 아님
                        Block(hingePivot, "SignFixingBar", new Vector3(1.0f, 0.5f, -0.085f),
                              Quaternion.Euler(0f, 0f, -6f), new Vector3(0.44f, 0.05f, 0.01f),
                              ColBearDark, noCollider: true).isStatic = false;
                    }

                    var stall = hingePivot.gameObject.AddComponent<HingedDoor>();
                    stall.leaves = new[] { hingePivot };
                    // ★ 2026-09-22 <b>칸마다 이름을 준다.</b> 유저: *"오른쪽 칸막이 문 열고 싶은데
                    // 왼쪽 칸막이 문이 나와."* 둘 다 «문 열기» 로만 뜨면 어느 쪽이 잡혔는지 모른다.
                    // 판자를 막는 건 `boardable` 이 따로 맡는다 — 전에는 라벨을 비워서 막았다.
                    // ★★ <b>좌우가 반대였다</b>(2026-09-22 유저 지적).
                    // 들어오는 사람은 <b>−Z 를 보고</b> 걷는데, 유니티는 왼손 좌표계라
                    // −Z 를 볼 때 <b>오른손은 −X</b> 쪽이다(right = cross(up, forward)).
                    // 그래서 x 가 <b>작은</b> i = 0(곰이 앉은 칸)이 플레이어의 <b>오른쪽</b>이다.
                    // 좌표의 «왼쪽» 과 보는 사람의 «왼쪽» 은 다르다 —
                    // 이 프로젝트에서 좌우가 뒤집힌 게 네 번째다(현판 · 진열장 숫자 · 시계 · 이번).
                    stall.label = i == 0 ? "오른쪽 칸" : "왼쪽 칸";
                    stall.boardable = false;
                    stall.swingDegrees = -78f;   // 안쪽으로 젖혀진다
                    stall.openSeconds = 0.38f;
                    if (i == 0) bearStallDoor = stall;

                    // ★ 2026-09-21 유저: *"곰이 앉는 칸은 양변기, 오른쪽 빈 칸은 푸세식으로."*
                    // 칸 둘이 <b>다른 변기</b>인 건 실제 한국 공중화장실 그대로고,
                    // 곰이 앉아 있는 쪽만 양변기면 «왜 저기 앉았나» 도 설명이 된다.
                    if (i == 0)
                    {
                        // ── 양변기 ──
                        // 변기를 변기로 만드는 건 셋이다:
                        // <b>바닥에서 좁아지는 기둥 · 둥근 몸통 · 구멍이 뚫린 좌석 링.</b>
                        float tz = inZb + 0.75f;

                        // 물통
                        Block(t, $"Cistern_{i}", new Vector3(cx, 0.85f, inZb + 0.16f),
                              Quaternion.identity, new Vector3(0.62f, 0.9f, 0.22f), ColCream, noCollider: true);
                        Block(t, $"CisternLid_{i}", new Vector3(cx, 1.32f, inZb + 0.16f),
                              Quaternion.identity, new Vector3(0.66f, 0.06f, 0.26f), ColCream, noCollider: true);
                        Block(t, $"FlushBtn_{i}", new Vector3(cx + 0.2f, 1.26f, inZb + 0.16f),
                              Quaternion.identity, new Vector3(0.11f, 0.04f, 0.11f), ColStoneWall, noCollider: true);

                        // 발치 — 바닥에 닿는 곳은 좁다. 상자 하나면 냉장고로 보인다
                        Block(t, $"Foot_{i}", new Vector3(cx, 0.04f, tz - 0.02f), Quaternion.identity,
                              new Vector3(0.26f, 0.08f, 0.34f), ColCream, noCollider: true);
                        Block(t, $"Pedestal_{i}", new Vector3(cx, 0.26f, tz - 0.04f), Quaternion.identity,
                              new Vector3(0.22f, 0.40f, 0.28f), ColCream, noCollider: true);

                        // 몸통 — 둥글어야 도기다. Disc 는 원통이라 scale.y 가 «반높이» 다
                        Disc(t, $"Bowl_{i}", new Vector3(cx, 0.48f, tz), new Vector3(0.46f, 0.11f, 0.60f), ColCream);
                        Disc(t, $"BowlRim_{i}", new Vector3(cx, 0.585f, tz), new Vector3(0.50f, 0.025f, 0.64f), ColCream);

                        // 좌석 링 + 구멍. <b>구멍이 보여야 변기다</b> — 통판이면 의자야
                        Disc(t, $"Seat_{i}", new Vector3(cx, 0.625f, tz + 0.01f),
                             new Vector3(0.52f, 0.022f, 0.66f), ColWoodRail);
                        // 구멍은 링보다 <b>낮게</b> 앉혀야 파인 것으로 읽힌다.
                        Disc(t, $"SeatHole_{i}", new Vector3(cx, 0.620f, tz + 0.02f),
                             new Vector3(0.30f, 0.018f, 0.42f), ColBearDark);

                        // 올려둔 뚜껑 — 물통에 기대 <b>세워야</b> «쓰는 중» 으로 읽힌다.
                        // ★ 2026-09-21 유저: *"변기 뚜껑이 이미 내려간 상태인 것 같아."* 맞다 —
                        // −14° 는 거의 <b>수평</b>이라 «덮어둔 뚜껑» 으로 보였다.
                        // +74° 로 세우면 판의 뒤쪽 끝이 위로, 물통 쪽으로 기댄다
                        // (X 회전은 +Z 끝을 내리고 −Z 끝을 올린다 — 부호를 헷갈리면 앞으로 넘어간다).
                        Block(t, $"SeatLid_{i}", new Vector3(cx, 0.957f, inZb + 0.40f),
                              Quaternion.Euler(74f, 0f, 0f), new Vector3(0.52f, 0.05f, 0.64f),
                              ColWoodRail, noCollider: true);
                    }
                    else
                    {
                        // ── 푸세식(쪼그려 앉는 것) ──
                        //
                        // ★★ 2026-09-22 (2차) 유저: *"푸세식 화장실이 너무 세련돼서 푸세식인 줄
                        // 모르겠다."* 맞다 — 1차는 <b>반듯한 타일 단 위에 하얀 도기</b>라
                        // 세면대나 족욕탕으로 보인다. 푸세식을 푸세식으로 만드는 건 도기가 아니라
                        // <b>바닥에 길게 뚫린 검은 홈</b>이다. 그게 전체 길이의 4분의 3을 먹어야 한다.
                        //
                        // 고친 것 넷: <b>구멍을 길고 넓게</b>(0.26×0.74 → 0.34×0.98, 도기의 82%) ·
                        // <b>뒤가 둥글게 솟은 덮개</b>(네모 상자가 아니라 돔) ·
                        // <b>낡은 색</b>(하얀 도기 → 누런 도기 + 얼룩) · <b>물 흐르는 홈</b>.
                        float dz = inZb + 0.95f;
                        const float dais = 0.13f;
                        float daisTop = dais - 0.01f;

                        // 단은 바닥에 1cm 묻는다 — 밑면을 바닥 윗면과 딱 맞추면 같은 평면이다.
                        Block(t, $"SquatDais_{i}", new Vector3(cx, dais * 0.5f - 0.01f, dz),
                              Quaternion.identity, new Vector3(1.9f, dais, 1.72f), ColStoneWall, noCollider: true);
                        Block(t, $"SquatDaisLip_{i}", new Vector3(cx, daisTop, dz + 0.86f),
                              Quaternion.identity, new Vector3(1.9f, 0.04f, 0.08f), ColWallTile, noCollider: true);
                        // 단 줄눈 — 통짜 한 색이면 «올려둔 상자» 로 보인다.
                        // ★ 얹는 물건은 <b>얹히는 면의 좌표</b>에서 계산해라(1차에 11mm 떠 있었다).
                        for (int g = -2; g <= 2; g++)
                            Block(t, $"SquatDaisSeam_{i}_{g}", new Vector3(cx + g * 0.42f, daisTop - 0.002f, dz),
                                  Quaternion.identity, new Vector3(0.025f, 0.012f, 1.68f), ColWallTile, noCollider: true);
                        // 물때 얼룩 — <b>낡아 보여야</b> 푸세식이다. 반듯하고 깨끗하면 신상 세면대야.
                        for (int g = 0; g < 3; g++)
                            Block(t, $"SquatStain_{i}_{g}",
                                  new Vector3(cx - 0.5f + g * 0.5f, daisTop - 0.001f, dz + 0.4f + g * 0.12f),
                                  Quaternion.Euler(0f, g * 17f, 0f), new Vector3(0.26f, 0.01f, 0.5f),
                                  ColWallTile, noCollider: true);

                        // 도기 — <b>길고 좁다.</b> 정사각형에 가까우면 배수구로 보인다.
                        Block(t, $"SquatPan_{i}", new Vector3(cx, 0.145f, dz),
                              Quaternion.identity, new Vector3(0.52f, 0.11f, 1.20f), ColCream, noCollider: true);
                        Block(t, $"SquatRim_{i}", new Vector3(cx, 0.197f, dz),
                              Quaternion.identity, new Vector3(0.58f, 0.03f, 1.28f), ColCream, noCollider: true);
                        // ★ <b>구멍이 주인공이다.</b> 도기 길이의 82% 를 먹는 검은 홈.
                        Block(t, $"SquatHole_{i}", new Vector3(cx, 0.176f, dz + 0.04f),
                              Quaternion.identity, new Vector3(0.34f, 0.03f, 0.98f), ColBearDark, noCollider: true);
                        // 홈 안쪽 턱 — 구멍이 <b>깊어 보여야</b> 구멍이다. 검은 판 하나면 칠한 자국이야.
                        for (int sx = -1; sx <= 1; sx += 2)
                            Block(t, $"SquatHoleWall_{i}_{sx}",
                                  new Vector3(cx + sx * 0.175f, 0.168f, dz + 0.04f),
                                  Quaternion.identity, new Vector3(0.03f, 0.05f, 0.98f), ColWoodRail, noCollider: true);

                        // 뒤가 솟은 덮개 — <b>둥글어야</b> 한다. 네모 상자면 «앞에 놓인 벽돌» 이다.
                        // ★ 돔 중심은 <b>반지름만큼 올려야</b> 단 위에 얹힌다 —
                        // 0.19 에 두니 바닥(y 0) 아래로 4cm 뚫고 내려갔다.
                        Ball(t, $"SquatHood_{i}", new Vector3(cx, 0.26f, dz - 0.60f),
                             new Vector3(0.50f, 0.46f, 0.44f), ColCream);
                        Block(t, $"SquatHoodSkirt_{i}", new Vector3(cx, 0.20f, dz - 0.60f),
                              Quaternion.identity, new Vector3(0.54f, 0.12f, 0.40f), ColCream, noCollider: true);
                        // 물 나오는 홈 — 덮개에서 구멍 쪽으로. 물길이 보이면 «변기» 가 된다.
                        Block(t, $"SquatChannel_{i}", new Vector3(cx, 0.192f, dz - 0.38f),
                              Quaternion.identity, new Vector3(0.18f, 0.02f, 0.3f), ColWoodRail, noCollider: true);

                        // 발판 — 도기 <b>양옆에 딱 붙여</b> 세운다. 떨어져 있으면 그냥 턱이다.
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Block(t, $"SquatStep_{i}_{s}", new Vector3(cx + s * 0.35f, 0.18f, dz + 0.10f),
                                  Quaternion.identity, new Vector3(0.20f, 0.08f, 0.76f), ColCream, noCollider: true);
                            for (int g = 0; g < 5; g++)
                                Block(t, $"SquatGrip_{i}_{s}_{g}",
                                      new Vector3(cx + s * 0.35f, 0.225f, dz - 0.22f + g * 0.15f),
                                      Quaternion.identity, new Vector3(0.18f, 0.012f, 0.035f),
                                      ColStoneWall, noCollider: true);
                        }

                        // 물내림 — 벽에서 내려오는 관 + 손잡이. 물이 어디서 오는지가 보여야 한다
                        Block(t, $"SquatPipe_{i}", new Vector3(cx, 1.05f, inZb + 0.12f),
                              Quaternion.identity, new Vector3(0.07f, 1.7f, 0.07f), ColStoneWall, noCollider: true);
                        Block(t, $"SquatElbow_{i}", new Vector3(cx, 0.24f, inZb + 0.22f),
                              Quaternion.identity, new Vector3(0.07f, 0.07f, 0.26f), ColStoneWall, noCollider: true);
                        // ★ 손잡이도 발광이었다 — 금속색으로.
                        Block(t, $"SquatLever_{i}", new Vector3(cx + 0.14f, 1.32f, inZb + 0.14f),
                              Quaternion.Euler(0f, 0f, -22f), new Vector3(0.2f, 0.04f, 0.04f),
                              ColStoneWall, noCollider: true);

                        // 손잡이 봉 — 쪼그려 앉는 칸에는 잡을 데가 있어야 한다
                        Block(t, $"SquatBar_{i}", new Vector3(cx - 0.88f, 0.72f, dz),
                              Quaternion.identity, new Vector3(0.05f, 0.05f, 0.8f), ColStoneWall, noCollider: true);
                        for (int s = -1; s <= 1; s += 2)
                            Block(t, $"SquatBarFoot_{i}_{s}", new Vector3(cx - 0.94f, 0.72f, dz + s * 0.4f),
                                  Quaternion.identity, new Vector3(0.09f, 0.09f, 0.05f), ColStoneWall, noCollider: true);

                        // ── 물통과 바가지 ──
                        // ★★ 2026-09-22 (3차) 유저: *"저 빛나는 막대랑 부속품은 뭔데."*
                        // <b>바가지가 빛나고 있었다.</b> `ColLantern`(#F5C069)은 석등 색이라
                        // <see cref="Surfaces"/> 표에서 <b>발광</b>으로 잡힌다 — 소품에 쓰면
                        // 화장실 구석에서 <b>전구처럼 빛난다.</b>
                        //
                        // > **팔레트에서 색을 고를 때 마감(Finish)까지 같이 고르는 것이다.**
                        // > 발광인 색은 넷뿐인데(창문 · 석등 · 발판 화살표 · 천창) 그 중 하나를
                        // > 플라스틱 바가지에 썼다. 노란 소품은 `ColMapleGold`(#C9933E)로.
                        //
                        // 자리도 정리했다 — 셋이 칸 한가운데에 흩어져 있어서 «부속품» 으로 보였다.
                        // <b>물통을 구석에 놓고, 바가지를 그 안에 담고, 뚫어뻥을 뒤에 기대 세운다.</b>
                        // 한 덩어리로 모으면 «청소 도구가 놓인 구석» 이라는 한 장면이 된다.
                        float wx = cx + 0.78f, wz = inZb + 0.52f;
                        Disc(t, $"WaterBinFoot_{i}", new Vector3(wx, 0.02f, wz),
                             new Vector3(0.44f, 0.02f, 0.44f), ColStoneWall);
                        Disc(t, $"WaterBin_{i}", new Vector3(wx, 0.26f, wz),
                             new Vector3(0.52f, 0.24f, 0.52f), ColMint);
                        Disc(t, $"WaterBinRim_{i}", new Vector3(wx, 0.505f, wz),
                             new Vector3(0.56f, 0.025f, 0.56f), ColStoneWall);
                        Disc(t, $"WaterBinFill_{i}", new Vector3(wx, 0.47f, wz),
                             new Vector3(0.46f, 0.012f, 0.46f), ColWater);
                        // 손잡이 — 반원은 못 만드니 기둥 둘 + 가로대로 접는다(양동이와 같은 방식)
                        for (int sx = -1; sx <= 1; sx += 2)
                            Block(t, $"WaterBinEar_{i}_{sx}", new Vector3(wx + sx * 0.27f, 0.60f, wz),
                                  Quaternion.Euler(0f, 0f, sx * 13f), new Vector3(0.025f, 0.2f, 0.025f),
                                  ColStoneWall, noCollider: true);
                        Block(t, $"WaterBinBail_{i}", new Vector3(wx, 0.695f, wz), Quaternion.identity,
                              new Vector3(0.52f, 0.025f, 0.025f), ColStoneWall, noCollider: true);

                        // 바가지 — <b>반구 + 긴 자루.</b> 물통 테에 걸쳐 놓는다.
                        // 바가지 — <b>물통 안에 담겨 있고 자루만 테 밖으로</b> 나온다.
                        // 공중에 떠 있으면 «빛나는 막대» 가 되고, 담겨 있으면 «바가지» 가 된다.
                        Ball(t, $"Dipper_{i}", new Vector3(wx - 0.08f, 0.44f, wz + 0.06f),
                             new Vector3(0.30f, 0.18f, 0.30f), ColMapleGold);
                        Ball(t, $"DipperHollow_{i}", new Vector3(wx - 0.08f, 0.50f, wz + 0.06f),
                             new Vector3(0.24f, 0.10f, 0.24f), ColWoodRail);
                        Block(t, $"DipperGrip_{i}", new Vector3(wx - 0.08f, 0.52f, wz + 0.40f),
                              Quaternion.Euler(16f, 0f, 0f), new Vector3(0.05f, 0.05f, 0.40f),
                              ColMapleGold, noCollider: true);

                        // 수도꼭지
                        Block(t, $"Tap_{i}", new Vector3(cx + 0.72f, 0.95f, inZb + 0.2f),
                              Quaternion.identity, new Vector3(0.06f, 0.06f, 0.3f), ColStoneWall, noCollider: true);

                        // ★ <b>이 칸에도 사연을 준다</b>(유저: *"왼쪽 칸막이는 연출이 덜 된 것 같다"*).
                        // 정비 곰이 «화장실이 막혔다» 고 하는데 정작 <b>막힌 칸이 없었다</b> —
                        // 여기가 그 칸이다. 뚫어뻥과 슬리퍼, 문에는 «수리 중» 팻말(아래 연출 블록).
                        // 뚫어뻥 — <b>구석 벽에 기대</b> 세운다. 칸 한가운데에 비스듬히 두면
                        // 화면을 가로질러서 «정체 모를 막대» 가 된다.
                        Block(t, $"Plunger_{i}", new Vector3(cx + 0.88f, 0.60f, inZb + 0.30f),
                              Quaternion.Euler(13f, 0f, 9f), new Vector3(0.045f, 1.0f, 0.045f),
                              ColWood, noCollider: true);
                        // 컵은 <b>납작한 돔 + 목</b>. 공 하나면 «빨간 구슬» 이고, 눌린 돔이라야 고무 컵이다.
                        Ball(t, $"PlungerCup_{i}", new Vector3(cx + 0.78f, 0.13f, inZb + 0.20f),
                             new Vector3(0.30f, 0.17f, 0.30f), ColRibbon);
                        Disc(t, $"PlungerNeck_{i}", new Vector3(cx + 0.79f, 0.22f, inZb + 0.22f),
                             new Vector3(0.11f, 0.04f, 0.11f), ColRibbon);
                        for (int s = -1; s <= 1; s += 2)
                            Block(t, $"Slipper_{i}_{s}", new Vector3(cx + s * 0.17f, 0.04f, dz + 1.02f),
                                  Quaternion.Euler(0f, s * 9f, 0f), new Vector3(0.14f, 0.07f, 0.3f),
                                  ColMint, noCollider: true);
                    }

                    // 휴지걸이 — 칸마다. 작은 물건이 있어야 칸이 «쓰는 곳» 이 된다
                    Block(t, $"PaperHolder_{i}", new Vector3(cx + 0.95f, 0.85f, inZb + 1.0f),
                          Quaternion.identity, new Vector3(0.1f, 0.12f, 0.16f), ColStoneWall, noCollider: true);
                    Disc(t, $"PaperRoll_{i}", new Vector3(cx + 0.86f, 0.85f, inZb + 1.0f),
                         new Vector3(0.22f, 0.06f, 0.22f), ColCream, Quaternion.Euler(0f, 0f, 90f));

                    // 걸이 못 — 옷 거는 자리. 칸 문 안쪽에 있는 게 실제 모습이다
                    Block(t, $"Hook_{i}", new Vector3(cx, 1.6f, inZb + 2.1f),
                          Quaternion.identity, new Vector3(0.06f, 0.1f, 0.1f), ColStoneWall, noCollider: true);
                }

                // ── 세면대 줄 ──  ★ 2026-09-21 <b>오른쪽 옆벽으로 옮겼다.</b>
                //
                // 유저 둘: *"화장실 처음 문 열면 세면대가 바깥쪽 문을 막아"* ·
                // *"세면대 방향이 반대야. 손 씻는데 문 밖으로 나가는 쪽을 보고 있어."*
                // <b>원인은 하나다</b> — 세면대가 <b>출입문이 있는 앞벽</b>에 붙어 있었다.
                // 문 구멍이 x −1.8~1.8 인데 상판이 x 0.05~3.55 라 절반을 막았고,
                // 거울이 앞벽이라 거울을 보면 <b>등 뒤가 방이고 눈앞이 밖</b>이었다.
                //
                // 옆벽으로 옮기면 셋이 한 번에 풀린다 — 문 앞이 비고, 거울을 보면 방을 등지고,
                // «들어와서 오른쪽» 이라는 동선이 생긴다.
                {
                    float rowZ = 0.2f;
                    var waterBits = new System.Collections.Generic.List<GameObject>();

                    // 벽 타일 — 세면대 쪽 벽만. 크림 벽에 붙으면 «물 쓰는 자리» 로 갈린다
                    Block(t, "TileWall", new Vector3(inX - 0.03f, 1.2f, rowZ),
                          Quaternion.identity, new Vector3(0.06f, 2.4f, 3.6f), ColWallTile, noCollider: true);
                    // 타일 줄눈 — 한 덩어리 색이면 그게 레고다. 줄이 있어야 «타일» 로 읽힌다
                    for (int g = -3; g <= 3; g++)
                        Block(t, $"TileSeamV_{g}", new Vector3(inX - 0.065f, 1.2f, rowZ + g * 0.5f),
                              Quaternion.identity, new Vector3(0.01f, 2.4f, 0.025f), ColCream, noCollider: true);
                    for (int g = 0; g < 4; g++)
                        Block(t, $"TileSeamH_{g}", new Vector3(inX - 0.065f, 0.3f + g * 0.6f, rowZ),
                              Quaternion.identity, new Vector3(0.01f, 0.025f, 3.6f), ColCream, noCollider: true);

                    // 긴 상판 하나 — 세면대마다 따로 두면 조각만 늘고 덜 세면대 같다
                    Block(t, "CounterTop", new Vector3(inX - 0.32f, 0.84f, rowZ),
                          Quaternion.identity, new Vector3(0.62f, 0.09f, 3.5f), ColStoneWall, noCollider: true);
                    Block(t, "CounterApron", new Vector3(inX - 0.60f, 0.72f, rowZ),
                          Quaternion.identity, new Vector3(0.08f, 0.16f, 3.5f), ColStoneWall, noCollider: true);

                    for (int i = 0; i < 2; i++)
                    {
                        float bz = rowZ + 0.75f - i * 1.5f;

                        // 볼 — 상판보다 <b>아래로 파여야</b> 세면대다. 위에 얹으면 그릇이야
                        Disc(t, $"BasinRim_{i}", new Vector3(inX - 0.32f, 0.86f, bz),
                             new Vector3(0.42f, 0.04f, 0.52f), ColCream);
                        Disc(t, $"BasinBowl_{i}", new Vector3(inX - 0.32f, 0.76f, bz),
                             new Vector3(0.36f, 0.16f, 0.44f), ColCream);
                        Disc(t, $"BasinDrain_{i}", new Vector3(inX - 0.32f, 0.69f, bz),
                             new Vector3(0.09f, 0.02f, 0.09f), ColStoneWall);

                        // 수전 — <b>벽 쪽</b>에 선다. 방 쪽에 두면 서는 자리가 없다
                        Block(t, $"TapBody_{i}", new Vector3(inX - 0.12f, 0.99f, bz),
                              Quaternion.identity, new Vector3(0.07f, 0.28f, 0.07f), ColStoneWall, noCollider: true);
                        Block(t, $"TapSpout_{i}", new Vector3(inX - 0.25f, 1.11f, bz),
                              Quaternion.identity, new Vector3(0.28f, 0.05f, 0.05f), ColStoneWall, noCollider: true);
                        Block(t, $"TapLever_{i}", new Vector3(inX - 0.10f, 1.15f, bz),
                              Quaternion.Euler(0f, 0f, 24f), new Vector3(0.16f, 0.04f, 0.04f),
                              ColStoneWall, noCollider: true);

                        // 배수관 — 상판 밑이 비면 «벽에 붙은 선반» 으로 보인다
                        Block(t, $"Trap_{i}", new Vector3(inX - 0.32f, 0.55f, bz),
                              Quaternion.identity, new Vector3(0.07f, 0.34f, 0.07f), ColStoneWall, noCollider: true);
                        Block(t, $"TrapBend_{i}", new Vector3(inX - 0.20f, 0.4f, bz),
                              Quaternion.identity, new Vector3(0.3f, 0.07f, 0.07f), ColStoneWall, noCollider: true);

                        // 거울 — 테두리를 뒤에 깔고 유리를 앞에. 층을 나눠야 지지직 안 거린다
                        // (타일 앞면 inX−0.06 · 테두리 −0.095 · 유리 −0.145, 겹치는 데 없음)
                        Block(t, $"MirrorFrame_{i}", new Vector3(inX - 0.095f, 1.85f, bz),
                              Quaternion.identity, new Vector3(0.05f, 1.2f, 1.0f), ColWood, noCollider: true);
                        // ★ 거울에 `ColWindow`(#F0C070)를 쓰면 <b>거울이 스스로 빛난다</b> —
                        // 그 색은 한지 창 색이라 팔레트에서 발광으로 잡힌다.
                        // 거울은 <b>광택</b>이어야 주변을 비추는 것처럼 보인다(`ColWater` = 연못 색).
                        Block(t, $"Mirror_{i}", new Vector3(inX - 0.145f, 1.85f, bz),
                              Quaternion.identity, new Vector3(0.04f, 1.1f, 0.9f), ColWater, noCollider: true);
                        // 거울 위 조명 — 화장실을 화장실로 만드는 건 천장등이 아니라 <b>거울등</b>이다
                        Block(t, $"MirrorLamp_{i}", new Vector3(inX - 0.20f, 2.52f, bz),
                              Quaternion.identity, new Vector3(0.14f, 0.07f, 0.9f), ColLantern, noCollider: true);
                        Block(t, $"MirrorLampCase_{i}", new Vector3(inX - 0.20f, 2.60f, bz),
                              Quaternion.identity, new Vector3(0.18f, 0.1f, 1.0f), ColStoneWall, noCollider: true);

                        // 비누 — 작은 물건이 방 크기를 알려준다.
                        // ★ <b>네모 비누는 지우개로 보인다</b>(유저 요청: 타원 + 받침).
                        // 원통을 x/z 로 다르게 눌러 타원을 만든다 — 새 메시가 0개다.
                        Disc(t, $"SoapDish_{i}", new Vector3(inX - 0.30f, 0.900f, bz + 0.44f),
                             new Vector3(0.17f, 0.012f, 0.13f), ColStoneWall);
                        Disc(t, $"Soap_{i}", new Vector3(inX - 0.30f, 0.925f, bz + 0.44f),
                             new Vector3(0.13f, 0.022f, 0.09f), ColMint);

                        // ── 물 ──  틀면 나오고 잠그면 멈춘다(`Faucet`).
                        // 씬에 미리 지어 두고 <b>켜고 끄기만</b> 한다 — 실행 중에 만들면
                        // 씬을 다시 구울 때마다 자리가 달라진다.
                        var jet = Disc(t, $"WaterJet_{i}", new Vector3(inX - 0.33f, 0.913f, bz),
                                       new Vector3(0.024f, 0.173f, 0.024f), ColWater);
                        var pool = Disc(t, $"WaterPool_{i}", new Vector3(inX - 0.32f, 0.722f, bz),
                                        new Vector3(0.30f, 0.006f, 0.38f), ColWater);
                        // ★ <b>거울 김을 걷어냈다</b>(2026-09-22 유저: *"물을 켜면 왜 유리가
                        // 불투명해지는지 무슨 원리인지도 모르겠어."*). 맞는 반응이고 내 잘못이다 —
                        // 김을 <b>불투명한 크림색 판</b>으로 만들어 거울 앞에 세웠으니 «김» 이 아니라
                        // <b>거울이 판때기로 바뀐 것</b>으로 보인다. 게다가 <b>찬물은 거울을 안 흐린다</b>
                        // — 물리적으로도 틀렸다. 물 연출은 물줄기 · 웅덩이 · 배수구 자국으로 충분하다.
                        jet.isStatic = false; pool.isStatic = false;
                        jet.SetActive(false); pool.SetActive(false);
                        waterBits.Add(jet); waterBits.Add(pool);
                    }

                    // 종이타월과 쓰레기통 — 씻고 나서 갈 곳이 있어야 동선이 닫힌다.
                    // <b>문 앞이 아니라 세면대 끝</b>에 둔다(전에는 출입문 정면에 서 있었다)
                    Block(t, "TowelBox", new Vector3(inX - 0.22f, 1.45f, rowZ + 1.75f),
                          Quaternion.identity, new Vector3(0.16f, 0.46f, 0.34f), ColStoneWall, noCollider: true);
                    Block(t, "Bin", new Vector3(inX - 0.45f, 0.3f, rowZ + 2.15f),
                          Quaternion.identity, new Vector3(0.38f, 0.6f, 0.38f), ColBearDark, noCollider: true);
                    Block(t, "BinLid", new Vector3(inX - 0.45f, 0.62f, rowZ + 2.15f),
                          Quaternion.identity, new Vector3(0.42f, 0.05f, 0.42f), ColStoneWall, noCollider: true);

                    // 수건걸이와 수건 둘 — 씻은 손을 닦을 데가 있어야 동선이 끝난다.
                    // 타일(z −1.6~2.0) 바깥, 종이타월(z 1.78~2.12) 옆의 빈 벽에 건다.
                    Block(t, "TowelBar", new Vector3(inX - 0.10f, 1.35f, rowZ + 2.15f),
                          Quaternion.identity, new Vector3(0.05f, 0.05f, 0.62f), ColStoneWall, noCollider: true);
                    for (int k = 0; k < 2; k++)
                        Block(t, $"Towel_{k}", new Vector3(inX - 0.13f, 1.06f, rowZ + 2.10f + k * 0.26f),
                              Quaternion.Euler(0f, 0f, k == 0 ? 1.5f : -2f),
                              new Vector3(0.05f, 0.56f, 0.24f),
                              k == 0 ? ColMint : ColRibbon, noCollider: true);

                    // 배수구로 흘러가는 물자국 — 물이 «어디로 가는지» 가 보여야 물이 된다
                    var stream = Block(t, "WaterTrail", new Vector3(inX - 1.6f, 0.014f, -0.05f),
                                       Quaternion.Euler(0f, 14f, 0f), new Vector3(2.4f, 0.006f, 0.34f),
                                       ColWater, noCollider: true);
                    stream.isStatic = false;
                    stream.SetActive(false);
                    waterBits.Add(stream);

                    // 수도꼭지 — 다가가서 E. 물줄기를 <b>직접 들고 있어서</b> 찾을 일이 없다
                    // (`FindObjectsByType` 가 꺼진 것을 못 찾는 함정을 아예 안 만든다).
                    var faucetGo = new GameObject("Faucet");
                    faucetGo.transform.SetParent(t, false);
                    faucetGo.transform.localPosition = new Vector3(inX - 0.45f, 1.0f, rowZ);
                    var faucet = faucetGo.AddComponent<Faucet>();
                    faucet.water = waterBits.ToArray();
                    faucet.range = 3.0f;
                    faucetGo.isStatic = true;

                    // 배관 — 세면대 밑을 따라 한 줄, 벽을 타고 올라간다.
                    // 화장실에만 있는 실루엣이라 이것 하나로 «여기가 화장실» 이 된다
                    Block(t, "PipeRun", new Vector3(inX - 0.14f, 0.42f, rowZ),
                          Quaternion.identity, new Vector3(0.08f, 0.08f, 3.4f), ColStoneWall, noCollider: true);
                    Block(t, "PipeRise", new Vector3(inX - 0.14f, 1.6f, rowZ - 1.7f),
                          Quaternion.identity, new Vector3(0.08f, 2.4f, 0.08f), ColStoneWall, noCollider: true);
                    for (int g = 0; g < 3; g++)
                        Block(t, $"PipeClamp_{g}", new Vector3(inX - 0.14f, 0.7f + g * 0.9f, rowZ - 1.7f),
                              Quaternion.identity, new Vector3(0.12f, 0.06f, 0.12f), ColBearDark, noCollider: true);
                }

                // ── 방 마감 ──  «레고 냄새» 를 빼는 것은 물건을 늘리는 게 아니라 <b>면을 쪼개는</b> 것이다
                // (캠퍼스 외벽에서 배운 셋: 모서리 기둥 · 굽도리 턱 · 30cm 미만 작은 물건).

                // ★ <b>천장을 내린다.</b> 이 방은 8 × 7 × 6m 다 — 화장실이 아니라 창고 크기고,
                // 창고는 조명을 어떻게 달아도 창고다(곰밥마당에서 배운 것: 공간 → 배경 → 색 → 조명).
                // 앞벽 창이 y 2.6 이라 그 위 3.3m 에 반자를 깐다.
                Block(t, "DropCeiling", new Vector3(0f, ceilY, 0f), Quaternion.identity,
                      new Vector3(w - 1.2f, 0.1f, d - 1.2f), ColCream, noCollider: true);
                for (int g = -2; g <= 2; g++)
                    Block(t, $"CeilBeam_{g}", new Vector3(0f, ceilY - 0.11f, g * 1.15f),
                          Quaternion.identity, new Vector3(w - 1.2f, 0.12f, 0.14f), ColWoodRail, noCollider: true);
                // 천장 돌림띠 — 벽과 천장이 한 선으로 만나면 상자가 된다
                for (int s = -1; s <= 1; s += 2)
                {
                    Block(t, $"Cornice_X{s}", new Vector3(s * (inX - 0.07f), ceilY - 0.2f, 0f),
                          Quaternion.identity, new Vector3(0.14f, 0.12f, d - 1.2f), ColTrimDark, noCollider: true);
                    Block(t, $"Cornice_Z{s}", new Vector3(0f, ceilY - 0.2f, s * (inZf - 0.07f)),
                          Quaternion.identity, new Vector3(w - 1.2f, 0.12f, 0.14f), ColTrimDark, noCollider: true);
                }

                // 모서리 기둥 — 상자의 날 선 모서리 넷이 레고의 정체다. 덮으면 면이 셋으로 갈린다
                for (int sx2 = -1; sx2 <= 1; sx2 += 2)
                    for (int sz2 = -1; sz2 <= 1; sz2 += 2)
                        Block(t, $"CornerPost_{sx2}_{sz2}",
                              new Vector3(sx2 * (inX - 0.11f), ceilY * 0.5f, sz2 * (inZf - 0.11f)),
                              Quaternion.identity, new Vector3(0.22f, ceilY, 0.22f), ColTrimDark, noCollider: true);

                // 굽도리 타일 + 턱. 색만 바꾸면 «칠한 자국» 이고, 턱이 있어야 다른 재료로 읽힌다
                for (int s = -1; s <= 1; s += 2)
                {
                    Block(t, $"Skirt_X{s}", new Vector3(s * (inX - 0.04f), 0.15f, 0f),
                          Quaternion.identity, new Vector3(0.08f, 0.3f, d - 1.3f), ColWallTile, noCollider: true);
                    Block(t, $"SkirtLip_X{s}", new Vector3(s * (inX - 0.09f), 0.31f, 0f),
                          Quaternion.identity, new Vector3(0.1f, 0.04f, d - 1.3f), ColTrimDark, noCollider: true);
                    Block(t, $"Skirt_Z{s}", new Vector3(0f, 0.15f, s * (inZf - 0.04f)),
                          Quaternion.identity, new Vector3(w - 1.3f, 0.3f, 0.08f), ColWallTile, noCollider: true);
                    Block(t, $"SkirtLip_Z{s}", new Vector3(0f, 0.31f, s * (inZf - 0.09f)),
                          Quaternion.identity, new Vector3(w - 1.3f, 0.04f, 0.1f), ColTrimDark, noCollider: true);
                }

                // 바닥 줄눈 — 눈이 크기를 잴 자를 못 찾으면 방 전체가 장난감으로 보인다.
                // 바닥 윗면이 y 0 이라 1.2cm 위로 띄운다(같은 평면이면 지지직거린다)
                for (int g = -2; g <= 2; g++)
                {
                    Block(t, $"FloorSeamX_{g}", new Vector3(g * 1.3f, 0.012f, 0f), Quaternion.identity,
                          new Vector3(0.035f, 0.02f, d - 1.3f), ColWallTile, noCollider: true);
                    Block(t, $"FloorSeamZ_{g}", new Vector3(0f, 0.012f, g * 1.3f), Quaternion.identity,
                          new Vector3(w - 1.3f, 0.02f, 0.035f), ColWallTile, noCollider: true);
                }
                // 바닥 배수구 — 한국 공중화장실에 반드시 있다. 물을 쓰는 방이라는 신호
                Disc(t, "FloorDrain", new Vector3(-0.6f, 0.014f, -0.2f),
                     new Vector3(0.24f, 0.012f, 0.24f), ColBearDark);
                Disc(t, "FloorDrainRim", new Vector3(-0.6f, 0.01f, -0.2f),
                     new Vector3(0.3f, 0.01f, 0.3f), ColStoneWall);

                // 환풍기 — 뒷벽 높은 곳. 전에는 옆벽 x 3.92 였는데 그건 <b>벽 속</b>이었다
                Block(t, "Vent", new Vector3(1.9f, 2.7f, inZb + 0.04f), Quaternion.identity,
                      new Vector3(0.6f, 0.5f, 0.06f), ColStoneWall, noCollider: true);
                for (int i = 0; i < 4; i++)
                    Block(t, $"VentSlat_{i}", new Vector3(1.9f, 2.54f + i * 0.11f, inZb + 0.09f),
                          Quaternion.identity, new Vector3(0.52f, 0.04f, 0.04f), ColBearDark, noCollider: true);

                // ── 청소도구 구석 ──  사람이 관리한다는 신호. 정비 곰 대사와도 이어진다.
                // ★ 2026-09-22 유저: *"청소도구함이랑 밀대 같은 게 레고처럼 조잡해 보인다."*
                // 맞다 — <b>상자 둘</b>이었다. 물건이 물건으로 보이려면 «덩어리 하나」가 아니라
                // <b>덩어리 + 손잡이 + 이음매 + 작은 것</b>이 있어야 한다(레고 규칙 그대로).
                {
                    float cx0 = -inX + 0.62f;
                    float cz0 = inZf - 0.75f;

                    // 청소도구함 — 세로로 긴 철제 사물함. 몸통 하나면 냉장고로 보인다.
                    Block(t, "LockerBody", new Vector3(cx0, 0.94f, cz0),
                          Quaternion.identity, new Vector3(0.62f, 1.72f, 0.42f), ColWallTile, noCollider: true);
                    Block(t, "LockerTop", new Vector3(cx0, 1.82f, cz0),
                          Quaternion.identity, new Vector3(0.66f, 0.05f, 0.46f), ColStoneWall, noCollider: true);
                    // 다리 — 바닥에 <b>딱 붙은 상자</b>는 바닥에 그린 무늬로 보인다
                    for (int sx = -1; sx <= 1; sx += 2)
                        for (int sz = -1; sz <= 1; sz += 2)
                            Block(t, $"LockerFoot_{sx}{sz}",
                                  new Vector3(cx0 + sx * 0.25f, 0.05f, cz0 + sz * 0.15f),
                                  Quaternion.identity, new Vector3(0.07f, 0.1f, 0.07f), ColBearDark, noCollider: true);
                    // 문짝 둘 + 손잡이 + 통풍 살 — 이 셋이 «사물함» 을 만든다
                    for (int d2 = -1; d2 <= 1; d2 += 2)
                    {
                        Block(t, $"LockerDoor_{d2}", new Vector3(cx0 + d2 * 0.155f, 0.98f, cz0 - 0.215f),
                              Quaternion.identity, new Vector3(0.29f, 1.52f, 0.03f), ColMint, noCollider: true);
                        Block(t, $"LockerHandle_{d2}", new Vector3(cx0 + d2 * 0.035f, 0.98f, cz0 - 0.245f),
                              Quaternion.identity, new Vector3(0.025f, 0.22f, 0.025f), ColStoneWall, noCollider: true);
                        for (int v = 0; v < 4; v++)
                            Block(t, $"LockerVent_{d2}_{v}",
                                  new Vector3(cx0 + d2 * 0.155f, 1.58f - v * 0.07f, cz0 - 0.232f),
                                  Quaternion.identity, new Vector3(0.2f, 0.018f, 0.012f), ColBearDark, noCollider: true);
                    }
                    // 이름표 — 30cm 미만이 있어야 눈이 크기를 잰다
                    Block(t, "LockerLabel", new Vector3(cx0, 1.66f, cz0 - 0.235f),
                          Quaternion.identity, new Vector3(0.24f, 0.09f, 0.012f), ColCream, noCollider: true);

                    // 양동이 — <b>위가 넓고 아래가 좁아야</b> 양동이다. 정육면체는 상자야.
                    float bx = cx0 + 0.72f, bz = cz0 + 0.06f;
                    Disc(t, "PailBase", new Vector3(bx, 0.03f, bz), new Vector3(0.24f, 0.03f, 0.24f), ColMint);
                    Disc(t, "PailBody", new Vector3(bx, 0.17f, bz), new Vector3(0.30f, 0.14f, 0.30f), ColMint);
                    Disc(t, "PailRim",  new Vector3(bx, 0.315f, bz), new Vector3(0.33f, 0.02f, 0.33f), ColStoneWall);
                    Disc(t, "PailWater", new Vector3(bx, 0.27f, bz), new Vector3(0.27f, 0.01f, 0.27f), ColWater);
                    // 손잡이 — 반원을 못 만드니 <b>기둥 둘 + 가로대</b>로 접는다
                    for (int sx = -1; sx <= 1; sx += 2)
                        Block(t, $"PailEar_{sx}", new Vector3(bx + sx * 0.155f, 0.38f, bz),
                              Quaternion.Euler(0f, 0f, sx * 12f), new Vector3(0.02f, 0.16f, 0.02f),
                              ColStoneWall, noCollider: true);
                    Block(t, "PailBail", new Vector3(bx, 0.455f, bz), Quaternion.identity,
                          new Vector3(0.30f, 0.02f, 0.02f), ColStoneWall, noCollider: true);
                    // 짜는 망 — 한국 청소도구함에 반드시 있다
                    Block(t, "PailWringer", new Vector3(bx - 0.20f, 0.40f, bz), Quaternion.Euler(0f, 0f, 16f),
                          new Vector3(0.1f, 0.26f, 0.22f), ColStoneWall, noCollider: true);

                    // 밀대 — 자루 · 그립 · 목 · 머리 · 실. <b>막대 하나는 막대로 보인다.</b>
                    float mx = cx0 + 0.44f, mz = cz0 - 0.30f;
                    var lean = Quaternion.Euler(13f, 0f, -7f);
                    Block(t, "MopPole", new Vector3(mx, 0.78f, mz), lean,
                          new Vector3(0.035f, 1.5f, 0.035f), ColStoneWall, noCollider: true);
                    Block(t, "MopGrip", new Vector3(mx + 0.06f, 1.44f, mz - 0.14f), lean,
                          new Vector3(0.05f, 0.2f, 0.05f), ColBearDark, noCollider: true);
                    Block(t, "MopNeck", new Vector3(mx - 0.09f, 0.14f, mz + 0.21f), lean,
                          new Vector3(0.06f, 0.16f, 0.06f), ColBearDark, noCollider: true);
                    Block(t, "MopHead", new Vector3(mx - 0.10f, 0.05f, mz + 0.24f), Quaternion.Euler(0f, 9f, 0f),
                          new Vector3(0.34f, 0.07f, 0.16f), ColStoneWall, noCollider: true);
                    for (int f = 0; f < 5; f++)
                        Block(t, $"MopYarn_{f}", new Vector3(mx - 0.23f + f * 0.065f, 0.025f, mz + 0.26f),
                              Quaternion.Euler(0f, 9f, 0f), new Vector3(0.05f, 0.04f, 0.2f),
                              ColCream, noCollider: true);

                    // 빗자루 — 둘이 나란히 서 있어야 «도구함» 으로 읽힌다
                    float sx2 = cx0 - 0.44f, sz2 = cz0 - 0.26f;
                    var lean2 = Quaternion.Euler(10f, 0f, 9f);
                    Block(t, "BroomPole", new Vector3(sx2, 0.76f, sz2), lean2,
                          new Vector3(0.03f, 1.4f, 0.03f), ColWood, noCollider: true);
                    Block(t, "BroomHead", new Vector3(sx2 + 0.12f, 0.08f, sz2 + 0.16f), Quaternion.Euler(0f, -8f, 0f),
                          new Vector3(0.3f, 0.16f, 0.07f), ColWoodRail, noCollider: true);

                    // 작은 것들 — 세제통과 고무장갑. 30cm 미만이 크기의 자다
                    Block(t, "Detergent", new Vector3(cx0 - 0.15f, 1.95f, cz0), Quaternion.Euler(0f, 18f, 0f),
                          new Vector3(0.12f, 0.24f, 0.12f), ColRibbon, noCollider: true);
                    Block(t, "DetergentCap", new Vector3(cx0 - 0.15f, 2.09f, cz0), Quaternion.Euler(0f, 18f, 0f),
                          new Vector3(0.06f, 0.05f, 0.06f), ColCream, noCollider: true);
                    // ★ 고무장갑도 발광이었다 — 청소도구함 위에서 전구처럼 빛났다.
                    for (int g = 0; g < 2; g++)
                        Block(t, $"Glove_{g}", new Vector3(cx0 + 0.14f + g * 0.1f, 1.90f, cz0 - 0.1f),
                              Quaternion.Euler(0f, 0f, g == 0 ? 6f : -8f),
                              new Vector3(0.07f, 0.2f, 0.05f), ColMapleGold, noCollider: true);
                }

                // 바닥 물기 조심 표지 — 문 앞 한가운데를 피해 세면대 앞에 세운다
                Block(t, "WetSign", new Vector3(2.4f, 0.4f, 1.3f), Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(0.44f, 0.8f, 0.36f), ColRibbon, noCollider: true);

                // ── 조명 ──  천장을 내렸으니 등도 같이 내려온다.
                // 공통 천장등(`InLamp_*`)은 h−0.55 = 5.45m 에 있어서 <b>반자 위에 묻힌다.</b>
                foreach (string lampName in new[] { "InLamp_-1", "InLamp_1" })
                {
                    var lamp = t.Find(lampName);
                    if (lamp == null) continue;
                    lamp.localPosition = new Vector3(lamp.localPosition.x * 0.55f, ceilY - 0.14f, 0.3f);
                    lamp.localScale = new Vector3(1.2f, 0.08f, 0.55f);
                }

                // 실시간 조명 <b>한 개</b>. §7.6 이 막는 건 «방 열셋에 다 다는 것» 이고,
                // 씻는 자리 하나에 그림자 없는 등을 두는 건 그 예산 안이다.
                // 발광 재질만으로는 <b>바닥과 벽이 안 밝아져서</b> 방이 평평하게 보인다.
                var toiletLight = new GameObject("ToiletLight");
                toiletLight.transform.SetParent(t, false);
                toiletLight.transform.localPosition = new Vector3(inX - 1.3f, ceilY - 0.6f, 0.2f);
                var tl = toiletLight.AddComponent<Light>();
                tl.type = LightType.Point;
                tl.range = 9f;
                tl.intensity = 1.35f;
                tl.color = new Color(1f, 0.94f, 0.84f);
                tl.shadows = LightShadows.None;
                toiletLight.isStatic = true;

                // ── 연출 ──  2026-09-22 유저: *"화장실 연출도 뭐 없을까. 게임처럼."*
                // 가만히 있는 것만 잘 지어 놓으면 «방» 이지 «지금 뭔가 일어나는 곳» 이 아니다.
                // <b>도는 것 · 깜빡이는 것 · 반응하는 것</b> 셋을 더한다.
                {
                    // 환풍기 날개 — 벽에 붙은 살은 그대로 두고 <b>그 뒤에서</b> 돈다.
                    // 돌아가는 물건이 하나 있으면 방 전체가 «작동 중» 으로 읽힌다.
                    var fan = new GameObject("VentFan").transform;
                    fan.SetParent(t, false);
                    fan.localPosition = new Vector3(1.9f, 2.7f, inZb + 0.16f);
                    fan.gameObject.isStatic = false;
                    for (int b2 = 0; b2 < 3; b2++)
                    {
                        var blade = Block(fan, $"Blade_{b2}", Vector3.zero,
                                          Quaternion.Euler(0f, 0f, b2 * 60f),
                                          new Vector3(0.44f, 0.05f, 0.02f), ColBearDark, noCollider: true);
                        blade.isStatic = false;
                    }

                    // 바닥 웅덩이 — 배수구 둘레. 물색이라 «젖어 있다» 가 된다
                    Disc(t, "FloorPuddle", new Vector3(-0.6f, 0.016f, -0.2f),
                         new Vector3(1.05f, 0.004f, 0.9f), ColWater);
                    Disc(t, "FloorPuddle2", new Vector3(0.35f, 0.016f, 0.55f),
                         new Vector3(0.6f, 0.004f, 0.48f), ColWater);

                    // 젖은 발자국 — 칸에서 세면대 쪽으로. <b>누가 다녀갔다</b>가 방에 남는다
                    for (int f = 0; f < 6; f++)
                        Disc(t, $"PawPrint_{f}",
                             new Vector3(-1.5f + f * 0.62f, 0.015f, -0.9f + (f % 2 == 0 ? 0.16f : -0.16f)),
                             new Vector3(0.17f, 0.003f, 0.22f), ColWallTile);

                    // 칸 문에 붙는 «사용 중 / 비었음». 곰이 든 칸에만 단다 —
                    // 문을 여닫는 것이 <b>화면에 남는 결과</b>가 되어야 여는 맛이 생긴다.
                    GameObject busy = null, free = null;
                    if (bearStallDoor != null)
                    {
                        var hingeT = bearStallDoor.transform;
                        busy = Block(hingeT, "SignBusy", new Vector3(1.78f, 0.42f, -0.06f),
                                     Quaternion.identity, new Vector3(0.2f, 0.09f, 0.02f),
                                     ColRibbon, noCollider: true);
                        free = Block(hingeT, "SignFree", new Vector3(1.78f, 0.42f, -0.06f),
                                     Quaternion.identity, new Vector3(0.2f, 0.09f, 0.02f),
                                     ColMint, noCollider: true);
                        busy.isStatic = false; free.isStatic = false;
                        free.SetActive(false);
                    }

                    var mood = t.gameObject.AddComponent<ToiletMood>();
                    mood.flickerLamp = t.Find("InLamp_-1");
                    mood.roomLight = tl;
                    mood.fanBlades = fan;
                    mood.bearStall = bearStallDoor;
                    mood.busyMark = busy;
                    mood.freeMark = free;
                }

                // ★ 2026-09-21 — 유저가 받아온 곰을 <b>왼쪽 칸</b>에 앉힌다.
                // 원본은 50만 삼각형에 4096² 텍스처(67.8MB)였고, 게임용으로 줄여서 넣었다.
                //
                // ★★ <b>합치고 나서 줄여야 한다</b>(2026-09-21 유저: *"곰 폴리곤 깨진 것 같다"*).
                // 1차에서는 `remove_doubles` 를 데시메이트 <b>뒤에</b> 돌렸는데, AI 로 만든 원본은
                // 정점이 전부 쪼개져 있어서 조각마다 따로 줄어들며 <b>표면이 갈라졌다</b>
                // (3,999 tris 인데 정점 11,942 · 열린 가장자리 5,409).
                // 순서를 바꾸니 <b>열린 가장자리 2개</b>가 됐다 — 2026-09-16 곰에서 이미 배운 것이다.
                {
                    float stallX = -inX + 1.5f;

                    // ★ <b>변기 위에 앉힌다</b>(유저: *"곰인형이 앉아서 용변처리하는 칸"*).
                    // ★★ <b>targetWidth 가 «폭» 이 아니라 «폭과 깊이 중 큰 쪽» 이다.</b>
                    // 상체를 숙여 놔서 깊이(1.152)가 폭(0.790)보다 크다 —
                    // 실제 폭 0.46(변기 0.5 안쪽)을 얻으려면 0.46 × 1.152 / 0.790 = <b>0.671</b>.
                    // 좌석 링 윗면이 0.647. <b>7mm 만 묻는다</b> — 인형 엉덩이가 눌린 만큼이다.
                    var bear = MyModel(t, "Assets/My blender/Toilet_bear.fbx", "StallBear",
                                       new Vector3(stallX, 0.640f, inZb + 0.78f), 0.671f);
                    if (bear != null)
                    {
                        // ★★ <b>회전을 건드리지 마라</b>(2026-09-21). 전에 여기서
                        // `localRotation = Quaternion.identity` 로 덮었는데, 그게
                        // <b>FBX 의 축 변환 회전(−90° X)을 지워서 곰을 눕혀 놨다.</b>
                        // 재보니 유니티 바운즈가 (0.460, 0.671, 0.481) — 키(0.825×s=0.481)가
                        // <b>Z</b> 로, 깊이(1.152×s=0.671)가 <b>Y</b> 로 가 있었다. 누운 것이다.
                        // 유저가 "똥싸는 포즈가 아닌데" · "변기에 빠져서 이상한 짓" 이라고 한 게 이거야.
                        //
                        // 블렌더 +Y 가 유니티 +Z 라, <b>임포트 회전을 그대로 두면</b>
                        // 앞으로 숙인 방향이 저절로 칸 문 쪽(+Z)을 본다. 손댈 게 없다.

                        // ★ <b>본은 안 쓴다.</b> 몸 전체가 같이 오르내리는 동작은
                        // 트랜스폼 하나면 되고, 클립을 만들면 모델을 갈아끼울 때 깨진다.
                        // ★ 문이 열릴 때마다 <b>자세가 바뀐다</b>(유저: *"스파 마사지 받는 사람처럼
                        // 둥둥 떠 있다. 문 열 때마다 모션이 다르면 어때."*).
                        // 편안 / 힘주기 / 참는중 셋. <b>앞발은 못 든다</b> — 본이 없는 단일 메시라
                        // 부위를 따로 못 움직인다. 대신 박자 · 기울기 · 떨림으로 가른다.
                        var bob = bear.gameObject.AddComponent<BobMotion>();
                        bob.period = 0.8f;
                        bob.rise = 0.05f;
                        bob.squash = 0.06f;
                        bob.watchDoor = bearStallDoor;

                        // 움직일 물건이라 정적 배칭에 들어가면 안 된다 — 이 프로젝트에서
                        // 캠퍼스 문이 안 열린 원인이 그거였다(2026-09-18).
                        foreach (var kid in bear.GetComponentsInChildren<Transform>(true))
                            kid.gameObject.isStatic = false;
                    }
                }
                break;
            }

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

        // 배식대 — 스테인리스 상판에 급식 팬 다섯
        Block(t, "InServe", new Vector3(lineX, 0.5f, 0f), Quaternion.identity,
              new Vector3(1.9f, 1f, d - 8f), ColStoneWall, noCollider: true);
        Block(t, "InServeTop", new Vector3(lineX, 1.03f, 0f), Quaternion.identity,
              new Vector3(2.1f, 0.08f, d - 7.6f), ColWallTile, noCollider: true);

        // ★ 2026-09-23 유저 FBX 로 교체. 전에는 <see cref="Disc"/> 넷(지름 1.2 · 높이 0.28)이라
        // «납작한 원반» 이었고, 무엇보다 <b>메뉴는 다섯인데 통이 넷</b>이라 하나가 비었다.
        // 이제 밥·국·김치·반찬·후식이 <b>각자 제 팬</b>에 담겨 있다.
        //
        // <b>이름은 `InPot_0..4` 를 그대로 쓴다.</b> `CanteenDressing` 이 김과 국자를
        // 이 이름으로 찾는다 — 이름을 바꾸면 <b>수증기가 통째로 사라진다</b>(유저: "수증기는 그대로").
        //
        // 줄은 식판대(z +7.3)에서 반납대(z −7.5) 쪽으로 걷는다. 그래서 <b>밥이 맨 앞</b>이고
        // 후식이 맨 뒤 — 실제 급식 줄의 순서다.
        string[] pans =
        {
            "01_Rice_Pan.fbx", "02_Soup_Pan.fbx", "03_Kimchi_Pan.fbx",
            "04_Omelette_Pan.fbx", "05_Apple_Pan.fbx",
        };
        for (int i = 0; i < pans.Length; i++)
        {
            // 상판 윗면은 `1.03 + 0.08/2` = 1.07. `MyModel` 이 바운즈 최저점을 읽어 앉히니
            // 팬 원점이 바닥이 아니어도(이 모델들은 +0.015) 정확히 얹힌다.
            MyModel(t, "Assets/My blender/" + pans[i], $"InPot_{i}",
                    new Vector3(lineX, 1.07f, 4.4f - i * 2.2f), 1.0f);
        }

        // 위생 가림막 — 급식소에 반드시 있는 것
        Block(t, "InGuard", new Vector3(lineX + 1.1f, 1.75f, 0f), Quaternion.Euler(-18f, 0f, 0f),
              new Vector3(0.06f, 0.7f, d - 7.6f), ColWindow, noCollider: true);

        // 메뉴판 — 배식대 위 벽에
        Block(t, "InMenuBoard", new Vector3(-halfW - 0.05f, 2.9f, 0f), Quaternion.identity,
              new Vector3(0.12f, 1.6f, d - 8f), ColBearDark, noCollider: true);
        // ★ 2026-09-23 유저: *"밥·국·김치·반찬·후식 순서로 패널이 되어 있는데 <b>실제 급식
        // 순서가 각각 다르게</b> 되어 있다."* 맞다 — <b>메뉴판이 거꾸로 걸려 있었다.</b>
        //
        // 줄은 식판대(z +7.3)에서 반납대(z −7.5) 쪽으로 걷는다. 그런데 슬립은 `i * 1.5` 라
        // <b>0번(밥)이 z −3</b>, 즉 <b>줄의 끝</b>에 붙어 있었다. 걸어가면서 「후식 → 반찬 →
        // 김치 → 국 → 밥」 을 읽게 되니 팬 순서와 정반대다.
        // 이제 `−i * 1.5` — 밥이 z +3 으로 <b>줄의 시작</b>에 온다.
        for (int i = -2; i <= 2; i++)
            Block(t, $"InMenuSlip_{i + 2}", new Vector3(-halfW + 0.05f, 3.3f, -i * 1.5f),
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

        BapMadangExhibits(t, halfW, halfD);
    }

    /// <summary>
    /// <b>유저가 만든 곰밥마당 전시 소품 열 점</b>(2026-09-23). 합쳐서 5,488쿼드(10,976 tris) ·
    /// 전부 100% 쿼드 · 공용 아틀라스 하나 — §7.6 예산(150~250k)에 티도 안 난다.
    ///
    /// <b>자리는 «뒷벽 한 줄» 로 잡았다.</b> 큰 것 다섯을 흩뿌리면 급식실이 창고가 되는데,
    /// 한 줄로 세우면 <b>전시 동선</b>이 생긴다 — 밥 받는 줄(왼쪽 벽)과 자리(가운데·오른쪽)는
    /// 이미 있고, 뒷벽만 통째로 비어 있었다. 걸어 들어와 왼쪽으로 돌면 배식, 안쪽으로
    /// 들어가면 전시. <b>한 방에 두 동선이 겹치지 않는다.</b>
    ///
    /// 중간 소품 넷은 <b>제 일이 있는 자리</b>에 붙인다 — 잔반 저울과 완식 도장대는 반납대 옆,
    /// 당번 뽑기통은 식판 쌓아둔 줄 머리, 먹거리 표본은 오른쪽 벽. 전시물을 동선과 상관없는
    /// 데 놓으면 «장식» 이 되고, 쓰는 자리에 놓으면 «설비» 가 된다.
    ///
    /// ★ <b>풍경만 매단다.</b> `M05` 는 바닥이 y +0.08 이라 다른 아홉과 달리 원점이 바닥이
    /// 아니다 — 원래 <b>매다는 물건</b>이라 그게 맞다. 좌식 평상 둘 사이 눈높이 위에 건다.
    /// </summary>
    void BapMadangExhibits(Transform t, float halfW, float halfD)
    {
        const string Dir = "Assets/My blender/";

        // 뒷벽 안쪽 면. <see cref="Hollow"/> 의 벽은 0.6m 두께다(2026-09-21 화장실에서 배운 것) —
        // `halfD` 로 잡으면 소품이 벽 속에 절반 묻힌다.
        float back = -halfD + 0.6f;

        // 소품마다 깊이가 달라서 <b>제 깊이의 절반만큼</b> 앞으로 내야 벽에 딱 붙는다.
        void Wall(string file, string name, float x, float depth, float width)
            => Put(file, name, new Vector3(x, 0f, back + depth * 0.5f), width);

        void Put(string file, string name, Vector3 at, float width, float yaw = 0f)
        {
            var m = MyModel(t, Dir + file, name, at, width);
            if (m == null) return;
            // 덮어쓰지 않고 곱한다 — 임포트 축 회전을 지우면 모델이 눕는다(2026-09-21 곰).
            if (Mathf.Abs(yaw) > 0.01f) m.localRotation = Quaternion.Euler(0f, yaw, 0f) * m.localRotation;
        }

        // ── 뒷벽 전시 줄 ──  왼쪽 반납대(x −12.9까지)를 피해 x −11 부터, 평상(x 10.6~)까지.
        // 2026-09-23 유저: *"전반적으로 에셋 크기를 조금씩 키워 달라."* <b>×1.15</b> —
        // 방이 30 × 19 × 9m 라 실물 크기는 작게 읽힌다. 폭 합계 11.57 → 13.33m 인데
        // 가용 24m 라 간격이 1.78m 씩 남는다. 제일 높은 도구 전시벽이 3.25m(서까래 8.25).
        Wall("L01_Onggi_Terrace.fbx",      "ExOnggiTerrace", -7.7f, 1.738f, 3.050f);
        Wall("L02_Meju_Drying_Rack.fbx",   "ExMejuRack",     -2.9f, 0.711f, 2.900f);
        Wall("L03_Grain_Dwiju.fbx",        "ExGrainDwiju",    1.4f, 1.174f, 2.280f);
        Wall("L04_Giant_Tool_Exhibit.fbx", "ExToolWall",      5.6f, 0.763f, 2.600f);
        Wall("L05_Mill_Mortar_Exhibit.fbx","ExMillMortar",   10.0f, 1.134f, 2.500f);

        // ── 반납대 옆: 먹고 난 뒤에 하는 일 ──  잔반을 재고, 다 먹었으면 도장을 찍는다.
        Put("M01_Leftover_Scale.fbx",    "ExLeftoverScale", new Vector3(-11.2f, 0f, -7.9f), 1.000f, 90f);
        Put("M03_Clean_Plate_Stamp.fbx", "ExCleanStamp",    new Vector3(-11.2f, 0f, -5.3f), 0.900f, 90f);

        // ── 줄 머리: 받기 전에 하는 일 ──  오늘 배식 당번을 뽑는다.
        // ★ 2026-09-23 유저: *"배식 당번 뽑기통이 나무상자에 가려 안 보인다."*
        // 재 보니 <b>방향이 아니라 자리</b>였다 — z 7.4 는 문(z 8.9)에서 들어와 서는 자리보다
        // <b>뒤</b>라서 등 뒤에 있었고, 식판대(나무 상자, x −13.8 z 7.3)와 같은 줄이라 묻혔다.
        // <b>통로 쪽으로 당긴다</b>: 들어오면서 왼쪽을 보면 바로 걸린다. 크기도 ×1.4.
        Put("M02_Duty_Lottery.fbx", "ExDutyLottery", new Vector3(-10.8f, 0f, 5.6f), 1.350f, 90f);

        // ── 오른쪽 벽: 곰이 뭘 먹는지 ──
        // ★ 유저: *"대나무 들어 있는 에셋의 방향이 반대."* 맞다 — 이 열 점 중
        // <b>이것만 블렌더 기준 앞면이 +Y</b> 고 나머지 아홉은 −Y 다(측정).
        // 같은 묶음이라고 같은 방향일 거라 믿고 같은 yaw 를 준 게 잘못이야. 180° 돌린다.
        // 크기도 ×1.5 — 열린 앞면에 내용물이 있는 전시물이라 작으면 안이 안 보인다.
        Put("M04_Bear_Diet_Case.fbx", "ExDietCase",
            new Vector3(halfW - 0.6f - 0.35f, 0f, 0f), 1.600f, 270f);

        // ── 평상 둘 사이에 매단 풍경 ──  y 를 주면 <b>바닥이 그 높이</b>에 온다.
        // 2.35 + 1.53 = 3.88m 라 서까래(8.25)에는 안 닿고 앉은 사람 머리 위로 지나간다.
        Put("M05_Bear_Fish_Windchime.fbx", "ExWindchime", new Vector3(12.4f, 2.35f, 0f), 0.450f);
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
                // 2026-09-18 유저: *"학생회실이 나무판자로 가려져 있네."* 판자가 아니라
                // <b>게시판이 문 정면 2.6m 앞</b>에 폭 w×0.7 로 서 있었다 — 문을 통째로 가린다.
                // <b>문 앞은 비운다.</b> 나무를 17m 밖으로 물린 것과 같은 규칙이야.
                Block(t, "Board", new Vector3(-w * 0.34f, 1.9f, front + 2.6f), Quaternion.identity,
                      new Vector3(w * 0.42f, 3.4f, 0.24f), ColWood, noCollider: true);
                Block(t, "BoardFace", new Vector3(-w * 0.34f, 1.9f, front + 2.75f), Quaternion.identity,
                      new Vector3(w * 0.38f, 3f, 0.06f), ColCream, noCollider: true);
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
                // ★ 지붕 위 간판 그릇. 2026-09-18 유저가 블렌더로 만들어 온 것 —
                // *"지붕 위에 보기 좋게 배치해 줘. 오른쪽에는 국밥 그릇 넣을 거니까 왼쪽에 밥그릇."*
                //
                // 지붕 위에 실물 그릇을 얹는 건 <b>한국 식당 간판의 실제 문법</b>이야
                // (통닭집 닭 모형, 국밥집 뚝배기). 급식소를 한눈에 알아보게 만드는 데
                // 글자 간판보다 낫고, 유저가 만든 모델을 제일 잘 보이는 자리에 쓰는 거다.
                //
                // <b>받침대는 뺐다</b>(2026-09-18 유저: *"밥상 받침대 제거하고 크게 배치해줘"*).
                // 처음에 깐 이유는 "그릇만 있으면 떠 보인다" 였는데, 이 지붕은 경사면이 아니라
                // <b>납작한 판 세 장</b>(Eaves·Roof·Ridge)이라 능선 위가 그냥 평평하다 —
                // 받침대가 없어도 그릇이 바닥에 닿는다. 밥상만 하나 더 떠 있는 꼴이었어.
                {
                    // ★ 지붕 <b>가운데</b>에 놓는다. 처음에 `front − 1.4`(앞쪽 끝)에 뒀더니
                    // 처마가 폭+4.5 로 튀어나와서 <b>그릇이 처마 밑에 가렸다</b> —
                    // 유저: "위에 내 밥그릇 에셋이 없어." 지붕 능선은 h + 0.55 쯤이야.
                    // 측정해서 정했다. 곰밥마당 h=9 인데 <b>지붕 윗면이 y 10.95</b> 였다 —
                    // 능선(h+0.55)이 아니라 기와 층까지 쌓인 값이야. h+0.9(9.9)에 뒀더니
                    // 그릇 바닥이 10.16 으로 <b>지붕에 0.8m 묻혔다.</b>
                    // 지붕을 눈으로 어림하지 말고 <b>렌더러 최고점을 읽어라.</b>
                    // <b>능선 윗면에 바로 앉힌다.</b> 0.06 만 묻어서 접지선을 만든다 —
                    // 딱 0 에 맞추면 z-파이팅 위험이 있고, 조금 묻히면 "놓인" 걸로 읽힌다.
                    float bowlY = RoofTopLocal(t, h) - 0.06f;
                    float bowlZ = 0f;           // 건물 앞뒤 한가운데
                    // 유저의 기준(2026-09-18): *"면수는 적게, 에셋 크기는 크게. 멀리서 봐도
                    // 솥뚜껑이랑 숟가락이 보여서 그 건물이 뭐 하는 데인지 알았으면."*
                    // 크게 만드는 값은 `BowlSize`, 싸게 만드는 값은 에셋의 면 수 —
                    // <b>둘은 서로 안 묶여 있다.</b> 그래서 둘 다 극단으로 갈 수 있다.
                    const float BowlSize = 10.0f;    // 폭
                    const float BowlTall = 8.5f;     // 키 상한
                    float bowlX = -6.5f;             // <b>왼쪽</b> — 오른쪽은 솥뚜껑 자리

                    // ★ <b>바운딩박스를 능선 안에 가두려던 걸 그만뒀다.</b> 그 규칙으로는
                    // 7m 가 한계였는데(능선 폭 `w × 0.7` = ±10.5), 유저가 원하는 건
                    // <b>멀리서 봐도 뭐 하는 건물인지 아는 것</b>이라 크기가 먼저다.
                    //
                    // 실제로 떠 보이는지는 <b>바운딩박스가 아니라 접지면</b>이 정한다:
                    // 밥그릇은 굽(바닥)이 지름의 40% 남짓이라 중심 ±6.5 면 굽이 4.5~8.5 에
                    // 얹혀 능선(±10.5) 한가운데다. 밖으로 나가는 건 <b>위로 벌어진 전</b>뿐이고
                    // 그 밑에는 `Roof` 판이 0.3m 아래·±15.8 까지 깔려 있어 하늘이 안 보인다.
                    //
                    // 결과: 밥그릇 10.0 × 2.63 × 10.0, 솥뚜껑 8.36 × 8.50 × 9.33,
                    // 둘 사이 3.8m. 전보다 <b>43% 크다.</b>
                    MyModel(t, "Assets/My blender/Rice_bowl.fbx", "RiceBowl",
                            new Vector3(bowlX, bowlY, bowlZ), BowlSize, BowlTall);

                    // 오른쪽 — <b>솥뚜껑 + 쇠숟가락</b>(2026-09-18 유저가 만든 두 번째 모델).
                    // 파일이 없거나 <b>기하가 비어 있으면</b> 아무 것도 안 놓는다(`MyModel` 이 판단).
                    MyModel(t, "Assets/My blender/pot lid_PLUS IRON_spoon_2fbx.fbx", "PotLidSpoon",
                            new Vector3(-bowlX, bowlY, bowlZ), BowlSize, BowlTall);
                }
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
