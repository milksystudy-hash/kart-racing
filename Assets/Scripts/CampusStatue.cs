using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <b>명예 후원자 동상</b> — 웅지관(행정동) 앞에 선 시의원 권대호의 청동상.
///
/// 유저: *"그 뚱뚱한 시의원에 근육(이라고 주장하는) 동상 세워도 돼?"*
///
/// ★ <b>희화화하지 않는다.</b> 이야기 설계 문서의 규칙이다 —
/// *"우스운 악당은 안 무섭고, 안 무서우면 이겨도 안 시원하다.
/// 웃기는 건 주인공 몫이고 악당은 정색해야 다크코미디가 된다."*
///
/// 그래서 이 동상은 <b>진지하게 웅장하다.</b> 명판도 진지하고, 게임은 농담을 한마디도 안 한다.
/// 웃긴 건 <b>실물을 아는 플레이어 머릿속</b>에서만 일어난다 — 시트의 실루엣 비교를 본 사람은
/// 이 역삼각형 체형이 무엇을 주장하는지 안다. 「근육(자칭)」 같은 셀프 디스 명판을 붙이는
/// 순간 게임이 제 농담을 설명하는 꼴이 되고, 그게 바로 희화화야.
///
/// 웃음은 나중에 <b>세진</b>이 지나가면서 한마디 하는 것으로 붙인다(유저 계획).
///
/// <b>왜 여기 서 있나:</b> 동상이 있다는 건 그가 <b>예전부터 이 박물관과 엮여 있었다</b>는 뜻이다.
/// 수집품 6번(비밀 계약서)이 나올 때 "아, 저 동상" 이 되고, 설명이 필요 없어진다.
/// 행정동 앞인 것도 겹친다 — 폐과 딱지가 <b>절대 안 붙는 유일한 건물</b> 앞이야.
///
/// <b>씬을 안 건드린다.</b> <see cref="CanteenDressing"/> 과 같은 방식으로 실행할 때
/// 스스로 들어온다 — Campus 씬을 다시 구우면 유저가 손으로 놓은 게 날아가니까.
/// 나중에 씬을 다시 구울 일이 생기면 <c>CampusBuilder</c> 로 옮겨도 된다.
/// </summary>
public class CampusStatue : MonoBehaviour
{
    const string RootName = "DonorStatue";

    /// <summary>
    /// 웅지관(0, 0, −107 · 36 × 22 × 13)의 정면 오른쪽.
    /// 측정해서 골랐다 — 제일 가까운 물건(깃대)까지 5.7m, 코스까지 6m, 문 앞은 비어 있다.
    /// 문 정면(x 0)을 피한 이유는 <b>문 앞에 뭘 놓지 마라</b>(2026-09-18) 그대로야.
    /// </summary>
    static readonly Vector3 Spot = new Vector3(13f, 0f, -90f);

    static readonly Color Bronze = new Color32(0x7E, 0x5F, 0x38, 0xFF);
    static readonly Color BronzeLit = new Color32(0x9A, 0x77, 0x46, 0xFF);
    static readonly Color Stone = new Color32(0x8E, 0x8A, 0x80, 0xFF);
    static readonly Color StoneDark = new Color32(0x6E, 0x6A, 0x62, 0xFF);
    static readonly Color Plate = new Color32(0xB0, 0x8E, 0x4C, 0xFF);

    // ---- 들어오기 ----------------------------------------------------------

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
        // 장식이 게임을 망가뜨리면 안 된다. 씬이 로드되는 길목이라 여기서 예외가 나면
        // 그 뒤 초기화가 통째로 건너뛰어진다 — 실패하면 조용히 포기한다.
        try
        {
            // 웅지관이 있는 씬에만 선다. 이름은 씬에 하나뿐이다.
            var hall = GameObject.Find("웅지관");
            if (hall == null) return;

            var host = hall.transform.parent;
            if (host == null) host = hall.transform;
            if (host.Find(RootName) != null) return;   // 이미 세웠다

            var go = new GameObject(RootName);

            // ★ AddComponent 는 Awake 를 그 자리에서 부른다. 꺼진 채로 만들어야
            // 값을 꽂은 뒤에 Awake 가 돈다(2026-09-21 여기서 한 번 터졌다).
            go.SetActive(false);
            go.transform.SetParent(host, false);
            go.transform.position = Spot;
            // 캠퍼스 안쪽(+Z)을 본다 — 건물을 등지고 선다. 후원자 동상은 늘 그렇게 선다.
            go.transform.rotation = Quaternion.identity;

            go.AddComponent<CampusStatue>();
            go.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[동상] 세우다 실패했지만 게임은 계속된다: {e}");
        }
    }

    // ---- 짓기 --------------------------------------------------------------

    void Awake()
    {
        Pedestal();
        Figure();
        Plaque();
    }

    /// <summary>
    /// 기단. <b>세 단으로 쌓는다</b> — 한 덩이면 받침이 아니라 상자로 보인다.
    /// 여기만 콜라이더를 남긴다. 동상을 뚫고 지나가면 있으나 마나니까.
    /// </summary>
    void Pedestal()
    {
        Block("Base", new Vector3(0f, 0.11f, 0f), new Vector3(2.60f, 0.22f, 2.60f), StoneDark, solid: true);
        Block("Step", new Vector3(0f, 0.32f, 0f), new Vector3(2.20f, 0.22f, 2.20f), Stone, solid: true);
        Block("Pillar", new Vector3(0f, 0.95f, 0f), new Vector3(1.70f, 1.06f, 1.70f), Stone, solid: true);
        Block("Cap", new Vector3(0f, 1.52f, 0f), new Vector3(1.90f, 0.14f, 1.90f), StoneDark);
    }

    /// <summary>
    /// 상. <b>키 2.4m, 기단까지 4.0m.</b> 사람(1.75)의 두 배가 넘어야 «기념» 으로 읽힌다.
    ///
    /// 체형은 <b>역삼각형</b>이다 — 어깨 1.30, 허리 0.62. 실물은 178cm 에 둥근 사람이야.
    /// 그 차이가 이 동상이 하는 유일한 말이고, <b>동상 자신은 농담을 안 한다.</b>
    ///
    /// 정장 차림으로 뒀다. 웃통을 벗기면 대놓고 웃기라는 신호가 돼서 규칙을 어긴다 —
    /// 정장인데 체형이 저러면 <b>실제로 있을 법한 허영</b>이라 더 서늘하다.
    /// </summary>
    void Figure()
    {
        Transform body = Sub("Figure", new Vector3(0f, 1.59f, 0f));

        // 다리 — 한 발을 앞으로. 차렷 자세는 기념비가 아니라 마네킹이다.
        Limb(body, "Leg_L", new Vector3(-0.26f, 0.42f, 0.10f), new Vector3(-8f, 0f, 0f), 0.34f, 0.92f, Bronze);
        Limb(body, "Leg_R", new Vector3(0.26f, 0.42f, -0.06f), new Vector3(4f, 0f, 0f), 0.34f, 0.92f, Bronze);
        Ball(body, "Shoe_L", new Vector3(-0.26f, 0.05f, 0.26f), new Vector3(0.34f, 0.12f, 0.52f), Bronze);
        Ball(body, "Shoe_R", new Vector3(0.26f, 0.05f, 0.04f), new Vector3(0.34f, 0.12f, 0.52f), Bronze);

        // 몸통 — 허리 0.62, 가슴 1.16, 어깨 1.30. 위로 갈수록 벌어진다.
        Ball(body, "Waist", new Vector3(0f, 0.98f, 0f), new Vector3(0.62f, 0.34f, 0.46f), Bronze);
        Ball(body, "Chest", new Vector3(0f, 1.34f, 0.01f), new Vector3(1.16f, 0.58f, 0.62f), Bronze);
        Ball(body, "Shoulders", new Vector3(0f, 1.52f, 0f), new Vector3(1.30f, 0.34f, 0.56f), Bronze);

        // 재킷 앞섶 — 정장이라는 게 보여야 «허영» 이 된다
        Ball(body, "Lapel", new Vector3(0f, 1.30f, 0.30f), new Vector3(0.44f, 0.52f, 0.10f), BronzeLit);

        // 오른팔은 앞으로 뻗어 가리킨다 — 기념비의 문법이다. "여기에 리조트가 들어섭니다."
        Limb(body, "Arm_R", new Vector3(0.62f, 1.46f, 0.34f), new Vector3(68f, 0f, -14f), 0.28f, 0.98f, Bronze);
        Ball(body, "Hand_R", new Vector3(0.74f, 1.62f, 0.86f), new Vector3(0.24f, 0.20f, 0.28f), Bronze);

        // 왼손은 허리에. 양팔을 다 뻗으면 허수아비가 된다.
        Limb(body, "Arm_L", new Vector3(-0.70f, 1.22f, 0.02f), new Vector3(0f, 0f, 22f), 0.28f, 0.86f, Bronze);
        Ball(body, "Hand_L", new Vector3(-0.50f, 0.92f, 0.06f), new Vector3(0.22f, 0.20f, 0.24f), Bronze);

        // 머리 — 작게. 어깨 대비 머리가 작아야 몸이 커 보인다(영웅상의 오랜 수법이야).
        Ball(body, "Neck", new Vector3(0f, 1.68f, 0f), new Vector3(0.26f, 0.16f, 0.26f), Bronze);
        Ball(body, "Head", new Vector3(0f, 1.86f, 0.01f), new Vector3(0.40f, 0.44f, 0.42f), Bronze);
        Ball(body, "Hair", new Vector3(0f, 1.98f, -0.03f), new Vector3(0.44f, 0.28f, 0.44f), BronzeLit);
    }

    /// <summary>
    /// 명판. <b>정색한 문구만</b> 적는다 — 여기에 농담을 넣으면 규칙을 어긴다.
    /// 글자가 없으면(폰트를 못 찾으면) 판만 남는데, 그래도 «명판» 으로는 읽힌다.
    /// </summary>
    void Plaque()
    {
        Block("Plate", new Vector3(0f, 1.02f, 0.87f), new Vector3(1.24f, 0.46f, 0.05f), Plate);

        var font = HudFont.Resolve(null);
        if (font == null) return;

        var go = new GameObject("PlateText");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.02f, 0.91f);

        // ★ 글자는 <b>보는 사람 반대쪽</b>을 보게 단다. 현판이 전부 거울 글씨로 나왔던
        // 그 규칙이야(2026-09-17). 보는 사람이 +Z 에 있으니 −Z 를 본다.
        go.transform.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        var text = go.AddComponent<TextMesh>();
        text.font = font;
        text.text = "명예 후원자\n시의원 권대호";
        text.fontSize = 120;                       // 크게 구워서 줄여 쓴다 — 작게 구우면 계단이 보인다
        text.characterSize = 0.155f * 10f / 120f;  // 한 줄 높이 0.155m, 두 줄
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color32(0x3A, 0x2E, 0x1E, 0xFF);
        go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
    }

    // ---- 도우미 ------------------------------------------------------------

    Transform Sub(string name, Vector3 at)
    {
        var go = new GameObject(name).transform;
        go.SetParent(transform, false);
        go.localPosition = at;
        return go;
    }

    /// <summary>기단 — 상자. <paramref name="solid"/> 면 콜라이더를 남긴다.</summary>
    Transform Block(string name, Vector3 at, Vector3 size, Color color, bool solid = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var t = Dress(go, transform, name, at, Vector3.zero, size, color, Finish.석재, solid);
        return t;
    }

    /// <summary>구 — 사람 형태는 전부 이것과 캡슐로. 모서리가 서면 조각상이 아니라 상자다.</summary>
    Transform Ball(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        return Dress(go, parent, name, at, Vector3.zero, size, color, Finish.금속, false);
    }

    /// <summary>캡슐 — 팔다리. 캡슐 메시는 높이 2 라 길이를 반으로 나눠 준다.</summary>
    Transform Limb(Transform parent, string name, Vector3 at, Vector3 tilt,
                   float thick, float length, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        return Dress(go, parent, name, at, tilt,
                     new Vector3(thick, length * 0.5f, thick), color, Finish.금속, false);
    }

    Transform Dress(GameObject go, Transform parent, string name, Vector3 at, Vector3 tilt,
                    Vector3 size, Color color, Finish finish, bool solid)
    {
        go.name = name;

        // 기단 말고는 전부 콜라이더를 뗀다. 눌린 캡슐 콜라이더는 커다란 구로 부푼다(CLAUDE.md).
        if (!solid)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localRotation = Quaternion.Euler(tilt);
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color, finish);
        return go.transform;
    }
}
