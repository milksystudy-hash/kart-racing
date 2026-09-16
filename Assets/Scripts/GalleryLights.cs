using UnityEngine;

/// <summary>
/// 전시품 여덟 개를 다 모으면 전시실에 불이 들어온다 — 어두운 박물관에서 문을 연 박물관으로.
///
/// 이게 이 게임에서 <b>수집의 대가가 눈에 보이는 유일한 자리</b>야. 숫자가 8/8 이 되는 것보다
/// 방이 밝아지는 게 훨씬 크게 읽힌다. 기획서 §3.4 의 해금 보상이 실제로 어디에 쌓이는지를
/// 보여주는 장소가 전시실이라는 것과도 맞아.
///
/// 천천히 밝아진다. 한 프레임에 탁 켜지면 "설정이 바뀌었네" 로 보이고,
/// 몇 초에 걸쳐 올라오면 "불이 켜지네" 로 보인다. 같은 값인데 읽히는 게 달라.
///
/// <b>머티리얼은 안 건드린다.</b> 발광 재질은 .mat 에셋이라 실행 중에 바꾸면 에디터에서
/// 그 파일이 실제로 변해버리고, 다른 씬까지 따라 바뀐다. 조명과 환경광만 움직인다.
/// </summary>
public class GalleryLights : MonoBehaviour
{
    [Header("조명")]
    [Tooltip("천창 — 어두울 때도 켜져 있다")]
    public Light skylight;
    [Tooltip("불이 들어오면 켜지는 천장 등. 어두울 때는 세기 0")]
    public Light[] ceilingLights;

    [Header("세기")]
    public float skylightDark = 0.85f;
    public float skylightBright = 1.25f;
    public float ceilingBright = 1.6f;

    [Header("환경광")]
    public Color ambientSkyDark     = new Color(0.40f, 0.43f, 0.52f);
    public Color ambientEquatorDark = new Color(0.30f, 0.31f, 0.36f);
    public Color ambientGroundDark  = new Color(0.18f, 0.17f, 0.18f);

    public Color ambientSkyBright     = new Color(0.72f, 0.70f, 0.64f);
    public Color ambientEquatorBright = new Color(0.58f, 0.55f, 0.50f);
    public Color ambientGroundBright  = new Color(0.34f, 0.31f, 0.28f);

    [Tooltip("다 모은 걸 확인했을 때 밝아지는 데 걸리는 시간(초)")]
    public float fadeSeconds = 3f;

    /// <summary>0 이면 어둡고 1 이면 불이 다 들어온 상태.</summary>
    public float Lit { get; private set; }

    public static bool AllCollected => CollectionState.Count >= ExhibitCatalogue.Count;

    void Start()
    {
        // 이미 다 모은 채로 들어왔으면 그래도 어두운 데서 시작해서 켜지는 걸 보여준다.
        // 그 3초가 이 장면의 보상이야 — 처음부터 밝으면 아무 일도 안 일어난 게 된다.
        Lit = 0f;
        Apply();
    }

    void Update()
    {
        float target = AllCollected ? 1f : 0f;

        // 로비 디버그 키로 수집을 지웠다 채웠다 할 수 있어서, 양쪽으로 움직이게 둔다.
        Lit = fadeSeconds > 0f
            ? Mathf.MoveTowards(Lit, target, Time.deltaTime / fadeSeconds)
            : target;

        Apply();
    }

    void Apply()
    {
        float t = Mathf.SmoothStep(0f, 1f, Lit);

        if (skylight != null) skylight.intensity = Mathf.Lerp(skylightDark, skylightBright, t);

        if (ceilingLights != null)
            foreach (var light in ceilingLights)
                if (light != null) light.intensity = ceilingBright * t;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = Color.Lerp(ambientSkyDark, ambientSkyBright, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(ambientEquatorDark, ambientEquatorBright, t);
        RenderSettings.ambientGroundColor  = Color.Lerp(ambientGroundDark, ambientGroundBright, t);
    }
}
