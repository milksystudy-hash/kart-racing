using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// <b>게임 첫 화면.</b> 유저: *"게임 첫화면도 만들어야 하는데. 그냥 키자마자 로비씬 나오면
/// 그렇잖아."* 맞는 말이다 — 문 없이 방부터 나오는 집은 없다.
///
/// ★★ <b>씬을 새로 만들지 않는다.</b> 이야기 장면을 로비 안에서 돌리는 것과 같은 이유고
/// (2026-09-15), 여기서는 이유가 하나 더 있다:
///
/// <b>유니티는 빌드 설정의 0번 씬으로 부팅한다.</b> 타이틀을 별도 씬으로 만들면
/// 0번에 놓아야 하는데, 이 프로젝트의 씬 등록은 <b>뒤에 붙이기만</b> 한다
/// (F1 로비 · F2 트랙 · F3 전시실 · F4 캠퍼스 — 중간에 끼우면 외운 F 키가 전부 밀린다).
/// 즉 «별도 씬» 과 «F 키 유지» 는 동시에 성립하지 않는다.
///
/// 로비 안에서 띄우면 셋이 한꺼번에 풀린다 —
/// <b>0번 씬이 곧 첫 화면</b>이고, <b>기하를 하나도 안 만들고</b>,
/// «게임 시작» 이 <b>로딩 없이 즉시</b>다(이미 로비에 서 있으니까).
/// 배경은 유저가 꾸민 중앙홀 그대로고, 궤도 카메라가 알아서 천천히 돈다.
///
/// <b>스스로 씬에 들어온다</b>(<see cref="CampusVictory"/> 와 같은 꼴) —
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 이 프로젝트에서 다섯 번 겪었다.
/// 로비를 다시 굽지 않아도 바로 뜬다.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    // ==================================================================
    //  ★ 게임 이름 — 여기 한 줄만 고치면 화면이 따라온다
    // ==================================================================
    /// <summary>
    /// 게임 제목. <b>유저가 아직 안 정했다</b>(2026-09-28: *"게임 이름 나중에 바꿀 거니까
    /// 타이틀 이름은 나중에 적어주고"*). 비워 두면 <see cref="Placeholder"/> 가 흐리게 뜬다 —
    /// 빈 판으로 두면 «글자가 안 나오는 버그» 로 보이고, 흐린 글씨는 «아직 안 적었다» 로 보인다.
    /// </summary>
    public static string GameTitle = "";

    /// <summary>제목 아래 한 줄. 부제가 생기면 여기. 비어 있으면 아예 안 그린다.</summary>
    public static string GameSubtitle = "";

    const string Placeholder = "제목 미정";

    static readonly string[] Items = { "게임 시작", "나가기" };

    // ==================================================================
    //  상태
    // ==================================================================
    /// <summary>
    /// 타이틀이 떠 있나. <b>다른 것들이 이 값을 보고 스스로 비킨다</b> —
    /// <see cref="RacePause"/>·<see cref="Canteen"/> 과 같은 방식이라
    /// 타이틀이 남을 붙잡으러 다닐 필요가 없다.
    /// </summary>
    public static bool Up { get; private set; }

    /// <summary>앱을 켜고 <b>한 번만</b> 띄운다. 레이스 끝나고 로비로 돌아올 때마다 뜨면 문이 아니라 벽이다.</summary>
    static bool shownThisRun;

    /// <summary>
    /// 배경 그림. <b>있으면 쓰고 없으면 3D 로비가 그대로 배경</b>이다 —
    /// 유저가 나중에 <c>Assets/Resources/Backgrounds/Title.png</c> 를 넣기만 하면 들어온다.
    /// 코드도 씬도 안 고친다.
    /// </summary>
    static Texture2D backdrop;
    static bool backdropTried;

    static Texture2D Backdrop()
    {
        if (backdropTried) return backdrop;
        backdropTried = true;
        backdrop = Resources.Load<Texture2D>("Backgrounds/Title");
        return backdrop;
    }

    Behaviour[] paused;
    LobbyOrbitCamera orbit;
    float savedIdleDelay;
    int cursor;
    float bornAt;

    // ==================================================================
    //  스스로 들어온다
    // ==================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        shownThisRun = false;
        Up = false;
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shownThisRun) return;

        // <b>0번 씬 = 게임이 부팅하는 씬</b>. 이름이 아니라 번호로 보는 이유는,
        // 나중에 씬 순서가 바뀌어도 «첫 화면» 이라는 뜻이 안 흔들리기 때문이야.
        // 에디터에서 트랙(1번)을 열고 ▶ 를 누르면 안 뜬다 — 그게 맞다.
        if (scene.buildIndex != 0) return;

        shownThisRun = true;
        new GameObject("TitleScreen").AddComponent<TitleScreen>();
    }

    // ==================================================================
    //  로비를 잠깐 멈춘다
    // ==================================================================
    void Awake()
    {
        // ★ Awake 에서 켜는 게 중요하다. <see cref="SceneManager.sceneLoaded"/> 는
        // 씬 오브젝트들의 Start 보다 <b>먼저</b> 돌고 AddComponent 는 그 자리에서 Awake 를
        // 돌리니까, <see cref="StoryStage"/>.Start 가 이 값을 보고 이야기를 미룰 수 있다.
        Up = true;
        bornAt = Time.unscaledTime;

        var list = new List<Behaviour>();
        Pause<LobbySelector>(list);   // 받침대 고르기
        Pause<LobbyHUD>(list);        // 안내 대여섯 장 — 큰 화면은 한 번에 한 장
        Pause<WalkMode>(list);        // TAB 걷기
        Pause<DeskClock>(list);       // E 시계
        paused = list.ToArray();
        foreach (var b in paused) b.enabled = false;

        // 궤도 카메라는 <b>안 끈다</b> — 홀이 천천히 도는 게 이 화면의 배경이다.
        // 원래 5초 기다렸다 도는 걸(idleDelay) 거의 바로 돌게만 바꾼다.
        // 0 이하로 두면 <see cref="LobbyOrbitCamera.HandleIdleSpin"/> 가 통째로 꺼진다.
        orbit = FindFirstObjectByType<LobbyOrbitCamera>(FindObjectsInactive.Include);
        if (orbit != null)
        {
            savedIdleDelay = orbit.idleDelay;
            orbit.idleDelay = 0.4f;
        }
    }

    /// <summary>켜져 있던 것만 목록에 넣는다 — 되돌릴 때 <b>꺼져 있던 걸 켜면 안 되니까.</b></summary>
    static void Pause<T>(List<Behaviour> into) where T : Behaviour
    {
        var b = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (b != null && b.enabled) into.Add(b);
    }

    void OnDestroy()
    {
        // ★ 되돌리는 자리를 <b>여기 하나로</b> 모은다. 나가는 길이 여럿이어도
        // (ENTER · 클릭 · 오브젝트 삭제) 전부 이 함수를 지난다 —
        // 한 군데라도 빠뜨리면 로비가 잠긴 채로 남는다(일시정지에서 배운 것).
        Up = false;

        if (paused != null)
            foreach (var b in paused)
                if (b != null) b.enabled = true;

        if (orbit != null) orbit.idleDelay = savedIdleDelay;
    }

    // ==================================================================
    //  조작
    // ==================================================================
    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Move(1);
        if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Move(-1);

        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame
            || k.spaceKey.wasPressedThisFrame)
        {
            Choose(cursor);
            return;
        }

        // 마우스 — 올리면 그 칸이 잡히고, 누르면 고른다.
        // ★ 가상 1280×720 으로 내려서 잰다. <see cref="Hud.ScaleFactor"/> 를 안 나누면
        // 1080p 에서 칸이 통째로 어긋난다(태엽 회전축에서 겪은 것과 같은 함정).
        var m = Mouse.current;
        if (m == null) return;

        Vector2 p = m.position.ReadValue();
        float s = Mathf.Max(0.01f, Hud.ScaleFactor);
        var v = new Vector2(p.x / s, (Screen.height - p.y) / s);
        float w = Screen.width / s, h = Screen.height / s;

        for (int i = 0; i < Items.Length; i++)
        {
            if (!ItemRect(i, w, h).Contains(v)) continue;
            cursor = i;
            if (m.leftButton.wasPressedThisFrame) Choose(i);
            return;
        }
    }

    void Move(int step)
    {
        cursor = (cursor + step + Items.Length) % Items.Length;
    }

    void Choose(int index)
    {
        // 뜨자마자 눌린 키 하나로 닫히는 걸 막는다 — 앞 화면에서 누르던 키가 넘어온다.
        if (Time.unscaledTime - bornAt < 0.25f) return;

        if (index == 0)
        {
            Destroy(gameObject);   // 게임 시작 — 이미 로비에 서 있으니 로딩이 0이다
            return;
        }

        Quit();
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================================================================
    //  화면
    // ==================================================================
    /// <summary>
    /// 칸 자리. <b>Update 와 OnGUI 가 같은 함수를 쓴다</b> —
    /// 같은 숫자를 두 군데서 계산하면 반드시 어긋난다(마우스가 빈 데를 누르게 된다).
    /// </summary>
    static Rect ItemRect(int i, float w, float h)
    {
        const float iw = 320f, ih = 58f, gap = 16f;
        return new Rect((w - iw) * 0.5f, h * 0.56f + i * (ih + gap), iw, ih);
    }

    void OnGUI()
    {
        Rect screen = Hud.Begin(null);
        float w = screen.width, h = screen.height;

        // ★ <b>ScaleAndCrop</b> — 화면 비율이 그림과 달라도 여백 없이 꽉 채우고
        // 넘치는 쪽을 자른다. 늘리면(StretchToFill) 사람 얼굴이 홀쭉해진다.
        var bg = Backdrop();
        if (bg != null) GUI.DrawTexture(new Rect(0f, 0f, w, h), bg, ScaleMode.ScaleAndCrop);

        Dim(w, h, bg != null);
        Plaque(w, h);

        for (int i = 0; i < Items.Length; i++) DrawItem(i, ItemRect(i, w, h));

        // 아래 구석 안내. 키를 안 적어 두면 «마우스로만 되는 줄» 안다.
        // 자리는 <b>마지막 칸에서 뽑는다</b> — 칸 수가 늘어도 저절로 따라온다.
        float hintY = ItemRect(Items.Length - 1, w, h).yMax + 16f;
        GUI.Label(new Rect(0f, hintY, w, 22f), "↑↓  고르기      ENTER  결정",
                  Hud.Resize(Hud.Tiny, 12, TextAnchor.MiddleCenter));

        Hud.End();
    }

    /// <summary>
    /// 배경 누르기. <b>아래로 갈수록 짙게</b> — 메뉴가 아래에 있고,
    /// 균일하게 덮으면 «회색 필터» 로 보이지 «조명» 으로 안 보인다.
    /// 초상화 뒤를 눌러 준 것과 같은 방식(2026-09-28).
    /// </summary>
    static void Dim(float w, float h, bool hasArt)
    {
        // 그린 배경이 있으면 <b>훨씬 약하게</b> 누른다 — 명암은 그린 사람이 이미 잡아 놨고,
        // 그 위에 또 덮으면 그림을 망치는 거다. 3D 로비가 배경일 때만 세게 누른다.
        float lo = hasArt ? 0.10f : 0.34f;
        float hi = hasArt ? 0.46f : 0.80f;

        const int bands = 20;
        var old = GUI.color;
        for (int i = 0; i < bands; i++)
        {
            float t = i / (float)(bands - 1);
            GUI.color = new Color(0.05f, 0.04f, 0.035f, Mathf.Lerp(lo, hi, t * t));
            GUI.DrawTexture(new Rect(0f, h * i / bands, w, h / bands + 1f), Texture2D.whiteTexture);
        }
        GUI.color = old;
    }

    /// <summary>
    /// 제목판. <b>매다는 끈 두 줄</b>을 위로 뽑았다 — 벽에 박힌 판과 매달린 판은 다르게 보이고,
    /// 이 게임의 건물 현판이 전부 처마 밑에 매달려 있어서 <b>같은 물건으로 읽힌다.</b>
    /// </summary>
    void Plaque(float w, float h)
    {
        bool named = !string.IsNullOrWhiteSpace(GameTitle);
        string text = named ? GameTitle : Placeholder;

        float pw = Mathf.Min(640f, w - 60f);
        float ph = Mathf.Min(148f, h * 0.26f);
        var r = new Rect((w - pw) * 0.5f, h * 0.17f, pw, ph);

        var old = GUI.color;
        GUI.color = Hud.WoodDark;
        GUI.DrawTexture(new Rect(r.x + pw * 0.22f, 0f, 3f, r.y), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - pw * 0.22f, 0f, 3f, r.y), Texture2D.whiteTexture);
        GUI.color = old;

        Hud.Panel(r);
        Rect inner = Hud.Inner(r);

        bool sub = !string.IsNullOrWhiteSpace(GameSubtitle);
        var titleStyle = Hud.Resize(Hud.Title, 40, TextAnchor.MiddleCenter);

        // 이름이 아직 없으면 <b>흐리게</b>. 자리는 잡혀 있고 글자만 안 정해졌다는 뜻이 된다.
        GUI.color = named ? Color.white : new Color(1f, 1f, 1f, 0.42f);
        GUI.Label(sub ? new Rect(inner.x, inner.y, inner.width, inner.height * 0.62f) : inner,
                  text, titleStyle);
        GUI.color = old;

        if (!sub) return;

        Hud.Rule(inner.x + inner.width * 0.3f, inner.y + inner.height * 0.64f, inner.width * 0.4f);
        GUI.Label(new Rect(inner.x, inner.y + inner.height * 0.66f, inner.width, inner.height * 0.34f),
                  GameSubtitle, Hud.Resize(Hud.Label, 16, TextAnchor.MiddleCenter));
    }

    /// <summary>
    /// 고른 칸은 <b>나무 판</b>, 안 고른 칸은 <b>옅은 쪽지</b>.
    /// 색만 바꾸면 «둘 다 버튼» 으로 보이고 어느 게 잡혔는지 헷갈린다 —
    /// 재질이 달라야 한눈에 갈린다(전시실에서 배운 것).
    /// </summary>
    void DrawItem(int i, Rect r)
    {
        bool on = i == cursor;
        var old = GUI.color;

        if (on)
        {
            Hud.Panel(r);
            GUI.color = Hud.Brass;
            GUI.DrawTexture(new Rect(r.x + 14f, r.y + r.height * 0.5f - 5f, 10f, 10f),
                            Texture2D.whiteTexture);
            GUI.color = old;
        }
        else
        {
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Hud.Chip(r);
            GUI.color = old;
        }

        var style = Hud.Resize(Hud.Text, on ? 24 : 21, TextAnchor.MiddleCenter);
        style.normal.textColor = on ? Hud.Ink : Hud.InkSoft;
        GUI.Label(r, Items[i], style);
    }
}
