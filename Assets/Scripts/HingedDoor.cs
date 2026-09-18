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
/// <b>열고 닫는다.</b> 2026-09-18 유저: *"모든 문이 존재하는 건물에 문 열기/문 닫기
/// 시스템이 있으면 좋겠어. 연출적으로 좋아 보이게."*
///
/// 전에는 한 번 열면 다시 안 닫았다 — "나갈 때 또 눌러야 하면 짜증" 이라고 판단했는데,
/// 실제로 써 보니 <b>열린 문이 캠퍼스에 열셋 널려 있는 게</b> 더 어수선했다.
/// 게다가 문을 닫을 수 있으면 그 자체가 <b>연출</b>이 된다 — 들어와서 문을 닫는 동작은
/// 공짜로 생기는 이야기야.
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

    [Tooltip("문이 왜 안 움직이는지 콘솔에 찍는다. 확인 끝나면 꺼")]
    public bool doorDebug = true;

    public bool Open { get; private set; }

    /// <summary>
    /// <b>못질한 판자가 박혀 있나.</b> <see cref="CampusBoarding"/> 가 문 루트에 `Boarding` 을
    /// 붙이는데, 그게 켜져 있으면 문짝이 미끄러져도 <b>판자가 그대로 덮고 있어서</b>
    /// 아무 일도 안 일어난 것처럼 보였다(2026-09-18 유저가 두 번 신고).
    ///
    /// 고치는 방향은 둘이었다 — 판자를 문짝에 붙여 같이 움직이게 하거나,
    /// <b>판자가 박힌 문은 아예 못 열게</b> 하거나. 후자가 맞다:
    /// 못질한 판자를 열고 들어가는 문은 판자가 아니라 커튼이야.
    /// </summary>
    public bool Barred
    {
        get
        {
            var boarding = transform.Find("Boarding");
            return boarding != null && boarding.gameObject.activeSelf;
        }
    }

    [Tooltip("열려 있을 때도 표시를 띄울지. 끄면 한 번 열면 끝")]
    public bool canClose = true;

    public static HingedDoor Nearest { get; private set; }

    static int frameStamp = -1;
    static float nearestDistance;

    float movedAt = -99f;
    Vector3[] shut, swung;

    void Start()
    {
        // <b>씬을 다시 안 구워도 고쳐지게.</b> 옛날에 구운 씬에는 문짝이 안 꽂혀 있거나
        // 이름이 달라서, 문을 열어도 아무 일이 없고 표시만 사라졌다(2026-09-18 유저:
        // *"문열기만 나오고 끝이네, E 는 다시는 못 누르고"*).
        // <b>통을 먼저 찾는다.</b> `LeafRoot_s` 는 문짝 여섯 조각을 다 담고 있어서
        // 이걸 옮기면 문이 통째로 움직인다. 없으면(옛 씬) 판 하나라도 옮긴다.
        var rootL = transform.Find("LeafRoot_-1");
        var rootR = transform.Find("LeafRoot_1");

        if (rootL != null && rootR != null) leaves = new[] { rootL, rootR };
        else if (leaves == null || leaves.Length < 2 || leaves[0] == null || leaves[1] == null)
            leaves = new[] { transform.Find("Leaf_-1"), transform.Find("Leaf_1") };

        if (leaves == null || leaves[0] == null || leaves[1] == null)
        {
            Debug.LogWarning($"[문] '{label}' 의 문짝을 못 찾았어. 캠퍼스 씬을 다시 구우면 붙는다.", this);
            return;
        }

        if (doorDebug)
            Debug.Log($"[문] '{label}' 문짝 [{leaves[0].name}] / [{leaves[1].name}] " +
                      $"· 조각 {leaves[0].childCount} + {leaves[1].childCount}", this);

        shut = new Vector3[leaves.Length];
        swung = new Vector3[leaves.Length];

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            shut[i] = leaves[i].localPosition;

            // 두 짝이 서로 반대쪽으로 미끄러진다. 같은 쪽으로 가면 한 짝이 다른 짝을 뚫는다.
            // ★ <b>통의 스케일은 1 이다.</b> 그걸 문짝 폭으로 쓰면 실제 폭 1.96m 짜리 문이
            // 0.96m 만 미끄러져서 <b>구멍을 절반 막은 채로 멈춘다</b> — 유저가 네 번째로
            // "문이 안 열린다" 고 한 게 이거야(2026-09-18). 로그의 `폭 1.00` 이 단서였다.
            //
            // 전 조건은 `width < 0.2f` 였는데 1.0 은 그보다 크니 <b>자식을 안 봤다.</b>
            // <b>자식이 있으면 무조건 자식에서 읽는다</b> — 통은 늘 스케일 1 이라 못 믿는다.
            float width = 0f;
            for (int c = 0; c < leaves[i].childCount; c++)
            {
                var kid = leaves[i].GetChild(c);
                if (kid.name.StartsWith("Leaf_")) { width = Mathf.Abs(kid.localScale.x); break; }
                width = Mathf.Max(width, Mathf.Abs(kid.localScale.x));
            }
            if (width < 0.2f) width = Mathf.Abs(leaves[i].localScale.x);

            float dir = shut[i].x >= 0f ? 1f : -1f;
            float travel = width * Mathf.Max(0.5f, slideRatio);

            // 옛 씬에 <c>slideRatio</c> 0 이 저장돼 있으면 문이 제자리에서 안 움직인다.
            // 폭을 못 읽는 경우까지 대비해 바닥값을 준다 — 안 움직이는 문이 제일 나쁘다.
            if (travel < 0.2f) travel = 1.6f;
            swung[i] = shut[i] + new Vector3(dir * travel, 0f, 0f);

            if (doorDebug)
                Debug.Log($"[문] '{label}' {leaves[i].name} 닫힘 x {shut[i].x:F2} → 열림 x {swung[i].x:F2} " +
                          $"(폭 {width:F2} × 비율 {slideRatio:F2} = {travel:F2}m)", this);
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
        // 열린 문도 표시를 띄운다 — <b>닫을 수 있어야</b> 여닫이다.
        if (Open && !canClose) return;

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
        if (Barred)
        {
            if (doorDebug) Debug.Log($"[문] '{label}' 판자가 박혀 있어서 안 열린다", this);
            Toast.Show($"{label} — 판자가 박혀 있다");
            return;
        }

        if (shut == null)
        {
            // Start 가 문짝을 못 찾고 돌아간 경우. 여기서 한 번 더 시도한다 —
            // 씬을 다시 굽지 않아도 고쳐지게.
            if (doorDebug) Debug.LogWarning($"[문] '{label}' 문짝이 없어서 다시 찾는다", this);
            SendMessage("Start");
            if (shut == null) return;
        }

        // 여는 중이거나 닫는 중이면 무시한다. 반쯤 열린 문에서 또 누르면
        // 문짝이 <b>제자리에서 튀어</b> 고장처럼 보인다.
        if (Time.time - movedAt < openSeconds) return;

        Open = canClose ? !Open : true;
        movedAt = Time.time;

        if (doorDebug) Debug.Log($"[문] '{label}' {(Open ? "연다" : "닫는다")}", this);
    }

    /// <summary>화면에 띄울 말. 상태에 따라 달라야 한다 — 늘 "문 열기" 면 닫는 법을 모른다.</summary>
    public string Action => Barred ? "판자가 박혀 있다" : (Open ? "문 닫기" : "문 열기");

    /// <summary>눌러서 뭔가 되나. HUD 가 키를 보여줄지 결정한다.</summary>
    public bool Actionable => !Barred;

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

        // <b>닫힐 때도 같은 곡선을 탄다.</b> 열 때만 부드럽고 닫을 때 툭 끊기면
        // 같은 문이 두 물건처럼 보인다.
        float p = Mathf.Clamp01((Time.time - movedAt) / Mathf.Max(0.05f, openSeconds));
        float t = Open ? p : 1f - p;

        // <b>"한 번도 안 건드린 문" 가드는 필요 없다.</b> 처음엔 Open 이 false 라
        // `t = 1 − p` 이고 p 가 1 이라 t 가 이미 0 이야. 가드를 두면 `Time.time` 이
        // 작을 때 movedAt 이 −90 아래로 내려가서 <b>영영 t = 0</b> 이 된다 — 문이 안 움직인다.

        t = t * t * (3f - 2f * t);   // 부드럽게 — 일정한 속도로 움직이면 기계 같다

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null) continue;
            leaves[i].localPosition = Vector3.Lerp(shut[i], swung[i], t);
        }
    }
}
