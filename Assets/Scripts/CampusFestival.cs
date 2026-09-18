using UnityEngine;

/// <summary>
/// <b>여덟 개를 다 모으면 캠퍼스가 축제가 된다.</b>
///
/// 2026-09-18 유저: *"전시실은 덮개로 전후 연출이 되어 있잖아. 캠퍼스만의 전용 연출이 없을까.
/// 닌텐도 DS 게임 같은 연출을 좋아하는데."*
///
/// DS 시절 게임의 연출은 대개 <b>물건이 늘어나는 것</b>이었다 — 조명을 바꾸거나 셰이더를
/// 넣는 게 아니라, 어제 없던 깃발이 오늘 걸려 있고 사람이 하나 더 서 있다. 하드웨어가
/// 약해서 그렇게 할 수밖에 없었는데, 그게 오히려 <b>"내가 바꿨다"</b> 를 제일 크게 만든다.
///
/// 그래서 여기도 색을 안 만지고 <b>물건을 건다</b>:
///
/// | | 다 모으기 전 | 다 모은 뒤 |
/// |---|---|---|
/// | 문 | 못질한 판자 (<see cref="CampusBoarding"/>) | 걷힌다 — 한 판에 한 동씩 |
/// | 현판 | 빨간 폐과 딱지 (<see cref="CampusMood"/>) | 떨어진다 |
/// | 하늘 | 아무것도 없음 | <b>만국기 · 청사초롱 · 풍선</b> |
/// | 정문 | 아무것도 없음 | <b>개교 현수막</b> |
///
/// 앞의 둘은 <b>줄어드는 연출</b>이고 이건 <b>늘어나는 연출</b>이다. 둘 다 있어야
/// "없어지는 걸 막았다" 가 "지켜냈다" 로 넘어간다 — 빈칸이 채워지는 것만으로는
/// 원래대로 돌아온 것뿐이지 이긴 게 아니야.
///
/// <b>기하는 한 번만 짓고 켜고 끈다.</b> 매 프레임 만들면 프레임이 죽고,
/// 지웠다 만들면 F9/F10 으로 오갈 때마다 자리가 달라진다.
/// </summary>
public class CampusFestival : MonoBehaviour
{
    [Tooltip("만국기를 걸 높이(m)")]
    public float flagHeight = 9.5f;

    [Tooltip("축제가 켜지는 데 걸리는 시간(초). 한 프레임에 켜지면 설정이 바뀐 것처럼 보인다")]
    public float riseSeconds = 1.6f;

    static readonly Color[] FlagColors =
    {
        new Color32(0xC4, 0x45, 0x3E, 0xFF),   // 붉은색
        new Color32(0xF0, 0xC0, 0x70, 0xFF),   // 노란색
        new Color32(0x4E, 0x7A, 0x70, 0xFF),   // 청기와색
        new Color32(0xEF, 0xE7, 0xD6, 0xFF),   // 크림색
        new Color32(0x7E, 0x94, 0x62, 0xFF),   // 초록색
    };

    Transform party;
    float shown;

    void Start()
    {
        party = Build();
        shown = Done ? 1f : 0f;
        Apply();
    }

    void Update()
    {
        float goal = Done ? 1f : 0f;
        if (Mathf.Approximately(shown, goal)) return;

        shown = riseSeconds > 0f
            ? Mathf.MoveTowards(shown, goal, Time.deltaTime / riseSeconds)
            : goal;
        Apply();
    }

    static bool Done => ExhibitCatalogue.Count > 0 && CollectionState.Count >= ExhibitCatalogue.Count;

    /// <summary>
    /// 켜질 때 <b>위에서 내려온다.</b> 그냥 나타나면 "언제 생겼지" 가 되고,
    /// 내려오면 "지금 걸렸다" 가 된다 — 같은 물건인데 읽히는 게 다르다.
    /// </summary>
    void Apply()
    {
        if (party == null) return;

        bool on = shown > 0.001f;
        if (party.gameObject.activeSelf != on) party.gameObject.SetActive(on);
        if (!on) return;

        float t = Mathf.SmoothStep(0f, 1f, shown);
        party.localPosition = new Vector3(0f, Mathf.Lerp(14f, 0f, t), 0f);
    }

    Transform Build()
    {
        var root = new GameObject("Festival").transform;
        root.SetParent(transform, false);

        // ---- 만국기 : 캠퍼스 하늘을 가로지르는 줄 넷 ----
        // 한 줄이면 빨래처럼 보인다. 여러 줄이 <b>교차</b>해야 축제로 읽혀.
        Garland(root, new Vector3(-95f, flagHeight, -95f), new Vector3(95f, flagHeight + 3f, 95f), 26);
        Garland(root, new Vector3(95f, flagHeight, -95f), new Vector3(-95f, flagHeight + 3f, 95f), 26);
        Garland(root, new Vector3(0f, flagHeight + 5f, -95f), new Vector3(0f, flagHeight + 5f, 95f), 24);
        Garland(root, new Vector3(-95f, flagHeight + 2f, 0f), new Vector3(95f, flagHeight + 2f, 0f), 24);

        // ---- 청사초롱 : 정문에서 본관까지 길을 따라 ----
        // 길을 따라 늘어선 등은 <b>여기로 오라</b>는 뜻이라 안내 표지 노릇도 한다.
        for (int i = 0; i < 14; i++)
        {
            float z = Mathf.Lerp(88f, -92f, i / 13f);
            for (int s = -1; s <= 1; s += 2)
            {
                var at = new Vector3(s * 11f, 0f, z);
                Cube(root, $"LampPost_{i}_{s}", at + Vector3.up * 2.2f,
                     new Vector3(0.16f, 4.4f, 0.16f), new Color32(0x6B, 0x4A, 0x33, 0xFF));
                Cube(root, $"Lantern_{i}_{s}", at + Vector3.up * 4.3f,
                     new Vector3(0.66f, 0.9f, 0.66f),
                     i % 2 == 0 ? new Color32(0xC4, 0x45, 0x3E, 0xFF) : new Color32(0x4E, 0x7A, 0x70, 0xFF),
                     glow: true);
            }
        }

        // ---- 개교 현수막 : 정문 안쪽 ----
        Cube(root, "Banner", new Vector3(0f, 7.4f, 96f), new Vector3(26f, 2.6f, 0.2f),
             new Color32(0xEF, 0xE7, 0xD6, 0xFF));
        Cube(root, "BannerTop", new Vector3(0f, 8.8f, 95.8f), new Vector3(26.6f, 0.3f, 0.26f),
             new Color32(0xC4, 0x45, 0x3E, 0xFF));
        Cube(root, "BannerBottom", new Vector3(0f, 6.0f, 95.8f), new Vector3(26.6f, 0.3f, 0.26f),
             new Color32(0xC4, 0x45, 0x3E, 0xFF));

        // ---- 풍선 : 본관 앞 광장에 묶어 둔 다발 ----
        var random = new System.Random(2026);
        for (int i = 0; i < 18; i++)
        {
            float a = (float)random.NextDouble() * Mathf.PI * 2f;
            float r = 12f + (float)random.NextDouble() * 22f;
            var at = new Vector3(Mathf.Cos(a) * r, 0f, -78f + Mathf.Sin(a) * r * 0.5f);
            float rise = 5f + (float)random.NextDouble() * 4f;

            Cube(root, $"String_{i}", at + Vector3.up * (rise * 0.5f),
                 new Vector3(0.05f, rise, 0.05f), new Color32(0xEF, 0xE7, 0xD6, 0xFF));
            Ball(root, $"Balloon_{i}", at + Vector3.up * (rise + 0.5f),
                 new Vector3(1.1f, 1.35f, 1.1f), FlagColors[i % FlagColors.Length]);
        }

        return root;
    }

    /// <summary>줄 하나에 삼각기를 매단다. 줄이 <b>가운데로 처져야</b> 걸어 놓은 것처럼 보인다.</summary>
    void Garland(Transform parent, Vector3 from, Vector3 to, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            var at = Vector3.Lerp(from, to, t);
            at.y -= Mathf.Sin(t * Mathf.PI) * 2.6f;   // 처짐

            Cube(parent, $"Flag_{from.x:0}_{i}", at, new Vector3(0.9f, 1.2f, 0.06f),
                 FlagColors[i % FlagColors.Length]);
        }
    }

    void Cube(Transform parent, string name, Vector3 at, Vector3 size, Color color, bool glow = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Strip(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial =
            glow ? FlatMaterial.Get(color, Finish.발광) : FlatMaterial.Get(color);
    }

    void Ball(Transform parent, string name, Vector3 at, Vector3 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Strip(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(color);
    }

    static void Strip(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}
