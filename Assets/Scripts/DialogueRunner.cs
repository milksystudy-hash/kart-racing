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

    /// <summary>
    /// 지금 보여줄 배경 그림 이름. 장면 id 로 시작했다가
    /// <see cref="DialogueLine.At"/> 가 걸린 줄을 지나면 그 이름으로 바뀐다.
    /// <b>한 번 바뀌면 그대로 간다</b> — 줄마다 적을 필요가 없다.
    /// </summary>
    public string Place { get; private set; } = "";

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
        Place = id;                       // 장면 이름이 곧 첫 배경

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
        lineAt = Time.unscaledTime;

        if (index < lines.Count)
        {
            // ★ 이 줄에 장소가 적혀 있고 <b>그 그림이 실제로 있으면</b> 바꾼다.
            //   없으면 앞 그림이 그대로 간다 — 이름을 잘못 적었다고 배경이 사라지면
            //   «깨진 것» 으로 보이고, 원인이 오타라는 걸 알 방법이 없다.
            // 암전하고 넘어가는 줄 — Fade 가 씬을 안 바꾸고 잠깐 까맣게 덮는다
            if (lines[index].cut) Fade.Blink(0.22f);

            string p = lines[index].place;
            if (!string.IsNullOrEmpty(p))
            {
                if (Resources.Load<Texture2D>("StoryBackdrops/" + p) != null) Place = p;
                else Debug.LogWarning($"[대화] '{p}' 배경이 없어 앞 그림을 그대로 쓴다 — " +
                                      $"Assets/Resources/StoryBackdrops/{p}.png 를 넣어줘.");
            }
            return;
        }

        // 마지막 줄까지 다 나왔다
        index = lines.Count - 1;
        IsPlaying = false;
        Finished = true;
        onSceneFinished?.Invoke(sceneId);
    }

    /// <summary>
    /// <b>빠르게 두 번 누르면 그 줄이 한꺼번에 나온다.</b>
    ///
    /// 2026-10-02 유저: *"CTRL 눌러도 바뀌는 게 없으니 아예 빼고, 대신 마우스를
    /// 빠르게 두 번 누르면 대사가 한 번에 나오게."* CTRL 은 뺐다.
    ///
    /// 한 번 누르는 것과 두 번 누르는 것이 <b>다른 일을 한다</b>:
    /// 한 번은 «읽었다, 다음», 두 번은 «기다리기 싫다, 다 보여줘».
    /// 두 번째 누름은 <see cref="MinOnScreen"/> 도 뚫는다 — 참을성이 없다는 뜻이니까.
    /// </summary>
    const float DoubleClick = 0.32f;

    float lastClickAt = -99f;

    /// <summary>줄이 뜨고 이만큼은 못 넘긴다. 짧은 대사가 스쳐 지나가는 걸 막는다.</summary>
    const float MinOnScreen = 0.28f;

    float lineAt;

    void Update()
    {
        if (IsPlaying && !LineFullyShown && charsPerSecond > 0f)
            revealed += charsPerSecond * Time.unscaledDeltaTime;

        if (!AdvancePressed)
        {
            if (Finished && allowReplay && ReplayPressed) Replay();
            return;
        }

        // 빠르게 두 번 — 그 줄을 한꺼번에 보여준다. 여기서는 넘기지 않는다,
        // <b>다 읽을 기회를 주는 것</b>이지 건너뛰는 게 아니니까.
        bool again = Time.unscaledTime - lastClickAt <= DoubleClick;
        lastClickAt = Time.unscaledTime;

        if (again && IsPlaying && !LineFullyShown)
        {
            revealed = float.MaxValue;
            return;
        }

        // ★★ 2026-10-02 유저: *"짧은 대사는 바로 휘릭 지나간다."*
        //   짧은 줄은 타자가 0.1초면 끝나서 <b>«다 보여줘» 로 누른 키가 그대로 «다음»</b>
        //   이 된다. 줄이 바뀐 뒤 잠깐은 안 넘긴다 — 긴 줄에서는 타자가 그보다
        //   오래 걸려서 아무 차이가 없다.
        if (Time.unscaledTime - lineAt < MinOnScreen) return;

        Advance();
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
