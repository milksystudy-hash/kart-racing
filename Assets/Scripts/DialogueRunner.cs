using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이야기 장면을 한 줄씩 재생한다. 글자를 타자기처럼 흘리고, 키를 누르면 다음 줄로 넘어간다.
///
/// 이 스크립트는 <b>대사를 하나도 모른다</b> — StoryScript 에서 받아올 뿐이다.
/// 그래서 대사를 아무리 고쳐도 여기는 손댈 일이 없고, 반대로 연출을 바꿔도 대사는 안전하다.
///
/// 조건 판정(다 모았을 때 / 아직일 때)은 <b>장면을 틀 때 딱 한 번</b> 한다.
/// 장면 도중에 수집품이 늘어날 일은 없으니 그게 맞고, 매 줄 검사하는 것보다 싸다.
///
/// 그리는 건 DialogueHUD 가 맡는다. 나중에 진짜 UI(캔버스)로 갈아엎을 때
/// 버리는 건 HUD 쪽이고 이 스크립트는 그대로 남는다.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Header("어느 장면을 재생할지")]
    [Tooltip("StoryScript 에 적힌 id — prologue / ch1 / ch2 / ch3 / epilogue")]
    public string sceneId = "prologue";

    [Tooltip("켜면 sceneId 를 무시하고, 지금 진행 중인 장에 맞는 장면을 튼다")]
    public bool followChapter = false;

    public bool playOnStart = true;

    [Header("속도와 조작")]
    [Tooltip("1초에 몇 글자씩 나올지. 0 이하면 즉시 전부 나온다")]
    public float charsPerSecond = 34f;

    [Tooltip("켜두면 장면이 끝난 뒤 R 로 다시 볼 수 있다 (테스트용)")]
    public bool allowReplay = true;

    readonly List<DialogueLine> lines = new();
    int index = -1;
    float revealed;

    public bool IsPlaying { get; private set; }
    public bool Finished { get; private set; }
    public string SceneTitle { get; private set; } = "";

    public int LineCount => lines.Count;
    public int LineNumber => Mathf.Clamp(index + 1, 0, lines.Count);

    public DialogueLine Current =>
        (index >= 0 && index < lines.Count) ? lines[index] : default;

    public string CurrentText => Current.text ?? "";

    /// <summary>지금까지 드러난 글자 수. HUD 가 이만큼만 보이게 그린다.</summary>
    public int RevealedCount => Mathf.Clamp(Mathf.FloorToInt(revealed), 0, CurrentText.Length);

    public bool LineFullyShown => RevealedCount >= CurrentText.Length;

    /// <summary>장면이 끝났을 때 불린다. 장 넘기기나 씬 이동을 여기에 붙이면 된다.</summary>
    public System.Action<string> onSceneFinished;

    void Start()
    {
        if (playOnStart) Play(followChapter ? StoryScript.SceneForChapter(StoryProgress.CurrentChapter) : sceneId);
    }

    public void Play(string id)
    {
        sceneId = id;
        SceneTitle = StoryScript.TitleOf(id);

        lines.Clear();
        lines.AddRange(StoryScript.Playable(id));   // ← 조건 판정은 여기서 한 번

        index = -1;
        revealed = 0f;
        Finished = false;

        if (lines.Count == 0)
        {
            Debug.LogWarning($"[대화] '{id}' 장면에 나올 대사가 하나도 없어. " +
                             "StoryScript 의 id 가 맞는지, 조건이 전부 막고 있는 건 아닌지 확인해줘.");
            IsPlaying = false;
            Finished = true;
            return;
        }

        IsPlaying = true;
        Step();
    }

    public void Replay() => Play(sceneId);

    /// <summary>글자가 아직 흐르는 중이면 전부 드러내고, 다 나왔으면 다음 줄로.</summary>
    public void Advance()
    {
        if (!IsPlaying) return;

        if (!LineFullyShown) { revealed = CurrentText.Length; return; }
        Step();
    }

    void Step()
    {
        index++;
        revealed = charsPerSecond > 0f ? 0f : float.MaxValue;

        if (index < lines.Count) return;

        // 마지막 줄까지 다 나왔다
        index = lines.Count - 1;
        IsPlaying = false;
        Finished = true;
        onSceneFinished?.Invoke(sceneId);
    }

    void Update()
    {
        if (IsPlaying && !LineFullyShown && charsPerSecond > 0f)
            revealed += charsPerSecond * Time.unscaledDeltaTime;

        if (AdvancePressed) Advance();
        else if (Finished && allowReplay && ReplayPressed) Replay();
    }

    static bool AdvancePressed
    {
        get
        {
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame ||
                              k.enterKey.wasPressedThisFrame ||
                              k.numpadEnterKey.wasPressedThisFrame))
                return true;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

            var pad = Gamepad.current;
            return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
        }
    }

    static bool ReplayPressed
    {
        get
        {
            var k = Keyboard.current;
            return k != null && k.rKey.wasPressedThisFrame;
        }
    }
}
