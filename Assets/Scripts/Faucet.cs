using UnityEngine;

/// <summary>
/// <b>수도꼭지.</b> 다가가서 E 를 누르면 물이 나오고, 한 번 더 누르면 잠긴다
/// (2026-09-21 유저: *"세면대에 물 나오게 해주라. 물은 끄고 켤 수 있게."*).
///
/// <see cref="HingedDoor"/> 와 같은 방식이다 — <b>제일 가까운 하나에만</b> 표시가 뜨고,
/// 겨루기는 한 프레임 늦게 공개한다(실행 순서에 안 기댄다).
///
/// 물줄기는 <b>씬에 미리 지어 두고 켜고 끈다.</b> 실행 중에 만들면 씬을 다시 구울 때마다
/// 자리가 달라지고, 지웠다 만들면 켜는 쪽이 <b>꺼진 것을 못 찾는</b> 함정에 걸린다
/// (`FindObjectsByType` 는 꺼진 오브젝트를 건너뛴다). 여기서는 <b>인스펙터 배열로 직접</b>
/// 들고 있어서 찾을 일이 없다.
/// </summary>
public class Faucet : MonoBehaviour
{
    [Tooltip("틀면 켜지는 물줄기 조각들")]
    public GameObject[] water;

    [Tooltip("이 거리 안에 들어와야 만질 수 있다")]
    public float range = 3.0f;

    [Tooltip("걸어다니는 몸. 비워두면 카메라")]
    public Transform visitor;

    public bool On { get; private set; }

    /// <summary>화면에 띄울 말. 상태에 맞아야 한다 — 늘 "물 틀기" 면 잠그는 법을 모른다.</summary>
    public string Action => On ? "물 잠그기" : "물 틀기";

    public static Faucet Nearest { get; private set; }
    public static float NearestScore { get; private set; } = float.MaxValue;

    static int frameStamp = -1;
    static float nearestDistance;
    static Faucet pending;
    static float pendingScore = float.MaxValue;

    void Start() { Apply(); }

    void Apply()
    {
        if (water == null) return;
        foreach (var g in water) if (g != null) g.SetActive(On);
    }

    public void Toggle()
    {
        On = !On;
        Apply();
    }

    void Update()
    {
        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            Nearest = pending;      // ← 지난 프레임에 다 끝난 결과를 이제 공개한다
            NearestScore = pending != null ? pendingScore : float.MaxValue;
            pending = null;
            pendingScore = float.MaxValue;
            nearestDistance = float.MaxValue;
        }

        Transform who = visitor != null ? visitor
                      : (Camera.main != null ? Camera.main.transform : null);
        if (who == null) return;
        if (visitor != null && !visitor.gameObject.activeInHierarchy) return;

        if (!Reach.Score(who, transform.position, range, out float d)) return;
        if (d >= nearestDistance) return;

        nearestDistance = d;
        pendingScore = d;
        pending = this;
    }
}
