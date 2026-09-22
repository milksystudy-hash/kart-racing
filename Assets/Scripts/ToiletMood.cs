using UnityEngine;

/// <summary>
/// <b>화장실이 살아 있게 만든다.</b>
///
/// 2026-09-22 유저: *"화장실 연출도 뭐 없을까. 게임처럼. 지금 연출이 너무 약한 것 같은데."*
///
/// 방은 이미 손봤다(반자 3.3m · 모서리 기둥 · 굽도리 · 줄눈 · 거울등 · 배관).
/// 그런데 그건 전부 <b>가만히 있는 것</b>이라, 걸어 들어가면 «잘 지어 둔 방» 이지
/// «지금 여기서 무슨 일이 벌어지는 곳» 은 아니다.
///
/// <b>움직이는 것 셋과 반응하는 것 하나</b>를 더한다. 전부 이미 선 물건을 쓰거나
/// 얇은 판 하나를 켜고 끄는 것이라 조각 수가 거의 안 는다:
///
/// <list type="bullet">
/// <item><b>형광등이 깜빡인다</b> — 천장등 하나만. 방이 어두워지면 안 되니 한 짝만 껌뻑이고,
///       실시간 조명도 같이 흔들려서 바닥과 벽에 그 깜빡임이 닿는다.
///       «정비가 안 된 화장실» 이라 정비 곰 대사와도 이어진다.</item>
/// <item><b>환풍기가 돈다</b> — 벽에 붙은 살은 그대로 두고 <b>뒤에서 날개가</b> 돈다.
///       돌아가는 물건이 하나 있으면 방 전체가 «작동 중» 으로 읽힌다.</item>
/// <item><b>물이 흐른다</b> — 수도를 틀면 배수구 쪽으로 물자국이 생기고 거울에 김이 서린다.
///       <see cref="Faucet"/> 가 이미 «물조각을 켜고 끄는」 구조라 목록에 더 넣기만 하면 된다.</item>
/// <item><b>사용 중 표시</b> — 곰이 든 칸 문에 붙는다. 닫으면 빨강, 열면 초록.
///       문을 여닫는 게 <b>화면에 남는 결과</b>가 되어서 여는 맛이 생긴다.</item>
/// </list>
///
/// <b>재질을 건드리지 않는다.</b> 발광 면은 `.mat` 에셋이라 런타임에 색을 쓰면
/// 디스크 파일이 바뀌어 다른 씬까지 따라간다(`GalleryLights` 에서 이미 정한 규칙).
/// 깜빡임은 <b>오브젝트를 켜고 끄는 것</b>과 <b>Light.intensity</b> 로만 한다.
/// </summary>
public class ToiletMood : MonoBehaviour
{
    [Tooltip("깜빡일 천장등. 하나만 준다 — 둘 다 껌뻑이면 방이 캄캄해진다")]
    public Transform flickerLamp;

    [Tooltip("같이 흔들릴 실시간 조명")]
    public Light roomLight;

    [Tooltip("도는 환풍기 날개")]
    public Transform fanBlades;

    [Tooltip("곰이 든 칸 문")]
    public HingedDoor bearStall;

    [Tooltip("그 문에 붙은 «사용 중» 판. 닫히면 켜진다")]
    public GameObject busyMark;

    [Tooltip("그 문에 붙은 «비었음» 판. 열리면 켜진다")]
    public GameObject freeMark;

    float baseIntensity = 1.35f;
    float nextBlink;
    float blinkUntil;

    void Start()
    {
        if (roomLight != null) baseIntensity = roomLight.intensity;
        nextBlink = Time.time + Random.Range(3f, 9f);
        Mark(bearStall != null && bearStall.Open);
    }

    void Update()
    {
        Flicker();

        // 환풍기 — 느리게. 빠르면 «선풍기» 로 보이고 눈이 피곤하다.
        if (fanBlades != null) fanBlades.Rotate(0f, 0f, 68f * Time.deltaTime, Space.Self);

        if (bearStall != null) Mark(bearStall.Open);
    }

    /// <summary>
    /// 오래 멀쩡히 켜져 있다가 <b>짧게 몇 번</b> 껌뻑인다. 계속 껌뻑이면 그건 고장이 아니라
    /// 화면 효과가 되고, 오래 보고 있기 힘들다(주행 진동을 늘 켜두면 안 되는 것과 같다).
    /// </summary>
    void Flicker()
    {
        float now = Time.time;

        if (now > nextBlink && now > blinkUntil)
        {
            blinkUntil = now + Random.Range(0.35f, 0.9f);
            nextBlink = blinkUntil + Random.Range(6f, 16f);
        }

        bool on = true;
        if (now < blinkUntil)
        {
            // 껌뻑이는 동안에도 <b>불규칙</b>해야 한다. 일정한 점멸은 신호등으로 보인다.
            on = Mathf.PerlinNoise(now * 26f, 0.7f) > 0.42f;
        }

        if (flickerLamp != null && flickerLamp.gameObject.activeSelf != on)
            flickerLamp.gameObject.SetActive(on);

        if (roomLight != null)
            roomLight.intensity = on ? baseIntensity : baseIntensity * 0.35f;
    }

    void Mark(bool open)
    {
        if (busyMark != null && busyMark.activeSelf == open) busyMark.SetActive(!open);
        if (freeMark != null && freeMark.activeSelf != open) freeMark.SetActive(open);
    }
}
