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

    [Tooltip("몇 도까지 열리는지")]
    public float openAngle = 96f;

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
    Quaternion[] shut, swung;

    void Start()
    {
        if (leaves == null) return;

        shut = new Quaternion[leaves.Length];
        swung = new Quaternion[leaves.Length];

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            shut[i] = leaves[i].localRotation;

            // 두 짝이 서로 반대로 젖혀진다. 같은 쪽으로 열리면 한 짝이 다른 짝을 뚫는다.
            float dir = leaves[i].localPosition.x >= 0f ? 1f : -1f;
            swung[i] = shut[i] * Quaternion.Euler(0f, dir * openAngle, 0f);
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
    /// 문짝을 돌린다. <b>경첩이 문 가장자리</b>라 그냥 회전시키면 가운데를 축으로 돌아
    /// 벽을 뚫는다 — 돌리면서 옆으로도 같이 밀어준다.
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
            leaves[i].localRotation = Quaternion.Slerp(shut[i], swung[i], t);
        }
    }
}
