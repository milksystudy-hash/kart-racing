using System.Collections.Generic;
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
        storySign = StorySignSeed;   // 묶음 방향 — 실측값에서 시작해서 더 센 신호가 나오면 바뀐다

        // 상징물 방향은 <b>이번 빌드에서 다시 잰다</b> — 모델을 갈아끼웠을 수 있다
        pendingEmblems.Clear();
        emblemSign = 0;

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

        // 지붕 상징물은 <b>전부 올려놓고 한 번에</b> 돌린다 — 아래 설명 참고
        OrientRoofEmblems();

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
        RoofEmblem(t, name, width, height);

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
    const string RoofFolder = "Assets/My blender/RoofObjects/";

    /// <summary>
    /// 캠퍼스 지붕 <b>반자널 아랫면</b>(<see cref="BuildRoof"/>). 지붕 위에 뭘 올릴 때
    /// 여기에 닿으면 안 된다 — <b>같은 숫자를 두 군데 적으면 반드시 어긋난다.</b>
    /// </summary>
    public const float CampusCeilingY = 26f;

    /// <summary>
    /// 건물 이름 → 지붕에 올릴 상징물(2026-09-28, 유저가 만든 열 점 · 7,304쿼드 · 100% 쿼드).
    ///
    /// <b>글자 없는 조형물</b>이라 멀리서도 «저 건물이 뭐 하는 데인지» 가 읽힌다 —
    /// 현판은 가까이 가야 읽히고, 지붕 위 물건은 <b>코스를 달리면서도 보인다.</b>
    /// 곰밥마당 지붕에 밥그릇·솥뚜껑을 올린 것과 같은 문법이고(2026-09-18),
    /// 그래서 곰밥마당은 이 표에 없다 — 이미 둘이 올라가 있다.
    /// </summary>
    static readonly (string hall, string file)[] RoofEmblems =
    {
        ("곰솥관",       "01_Gomsot_Baking"),              // 반죽 볼 + 밀대 + 밀 이삭
        ("웅지관",       "02_Leadership_Stamp"),           // 결재 도장 + 서류 + 동전
        ("화장실",       "03_Restroom_Toilet"),            // 곰 귀 변기 + 두루마리 휴지
        ("곰짝박수마당", "04_Applause_Paws"),              // 곰발 + 행사 북
        ("곰테크관",     "05_Tech_Gear"),                  // 기어 + 카트 바퀴 + 스패너
        ("대충기념관",   "06_Memorial_Bust"),              // 곰 흉상 + 역사책
        ("재주관",       "07_Arts_Palette"),               // 팔레트 + 붓 + 음표
        ("참잘했어요관", "08_Awards_Trophy"),              // 곰발 트로피 + 시상대
        ("곰누리관",     "09_International_Globe"),        // 지구본 + 여행 가방 + 비행기
        ("곰생회관",     "10_Student_Council_Megaphone"),  // 확성기 + 말풍선
        // 11 은 건물이 아니라 <b>복귀 문간채</b>(중앙홀로 가는 문)라
        // <see cref="CampusSceneBuilder"/> 가 직접 얹는다 — 그 문간채만 Hanok 이 아니다.
        ("웅성관",       "12_Ungseong_Broadcast"),         // 방송 마이크 + 카메라
        ("곰머리관",     "13_Gommeori_Book"),              // 안경 쓴 곰 + 책
        ("곰손관",       "14_Gomson_Sewing"),              // 실타래 + 바늘 + 곰인형
        ("철곰관",       "15_Cheolgom_Safety"),            // 곰발 방패 + 안전모
    };

    /// <summary>
    /// <b>앞면에만 있는 부품.</b> ★ 뒤쪽 부품을 넣으면 열 개가 통째로 거꾸로 선다 —
    /// 변기 <c>Lid</c>(+0.63)는 물탱크 쪽이고 팔레트 <c>Brush</c>(+0.15)는 뒤에 꽂혀 있다.
    /// 여기 넣은 넷은 전부 <b>앞에 놓인 것</b>이다: 비행기 · 역사책 · 스패너 · 휴지.
    /// </summary>
    static readonly string[] RoofFrontMarks =
    {
        "Airplane", "History_Book", "Spanner", "Paper_Roll",   // 01~10
        "Open_Book", "Teddy", "Kart",                          // 11~15
    };

    static readonly List<Transform> pendingEmblems = new List<Transform>();

    /// <summary>0 = 아직 안 쟀다 · +1 = 모델의 +Z 가 앞 · −1 = −Z 가 앞.</summary>
    static int emblemSign;

    /// <summary>
    /// 지붕 능선 한가운데에 상징물 하나. <b>크기는 건물에서 뽑는다</b> —
    /// 폭 <c>w × 0.45</c> · 키 상한 <c>h × 1.05</c>(그리고 캠퍼스 천장). 손으로 열 개를 적으면
    /// 모델을 새로 뽑을 때 또 틀린다(밥그릇에서 배운 것).
    ///
    /// 유저 기준은 <b>«멀리서 봐도 뭐 하는 건물인지 아는 것»</b>(2026-09-18)이라
    /// 크기가 먼저다 — 곰밥마당 그릇이 30m 건물에 폭 10m · 키 8.5m 니 같은 결이야.
    ///
    /// 높이는 <see cref="RoofTopLocal"/> 로 <b>읽는다.</b> <c>h + 0.9</c> 같은 어림값을 쓰면
    /// 처마·기와·능선이 층층이 쌓인 만큼 <b>지붕에 묻힌다</b>(2026-09-18에 0.8m 묻혔다).
    /// </summary>
    void RoofEmblem(Transform t, string name, float w, float h)
    {
        string file = null;
        foreach (var e in RoofEmblems)
            if (e.hall == name) { file = e.file; break; }
        if (file == null) return;

        // 능선 윗면에 바로 앉힌다. 0.06 만 묻어서 접지선을 만든다 —
        // 딱 0 이면 z-파이팅 위험이고, 조금 묻히면 «놓인» 걸로 읽힌다.
        float seat = RoofTopLocal(t, h) - 0.06f;

        // ★ <b>캠퍼스 지붕을 뚫으면 안 된다.</b> 웅지관은 능선이 y 15.25 라
        // 키 상한(h × 1.05 = 13.65)을 그대로 주면 꼭대기가 <b>26.7</b> 로
        // 반자널(26)을 0.7m 뚫고 나간다. 1m 를 남긴다.
        float room = CampusCeilingY - 1f - seat;

        AddRoofEmblem(t, file, new Vector3(0f, seat, 0f),
                      w * 0.45f, Mathf.Min(h * 1.05f, room));
    }

    /// <summary>
    /// 상징물 하나를 얹는다. <b>방향을 이미 알면 그 자리에서 돌리고, 모르면 줄을 세운다</b> —
    /// 그래서 <see cref="Build"/> 가 끝난 <b>뒤에</b> 얹는 것(복귀 문간채)도 제대로 선다.
    /// </summary>
    public static Transform AddRoofEmblem(Transform parent, string file, Vector3 at,
                                          float width, float maxHeight)
    {
        var m = MyModel(parent, RoofFolder + file + ".fbx", "RoofEmblem", at, width, maxHeight);
        if (m == null) return null;

        if (emblemSign != 0) Face(m);
        else pendingEmblems.Add(m);
        return m;
    }

    static void Face(Transform m)
    {
        if (emblemSign < 0)
            m.localRotation = Quaternion.Euler(0f, 180f, 0f) * m.localRotation;
    }

    /// <summary>
    /// 열 개의 <b>앞면을 한 번에</b> 정한다.
    ///
    /// ★ 하나씩 물어보면 안 된다. 열 점 중 <b>앞쪽 부품이 있는 건 넷뿐</b>이고
    /// (비행기 · 역사책 · 스패너 · 휴지) 나머지 여섯은 신호가 0 이라 안 돌아간다 —
    /// 그러면 <b>여섯 개가 등을 돌리고 선다.</b> 게다가 건물을 짓는 순서에 따라
    /// 먼저 걸리는 모델이 달라져서 <b>돌릴 때마다 결과가 바뀐다.</b>
    ///
    /// 한 블렌드 파일에서 같은 설정으로 나왔으니 <b>방향은 열 개가 같다.</b>
    /// 전부 올려놓고 <b>제일 뚜렷한 신호 하나</b>로 정해서 열 개에 같이 적용한다 —
    /// 철곰관·웅지관 묶음과 같은 방식이고, «블렌더에서 이랬으니 유니티도 이럴 것» 은
    /// 이 프로젝트에서 <b>두 번 틀렸다</b>(곰이 누움 · 스피커가 뒤돎).
    /// </summary>
    void OrientRoofEmblems()
    {
        float best = 0f;
        foreach (var m in pendingEmblems)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, RoofFrontMarks);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }

        if (Mathf.Abs(best) < 0.01f)
        {
            Debug.LogWarning("[지붕] 상징물 앞면을 못 쟀다 — 부품 이름(Airplane·Open_Book…)이 " +
                             "바뀌었는지 확인해라. 일단 안 돌리고 세운다.");
            pendingEmblems.Clear();
            return;
        }

        // 신호가 음수면 모델의 −Z 가 앞이다 → 180 도 돌려 <b>문 쪽(+Z)</b>을 보게 한다
        emblemSign = best < 0f ? -1 : 1;
        foreach (var m in pendingEmblems)
            if (m != null) Face(m);

        Debug.Log($"[지붕] 상징물 {pendingEmblems.Count}개 · 앞면 신호 {best:+0.000;-0.000}m → " +
                  $"{(emblemSign < 0 ? "180도 돌려" : "그대로")} 세웠다");
        pendingEmblems.Clear();
    }

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
#if UNITY_EDITOR
    static System.Collections.Generic.Dictionary<string, string> assetByName;

    /// <summary>
    /// ★★ <b>파일이 적힌 자리에 없으면 이름으로 찾아낸다.</b>
    ///
    /// 수연이 소품을 폴더로 묶으면 코드에 적힌 경로가 전부 어긋난다. 그때 <see cref="MyModel"/>
    /// 은 <b>조용히 아무 것도 안 만들고</b>, 화면에서는 «소품이 통째로 사라진» 것으로 보인다 —
    /// 2026-10-07 에 곰밥마당 열다섯 · 로비 가구 일곱 · 지붕 그릇 둘 · 화장실 곰이 한꺼번에 그랬다.
    ///
    /// 그러니 <b>정리해도 안 깨지게</b> 둔다. 찾는 곳은 `My blender` 와 `NPC_bear` 두 폴더뿐이고,
    /// 이름이 같은 파일이 둘이면 <b>먼저 찾은 것</b>을 쓴다(이 프로젝트에 중복 이름은 없다).
    /// </summary>
    public static string ResolveAsset(string assetPath)
    {
        if (System.IO.File.Exists(assetPath)) return assetPath;

        string want = System.IO.Path.GetFileName(assetPath);

        // 못 찾을 때만 다시 훑는다 — 있으면 캐시가 그대로 듣는다.
        if (assetByName == null || !assetByName.ContainsKey(want))
        {
            assetByName = new System.Collections.Generic.Dictionary<string, string>();
            // ★ 2026-10-08 — <b>«My blender» 밖에 넣어도 찾는다.</b> 참잘했어요관 소품이
            //   `Assets/Awards_Hall_/` 로 왔다. 폴더를 하나씩 적으면 새 묶음이 올 때마다 또 고쳐야 해서,
            //   <b>Assets 를 통째로 훑는다</b> — 150개 남짓이라 한 번 도는 비용이 공짜다.
            foreach (var dir in new[] { "Assets" })
            {
                if (!System.IO.Directory.Exists(dir)) continue;
                foreach (var f in System.IO.Directory.GetFiles(dir, "*.fbx",
                                                               System.IO.SearchOption.AllDirectories))
                {
                    string got = f.Replace('\\', '/');
                    assetByName[System.IO.Path.GetFileName(got)] = got;
                }
            }
        }

        if (assetByName.TryGetValue(want, out var found))
        {
            Debug.Log($"[모델] '{want}' 는 {found} 에 있다 (적힌 자리: {assetPath}).");
            return found;
        }
        return assetPath;
    }
#endif

    public static Transform MyModel(Transform parent, string assetPath, string name, Vector3 at, float targetWidth,
                      float maxHeight = 0f)
    {
#if UNITY_EDITOR
        // ★★ 2026-10-07 — <b>폴더를 정리하면 적어 둔 경로가 전부 null 이 된다.</b>
        // 수연이 곰밥마당 소품 열다섯을 `My blender/Gombap/` 으로 묶자 급식실 소품이 통째로
        // 사라졌고, 로비 가구·스피커·지붕 그릇·화장실 곰도 같이 날아갔다 — 전부 같은 병이다.
        //
        // 경로를 하나씩 고쳐 적는 건 <b>다음에 또 정리하면 또 깨진다.</b>
        // <b>이름으로 찾게</b> 두면 어디로 옮겨도 알아서 따라온다.
        assetPath = ResolveAsset(assetPath);

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
                // 방이 씨름판에서 <b>안전 체험 훈련장</b>으로 바뀌었다(2026-09-28).
                // <b>간판과 방이 다른 말을 하면</b> 「준비 중」이 아니라 「고장」으로 읽힌다.
                title = "안전 점검 훈련"; blurb = "철거 사유를 하나씩 지운다"; ready = false; break;
            case "재주관":
                // 2026-09-29 — 유저 설비 열 점이 들어오면서 이젤이 진짜 화판이 됐다.
                // 세 번째 미니게임 자리.
                title = "따라 그리기"; blurb = "마우스로 윤곽을 덧그린다"; ready = false; break;
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
    const string CheolgomFolder = "Assets/My blender/Cheolgom/";

    /// <summary>
    /// 철곰관 소품 — 파일 · 오브젝트 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> · 실제 폭(m).
    ///
    /// 방향은 방 기준이다: <b>0 = 문 쪽(+Z) · 180 = 뒷벽 · 90 = +X · 270 = −X.</b>
    /// 폭은 재 온 실측값이라 그대로 넘기면 배율이 1.0 이 된다 —
    /// 숫자를 적어 두는 게 <c>Lcl Scaling = 100</c> 함정에도 안전하다(2026-09-22).
    /// </summary>
    static readonly (string file, string label, float x, float z, float yaw, float width)[]
        CheolgomLayout =
    {
        // 앞벽 — 들어오자마자 보이는 «관제·장비» 줄. 문 앞(|x| < 2.2)은 비운다
        ("01_Security_Console",        "관제콘솔",   -5.5f,  5.35f, 180f, 2.88f),
        ("02_Protective_Gear_Rack",    "보호장비",    5.4f,  5.40f, 180f, 2.37f),
        // 왼쪽 벽 — 훈련 설비
        ("04_Reaction_Training_Wall",  "반응훈련벽", -8.3f,  4.10f,  90f, 2.73f),
        ("06_Rescue_Equipment_Station","구조장비",   -8.4f,  0.60f,  90f, 2.61f),
        ("10_Crash_Mat_Trolley",       "매트운반대", -8.2f, -2.90f,  90f, 2.36f),
        // 오른쪽 벽
        ("05_Mobile_Safety_Barrier",   "안전펜스",    8.4f,  3.60f, 270f, 3.30f),
        ("07_Rope_Rescue_Frame",       "로프구조",    8.0f,  0.00f, 270f, 2.61f),
        // 가운데 — 체험 동선은 균형대 → 대피터널 순서다. 그 뒤가 더미
        ("08_Balance_Walkway",         "균형보행",   -3.2f, -3.40f,   0f, 3.11f),
        ("09_Low_Crawl_Course",        "대피터널",    0.9f, -3.40f,   0f, 1.48f),
        ("03_Bear_Training_Dummies",   "훈련더미",    5.0f, -4.60f,   0f, 2.31f),
    };

    /// <summary>
    /// 철곰관 — 경호·체육·안전. 유저가 만든 대형 소품 열 점(<b>7,175쿼드 · 100% 쿼드 ·
    /// 바닥 원점 · 실물 크기</b>)이 방의 뼈대고, 코드는 <b>바닥 선과 마감만</b> 얹는다.
    ///
    /// 자리는 세 줄이다 — <b>앞벽(관제·장비) · 양옆 벽(훈련 설비) · 가운데(체험 동선)</b>.
    /// 큰 것을 흩뿌리면 체육관이 창고가 되는데, 벽에 붙이고 가운데를 비우면
    /// <b>지나가는 길</b>이 생긴다(곰밥마당 배식 줄과 같은 판단).
    /// 문 앞(|x| &lt; 2.2 · z &gt; 4.4)과 미니게임 입간판 자리(0, 2.5)는 비운다.
    /// </summary>
    void Cheolgom(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;   // Hollow 벽은 0.6m 두께다

        // ---- 유저 소품 열 점 ----
        var placed = new Transform[CheolgomLayout.Length];
        int made = 0;
        for (int i = 0; i < CheolgomLayout.Length; i++)
        {
            var p = CheolgomLayout[i];
            placed[i] = MyModel(t, CheolgomFolder + p.file + ".fbx", "In_" + p.label,
                                new Vector3(p.x, 0f, p.z), p.width);
            if (placed[i] != null) made++;
        }

        // ★★ <b>앞면은 짐작하지 않고 씬에서 잰다.</b> 블렌더 기준 앞면이 −Y 인 건 헤드리스로
        // 확인했지만(Target·Equipment·Cabinet_Door 여섯 점의 부품 자리), <b>블렌더 축이
        // 유니티에서 어느 쪽이 되는지는 이 프로젝트에서 두 번 틀렸다</b> —
        // 곰이 누웠고(2026-09-21) 스피커가 뒤돌아 섰다(2026-09-22).
        //
        // 열 점이 한 블렌드 파일에서 같은 설정으로 나왔으니 <b>방향은 열 개가 같다.</b>
        // 그래서 <b>제일 뚜렷한 신호 하나</b>로 정하고 열 개에 같이 적용한다 —
        // 신호가 약한 모델(반응벽 타겟은 벽 두께의 절반밖에 안 튀어나온다)에 각자
        // 물어보면 그 하나만 거꾸로 설 수 있다.
        float best = 0f;
        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }
        float extra = best >= 0f ? 0f : 180f;

        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, CheolgomLayout[i].yaw + extra, 0f) * placed[i].localRotation;

        if (Mathf.Abs(best) < 0.01f)
            Debug.LogWarning("[철곰관] 소품 앞면을 못 쟀다 — 부품 이름(Target·Cabinet_Door…)이 " +
                             "바뀌었는지 확인해라. 일단 안 돌리고 세운다.");
        else
        {
            // ★ 여기서 정한 부호를 <b>같은 묶음의 다른 방</b>이 그대로 쓴다 — 기념관 전시물은
            // 여덟이 통짜라 스스로 잴 신호가 없다(<see cref="Memorial"/> 참고).
            propFrontExtra = extra;
            propFrontKnown = true;
            Debug.Log($"[철곰관] 소품 {made}/10 · 앞면 신호 {best:+0.000;-0.000}m → " +
                      $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");
        }

        // ---- 바닥 체험 동선 ----
        // 코스가 바닥에 그려져 있어야 «지나가는 길» 이 된다. 선이 없으면 큰 물건 열 개가
        // 그냥 놓여 있는 방이야. 바닥 윗면이 y 0 이라 <b>2mm 띄워서</b> 깐다(지지직 방지).
        Block(t, "InLaneMain", new Vector3(-1.0f, 0.012f, -3.4f), Quaternion.identity,
              new Vector3(11.0f, 0.02f, 0.16f), ColMapleGold, noCollider: true);
        for (int i = 0; i < 10; i++)
            Block(t, $"InLaneDash_{i}", new Vector3(-6.4f + i * 1.45f, 0.012f, 1.4f),
                  Quaternion.identity, new Vector3(0.85f, 0.02f, 0.12f), ColCream, noCollider: true);

        // 삼각콘 — 「안전 체험」을 한 물건으로 말해 준다. 코스 양 끝에만 둔다
        float[] coneX = { -5.6f, -0.9f, 2.4f, 6.4f };
        for (int i = 0; i < coneX.Length; i++)
        {
            Block(t, $"InConeFoot_{i}", new Vector3(coneX[i], 0.04f, -1.5f), Quaternion.identity,
                  new Vector3(0.36f, 0.08f, 0.36f), ColMapleGold, noCollider: true);
            Block(t, $"InConeBody_{i}", new Vector3(coneX[i], 0.3f, -1.5f), Quaternion.identity,
                  new Vector3(0.16f, 0.52f, 0.16f), ColMapleGold, noCollider: true);
            Block(t, $"InConeBand_{i}", new Vector3(coneX[i], 0.38f, -1.5f), Quaternion.identity,
                  new Vector3(0.19f, 0.10f, 0.19f), ColCream, noCollider: true);
        }

        // ---- 뒷벽 — 안전 수칙과 기록판 ----
        // 더미가 x 3.8~6.2 를 쓰니 그 왼쪽에 건다.
        Block(t, "InRuleFrame", new Vector3(-5.0f, 2.30f, -inZ + 0.10f), Quaternion.identity,
              new Vector3(3.4f, 2.0f, 0.06f), ColWood, noCollider: true);
        Block(t, "InRuleBoard", new Vector3(-5.0f, 2.30f, -inZ + 0.16f), Quaternion.identity,
              new Vector3(3.1f, 1.7f, 0.05f), ColCream, noCollider: true);
        for (int i = 0; i < 4; i++)
            Block(t, $"InRuleLine_{i}", new Vector3(-5.0f, 2.85f - i * 0.36f, -inZ + 0.20f),
                  Quaternion.identity, new Vector3(2.5f, 0.11f, 0.03f),
                  i == 0 ? ColRibbon : ColTrimDark, noCollider: true);

        Block(t, "InRecordFrame", new Vector3(-0.2f, 2.30f, -inZ + 0.10f), Quaternion.identity,
              new Vector3(2.6f, 1.6f, 0.06f), ColWood, noCollider: true);
        Block(t, "InRecordBoard", new Vector3(-0.2f, 2.30f, -inZ + 0.16f), Quaternion.identity,
              new Vector3(2.3f, 1.3f, 0.05f), ColBearDark, noCollider: true);
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"InRecordSlot_{i}", new Vector3(-0.2f + i * 0.55f, 2.25f, -inZ + 0.20f),
                  Quaternion.identity, new Vector3(0.9f, 0.8f, 0.03f), ColCream, noCollider: true);

        // ---- 마감 — 레고 느낌을 빼는 셋 중 둘(모서리 기둥 · 굽도리) ----
        // 8m 짜리 체육관은 <b>천장을 안 내린다</b> — 체육관은 원래 높고, 여기서 반자를
        // 내리면 대형 소품이 갇혀 보인다(곰손관과 반대 판단).
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.13f), h * 0.5f, sz * (inZ - 0.13f)),
                      Quaternion.identity, new Vector3(0.26f, h, 0.26f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.45f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.90f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.45f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.90f, 0.10f), ColWallFoot, noCollider: true);

        // 천장 트러스 — 8m 민짜 천장은 «아직 안 지은 방» 으로 보인다
        for (int i = 0; i < 7; i++)
            Block(t, $"InTruss_{i}", new Vector3(0f, h - 0.30f, -4.5f + i * 1.5f), Quaternion.identity,
                  new Vector3(w - 1.6f, 0.20f, 0.16f), ColWood, noCollider: true);
        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InTrussBeam_{sx}", new Vector3(sx * 4.2f, h - 0.52f, 0f), Quaternion.identity,
                  new Vector3(0.28f, 0.26f, d - 1.6f), ColWoodRail, noCollider: true);
    }

    const string MemorialFolder = "Assets/My blender/Memorial/";

    /// <summary>
    /// <b>전시물을 실물보다 이만큼 키운다</b>(2026-09-29 유저: *"10개 에셋 크기 키워서
    /// 배치해줘"*). 원본은 폭 1.5~4.3m 의 «실물 크기» 전시물인데, 천장 7m 짜리 방에 실물
    /// 크기로 놓으면 <b>전시물이 아니라 가구</b>로 보인다. 기념관은 물건이 사람보다 커야
    /// «기념» 이 된다 — 방이 15 × 11m 라 1.25 배가 서로 안 부딪히는 한계다.
    /// </summary>
    const float MemorialScale = 1.25f;

    /// <summary>
    /// 대충기념관 전시물 — 파일 · 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> ·
    /// <b>실측 폭 · 실측 깊이</b>(m, 애셋 README 표 그대로).
    ///
    /// ★★ <b>«넣을 폭» 을 그대로 적으면 안 된다.</b> <see cref="MyModel"/> 은 폭을
    /// <c>max(월드 bounds.x, 월드 bounds.z)</c> 로 재는데, 그 시점의 월드 AABB 에는
    /// <b>건물의 yaw 가 이미 섞여 있다.</b> 기념관은 yaw 300 이라 같은 숫자를 넣어도
    /// 실제 배율이 <b>1.01 ~ 1.30 으로 흩어진다</b> — 배치모드에서 재서 잡았다:
    ///
    /// <code>흉상(1.55×1.15) → 잰 폭 1.92 → 1.01배 · 병풍(4.26×0.85) → 4.11 → 1.30배</code>
    ///
    /// 그래서 <b>실측값을 적어 두고 배율을 여기서 거꾸로 계산</b>한다(<see cref="Memorial"/>).
    /// <c>MyModel</c> 을 고치면 철곰관·웅지관까지 다 움직이니 건드리지 않는다.
    /// </summary>
    static readonly (string file, string label, float x, float z, float yaw, float w, float d)[]
        MemorialLayout =
    {
        // 뒷벽 — 들어오면 정면으로 보이는 «역사» 줄
        ("09_Portrait_Wall",      "초상벽",    -4.1f, -4.19f,   0f, 3.46f, 1.06f),
        ("03_History_Screen",     "연혁병풍",   2.4f, -4.32f,   0f, 4.26f, 0.85f),
        // 왼쪽 벽 — 보관과 진열
        ("04_Archive_Cabinet",    "사료보관장", -6.15f, -1.5f,  90f, 3.02f, 1.12f),
        ("05_Relic_Showcase",     "유물전시장", -6.06f,  2.5f,  90f, 2.93f, 1.26f),
        // 오른쪽 벽 — 인물과 행사
        ("02_Memorial_Bust",      "기념흉상",   6.13f, -3.7f, 270f, 1.55f, 1.15f),
        ("08_Commemorative_Drum", "기념대북",   5.89f, -0.9f, 270f, 2.04f, 1.54f),
        ("10_Travel_Memories",    "여행가방",   5.92f,  2.5f, 270f, 2.40f, 1.49f),
        // 가운데 — 동선. 기록첩은 <b>문에서 제일 먼저 만나는</b> 자리에 둔다
        ("07_Paw_Monument",       "곰발기념비",  0f,   -1.4f,   0f, 1.75f, 0.94f),
        ("01_Footprints_Album",   "기록첩",    -2.6f,   2.2f,   0f, 2.15f, 1.38f),
        ("06_Museum_Diorama",     "배치모형",   2.6f,   2.0f,   0f, 2.90f, 1.92f),
    };

    /// <summary>
    /// 철곰관이 <b>씬에서 재서</b> 정한 소품 앞면 보정(0 또는 180). 같은 묶음에서 나온
    /// 다른 방이 그대로 쓴다 — 재기 어려운 쪽이 잰 쪽에 기대는 게, 각자 약한 신호로 짐작하다
    /// <b>한 점만 거꾸로 서는</b> 것보다 낫다.
    /// </summary>
    float propFrontExtra;
    bool propFrontKnown;

    /// <summary>
    /// 대충기념관 — 기념. 유저가 만든 전시물 열 점(<b>2,664쿼드 · 100% 쿼드 · 바닥 원점 ·
    /// 실물 크기 · 텍스처 없음</b>)이 방의 전부고, 코드는 <b>바닥 선과 마감만</b> 얹는다.
    ///
    /// 자리는 철곰관과 같은 세 줄 — <b>뒷벽(역사) · 양옆 벽(보관·인물) · 가운데(동선)</b>.
    /// 방이 15 × 11m 로 철곰관(19 × 13)보다 좁아서 가운데엔 셋만 놓고 길을 남긴다.
    ///
    /// ★ <b>「안 푼 상자」를 걷어냈다.</b> 이름값으로 상자 여섯과 천막을 놓아 뒀었는데,
    /// 진짜 전시물이 들어온 이상 상자는 <b>전시물을 가리는 짐</b>이다.
    /// 되살리고 싶으면 이 함수를 부르는 case 하나만 되돌리면 된다.
    /// </summary>
    void Memorial(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;

        // ★ <b>건물 yaw 를 되돌려 «잴 폭» 을 미리 구한다.</b> MyModel 이 보게 될 월드 AABB 는
        // 실측 (w × d) 상자를 건물 각도만큼 돌린 것이라, 그 값에 원하는 배율을 곱해 넘기면
        // <b>열 점이 전부 정확히 같은 배율</b>로 선다(위 MemorialLayout 주석 참고).
        float yaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(yaw)), sw = Mathf.Abs(Mathf.Sin(yaw));

        var placed = new Transform[MemorialLayout.Length];
        int made = 0;
        for (int i = 0; i < MemorialLayout.Length; i++)
        {
            var p = MemorialLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);

            placed[i] = MyModel(t, MemorialFolder + p.file + ".fbx", "In_" + p.label,
                                new Vector3(p.x, 0f, p.z), seen * MemorialScale);
            if (placed[i] != null) made++;
        }

        // ★★ <b>여기서는 앞면을 «잴 수가 없다».</b> 철곰관은 앞에만 있는 부품(Target ·
        // Cabinet_Door)이 여섯 점에 있어서 씬에서 재고 부호를 정했는데, 기념관은 열 점 중
        // <b>여덟이 통짜 `Structure` 한 덩이</b>다. 헤드리스로 재 보니 남은 둘도 신호가
        // 거의 없다(기록첩 페이지 +0.030 · 흉상 −0.077 — 둘 다 세로로만 치우쳐 있다).
        //
        // 그래서 <b>같은 빌드에서 철곰관이 이미 정한 부호를 그대로 쓴다.</b> 두 묶음은
        // 같은 블렌더(4.5.3 stable FBX IO) · 같은 설정 · 같은 생성 스크립트에서 나왔으니
        // 축 변환이 다를 수가 없다. 철곰관이 <b>먼저</b> 지어지는 것(건물 표에서 철곰관 6번째,
        // 대충기념관 10번째)에 기대는 코드라, 표 순서를 흔들면 아래 로그부터 봐라.
        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, MemorialLayout[i].yaw + propFrontExtra, 0f)
                    * placed[i].localRotation;

        Debug.Log($"[기념관] 전시물 {made}/10 · 앞면 보정 {propFrontExtra:0}도 "
                + (propFrontKnown ? "(철곰관이 잰 값)" : "★(철곰관이 못 쟀다 — 안 돌리고 세운다)"));

        // ---- 기록첩을 «넘겨 보는 책» 으로 ----
        // 컴포넌트는 여기서 붙어 씬에 구워지지만, <b>장수는 런타임에 Resources 에서 센다</b> —
        // 낙서를 넣고 빼는 것만으로는 씬을 다시 안 구워도 된다.
        for (int i = 0; i < placed.Length; i++)
        {
            if (placed[i] == null || MemorialLayout[i].label != "기록첩") continue;
            placed[i].gameObject.AddComponent<AlbumBook>();
        }

        // ---- 바닥 관람 동선 ----
        // 기념관은 «지나가며 보는» 방이라 길이 그려져 있어야 한다(철곰관 체험 동선과 같은 판단).
        Block(t, "InLaneMain", new Vector3(0f, 0.012f, 0.2f), Quaternion.identity,
              new Vector3(9.4f, 0.02f, 0.16f), ColMapleGold, noCollider: true);
        for (int i = 0; i < 8; i++)
            Block(t, $"InLaneDash_{i}", new Vector3(-4.9f + i * 1.4f, 0.012f, -2.6f),
                  Quaternion.identity, new Vector3(0.8f, 0.02f, 0.12f), ColCream, noCollider: true);

        // ---- 마감 — 레고 느낌을 빼는 셋(모서리 기둥 · 굽도리 · 반자) ----
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.13f), h * 0.5f, sz * (inZ - 0.13f)),
                      Quaternion.identity, new Vector3(0.24f, h, 0.24f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.42f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.84f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.42f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.84f, 0.10f), ColWallFoot, noCollider: true);

        // 기념관은 <b>천장을 내린다.</b> 7m 는 전시실로는 휑하고, 반자를 깔면 전시물이
        // 커 보인다 — 체육관(철곰관)과 <b>반대 판단</b>이다.
        Block(t, "InCeilPanel", new Vector3(0f, h - 1.5f, 0f), Quaternion.identity,
              new Vector3(w - 1.6f, 0.12f, d - 1.6f), ColCream, noCollider: true);
        for (int i = -1; i <= 1; i++)
            Block(t, $"InCeilBeam_{i}", new Vector3(i * 3.4f, h - 1.62f, 0f), Quaternion.identity,
                  new Vector3(0.22f, 0.16f, d - 1.6f), ColWood, noCollider: true);
    }

    const string JaejuFolder = "Assets/My blender/Jaeju/";

    /// <summary>재주관에서 <b>앞면에만</b> 있는 부품. 이젤의 화판과 작품벽의 그림 다섯.</summary>
    static readonly string[] JaejuMarks = { "Canvas", "Artwork" };

    /// <summary>
    /// 재주관 소품 — 파일 · 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> · 실측 폭 · 실측 깊이(m).
    ///
    /// ★ <b>배율은 1.0 이다.</b> 기념관은 1.25 배로 키웠는데 여기는 안 키운다 —
    /// 이 묶음은 처음부터 전시물이 아니라 <b>설비</b> 크기로 왔다(무대 4.8m · 작품벽 7.2m ·
    /// 이젤 2.83m). 방도 21 × 13m 로 넓어서 실물 크기로 세워야 <b>지나다닐 길</b>이 남는다.
    ///
    /// <b>이젤이 이 방의 주인공</b>이다(따라 그리기 미니게임의 화판). 가운데 앞쪽에 한 대만
    /// 세운다 — 세 대를 세우면 어느 것이 게임판인지 알 수가 없다.
    /// </summary>
    static readonly (string file, string label, float x, float z, float yaw, float w, float d)[]
        JaejuLayout =
    {
        // 뒷벽 — 전시와 무대
        ("02_Five_Artwork_Wall",     "작품벽",   -5.5f, -5.20f,   0f, 7.22f, 1.12f),
        ("05_Performance_Stage",     "공연무대",  5.5f, -4.40f,   0f, 4.80f, 2.55f),
        // 왼쪽 벽 — 작업과 전통
        ("08_Artwork_Drying_Rack",   "건조대",   -9.30f, -1.0f,  90f, 2.49f, 1.12f),
        ("10_Gayageum_Display",      "가야금",   -9.40f,  3.2f,  90f, 3.14f, 0.96f),
        // 오른쪽 벽 — 소리
        ("04_Upright_Piano",         "피아노",    8.80f, -0.6f, 270f, 2.65f, 2.11f),
        ("07_Sound_Desk",            "음향탁자",  9.20f,  3.2f, 270f, 2.40f, 1.33f),
        // 가운데 — 이젤이 주인공, 나머지는 그 둘레
        ("01_Art_Easel",             "이젤",      0f,     0.2f,   0f, 1.70f, 1.49f),
        ("03_Palette_Brush_Stand",   "팔레트",   -3.2f,   1.0f,   0f, 2.24f, 1.20f),
        ("09_Sculpture_Workstation", "조각대",    3.4f,   0.6f,   0f, 1.40f, 1.40f),
        ("06_Cinema_Camera",         "촬영카메라", 5.6f,   2.6f,   0f, 2.16f, 1.84f),
    };

    /// <summary>
    /// 재주관 — 미술·음악·영상·공연. 유저가 만든 설비 열 점(<b>2,846쿼드 · 100% 쿼드 ·
    /// 바닥 원점 · 실물 크기</b>)이 방의 전부고, 코드는 <b>마감만</b> 얹는다.
    ///
    /// ★ 이젤·작품벽에는 <b>0~1 UV 를 가진 전용 면</b>이 들어 있다
    /// (<c>Canvas</c> / <c>Artwork_01</c>~<c>05</c>). 따라 그리기 미니게임이 거기에
    /// 그린 그림을 올린다 — 기록첩의 <c>Page_Left</c>·<c>Page_Right</c> 와 같은 방식이다.
    /// </summary>
    void Jaeju(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;

        // 기념관과 같은 보정 — MyModel 이 보는 폭에는 건물 yaw 가 섞여 있다
        float yaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(yaw)), sw = Mathf.Abs(Mathf.Sin(yaw));

        var placed = new Transform[JaejuLayout.Length];
        int made = 0;
        for (int i = 0; i < JaejuLayout.Length; i++)
        {
            var p = JaejuLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);

            placed[i] = MyModel(t, JaejuFolder + p.file + ".fbx", "In_" + p.label,
                                new Vector3(p.x, 0f, p.z), seen);
            if (placed[i] != null) made++;
        }

        // ★★ <b>여기는 스스로 잰다.</b> 처음엔 철곰관이 잰 값을 빌려 쓰게 짰는데,
        // <b>건물 표에서 재주관이 철곰관보다 먼저 지어진다</b>(4번째 대 6번째) — 빌릴 값이
        // 아직 없어서 «못 쟀다» 경고가 떴다(2026-09-29, 첫 빌드 로그에서 잡았다).
        //
        // 다행히 이 묶음에는 <b>앞면에만 있는 부품이 진짜로 있다</b>: 이젤의 `Canvas` 와
        // 작품벽의 `Artwork_01`~`05`. 기념관이 못 쟀던 건 열 점 중 여덟이 통짜였기 때문이지
        // 방법이 없어서가 아니었다. <b>잴 수 있는 방은 자기가 잰다.</b>
        float best = 0f;
        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, JaejuMarks);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }
        float extra = Mathf.Abs(best) < 0.01f ? propFrontExtra : (best >= 0f ? 0f : 180f);

        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, JaejuLayout[i].yaw + extra, 0f) * placed[i].localRotation;

        if (Mathf.Abs(best) >= 0.01f)
        {
            // 먼저 잰 방이 뒤에 오는 방들(기념관)에 값을 넘긴다
            if (!propFrontKnown) { propFrontExtra = extra; propFrontKnown = true; }
            Debug.Log($"[재주관] 설비 {made}/10 · 앞면 신호 {best:+0.000;-0.000}m → "
                    + $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");
        }
        else
            Debug.LogWarning($"[재주관] 설비 {made}/10 · 앞면을 못 쟀다 — 부품 이름"
                           + "(Canvas·Artwork_01…)이 바뀌었는지 확인해라. 보정 "
                           + $"{extra:0}도로 세운다.");

        // ---- 바닥 동선 ----
        Block(t, "InLaneMain", new Vector3(0f, 0.012f, 3.6f), Quaternion.identity,
              new Vector3(14f, 0.02f, 0.16f), ColMapleGold, noCollider: true);

        // ---- 마감 ----
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.13f), h * 0.5f, sz * (inZ - 0.13f)),
                      Quaternion.identity, new Vector3(0.24f, h, 0.24f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.45f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.90f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.45f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.90f, 0.10f), ColWallFoot, noCollider: true);

        // 9m 천장은 공연장이라 그대로 두고, 무대 위에만 조명 바를 건다
        for (int i = -1; i <= 1; i++)
            Block(t, $"InLightBar_{i}", new Vector3(i * 4.5f, h - 0.7f, -2.2f), Quaternion.identity,
                  new Vector3(0.18f, 0.16f, d - 3f), ColWood, noCollider: true);
    }

    const string GommeoriFolder = "Assets/My blender/Gommeori/";

    /// <summary>
    /// 곰머리관에서 <b>앞면에만</b> 있는 부품. 서책장 하부 문 · 기록장 서랍 앞판 ·
    /// 상담 좌석과 낮은 상 · 두루마리장 걸개.
    ///
    /// ★ <c>Counseling_Seat</c> 를 넣은 건 <b>상담 공간의 병풍이 뒤에 있기 때문</b>이다
    /// (병풍 +0.67 · 좌석 −0.09). 병풍을 앞으로 착각하면 이 묶음 전체가 180도 돌아간다 —
    /// <see cref="FrontEvidence"/> 는 <b>제일 굵은 신호 하나</b>로 열 점을 다 정하니까
    /// «앞에만 있는 것» 을 골라야지 «눈에 띄는 것» 을 고르면 안 된다.
    /// </summary>
    static readonly string[] GommeoriMarks =
        { "Lower_Door", "Drawer_", "Counseling_Seat", "Low_Table", "Hanging_Scroll" };

    /// <summary>
    /// 곰머리관 소품 — 파일 · 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> ·
    /// 실측 폭 · 실측 깊이(m).
    ///
    /// <b>이 방은 교실이다.</b> 패키지 README 가 <i>"강학대와 칠판을 교실 앞쪽에"</i> 라고
    /// 했는데, 교실의 «앞» 은 <b>문 반대쪽</b>이다 — 그래서 교단을 −Z 벽에 붙이고
    /// 좌식 학습석이 그쪽을 보게 했다. 서책장·기록장·두루마리장은 옆벽으로 돌렸다.
    /// 그렇게 해야 <b>들어서면 교실인지 서고인지 바로 안다.</b>
    ///
    /// ★★ <b>바닥 띄움은 여기서 고치는 게 아니다.</b> 서책장·기록장·두루마리장은 원점이
    /// 지오메트리 맨 아래가 아니라 2~3cm 아래에 있다(README 에도 적혀 있고, 블렌더로 재서
    /// 확인했다). 그걸 <c>y</c> 칸을 만들어 −0.03 씩 내렸더니 <b>씬에서 −0.06 이 나왔다</b> —
    /// <see cref="MyModel"/> 이 이미 <c>bounds.min.y</c> 를 빼서 바닥에 앉히고 있었다.
    /// <b>보정이 두 번 먹어 소품이 바닥에 파묻혔다.</b> 칸을 없앴다.
    ///
    /// > 이미 있는 보정을 모르고 같은 보정을 한 번 더 넣는 건, 값이 <b>정확히 두 배</b>로
    /// > 나오는 걸로 드러난다. 배치 뒤에 씬을 다시 읽어 보지 않았으면 못 잡았다.
    ///
    /// ★ 방이 18 × 12 로 <b>곰솥관보다 작은데 소품은 더 크다</b>(상담 공간 3.31m ·
    /// 서책장 2.99m). 그래서 뒷벽 한 줄로 몰지 않고 <b>네 벽에 나눠</b> 걸었다.
    /// </summary>
    static readonly (string file, string label, float x, float z, float yaw, float w, float d)[]
        GommeoriLayout =
    {
        // ── 교단 — 문 반대쪽이 교실 앞이다 ──
        ("07_Lecture_Thinking_Board",  "성찰칠판",   -2.40f, -4.88f,   0f, 2.910f, 0.850f),
        ("02_Teacher_Lecture_Dais",    "강학대",      2.30f, -4.38f,   0f, 2.850f, 1.850f),

        // ── 왼쪽 벽 — 서고 ──
        ("01_Seodang_Bookcase",        "서책장",     -7.93f, -1.00f,  90f, 2.990f, 0.739f),
        ("05_Research_Archive_Cabinet","기록장",     -7.90f,  2.60f,  90f, 2.910f, 0.797f),

        // ── 오른쪽 벽 — 두루마리와 붓 ──
        ("10_Scroll_Study_Cabinet",    "두루마리장",   7.92f, -2.20f, 270f, 2.590f, 0.761f),
        ("04_Calligraphy_Worktable",   "서예실습대",   7.82f,  1.80f, 270f, 2.730f, 0.972f),

        // ── 가운데 — 학습석은 교단을 보고, 열람대는 연구 구역 ──
        ("03_Floor_Study_Desks",       "좌식학습석",  -1.80f, -0.60f,   0f, 2.440f, 2.575f),
        ("06_Manuscript_Reading_Table","고문헌열람대", 3.00f,  0.60f,   0f, 2.520f, 1.159f),

        // ── 조용한 구석 — 문 쪽 벽에 등을 붙인다 ──
        ("08_Counseling_Alcove",       "상담공간",   -5.00f,  3.60f, 180f, 3.310f, 1.480f),
        ("09_Quiet_Meditation_Platform","명상평상",   2.60f,  3.80f, 180f, 2.650f, 1.652f),
    };

    /// <summary>
    /// 곰머리관 — 인문·교육·연구·심리. 유저 소품 열 점(<b>11,604쿼드 · 100% 쿼드 ·
    /// 실물 크기</b>)이 방의 전부고, 코드는 <b>바닥 구획과 마감만</b> 얹는다.
    ///
    /// ★★ 전에는 <b>색깔 상자 서가 여섯 장 + 책상 하나</b>였다(2026-09-17). 학과 팻말이
    /// 「인문·교육·연구·심리」인데 방에는 <b>연구도 심리도 없었다</b> — 이제 서당·서고·상담이
    /// 다 있다. 되살리고 싶으면 이 case 하나만 되돌리면 된다.
    ///
    /// 건물 앞마당의 「쌓아 올린 책 더미」는 <b>바깥 표식</b>이라 그대로 뒀다.
    /// </summary>
    void Gommeori(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;

        // 다른 방들과 같은 보정 — MyModel 이 보는 폭에는 건물 yaw 가 섞여 있다.
        // ★ 곰머리관은 yaw 가 <b>95도</b>라 이 보정이 제일 크게 먹는다 —
        // 안 하면 폭과 깊이가 통째로 바뀌어 들어간다.
        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var placed = new Transform[GommeoriLayout.Length];
        int made = 0;
        for (int i = 0; i < GommeoriLayout.Length; i++)
        {
            var p = GommeoriLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);

            placed[i] = MyModel(t, GommeoriFolder + p.file + ".fbx", "In_" + p.label,
                                new Vector3(p.x, 0f, p.z), seen);
            if (placed[i] != null) made++;
        }

        // 앞면은 씬에서 잰다 — 다른 방들과 같은 절차
        float best = 0f;
        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, GommeoriMarks);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }
        float extra = Mathf.Abs(best) < 0.01f ? propFrontExtra : (best >= 0f ? 0f : 180f);

        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, GommeoriLayout[i].yaw + extra, 0f) * placed[i].localRotation;

        if (Mathf.Abs(best) >= 0.01f)
        {
            if (!propFrontKnown) { propFrontExtra = extra; propFrontKnown = true; }
            Debug.Log($"[곰머리관] 소품 {made}/10 · 앞면 신호 {best:+0.000;-0.000}m → "
                    + $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");
        }
        else
            Debug.LogWarning($"[곰머리관] 소품 {made}/10 · 앞면을 못 쟀다 — 부품 이름"
                           + "(Lower_Door·Drawer_1…)이 바뀌었는지 확인해라. 보정 "
                           + $"{extra:0}도로 세운다.");

        // ---- 바닥 ----
        // 교단 앞에 <b>마루단</b> 을 한 겹 깐다. 서당은 원래 바닥에 앉는 방이라,
        // 앉는 자리가 바닥과 같은 색이면 «학습석이 왜 여기 있나» 가 안 읽힌다.
        Block(t, "InMat", new Vector3(-1.8f, 0.014f, -0.6f), Quaternion.identity,
              new Vector3(4.0f, 0.03f, 4.2f), ColWoodRail, noCollider: true);
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InMatEdge_{s}", new Vector3(-1.8f + s * 2.0f, 0.022f, -0.6f),
                  Quaternion.identity, new Vector3(0.10f, 0.03f, 4.2f), ColTrimDark, noCollider: true);

        // 연구 구역 — 열람대와 서예대를 한 구역으로 묶는 테두리(웅지관에서 쓴 방법)
        for (int s = -1; s <= 1; s += 2)
        {
            Block(t, $"InZoneX_{s}", new Vector3(5.6f + s * 3.0f, 0.012f, 1.2f), Quaternion.identity,
                  new Vector3(0.10f, 0.02f, 4.4f), ColTrimDark, noCollider: true);
            Block(t, $"InZoneZ_{s}", new Vector3(5.6f, 0.012f, 1.2f + s * 2.2f), Quaternion.identity,
                  new Vector3(6.0f, 0.02f, 0.10f), ColTrimDark, noCollider: true);
        }

        // ---- 벽 마감 ----
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.15f), h * 0.5f, sz * (inZ - 0.15f)),
                      Quaternion.identity, new Vector3(0.26f, h, 0.26f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.42f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.84f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.42f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.84f, 0.10f), ColWallFoot, noCollider: true);

        // 주련 — 서당이니까 기둥에 세로 글판이 걸려야 말이 된다(웅지관과 같은 장치)
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InPillarSlip_{s}", new Vector3(s * (inX - 0.30f), 4.0f, -inZ + 2.2f),
                  Quaternion.identity, new Vector3(0.06f, 3.0f, 0.42f), ColCream, noCollider: true);

        // ---- 천장 ----
        // 제일 높은 소품이 두루마리장 2.72m 라 반자를 내려도 되지만, 서당은 <b>서까래가
        // 보이는 방</b>이다. 웅지관·곰솥관과 같이 높이 두고 층만 얹는다.
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InGirder_{s}", new Vector3(s * 4.0f, h - 0.70f, 0f), Quaternion.identity,
                  new Vector3(0.36f, 0.36f, d - 1.6f), ColWoodRail, noCollider: true);
        for (int i = 0; i < 7; i++)
            Block(t, $"InRafter_{i}", new Vector3(0f, h - 0.32f, -4.2f + i * 1.4f),
                  Quaternion.identity, new Vector3(w - 1.6f, 0.20f, 0.16f), ColWood, noCollider: true);
    }

    const string GomsotFolder = "Assets/My blender/Gomsot/";

    /// <summary>
    /// 곰솥관 소품 배율. 업소용 주방 설비라 실물 크기도 작지 않은데, 20 × 14m 방에서는
    /// <b>그래도 작아 보인다</b>(2026-09-29 수연). <b>계산으로 잰 한계는 1.50</b> —
    /// 여유를 두고 1.45 를 쓴다. 가마솥 조리대가 폭 4.12m · 후드까지 4.46m 가 된다.
    /// </summary>
    const float GomsotScale = 1.45f;

    /// <summary>
    /// 곰솥관에서 <b>앞면에만</b> 있는 부품. 발효장 문 · 오븐 문 · 냉장고 문 · 반죽기 볼.
    ///
    /// ★ 열 점을 블렌더로 다 뜯어 보고 고른 것이다. 신호가 제일 굵은 건
    /// <c>Proofer_Door</c>(문이 열린 채로 만들어져 몸통 중심에서 1.2m 나와 있다) —
    /// 오븐·냉장고 문은 0.1m 대라 혼자서는 못 믿는다. <see cref="FrontEvidence"/> 가
    /// <b>제일 굵은 신호 하나</b>로 묶음 전체를 정하니까 굵은 것을 반드시 넣어야 한다.
    ///
    /// <c>"Door_"</c> 는 냉장고의 <c>Door_1~4</c> 만 잡는다 — 발효장은 부품 이름이
    /// <c>Proofer_Door</c> 라 <c>StartsWith("Door_")</c> 에 안 걸린다.
    /// </summary>
    static readonly string[] GomsotMarks = { "Proofer_Door", "Oven_Door", "Door_", "Bowl" };

    /// <summary>
    /// 곰솥관 설비 — 파일 · 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> · 실측 폭 · 실측 깊이(m).
    ///
    /// <b>배율 1.0.</b> 재주관과 같은 이유다 — 이 묶음은 전시물이 아니라 <b>업소용 주방 설비</b>
    /// 크기로 왔다(가마솥 조리대 2.84m · 후드까지 높이 3.08m). 20 × 14m 방에 실물로 세워야
    /// 가운데 통로가 남는다.
    ///
    /// 자리는 패키지 README 의 권장 배치를 그대로 따랐다 —
    /// <i>"벽면에는 솥 조리대·오븐·발효장·냉장고, 중앙에는 제빵 작업대와 반죽기,
    /// 전처리 구역에는 세척대, 출입구 근처에는 재료 선반과 공급 카트."</i>
    /// <b>만든 사람이 어디에 두라고 적어 놨으면 그대로 둔다.</b> L01~L05 를 내 판단으로
    /// 옮겼다가 도로 물린 게 바로 이 방 때문이었다(<see cref="BapMadangExhibits"/>).
    ///
    /// ★ 문은 <b>+Z 로 열린다.</b> 발효장(깊이 2.02m)은 문이 열린 채 모델링돼 있어서
    /// 벽에서 제 깊이의 <b>절반</b>만 띄우면 문짝이 벽을 뚫는다 — 깊이를 그대로 쓴다.
    /// </summary>
    static readonly (string file, string label, Wall wall, float x, float z,
                     float yaw, float w, float d)[]
        GomsotLayout =
    {
        // ── 뒷벽 — 불과 냉기. 서서 쓰는 큰 장비를 한 줄로 몰았다 ──
        ("01_Cauldron_Stove",         "가마솥조리대", Wall.뒤,   0f, 0f,   0f, 2.840f, 1.297f),
        ("02_Three_Deck_Oven",        "데크오븐",     Wall.뒤,   0f, 0f,   0f, 2.430f, 1.293f),
        ("05_Proofing_Cabinet",       "발효장",       Wall.뒤,   0f, 0f,   0f, 1.343f, 2.024f),
        ("08_Four_Door_Refrigerator", "냉장고",       Wall.뒤,   0f, 0f,   0f, 2.010f, 1.225f),

        // ── 왼쪽 벽 — 전처리 구역. 씻고 식히는 일은 불에서 떨어뜨린다 ──
        // 순서가 곧 자리다 — 앞쪽(−Z)부터 적는다.
        ("06_Bread_Cooling_Rack",     "냉각랙",       Wall.왼,   0f, 0f,  90f, 1.425f, 1.100f),
        ("09_Twin_Wash_Station",      "세척대",       Wall.왼,   0f, 0f,  90f, 2.710f, 0.950f),

        // ── 오른쪽 벽 — 재료 ──
        ("07_Ingredient_Store_Shelves", "재료선반",   Wall.오른, 0f, 0f, 270f, 2.560f, 0.971f),

        // ── 가운데 — 실습 구역. 둘 사이 <b>3.0m</b> 가 문에서 들어오는 통로다 ──
        // 배율을 1.45 로 올리면서 둘을 더 벌렸다 — 실물 크기 때의 간격(2.5m)을 그대로 두면
        // 통로가 1.3m 로 좁아져서 <b>문에서 곧장 못 들어온다.</b>
        ("04_Bakery_Workbench",       "제빵작업대", Wall.가운데, -3.40f, 1.40f,  0f, 2.650f, 1.148f),
        ("03_Floor_Dough_Mixer",      "반죽기",     Wall.가운데,  2.40f, 1.40f,  0f, 1.170f, 1.200f),

        // ── 출입구 옆 — 재료가 들어오는 자리 ──
        ("10_Ingredient_Delivery_Cart", "공급카트", Wall.가운데,  5.60f, 3.50f, 15f, 2.303f, 1.133f),
    };

    /// <summary>
    /// 곰솥관 — 조리실습·제과제빵. 현판 한 줄이 「불 조심. 곰은 더 조심」이다.
    /// 유저 설비 열 점(<b>9,242쿼드 · 100% 쿼드 · 바닥 원점 · 실물 크기</b>)이 방의 전부고,
    /// 코드는 <b>바닥 구획과 마감만</b> 얹는다.
    ///
    /// ★★ <b>이 방에는 여태 case 가 없었다.</b> 그래서 <see cref="Interior"/> 의 `default`
    /// 로 떨어져 <b>웅지관 소품이 한 벌 더</b> 생겼고, 좌표가 큰 방 기준이라 여덟 점이
    /// 벽을 뚫고 마당에 서 있었다(2026-09-29 수연 발견).
    /// </summary>
    void Gomsot(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;

        // 재주관·기념관과 같은 보정 — MyModel 이 보는 폭에는 건물 yaw 가 섞여 있다
        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var box = new (Wall, float, float, float, float)[GomsotLayout.Length];
        for (int i = 0; i < GomsotLayout.Length; i++)
        {
            var p = GomsotLayout[i];
            box[i] = (p.wall, p.x, p.z, p.w, p.d);
        }
        var at = Seat(box, "곰솥관", w, d, GomsotScale);

        var placed = new Transform[GomsotLayout.Length];
        int made = 0;
        for (int i = 0; i < GomsotLayout.Length; i++)
        {
            var p = GomsotLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);

            placed[i] = MyModel(t, GomsotFolder + p.file + ".fbx", "In_" + p.label,
                                at[i], seen * GomsotScale);
            if (placed[i] != null) made++;
        }

        // 앞면은 씬에서 잰다 — 다른 방들과 같은 절차
        float best = 0f;
        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, GomsotMarks);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }
        float extra = Mathf.Abs(best) < 0.01f ? propFrontExtra : (best >= 0f ? 0f : 180f);

        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, GomsotLayout[i].yaw + extra, 0f) * placed[i].localRotation;

        if (Mathf.Abs(best) >= 0.01f)
        {
            if (!propFrontKnown) { propFrontExtra = extra; propFrontKnown = true; }
            Debug.Log($"[곰솥관] 설비 {made}/10 · 앞면 신호 {best:+0.000;-0.000}m → "
                    + $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");
        }
        else
            Debug.LogWarning($"[곰솥관] 설비 {made}/10 · 앞면을 못 쟀다 — 부품 이름"
                           + "(Proofer_Door·Oven_Door…)이 바뀌었는지 확인해라. 보정 "
                           + $"{extra:0}도로 세운다.");

        // ---- 바닥 구획 ----
        // 실습실은 <b>구역이 보이는 방</b>이다. 불 쓰는 뒷줄과 손 쓰는 가운뎃줄 사이에
        // 선이 없으면 그냥 장비 창고로 보인다. 웅지관에서 쓴 테두리와 같은 방법.
        Block(t, "InZoneFire", new Vector3(0f, 0.012f, -3.9f), Quaternion.identity,
              new Vector3(w - 3.2f, 0.02f, 0.14f), ColRibbon, noCollider: true);
        for (int s = -1; s <= 1; s += 2)
        {
            Block(t, $"InZoneBakeX_{s}", new Vector3(s * 5.2f, 0.012f, 1.4f), Quaternion.identity,
                  new Vector3(0.12f, 0.02f, 3.6f), ColTrimDark, noCollider: true);
            Block(t, $"InZoneBakeZ_{s}", new Vector3(0f, 0.012f, 1.4f + s * 1.8f),
                  Quaternion.identity, new Vector3(10.4f, 0.02f, 0.12f), ColTrimDark, noCollider: true);
        }

        // 문에서 실습대까지 이어지는 안내선 — 곰밥마당·웅지관과 같은 장치
        for (int i = 0; i < 5; i++)
            Block(t, $"InGuide_{i}", new Vector3(0.1f + i * 0.1f, 0.012f, 5.6f - i * 0.9f),
                  Quaternion.identity, new Vector3(0.8f, 0.02f, 0.12f), ColMapleGold, noCollider: true);

        // ---- 벽 마감 ----
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.15f), h * 0.5f, sz * (inZ - 0.15f)),
                      Quaternion.identity, new Vector3(0.28f, h, 0.28f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.42f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.84f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.42f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.84f, 0.10f), ColWallFoot, noCollider: true);

        // 불 쓰는 벽은 <b>타일</b>이다. 뒷줄 장비 뒤로 허리높이 띠를 둘러 조리실처럼 보이게 한다
        Block(t, "InTileBack", new Vector3(0f, 1.9f, -inZ + 0.04f), Quaternion.identity,
              new Vector3(w - 1.6f, 2.6f, 0.08f), ColWallTile, noCollider: true);

        // ---- 천장 — 대들보와 서까래 ----
        // 가마솥 후드가 3.08m 라 반자를 내리면 부딪힌다. 웅지관처럼 높이 두고 층만 얹는다.
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InGirder_{s}", new Vector3(s * 4.6f, h - 0.70f, 0f), Quaternion.identity,
                  new Vector3(0.40f, 0.40f, d - 1.6f), ColWoodRail, noCollider: true);
        for (int i = 0; i < 7; i++)
            Block(t, $"InRafter_{i}", new Vector3(0f, h - 0.32f, -5.1f + i * 1.7f),
                  Quaternion.identity, new Vector3(w - 1.6f, 0.20f, 0.16f), ColWood, noCollider: true);
    }

    /// <summary>소품이 붙는 벽. <see cref="Wall.가운데"/> 는 자리를 손으로 적는다.</summary>
    public enum Wall { 가운데, 뒤, 앞, 왼, 오른 }

    /// <summary>
    /// <b>벽에 붙인 소품을 배율 하나로 다시 앉힌다.</b>
    ///
    /// ★★ 2026-09-29 수연: *"웅지관 크기에 비해 안에 들어있는 에셋 크기 작다."* 재 보니
    /// 웅지관·곰솥관 소품이 <b>전부 배율 1.0 — 실물 크기</b>였다. 36 × 22m 짜리 방에
    /// 실물 크기 가구를 놓으면 <b>인형의 집처럼</b> 보인다. 방이 크면 소품도 커야 한다.
    ///
    /// 그런데 <b>크기만 올리면 두 군데가 동시에 깨진다:</b>
    /// <list type="number">
    /// <item>벽에 붙인 소품은 <b>깊이가 같이 자라서 벽을 뚫는다</b></item>
    /// <item>한 벽에 여럿이면 <b>서로 먹는다</b> — 자리가 실물 크기 기준으로 박혀 있으니까</item>
    /// </list>
    ///
    /// 그래서 자리를 <b>손으로 적지 않는다.</b> 벽마다 「이 벽에 이 순서로」만 적고,
    /// 깊이의 절반으로 벽에서 띄우고 남는 길이를 고르게 나눈다. <b>배율 하나만 바꾸면
    /// 열 점이 따라온다</b> — 「더 키워라」가 또 올 것이기 때문이다.
    ///
    /// ★ <b>모서리를 비운다.</b> 처음엔 이걸 안 해서 «왼쪽 벽 줄» 과 «뒷벽 줄» 이 코너에서
    /// 겹쳤다(배율 1.00 에서도 세 쌍이 겹쳤다). 옆벽 줄은 앞뒤 벽 줄이 <b>벽에서 나온
    /// 깊이</b>만큼 양끝을 물러선다.
    /// </summary>
    /// <returns><paramref name="rows"/> 와 같은 순서의 자리. y 는 0 — 바닥 앉히기는
    /// <see cref="MyModel"/> 이 한다(곰머리관에서 두 번 빼서 파묻은 적 있다).</returns>
    static Vector3[] Seat((Wall wall, float x, float z, float w, float d)[] rows,
                          string hall, float roomW, float roomD, float scale,
                          float margin = 0.45f, float gapMin = 0.55f)
    {
        float inX = roomW * 0.5f - 0.6f, inZ = roomD * 0.5f - 0.6f;
        var at = new Vector3[rows.Length];

        float Reach(Wall side)
        {
            float best = 0f;
            foreach (var r in rows)
                if (r.wall == side) best = Mathf.Max(best, r.d * scale + 0.10f);
            return best;
        }

        foreach (var side in new[] { Wall.뒤, Wall.앞, Wall.왼, Wall.오른 })
        {
            bool alongZ = side == Wall.왼 || side == Wall.오른;
            float half = alongZ ? inZ : inX;
            float loCut = Reach(alongZ ? Wall.뒤 : Wall.왼);
            float hiCut = Reach(alongZ ? Wall.앞 : Wall.오른);

            float total = 0f;
            int n = 0;
            foreach (var r in rows)
                if (r.wall == side) { total += r.w * scale; n++; }
            if (n == 0) continue;

            float free = half * 2f - loCut - hiCut - margin * 2f - total;
            float gap = n > 1 ? free / (n - 1) : 0f;

            // ★ 조용히 넘기지 않는다. 배율을 올리다 못 받게 되면 <b>소품이 벽을 뚫고
            // 마당에 서는데</b>, 그건 씬을 열어 보기 전에는 안 보인다(2026-09-29 웅지관).
            if (free < 0f || (n > 1 && gap < gapMin))
                Debug.LogWarning($"[{hall}] {side}쪽 벽이 배율 {scale:0.00} 를 못 받는다 — "
                               + $"남는 자리 {free:0.00}m · 간격 {gap:0.00}m. 배율을 낮추거나 "
                               + $"이 벽의 소품 하나를 다른 벽으로 옮겨라.");

            float cur = -half + loCut + margin;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].wall != side) continue;
                float c = cur + rows[i].w * scale * 0.5f;
                cur += rows[i].w * scale + gap;

                // 소품마다 깊이가 달라서 <b>제 깊이의 절반만큼</b> 앞으로 내야 벽에 딱 붙는다
                float off = rows[i].d * scale * 0.5f + 0.10f;
                at[i] = side switch
                {
                    Wall.왼 => new Vector3(-inX + off, 0f, c),
                    Wall.오른 => new Vector3(inX - off, 0f, c),
                    Wall.뒤 => new Vector3(c, 0f, -inZ + off),
                    _ => new Vector3(c, 0f, inZ - off),
                };
            }
        }

        for (int i = 0; i < rows.Length; i++)
            if (rows[i].wall == Wall.가운데)
                at[i] = new Vector3(rows[i].x, 0f, rows[i].z);

        return at;
    }


    // ══════════════════════════════════════════════════════════════════
    //  이야기 단서 에셋 열 점 — 웅성관 · 곰머리관 · 곰테크관 · 참잘했어요관
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 곰누리관 소품 배율. 방이 20 × 13m 로 캠퍼스에서 두 번째로 크다 —
    /// 유저가 «크면 좋지만 너무 크면 조절해» 라고 했는데, <b>1.05 가 상한</b>이다:
    /// 왼벽에 세계지도(5.08) + 안내책자대(3.09) 가 붙는데 그 벽이 11.8m 라
    /// 1.1 이면 간격이 1m 밑으로 떨어진다.
    /// </summary>
    const float GomnuriScale = 1.05f;

    /// <summary>
    /// ★★ 상황판에 적히는 <b>연간 방문객 목표</b>.
    ///
    /// 제작자가 숫자를 비워서(`— 명`) 보냈다 — 기획서에 수치가 없었기 때문이야.
    /// <b>이 한 군데서만 정한다.</b> 1장 대사가 «시 발표의 열 배» 라고 하니까,
    /// 시가 발표한 숫자 = 이 목표치 = <b>12,000</b>, 실제 기록부 = <b>120,000</b> 이 된다.
    ///
    /// 떡밥이 여기서 완성된다: <b>아무도 «목표» 를 «실적» 이라고 말하지 않았지만,
    /// 시가 발표한 숫자가 여기 적힌 목표치와 똑같다.</b> 플레이어가 혼자 잇는다.
    ///
    /// 바꾸려면 이 줄만 고치고, <b>1장 대사의 «열 배» 와 어긋나지 않는지</b> 확인해라 —
    /// 같은 숫자를 두 군데서 정하면 반드시 어긋난다.
    /// </summary>
    const string TourismTarget = "12,000";

    /// <summary>
    /// 상황판의 빈 숫자판에 목표치를 얹는다. 아틀라스를 고치는 대신 <b>월드 글자</b>로 —
    /// 현판·안내판·접수대 시계가 쓰는 그 길이다(<see cref="BuildingSign.TextMaterial"/>).
    /// 새로 넣는 에셋이 0개고, 숫자를 바꾸려면 위 상수 한 줄만 고치면 된다.
    /// </summary>
    void TourismNumber(Transform t, float w, float d)
    {
        var board = t.Find("In_관광상황판");
        if (board == null) return;

        MeshRenderer plate = null;
        foreach (var r in board.GetComponentsInChildren<MeshRenderer>())
            if (r.name.StartsWith("Target_Number")) { plate = r; break; }
        if (plate == null) { Debug.LogWarning("[곰누리관] 상황판의 Target_Number 를 못 찾았다."); return; }

        // ★★ 2026-10-07 유저: *"12,000 숫자가 떠 있으니 이건 고치는 게 좋아."*
        //   <b><c>plate.position</c> 을 썼던 게 틀렸다.</b> FBX 부품의 트랜스폼은
        //   보통 <b>모델 원점(바닥 한가운데)</b>에 있어서, 숫자가 판이 아니라
        //   <b>바닥으로 떨어졌다.</b> 보이는 자리를 쓰려면 <b>렌더러 바운즈 한가운데</b>다.
        //
        //   > <b>부품에 뭘 얹을 때 <c>transform.position</c> 을 믿지 마라.</b>
        //   > 눈에 보이는 자리는 <c>renderer.bounds.center</c> 다.
        Bounds wb = plate.bounds;
        Vector3 local = t.InverseTransformPoint(wb.center);

        // 이 판은 뒤벽에 붙어 있으니 <b>방 안쪽(+Z)</b>을 본다. 자리로 정하면
        // 나중에 판을 다른 벽으로 옮겨도 글자가 따라온다.
        Vector3 face = new Vector3(0f, 0f, local.z < 0f ? 1f : -1f);

        // 판 크기에서 글자 크기를 뽑는다 — 판을 다시 만들어도 안 넘친다(현판에서 배운 것).
        Vector3 ps = t.InverseTransformVector(wb.size);
        float plateH = Mathf.Max(0.2f, Mathf.Abs(ps.y));
        float plateW = Mathf.Max(0.2f, Mathf.Abs(ps.x));

        // ★ <b>높이만 보면 안 된다.</b> «12,000» 은 여섯 자라 높이의 2.3배로 퍼져서
        //   판(0.76m)보다 넓어진다 — 재 보고 잡았다. <b>폭에서도 깎아 더 작은 쪽</b>을 쓴다.
        //   한 글자 폭 ≈ 높이 × 0.42 로 어림한다(숫자와 쉼표는 한글보다 좁아서 <b>작게 잡히는 쪽</b>으로 틀린다).
        float digits = Mathf.Max(1, TourismTarget.Length);
        float glyph = plateH * 0.36f;
        glyph = Mathf.Min(glyph, plateW * 0.90f / (digits * 0.42f));

        // ★ 원래 적혀 있던 «— 명» 을 가린다. 글자만 얹으면 둘이 겹쳐 보인다.
        //   덮개는 판 <b>앞</b>, 글자는 그보다 <b>더 앞</b> — 층을 뒤에서 앞으로 쌓는다.
        var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cover.GetComponent<Collider>());
        cover.name = "InTargetCover";
        cover.transform.SetParent(t, false);
        cover.transform.localPosition = local + face * 0.08f;
        cover.transform.localRotation = Quaternion.LookRotation(face, Vector3.up);
        cover.transform.localScale = new Vector3(plateW * 0.94f, glyph * 1.7f, 0.03f);
        cover.GetComponent<MeshRenderer>().sharedMaterial =
            FlatMaterial.Get(new Color32(0xFD, 0xF8, 0xEC, 0xFF));

        var go = new GameObject("InTargetNumber");
        go.transform.SetParent(t, false);
        go.transform.localPosition = local + face * 0.12f;
        // ★ 180도 돌려서 단다 — TextMesh 를 그대로 붙이면 <b>좌우가 뒤집힌다</b>.
        //   이 프로젝트에서 거울상으로 다섯 번 틀렸다(현판·진열장 숫자·시계·화장실 칸·문).
        go.transform.localRotation = Quaternion.LookRotation(face, Vector3.up)
                                   * Quaternion.Euler(0f, 180f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = TourismTarget;
        tm.font = Resources.Load<Font>("HudFont");
        tm.fontSize = 120;
        // 판 높이의 36% — 판에서 거꾸로 뽑으니 판을 다시 만들어도 안 넘친다
        tm.characterSize = glyph * 10f / 120f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color32(0x4A, 0x3A, 0x2C, 0xFF);

        var mr = go.GetComponent<MeshRenderer>();
        if (tm.font != null) mr.sharedMaterial = BuildingSign.TextMaterial(tm.font);

        Debug.Log($"[곰누리관] 목표 숫자 '{TourismTarget}' 를 상황판에 얹었다 · " +
                  $"판 local z {local.z:0.00} · 글자가 보는 쪽 {face.z:+0;-0}");
    }
    const string StoryFolder = "Assets/My blender/StoryHalls/";
    const string GomnuriFolder = "Assets/My blender/Gomnuri/";

    /// <summary>
    /// ★★ 2026-10-07 수연이 만든 <b>웅성관 전용 열 점.</b> 방송국이면서 <b>증거가 제일 많은 방</b>이다 —
    /// 광고 심의 서류에 「골든베어 승인」이 붙어 있고, 테이프 하나는 라벨을 <b>고쳐 붙였다</b>.
    /// 전에는 상자로 흉내 낸 부스 하나뿐이라 «방송국» 이라는 말만 있고 물건이 없었다.
    /// </summary>
    const string WoongFolder = "Assets/My blender/Woongseong_/";

    /// <summary>
    /// ★ 2026-10-08 수연의 <b>참잘했어요관 열 점.</b> 시상관인데 트로피 진열장 하나뿐이라
    /// «한 자리가 비어 있다» 는 농담이 <b>빈 방 때문에 안 보였다</b> — 시상대와 기념 아치가
    /// 서야 그 빈자리가 눈에 띈다.
    /// </summary>
    const string AwardsFolder = "Assets/Awards_Hall_/";

    /// <summary>★ 2026-10-08 수연의 <b>곰테크관 열 점.</b> 「왜 카트인가」에 답하는 방이다.</summary>
    const string GomtechFolder = "Assets/My blender/Gom_Gomtech/";

    /// <summary>★ 2026-10-08 수연의 <b>곰짝박수마당 열 점.</b> 무대 뒤 장비 묶음이다.</summary>
    const string GomjjakFolder = "Assets/Gomjjak_/";

    // ──────────────────────────────────────────────────────────────────────────
    //  이야기 단서 열 점 — 「읽으면 알게 되는 것」
    //
    //  ★★ 2026-10-08 수연: *"너무 대놓고 떡밥을 주지 말고 유추할 수 있는 수준으로."*
    //     그래서 <b>받침대에 올려 조명을 주지 않았다.</b> 전부 <b>바닥에 놓는다</b> —
    //     문에서 보이는 자리가 아니라 <b>그 방의 물건 옆</b>에, 걸어 들어가서 돌아봐야 보이게.
    //     철거 심사를 받는 건물에 <b>굴러다니는 서류</b>라는 설정과도 맞는다.
    //
    //  ★ 열 점이 한 묶음이라 <b>방향을 한 번만 재서 같이 쓴다.</b> 관마다 재면
    //    신호가 약한 방(종이 한 장짜리는 쏠림이 7cm 다)이 혼자 거꾸로 선다.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>★ 폴더 이름의 «–» 는 <b>붙임표가 아니라 en-dash</b>(U+2013)다.</summary>
    const string ClueFolder = "Assets/Clue 001–010/";

    /// <summary>
    /// 앞면 표식. <b>`Front_` 로 시작하는 넷만</b> 쓴다 — 나머지(도장·클립·종이집게)는
    /// 물건 가운데에 놓여서 쏠림이 거의 0 이라 방향을 못 가른다.
    /// </summary>
    static readonly string[] ClueMarks =
        { "Front_Overdue_Notice", "Front_Lost_Box_Label", "Front_Photo_Caption", "Front_Bear_Drawing" };

    /// <summary>
    /// ★ <b>−0.15 에서 시작한다.</b> 이 프로젝트에 들어온 묶음 넷이 전부 블렌더 +Y 를
    /// 앞면이라 적어 놓고 유니티에서 <b>음수</b>로 측정됐다(StoryHalls −1.010 · 웅성관 −1.094 ·
    /// 참잘했어요관 −0.592). 단서는 종이 한 장이라 신호가 약해서(최대 0.20) 0 에서 재면
    /// <b>약한 양수 하나에 열 점이 통째로 뒤돌아설 수 있다.</b> 더 센 값이 나오면 그때 바뀐다.
    /// </summary>
    const float ClueSignSeed = -0.15f;

    float clueSign = ClueSignSeed;

    /// <summary>
    /// 자리표 — 파일 · 이름 · <b>어느 관</b> · (x, y, z) · 앞면이 향할 각 · 실제 폭(m).
    /// y 는 바닥(0)이 기본이고, 벽에 거는 것만 높이를 준다.
    /// </summary>
    static readonly (string hall, string file, string label,
                     float x, float y, float z, float yaw, float w)[] ClueLayout =
    {
        // 웅지관(행정) — <b>관장이 왜 서명했나.</b> 변호가 아니라 사정이다
        ("웅지관", "C01_Overdue_Utility_Drawer",      "공과금서랍", -10.5f, 0f, -6.2f,  20f, 0.66f),
        ("웅지관", "C02_First_Demolition_Decision",   "기각결정문",  16.3f, 0f,  1.6f, 270f, 0.39f),

        // 곰생회관(학생회) — <b>주인 없는 곰은 밖에 못 나간다</b>를 서류 한 장으로
        ("곰생회관", "C03_Rejected_Exit_Application", "외출신청서",  -6.4f, 0f,  4.2f, 150f, 0.61f),
        ("곰생회관", "C04_Unclaimed_Teddy_Box",       "분실물함",     4.8f, 0f, -3.4f, 200f, 0.58f),

        // 곰손관(봉제) — 세 형제가 어떻게 「한」 성을 받았나 · 십 년이 흘렀다
        ("곰손관", "C05_Han_Family_Adoption_Ledger",  "입양기록부",  -8.2f, 0f,  4.2f, 110f, 0.68f),
        ("곰손관", "C06_Ten_Year_Repair_Ticket",      "수선대기표",  -8.2f, 0f,  1.4f, 100f, 0.43f),

        // 대충기념관 — 1장의 «열 배 차이» 가 사진 한 장으로 보인다
        ("대충기념관", "C07_Opening_Day_Photograph",  "개관식사진",   2.6f, 0f, -3.4f,   0f, 0.76f),

        // 곰밥마당 — <b>벽에 건다</b>(걸이 쇠가 달려 있다). 연혁의 빈 3년과 같은 구멍
        ("곰밥마당", "C08_Missing_Meal_Years",        "급식추이표",  14.3f, 1.0f, 3.0f, 270f, 0.63f),

        // 철곰관 — 2장 서류 조작의 출처
        ("철곰관", "C09_Safety_Inspection_Original",  "점검일지",    -2.8f, 0f,  3.6f, 150f, 0.56f),

        // 재주관 — ★ 뒷면에 한마디가 적혀 있다. <b>그림이 문 쪽을 보게</b> 세운다.
        //   플레이어는 귀여운 그림을 먼저 보고, <b>돌아가야</b> 뒷면을 읽는다.
        //   ⚠ 렌더로 확인하고 180도 돌렸다 — 종이 한 장이라 앞면 쏠림이 7mm 뿐이라
        //   자동 측정으로는 앞뒤를 못 가린다. <b>이 한 줄만은 눈으로 보고 정한 값</b>이다.
        ("재주관", "C10_Bears_Museum_Drawing",        "곰의그림",    -6.5f, 0f,  3.0f,  20f, 0.62f),
    };

    /// <summary>이 관 몫의 단서를 세운다. 관마다 <see cref="Interior"/> 끝에서 한 번 부른다.</summary>
    void Clues(Transform t, string hall)
    {
        var rows = new System.Collections.Generic.List<int>();
        for (int i = 0; i < ClueLayout.Length; i++)
            if (ClueLayout[i].hall == hall) rows.Add(i);
        if (rows.Count == 0) return;

        // 건물이 yaw 로 돌아가 있으면 월드 AABB 가 실제보다 넓게 잡힌다(MyModel 이 그걸로 잰다)
        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var placed = new Transform[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var p = ClueLayout[rows[i]];
            // ★ 실물보다 <b>1.25배</b>. 종이 한 장은 실물 크기면 바닥에서 안 보이고,
            //   두 배를 넘기면 «전시물» 이 되어 대놓고 가리키는 꼴이 된다.
            float seen = p.w * 1.25f * Mathf.Max(cw + sw * 0.5f, sw + cw * 0.5f);
            placed[i] = MyModel(t, ClueFolder + p.file + ".fbx", "In_단서_" + p.label,
                                new Vector3(p.x, p.y, p.z), seen);

            // ★ 읽을 수 있게 만든다. 라벨이 곧 <see cref="ClueText"/> 의 id 라
            //   글을 고치려면 그 파일 한 곳만 보면 된다.
            if (placed[i] != null)
                placed[i].gameObject.AddComponent<ClueBoard>().id = p.label;
        }

        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, ClueMarks);
            if (Mathf.Abs(e) > Mathf.Abs(clueSign)) clueSign = e;
        }
        float extra = clueSign < 0f ? 180f : 0f;

        int made = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (placed[i] == null) continue;
            made++;
            placed[i].localRotation =
                Quaternion.Euler(0f, ClueLayout[rows[i]].yaw + extra, 0f) * placed[i].localRotation;
        }
        Debug.Log($"[{hall}] 단서 {made}/{rows.Count} · 앞면 신호 {clueSign:+0.000;-0.000}m → " +
                  $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");
    }


    /// <summary>
    /// 네 관이 다 18 × 12m 안팎이라 <b>한 배율로 충분하다.</b> 소품이 이미 실물 크기고
    /// (2.2~2.9m 높이) 방 천장이 8~9m 라, 1.10 이면 <b>조금 커 보이는 쪽</b>으로 맞는다 —
    /// 달리면서 보는 게 아니라 <b>걸어 들어와서 읽는</b> 물건들이다.
    /// </summary>
    const float StoryScale = 1.10f;

    /// <summary>
    /// ★ 앞면 표식. <b>«앞에 놓인 것» 만 쓴다.</b>
    ///
    /// 이 묶음에서 쓰면 안 되는 것 둘을 재서 걸러냈다:
    /// <c>Structure</c> 는 카트 리프트에서 <b>−0.76</b>(공구벽이 뒤에 선다)이고,
    /// <c>Screen_01~03</c> 은 송출대에서 <b>−0.33</b>(모니터가 책상 안쪽에 선다)이다.
    /// 둘을 넣으면 제일 센 신호가 되어 <b>열 점이 통째로 등을 돌린다</b> —
    /// 지붕 상징물에서 변기 뚜껑·팔레트 붓을 뺀 것과 같은 이유야(2026-09-28).
    /// </summary>
    static readonly string[] StoryMarks =
        { "Controller_", "Panel_", "Ledgers", "Trophy_", "Wheel_F",
          // 곰누리관 — 지도 핀 6개와 여행가방 3개가 제일 센 신호다
          "Map_Pin_", "Suitcase_", "Target_Number",
          // 웅성관 — <b>ON AIR 등</b>이 제일 세다(쏠림 0.995). 「Clip」은 종이 <b>윗단</b>에
          // 붙은 집게라 앞뒤와 상관이 없어서 뺐다 — 뒤쪽 부품을 넣으면 열 점이 통째로 돌아선다.
          "ON_AIR_Light", "Correction_Copy", "GoldenBear_Approval",
          "Control_Panel", "Return_Stamp",
          // 참잘했어요관 — 설명 패널(Caption_02, 쏠림 0.710)이 제일 세다.
          // ★ <b>`Backdrop`(시상대 뒤판 −1.018)과 `Backrest`(벤치 등받이 −0.372)는 넣지 마라</b> —
          //   둘 다 <b>뒤쪽</b> 부품이라 넣는 순간 열 점이 통째로 등을 돌린다.
          "Caption_", "Front_Plaque", "Certificate_", "Controls", "Medals", "Laurel",
          // 곰테크관 — 손잡이와 입력부. ★ <b>`Screen` 은 넣지 마라</b>(주행 시뮬레이터 −0.961):
          //   화면은 <b>앉은 사람 쪽</b>을 보니까 밖에서 보면 뒤쪽 부품이다.
          //   `Title` 도 뺐다 — 이름이 흔해서 다른 묶음에 같은 이름이 생기면 거기까지 흔든다.
          "Crank", "Handwheels", "Inputs",
          // 곰짝박수마당 — `Front_Label` 이 열 점 중 일곱에 있고 전부 앞쪽이다(최대 +1.100).
          "Front_Label" };

    /// <summary>
    /// 자리표 — 파일 · 이름 · <b>어느 관</b> · 벽 · (x, z) · 앞면이 향할 각 · 실제 폭 · 깊이.
    /// 크기는 제작자가 준 실측값 그대로다(README 표).
    /// </summary>
    static readonly (string folder, string file, string label, string hall, Wall wall,
                     float x, float z, float yaw, float w, float d)[]
        StoryLayout =
    {
        // ── 웅성관(방송) : 결승이 생중계되는 방. 수집품 7번 「중계 기록 장치」의 출처
        (StoryFolder, "S01_Live_Broadcast_Console",    "생중계송출대", "웅성관",   Wall.뒤,   0f, 0f,   0f, 3.70f, 1.45f),
        (StoryFolder, "S02_Local_News_Board",          "지역뉴스판",   "웅성관",   Wall.뒤,   0f, 0f,   0f, 4.02f, 1.05f),
        (StoryFolder, "S03_Corrected_Broadcast_Scripts","수정원고",    "웅성관",   Wall.오른, 0f, 0f, 270f, 2.45f, 1.35f),
        // ★ 2026-10-07 수연의 웅성관 열 점 중 <b>바닥에 서는 일곱.</b> 나머지 셋(원본 테이프 ·
        //   팩스 보고서 · 출입증)은 20~50cm 짜리 <b>책상 위 물건</b>이라 바닥에 두면
        //   «흘린 쓰레기» 로 보인다 — <see cref="OnTop"/> 로 따로 얹는다.
        (WoongFolder, "W03_Advertising_Review_Cabinet", "광고심의장",  "웅성관",  Wall.뒤,    0f,   0f,    0f, 2.80f, 0.60f),
        (WoongFolder, "W04_Antenna_Control_Station",   "안테나제어대","웅성관",  Wall.왼,    0f,   0f,   90f, 1.60f, 0.80f),
        (WoongFolder, "W05_One_Record_Shelf",          "기록보관선반","웅성관",  Wall.왼,    0f,   0f,   90f, 1.40f, 0.50f),
        // 부스는 <b>방 가운데</b>다. 실제 방송국 부스가 그렇고, 벽에 붙이면 뒷벽 자리를
        // 3.3m 나 먹어서 심의장이 들어갈 데가 없어진다(계산으로 확인했다).
        (WoongFolder, "W06_Radio_Booth",               "라디오부스",  "웅성관",  Wall.가운데, -4.8f,  0.8f,  90f, 3.40f, 3.00f),
        (WoongFolder, "W07_Tape_Editing_Desk",         "테이프편집대","웅성관",  Wall.가운데,  1.2f, -1.0f,   0f, 2.60f, 1.10f),
        (WoongFolder, "W01_Broadcast_Logbook_Stand",   "방송일지대",  "웅성관",  Wall.가운데,  4.4f,  1.2f, 200f, 2.20f, 0.90f),
        // 반송된 홍보물은 <b>구석에 쌓여</b> 있어야 «아무도 안 치운 것» 으로 읽힌다
        (WoongFolder, "W02_Returned_Publicity_Parcels","반송홍보물",  "웅성관",  Wall.가운데,  5.2f,  3.8f, 150f, 1.80f, 1.20f),

        // ── 곰머리관(인문·연구) : 1·2장 떡밥. 빠진 장부 한 권과 비어 있는 3년
        // ★ 자리를 <b>손으로 찍는다</b>(Wall.가운데). 이 방에는 유저 가구 열 점이 이미 서 있어서
        //   Seat 가 벽을 따라 자동으로 재면 <b>그 가구들과 겹친다</b> — 배치모드로 재서 잡았다
        //   (성찰칠판·강학대가 뒤벽을, 서책장·기록장이 왼벽을 이미 쓰고 있다).
        (StoryFolder, "S04_Visitor_Ledger_Shelf",      "관람객장부",   "곰머리관", Wall.가운데, -6.10f, -4.90f,   0f, 3.41f, 0.73f),
        (StoryFolder, "S06_Microfilm_Reader",          "필름열람기",   "곰머리관", Wall.가운데, -6.00f, -1.50f,  90f, 1.56f, 1.12f),

        // ── 곰테크관(공학·카트) : 「왜 카트인가」에 물리적 답을 주는 방
        (StoryFolder, "S08_Wooden_Controller_Cabinet", "조종기보관장", "곰테크관", Wall.뒤,   0f, 0f,   0f, 3.85f, 0.82f),
        // 정비 리프트는 <b>작업장 한가운데</b>다. 벽에 붙이면 깊이 3m 가 뒷벽 자리를 먹어서
        // 기계 열셋이 안 들어간다(계산으로 확인했다).
        (StoryFolder, "S07_Kart_Service_Lift",         "카트정비대",   "곰테크관", Wall.가운데, -3.6f, -2.0f, 90f, 3.85f, 3.00f),
        // 가운데 — 만들다 만 카트는 <b>길 한가운데</b>에 있어야 «작업 중» 으로 읽힌다
        (StoryFolder, "S09_Prototype_Kart_Frame",      "시제품카트",   "곰테크관", Wall.가운데, 2.0f, -2.3f, 200f, 2.66f, 2.66f),

        // ★ 2026-10-08 수연의 곰테크관 열 점. <b>이 방도 배율 1.0</b> —
        //   17 x 12m 에 기계 열셋이라 1.10 을 먹이면 지나다닐 길이 없다.
        (GomtechFolder, "T10_Game_Logic_Lab_Wall",    "게임로직벽",   "곰테크관", Wall.뒤,   0f, 0f,   0f, 3.10f, 1.05f),
        (GomtechFolder, "T07_Battery_Charging_Cabinet","충전함",      "곰테크관", Wall.뒤,   0f, 0f,   0f, 2.00f, 0.82f),
        (GomtechFolder, "T09_Vertical_Milling_Machine","수직밀링",    "곰테크관", Wall.왼,   0f, 0f,  90f, 1.60f, 1.15f),
        (GomtechFolder, "T05_Electronics_Workbench",  "전자작업대",   "곰테크관", Wall.왼,   0f, 0f,  90f, 2.60f, 1.20f),
        (GomtechFolder, "T02_Additive_Printer",       "3D프린터",     "곰테크관", Wall.오른, 0f, 0f, 270f, 1.45f, 1.10f),
        (GomtechFolder, "T06_Wheel_Balancer",         "휠밸런서",     "곰테크관", Wall.오른, 0f, 0f, 270f, 1.80f, 1.12f),
        (GomtechFolder, "T03_Robot_Training_Cell",    "로봇실습실",   "곰테크관", Wall.오른, 0f, 0f, 270f, 2.00f, 1.65f),
        // 가운데 — 시뮬레이터는 <b>문을 등지고</b> 앉는다(화면이 안쪽을 본다)
        (GomtechFolder, "T01_Driving_Simulator",      "주행시뮬",     "곰테크관", Wall.가운데,  4.9f, -1.8f, 180f, 1.60f, 2.60f),
        (GomtechFolder, "T08_Sensor_Test_Course",     "센서시험장",   "곰테크관", Wall.가운데, -3.0f,  1.6f,   0f, 3.10f, 1.95f),
        (GomtechFolder, "T04_Gear_Teaching_Stand",    "기어실습대",   "곰테크관", Wall.가운데,  3.2f,  2.2f,   0f, 2.70f, 1.05f),

        // ★ 2026-10-08 수연의 곰짝박수마당 열 점. <b>객석이 아니라 무대 뒤 장비</b>다 —
        //   접의자가 «카트에 실려» 있고 의상걸이와 플라이트 케이스가 있다. 그래서
        //   상자로 흉내 낸 객석 열다섯을 걷어냈다(진짜 물건이 온 이상 그건 짐이다).
        //   15 x 11m 라 <b>여기도 배율 1.0</b>.
        (GomjjakFolder, "P02_Choir_Risers",           "합창단",       "곰짝박수마당", Wall.뒤,   0f, 0f,   0f, 3.60f, 2.20f),
        (GomjjakFolder, "P01_Outdoor_Lighting_Tower", "조명타워",     "곰짝박수마당", Wall.뒤,   0f, 0f,   0f, 3.20f, 1.60f),
        (GomjjakFolder, "P03_Vocal_Performance_Set",  "보컬세트",     "곰짝박수마당", Wall.뒤,   0f, 0f,   0f, 2.50f, 1.50f),
        (GomjjakFolder, "P05_Acoustic_Guitar_Rack",   "기타걸이",     "곰짝박수마당", Wall.왼,   0f, 0f,  90f, 1.80f, 0.90f),
        (GomjjakFolder, "P07_Backstage_Costume_Rail", "의상걸이",     "곰짝박수마당", Wall.왼,   0f, 0f,  90f, 2.20f, 1.05f),
        (GomjjakFolder, "P08_Flight_Cases_Cable_Reel","장비케이스",   "곰짝박수마당", Wall.오른, 0f, 0f, 270f, 2.40f, 1.15f),
        (GomjjakFolder, "P10_Applause_Cue_Tower",     "박수신호탑",   "곰짝박수마당", Wall.오른, 0f, 0f, 270f, 1.25f, 1.05f),
        // 가운데 셋 — 무대 앞 펜스, 타악기, 실려 있는 접의자
        (GomjjakFolder, "P09_Crowd_Barrier_Pair",     "관객펜스",     "곰짝박수마당", Wall.가운데, -2.6f, -1.4f,   0f, 3.30f, 0.80f),
        (GomjjakFolder, "P04_Janggu_Jing_Station",    "장구징대",     "곰짝박수마당", Wall.가운데,  2.8f, -1.3f,   0f, 2.30f, 1.15f),
        (GomjjakFolder, "P06_Folding_Chair_Cart",     "접의자수레",   "곰짝박수마당", Wall.가운데, -4.3f,  1.8f, 250f, 1.30f, 1.35f),

        // ── 참잘했어요관(시상) : 한 자리만 비어 있다. 끝까지 설명하지 않는다
        // ★ <b>연혁판이 여기로 왔다</b>(곰머리관이 꽉 차서). 오히려 이게 맞는 자리다 —
        //   <b>둘 다 «비어 있는 것» 에 대한 물건</b>이라 한 방에 서면 서로를 설명한다:
        //   트로피 한 자리가 비고, 연혁 최근 3년이 비었다.
        (StoryFolder, "S10_Missing_Trophy_Cabinet",    "트로피진열장", "참잘했어요관", Wall.뒤, 0f, 0f, 0f, 4.59f, 0.89f),
        (StoryFolder, "S05_Museum_History_Wall",       "박물관연혁",   "참잘했어요관", Wall.뒤, 0f, 0f, 0f, 4.92f, 1.05f),

        // ★ 2026-10-08 수연의 시상관 열 점. <b>이 방만 배율이 1.0</b>(StoryHall 호출 참고) —
        //   16 × 12m 에 시상대가 5.8m 라, 1.10 을 먹이면 가운데에 길이 안 남는다.
        //
        //   ★ <b>앞벽은 비운다.</b> 거기가 문이고, <see cref="Seat"/> 는 문이 어디인지 모른다 —
        //     벽을 따라 고르게 펴다가 <b>출입구 한가운데</b>에 소품을 세운다(곰누리관과 같은 판단).
        (AwardsFolder, "A02_Ceremonial_Lectern",   "시상연단",     "참잘했어요관", Wall.왼,   0f, 0f,  90f, 1.25f, 0.95f),
        (AwardsFolder, "A09_Honor_Banner_Stand",   "명예현수막",   "참잘했어요관", Wall.왼,   0f, 0f,  90f, 2.60f, 0.80f),
        (AwardsFolder, "A03_Medal_Display_Wall",   "메달진열벽",   "참잘했어요관", Wall.오른, 0f, 0f, 270f, 3.30f, 0.65f),
        (AwardsFolder, "A04_Certificate_Screen",   "상장게시판",   "참잘했어요관", Wall.오른, 0f, 0f, 270f, 3.60f, 0.80f),

        // 가운데 여섯 — <b>시상대가 문을 마주 본다.</b> 들어오면 정면이 단상이라야 시상관이다.
        (AwardsFolder, "A01_Ceremony_Dais",        "시상대",       "참잘했어요관", Wall.가운데, -1.0f, -2.6f,   0f, 5.80f, 2.50f),
        // 월계 아치는 단상 앞 <b>기념 촬영 자리</b>. 문 정면(|x| &lt; 2.2)은 비워 둔다
        (AwardsFolder, "A06_Laurel_Photo_Arch",    "월계아치",     "참잘했어요관", Wall.가운데, -1.0f,  0.4f,   0f, 3.50f, 0.90f),
        (AwardsFolder, "A05_Presentation_Table",   "수여탁자",     "참잘했어요관", Wall.가운데,  4.6f, -2.8f,   0f, 2.60f, 1.05f),
        (AwardsFolder, "A08_Modular_Display_Island","전시섬",      "참잘했어요관", Wall.가운데,  3.6f,  1.6f,   0f, 3.20f, 1.80f),
        // 벤치는 등받이가 문 쪽 — 앉으면 단상을 본다
        (AwardsFolder, "A07_Exhibition_Bench",     "관람벤치",     "참잘했어요관", Wall.가운데, -3.6f,  2.1f, 180f, 3.60f, 0.95f),
        (AwardsFolder, "A10_Exhibition_Guide_Kiosk","안내키오스크", "참잘했어요관", Wall.가운데,  5.3f,  4.2f, 200f, 1.40f, 0.85f),

        // ── 곰누리관(관광·외국어) : <b>«왜 하필 이 땅인가» 에 답하는 유일한 방</b>이다.
        //   골든베어는 리조트, 즉 <b>관광 개발</b>이라 그 답을 가질 수 있는 과가 여기뿐이야.
        //   ★ 상황판의 숫자가 1장의 «열 배 차이» 와 이어진다 — <see cref="TourismTarget"/> 참고.
        (GomnuriFolder, "N01_Tourism_Target_Board",      "관광상황판",   "곰누리관", Wall.뒤,    0f,    0f,    0f, 4.96f, 1.05f),
        (GomnuriFolder, "N09_International_Culture_Wall","문화전시벽",   "곰누리관", Wall.뒤,    0f,    0f,    0f, 5.21f, 1.19f),
        (GomnuriFolder, "N03_Sister_Museum_Map",         "자매박물관지도","곰누리관", Wall.왼,    0f,    0f,   90f, 5.08f, 1.05f),
        (GomnuriFolder, "N04_Foreign_Guide_Credenza",    "안내책자대",   "곰누리관", Wall.왼,    0f,    0f,   90f, 3.09f, 0.96f),
        (GomnuriFolder, "N07_Travel_Luggage_Rack",       "여행가방장",   "곰누리관", Wall.오른,  0f,    0f,  270f, 3.50f, 1.21f),
        (GomnuriFolder, "N10_Language_Listening_Booth",  "청취부스",     "곰누리관", Wall.오른,  0f,    0f,  270f, 3.31f, 2.02f),
        // 가운데 셋 — 문에서 들어오는 길(가운데)을 비우려고 지구본을 안쪽으로 물렸다
        (GomnuriFolder, "N06_Grand_Globe_Display",       "대형지구본",   "곰누리관", Wall.가운데,  0.0f, -2.6f,   0f, 2.40f, 2.23f),
        (GomnuriFolder, "N08_Tourism_Route_Diorama",     "관광동선모형", "곰누리관", Wall.가운데, -4.8f,  1.2f,   0f, 4.25f, 2.57f),
        (GomnuriFolder, "N05_Translation_Workstation",   "번역작업대",   "곰누리관", Wall.가운데,  4.8f,  1.2f, 180f, 3.35f, 2.46f),
        // 입간판은 <b>들어오자마자 보이는 자리</b>에. 문 정면(x 0)은 비워 둔다
        (GomnuriFolder, "N02_Past_Community_Meeting",    "주민설명회판", "곰누리관", Wall.가운데,  3.6f,  4.6f,  200f, 2.14f, 1.44f),
    };

    /// <summary>
    /// 한 관의 몫만 골라 세운다. 네 관이 같은 표를 나눠 쓰니 <b>표 한 줄만 옮기면</b>
    /// 소품이 다른 관으로 이사한다 — 관마다 표를 따로 두면 그때 반드시 하나가 어긋난다.
    /// </summary>
    void StoryHall(Transform t, string hall, float w, float d, float scale = StoryScale, float seed = StorySignSeed)
    {
        var rows = new System.Collections.Generic.List<int>();
        for (int i = 0; i < StoryLayout.Length; i++)
            if (StoryLayout[i].hall == hall) rows.Add(i);
        if (rows.Count == 0) return;

        var box = new (Wall, float, float, float, float)[rows.Count];
        for (int i = 0; i < rows.Count; i++)
        {
            var p = StoryLayout[rows[i]];
            box[i] = (p.wall, p.x, p.z, p.w, p.d);
        }
        var at = Seat(box, hall, w, d, scale);

        // 건물이 yaw 로 돌아가 있으면 <b>월드 AABB 폭이 실제 폭보다 넓게</b> 잡힌다.
        // MyModel 은 그 AABB 로 배율을 맞추니, 돌아간 만큼을 미리 먹여 보낸다.
        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var placed = new Transform[rows.Count];
        int made = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            var p = StoryLayout[rows[i]];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);
            placed[i] = MyModel(t, p.folder + p.file + ".fbx", "In_" + p.label,
                                at[i], seen * scale);
            if (placed[i] != null) made++;
        }

        // ★ 앞면은 <b>씬에서 잰다.</b> 블렌더 축이 유니티에서 어느 쪽이 되는지는 이 프로젝트에서
        // 두 번 틀렸다. 관마다 따로 재지 않고 <see cref="storySign"/> 에 캐시해서
        // <b>열 점이 같은 방향</b>을 쓰게 한다 — 신호가 약한 방(참잘했어요관은 소품이 하나뿐)이
        // 혼자 거꾸로 서는 걸 막는다.
        // ★ 묶음마다 <b>제 씨앗</b>에서 시작한다. 곰누리관은 한 관에 열 점이 다 모여 있어서
        //   (지도 핀 6개 · 여행가방 3개) 신호가 세니 <b>0 에서 재도 안전</b>하다.
        // ★★ 2026-10-07 — <b>앞면 신호는 묶음(폴더)마다 따로 잰다.</b>
        // 웅성관에 수연의 새 열 점이 들어오면서 <b>한 방에 두 묶음</b>이 섞였는데,
        // 블렌더 익스포트가 다르면 축도 다를 수 있다 — 하나로 묶어 재면 센 쪽이 약한 쪽을
        // 끌고 가서 <b>한 묶음이 통째로 등을 돌린다</b>(곰머리관에서 이미 그렇게 틀렸다).
        var sign = new System.Collections.Generic.Dictionary<string, float>();
        for (int i = 0; i < rows.Count; i++)
        {
            if (placed[i] == null) continue;
            string folder = StoryLayout[rows[i]].folder;
            // 씨앗은 <b>StoryHalls 묶음만</b> 쓴다. 그 묶음은 관마다 한두 점뿐이라 신호가 약해서
            // 실측값에서 출발해야 하고, 다른 묶음은 한 방에 다 모여 있어 0 에서 재도 안전하다.
            float cur = sign.TryGetValue(folder, out var v) ? v
                      : (folder == StoryFolder ? seed : 0f);
            float e = FrontEvidence(placed[i], StoryMarks);
            if (Mathf.Abs(e) > Mathf.Abs(cur)) cur = e;
            sign[folder] = cur;
        }
        if (seed != 0f && sign.TryGetValue(StoryFolder, out var ss)) storySign = ss;

        for (int i = 0; i < rows.Count; i++)
        {
            if (placed[i] == null) continue;
            var p = StoryLayout[rows[i]];
            float extra = sign.TryGetValue(p.folder, out var g) && g < 0f ? 180f : 0f;
            storyExtra[p.folder] = extra;
            placed[i].localRotation = Quaternion.Euler(0f, p.yaw + extra, 0f) * placed[i].localRotation;
        }

        foreach (var kv in sign)
            Debug.Log($"[{hall}] {kv.Key.Split('/')[^2]} 묶음 · 앞면 신호 {kv.Value:+0.000;-0.000}m → " +
                      $"{(kv.Value < 0f ? "180도 돌려" : "그대로")} 세웠다");
        Debug.Log($"[{hall}] 이야기 소품 {made}/{rows.Count}");
    }

    /// <summary>
    /// 묶음 전체가 같은 방향을 쓰게 들고 있는 값.
    ///
    /// ★★ <b>0 에서 시작하면 안 된다.</b> 관을 짓는 순서가 곰머리관 → 곰테크관인데,
    /// 곰머리관 몫은 장부 선반(<c>Ledgers</c>, 쏠림 0.12)뿐이라 <b>신호가 약하다</b> —
    /// 그 약한 값으로 세워 버리고, 뒤에 오는 곰테크관이 −1.01 을 읽어 180도로 바꾼다.
    /// 결과는 <b>곰머리관 둘만 거꾸로</b>. 배치모드로 재서 잡았다.
    ///
    /// 그래서 <b>실측값에서 시작</b>한다(조종기 보관장 −1.010m). 더 센 값이 나오면 그때 바뀌니,
    /// 모델을 다시 내보내서 축이 뒤집혀도 로그에 그 값이 찍힌다.
    /// </summary>
    const float StorySignSeed = -1.010f;

    float storySign = StorySignSeed;

    /// <summary>묶음마다 «180도 돌렸나». <see cref="OnTop"/> 이 책상 위 자리를 같이 돌릴 때 쓴다.</summary>
    readonly System.Collections.Generic.Dictionary<string, float> storyExtra = new();

    /// <summary>
    /// ★ <b>책상 위에 얹는다.</b> 20~50cm 짜리 소품을 바닥에 두면 «흘린 쓰레기» 로 보이고,
    /// 웅성관의 증거 셋(원본 테이프 · 팩스 보고서 · 출입증)은 <b>누군가 놓아둔 것</b>이어야 한다.
    ///
    /// 높이는 <b>받침이 될 물건에서 읽지 않는다</b> — 그 물건의 바운즈 꼭대기는 모니터나
    /// 펼친 장부라서, 거기 얹으면 공중에 뜬다. <paramref name="surfaceY"/> 로
    /// <b>실측한 상판 높이</b>를 직접 준다(편집대 0.86 · 일지대 1.00, 블렌더에서 쟀다).
    ///
    /// 자리는 받침의 <b>제 좌표계</b>로 적는다. 받침이 180도 돌아 서면 얹힌 것도 같이 돈다.
    /// </summary>
    void OnTop(Transform t, string host, string asset, string label,
               Vector2 localOff, float surfaceY, float width, float yaw)
    {
        var h = t.Find("In_" + host);
        if (h == null) { Debug.LogWarning($"[웅성관] 받침 'In_{host}' 가 없어 '{label}' 을 못 얹었다."); return; }

        float extra = storyExtra.TryGetValue(WoongFolder, out var e) ? e : 0f;
        float a = (yawOf(host) + extra) * Mathf.Deg2Rad;
        float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
        // 유니티 y 회전: (x, z) → (x·cos + z·sin, −x·sin + z·cos)
        var off = new Vector3(localOff.x * cs + localOff.y * sn, 0f,
                             -localOff.x * sn + localOff.y * cs) * StoryScale;

        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));
        float seen = width * Mathf.Max(cw + sw * 0.6f, sw + cw * 0.6f);

        var m = MyModel(t, asset, "In_" + label,
                        h.localPosition + off + Vector3.up * (surfaceY * StoryScale),
                        seen * StoryScale);
        if (m != null)
            m.localRotation = Quaternion.Euler(0f, yawOf(host) + extra + yaw, 0f) * m.localRotation;
    }

    /// <summary>자리표에 적어 둔 그 소품의 yaw. 받침 위에 뭘 얹을 때 같이 돌리려고 본다.</summary>
    static float yawOf(string label)
    {
        foreach (var p in StoryLayout) if (p.label == label) return p.yaw;
        return 0f;
    }
    const string LeadershipFolder = "Assets/My blender/Leadership/";

    /// <summary>
    /// 웅지관 소품 배율. 36 × 22m 는 이 캠퍼스에서 제일 큰 방이다.
    /// <b>계산으로 잰 한계는 1.34</b>(오른쪽 벽이 병목) — 여유를 두고 1.30 을 쓴다.
    /// </summary>
    const float LeadershipScale = 1.30f;

    /// <summary>
    /// 웅지관 소품 — 파일 · 이름 · 자리(x, z) · <b>앞면이 향할 방향</b> · 실제 폭(m) ·
    /// <paramref name="flip"/>(이 모델만 앞뒤가 반대일 때).
    ///
    /// ★ <b>접수대만 `flip` 이다.</b> 이 묶음에서 «앞면에만 있는 부품» 을 재 보면
    /// 기록장 문 −0.38 · 투표함 −0.56 · 예산 막대 −0.16 으로 <b>−Y 가 앞면</b>인데,
    /// 접수대의 창구(`Window_1~3`)만 <b>+0.11 로 반대쪽</b>에 있다. 창구 구멍은
    /// 정의상 <b>손님 쪽</b>이라 그게 이 모델의 앞면이다 —
    /// 곰밥마당 표본상자 하나만 방향이 달랐던 것과 같은 경우야(2026-09-23).
    /// 혹시 접수대가 등을 돌리고 서 있으면 <b>이 `true` 하나만 지우면 된다.</b>
    /// </summary>
    static readonly (string file, string label, Wall wall, float x, float z,
                     float yaw, float w, float d, bool flip)[]
        LeadershipLayout =
    {
        // 입구 — 들어오면 행정 창구. 손님 쪽(문)을 본다
        ("03_Administration_Counter",  "행정접수대", Wall.앞,   0f, 0f,   0f, 5.85f, 1.160f, true),
        // 왼쪽 벽 — 의사결정
        ("01_Council_Dais",            "의회단상",  Wall.왼,   0f, 0f,  90f, 6.10f, 2.850f, false),
        ("10_Leadership_Speech_Stage", "연설무대",  Wall.왼,   0f, 0f,  90f, 4.62f, 2.345f, false),
        // 오른쪽 벽 — 경제와 기록
        //
        // ★ 2026-09-29 <b>예산현황판을 여기서 뒷벽으로 옮겼다.</b> 오른쪽 세 점이
        // 14.37m 라 <b>배율의 병목</b>이었다(한계 1.03). 둘로 줄이니 1.34 까지 올라간다.
        // 모의선거 옆에 붙으니 「행정 실습」 으로 읽히기도 더 낫다.
        ("05_Grand_Records_Wall",      "문서보관벽", Wall.오른, 0f, 0f, 270f, 5.22f, 0.891f, false),
        ("06_Economy_Abacus",          "경제주판",   Wall.오른, 0f, 0f, 270f, 4.24f, 1.100f, false),
        // 뒷벽 — 집무와 실습
        ("09_Executive_Desk",          "집무책상",   Wall.뒤,   0f, 0f,   0f, 4.14f, 2.355f, false),
        ("08_Election_Practice_Station","모의선거",  Wall.뒤,   0f, 0f,   0f, 4.92f, 2.054f, false),
        ("04_Budget_Planning_Wall",    "예산현황판", Wall.뒤,   0f, 0f,   0f, 4.91f, 1.180f, false),
        // 가운데 — 회의와 정책 모형. 여기만 자리를 손으로 적는다(벽이 안 잡아 주니까).
        // 문에서 들어오는 길이 x −3 ~ +3 으로 남게 둘을 양옆으로 벌려 뒀다.
        ("02_Conference_Table_Set",    "협의탁자",  Wall.가운데, -6.0f, 1.2f, 0f, 4.70f, 3.090f, false),
        ("07_Campus_Policy_Model",     "정책모형대", Wall.가운데,  6.2f, 1.2f, 0f, 4.55f, 2.472f, false),
    };

    /// <summary>앞면에만 있는 부품. <b>의자와 칸막이 뒤판은 빼야 한다</b> — 그건 뒷면 표식이다.</summary>
    static readonly string[] LeadershipMarks = { "Ballot_Chest", "Archive_Door", "Budget_Bar", "Window" };

    /// <summary>
    /// 웅지관(본관) — 리더십·경영·행정. 시우가 공부하는 방이다.
    /// 유저가 만든 대형 소품 열 점(<b>7,195쿼드 · 100% 쿼드 · 전부 바닥 원점 · 실물 크기</b>).
    ///
    /// 36 × 22m 는 이 캠퍼스에서 제일 큰 방이라 <b>벽에 붙이는 것만으로는 가운데가 빈다.</b>
    /// 그래서 네 구역으로 나눴다 — <b>입구(창구) · 왼쪽 벽(의사결정) · 오른쪽 벽(경제·기록) ·
    /// 뒷벽(집무·실습)</b>, 그리고 <b>가운데에 회의 탁자와 정책 모형대</b>.
    /// 가운데에 두 개를 놓는 게 핵심이야: 벽에만 붙이면 «복도» 가 되고,
    /// 가운데에 앉을 자리가 있으면 <b>«회의하는 방»</b> 이 된다.
    ///
    /// 문 앞(|x| &lt; 4.0 · z &gt; 7.0)은 비운다 — 본관 문은 6.4m 로 제일 넓다.
    /// </summary>
    void Leadership(Transform t, float w, float d, float h)
    {
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;   // Hollow 벽은 0.6m 두께다

        // ---- 유저 소품 열 점 ----
        // 자리는 <see cref="Seat"/> 가 배율에 맞춰 잡는다 — 손으로 적힌 건 가운데 둘뿐이다.
        var box = new (Wall, float, float, float, float)[LeadershipLayout.Length];
        for (int i = 0; i < LeadershipLayout.Length; i++)
        {
            var p = LeadershipLayout[i];
            box[i] = (p.wall, p.x, p.z, p.w, p.d);
        }
        var at = Seat(box, "웅지관", w, d, LeadershipScale);

        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var placed = new Transform[LeadershipLayout.Length];
        int made = 0;
        for (int i = 0; i < LeadershipLayout.Length; i++)
        {
            var p = LeadershipLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);
            placed[i] = MyModel(t, LeadershipFolder + p.file + ".fbx", "In_" + p.label,
                                at[i], seen * LeadershipScale);
            if (placed[i] != null) made++;
        }

        // ★ 앞면은 <b>씬에서 잰다.</b> 블렌더 축이 유니티에서 어느 쪽이 되는지는
        // 이 프로젝트에서 두 번 틀렸다(곰이 누움 · 스피커가 뒤돎). 철곰관과 같은 방식으로,
        // <b>제일 뚜렷한 신호 하나</b>로 묶음 전체의 방향을 정한다.
        float best = 0f;
        foreach (var m in placed)
        {
            if (m == null) continue;
            float e = FrontEvidence(m, LeadershipMarks);
            if (Mathf.Abs(e) > Mathf.Abs(best)) best = e;
        }
        float extra = best >= 0f ? 0f : 180f;

        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, LeadershipLayout[i].yaw + extra
                                       + (LeadershipLayout[i].flip ? 180f : 0f), 0f)
                    * placed[i].localRotation;

        if (Mathf.Abs(best) < 0.01f)
            Debug.LogWarning("[웅지관] 소품 앞면을 못 쟀다 — 부품 이름(Ballot_Chest·Archive_Door…)이 " +
                             "바뀌었는지 확인해라. 일단 안 돌리고 세운다.");
        else
            Debug.Log($"[웅지관] 소품 {made}/10 · 앞면 신호 {best:+0.000;-0.000}m → " +
                      $"{(extra > 0f ? "180도 돌려" : "그대로")} 세웠다");

        // ---- 바닥 — 가운데를 «회의 구역» 으로 묶는다 ----
        // 큰 방에서 가운데 가구 둘이 그냥 놓여 있으면 «치우다 만 것» 으로 보인다.
        // 바닥에 테두리를 그려 주면 <b>그 자리가 하나의 구역</b>이 된다(실제 관청이 그렇게 한다).
        for (int s = -1; s <= 1; s += 2)
        {
            Block(t, $"InZoneX_{s}", new Vector3(s * 8.5f, 0.012f, 1.2f), Quaternion.identity,
                  new Vector3(0.14f, 0.02f, 8.4f), ColTrimDark, noCollider: true);
            Block(t, $"InZoneZ_{s}", new Vector3(0f, 0.012f, 1.2f + s * 4.2f), Quaternion.identity,
                  new Vector3(17.0f, 0.02f, 0.14f), ColTrimDark, noCollider: true);
        }

        // 입구에서 창구까지 이어지는 안내선 — 처음 온 사람이 어디로 갈지 글자 없이 안다
        for (int i = 0; i < 7; i++)
            Block(t, $"InGuide_{i}", new Vector3(-1.2f - i * 1.4f, 0.012f, 9.4f - i * 0.12f),
                  Quaternion.identity, new Vector3(0.9f, 0.02f, 0.12f), ColMapleGold, noCollider: true);

        // ---- 벽 마감 — 모서리 기둥과 굽도리 ----
        // 13m 짜리 통짜 크림색 벽 넉 장은 레고 상자의 정체 그대로다.
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.15f), h * 0.5f, sz * (inZ - 0.15f)),
                      Quaternion.identity, new Vector3(0.30f, h, 0.30f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.48f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.96f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.48f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.96f, 0.10f), ColWallFoot, noCollider: true);

        // ---- 천장 — 대들보와 서까래 ----
        // 본관은 <b>원래 높다</b>(대강당). 철곰관과 같은 판단으로 반자를 안 내리고,
        // 대신 층을 얹어 «아직 안 지은 방» 으로 안 보이게 한다.
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InGirder_{s}", new Vector3(s * 8.4f, h - 0.70f, 0f), Quaternion.identity,
                  new Vector3(0.44f, 0.44f, d - 1.6f), ColWoodRail, noCollider: true);
        for (int i = 0; i < 11; i++)
            Block(t, $"InRafter_{i}", new Vector3(0f, h - 0.32f, -8.5f + i * 1.7f), Quaternion.identity,
                  new Vector3(w - 1.6f, 0.22f, 0.18f), ColWood, noCollider: true);

        // 주련 — 행정동이라도 한옥이다. 기둥에 세로 글판이 없으면 사무실로 보인다
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InPillarSlip_{s}", new Vector3(s * (inX - 0.32f), 4.2f, -inZ + 2.6f),
                  Quaternion.identity, new Vector3(0.06f, 3.4f, 0.46f), ColCream, noCollider: true);

        SteelDoor(t, inX);
    }

    /// <summary>
    /// ★★ 2026-10-06 <b>웅지관 안쪽, 끝까지 안 열리는 철제 문.</b>
    ///
    /// 행정동을 가리키는 <b>세 번째 신호</b>다 — 딱지가 안 붙고, 판자도 안 박히고,
    /// 밤새 불이 켜져 있고, 그리고 <b>안에 안 열리는 문이 하나 있다.</b>
    ///
    /// <b>한옥 건물에 철제 문</b>인 게 요점이다. 캠퍼스의 다른 문 열다섯은 전부
    /// 나무 장지문인데 여기만 철판이야 — 나중에 끼워 넣은 것이고, 나중에 끼울 만한
    /// 이유가 있었다는 뜻이다. 글자로는 한 마디도 설명 안 한다.
    ///
    /// 자리는 <b>왼쪽 벽 뒤쪽 구석</b>이다. <see cref="Seat"/> 가 왼벽 소품을
    /// z −6.79 부터 놓으니 −10.4 ~ −6.79 가 비어 있다 — 거기 3.6m 를 쓴다.
    /// <see cref="HingedDoor.lockedNote"/> 가 차 있으면 <see cref="HingedDoor.Start"/> 가
    /// 문짝을 안 찾고 돌아가므로 경고도 안 뜬다.
    /// </summary>
    void SteelDoor(Transform hall, float inX)
    {
        var door = new GameObject("SteelDoor").transform;
        door.SetParent(hall, false);
        door.localPosition = new Vector3(-inX + 0.10f, 0f, -8.6f);
        door.localRotation = Quaternion.Euler(0f, 90f, 0f);   // 로컬 +Z 가 방 안쪽을 본다

        // 문틀 — 틀이 있어야 «뚫린 데» 로 읽힌다(벽에 칠한 자국으로 안 보이게)
        for (int s = -1; s <= 1; s += 2)
            Block(door, $"Jamb_{s}", new Vector3(s * 1.04f, 1.38f, 0.06f), Quaternion.identity,
                  new Vector3(0.16f, 2.76f, 0.30f), ColTrimDark, noCollider: true);
        Block(door, "Lintel", new Vector3(0f, 2.84f, 0.06f), Quaternion.identity,
              new Vector3(2.24f, 0.20f, 0.30f), ColTrimDark, noCollider: true);
        Block(door, "Sill", new Vector3(0f, 0.05f, 0.08f), Quaternion.identity,
              new Vector3(2.24f, 0.10f, 0.34f), ColStoneWall, noCollider: true);

        // 철판 두 짝. 금속 마감이라 나무 장지문 옆에서 <b>재질부터 다르게</b> 보인다
        for (int s = -1; s <= 1; s += 2)
        {
            Block(door, $"Leaf_{s}", new Vector3(s * 0.47f, 1.40f, 0.10f), Quaternion.identity,
                  new Vector3(0.94f, 2.60f, 0.08f), ColRock, noCollider: true);
            // 가로 보강대 — 민짜 철판은 벽처럼 보인다
            for (int k = 0; k < 2; k++)
                Block(door, $"Rib_{s}_{k}", new Vector3(s * 0.47f, 0.85f + k * 1.10f, 0.15f),
                      Quaternion.identity, new Vector3(0.86f, 0.10f, 0.04f), ColBearDark,
                      noCollider: true);
        }

        Block(door, "Handle_L", new Vector3(-0.10f, 1.25f, 0.17f), Quaternion.identity,
              new Vector3(0.06f, 0.44f, 0.06f), ColBearDark, noCollider: true);
        Block(door, "Handle_R", new Vector3(0.10f, 1.25f, 0.17f), Quaternion.identity,
              new Vector3(0.06f, 0.44f, 0.06f), ColBearDark, noCollider: true);

        // 잠금 걸쇠 — 밖에서 걸어 잠근 게 아니라 <b>안에서</b> 잠겼다는 건 대사 몫이다.
        // 여기서는 «잠겨 있다» 만 보이면 된다
        Block(door, "Latch", new Vector3(0f, 1.25f, 0.16f), Quaternion.identity,
              new Vector3(0.30f, 0.16f, 0.07f), ColRock, noCollider: true);

        var hinged = door.gameObject.AddComponent<HingedDoor>();
        hinged.label = "철제 문";
        hinged.lockedNote = "안에서 잠겨 있다";
        hinged.boardable = false;
        hinged.canClose = false;
    }

    const string CouncilFolder = "Assets/My blender/StudentCouncil/";

    /// <summary>
    /// 곰생회관 소품 배율. 방이 17 × 12m(안쪽 15.8 × 10.8)로 <b>이 캠퍼스에서 작은 편</b>인데
    /// 열 점의 가로를 다 더하면 24.6m 라, 계산상 상한이 1.11 이다(뒷벽이 병목).
    ///
    /// 그런데 <b>키울 이유가 애초에 없다.</b> 여기 물건은 전부 <b>사람이 쓰는 가구</b>라
    /// 치수가 고정돼 있고, 그래서 보는 사람이 <b>방 크기를 재는 자</b>로 쓴다 —
    /// 전시실에서 벤치와 신발장을 안 키운 것과 같은 기준이야(간판 성격인 물건만 키운다).
    /// </summary>
    const float CouncilScale = 1.00f;

    /// <summary>
    /// 곰생회관 소품 — 파일 · 이름 · 벽 · 자리(가운데일 때만) · 앞면이 향할 방향 · 실제 폭·깊이(m).
    ///
    /// 네 구역이다 — <b>앞벽(안내와 대기) · 뒷벽(정보와 살림) · 왼벽(발언) · 오른벽(접수와 안내)</b>,
    /// 그리고 <b>가운데에 회의 탁자</b>. 가운데를 비우면 «복도» 고, 앉을 자리가 있어야
    /// <b>«회의하는 방»</b> 이 된다(웅지관에서 배운 것).
    ///
    /// 앞벽 둘은 <b>문 양옆</b>으로 갈라진다 — <see cref="Seat"/> 가 n개를 벽 양끝에 붙이고
    /// 남는 길이를 가운데로 몰아 주기 때문에, 문(폭 3.6m)이 저절로 비워진다.
    /// 측정: 안내데스크 오른끝 −2.30 · 벤치 왼끝 +3.86 → 문턱(±1.80)에서 0.50m / 2.06m.
    /// </summary>
    static readonly (string file, string label, Wall wall, float x, float z,
                     float yaw, float w, float d)[]
        CouncilLayout =
    {
        // 앞벽(문) — 들어오면 왼쪽이 안내, 오른쪽이 기다리는 자리
        ("SU_01_Information_Desk",     "안내데스크", Wall.앞,   0f,  0f,  180f, 3.300f, 1.170f),
        ("SU_04_Waiting_Bench",        "대기벤치",   Wall.앞,   0f,  0f,  180f, 2.650f, 0.680f),
        // 뒷벽 — 들어와서 마주 보는 면. 정보와 살림을 한 줄로 모은다
        ("SU_02_Notice_Board",         "공지게시판", Wall.뒤,   0f,  0f,    0f, 2.448f, 0.700f),
        ("SU_05_Mail_Station",         "우편수령함", Wall.뒤,   0f,  0f,    0f, 2.180f, 0.699f),
        ("SU_07_Supply_Cabinet",       "물품수납장", Wall.뒤,   0f,  0f,    0f, 2.220f, 0.754f),
        ("SU_09_Tea_Station",          "차물코너",   Wall.뒤,   0f,  0f,    0f, 2.230f, 0.815f),
        // 왼벽 — 발언하는 자리. 뒤쪽에 서서 탁자를 내려다본다
        ("SU_06_Event_Stage",          "행사연단",   Wall.왼,   0f,  0f,   90f, 3.220f, 1.755f),
        // 오른벽 — 안쪽이 접수, 문 가까운 쪽이 안내 지도
        ("SU_08_Suggestion_Reception", "건의접수대", Wall.오른, 0f,  0f,  270f, 1.750f, 0.840f),
        ("SU_10_Village_Map",          "안내지도대", Wall.오른, 0f,  0f,  270f, 2.091f, 0.680f),
        // 가운데 — 회의 탁자와 의자 여섯. 문에서 3.2m 떨어져 있어 들어오는 길을 안 막는다
        ("SU_03_Meeting_Set",          "회의세트", Wall.가운데, 0f, -0.4f,   0f, 2.620f, 2.490f),
    };

    /// <summary>
    /// 곰생회관 가구 열 점의 <b>앞면 쏠림</b> — 블렌더 헤드리스로 미리 재 둔 값이다.
    /// 양수면 +Z 가 앞이라 안 돌려도 된다.
    ///
    /// ★★ <b>처음에는 런타임에 <c>mesh.vertices</c> 로 쟀는데 그게 틀렸다.</b>
    /// 정점은 임포터의 <c>Read/Write Enabled</c> 가 꺼져 있으면 <b>런타임에 못 읽는다</b> —
    /// 트랙 씬은 <c>buildOnAwake</c> 라 캠퍼스를 <b>실행 중에</b> 짓기 때문에 여기가 런타임 코드고,
    /// 모델 수만큼 «Not allowed to access vertices on mesh» 가 쏟아졌다.
    /// (2026-09-30 따라 그리기에서 이미 겪은 함정인데 또 밟았다.)
    ///
    /// 다시 재려면: 블렌더에서 FBX 열 점을 불러 <c>평균(y) − (min(y)+max(y))/2</c> 를 합한다.
    /// 블렌더 +Y 가 유니티 +Z 다. 실측(2026-10-06):
    /// <code>
    /// 안내데스크 +0.049  공지게시판 +0.086  회의세트 +0.003  대기벤치 −0.071
    /// 우편수령함 +0.116  행사연단 −0.039  물품수납장 +0.084  건의접수대 +0.022
    /// 차물코너  +0.103  안내지도대 +0.081          합계 +0.384
    /// </code>
    /// 벤치만 등받이 때문에 뒤로 쏠리는데 <b>한 점의 사고를 아홉이 덮는다.</b>
    /// </summary>
    const float CouncilSkew = +0.384f;

    /// <summary>
    /// 부품 이름이 없는 묶음의 앞면 — <b>정점 무게중심</b>으로 잰다.
    /// <b>에디터 전용</b>(위 <see cref="CouncilSkew"/> 설명 참고). 런타임에서 부르지 마라.
    ///
    /// ★ 이 열 점은 <b>한 파일이 메시 한 덩이</b>라(블렌더에서 합쳐서 내보냈다)
    /// <see cref="FrontEvidence"/> 가 쓰는 «앞면에만 있는 부품» 이 아예 없다.
    /// 대신 <b>앞쪽에 기하가 더 많다</b>는 성질을 쓴다 — 수령함의 칸막이, 게시판의 판,
    /// 수납장의 문, 지도대의 지도는 전부 앞면에 붙어 있다.
    ///
    /// 반환값은 <b>정점 무게중심 − 바운즈 한가운데</b>의 부모(건물) 기준 z. 양수면 +Z 가 앞.
    /// 월드로 재면 안 된다 — 건물이 yaw 로 돌아가 있어서 거짓말한다(네 번 걸린 함정).
    /// </summary>
#if UNITY_EDITOR
    static float FrontSkew(Transform m)
    {
        if (m == null || m.parent == null || Application.isPlaying) return 0f;

        double sum = 0.0; int n = 0;
        float lo = float.MaxValue, hi = float.MinValue;

        foreach (var mf in m.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;
            var vs = mesh.vertices;                       // 에디터 전용 — 런타임에는 안 읽힌다
            for (int i = 0; i < vs.Length; i++)
            {
                float z = m.parent.InverseTransformPoint(mf.transform.TransformPoint(vs[i])).z;
                sum += z; n++;
                if (z < lo) lo = z;
                if (z > hi) hi = z;
            }
        }
        if (n == 0) return 0f;
        return (float)(sum / n) - (lo + hi) * 0.5f;
    }
#endif

    /// <summary>
    /// 곰생회관 — 학생회. 유저가 만든 가구 열 점(<b>6,238쿼드 · 15,859 tris · 전부 바닥 원점 ·
    /// 실물 크기 · 텍스처 0</b>)으로 짓는다.
    ///
    /// ★ <b>앞면은 묶음 열 점을 합쳐서 정한다.</b> 한 점만 보면 거짓말하는 게 섞여 있어서다 —
    /// 블렌더에서 재 보면 벤치만 <b>등받이 때문에 −0.10</b> 으로 뒤로 쏠린다(나머지 아홉은
    /// 앞으로). 합을 보면 +0.38m 로 뚜렷하니, <b>한 점의 사고를 아홉이 덮는다.</b>
    /// 철곰관·웅지관이 «제일 센 신호 하나» 를 쓴 것보다 이쪽이 안전하다.
    /// </summary>
    void StudentCouncil(Transform t, float w, float d, float h)
    {
        var box = new (Wall, float, float, float, float)[CouncilLayout.Length];
        for (int i = 0; i < CouncilLayout.Length; i++)
        {
            var p = CouncilLayout[i];
            box[i] = (p.wall, p.x, p.z, p.w, p.d);
        }
        var at = Seat(box, "곰생회관", w, d, CouncilScale);

        // 건물이 yaw 로 돌아가 있으면 <b>월드 AABB 폭이 실제 폭보다 넓게</b> 잡힌다.
        // MyModel 은 그 AABB 로 배율을 맞추니, 돌아간 만큼을 미리 먹여 보낸다.
        float byaw = t.eulerAngles.y * Mathf.Deg2Rad;
        float cw = Mathf.Abs(Mathf.Cos(byaw)), sw = Mathf.Abs(Mathf.Sin(byaw));

        var placed = new Transform[CouncilLayout.Length];
        int made = 0;
        float signal = 0f;

        for (int i = 0; i < CouncilLayout.Length; i++)
        {
            var p = CouncilLayout[i];
            float seen = Mathf.Max(p.w * cw + p.d * sw, p.w * sw + p.d * cw);
            placed[i] = MyModel(t, CouncilFolder + p.file + ".fbx", "In_" + p.label,
                                at[i], seen * CouncilScale);
            if (placed[i] == null) continue;
            made++;
#if UNITY_EDITOR
            // 에디터에서 씬을 구울 때만 실제로 잰다. 재 보고 CouncilSkew 와 다르면 아래에서 알려준다.
            signal += FrontSkew(placed[i]);   // 아직 안 돌린 상태에서 잰다 — 열 점이 같은 자세다
#endif
        }

        // ★ 런타임(트랙 씬의 Awake 빌드)에서는 못 잰다 — 미리 재 둔 값을 쓴다.
        if (made > 0 && Mathf.Abs(signal) < 0.001f) signal = CouncilSkew;

        float extra = signal >= 0f ? 0f : 180f;
        for (int i = 0; i < placed.Length; i++)
            if (placed[i] != null)
                placed[i].localRotation =
                    Quaternion.Euler(0f, CouncilLayout[i].yaw + extra, 0f) * placed[i].localRotation;

        if (made == 0)
            Debug.LogWarning("[곰생회관] 가구 FBX 를 하나도 못 찾았다 — "
                           + CouncilFolder + " 에 SU_01~SU_10 이 있는지 확인해라.");
#if UNITY_EDITOR
        else if (Mathf.Abs(signal - CouncilSkew) > 0.08f)
            Debug.LogWarning($"[곰생회관] 가구 {made}/10 · 잰 쏠림 {signal:+0.000;-0.000}m 가 "
                           + $"적어 둔 값 {CouncilSkew:+0.000}m 과 다르다 — 모델을 다시 내보냈다면 "
                           + "CampusBuilder.CouncilSkew 를 그 값으로 고쳐라.");
#endif

        // ---- 벽 마감 — 모서리 기둥과 굽도리 ----
        // 가구를 들여놓으면 <b>빈 벽이 더 눈에 띈다.</b> 통짜 크림색 벽 넉 장은
        // 레고 상자의 정체 그대로라, 캠퍼스 외벽에서 쓴 둘을 그대로 가져온다.
        float inX = w * 0.5f - 0.6f, inZ = d * 0.5f - 0.6f;

        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Block(t, $"InCorner_{sx}_{sz}",
                      new Vector3(sx * (inX - 0.15f), h * 0.5f, sz * (inZ - 0.15f)),
                      Quaternion.identity, new Vector3(0.28f, h, 0.28f), ColTrimDark, noCollider: true);

        for (int sx = -1; sx <= 1; sx += 2)
            Block(t, $"InSkirt_{sx}", new Vector3(sx * (inX - 0.05f), 0.42f, 0f),
                  Quaternion.identity, new Vector3(0.10f, 0.84f, d - 1.8f), ColWallFoot, noCollider: true);
        Block(t, "InSkirtBack", new Vector3(0f, 0.42f, -inZ + 0.05f), Quaternion.identity,
              new Vector3(w - 1.8f, 0.84f, 0.10f), ColWallFoot, noCollider: true);

        // 천장 — 대들보 둘과 서까래. 민짜 반자는 «아직 안 지은 방» 으로 보인다
        for (int s = -1; s <= 1; s += 2)
            Block(t, $"InGirder_{s}", new Vector3(s * 3.9f, h - 0.70f, 0f), Quaternion.identity,
                  new Vector3(0.40f, 0.40f, d - 1.6f), ColWoodRail, noCollider: true);
        for (int i = 0; i < 7; i++)
            Block(t, $"InRafter_{i}", new Vector3(0f, h - 0.32f, -4.2f + i * 1.4f), Quaternion.identity,
                  new Vector3(w - 1.6f, 0.20f, 0.16f), ColWood, noCollider: true);
    }

    /// <summary>
    /// 앞에만 있는 부품이 <b>몸통 중심의 어느 쪽</b>에 있는지 — 부호가 곧 앞면이고
    /// (양수면 모델의 +Z 가 앞), 절댓값이 <b>신호의 세기</b>다. 신호가 없으면 0.
    ///
    /// 부모(건물) 기준으로 내려서 잰다 — 건물이 yaw 로 돌아가 있어서
    /// <b>월드 좌표로 재면 거짓말한다</b>(이 프로젝트에서 네 번 걸린 함정).
    /// </summary>
    static readonly string[] CheolgomMarks =
        { "Cabinet", "Stretcher", "Winch", "Load", "Target", "Equipment", "Dummy" };

    static float FrontEvidence(Transform m) => FrontEvidence(m, CheolgomMarks);

    /// <summary>
    /// 이 부품이 <b>앞면 표식</b>인가.
    ///
    /// ★ 2026-09-29 <b>이름을 그대로 비교하다가 웅지관에서 걸렸다.</b> 부품 이름이
    /// 묶음마다 다르게 들어온다 — 철곰관은 <c>Target_1</c> 인데 웅지관은
    /// <c>05_Grand_Records_Wall__Archive_Door_1</c> 처럼 <b>파일 이름이 앞에 붙어</b> 온다
    /// (내보낼 때 설정이 달랐다). 그래서 <c>StartsWith("Archive_Door")</c> 가 열 점 전부
    /// 안 맞았고, 「앞면을 못 쟀다」 경고를 내며 <b>안 돌리고</b> 세워 왔다.
    ///
    /// <c>__</c> 뒤를 잘라 보고 원래 이름으로도 한 번 본다. <b>`Contains` 로 넓히지 않는 건</b>
    /// 파일 이름에 표식과 같은 낱말이 들어 있으면(<c>Budget_Planning_Wall</c>)
    /// <b>몸통까지 앞면으로 세어져</b> 신호가 0에 가까워지기 때문이다.
    /// </summary>
    static bool Marked(string name, string[] marks)
    {
        int cut = name.LastIndexOf("__", System.StringComparison.Ordinal);
        string part = cut >= 0 ? name.Substring(cut + 2) : name;
        foreach (var k in marks)
            if (part.StartsWith(k) || name.StartsWith(k)) return true;
        return false;
    }

    static float FrontEvidence(Transform m, string[] marks)
    {
        Bounds whole = default, front = default;
        bool anyAll = false, anyFront = false;

        foreach (var r in m.GetComponentsInChildren<Renderer>())
        {
            if (!anyAll) { whole = r.bounds; anyAll = true; } else whole.Encapsulate(r.bounds);

            if (!Marked(r.transform.name, marks)) continue;

            if (!anyFront) { front = r.bounds; anyFront = true; } else front.Encapsulate(r.bounds);
        }
        if (!anyAll || !anyFront || m.parent == null) return 0f;

        return m.parent.InverseTransformPoint(front.center).z
             - m.parent.InverseTransformPoint(whole.center).z;
    }

    void Interior(Transform t, string name, float w, float d, float h)
    {
        float halfW = w * 0.5f - 0.9f, halfD = d * 0.5f - 0.9f;

        // 어느 방에나 있는 것 — 천장등 둘, 뒷벽 걸레받이
        for (int i = -1; i <= 1; i += 2)
            Block(t, $"InLamp_{i}", new Vector3(i * w * 0.22f, h - 0.55f, 0f), Quaternion.identity,
                  new Vector3(1.6f, 0.18f, 1.6f), ColLantern, noCollider: true);

        Block(t, "InBase", new Vector3(0f, 0.28f, -halfD - 0.1f), Quaternion.identity,
              new Vector3(w - 1.2f, 0.46f, 0.18f), ColWood, noCollider: true);

        // ★ 이야기 단서. <b>관을 가리지 않고 방마다 한두 점</b>씩 바닥에 놓인다 —
        //   자리표(<see cref="ClueLayout"/>)에 없는 관은 아무 일도 안 일어난다.
        Clues(t, name);

        switch (name)
        {
            // ★ 2026-09-23 <b>수예부로 다시 지었다.</b> 전에는 다섯 조각(작업대 3 · 재봉틀 1 ·
            // 천 1)뿐이라 22 × 14m 방이 거의 비어 있었고, 그래서 곰솥관과 «사실상 차이가 없는»
            // 것처럼 보였다.
            //
            // <b>이 방이 곰인형 박물관에서 제일 중요한 방이다</b> — 여기서 곰들이 만들어진다.
            // 그래서 재봉실에 있을 법한 물건만 늘어놓는 게 아니라 <b>만들어지는 중인 곰</b>을
            // 놓는다. 팻말의 「여기서 다들 태어났습니다」가 그 한 장면으로 설명된다.
            // ★ 2026-09-23 <b>수예실로 다듬었다.</b> 유저: *"전시실 때 한 연출처럼 다듬어 달라.
            // 모델링도 좀 더 세심하게, 진짜 수예실처럼."*
            //
            // 순서는 <b>공간 → 배경 → 색 → 조명</b>이다(곰밥마당·화장실에서 두 번 배운 것).
            // 이 방은 22 × 14 × <b>9m</b> 라, 바느질하는 방치고 천장이 너무 높았다 —
            // 소품을 아무리 늘려도 «작업실» 이 아니라 «창고에 재봉틀을 둔 것» 으로 보인다.
            case "곰손관":     // 인형제작·공예·봉제 — 수예부
            {
                float sX = w * 0.5f - 0.6f;      // 옆벽 안쪽 (Hollow 벽 두께 0.6)
                float sZb = -d * 0.5f + 0.6f;    // 뒷벽 안쪽
                float sZf = d * 0.5f - 0.6f;     // 앞벽(출입문) 안쪽
                float ceilY = 4.6f;              // 내린 반자. 문(4.2)과 상인방 위를 지나간다

                // ══ 공간 ══ 반자를 내린다. <b>이게 제일 크게 듣는다.</b>
                Block(t, "DropCeiling", new Vector3(0f, ceilY, 0f), Quaternion.identity,
                      new Vector3(w - 1.0f, 0.12f, d - 1.0f), ColCream, noCollider: true);

                // 서까래 — 한옥 반자는 민짜가 아니다. 이게 없으면 «석고보드 천장» 이 된다
                for (int g = -6; g <= 6; g++)
                    Block(t, $"CeilBeam_{g}", new Vector3(0f, ceilY - 0.13f, g * 1.0f),
                          Quaternion.identity, new Vector3(w - 1.4f, 0.14f, 0.16f),
                          ColWood, noCollider: true);
                // 대들보 둘 — 서까래와 직각. 층이 둘이어야 천장이 «구조» 로 읽힌다
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Block(t, $"CeilGirder_{s2}", new Vector3(s2 * 5.2f, ceilY - 0.28f, 0f),
                          Quaternion.identity, new Vector3(0.28f, 0.3f, d - 1.4f),
                          ColTrimDark, noCollider: true);

                // 천장 돌림띠 — 벽과 천장이 맞닿는 선에 턱이 있어야 방이 된다
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    Block(t, $"Cornice_X{s2}", new Vector3(s2 * (sX - 0.07f), ceilY - 0.22f, 0f),
                          Quaternion.identity, new Vector3(0.16f, 0.2f, d - 1.2f),
                          ColTrimDark, noCollider: true);
                    Block(t, $"Cornice_Z{s2}", new Vector3(0f, ceilY - 0.22f, s2 * (sZf - 0.07f)),
                          Quaternion.identity, new Vector3(w - 1.2f, 0.2f, 0.16f),
                          ColTrimDark, noCollider: true);
                }

                // 모서리 기둥 넷 — 상자의 날 선 모서리가 레고의 정체다(2026-09-18)
                for (int sx2 = -1; sx2 <= 1; sx2 += 2)
                    for (int sz2 = -1; sz2 <= 1; sz2 += 2)
                        Block(t, $"InCornerPost_{sx2}_{sz2}",
                              new Vector3(sx2 * (sX - 0.12f), ceilY * 0.5f, sz2 * (sZf - 0.12f)),
                              Quaternion.identity, new Vector3(0.24f, ceilY, 0.24f),
                              ColTrimDark, noCollider: true);

                // ══ 배경 ══ 굽도리 + 바닥 줄눈. 벽이 통짜 한 색이면 크기를 잴 수가 없다
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    Block(t, $"InSkirtX_{s2}", new Vector3(s2 * (sX - 0.06f), 0.45f, 0f),
                          Quaternion.identity, new Vector3(0.12f, 0.9f, d - 1.2f),
                          ColWoodRail, noCollider: true);
                    Block(t, $"InSkirtLipX_{s2}", new Vector3(s2 * (sX - 0.14f), 0.92f, 0f),
                          Quaternion.identity, new Vector3(0.2f, 0.06f, d - 1.2f),
                          ColWood, noCollider: true);
                }
                Block(t, "InSkirtZ", new Vector3(0f, 0.45f, sZb + 0.06f), Quaternion.identity,
                      new Vector3(w - 1.2f, 0.9f, 0.12f), ColWoodRail, noCollider: true);
                Block(t, "InSkirtLipZ", new Vector3(0f, 0.92f, sZb + 0.14f), Quaternion.identity,
                      new Vector3(w - 1.2f, 0.06f, 0.2f), ColWood, noCollider: true);

                for (int g = -4; g <= 4; g++)
                    Block(t, $"InFloorSeam_{g + 4}", new Vector3(g * 2.4f, 0.006f, 0f),
                          Quaternion.identity, new Vector3(0.05f, 0.012f, d - 1.4f),
                          ColTrimDark, noCollider: true);

                // ══ 재봉 작업대 넷 ══
                // ★ <b>재봉틀을 상자 셋에서 열한 조각으로.</b> 유저: *"모델링도 좀 더 세심하게."*
                // 재봉틀을 재봉틀로 만드는 건 몸통이 아니라 <b>ㄷ 자 실루엣과 손잡이 바퀴</b>다 —
                // 아래팔 · 세로 기둥 · 윗팔이 갈라져야 그 모양이 나온다.
                for (int i = -1; i <= 2; i++)
                {
                    float bx = i * 4.4f - 2.2f;
                    Vector3 at = new Vector3(bx, 0f, sZf - 4.2f);

                    // 상판 · 다리 · 가로대 — 다리가 없으면 바닥에 그린 무늬로 보인다
                    Block(t, $"InBenchTop_{i}", at + Vector3.up * 0.86f, Quaternion.identity,
                          new Vector3(3.4f, 0.09f, 1.6f), ColWood, noCollider: true);
                    Block(t, $"InBenchLip_{i}", at + new Vector3(0f, 0.80f, 0.79f),
                          Quaternion.identity, new Vector3(3.4f, 0.05f, 0.08f),
                          ColWoodRail, noCollider: true);
                    for (int lx = -1; lx <= 1; lx += 2)
                        for (int lz = -1; lz <= 1; lz += 2)
                            Block(t, $"InBenchLeg_{i}_{lx}_{lz}",
                                  at + new Vector3(lx * 1.5f, 0.41f, lz * 0.66f),
                                  Quaternion.identity, new Vector3(0.11f, 0.82f, 0.11f),
                                  ColTrimDark, noCollider: true);
                    Block(t, $"InBenchRail_{i}", at + new Vector3(0f, 0.26f, 0f),
                          Quaternion.identity, new Vector3(2.9f, 0.07f, 0.07f),
                          ColTrimDark, noCollider: true);

                    // ── 재봉틀 ──
                    Vector3 m = at + new Vector3(-0.8f, 0.905f, 0f);
                    Block(t, $"InMachBed_{i}", m + new Vector3(0f, 0.04f, 0f), Quaternion.identity,
                          new Vector3(1.05f, 0.08f, 0.42f), ColTrimDark, noCollider: true);   // 받침판
                    Block(t, $"InMachArmLow_{i}", m + new Vector3(-0.06f, 0.17f, 0f),
                          Quaternion.identity, new Vector3(0.82f, 0.22f, 0.3f),
                          ColBearDark, noCollider: true);                                     // 아래팔
                    Block(t, $"InMachPillar_{i}", m + new Vector3(0.34f, 0.38f, 0f),
                          Quaternion.identity, new Vector3(0.24f, 0.62f, 0.28f),
                          ColBearDark, noCollider: true);                                     // 세로 기둥
                    Block(t, $"InMachArmTop_{i}", m + new Vector3(-0.02f, 0.62f, 0f),
                          Quaternion.identity, new Vector3(0.92f, 0.18f, 0.24f),
                          ColBearDark, noCollider: true);                                     // 윗팔
                    Block(t, $"InMachHead_{i}", m + new Vector3(-0.42f, 0.55f, 0f),
                          Quaternion.identity, new Vector3(0.18f, 0.3f, 0.22f),
                          ColBearDark, noCollider: true);                                     // 머리
                    // 바늘대와 노루발 — <b>이 두 개가 «바느질하는 기계» 를 만든다</b>
                    Block(t, $"InMachNeedle_{i}", m + new Vector3(-0.42f, 0.30f, 0f),
                          Quaternion.identity, new Vector3(0.035f, 0.2f, 0.035f),
                          ColStoneWall, noCollider: true);
                    Block(t, $"InMachFoot_{i}", m + new Vector3(-0.42f, 0.16f, 0f),
                          Quaternion.identity, new Vector3(0.1f, 0.05f, 0.14f),
                          ColStoneWall, noCollider: true);
                    // 손잡이 바퀴 — 재봉틀의 얼굴
                    Disc(t, $"InMachWheel_{i}", m + new Vector3(0.46f, 0.58f, 0f),
                         new Vector3(0.26f, 0.05f, 0.26f), ColMapleGold);
                    // 실패 핀과 실 — 위에서 실이 내려온다
                    Block(t, $"InMachPin_{i}", m + new Vector3(0.18f, 0.76f, 0f),
                          Quaternion.identity, new Vector3(0.03f, 0.1f, 0.03f),
                          ColStoneWall, noCollider: true);
                    Disc(t, $"InMachSpool_{i}", m + new Vector3(0.18f, 0.82f, 0f),
                         new Vector3(0.1f, 0.13f, 0.1f),
                         i % 2 == 0 ? ColRibbon : ColMint);
                    Block(t, $"InMachThread_{i}", m + new Vector3(-0.13f, 0.7f, 0f),
                          Quaternion.Euler(0f, 0f, 24f), new Vector3(0.42f, 0.012f, 0.012f),
                          ColCream, noCollider: true);
                    // 페달과 연결봉 — 발밑까지 내려와야 «발로 밟는 기계» 다
                    Block(t, $"InMachPedal_{i}", at + new Vector3(-0.8f, 0.05f, 0.45f),
                          Quaternion.Euler(-7f, 0f, 0f), new Vector3(0.34f, 0.06f, 0.22f),
                          ColTrimDark, noCollider: true);
                    Block(t, $"InMachRod_{i}", at + new Vector3(-0.8f, 0.48f, 0.3f),
                          Quaternion.Euler(18f, 0f, 0f), new Vector3(0.03f, 0.86f, 0.03f),
                          ColStoneWall, noCollider: true);

                    // 작업 중인 천 — 노루발 아래로 들어간다
                    Block(t, $"InFabric_{i}", at + new Vector3(-0.35f, 0.93f, 0.05f),
                          Quaternion.Euler(0f, 14f, 0f), new Vector3(1.1f, 0.03f, 0.72f),
                          i % 2 == 0 ? ColMint : ColRibbon, noCollider: true);

                    // 의자 — 등받이가 있어야 «앉아서 오래 하는 일» 이다
                    Block(t, $"InStool_{i}", at + new Vector3(0f, 0.46f, 1.45f),
                          Quaternion.identity, new Vector3(0.48f, 0.07f, 0.44f),
                          ColWoodRail, noCollider: true);
                    Block(t, $"InStoolBack_{i}", at + new Vector3(0f, 0.72f, 1.65f),
                          Quaternion.Euler(-8f, 0f, 0f), new Vector3(0.46f, 0.42f, 0.05f),
                          ColWoodRail, noCollider: true);
                    for (int lx = -1; lx <= 1; lx += 2)
                        for (int lz = -1; lz <= 1; lz += 2)
                            Block(t, $"InStoolLeg_{i}_{lx}_{lz}",
                                  at + new Vector3(lx * 0.19f, 0.22f, 1.45f + lz * 0.17f),
                                  Quaternion.identity, new Vector3(0.05f, 0.44f, 0.05f),
                                  ColTrimDark, noCollider: true);

                    // 작업등 — <b>바느질은 손 앞이 밝아야 한다.</b> 천장등만으로는 공방이 안 된다
                    Block(t, $"InTaskArm_{i}", at + new Vector3(1.2f, 1.28f, -0.5f),
                          Quaternion.Euler(0f, 0f, -18f), new Vector3(0.05f, 0.8f, 0.05f),
                          ColStoneWall, noCollider: true);
                    Block(t, $"InTaskShade_{i}", at + new Vector3(0.95f, 1.62f, -0.5f),
                          Quaternion.Euler(0f, 0f, 26f), new Vector3(0.3f, 0.16f, 0.3f),
                          ColTrimDark, noCollider: true);
                    Block(t, $"InTaskBulb_{i}", at + new Vector3(0.95f, 1.52f, -0.5f),
                          Quaternion.identity, new Vector3(0.14f, 0.06f, 0.14f),
                          ColLantern, noCollider: true);   // ColLantern 은 발광이다
                }

                // ══ 마름질 대 ══ 방 한가운데. <b>큰 것 하나가 사람 크기를 알려준다</b>
                // (전시실 벤치와 같은 역할). 재봉 전에 천을 재고 자르는 자리다.
                Vector3 cut = new Vector3(0.5f, 0f, -0.8f);
                Block(t, "InCutTop", cut + Vector3.up * 0.94f, Quaternion.identity,
                      new Vector3(5.2f, 0.1f, 2.0f), ColWoodRail, noCollider: true);
                for (int lx = -1; lx <= 1; lx += 2)
                    for (int lz = -1; lz <= 1; lz += 2)
                        Block(t, $"InCutLeg_{lx}_{lz}",
                              cut + new Vector3(lx * 2.4f, 0.45f, lz * 0.85f),
                              Quaternion.identity, new Vector3(0.14f, 0.9f, 0.14f),
                              ColTrimDark, noCollider: true);
                Block(t, "InCutShelf", cut + Vector3.up * 0.3f, Quaternion.identity,
                      new Vector3(4.8f, 0.06f, 1.6f), ColWood, noCollider: true);
                // 아래칸에 개어 둔 천 — 선반이 비면 «다리 넷 달린 판» 이다
                for (int i = 0; i < 5; i++)
                    Block(t, $"InFolded_{i}", cut + new Vector3(-1.8f + i * 0.9f, 0.42f, 0f),
                          Quaternion.Euler(0f, i * 6f, 0f), new Vector3(0.72f, 0.18f, 1.2f),
                          i % 3 == 0 ? ColMint : i % 3 == 1 ? ColCream : ColRibbon, noCollider: true);

                // 상판 위 — 자·초크·가위·재단한 천. <b>작은 것이 방의 크기를 알려준다</b>
                Block(t, "InRuler", cut + new Vector3(-1.1f, 1.0f, -0.55f),
                      Quaternion.Euler(0f, 4f, 0f), new Vector3(1.5f, 0.015f, 0.09f),
                      ColCream, noCollider: true);
                Block(t, "InChalk", cut + new Vector3(-0.2f, 1.01f, -0.6f),
                      Quaternion.Euler(0f, 32f, 0f), new Vector3(0.1f, 0.03f, 0.05f),
                      ColWater, noCollider: true);
                // 가위 — 날 둘이 벌어져 있어야 가위다
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Block(t, $"InScissorBlade_{s2}", cut + new Vector3(1.2f, 1.0f, -0.4f),
                          Quaternion.Euler(0f, 18f + s2 * 9f, 0f), new Vector3(0.36f, 0.012f, 0.035f),
                          ColStoneWall, noCollider: true);
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Block(t, $"InScissorRing_{s2}", cut + new Vector3(1.44f, 1.0f, -0.4f + s2 * 0.05f),
                          Quaternion.identity, new Vector3(0.1f, 0.012f, 0.07f),
                          ColBearDark, noCollider: true);
                // 재단해 둔 곰 조각 — 여기서 곰이 «오려진다»
                for (int i = 0; i < 4; i++)
                    Block(t, $"InCutPiece_{i}", cut + new Vector3(0.4f + (i % 2) * 0.5f, 1.0f,
                                                                  0.35f + (i / 2) * 0.4f),
                          Quaternion.Euler(0f, 20f + i * 34f, 0f), new Vector3(0.34f, 0.012f, 0.24f),
                          ColMapleGold, noCollider: true);

                // ══ 자수틀 ══ <b>수예실의 상징.</b> 둥근 틀 두 겹에 천을 끼운다
                for (int i = 0; i < 3; i++)
                {
                    Vector3 hp = new Vector3(-8.4f, 1.55f + i * 0.72f, sZb + 0.2f);
                    Disc(t, $"InHoopOut_{i}", hp, new Vector3(0.52f, 0.05f, 0.52f), ColWood);
                    Disc(t, $"InHoopCloth_{i}", hp + new Vector3(0f, 0.01f, 0.03f),
                         new Vector3(0.44f, 0.03f, 0.44f), ColCream);
                    // 수놓인 곰 얼굴 — 귀 둘과 코
                    for (int e = -1; e <= 1; e += 2)
                        Disc(t, $"InHoopEar_{i}_{e}", hp + new Vector3(e * 0.12f, 0.12f, 0.05f),
                             new Vector3(0.1f, 0.02f, 0.1f), ColMapleGold);
                    Disc(t, $"InHoopNose_{i}", hp + new Vector3(0f, -0.02f, 0.05f),
                         new Vector3(0.07f, 0.02f, 0.07f), ColBearDark);
                }

                // ══ 실 벽 ══ 색이 이 방의 정체다 — 나머지 방은 다 나무·크림·돌색이다
                Block(t, "InThreadBoard", new Vector3(-sX + 0.25f, 2.3f, 1.8f), Quaternion.identity,
                      new Vector3(0.12f, 2.4f, 6.0f), ColWood, noCollider: true);
                Block(t, "InThreadFrame", new Vector3(-sX + 0.31f, 2.3f, 1.8f), Quaternion.identity,
                      new Vector3(0.05f, 2.6f, 6.3f), ColTrimDark, noCollider: true);
                var threads = new[] { ColRibbon, ColMint, ColLantern, ColMapleGold, ColWater, ColCream };
                for (int r = 0; r < 5; r++)
                {
                    // 실패를 거는 가로대 — 못이 아니라 봉에 꽂혀 있어야 «수납» 이다
                    Block(t, $"InThreadRod_{r}", new Vector3(-sX + 0.42f, 1.4f + r * 0.46f, 1.8f),
                          Quaternion.identity, new Vector3(0.03f, 0.03f, 5.6f),
                          ColStoneWall, noCollider: true);
                    for (int c = 0; c < 9; c++)
                        Disc(t, $"InThread_{r}_{c}",
                             new Vector3(-sX + 0.44f, 1.4f + r * 0.46f, -1.0f + c * 0.7f),
                             new Vector3(0.15f, 0.19f, 0.15f), threads[(r * 9 + c) % threads.Length]);
                }

                // ══ 원단 두루마리 ══ 비스듬해야 «세워 둔 것» 이다. 심지가 보여야 두루마리다
                for (int i = 0; i < 6; i++)
                {
                    Vector3 bp = new Vector3(sX - 0.55f, 0f, -4.6f + i * 0.58f);
                    Block(t, $"InBolt_{i}", bp + new Vector3(0f, 1.15f, 0f),
                          Quaternion.Euler(9f, 0f, 6f), new Vector3(0.32f, 2.3f, 0.32f),
                          i % 3 == 0 ? ColRibbon : i % 3 == 1 ? ColMint : ColCream, noCollider: true);
                    Block(t, $"InBoltCore_{i}", bp + new Vector3(-0.19f, 2.3f, 0f),
                          Quaternion.Euler(9f, 0f, 6f), new Vector3(0.07f, 0.16f, 0.07f),
                          ColWood, noCollider: true);
                }

                // ══ 솜 자루 ══ 곰인형 박물관이라 <b>솜이 없으면 안 된다</b>
                for (int i = 0; i < 3; i++)
                {
                    Vector3 sp = new Vector3(sX - 1.7f - i * 0.12f, 0f, 2.2f + i * 1.15f);
                    Block(t, $"InStuffing_{i}", sp + Vector3.up * 0.58f,
                          Quaternion.Euler(0f, i * 14f, 0f), new Vector3(1.1f, 1.16f, 1.1f),
                          ColCream, noCollider: true);
                    Block(t, $"InStuffTie_{i}", sp + Vector3.up * 1.14f,
                          Quaternion.Euler(0f, i * 14f, 0f), new Vector3(0.5f, 0.12f, 0.5f),
                          ColWoodRail, noCollider: true);
                }
                // 터진 자루에서 솜이 비어져 나온다 — 한 군데만. <b>쓰던 자루</b>로 읽힌다
                Ball(t, "InStuffPuff", new Vector3(sX - 1.7f, 1.24f, 2.2f),
                     new Vector3(0.52f, 0.3f, 0.52f), ColWallTile);

                // ══ 만들다 만 곰 ══ 이 방의 주인공.
                // <b>완성품을 놓으면 «전시» 고, 조각을 놓으면 «작업 중» 이다.</b>
                Vector3 wip = new Vector3(6.6f, 0f, sZf - 4.2f);
                Block(t, "InBearBody", wip + new Vector3(0.15f, 1.22f, 0.15f),
                      Quaternion.Euler(0f, 20f, 8f), new Vector3(0.5f, 0.6f, 0.4f),
                      ColMapleGold, noCollider: true);
                for (int i = 0; i < 4; i++)
                    Block(t, $"InBearLimb_{i}",
                          wip + new Vector3(0.85f + (i % 2) * 0.26f, 0.96f, -0.12f - (i / 2) * 0.24f),
                          Quaternion.Euler(0f, 30f + i * 25f, 78f), new Vector3(0.16f, 0.42f, 0.16f),
                          ColMapleGold, noCollider: true);
                for (int i = 0; i < 2; i++)
                    Disc(t, $"InBearEar_{i}", wip + new Vector3(0.0f + i * 0.3f, 0.95f, 0.52f),
                         new Vector3(0.2f, 0.06f, 0.2f), ColMapleGold);
                // 시침핀이 몸통에 꽂혀 있다 — «꿰매기 직전» 이 보인다
                for (int i = 0; i < 5; i++)
                    Block(t, $"InPin_{i}", wip + new Vector3(0.02f + i * 0.07f, 1.42f, 0.18f),
                          Quaternion.Euler(0f, 0f, 14f + i * 7f), new Vector3(0.008f, 0.09f, 0.008f),
                          ColStoneWall, noCollider: true);

                // 눈 단추 상자 — 칸마다 까만 단추
                Block(t, "InButtonBox", wip + new Vector3(-0.2f, 0.96f, -0.5f), Quaternion.identity,
                      new Vector3(0.46f, 0.1f, 0.32f), ColWood, noCollider: true);
                Block(t, "InButtonLid", wip + new Vector3(-0.2f, 1.02f, -0.68f),
                      Quaternion.Euler(-62f, 0f, 0f), new Vector3(0.46f, 0.02f, 0.3f),
                      ColWoodRail, noCollider: true);
                for (int i = 0; i < 6; i++)
                    Disc(t, $"InEyeButton_{i}",
                         wip + new Vector3(-0.35f + (i % 3) * 0.16f, 1.02f, -0.57f + (i / 3) * 0.14f),
                         new Vector3(0.07f, 0.02f, 0.07f), ColBearDark);

                // 핀쿠션 — 토마토 모양. 수예실에 이거 없으면 섭섭하다
                Ball(t, "InPinCushion", wip + new Vector3(0.75f, 0.99f, 0.5f),
                     new Vector3(0.18f, 0.14f, 0.18f), ColRibbon);
                for (int i = 0; i < 6; i++)
                    Block(t, $"InCushionPin_{i}",
                          wip + new Vector3(0.75f, 1.06f, 0.5f), Quaternion.Euler(28f, i * 60f, 14f),
                          new Vector3(0.008f, 0.12f, 0.008f), ColStoneWall, noCollider: true);

                // ══ 완성품 선반 ══ 뒷벽. 다 만든 곰들이 앉아 있다
                Block(t, "InShelfDone", new Vector3(1.5f, 1.95f, sZb + 0.35f), Quaternion.identity,
                      new Vector3(14f, 0.12f, 0.62f), ColWood, noCollider: true);
                Block(t, "InShelfLip", new Vector3(1.5f, 2.04f, sZb + 0.64f), Quaternion.identity,
                      new Vector3(14f, 0.07f, 0.05f), ColWoodRail, noCollider: true);
                for (int i = -3; i <= 3; i++)
                {
                    float bx = 1.5f + i * 2.0f;
                    Block(t, $"InDoneBear_{i + 3}", new Vector3(bx, 2.28f, sZb + 0.35f),
                          Quaternion.Euler(0f, i * 11f, 0f), new Vector3(0.42f, 0.52f, 0.34f),
                          i % 2 == 0 ? ColMapleGold : ColWoodRail, noCollider: true);
                    for (int e = 0; e < 2; e++)
                        Disc(t, $"InDoneEar_{i + 3}_{e}",
                             new Vector3(bx - 0.13f + e * 0.26f, 2.56f, sZb + 0.35f),
                             new Vector3(0.17f, 0.05f, 0.17f),
                             i % 2 == 0 ? ColMapleGold : ColWoodRail);
                    // 목에 리본 — 완성품과 «만들다 만 것» 을 한눈에 가른다
                    Block(t, $"InDoneRibbon_{i + 3}", new Vector3(bx, 2.14f, sZb + 0.2f),
                          Quaternion.identity, new Vector3(0.44f, 0.08f, 0.1f),
                          i % 2 == 0 ? ColRibbon : ColMint, noCollider: true);
                    // 이름표 — 실제 공방은 누가 만든 건지 붙여 둔다
                    Block(t, $"InDoneTag_{i + 3}", new Vector3(bx + 0.28f, 2.02f, sZb + 0.5f),
                          Quaternion.Euler(0f, 0f, -12f), new Vector3(0.16f, 0.1f, 0.01f),
                          ColCream, noCollider: true);
                }

                // ══ 패턴 종이 ══ 벽에 핀으로 붙인 도면 + 걸어 둔 두루마리
                for (int i = 0; i < 5; i++)
                {
                    Block(t, $"InPattern_{i}", new Vector3(-3.6f + i * 1.9f, 3.35f, sZb + 0.08f),
                          Quaternion.Euler(0f, 0f, -4f + i * 2f), new Vector3(0.8f, 1.02f, 0.02f),
                          ColCream, noCollider: true);
                    Block(t, $"InPatternPin_{i}", new Vector3(-3.6f + i * 1.9f, 3.82f, sZb + 0.1f),
                          Quaternion.identity, new Vector3(0.04f, 0.04f, 0.03f),
                          ColMapleGold, noCollider: true);
                }

                // ══ 다리미대 ══ 좁고 긴 것 하나가 방의 리듬을 깬다
                Vector3 ib = new Vector3(-6.6f, 0f, -3.6f);
                Block(t, "InIronBoard", ib + Vector3.up * 0.88f, Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(1.7f, 0.07f, 0.56f), ColCream, noCollider: true);
                Block(t, "InIronCover", ib + Vector3.up * 0.92f, Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(1.6f, 0.02f, 0.5f), ColMint, noCollider: true);
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Block(t, $"InIronLeg_{s2}", ib + new Vector3(s2 * 0.5f, 0.44f, 0f),
                          Quaternion.Euler(0f, 24f, s2 * 13f), new Vector3(0.07f, 0.88f, 0.07f),
                          ColStoneWall, noCollider: true);
                Block(t, "InIron", ib + new Vector3(0.42f, 0.98f, -0.1f), Quaternion.Euler(0f, 24f, 0f),
                      new Vector3(0.32f, 0.12f, 0.19f), ColStoneWall, noCollider: true);
                Block(t, "InIronHandle", ib + new Vector3(0.42f, 1.09f, -0.1f),
                      Quaternion.Euler(0f, 24f, 0f), new Vector3(0.26f, 0.08f, 0.07f),
                      ColBearDark, noCollider: true);

                // ══ 마네킹 둘 ══ 사람 키를 알려주는 물건
                for (int i = 0; i < 2; i++)
                {
                    Vector3 mAt = new Vector3(8.2f + i * 1.9f, 0f, -3.8f);
                    Disc(t, $"InFormFoot_{i}", mAt + Vector3.up * 0.05f,
                         new Vector3(0.52f, 0.1f, 0.52f), ColBearDark);
                    Block(t, $"InFormPost_{i}", mAt + Vector3.up * 0.52f, Quaternion.identity,
                          new Vector3(0.08f, 1.0f, 0.08f), ColStoneWall, noCollider: true);
                    Block(t, $"InFormBody_{i}", mAt + Vector3.up * 1.34f,
                          Quaternion.Euler(0f, i * 26f, 0f), new Vector3(0.52f, 0.74f, 0.36f),
                          i == 0 ? ColCream : ColMint, noCollider: true);
                    Block(t, $"InFormNeck_{i}", mAt + Vector3.up * 1.74f, Quaternion.identity,
                          new Vector3(0.16f, 0.1f, 0.16f), ColWood, noCollider: true);
                    // 줄자가 목에 걸려 있다 — 마네킹을 «쓰는 물건» 으로 만든다
                    if (i == 0)
                        for (int s2 = -1; s2 <= 1; s2 += 2)
                            Block(t, $"InTape_{s2}", mAt + new Vector3(s2 * 0.14f, 1.46f, 0.14f),
                                  Quaternion.Euler(0f, 0f, s2 * 6f), new Vector3(0.04f, 0.62f, 0.012f),
                                  ColMapleGold, noCollider: true);
                }

                // ══ 바닥에 떨어진 천 조각과 실밥 ══ 작은 것이 있어야 레고로 안 보인다
                for (int i = 0; i < 11; i++)
                    Block(t, $"InScrap_{i}",
                          new Vector3(-8f + i * 1.6f, 0.014f, -1.2f + (i % 3) * 1.3f),
                          Quaternion.Euler(0f, i * 37f, 0f), new Vector3(0.26f, 0.012f, 0.19f),
                          i % 3 == 0 ? ColRibbon : i % 3 == 1 ? ColMint : ColCream, noCollider: true);
                for (int i = 0; i < 7; i++)
                    Block(t, $"InLint_{i}",
                          new Vector3(-5.5f + i * 1.9f, 0.013f, 1.4f - (i % 2) * 0.8f),
                          Quaternion.Euler(0f, i * 51f, 0f), new Vector3(0.22f, 0.008f, 0.02f),
                          i % 2 == 0 ? ColCream : ColMapleGold, noCollider: true);

                // 휴지통 — 실밥이 나오는 방에는 있어야 한다
                Disc(t, "InBin", new Vector3(-9.4f, 0.24f, -1.4f),
                     new Vector3(0.44f, 0.48f, 0.44f), ColTrimDark);
                Block(t, "InBinScrap", new Vector3(-9.4f, 0.5f, -1.4f), Quaternion.Euler(0f, 22f, 0f),
                      new Vector3(0.36f, 0.1f, 0.3f), ColMint, noCollider: true);

                // ══ 조명 ══
                // ★ 공통 천장등(`InLamp_*`)은 `h − 0.55` = <b>8.45m</b> 에 있어서
                // 방금 내린 반자(4.6) <b>위에 묻힌다.</b> 반자 밑으로 끌어내린다 —
                // 화장실에서 이미 겪은 것이고, 반자를 내릴 때마다 딸려 오는 일이야.
                foreach (string lampName in new[] { "InLamp_-1", "InLamp_1" })
                {
                    var lamp = t.Find(lampName);
                    if (lamp == null) continue;
                    lamp.localPosition = new Vector3(lamp.localPosition.x, ceilY - 0.16f, 0f);
                    lamp.localScale = new Vector3(3.2f, 0.1f, 0.7f);
                }
                // 갓 — 알전구만 있으면 «공사장» 이다. 작업실 등은 갓이 있다
                for (int s2 = -1; s2 <= 1; s2 += 2)
                    Block(t, $"InLampShade_{s2}", new Vector3(s2 * w * 0.22f, ceilY - 0.07f, 0f),
                          Quaternion.identity, new Vector3(3.5f, 0.1f, 0.9f),
                          ColTrimDark, noCollider: true);

                // 실시간 조명 <b>한 개</b> — 마름질 대 위. §7.6 이 막는 건 «방 열셋에 다 다는 것»
                // 이지 한 방에 하나가 아니다. 발광 재질만으로는 <b>바닥과 벽이 안 밝아져서</b>
                // 방이 평평하게 보인다(화장실에서 확인한 것).
                var craftLight = new GameObject("CraftLight");
                craftLight.transform.SetParent(t, false);
                craftLight.transform.localPosition = new Vector3(0.5f, ceilY - 0.8f, -0.8f);
                var cl = craftLight.AddComponent<Light>();
                cl.type = LightType.Point;
                cl.range = 13f;
                cl.intensity = 1.5f;
                cl.color = new Color(1f, 0.96f, 0.88f);
                cl.shadows = LightShadows.None;   // §7.6 — 실시간 그림자는 주요 조명 하나만
                craftLight.isStatic = true;
                break;
            }

            // ★★ 2026-09-29 <b>유저가 만든 서당·서고·상담 소품 열 점으로 다시 지었다.</b>
            // 전에는 색깔 상자 서가 여섯 장 + 책상 하나였다 — 학과 팻말이
            // 「인문·교육·연구·심리」인데 방에는 <b>연구도 심리도 없었다.</b>
            // 되살리고 싶으면 이 case 하나만 되돌리면 된다.
            case "곰머리관":
                Gommeori(t, w, d, h);
                // ★★ 2026-10-06 — 1·2장 떡밥 셋이 여기 선다: <b>빠진 장부 한 권</b>,
                //   <b>비어 있는 최근 3년</b>, 그리고 그걸 들여다보는 필름 열람기.
                //   조건이 다른 세 관과 달리 <b>기존 인테리어를 안 걷어낸다</b> —
                //   곰머리관은 내가 이미 제대로 지어 놓은 방이라 소품만 얹으면 된다.
                StoryHall(t, "곰머리관", w, d);
                break;

            // ★★ 2026-09-28 <b>유저가 만든 경호·안전 대형 소품 열 점으로 다시 지었다.</b>
            // 학과 팻말은 「경호·체육·안전」인데 방은 씨름판이었다 — 이제 <b>안전 체험
            // 훈련장</b>이고, 팻말·소품·미니게임 자리가 처음으로 같은 말을 한다.
            //
            // <b>씨름판은 걷어냈다.</b> 지름 8m 짜리 모래판과 양옆 관중석이 벽을 다 먹어서
            // 2.3~3.3m 짜리 열 점이 들어갈 자리가 없었다. 되살리고 싶으면 말해 줘 —
            // 이 case 하나만 되돌리면 된다.
            case "철곰관":
                Cheolgom(t, w, d, h);
                break;


            // ★★ 2026-09-29 <b>유저가 만든 설비 열 점으로 다시 지었다.</b>
            // 임시 이젤은 «크림색 네모 상자 + 막대기 다리» 였는데, 따라 그리기 미니게임의
            // 화판이 될 물건이라 진짜 이젤(<c>Canvas</c> 면이 0~1 UV 로 갈려 있다)로 바꿨다.
            case "재주관":
                Jaeju(t, w, d, h);
                break;

            // ★★ 2026-10-06 <b>유저가 만든 이야기 단서 소품으로 다시 지었다.</b>
            //   상자로 흉내 낸 리프트·타이어·공구벽은 걷어낸다 — 진짜 정비대가 들어온 이상
            //   그건 가구를 가리는 짐이다(대충기념관·곰생회관과 같은 판단).
            //   ★ <b>조종기 보관장이 이 방의 핵심</b>이다: 「곰이 밖에 못 나가서 카트만
            //   내보낸다」를 대사 한 줄 없이 설명하는 유일한 물건이야.
            case "곰테크관":
                // ★ <b>배율 1.0.</b> 17 x 12m 에 기계 열셋이라 1.10 이면 길이 안 남는다.
                StoryHall(t, "곰테크관", w, d, 1.0f);
                break;

            // ★★ 2026-10-07 <b>유저가 만든 관광·외국어 소품 열 점으로 다시 지었다.</b>
            //   상자로 그린 지구본·안내대·지도는 걷어낸다 — 진짜 지구본(N06)과 세계지도(N03)가
            //   들어온 이상 그건 <b>같은 물건이 둘</b>이 되는 것이다.
            //   ★ 이 방이 <b>«왜 하필 이 땅인가» 에 답하는 유일한 방</b>이다.
            case "곰누리관":
                StoryHall(t, "곰누리관", w, d, GomnuriScale, 0f);
                TourismNumber(t, w, d);
                break;

            // ★★ 2026-10-06 이야기 단서 소품. <b>결승이 생중계되는 방</b>이다 —
            //   수집품 7번 「중계 기록 장치」가 어디서 나왔는지가 여기 서 있다.
            //   부스와 유리창은 남긴다(방을 나누는 역할이라 소품과 안 겹친다).
            case "웅성관":
                // ★ 2026-10-07 <b>상자로 흉내 낸 부스를 걷어냈다.</b> 수연이 진짜 라디오 부스를
                //   만들어 왔으니(`W06`, ON AIR 등까지 달려 있다) 상자는 그걸 가리는 짐이다 —
                //   학생회관·철곰관에서 내린 것과 같은 판단이야.
                StoryHall(t, "웅성관", w, d);

                // 증거 셋은 <b>책상 위</b>에. 상판 높이는 블렌더에서 쟀다(일지대 1.00 · 편집대 0.86) —
                // 바운즈 꼭대기를 쓰면 펼친 장부(1.10)와 모니터(1.45) 위에 뜬다.
                // 일지대는 가운데를 펼친 장부가 차지해서(x ±0.63) <b>양 끝</b>에만 자리가 있다.
                OnTop(t, "테이프편집대", WoongFolder + "W08_Relabelled_Original_Tape.fbx",
                      "원본테이프",  new Vector2( 0.05f,  0.34f), 0.86f, 0.30f,  18f);
                OnTop(t, "방송일지대", WoongFolder + "W09_Fax_Transmission_Report.fbx",
                      "팩스보고서",  new Vector2( 0.86f,  0.02f), 1.00f, 0.45f, -14f);
                OnTop(t, "방송일지대", WoongFolder + "W10_Press_Access_Pass.fbx",
                      "출입증",      new Vector2(-0.86f, -0.04f), 1.00f, 0.50f,  26f);
                break;

            // ★★ 2026-10-02 <b>유저가 만든 학생회 가구 열 점으로 다시 지었다.</b>
            // (유저: *"학생회·마을 회관용 큰 에셋이 준비됬어 넣어."*)
            //
            // 전에 있던 건 <b>상자로 흉내 낸 것</b>이다 — 탁자 한 장에 의자 열, 벽에 그린
            // 게시판, 뚜껑에 홈 판 안건함. 진짜 가구가 들어온 이상 그건 가구를 가리는 짐이야
            // (대충기념관에서 「안 푼 상자」를 걷어낸 것과 같은 판단).
            // 되살리려면 이 case 를 되돌리면 된다.
            case "곰생회관":
                StudentCouncil(t, w, d, h);
                break;

            // ★★ 2026-10-06 진짜 진열장이 들어왔다. 상자로 그린 유리장과 노란 막대는 걷어낸다.
            //   ★ <b>한 자리가 비어 있고 먼지 자국만 남아 있다.</b> 왜 비었는지는
            //   게임이 끝까지 설명하지 않는다 — 그게 이 물건의 전부다.
            case "참잘했어요관":
                // ★ <b>배율 1.0.</b> 16 × 12m 방에 시상대가 5.8m 라, 다른 방처럼 1.10 을 먹이면
                //   가운데에 지나다닐 길이 안 남는다. 소품이 이미 실물 크기라 그대로가 맞다.
                StoryHall(t, "참잘했어요관", w, d, 1.0f);
                break;

            // ★★ 2026-09-29 <b>유저가 만든 전시물 열 점으로 다시 지었다.</b>
            // 「안 푼 상자」는 이름값 농담이었는데, 진짜 전시물이 들어온 이상 상자는
            // <b>전시물을 가리는 짐</b>이다. 되살리려면 이 case 를 되돌리면 된다.
            case "대충기념관":
                Memorial(t, w, d, h);
                break;

            case "곰밥마당":   // 학생식당 — 한옥 급식소

                BapMadang(t, w, d, h, halfW, halfD);
                break;

            case "곰짝박수마당":  // 행사 — 무대 뒤 장비
                // ★ 2026-10-08 <b>상자로 흉내 낸 객석 열다섯을 걷어냈다.</b> 수연의 열 점은
                //   «객석» 이 아니라 <b>무대 뒤 장비</b>다 — 접의자가 수레에 실려 있고
                //   의상걸이와 플라이트 케이스가 있다. 의자를 펴 놓으면 그 설정과 어긋나고,
                //   무엇보다 15 x 11m 방에 상자 열다섯이 들어가면 장비가 설 자리가 없다.
                Block(t, "InBanner", new Vector3(0f, 2.6f, -halfD - 0.05f), Quaternion.identity,
                      new Vector3(w - 4f, 1.4f, 0.1f), ColRibbon, noCollider: true);
                StoryHall(t, "곰짝박수마당", w, d, 1.0f);
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

            // ★ 2026-09-28 <b>유저가 만든 리더십·경제·행정 대형 소품 열 점으로 지었다.</b>
            // 전에는 카운터 하나 + 서류함 다섯이라 36 × 22m 짜리 본관이 거의 비어 있었다.
            //
            // ★★★ 2026-09-29 <b>이걸 `default:` 에 뒀던 게 버그였다</b>
            // (수연: *"이 에셋들 웅지관꺼 아냐? 왜 여전히 바깥에 있어."*).
            // 제 방 case 가 없는 건물은 전부 여기로 떨어지는데, <b>곰솥관에 case 가 없었다.</b>
            // 그래서 웅지관 소품 열 점이 <b>곰솥관에도 한 벌 더</b> 생겼고, 자리 좌표는
            // 웅지관(36 × 22) 기준이라 곰솥관(20 × 14) 에서는 <b>여덟 점이 벽을 뚫고 마당에</b>
            // 서 있었다. 씬에서 재서 잡았다 — 의회단상 x −15.8(벽 ±9.4) · 문서보관벽 z −7.6(벽 ±6.4).
            //
            // <b>«아무것도 안 정한 방» 에 커다란 기본값을 두면 안 된다.</b> 빈 방으로 두는 건
            // 눈에 띄지만, 남의 방 물건이 한 벌 더 생기는 건 <b>한참 뒤에 밖에서 발견된다.</b>
            case "웅지관":
                Leadership(t, w, d, h);
                break;

            // ★ 2026-09-29 <b>드디어 제 방을 얻었다.</b> 옹기 테라스 · 메주 건조대 · 곡물 뒤주 ·
            // 도구 전시 · 맷돌 절구 — 여태 곰밥마당 뒷벽에 서 있던 다섯이다.
            case "곰솥관":
                Gomsot(t, w, d, h);
                break;

            default:
                // 아직 안 꾸민 방. <b>비워 둔다</b> — 여기에 무엇이든 놓으면 그 방 것이 아니다.
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

        // ── 뒷벽 ──
        // ★★★ 2026-09-29 <b>이 다섯을 곰솥관으로 옮겼다가 도로 가져왔다.</b>
        // 「음식을 만드는 물건이니 조리실습인 곰솥관 것」이라고 <b>내가 이름만 보고 판단</b>했는데,
        // 수연이 바로 잡아 줬다 — *"원래 곰밥마당 꺼잖아."*
        //
        // 근거는 이 묶음이 온 패키지에 그대로 적혀 있었다
        // (`Downloads\Gombap_Unique_Props_FBX\README_KO.md`):
        //
        // > <b>곰밥마당</b> — 한옥 · 곰박물관 · 급식실 소품 10종
        // > 메주 걸이·뒤주·도구 전시벽·맷돌 체험대: 넓은 <b>벽면을 따라 배치</b>해
        // > <b>급식실에 전통 식문화 전시</b> 느낌을 더합니다.
        //
        // L 다섯과 M 다섯은 <b>한 묶음으로 곰밥마당을 위해</b> 만들어진 것이고, L 은 전시,
        // M 은 설비다. 급식실에 식문화 전시가 있는 건 이상한 일이 아니야 — 실제 급식실이 그렇다.
        //
        // <b>물건을 옮기기 전에 그게 어느 묶음으로 왔는지부터 봐라.</b> 이름에서 읽어낸 용도는
        // 만든 사람의 의도가 아니다. 소품이 온 폴더에 README 가 같이 온다.
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
                // ★ 2026-10-06 유저: *"달릴 때 웅성관이 비비빅 거린다."* 원인 둘을 같이 잡았다.
                //
                // ① <b>확성기 색이 발광이었다.</b> <c>ColLantern</c>(#F5C069)은
                //    <see cref="Surfaces"/> 표에서 <b>석등 색</b>이라 무엇에 칠하든 발광이 된다.
                //    1.1m 짜리 발광 상자 둘이 건물 정면에 붙어 있으니, 달리면서 보면
                //    Bloom 이 화소 경계에서 들쑥날쑥해져 <b>깜빡이는 것처럼</b> 보인다.
                //    (화장실 바가지가 전구처럼 빛나던 것과 같은 함정 — 2026-09-22)
                //    → 발광이 아닌 <c>ColMapleGold</c>(#C9933E).
                //
                // ② <b>안테나가 지붕 상징물 속에 박혀 있었다.</b> 기둥이 z −1.8 인데
                //    상징물(방송 마이크, 깊이 4.25)이 지붕 한가운데라 z ±2.1 을 먹는다.
                //    면이 스치면서 깊이값이 뒤집힌다 → 뒤쪽(z −0.34d)으로 물렸다.
                for (int i = 0; i < 3; i++)
                    Block(t, $"Cross_{i}", new Vector3(0f, h + 5.4f + i * 0.9f, -d * 0.34f), Quaternion.identity,
                          new Vector3(3.2f - i * 0.7f, 0.16f, 0.16f), ColBearDark, noCollider: true);
                Block(t, "Mast", new Vector3(0f, h + 4f, -d * 0.34f), Quaternion.identity,
                      new Vector3(0.3f, 6f, 0.3f), ColBearDark, noCollider: true);
                // 확성기 뒷면이 <b>벽 안쪽 면(d/2 − 0.6)</b>과 정확히 같은 평면이었다.
                // 0.45 만큼 내서 벽 두께 한가운데에 묻힌다 — 어느 면과도 안 겹친다.
                for (int s = -1; s <= 1; s += 2)
                    Block(t, $"Horn_{s}", new Vector3(s * side * 0.6f, h + 1.2f, front + 0.45f), Quaternion.identity,
                          new Vector3(1.1f, 1.1f, 1.4f), ColMapleGold, noCollider: true);
                break;

            case "곰생회관":  // 학생회 — 현수막만
                // 2026-09-18 유저: *"학생회실이 나무판자로 가려져 있네."* 판자가 아니라
                // 게시판이 문 정면 2.6m 앞에 서 있었다 — 그때는 <b>옆으로 비켜</b> 놓았다.
                //
                // ★★ 2026-09-29 유저: *"학생회에서 쓸 관련 에셋들이 밖에 나와있는데 안으로
                // 좀 넣어주지 않으련."* 맞다 — 비킨다고 될 일이 아니었다. <b>학생회 게시판은
                // 학생회실 안에 있는 물건</b>이고, 마당에 세워 두니 «치우다 만 것» 으로 보인다.
                // 통째로 <see cref="Interior"/> 안으로 옮겼다.
                //
                // <b>현수막만 남긴다.</b> 이건 처마 밑에 거는 것이라 밖이 제 자리다 —
                // 밖에서 «여기가 학생회구나» 를 읽게 해 주는 유일한 것이기도 하다.
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

        // ★★ 2026-10-06 <b>캠퍼스에서 유일하게 불이 켜져 있는 건물.</b>
        //
        // 이 건물은 이미 둘을 숨기고 있다 — <b>폐과 딱지가 안 붙고</b>
        // (<see cref="CampusMood.neverClosed"/>), <b>판자도 안 박힌다</b>
        // (<see cref="CampusBoarding.alwaysOpen"/>). 거기에 셋째를 얹는다:
        // 0/8 에 열두 동이 판자로 막혀 캄캄할 때도 <b>여기만 등이 켜져 있다.</b>
        // 셋이 같은 건물을 가리키는데 <b>아무도 말해주지 않는다</b> — 그게 복선이야.
        var lit = new GameObject("LateShift").transform;
        lit.SetParent(t, false);
        lit.localPosition = new Vector3(0f, 0f, front);

        for (int i = -1; i <= 1; i += 2)
        {
            // 처마 밑 제등. ColLantern 은 <b>색이 곧 발광</b>이라 조명을 안 써도 켜 보인다
            Block(lit, $"Lantern_{i}", new Vector3(i * 7.5f, 7.0f, 0.5f), Quaternion.identity,
                  new Vector3(0.9f, 1.3f, 0.9f), ColLantern, noCollider: true);
            Block(lit, $"LanternCap_{i}", new Vector3(i * 7.5f, 7.74f, 0.5f), Quaternion.identity,
                  new Vector3(1.1f, 0.16f, 1.1f), ColBearDark, noCollider: true);
            Block(lit, $"LanternCord_{i}", new Vector3(i * 7.5f, 8.5f, 0.5f), Quaternion.identity,
                  new Vector3(0.06f, 1.4f, 0.06f), ColBearDark, noCollider: true);
        }

        // ★ 「창에서 새는 빛」은 <b>넣었다가 뺐다.</b> 한옥 문의 한지는 이미 발광이라
        //   어느 건물이든 문이 따뜻하게 빛나고 있고, 거기에 ColWindow 판을 더 붙이니
        //   <b>하얗게 날아가서</b> 살창과 겹친 고장처럼 보였다. 렌더를 안 봤으면 몰랐다.
        //   구분은 <b>제등</b>이 만든다 — 입구에 등을 단 건물은 캠퍼스에 여기뿐이다.

        // ★ 실시간 조명 하나. <b>발광 재질은 제 몸만 빛나지 바닥을 못 밝힌다</b> —
        //   「여기만 불이 켜져 있다」가 보이려면 <b>계단에 빛 웅덩이</b>가 깔려야 한다.
        //   기획서 §7.6 의 «실시간 그림자는 주요 조명 하나만» 은 그림자 얘기고,
        //   이건 그림자를 안 만든다(캠퍼스 광원: 태양 + 이것 = 2개).
        var lamp = new GameObject("LateShiftLight");
        lamp.transform.SetParent(lit, false);
        lamp.transform.localPosition = new Vector3(0f, 5.5f, 1.8f);
        var light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.88f, 0.68f);
        light.intensity = 3.2f;
        light.range = 24f;
        light.shadows = LightShadows.None;
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
        const float ceiling = CampusCeilingY;

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
