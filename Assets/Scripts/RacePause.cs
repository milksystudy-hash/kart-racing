using UnityEngine;

/// <summary>
/// <b>레이스 일시정지.</b> 2026-09-18 유저: *"ESC 누르면 속력이 0으로 줄어드는데 일시정지
/// 같은 느낌이니까 속력 그대로 유지하고 시간도 계속 흐르게 두지 말고."*
///
/// ★ 처음에 <c>Time.timeScale = 0</c> 으로 했다가 <b>카트가 트랙 밑으로 빠졌다.</b>
/// 이 카트는 바닥을 <see cref="KartController.ApplySuspension"/> 의 레이캐스트로 «밀어 올려»
/// 떠 있는 거라, 물리가 멈추면 받쳐주던 힘도 같이 멈춘다. 게다가 timeScale 은
/// <b>씬 전체</b>에 걸려서 곰 NPC·문·연출까지 다 얼어붙고, 푸는 자리를 하나라도 빠뜨리면
/// 게임이 멈춘 채로 남는다.
///
/// ★★ 두 번째로 <b>리지드바디를 키네마틱으로 재웠다가</b> 또 틀렸다(2026-09-18 유저:
/// *"발판 밟아 105 였는데 풀면 65 가 돼"*). 키네마틱 전환은 속도를 지우는 데서 끝나지 않는다 —
/// 되돌릴 때 접촉이 다시 계산되면서 <b>충돌 처리가 한 번 더 돌고, 거기서 부스트가 취소</b>된다.
/// 부스트가 사라지면 다음 프레임에 최고 속도로 깎여서 105 → 65 가 된다.
/// <b>속도를 적었다 돌려주는 방식은 «속도만» 돌려준다</b> — 부스트·드리프트·접촉 상태는 못 돌려준다.
///
/// 그래서 <b>아무것도 건드리지 않고 물리 시뮬레이션만 세운다</b>:
/// <c>Physics.simulationMode = Script</c> 는 «내가 부를 때만 물리를 돌린다» 는 뜻이고,
/// 안 부르면 한 걸음도 안 나간다. 리지드바디는 <b>속도도 부스트도 접촉도 그대로</b> 들고
/// 그 자리에 선다. 중력도 안 걸리니 트랙 밑으로 빠지지도 않는다.
///
/// 남는 건 <b>그림</b>이다. `Update`·`LateUpdate` 는 계속 도니까 카트가 기우뚱거리고 바퀴가
/// 돌았다(유저: *"다른 자동차가 꿈틀꿈틀"*). 그건 각자 <c>RacePause.On</c> 을 보고 멈춘다 —
/// 이 프로젝트의 <see cref="RaceCountdown"/> 과 같은 사고방식이야.
/// </summary>
public static class RacePause
{
    public static bool On { get; private set; }

    static SimulationMode saved = SimulationMode.FixedUpdate;

    public static void Set(bool paused)
    {
        if (paused == On) return;
        On = paused;

        if (paused)
        {
            saved = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;   // 내가 부를 때만 물리가 돈다 = 안 돈다
        }
        else
        {
            Physics.simulationMode = saved;
        }
    }

    /// <summary>씬을 옮길 때 안전망. 멈춘 채로 넘어가면 다음 씬이 얼어 있다.</summary>
    /// <summary>
    /// ★★ 2026-10-06 <b>조건 없이 되돌린다.</b> 전에는 <c>if (On)</c> 였는데,
    /// <see cref="On"/> 과 <see cref="Physics.simulationMode"/> 가 <b>어긋나 있으면</b>
    /// 영영 못 푼다 — 그리고 <c>simulationMode</c> 는 <b>씬을 넘어 살아남는 전역값</b>이라
    /// 한 번 어긋나면 <b>다음 레이스가 통째로 멈춘 채로 시작한다</b>
    /// (유저: *"레이싱 시작 누르면 화면이 먹통이 되고 일시 정지가 된다"*).
    ///
    /// 이 프로젝트는 <c>FixedUpdate</c> 말고 다른 모드를 쓸 일이 없으니
    /// <b>저장값을 믿지 말고 그냥 제자리로</b> 돌려놓는 게 맞다.
    /// </summary>
    public static void Clear()
    {
        On = false;
        saved = SimulationMode.FixedUpdate;
        if (Physics.simulationMode != SimulationMode.FixedUpdate)
            Physics.simulationMode = SimulationMode.FixedUpdate;
    }
}
