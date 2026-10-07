using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 배경 음악. <b>이 프로젝트의 첫 오디오다</b>(2026-10-01).
///
/// 유저가 두 곡을 주면서 *"데시벨은 둘 다 −18 로 조정했는데 넣어주고 큰지 작은지
/// 판단 좀 해 줄 수 있어?"* 라고 했다. 재 보니 로비 곡이 <b>평균 RMS −18.3 dBFS ·
/// 피크 −4.5 dBFS</b> 로 정확히 맞춰져 있었다. 그건 <b>잘 만든 음원</b>이지
/// «게임에서 틀 크기» 가 아니다 — 음악은 대사·효과음 밑에 깔려야 하니
/// 출력에서 한 번 더 내려야 한다. 그 몫이 아래 <see cref="Default"/> 와 트랙별 이득이다.
///
/// ★ <b>루프 이음새를 크로스페이드로 넘긴다.</b> 받은 파일은 끝 1.2초가 페이드아웃이고
/// 마지막 201ms 는 완전 무음이라, 그냥 <c>loop = true</c> 로 두면 3분마다
/// <b>음악이 끊겼다 다시 시작</b>한다. 소스를 둘 두고 끝나기 2.2초 전에 겹쳐 틀면
/// 페이드아웃이 그대로 도입부(−20.3 dB 에서 올라온다)로 이어진다 —
/// <b>파일을 안 고치고 이음새만 없앤다.</b>
///
/// 스스로 씬에 들어오고 씬을 넘어가도 산다 — 「새 컴포넌트로 고치면 씬을 다시 구워야만
/// 고쳐진다」를 다섯 번 겪은 뒤의 기본형(<see cref="CampusVictory"/> 와 같은 꼴).
/// </summary>
public class Music : MonoBehaviour
{
    /// <summary>유저가 고른 크기. <see cref="ScreenEffects"/> 와 같은 자리에 산다.</summary>
    public const string PrefsKey = "음악크기";

    /// <summary>
    /// 기본 크기. 음원이 −18.3 dBFS 라 0.5 면 실제로 <b>−24.3 dBFS</b> 로 나간다 —
    /// 데스크톱 스피커에서 «들리되 말을 안 덮는» 자리다. 귀로 듣고 <c>−</c> <c>=</c> 로 바꾼다.
    /// </summary>
    const float Default = 0.5f;

    const float SwapFade = 1.1f;      // 곡이 바뀔 때

    /// <summary>
    /// ★★ <b>곡은 «씬 이름» 으로 찾는다</b>(2026-10-01). 유저: *"음악 넣는 방법 좀 알려줘.
    /// 내가 넣어서 네가 맞는지 보면 안 될까."* — 그러려면 <b>파일을 넣는 것 말고 할 일이
    /// 없어야</b> 한다. 유저는 C# 을 못 쓰니(기획서 §9.3) 표에 한 줄 추가하는 방식이면
    /// 결국 나를 불러야 한다.
    ///
    /// <c>Assets/Resources/Music/&lt;씬이름&gt;.wav</c> 를 넣으면 그 씬에서 그 곡이 나온다:
    /// <c>Title · Lobby · Track · Gallery · Campus</c>.
    /// 없는 씬은 그냥 조용하다 — <b>코드는 한 줄도 안 고친다.</b>
    ///
    /// 아래 표는 <b>내가 재서 채우는 것</b>이지 곡을 켜는 스위치가 아니다.
    ///   · <c>gain</c>    — 그 곡만 한 번 더 깎는 값. 곡마다 라우드니스가 다르다
    ///   · <c>overlap</c> — 루프 겹침. <b>파일 꼬리의 무음 + 페이드 길이</b>에서 나온다.
    ///     짧으면 이음새에 무음이 들리고, 길면 앞뒤가 겹쳐 두 겹으로 들린다.
    /// 표에 없는 곡은 (1.00, 1.6) 으로 돈다 — <b>들리기는 한다.</b> 재서 고치면 더 좋아질 뿐.
    /// </summary>
    static (float gain, float overlap) Tuning(string name) => name switch
    {
        // 측정: 피크 −3.9 dBFS · RMS −18.2 · 끝 1.39초 무음 + 그 앞 0.4초 페이드
        "Title"      => (1.00f, 2.0f),
        // 측정: 피크 −4.8 dBFS · RMS −18.3 · 끝 0.20초 무음 + 그 앞 1.2초 페이드
        "Lobby"      => (0.85f, 1.6f),
        // 측정: 피크 −1.2 dBFS · RMS −18.3 · 끝 0.29초 무음 + 그 앞 2.7초 페이드
        // 전시실은 혼자 둘러보는 방이라 조금 더 낮춘다
        "Gallery"    => (0.80f, 3.0f),
        // 측정: 피크 −2.9 dBFS · RMS −18.2 · 끝 0.39초 무음 + 그 앞 2.5초 페이드
        // 악당이 나타나는 자리라 프롤로그보다 조금 올린다 — 분위기가 <b>바뀐 게 들려야</b> 한다
        "villain"    => (0.84f, 2.9f),
        // 측정: 피크 −2.8 dBFS · RMS −18.3 · 끝 0.59초 무음 + 그 앞 1.5초 페이드
        // 다 모은 뒤 곡이라 평소보다 조금 올린다 — «불이 켜졌다» 가 소리로도 와야 한다
        "Gallery_완성" => (0.92f, 2.0f),
        // ── 캠퍼스. 걸어 다니며 건물을 보는 씬이라 <b>발소리와 곰 말소리를 덮으면 안 된다</b> ──
        "Track"       => (0.74f, 2.4f),   // RACE_01 · 피크 −4.9 RMS −18.3 · 꼬리무음 1.52초
        "Track_후반"  => (0.76f, 2.4f),   // 아직 파일 없음 — 없으면 Track 으로 되떨어진다
        "Track_결승"  => (0.82f, 2.0f),   // RACE_BOSS · 피크 −6.5 RMS −18.4 · 꼬리무음 1.08초
        "Track_완성"  => (0.76f, 2.6f),   // Racing After · 피크 −5.5 RMS −18.4 · 꼬리무음 1.66초
        "안전점검훈련" => (0.80f, 2.7f),  // 철곰관 · 피크 −6.3 RMS −18.3 · 꼬리무음 1.79초
        "오늘의급식"   => (0.78f, 2.0f),  // 곰밥마당 · 피크 −3.5 RMS −18.4 · 꼬리무음 1.14초
        "따라그리기"   => (0.76f, 2.7f),  // 재주관 · 피크 −1.7 RMS −18.5 · 꼬리무음 1.83초
        "Campus"      => (0.78f, 3.0f),
        "Campus_완성" => (0.90f, 2.4f),
        // ── 이야기 장면. 파일 이름 = 장면 id (StoryBackdrops 의 그림과 같은 이름) ──
        // 측정: 피크 −3.1 dBFS · RMS −18.1 · 끝 0.41초 무음 + 그 앞 2.4초 페이드
        // 대사 위에 깔리는 곡이라 제일 낮춘다 — 글을 읽는 화면이다
        "prologue"   => (0.72f, 2.8f),
        "ch3"        => (0.74f, 2.9f),   // 관장의 배신 · 피크 −2.9 RMS −18.5 · 제일 어두운 곡(밝기 677Hz)
        "epilogue"   => (0.80f, 3.1f),   // 에필로그 · 피크 −3.6 RMS −18.2 · 제일 밝은 곡(1162Hz)
        _            => (1.00f, 1.6f),
    };

    public static float Volume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKey, Default));
        set
        {
            PlayerPrefs.SetFloat(PrefsKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// <b>말소리가 나는 동안 음악을 낮춘다.</b>
    ///
    /// 2026-10-02 유저: *"배경음악이 너무 커서 배관공들 소리가 잘 안 들려."*
    /// 음악을 통째로 낮추면 <b>조용한 게임</b>이 되고, 효과음만 올리면 거슬린다.
    /// 방송에서 쓰는 방법이 <b>더킹</b>이다 — 말이 나올 때만 음악이 비켜 준다.
    /// 끝나면 천천히 돌아오니 음악이 꺼진 줄 모르고, 말은 또렷하게 들린다.
    /// </summary>
    const float DuckTo = 0.34f;

    static float duckUntil = -99f;

    /// <summary>이만큼 음악을 비켜 준다. 소리 길이보다 조금 길게 준다.</summary>
    public static void Duck(float seconds) =>
        duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + seconds);

    /// <summary>지금 음악에 곱할 값. 눌릴 때는 빠르게, 돌아올 때는 천천히.</summary>
    static float ducked = 1f;

    static float Ducked
    {
        get
        {
            bool down = Time.unscaledTime < duckUntil;
            float want = down ? DuckTo : 1f;
            float speed = down ? 7f : 1.6f;   // 비키는 건 빠르게, 돌아오는 건 천천히
            ducked = Mathf.MoveTowards(ducked, want, speed * Time.unscaledDeltaTime);
            return ducked;
        }
    }

    static Music live;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (live != null) return;
        var go = new GameObject("Music");
        DontDestroyOnLoad(go);
        live = go.AddComponent<Music>();
    }

    AudioSource[] deck;
    int front;
    string playing = "";
    float gain;
    float overlap = 1.6f;
    readonly System.Collections.Generic.HashSet<string> missing = new();

    void Awake()
    {
        deck = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;              // 이음새는 우리가 겹쳐서 넘긴다
            s.spatialBlend = 0f;         // 2D — 카메라가 바뀌어도 소리는 그대로
            s.volume = 0f;
            s.priority = 0;              // 음악은 밀려나면 안 된다
            deck[i] = s;
        }
    }

    void Update()
    {
        Keys();

        string want = Wanted();
        if (want != playing) Swap(want);

        var cur = deck[front];
        float target = Volume * gain * Ducked;

        // 끝나기 전에 다음 바퀴를 겹쳐 튼다
        if (cur.clip != null && cur.isPlaying && !string.IsNullOrEmpty(playing)
            && cur.time > cur.clip.length - overlap)
        {
            var next = deck[1 - front];
            if (!next.isPlaying)
            {
                next.clip = cur.clip;
                next.volume = 0f;
                next.time = 0f;
                next.Play();
                front = 1 - front;
            }
        }

        // 앞 소스는 올리고 뒤 소스는 내린다
        float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, Mathf.Min(overlap, SwapFade * 1.6f));
        for (int i = 0; i < 2; i++)
        {
            float goal = (i == front) ? target : 0f;
            deck[i].volume = Mathf.MoveTowards(deck[i].volume, goal, step);
            if (i != front && deck[i].volume <= 0.001f && deck[i].isPlaying) deck[i].Stop();
        }
    }

    /// <summary>지금 어느 곡이어야 하나. 타이틀이 떠 있으면 타이틀 곡.</summary>
    /// <summary>
    /// 지금 나와야 할 곡 이름 = <b>씬 이름</b>. 첫 화면만 예외로 <c>Title</c>.
    ///
    /// ★ <b>«_완성» 을 붙인 파일이 있으면 수집품 8개를 다 모은 뒤에 그걸 튼다</b>
    /// (2026-10-02 유저 요청 — 전시실 곡을 전후로 가른다). 어느 씬에나 통하는 규칙이라
    /// 나중에 캠퍼스나 로비에 «다 모은 뒤» 곡을 넣고 싶으면 <b>파일만 더 넣으면 된다.</b>
    /// 파일이 없으면 <see cref="Swap"/> 이 원래 이름으로 되떨어진다.
    /// </summary>
    static string Wanted()
    {
        if (TitleScreen.Up) return "Title";

        // ★★ 2026-10-02 — 이야기가 도는 동안에는 <b>장면 id 로 곡을 찾는다.</b>
        //   이야기는 로비 «안에서» 도니까(§3.6) 씬 이름으로만 고르면 프롤로그에도
        //   로비 곡이 깔린다. 파일 이름이 <see cref="StoryBackdrops"/> 의 그림과
        //   <b>똑같아서</b> 외울 게 하나뿐이다:
        //   <c>Music/prologue.wav</c> ↔ <c>StoryBackdrops/prologue.png</c>
        //   곡이 없는 장면은 <see cref="Swap"/> 가 로비 곡으로 되떨어진다.
        if (StoryStage.Talking)
        {
            // 장소에 제 곡이 있으면 그게 먼저다. 없으면 장면 곡, 그것도 없으면 씬 곡.
            // 이야기가 «바깥 → 악당 등장» 으로 넘어갈 때 음악만 갈아끼울 수 있다.
            string place = StoryStage.TalkingPlace;
            if (!string.IsNullOrEmpty(place) && Resources.Load<AudioClip>("Music/" + place) != null)
                return place;

            if (!string.IsNullOrEmpty(StoryStage.TalkingScene)) return StoryStage.TalkingScene;
        }

        // ★★ 2026-10-06 — 미니게임은 <b>제 곡을 쓴다.</b> 유저가 «배경음악으로 쓸까
        //   전용 테마로 쓸까» 물었는데 <b>전용 테마가 맞다</b>: 미니게임은 화면도 규칙도
        //   따로인 <b>다른 판</b>이라, 캠퍼스 곡이 그대로 깔리면 «메뉴를 연 것» 처럼 들린다.
        //   60초짜리 훈련에는 그 60초에 맞는 박자가 필요하다.
        //   곡이 없으면 <see cref="Swap"/> 가 씬 곡으로 되떨어진다 — 없어도 안 망가진다.
        if (SafetyDrill.Open) return "안전점검훈련";
        if (Canteen.Open) return "오늘의급식";
        if (TracingGame.Active != null) return "따라그리기";

        string scene = SceneManager.GetActiveScene().name;

        // ★★ 2026-10-06 유저: *"8판마다 각각 음악이 다 달라야 돼?"* <b>아니다.</b>
        //   한 판이 1분 27초인데 판마다 다른 곡이면 <b>어느 곡도 두 번 안 들린다</b> —
        //   그러면 «이 게임의 음악» 이 생기지 않는다. 레이싱 게임이 곡을 가르는 기준은
        //   «몇 판째» 가 아니라 <b>긴장도</b>고, 같은 트랙을 여덟 번 도는 지루함은
        //   이미 <see cref="RaceProps"/> 가 판마다 다른 풍경으로 맡고 있다.
        //
        //   그래서 셋이면 충분하다. <b>뒤의 셋은 없어도 된다</b>(없으면 Track 으로 되떨어진다):
        //     Track        판 1~5    — 기본
        //     Track_후반   판 6~8    — 같은 멜로디를 무겁게
        //     Track_결승   9판       — 개발업자·시의원과의 대결
        //     Track_완성   자유 주행 — 다 끝낸 뒤
        if (scene == "Track")
        {
            string tier = GrandFinal.FreeRun         ? "Track" + Done
                        : GrandFinal.Available       ? "Track_결승"
                        : CollectionState.Count >= 5 ? "Track_후반"
                        : "Track";
            if (tier != "Track" && Resources.Load<AudioClip>("Music/" + tier) == null) tier = "Track";
            return tier;
        }

        // ★ 2026-10-06 <b>캠퍼스만 기준이 다르다.</b> 다른 씬은 «수집품 8개» 가 분기점인데
        //   (전시실은 그때 불이 켜진다), <b>캠퍼스는 결승을 이겨야 바뀐다</b> —
        //   판자도 광고판도 동상도 <see cref="GrandFinal.FreeRun"/> 에서 걷힌다.
        //   8/8 에 곡만 먼저 바뀌면 <b>아직 판자로 막힌 캠퍼스에 축제 음악</b>이 깔린다.
        bool done = scene == "Campus"
                  ? GrandFinal.FreeRun
                  : CollectionState.Count >= ExhibitCatalogue.All.Length;

        return done ? scene + Done : scene;
    }

    const string Done = "_완성";

    void Swap(string want)
    {
        playing = want;
        (gain, overlap) = Tuning(want);

        // 지금 나오던 건 그냥 페이드아웃 — Update 가 내려 준다
        if (string.IsNullOrEmpty(want)) { front = 1 - front; deck[front].clip = null; return; }

        var clip = Resources.Load<AudioClip>("Music/" + want);

        // «_완성» 곡이 없으면 평소 곡으로 되떨어진다 — 그 씬이 조용해지면 안 된다
        if (clip == null && want.EndsWith(Done))
        {
            string plain = want.Substring(0, want.Length - Done.Length);
            clip = Resources.Load<AudioClip>("Music/" + plain);
            if (clip != null) (gain, overlap) = Tuning(plain);
        }

        // 이야기 장면에 제 곡이 없으면 <b>그 씬 곡</b>으로. 이야기 중에 음악이 뚝 끊기면
        // «버그» 로 읽힌다 — 조용해지는 건 연출일 때만 해야 한다.
        // ★ 이야기 장면뿐 아니라 <b>미니게임 곡이 없을 때도</b> 씬 곡으로 되떨어진다.
        //   «곡이 없으면 조용해진다» 가 기본값이면 파일 하나 빠뜨렸을 때 버그로 보인다.
        if (clip == null)
        {
            string scene = SceneManager.GetActiveScene().name;
            clip = Resources.Load<AudioClip>("Music/" + scene);
            if (clip != null) (gain, overlap) = Tuning(scene);
        }

        if (clip == null)
        {
            // ★ 곡이 없는 씬이 <b>정상</b>이다. 그래서 씬마다 한 번만 알리고 조용히 넘어간다 —
            //   «없다» 를 매번 띄우면 콘솔이 경고로 덮여서 진짜 문제를 못 본다.
            if (!missing.Contains(want))
            {
                missing.Add(want);
                Debug.Log($"[음악] '{want}' 씬 곡 없음 — Assets/Resources/Music/{want}.wav 를 넣으면 나온다.");
            }
            front = 1 - front;
            deck[front].clip = null;
            return;
        }

        front = 1 - front;
        var s = deck[front];
        s.clip = clip;
        s.volume = 0f;
        s.time = 0f;
        s.Play();
    }

    /// <summary>
    /// <c>−</c> 로 줄이고 <c>=</c> 로 키운다. 10% 씩, 화면에 숫자가 뜬다.
    /// <b>키가 있어도 화면에 없으면 없는 것</b>이라 조작법 카드에도 적어 뒀다.
    /// </summary>
    void Keys()
    {
        var k = Keyboard.current;
        if (k == null) return;

        int d = 0;
        if (k.minusKey.wasPressedThisFrame || k.numpadMinusKey.wasPressedThisFrame) d = -1;
        else if (k.equalsKey.wasPressedThisFrame || k.numpadPlusKey.wasPressedThisFrame) d = 1;
        if (d == 0) return;

        float v = Mathf.Clamp01(Mathf.Round(Volume * 10f + d) / 10f);
        Volume = v;
        shownAt = Time.unscaledTime;   // 글이 아니라 <b>그림</b>으로 보여준다
    }

    // ---- 음량 표시 ----------------------------------------------------------

    float shownAt = -99f;
    const float ShowFor = 1.8f;

    /// <summary>
    /// 2026-10-01 유저: *"소리 크게 하고 줄이는 아이콘 같은 걸로 표시하면 좋을 듯."*
    /// 맞다 — <b>«음악 70%» 라는 글은 읽어야 알지만 막대는 보면 안다.</b>
    /// 소리를 조절하는 손은 <b>숫자를 읽을 틈이 없다.</b>
    ///
    /// 나무 판 + 종이 라벨 + 스피커 — 이 게임의 HUD 문법 그대로다(<see cref="Hud"/>).
    /// 둥근 모서리도 알약 버튼도 안 쓴다.
    /// </summary>
    void OnGUI()
    {
        float age = Time.unscaledTime - shownAt;
        if (age > ShowFor) return;

        Rect screen = Hud.Begin(null);
        float fade = Mathf.Clamp01((ShowFor - age) / 0.45f);
        var keep = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, fade);

        const float PW = 214f, PH = 58f;
        var panel = new Rect(screen.width * 0.5f - PW * 0.5f, screen.height - PH - 26f, PW, PH);
        Hud.Panel(panel);

        var inner = Hud.Inner(panel);
        float v = Volume;
        bool off = v <= 0.001f;

        // ── 스피커 — 상자 둘로 «몸통 + 나팔». 삼각형은 IMGUI 로 못 그리니 계단으로 낸다
        float sx = inner.x + 6f, cy = inner.y + inner.height * 0.5f;
        Box(new Rect(sx, cy - 5f, 7f, 10f), Hud.Ink);                  // 몸통
        for (int i = 0; i < 4; i++)                                    // 나팔 — 뒤로 갈수록 넓게
            Box(new Rect(sx + 7f + i * 2.5f, cy - 5f - i * 2.6f, 2.5f, 10f + i * 5.2f), Hud.Ink);

        float wx = sx + 20f;
        if (off)
        {
            // 꺼짐은 <b>×</b>. 막대가 0개인 것과 «꺼졌다» 는 다르게 보여야 한다.
            for (int i = 0; i < 9; i++)
            {
                Box(new Rect(wx + 2f + i, cy - 8f + i * 1.8f, 2f, 2f), Hud.Ribbon);
                Box(new Rect(wx + 2f + i, cy + 8f - i * 1.8f, 2f, 2f), Hud.Ribbon);
            }
        }
        else
        {
            // 소리 결 — 크면 멀리까지 간다
            for (int i = 0; i < 3; i++)
            {
                if (v < (i + 1) * 0.28f) break;
                Box(new Rect(wx + i * 5f, cy - 4f - i * 3.5f, 2.2f, 8f + i * 7f), Hud.InkSoft);
            }
        }

        // ── 막대 10칸 ──────────────────────────────────────────────────
        float bx = inner.x + 56f, bw = (inner.width - 62f) / 10f;
        int lit = Mathf.RoundToInt(v * 10f);
        for (int i = 0; i < 10; i++)
        {
            var slot = new Rect(bx + i * bw, inner.y + 10f, bw - 3f, inner.height - 28f);
            Box(slot, i < lit ? Hud.Brass : new Color(Hud.InkSoft.r, Hud.InkSoft.g, Hud.InkSoft.b, 0.22f));
        }

        GUI.Label(new Rect(inner.x, inner.yMax - 17f, inner.width, 16f),
                  off ? "음악 꺼짐        −  =" : $"음악 {Mathf.RoundToInt(v * 100f)}%        −  =",
                  Hud.Resize(Hud.Tiny, 11, TextAnchor.MiddleCenter));

        GUI.color = keep;
        Hud.End();
    }

    static void Box(Rect r, Color c)
    {
        var keep = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * keep.a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = keep;
    }
}
