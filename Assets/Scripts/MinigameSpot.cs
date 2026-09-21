using UnityEngine;

/// <summary>
/// <b>미니게임이 들어올 자리.</b> 건물 안에 입간판을 세우고 상태를 알려준다.
///
/// 2026-09-18 유저: *"미리 건물마다 미니게임을 하나씩 다 만들어 놓는 건 어때.
/// 못 만든 게임은 '준비 중' 패널 씌우면 되잖아.
/// (정치인들도 폐관 절차를 밟으려고 건물 안을 싹 밀어버려서 복구 준비 중이야) 식으로."*
///
/// 핑계는 훌륭하다 — 설정에도 맞고 다크코미디 톤도 산다. 다만 <b>숫자가 중요하다:</b>
///
/// | 준비 중 개수 | 어떻게 읽히나 |
/// |---|---|
/// | 1~2개 | "여기는 나중에 열리겠구나" → 기대 |
/// | 10개 이상 | <b>"미완성"</b> |
///
/// 13동 전부에 붙이면 핑계가 아니라 <b>못 만든 목록</b>으로 보인다.
/// 그래서 <b>세 동에만</b> 단다 — 곰밥마당(만들 것) · 곰짝박수마당 · 철곰관(준비 중).
/// 나머지 열 동은 지금처럼 그냥 둘러보는 방이야. 시간이 남으면 그때 늘리면 된다.
///
/// <b>상태는 셋이다:</b>
/// <list type="bullet">
/// <item>잠김 — 여덟 판을 아직 다 안 깼다. "철거 심사가 끝나야 문을 연다"</item>
/// <item>준비 중 — 다 깼지만 이 게임은 아직 안 만들었다. 설정으로 덮는다</item>
/// <item>열림 — 들어가서 할 수 있다</item>
/// </list>
/// </summary>
public class MinigameSpot : MonoBehaviour
{
    [Tooltip("여기서 할 것. 화면에 그대로 뜬다")]
    public string title = "";

    [Tooltip("한 줄 설명")]
    public string blurb = "";

    [Tooltip("켜면 실제로 할 수 있다. 끄면 '준비 중'")]
    public bool ready;

    [Tooltip("이 거리 안에 들어와야 안내가 뜬다")]
    public float range = 5f;

    [Tooltip("걸어다니는 몸. 비워두면 카메라")]
    public Transform visitor;

    /// <summary>지금 제일 가까운 자리. <see cref="CampusHUD"/> 가 읽는다.</summary>
    public static MinigameSpot Nearest { get; private set; }

    // ★★ <b>«제일 가까운 것» 을 스크립트 실행 순서에 기대면 안 된다</b> (2026-09-21).
    //
    // 여태 각 오브젝트가 자기 Update 에서 «내가 제일 가까운가» 를 겨루고, HUD 가 그 값을
    // 곧바로 읽었다. 그런데 <b>유니티는 같은 우선순위 스크립트의 Update 순서를 정해 주지 않는다.</b>
    // HUD 가 <b>중간에</b> 끼면 아직 안 겨룬 것들이 빠진 <b>반쪽 결과</b>를 읽는다.
    //
    // 화장실 칸 문이 그랬다 — 문은 멀쩡히 젖혀지는데(측정: 회전 −78°, 문짝 1.259m) E 가
    // 그 문을 못 집었다. 칸 문은 건물 문보다 <b>나중에 만들어져서</b> HUD 뒤에 섰고,
    // 그래서 <b>안내는 «문 열기» 인데 눌러도 아무 일이 없었다.</b>
    //
    // 고치는 법: <b>한 프레임 늦게 공개한다.</b> 겨루기는 `pending` 에 쌓고, 프레임이 바뀌는
    // 순간 <b>다 끝난 지난 프레임 결과</b>를 `Nearest` 로 내보낸다. 한 프레임 차이는 눈에
    // 안 보이고, 순서에 대한 의존이 <b>완전히</b> 사라진다.
    static int frameStamp = -1;
    static float nearestDistance;
    static MinigameSpot pending;

    /// <summary>여덟 판을 다 깼나. 안 깼으면 어느 자리도 안 열린다.</summary>
    public static bool Unlocked =>
        ExhibitCatalogue.Count > 0 && CollectionState.Count >= ExhibitCatalogue.Count;

    public enum State { 잠김, 준비중, 열림 }

    /// <summary>실제로 만들어 둔 게임의 제목. 늘어나면 여기에 한 줄 더.</summary>
    public const string CanteenTitle = "오늘의 급식";

    /// <summary>
    /// ★ <b>코드가 판단한다, 씬이 아니라.</b> <see cref="ready"/> 는 씬에 구워진 값이라
    /// 옛 씬에서는 «준비 중」인 채로 남는다 — 이 프로젝트에서 «새 컴포넌트/새 값으로 고치면
    /// 씬을 다시 구워야만 고쳐진다」 를 네 번 겪었다. 제목만 보고 스스로 알게 만든다.
    /// </summary>
    static bool Made(string title) => title == CanteenTitle;

    public State Now => !Unlocked ? State.잠김
                      : (ready || Made(title)) ? State.열림 : State.준비중;

    /// <summary>
    /// 화면에 띄울 한 줄. <b>세 상태가 전부 다른 말을 해야 한다</b> —
    /// "안 된다" 만 세 번 뜨면 플레이어는 왜 안 되는지 모른다.
    /// </summary>
    public string Line => Now switch
    {
        State.잠김   => $"{title} — 철거 심사 중에는 문을 안 연다",
        // 유저가 준 핑계 그대로. 없는 걸 없다고 적는 것보다 <b>이유가 있는 게</b> 낫고,
        // 이 게임은 원래 그런 농담을 하는 게임이다.
        State.준비중 => $"{title} — 폐관 절차 때 안을 싹 밀어버렸다. 복구 중",
        _             => title,
    };

    /// <summary>지금 E 를 눌러서 뭔가 되나. HUD 가 키를 보여줄지 결정한다.</summary>
    public bool Actionable => Now == State.열림;

    void Update()
    {
        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            Nearest = pending;      // ← 지난 프레임에 <b>다 끝난</b> 결과를 이제 공개한다
            pending = null;
            nearestDistance = float.MaxValue;
        }

        Transform who = visitor != null ? visitor
                      : (Camera.main != null ? Camera.main.transform : null);
        if (who == null) return;
        if (visitor != null && !visitor.gameObject.activeInHierarchy) return;

        float d = Vector3.Distance(who.position, transform.position);
        if (d > range || d >= nearestDistance) return;

        nearestDistance = d;
        pending = this;
    }

    /// <summary>E 를 눌렀을 때.</summary>
    public void Enter()
    {
        if (Now != State.열림)
        {
            Toast.Show(Line);
            return;
        }

        // 씬을 갈아타지 않는다 — 곰밥마당은 이미 지어져 있고, 방을 두 벌 만들면 어긋난다
        // (이야기 장면을 로비 안에서 돌리는 것과 같은 이유).
        if (title == CanteenTitle) { Canteen.Begin(); return; }

        Toast.Show($"{title} — 곧 들어갑니다");
    }
}
