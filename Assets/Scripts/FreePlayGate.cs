using UnityEngine;

/// <summary>
/// ★★ <b>자유마당은 결승을 이긴 뒤에만 보인다.</b> (2026-10-13)
///
/// 수연: *"게임 클리어 전인데 판자 아래에 함께 찰칵 찍는 오브젝트랑 겹쳐."*
///
/// 원인은 자리가 아니라 <b>켜고 끄는 시점</b>이었다. 빌더가 <c>SetActive(GrandFinal.FreeRun)</c>
/// 를 <b>씬을 구울 때</b> 불렀고, 그 값이 씬 파일에 그대로 박힌다 —
/// 다 깬 상태로 구우면 <b>안 깬 사람에게도 켜진 채로</b> 저장된다.
///
/// 이 프로젝트에서 「씬을 구울 때 런타임 선택에 의존하는 값을 읽지 마라」를 어긴 게
/// 다섯 번째다(카트 중복 · AI · 장애물 · 광고판 · 이번).
/// <b>물건이 스스로 판단하게</b> 만든다 — <see cref="GrandFinal"/> 는 수집 기록과
/// PlayerPrefs 만 보니 컴포넌트 순서에도 안 흔들린다.
///
/// ★ 지웠다 만들지 않고 <b>켜고 끈다.</b> 지웠다 만들면 F8 로 오갈 때마다 자리가 달라진다.
/// ★ 자식을 끄는 게 아니라 <b>제 오브젝트를 끈다</b> — 꺼진 것을 다시 찾을 때
///   <c>FindObjectsInactive.Include</c> 를 안 써도 되게, 이 컴포넌트만 살아 있으면 된다.
/// </summary>
[DefaultExecutionOrder(-50)]
public class FreePlayGate : MonoBehaviour
{
    Transform held;
    bool shown = true;

    void Awake()
    {
        // 자식을 통째로 옮겨 들고 있는다 — 제 오브젝트를 끄면 Update 가 안 돌기 때문.
        held = transform;
        Apply(GrandFinal.FreeRun, instant: true);
    }

    void Update()
    {
        bool want = GrandFinal.FreeRun;
        if (want != shown) Apply(want, instant: false);
    }

    void Apply(bool on, bool instant)
    {
        shown = on;
        for (int i = 0; i < held.childCount; i++)
        {
            var c = held.GetChild(i);
            if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on);
        }

        if (!instant && on)
            Debug.Log($"[자유마당] 결승을 이겨서 소품 {held.childCount}개가 나왔다");
    }
}
