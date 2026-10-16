using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비를 잠깐 <b>이야기 무대</b>로 바꿨다가 되돌린다.
///
/// 이야기 장면 전용 씬을 따로 만들면 중앙홀이 두 벌이 되고, 한쪽만 꾸미는 순간
/// 같은 방인데 생김새가 달라진다. 그래서 방은 <b>로비 하나</b>만 두고,
/// 이야기가 흐를 때 카메라만 바꿔 앉는다.
///
/// 바뀌는 건 이것뿐이다:
///   · 로비 카메라 → StoryCamera (고정 시점)
///   · 로비 조작(둘러보기 · 캐릭터 고르기 · 로비 HUD) 잠깐 끔
///   · 대화창 켬
/// 끝나면 전부 되돌린다. <b>방 안의 물건은 하나도 건드리지 않는다</b> —
/// 네가 로비에 FBX 를 채워 넣으면 이야기 장면 배경도 같이 채워진다.
///
/// 이야기 장면 각도를 바꾸고 싶으면 <b>StoryRig > StoryCamera</b> 를 씬에서 옮기면 된다.
/// 코드는 그 카메라가 어디를 보든 상관하지 않는다.
/// </summary>
public class StoryStage : MonoBehaviour
{
    [Header("대화")]
    public DialogueRunner runner;
    public DialogueHUD hud;

    [Header("카메라 — 이야기 동안 이걸로 갈아탄다")]
    public Camera storyCamera;
    public Camera lobbyCamera;

    [Tooltip("이야기가 흐르는 동안 꺼둘 것들 — 로비 둘러보기, 캐릭터 고르기, 로비 HUD")]
    public Behaviour[] pauseWhileTalking;

    [Header("언제 트나")]
    [Tooltip("이 장의 이야기를 아직 안 봤으면 로비에 들어올 때 저절로 튼다")]
    public bool playUnseenOnEnter = true;
    // ★ 2026-10-13 — <b>여기 있던 <c>debugKeys</c> 를 걷어냈다.</b> 아무도 안 읽는
    //   죽은 칸이었는데, 인스펙터에 «debugKeys ☑» 로 떠 있으니 <b>«아직 디버그 키가
    //   켜져 있다»</b> 로 읽혔다. 제출 전에 끌 것 목록에도 그래서 올라가 있었다.
    //
    //   T 는 이제 디버그 키가 아니다 — 로비 발바닥 버튼이 「이야기 다시 보기   T」라고
    //   <b>직접 광고한다</b>. 빌드에 남아 있는 게 맞고, 그래서 <see cref="Dev"/> 로도 안 막는다.
    //   진짜 디버그 키(1~5 · F9 · F10)는 <see cref="DialogueHUD"/> 쪽에 있고 거기서 막힌다.


    public bool InStory { get; private set; }

    /// <summary>
    /// 지금 이야기가 도는 중인가. <b>static 인 이유가 있다</b> —
    /// 2026-10-02 유저: *"T 를 눌렀는데 레이스 시작도 안 눌렀는데 트랙으로 이동돼 버린다."*
    /// <see cref="StartGate"/> 가 ENTER·클릭으로 출발하는데, 그게 <b>대사를 넘기는 키와 같다.</b>
    /// 그래서 대사를 넘길 때마다 출발문도 같이 눌리고 있었다.
    ///
    /// 빌더의 «잠깐 꺼둘 것» 목록에 StartGate 를 더하는 방법도 있지만, 그러면
    /// <b>씬을 다시 구워야만 고쳐진다</b> — 이 프로젝트에서 다섯 번 겪었다.
    /// 물건이 <b>스스로 판단하게</b> 둔다.
    /// </summary>
    public static bool Talking { get; private set; }

    /// <summary>지금 도는 이야기 장면 id. 음악이 이걸 보고 곡을 고른다.</summary>
    public static string TalkingScene { get; private set; } = "";

    /// <summary>
    /// 지금 이야기의 <b>장소</b>(<see cref="DialogueLine.At"/>). 음악이 장면보다 먼저 이걸 본다 —
    /// 2026-10-02 유저: *"프롤로그 음악 쓰다가 시의원이 나타나면 그 음악으로 바꿔줘."*
    /// 한 장면 안에서 분위기가 바뀌는 자리가 있고, 그 신호는 <b>장소</b>가 들고 있다.
    /// </summary>
    public static string TalkingPlace =>
        Live != null && Live.runner != null && Live.runner.IsPlaying ? Live.runner.Place : "";

    public static StoryStage Live;

    /// <summary>
    /// 첫 화면(<see cref="TitleScreen"/>) 때문에 미뤄 둔 장면. 타이틀이 로비 <b>안에서</b>
    /// 도니까, 그게 떠 있는 동안 Enter 를 부르면 대사가 <b>타이틀에 가려진 채로 흘러간다.</b>
    /// </summary>
    string pending;

    /// <summary>
    /// ★★ <b>한 방문에 장면 둘까지 연달아 튼다.</b> (2026-10-12)
    ///
    /// 전에는 <b>로비에 한 번 들어오면 장면이 하나</b>였다. 그런데 판마다 후기 장면
    /// (<c>after1</c> 같은 것)을 쓰기 시작하면 <b>장 장면(ch1·ch2·ch3)이 밀린다</b> —
    /// 장 장면이 뜨는 자리가 하필 <b>1·3·5판을 깬 직후</b>라 <c>after</c> 와 정면으로 겹친다.
    /// 로비 → 레이스 → 로비 로만 다니면 ch1 이 영영 차례가 안 온다.
    ///
    /// 그래서 장면이 끝나면 <b>그 자리에서 다음 것을 한 번 더 묻는다.</b>
    /// «후기 → 장 이야기» 가 이어 붙고, 둘째가 없으면 그냥 로비로 나간다.
    ///
    /// <b>한 번만 잇는다</b>(최대 두 장면). 셋을 이어 붙이면 레이스 끝나고
    /// 읽을 게 너무 길어져서 «다음 판» 으로 못 넘어간다.
    /// </summary>
    const int MaxChain = 1;
    int chained;

    /// <summary>바깥에서 장면을 시작한다 — 이어 붙인 횟수를 0 으로 되돌리고 들어간다.</summary>
    void EnterFresh(string sceneId)
    {
        chained = 0;
        Enter(sceneId);
    }

    void Start()
    {
        Live = this;
        Leave();   // 시작은 무조건 로비 상태로

        if (!playUnseenOnEnter) return;

        // ★ 장 번호만 보면 결말이 어긋난다(2026-09-21) — 결승 장면이 영영 안 나오고,
        // 에필로그가 7판 뒤에 떴다. CurrentScene 이 «결승을 깼는가» 까지 보고 고른다.
        string sceneId = StoryScript.CurrentScene();
        if (string.IsNullOrEmpty(sceneId)) return;

        // ★★ 2026-10-02 유저: *"게임 키자마자 대화씬이 안 뜨더라. 게임 시작 누르면
        //   바로 로비로 가고, T 를 눌러야 읽을 수 있다."* 원인은 <b>이미 본 것</b>이라
        //   <see cref="StoryProgress.HasSeen"/> 가 막고 있던 것.
        //
        //   «게임 시작» 은 플레이어가 <b>일부러 누른 것</b>이라 다르게 봐야 한다 —
        //   지금 장 이야기를 <b>본 것이어도</b> 튼다. 레이스를 마치고 로비로 돌아오는 길은
        //   그대로 «안 본 것만» 이다(매번 같은 장면이 또 뜨면 안내가 아니라 장애물이 된다).
        //
        //   첫 화면이 떠 있으면 적어 뒀다가 <b>타이틀이 닫히는 프레임</b>에 튼다 —
        //   그래야 프롤로그를 한 줄도 안 놓친다.
        if (TitleScreen.Up) { pending = sceneId; return; }

        if (StoryProgress.HasSeen(sceneId)) return;
        EnterFresh(sceneId);
    }

    void Update()
    {
        if (Dev.Typing) return;   // 암호 치는 중 — 키를 건드리지 않는다
        // 첫 화면 위로 디버그 키(T · 1~5)가 새어나가면 안 된다.
        if (TitleScreen.Up) return;

        if (pending != null)
        {
            string queued = pending;
            pending = null;
            // ★ 2026-10-16 <b>본 것은 안 튼다.</b> 전에는 «게임 시작은 일부러 누른 것이니
            //   본 것이어도 튼다» 였는데, 그러면 <b>이어하기를 누를 때마다 같은 장면</b>이 뜬다.
            //   처음 켠 사람에게는 어차피 아무것도 안 봤으니 프롤로그가 그대로 나온다.
            if (!StoryProgress.HasSeen(queued)) EnterFresh(queued);
            return;
        }

        if (InStory) return;

        var k = Keyboard.current;
        if (k == null) return;

        // T — 이번 장 이야기 다시 보기.
        //
        // ★ 2026-09-28 전에는 <b>틀 게 없으면 조용히 아무 일도 안 했다.</b> 유저 제보:
        // *"T 를 눌러도 이야기 장면이 재생이 안 돼."* 그때 상태가 «장 4 · 수집품 0» 이라
        // `CurrentScene()` 이 빈 문자열이었는데, <b>화면에도 콘솔에도 아무 말이 없으니</b>
        // 키가 고장 난 건지 조건이 안 맞는 건지 구분할 방법이 없었다.
        //
        // <b>디버그 키는 뭐라도 나와야 한다</b> — 못 고르면 프롤로그로 떨어진다.
        if (k.tKey.wasPressedThisFrame) Replay();
    }

    /// <summary>이번 장 이야기를 다시 튼다. T 키와 로비의 발바닥 버튼이 같이 쓴다.</summary>
    public void Replay()
    {
        {
            string again = StoryScript.CurrentScene();
            if (string.IsNullOrEmpty(again))
            {
                again = "prologue";
                Debug.Log($"[이야기] 지금 장({StoryProgress.CurrentChapter})에 걸린 장면이 없어 " +
                          $"프롤로그를 튼다. 수집품 {CollectionState.Count}/8");
            }
            EnterFresh(again);
        }
    }

    /// <summary>이야기를 튼다. 임무 판정이 붙으면 "필수 임무 성공 → 여기" 로 이어진다.</summary>
    public void Enter(string sceneId)
    {
        if (runner == null || hud == null)
        {
            Debug.LogWarning("[이야기] runner 나 hud 가 안 꽂혀 있어. " +
                             "메뉴에서 Racing → 로비에 대화 시스템 붙이기 를 한 번 눌러줘.");
            return;
        }

        InStory = true;
        Talking = true;
        TalkingScene = sceneId;
        SetLobbyActive(false);

        runner.enabled = true;
        hud.enabled = true;
        runner.onSceneFinished = OnFinished;

        // ★★ 2026-10-16 <b>«봤다»를 끝이 아니라 시작에 찍는다.</b>
        //   전에는 <see cref="OnFinished"/> 에서만 찍어서, 중간에 레이스로 가거나
        //   창을 닫으면 <b>영영 안 찍히고 다음에 또 나왔다.</b>
        //   시작한 장면은 이미 본 것으로 치는 게 맞다 — 또 보여주는 쪽이 훨씬 나쁘고,
        //   다시 보고 싶으면 <b>T</b> 와 곰 기억 앨범이 있다.
        StoryProgress.MarkSeen(sceneId);
        runner.Play(sceneId);
    }

    void OnFinished(string sceneId)
    {
        StoryProgress.MarkSeen(sceneId);

        // ★ 아직 안 본 게 하나 더 있으면 <b>로비로 나가지 않고</b> 바로 이어 튼다.
        //   <see cref="StoryScript.CurrentScene"/> 는 방금 본 것을 빼고 다시 고르니,
        //   «after3 → ch2» 처럼 저절로 순서가 잡힌다.
        // ★★★ 2026-10-15 — <b>에필로그도 바로 이어 붙인다.</b>
        //
        // 제작자: *"에필로그가 전시실 갔다 돌아와야 뜬다."* 전에는 일부러 그렇게 뒀다 —
        // 「결승 뒷이야기 다음 방문에 결말」이라는 <b>간격</b>을 주려던 것이다.
        //
        // <b>그 판단이 틀렸다.</b> 결승을 이기고 뒷이야기를 보고 나면 플레이어는
        // «끝났다» 고 생각하고 게임을 끈다. 심사하는 사람은 더더욱 그렇다 —
        // <b>한 달 쓴 이야기의 결말을 아무도 못 보고 끝난다.</b>
        // 간격으로 얻는 여운보다 <b>결말이 재생되는 것</b>이 훨씬 중요하다.
        //
        // 넘기려고 누른 사람에게 다음 장면을 이어 붙이면 두 번 눌러야 하니 그건 그대로 둔다.
        bool skipped = runner != null && runner.Skipped;

        if (!skipped && chained < MaxChain
            && (sceneId.StartsWith("after") || sceneId == "final_after"))
        {
            string next = StoryScript.CurrentScene();
            if (!string.IsNullOrEmpty(next) && next != sceneId && !StoryProgress.HasSeen(next))
            {
                chained++;
                Enter(next);
                return;
            }
        }

        chained = 0;
        Leave();
    }

    /// <summary>로비로 되돌린다.</summary>
    public void Leave()
    {
        InStory = false;
        Talking = false;
        TalkingScene = "";
        SetLobbyActive(true);

        if (runner != null) { runner.onSceneFinished = null; runner.enabled = false; }
        if (hud != null) hud.enabled = false;
    }

    void SetLobbyActive(bool on)
    {
        // 카메라는 컴포넌트만 껐다 켠다. 오브젝트째 끄면 AudioListener 까지 따라 죽는다.
        if (lobbyCamera != null) lobbyCamera.enabled = on;
        if (storyCamera != null) storyCamera.enabled = !on;

        if (pauseWhileTalking == null) return;
        foreach (var b in pauseWhileTalking)
            if (b != null) b.enabled = on;
    }
}
