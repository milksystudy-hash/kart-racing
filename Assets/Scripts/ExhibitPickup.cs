using UnityEngine;

/// <summary>
/// 트랙에 놓인 수집품. 플레이어 카트가 지나가면 전시실에 영구히 등록된다.
///
/// 주우면 그냥 사라지고 화면에 안내문만 잠깐 뜬다.
/// (레이스 중에 하늘로 솟는 전광등을 쐈었는데, 원래 노란 불빛은 **전시실에서** 모은 개수만큼
///  켜지는 거였다 — 2026-09-16 에 바로잡았다. 달리는 중엔 화면이 조용한 게 낫다.)
///
/// 이미 모은 것과 이번 장 것이 아닌 것은 아예 안 나타난다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExhibitPickup : MonoBehaviour
{
    [Header("무엇을 주우나")]
    [Tooltip("ExhibitCatalogue 의 id 와 같아야 전시실에 들어간다")]
    public string itemId = "";
    [Tooltip("이 장을 진행 중일 때만 트랙에 나타난다. 0 프롤로그 · 1~3 메인 · 4 마지막 장")]
    public int chapter = 1;

    [Header("아직 안 주웠을 때")]
    [Tooltip("돌면서 위아래로 떠다니는 부분")]
    public Transform visual;
    public float spinSpeed = 70f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 1.6f;
    public Light idleLight;

    /// <summary>HUD 가 잠깐 띄울 안내문. 가장 최근에 주운 것.</summary>
    public static string LastMessage { get; private set; } = "";
    public static float LastMessageTime { get; private set; } = -99f;

    Vector3 visualHome;
    float bobPhase;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (visual != null) visualHome = visual.localPosition;

        // 이번 장의 물건이 아니면 안 나온다. 여덟 개를 한꺼번에 깔면 한 바퀴에 다 주워버린다.
        if (chapter != StoryProgress.CurrentChapter) { gameObject.SetActive(false); return; }

        // 이미 모은 것도 안 나온다.
        if (CollectionState.Has(itemId)) gameObject.SetActive(false);

        경고_한번만();
    }

    void Update()
    {
        if (visual == null) return;
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        bobPhase += Time.deltaTime * bobSpeed;
        visual.localPosition = visualHome + Vector3.up * (Mathf.Sin(bobPhase) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        // 플레이어 카트만 줍는다. AI 가 이야기 증거를 먼저 가져가면
        // 플레이어가 챕터를 넘길 수 없게 막혀버린다 (기획서 §3.3).
        if (other.GetComponentInParent<PlayerKart>() == null) return;

        CollectionState.Collect(itemId);

        int caseNumber = ExhibitCatalogue.CaseNumberOf(itemId);
        LastMessage = caseNumber > 0
            ? $"{ExhibitCatalogue.NameOf(itemId)}  ·  전시실 {caseNumber}번에 등록"
            : ExhibitCatalogue.NameOf(itemId);
        LastMessageTime = Time.time;

        Destroy(gameObject);
    }

    static bool 경고했다;

    /// <summary>
    /// 줍는 쪽은 PlayerKart 표시가 붙은 카트만 인정한다. 씬이 낡아서 그 표시가 없으면
    /// 아이템은 멀쩡히 보이는데 지나가도 아무 일이 안 일어난다 — 제일 헷갈리는 종류의 고장이야.
    /// </summary>
    static void 경고_한번만()
    {
        if (경고했다) return;
        경고했다 = true;

        if (FindFirstObjectByType<PlayerKart>() != null) return;

        Debug.LogWarning("[수집품] 씬에 PlayerKart 표시가 붙은 카트가 없어. " +
                         "아이템이 보여도 주울 수가 없다 — 씬을 다시 구우면 고쳐진다.");
    }
}
