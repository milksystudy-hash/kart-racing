using UnityEngine;

/// <summary>
/// <b>열리는 문짝.</b> 걸어가서 E 를 누르면 두 짝이 밖으로 젖혀진다.
///
/// 2026-09-17 유저: *"모든 건물에 E 눌러서 문 열 수 있게 해주고, 안을 모델링 해줘."*
///
/// <see cref="SceneDoor"/> 와 다르다 — 그쪽은 <b>씬을 바꾸고</b>, 이쪽은 <b>같은 씬에서
/// 그냥 걸어 들어간다.</b> 건물 열셋마다 씬을 만들면 로딩만 열세 번이고, 방 하나 보자고
/// 씬을 갈아타는 건 비싸다. 문틀이 실제로 뚫려 있으니 열고 들어가면 된다.
///
/// 한 번 열면 <b>다시 안 닫는다.</b> 나갈 때 또 눌러야 하면 짜증만 난다.
/// </summary>
public class HingedDoor : MonoBehaviour
{
    [Tooltip("젖혀질 문짝 둘")]
    public Transform[] leaves;

    [Tooltip("문짝이 옆으로 미끄러지는 거리. 문짝 폭의 몇 배인지")]
    public float slideRatio = 0.96f;

    [Tooltip("여는 데 걸리는 시간(초)")]
    public float openSeconds = 0.5f;

    [Tooltip("이 거리 안에 들어와야 열 수 있다")]
    public float range = 3.4f;

    [Tooltip("이 문이 달린 곳 이름. 화면에 뜬다")]
    public string label = "";

    [Tooltip("걸어다니는 몸. 비워두면 카메라")]
    public Transform visitor;

    public bool Open { get; private set; }

    public static HingedDoor Nearest { get; private set; }

    static int frameStamp = -1;
    static float nearestDistance;

    float openedAt = -99f;
    Vector3[] shut, swung;

    void Start()
    {
        // <b>씬을 다시 안 구워도 고쳐지게.</b> 옛날에 구운 씬에는 문짝이 안 꽂혀 있거나
        // 이름이 달라서, 문을 열어도 아무 일이 없고 표시만 사라졌다(2026-09-18 유저:
        // *"문열기만 나오고 끝이네, E 는 다시는 못 누르고"*).
        if (leaves == null || leaves.Length < 2 || leaves[0] == null || leaves[1] == null)
            leaves = new[] { transform.Find("Leaf_-1"), transform.Find("Leaf_1") };

        if (leaves == null || leaves[0] == null || leaves[1] == null)
        {
            Debug.LogWarning($"[문] '{label}' 의 문짝을 못 찾았어. 캠퍼스 씬을 다시 구우면 붙는다.", this);
            return;
        }

        shut = new Vector3[leaves.Length];
        swung = new Vector3[leaves.Length];

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            shut[i] = leaves[i].localPosition;

            // 두 짝이 서로 반대쪽으로 미끄러진다. 같은 쪽으로 가면 한 짝이 다른 짝을 뚫는다.
            float dir = shut[i].x >= 0f ? 1f : -1f;
            float travel = Mathf.Abs(leaves[i].localScale.x) * Mathf.Max(0.5f, slideRatio);

            // 옛 씬에 <c>slideRatio</c> 0 이 저장돼 있으면 문이 제자리에서 안 움직인다.
            // 폭을 못 읽는 경우까지 대비해 바닥값을 준다 — 안 움직이는 문이 제일 나쁘다.
            if (travel < 0.2f) travel = 1.6f;
            swung[i] = shut[i] + new Vector3(dir * travel, 0f, 0f);
        }
    }

    void Update()
    {
        Swing();

        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            nearestDistance = float.MaxValue;
            Nearest = null;
        }
        if (Open) return;   // 이미 열린 문은 표시를 안 띄운다

        Transform who = visitor != null ? visitor
                      : (Camera.main != null ? Camera.main.transform : null);
        if (who == null) return;
        if (visitor != null && !visitor.gameObject.activeInHierarchy) return;

        float d = Vector3.Distance(who.position, transform.position);
        if (d > range || d >= nearestDistance) return;

        nearestDistance = d;
        Nearest = this;
    }

    public void Toggle()
    {
        Open = true;                 // 한 번 열면 계속 열려 있다
        openedAt = Time.time;
    }

    /// <summary>
    /// 문짝을 <b>옆으로 민다.</b> 한옥 장지문은 여닫이가 아니라 미닫이야
    /// (2026-09-17 유저: "좌우가 갈라져 에스컬레이터처럼 열리게").
    ///
    /// 젖히는 것보다 미는 쪽이 코드도 짧고 문제도 적다 — 경첩 축을 맞출 일이 없고,
    /// 열린 문짝이 벽을 뚫지도 않는다(문틀 뒤로 들어갈 뿐이야).
    /// </summary>
    void Swing()
    {
        if (leaves == null || shut == null) return;

        float t = Mathf.Clamp01((Time.time - openedAt) / Mathf.Max(0.05f, openSeconds));
        if (!Open) t = 0f;
        t = t * t * (3f - 2f * t);   // 부드럽게 — 일정한 속도로 열리면 기계 같다

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            leaves[i].localPosition = Vector3.Lerp(shut[i], swung[i], t);
        }
    }
}
