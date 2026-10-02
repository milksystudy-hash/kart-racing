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

    [Tooltip("켜두면 T 로 이번 장 이야기를 다시 본다. 제출 전에 꺼")]
    // ★ 2026-10-02 — <b>T(이야기 다시 보기)는 이제 디버그 키가 아니다.</b>
    //   유저: *"스토리는 플레이어가 진행한 상황대로 한 번 더 읽고 싶으면 몇 번 더
    //   읽을 수 있게 해줘."* 제출본에서도 살아 있어야 하니 이 스위치 밖으로 뺐고,
    //   <see cref="LobbyHUD"/> 가 화면에도 적는다 — 「키가 있어도 화면에 없으면 없는 것」.
    //   이 스위치는 이제 아무 것도 안 막는다(남겨둔 건 인스펙터에 이미 저장돼 있어서다).
    public bool debugKeys = true;

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

    static StoryStage Live;

    /// <summary>
    /// 첫 화면(<see cref="TitleScreen"/>) 때문에 미뤄 둔 장면. 타이틀이 로비 <b>안에서</b>
    /// 도니까, 그게 떠 있는 동안 Enter 를 부르면 대사가 <b>타이틀에 가려진 채로 흘러간다.</b>
    /// </summary>
    string pending;

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
        Enter(sceneId);
    }

    void Update()
    {
        // 첫 화면 위로 디버그 키(T · 1~5)가 새어나가면 안 된다.
        if (TitleScreen.Up) return;

        if (pending != null)
        {
            string queued = pending;
            pending = null;
            Enter(queued);
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
        if (k.tKey.wasPressedThisFrame)
        {
            string again = StoryScript.CurrentScene();
            if (string.IsNullOrEmpty(again))
            {
                again = "prologue";
                Debug.Log($"[이야기] 지금 장({StoryProgress.CurrentChapter})에 걸린 장면이 없어 " +
                          $"프롤로그를 튼다. 수집품 {CollectionState.Count}/8");
            }
            Enter(again);
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
        runner.Play(sceneId);
    }

    void OnFinished(string sceneId)
    {
        StoryProgress.MarkSeen(sceneId);
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
