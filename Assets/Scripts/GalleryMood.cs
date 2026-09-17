using UnityEngine;

/// <summary>
/// 전시실이 <b>폐관 중인 방</b>에서 <b>다시 연 방</b>으로 바뀐다.
///
/// 2026-09-17 유저: *"수집 8개 모으기 전이랑 후랑 구분해서 연출을 넣어 줘."*
/// 전에는 <see cref="GalleryLights"/> 의 불빛뿐이었다. 밝아지는 건 크게 읽히지만
/// <b>왜</b> 어두웠는지는 안 알려준다 — 그냥 조명 설정처럼 보인다.
///
/// 그래서 방에 <b>이유</b>를 놓는다. 다 모으기 전에는 철거 통지서와 출입 금지 띠가 붙어
/// 있고, 다 모으면 그게 내려가고 재개관 현수막과 붉은 카펫이 깔린다.
/// 진열장 먼지 덮개는 <see cref="GalleryCase"/> 가 <b>한 장씩</b> 걷는다 —
/// 캠퍼스의 폐과 딱지와 같은 생각이야. 마지막에 한 번 확 바뀌는 것보다,
/// 한 개 모을 때마다 방이 조금씩 변하는 쪽이 여덟 판을 도는 이유가 된다.
///
/// <b>기하를 새로 만들지도 부수지도 않는다.</b> 빌더가 양쪽을 다 지어 놓고 여기서는
/// 켜고 끄기만 한다 — 그래서 공짜고, 씬을 다시 구울 필요도 없다.
/// </summary>
public class GalleryMood : MonoBehaviour
{
    [Tooltip("다 모으기 전에만 보이는 것 — 철거 통지서, 출입 금지 띠, 쌓아둔 상자")]
    public GameObject[] beforeThings;

    [Tooltip("다 모은 뒤에만 보이는 것 — 재개관 현수막, 붉은 카펫, 화분")]
    public GameObject[] afterThings;

    [Tooltip("불이 켜지는 것과 박자를 맞추려고 본다. 비워두면 씬에서 찾는다")]
    public GalleryLights lights;

    [Tooltip("불이 이만큼 들어왔을 때 바뀐다. 0 이면 곧바로")]
    [Range(0f, 1f)] public float switchAt = 0.55f;

    bool applied, lastDone;

    void Start()
    {
        if (lights == null) lights = FindFirstObjectByType<GalleryLights>();
        Apply(false);
        applied = true;
        lastDone = false;
    }

    void Update()
    {
        // 불이 절반쯤 들어왔을 때 같이 바뀐다. 불보다 먼저 바뀌면 어두운 방에 현수막만
        // 둥둥 떠 보이고, 다 밝아진 다음에 바뀌면 두 번 연출한 것처럼 늘어진다.
        bool done = lights != null ? lights.Lit >= switchAt : GalleryLights.AllCollected;
        if (applied && done == lastDone) return;

        Apply(done);
        applied = true;
        lastDone = done;
    }

    void Apply(bool done)
    {
        foreach (var go in beforeThings) if (go != null) go.SetActive(!done);
        foreach (var go in afterThings) if (go != null) go.SetActive(done);
    }
}
