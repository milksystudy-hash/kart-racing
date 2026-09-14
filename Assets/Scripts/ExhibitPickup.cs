using UnityEngine;

/// <summary>
/// 트랙에 놓인 수집품. 카트가 지나가면 전시실에 영구히 등록된다.
///
/// 주우면 **노란 전광등**이 하늘로 솟았다가 사라진다 — 레이스 중엔 화면을 볼 겨를이 없으니
/// 큼직한 신호가 필요해. 아직 안 주운 것은 은은하게 빛나면서 돌고 있어서 멀리서도 보인다.
///
/// 이미 모은 것은 다음 레이스부터 아예 안 나타난다. 같은 걸 두 번 주울 이유가 없으니까.
/// (코인처럼 매번 리셋되는 점수용 수집품과는 다른 물건이야 — 이건 한 번 얻으면 영구다.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExhibitPickup : MonoBehaviour
{
    [Header("무엇을 주우나")]
    [Tooltip("ExhibitCatalogue 의 id 와 같아야 전시실에 들어간다")]
    public string itemId = "";

    [Header("아직 안 주웠을 때")]
    [Tooltip("돌면서 위아래로 떠다니는 부분. 주우면 사라진다")]
    public Transform visual;
    public float spinSpeed = 70f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 1.6f;
    public Light idleLight;

    [Header("주웠을 때 — 노란 전광등")]
    public float beaconSeconds = 1.5f;
    public float beaconHeight = 18f;
    public float beaconRadius = 0.7f;
    public Color beaconColor = new Color(1f, 0.86f, 0.25f);
    public float beaconLightIntensity = 40f;

    /// <summary>HUD 가 잠깐 띄울 안내문. 가장 최근에 주운 것.</summary>
    public static string LastMessage { get; private set; } = "";
    public static float LastMessageTime { get; private set; } = -99f;

    Vector3 visualHome;
    float bobPhase;
    bool taken;

    Transform beacon;
    Light beaconLight;
    float beaconTimer;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (visual != null) visualHome = visual.localPosition;

        // 이미 모은 것은 트랙에 나타나지 않는다
        if (CollectionState.Has(itemId)) gameObject.SetActive(false);
    }

    void Update()
    {
        if (taken) { UpdateBeacon(); return; }

        if (visual == null) return;
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        bobPhase += Time.deltaTime * bobSpeed;
        visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(bobPhase) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        if (other.GetComponentInParent<KartController>() == null) return;
        Take();
    }

    void Take()
    {
        taken = true;
        CollectionState.Collect(itemId);

        int caseNumber = ExhibitCatalogue.CaseNumberOf(itemId);
        LastMessage = caseNumber > 0
            ? $"{ExhibitCatalogue.NameOf(itemId)}  ·  전시실 {caseNumber}번에 등록"
            : ExhibitCatalogue.NameOf(itemId);
        LastMessageTime = Time.time;

        if (visual != null) visual.gameObject.SetActive(false);
        if (idleLight != null) idleLight.enabled = false;
        GetComponent<Collider>().enabled = false;

        SpawnBeacon();
    }

    /// <summary>하늘로 솟는 노란 기둥. 투명도 대신 크기를 줄여서 사라지게 한다 —
    /// 반투명 머티리얼을 안 쓰니 URP 설정에 상관없이 똑같이 보인다.</summary>
    void SpawnBeacon()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Beacon";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(beaconRadius, 0.1f, beaconRadius);
        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(beaconColor);
        beacon = go.transform;

        var lightGo = new GameObject("BeaconLight");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = Vector3.up * 2f;
        beaconLight = lightGo.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = beaconColor;
        beaconLight.range = 22f;
        beaconLight.intensity = beaconLightIntensity;
        beaconLight.shadows = LightShadows.None;

        beaconTimer = 0f;
    }

    void UpdateBeacon()
    {
        if (beacon == null) { Destroy(gameObject); return; }

        beaconTimer += Time.deltaTime;
        float t = Mathf.Clamp01(beaconTimer / Mathf.Max(0.01f, beaconSeconds));

        // 앞쪽 35% 동안 솟아오르고, 나머지 동안 가늘어지며 사라진다
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.35f));
        float fade = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((t - 0.35f) / 0.65f));

        float height = beaconHeight * rise;
        beacon.localScale = new Vector3(beaconRadius * fade, height * 0.5f, beaconRadius * fade);
        beacon.localPosition = new Vector3(0f, height * 0.5f, 0f);

        if (beaconLight != null) beaconLight.intensity = beaconLightIntensity * fade;

        if (t >= 1f) Destroy(gameObject);
    }
}
