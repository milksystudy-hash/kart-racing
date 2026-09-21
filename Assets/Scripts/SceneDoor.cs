using UnityEngine;

/// <summary>
/// <b>들어갈 수 있는 문.</b> 걸어가서 E 를 누르면 그 씬으로 간다.
///
/// 2026-09-17 에 생겼다. 유저: *"레이싱하다 내리면 좀 이상하잖아. 똑같은 씬을 만들어서
/// 그 씬을 걷기 모드로 바꾸는 게 낫지 않을까."* 맞는 판단이야 —
/// <b>한 씬이 두 가지 일을 하면 둘 다 어정쩡해진다.</b> 트랙은 달리는 곳, 캠퍼스는 걷는 곳.
///
/// 문패는 전부터 달려 있었는데(<see cref="HanokDoor"/>) 지금까지는 <b>그림</b>이었다.
/// 이제 로비 서쪽 "캠퍼스 · 준비 중" 이 진짜로 열린다.
///
/// 고르는 방식은 <see cref="BearNpc"/> 와 같다 — 제일 가까운 하나에만 표시가 뜬다.
/// 문 둘이 나란히 있을 때 어느 쪽으로 들어가는지 헷갈리면 안 되니까.
/// </summary>
public class SceneDoor : MonoBehaviour
{
    [Tooltip("빌드 설정의 씬 번호. F1 로비 0 · F2 트랙 1 · F3 전시실 2 · F4 캠퍼스 3")]
    public int sceneIndex;

    [Tooltip("화면에 뜨는 말. \"들어가기\" 앞에 붙는다")]
    public string label = "";

    [Tooltip("이 거리 안에 들어와야 표시가 뜬다")]
    public float range = 3.2f;

    [Tooltip("조종하는 몸. 비워두면 카메라를 기준으로 본다")]
    public Transform visitor;

    public static SceneDoor Nearest { get; private set; }

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
    static SceneDoor pending;

    void Update()
    {
        // 프레임마다 한 번만 초기화한다. 문이 여럿이어도 제일 가까운 하나만 남는다.
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

        // 몸이 꺼져 있으면(둘러보기 모드) 문도 안 잡힌다 — 걸어가서 여는 문이니까.
        if (visitor != null && !visitor.gameObject.activeInHierarchy) return;

        float d = Vector3.Distance(who.position, transform.position);
        if (d > range || d >= nearestDistance) return;

        nearestDistance = d;
        pending = this;
    }

    public void Enter()
    {
        if (sceneIndex < 0) return;
        SceneNavigator.LoadByIndex(sceneIndex);
    }
}
