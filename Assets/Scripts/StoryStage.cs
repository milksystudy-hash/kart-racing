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
    public bool debugKeys = true;

    public bool InStory { get; private set; }

    void Start()
    {
        Leave();   // 시작은 무조건 로비 상태로

        if (!playUnseenOnEnter) return;

        // ★ 장 번호만 보면 결말이 어긋난다(2026-09-21) — 결승 장면이 영영 안 나오고,
        // 에필로그가 7판 뒤에 떴다. CurrentScene 이 «결승을 깼는가» 까지 보고 고른다.
        string sceneId = StoryScript.CurrentScene();
        if (!string.IsNullOrEmpty(sceneId) && !StoryProgress.HasSeen(sceneId)) Enter(sceneId);
    }

    void Update()
    {
        if (!debugKeys || InStory) return;

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
