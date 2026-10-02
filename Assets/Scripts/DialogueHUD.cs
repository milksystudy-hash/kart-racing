using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이야기 장면을 화면에 그린다 — 초상화 자리, 이름표, 대화창.
///
/// 기획서 §3.6 의 구성 그대로다: 3D 박물관 배경 + 2D 초상화 + 이름표와 대화창.
/// 배경은 이 스크립트가 아니라 씬에 서 있는 진짜 3D 물건들이고, 여기서는 그 위에 얹는 것만 그린다.
///
/// <b>초상화가 아직 없어도 대사 흐름은 전부 확인된다.</b> 없으면 그 사람 색의 네모가
/// 대신 서 있고, Assets/Resources/Portraits/세진_기쁨.png 처럼 그림을 넣는 순간
/// 코드를 한 줄도 안 고치고 그 자리에 들어간다.
///
/// 임시 HUD 라서 OnGUI 로 그린다 — 캔버스 세팅이 필요 없다. 다른 HUD 들과 같이
/// 나중에 진짜 UI 로 갈아엎을 때 통째로 버릴 스크립트야. DialogueRunner 는 남는다.
/// </summary>
public class DialogueHUD : MonoBehaviour
{
    [Header("연결")]
    public DialogueRunner runner;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    [Header("테스트 도우미")]
    [Tooltip("켜두면 1~5 로 장면 이동, F9 전부 수집 / F10 전부 지우기. 제출 전에 꺼")]
    public bool debugKeys = true;

    static readonly Color BoxColor    = new Color(0.10f, 0.13f, 0.20f, 0.90f);
    static readonly Color FrameColor  = new Color(0.93f, 0.90f, 0.84f, 0.85f);
    static readonly Color TitleColor  = new Color(0.09f, 0.10f, 0.08f, 0.70f);

    readonly Dictionary<Color, Texture2D> textures = new();

    /// <summary>
    /// 장면 배경 그림 — <c>Resources/StoryBackdrops/{장면id}.png</c>.
    /// <b>못 찾은 것도 캐시에 넣는다</b>(null 로) — 안 그러면 매 프레임
    /// <see cref="Resources.Load"/> 를 때린다.
    /// </summary>
    static readonly Dictionary<string, Texture2D> backdrops = new();

    string shownId = "";
    float shownAt;
    Texture2D leaving;      // 넘어가는 중인 앞 그림

    GUIStyle textStyle, nameStyle, titleStyle, hintStyle, narrateStyle, slotStyle;
    bool stylesReady;

    void Reset() => runner = GetComponent<DialogueRunner>();

    // ==================================================================
    //  장면 배경
    // ==================================================================
    /// <summary>
    /// ★★ 2026-10-01 유저: *"박물관 바깥은 어떻게 생겼어. 추석에 내려왔다가 철거 예정인
    /// 컨셉인데 <b>바깥에서 보는 환웅 박물관 그림</b>이라도 있어야 하는데. 거기에서
    /// 대화창 대화할 거고."* 맞다 — §3.6 이 «3D 배경 + 2D 초상화» 인데, 그 3D 배경이
    /// <b>중앙홀 하나뿐</b>이라 프롤로그도 중앙홀에서 시작한다. 그런데 프롤로그의 핵심은
    /// <b>«이런 데가 있었구나»</b> 라서, 이감이 이미 홀 안에 서 있으면 그 놀람이 성립하지 않는다.
    ///
    /// <b>장면마다 그림 한 장을 깔 수 있게 한다.</b>
    /// <c>Assets/Resources/StoryBackdrops/{장면id}.png</c> 를 떨구면 그 장면의 배경이 되고,
    /// 없으면 지금처럼 3D 로비가 배경이다 — <b>코드는 한 줄도 안 고친다.</b>
    /// 초상화·배경화면과 같은 방식이야(폴더에 넣기만 하면 임포트 설정까지 맞춰진다).
    ///
    /// 그림 파일 이름 = <see cref="StoryScript"/> 의 장면 id:
    /// <c>prologue · ch1 · ch2 · ch3 · ch4 · final_before · final_after · epilogue</c>
    /// </summary>
    /// <summary>장소가 바뀔 때 겹치는 시간. 짧으면 «툭», 길면 «흐리멍덩» 하다.</summary>
    const float Crossfade = 0.7f;

    static Texture2D Backdrop(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (backdrops.TryGetValue(id, out var cached)) return cached;
        var tex = Resources.Load<Texture2D>("StoryBackdrops/" + id);
        backdrops[id] = tex;
        return tex;
    }

    /// <summary>
    /// 배경 그림을 화면 가득. <b>ScaleAndCrop</b> — 창 비율이 그림과 달라도 여백 없이
    /// 채우고 넘치는 쪽을 자른다. 늘리면(StretchToFill) 건물이 홀쭉해진다.
    ///
    /// <b>0.45초에 걸쳐 스며든다.</b> 한 프레임에 «툭» 바뀌면 로딩 화면처럼 보이고,
    /// 스며들면 «장면이 열렸다» 가 된다(초상화 등장과 같은 판단).
    /// </summary>
    void DrawBackdrop(float w, float h, string id)
    {
        var tex = Backdrop(id);
        if (tex == null) return;

        if (shownId != id)
        {
            // ★★ 2026-10-02 — <b>앞 그림을 들고 있다가 겹쳐서 넘긴다.</b>
            //   전에는 새 그림만 알파 0 에서 올렸더니, 넘어가는 동안 <b>3D 로비가 비쳐</b>
            //   «장소가 바뀐다» 가 아니라 «그림이 잠깐 사라진다» 로 보였다.
            //   유저 요청: *"자연스러운 애니메이션처럼."* 겹치면 그게 된다.
            leaving = shownId == "" ? null : Backdrop(shownId);
            shownId = id;
            shownAt = Time.unscaledTime;
        }

        float a = Mathf.Clamp01((Time.unscaledTime - shownAt) / Crossfade);

        var keep = GUI.color;

        // 앞 그림이 밑에서 버틴다 — 새 그림이 다 올라올 때까지
        if (leaving != null && a < 1f)
        {
            GUI.color = new Color(1f, 1f, 1f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), leaving, ScaleMode.ScaleAndCrop);
        }
        else leaving = null;

        // 새 그림이 위에서 떠오른다. <b>살짝 당겨 들어온다</b> — 같은 자리에서 알파만 바뀌면
        // «사진이 바뀌었다» 지만, 조금 움직이면 «장면이 넘어간다» 가 된다.
        float zoom = Mathf.Lerp(1.04f, 1f, Mathf.SmoothStep(0f, 1f, a));
        float dw = w * zoom, dh = h * zoom;
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.DrawTexture(new Rect((w - dw) * 0.5f, (h - dh) * 0.5f, dw, dh), tex, ScaleMode.ScaleAndCrop);
        GUI.color = keep;
    }

    void Awake()
    {
        if (runner == null) runner = GetComponent<DialogueRunner>();
    }

    void Update()
    {
        if (!debugKeys || runner == null) return;

        var k = Keyboard.current;
        if (k == null) return;

        // 장면 골라 보기 — StoryScript.All 순서대로
        var digits = new[] { k.digit1Key, k.digit2Key, k.digit3Key, k.digit4Key, k.digit5Key };
        for (int i = 0; i < digits.Length && i < StoryScript.All.Length; i++)
        {
            if (!digits[i].wasPressedThisFrame) continue;

            // ★★ 2026-10-02 유저: *"1~5 로 장면 선택이 되더라, 전시실에 아무것도 없는 상태인데."*
            //   맞는 지적이다 — 수집품 0개인데 4장을 틀면 <b>«일곱 개 모았다» 는 대사</b>가 나오고,
            //   전시실·캠퍼스는 아무것도 안 바뀐 채다. 대사와 세계가 따로 논다.
            //
            //   <b>장면을 고르면 그 장의 수집 상태로 같이 맞춘다.</b> 그래야 이 키로 본 것이
            //   실제로 플레이해서 보는 것과 같아진다 — 안 맞으면 확인용으로 쓸 수가 없다.
            SetProgressFor(i);
            runner.Play(StoryScript.All[i].id);
        }

        // 수집 상태를 뒤집어서 "다 모으기 전 / 다 모은 후" 대사를 바로 비교해 볼 수 있게.
        // 전시실(F9·F10)과 같은 키라 헷갈리지 않는다.
        if (k.f9Key.wasPressedThisFrame)
        {
            var ids = new List<string>();
            foreach (var e in ExhibitCatalogue.All) ids.Add(e.id);
            CollectionState.CollectAll(ids);
            runner.Replay();
        }
        if (k.f10Key.wasPressedThisFrame)
        {
            CollectionState.ClearAll();
            runner.Replay();
        }
    }

    // ------------------------------------------------------------------

    Texture2D Tex(Color c)
    {
        if (textures.TryGetValue(c, out var hit) && hit != null) return hit;

        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        textures[c] = t;
        return t;
    }

    void Fill(Rect r, Color c) => GUI.DrawTexture(r, Tex(c));

    /// <summary>
    /// 화면 전체를 위에서 아래로 점점 진하게 덮는다 — 이야기가 흐르는 동안만.
    ///
    /// <b>띠를 겹쳐 그리지, 그라데이션 텍스처를 굽지 않는다.</b> 텍스처는 픽셀 y 0 이
    /// 아래인데 화면 y 0 은 위라, 이 프로젝트에서 위아래·좌우가 뒤집힌 게 <b>네 번</b>이다
    /// (현판 글씨 · 진열장 숫자 · 접수대 시계 · 화장실 칸 이름). 띠는 좌표가 곧 화면
    /// 좌표라 틀릴 수가 없고, 16장이면 IMGUI 드로우콜 16개라 값도 싸다.
    /// </summary>
    void DimBackdrop(float w, float h, float topA = 0.16f, float bottomA = 0.52f, int bands = 16)
    {
        // ★★ 2026-10-02 유저: *"화면에 지지직거림이 보인다. 대화할 때 더 자주."*
        //   원인은 Game 뷰 배율이 아니라 <b>이 띠들이었다.</b> 전에는 높이를
        //   <c>bh + 1f</c> 로 그려서 <b>이웃한 띠가 1 px 겹쳤고</b>, 겹친 줄은 알파가 두 번
        //   칠해져 어두운 가로줄이 열다섯 개 생긴다. 화면 크기가 조금만 달라져도 그 줄이
        //   다른 픽셀로 옮겨가니까 <b>지지직거리는 것처럼</b> 보인다.
        //
        //   띠 경계를 <b>픽셀에 맞춰 반올림</b>해서 딱 맞붙인다 — 겹치지도, 틈이 생기지도 않는다.
        for (int i = 0; i < bands; i++)
        {
            float t = (i + 0.5f) / bands;                 // 0 화면 위 → 1 화면 아래
            float a = Mathf.Lerp(topA, bottomA, t * t);   // 아래로 갈수록 빠르게 — 대화창 쪽이 제일 어둡다
            float y0 = Mathf.Round(h * i / bands);
            float y1 = Mathf.Round(h * (i + 1) / bands);
            Fill(new Rect(0f, y0, w, y1 - y0), new Color(0.04f, 0.05f, 0.09f, a));
        }
    }

    /// <summary>
    /// 초상화가 설 자리. <b>초상화와 이름표가 같은 값을 봐야</b> 이름표가 안 겹친다 —
    /// 같은 숫자를 두 군데서 계산하면 반드시 어긋난다.
    ///
    /// 화면이 납작하면(에디터 Game 뷰가 대개 그렇다) 머리가 <b>화면 천장에 붙는다.</b>
    /// 측정: 1180 × 420 에서 초상화 위 여백이 <b>2px</b> 이었다. 대사창 위에 남은
    /// 높이 안으로 묶어서, 어떤 창 크기에서도 머리 위가 14px 은 비게 한다.
    /// </summary>
    Rect PortraitRect(Rect box, float w)
    {
        float pw = Mathf.Clamp(w * 0.17f, 132f, 224f);
        float ph = pw * 1.22f;

        float room = box.y - 4f;      // r.y = box.y - ph + 10 이 14 밑으로 안 내려가게
        if (ph > room) { ph = Mathf.Max(96f, room); pw = ph / 1.22f; }

        return new Rect(box.x + 26f, box.y - ph + 10f, pw, ph);
    }

    /// <summary>테두리만 있는 네모. 안쪽은 안 칠한다.</summary>
    void Outline(Rect r, Color c, float thickness = 2f)
    {
        Fill(new Rect(r.x, r.y, r.width, thickness), c);
        Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
        Fill(new Rect(r.x, r.y, thickness, r.height), c);
        Fill(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
    }

    void BuildStyles()
    {
        var font = HudFont.Resolve(uiFont);

        textStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 20, wordWrap = true, richText = true }, font);
        textStyle.normal.textColor = new Color(0.97f, 0.96f, 0.92f);

        narrateStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 19, wordWrap = true, richText = true, alignment = TextAnchor.MiddleCenter }, font);
        narrateStyle.normal.textColor = new Color(0.86f, 0.85f, 0.79f);

        nameStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }, font);
        nameStyle.normal.textColor = Color.white;

        titleStyle = HudFont.With(new GUIStyle(GUI.skin.label) { fontSize = 14 }, font);
        titleStyle.normal.textColor = new Color(1f, 1f, 1f, 0.75f);

        hintStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 13, alignment = TextAnchor.MiddleRight }, font);
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.45f);

        slotStyle = HudFont.With(new GUIStyle(GUI.skin.label)
        { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = false }, font);

        stylesReady = true;
    }

    /// <summary>
    /// 그 장이 <b>막 시작한 상태</b>로 수집 기록을 맞춘다.
    /// 장 번호는 <c>(수집품 + 1) / 2</c> 라, 1장은 1개 · 2장은 3개 · 3장은 5개 · 4장은 7개다.
    /// (프롤로그는 0개.) 8개로 맞추면 결승이 열려 버려서 4장을 7개로 둔다.
    /// </summary>
    static void SetProgressFor(int sceneIndex)
    {
        int want = sceneIndex <= 0 ? 0 : Mathf.Min(sceneIndex * 2 - 1, ExhibitCatalogue.Count);

        CollectionState.ClearAll();
        for (int i = 0; i < want; i++) CollectionState.Collect(ExhibitCatalogue.All[i].id);

        Debug.Log($"[대화] 장면을 고르면서 수집품을 {want}/{ExhibitCatalogue.Count} 로 맞췄다 — " +
                  $"전시실·캠퍼스도 그 상태로 바뀐다.");
    }

    void OnGUI()
    {
        if (runner == null) return;
        if (!stylesReady) BuildStyles();

        float w = Screen.width, h = Screen.height;

        var line = runner.Current;
        bool hasLine = !string.IsNullOrEmpty(line.text);

        // ★ 배경을 눌러야 인물이 보인다(2026-09-28). 유저: *"세진이 뒤에 무슨 네모칸 배경이나
        // 뭐 필요하지 않아? 비어 보이는데."* — <b>네모칸은 정답이 아니다.</b> 인물 뒤에 판을
        // 깔면 «배경 위에 붙인 스티커» 가 되고, 3D 로비를 배경으로 쓰는 이유(§3.6)가 사라진다.
        //
        // 비어 보인 진짜 원인은 <b>배경과 인물이 같은 밝기</b>인 것이다 — 크림색 홀 앞에
        // 크림색 인물이 서 있으니 서로 묻힌다. 비주얼 노벨이 쓰는 방법은 판을 까는 게 아니라
        // <b>배경을 어둡게 누르는 것</b>이고, 그러면 같은 그림이 앞으로 걸어 나온다.
        // 배경 그림이 있으면 먼저 깐다 — 3D 로비를 덮는다.
        // <b>대사가 없는 프레임에도 그린다</b>: 장면이 끝나는 순간 그림만 사라지고
        // 홀이 한 프레임 번쩍이면 «깨진 것» 으로 보인다.
        // ★ 장면 id 가 아니라 <b>지금 장소</b>를 본다 — 한 장면 안에서 바깥 → 안으로
        //   옮겨갈 수 있다(<see cref="DialogueLine.At"/>).
        if (runner.IsPlaying) DrawBackdrop(w, h, runner.Place);

        if (hasLine) DimBackdrop(w, h);

        DrawSceneTitle(w);
        if (!hasLine) return;

        float margin = Mathf.Max(24f, w * 0.045f);
        float boxH = Mathf.Clamp(h * 0.25f, 146f, 200f);
        var box = new Rect(margin, h - boxH - margin * 0.7f, w - margin * 2f, boxH);

        if (!line.IsNarration) DrawPortrait(box, line, w);

        // 대화창 — 남색 판에 크림색 테두리
        Fill(box, BoxColor);
        Outline(box, FrameColor);

        if (!line.IsNarration) DrawNamePlate(box, line, w);
        DrawText(box, line);
        DrawFooter(box);
    }

    void DrawSceneTitle(float w)
    {
        string title = runner.SceneTitle;
        if (string.IsNullOrEmpty(title)) return;

        // ★ 2026-09-28 <b>오른쪽 위로 옮겼다.</b> 유저: *"세진이가 이렇게 들어가면 뒤에
        // 철거 뭐시기 글자가 가려지는데."* 초상화는 언제나 <b>왼쪽</b>에 서니까, 겹치지 않는
        // 자리는 반대쪽 모서리 하나뿐이다. 판을 깔아 가리는 게 아니라 <b>비켜 놓는 것</b>이
        // 답이야 — 게임이 인물과 UI 를 같은 모서리에 두지 않는 게 그래서다.
        //
        // 칸 폭도 <b>글자 길이에서</b> 뽑는다. 360 고정이면 짧은 제목 옆에 빈 판이 붙어서
        // 그 자체가 «뭔가 가려진 자리» 로 보인다.
        float tw = Mathf.Min(w * 0.44f, titleStyle.CalcSize(new GUIContent(title)).x + 36f);
        var r = new Rect(w - tw - 20f, 18f, tw, 30f);
        Fill(r, TitleColor);
        GUI.Label(r, title, new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleCenter });
    }

    /// <summary>초상화. 그림이 있으면 그리고, 없으면 그 사람 색의 자리표시 네모.</summary>
    string portraitKey = "";
    float portraitAt = -99f;
    float portraitRise = 24f, portraitSpan = 0.20f;

    void DrawPortrait(Rect box, DialogueLine line, float w)
    {
        var r = PortraitRect(box, w);

        // ★ <b>아래에서 올라오며 스며든다</b>(2026-09-28). 유저: *"네모칸 말고 자연스럽게
        // 보이면서 하는, 보통의 게임사들이 이런 일러스트 띄울 때 하는 방법."*
        //
        // 그 방법은 판을 까는 게 아니라 <b>등장을 보여주는 것</b>이다. 한 프레임에 «툭»
        // 나타난 그림은 화면에 붙인 스티커지만, 0.2초 동안 <b>올라오면서 진해지면</b>
        // 그 자리에 선 사람이 된다. 말하는 사람이 바뀔 때마다 다시 논다 —
        // 그래서 «누가 말하는지» 가 이름표를 안 읽어도 눈에 들어온다.
        // ★ <b>사람이 바뀔 때와 표정만 바뀔 때는 세기가 달라야 한다</b>(2026-09-28).
        // 유저가 «그냥 매번 하는 게 좋은데 정신 사납지 않겠냐» 고 물어서 내가 정했다 —
        // <b>둘 다 논다. 대신 크기를 나눈다.</b>
        //
        // 표정만 바뀌는데 24px 을 올라오면 «그 사람이 나갔다가 다시 들어온» 것으로 보인다.
        // 반대로 아무 것도 안 하면 당황·기쁨으로 넘어가는 순간이 <b>그림 교체</b>로만 보인다.
        // 그래서 사람이 바뀌면 <b>등장</b>(24px · 0.20초), 표정만 바뀌면 <b>반응</b>(7px · 0.13초).
        // 상용 게임이 하는 구분이 이거다 — 같은 동작을 «얼마나» 하느냐로 뜻이 갈린다.
        string key = line.speakerId + "|" + line.mood;
        if (key != portraitKey)
        {
            bool sameSpeaker = portraitKey.StartsWith(line.speakerId + "|");
            portraitRise = sameSpeaker ? 7f : 24f;
            portraitSpan = sameSpeaker ? 0.13f : 0.20f;
            portraitKey = key;
            portraitAt = Time.unscaledTime;
        }

        float rise = Mathf.Clamp01((Time.unscaledTime - portraitAt) / portraitSpan);
        rise = 1f - (1f - rise) * (1f - rise);      // 끝에서 부드럽게 멎는다
        r.y += (1f - rise) * portraitRise;

        var keepColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, rise);

        // ★ 인물 뒤에 깔던 «옅은 어둠 기둥» 은 걷어냈다(2026-10-01).
        //   유저: *"뭔가 사각형에 막혀 있는 느낌."* 맞다 — 각진 사각형을 아홉 장 겹친 거라
        //   «스며 나간다» 가 아니라 <b>제일 바깥 테두리가 직선으로 남았다.</b>
        //   뒤가 밋밋한 크림색 벽이면 3% 짜리 그림자도 선으로 읽힌다.
        //
        //   그리고 이제 <b>필요가 없다.</b> 이걸 깐 이유가 «크림색 홀 앞에 크림색 인물이라
        //   서로 묻힌다» 였는데, 초상화마다 <b>인물 색 테두리</b>가 들어가면서 그 일을
        //   테두리가 한다. 기능을 하나 더하면 <b>앞서 내린 결정의 이유가 없어졌는지</b>
        //   확인해야 한다 — 접수대 시계를 되돌릴 때 배운 것과 같은 자리야.

        var tex = Cast.Portrait(line.speakerId, line.mood);
        if (tex != null)
        {
            // 그림 비율을 지키면서 이 칸 안에 맞춘다 — 세로로 긴 그림이든 정사각이든 안 찌그러진다
            float scale = Mathf.Min(r.width / tex.width, r.height / tex.height);
            float dw = tex.width * scale, dh = tex.height * scale;
            GUI.DrawTexture(new Rect(r.center.x - dw * 0.5f, r.yMax - dh, dw, dh), tex);
        }
        else
        {
            var c = Cast.ColorOf(line.speakerId);
            Fill(r, new Color(c.r, c.g, c.b, 0.20f));
            Outline(r, new Color(c.r, c.g, c.b, 0.85f));

            var s = new GUIStyle(slotStyle);
            s.normal.textColor = new Color(1f, 1f, 1f, 0.82f);
            // 꺾쇠(< >)는 쓰지 않는다 — 리치 텍스트 태그로 먹혀서 글자가 사라질 수 있다
            GUI.Label(new Rect(r.x + 8, r.center.y - 34, r.width - 16, 68),
                      $"{Cast.NameOf(line.speakerId)}\n{line.mood} 표정\n\n초상화 자리", s);
        }

        GUI.color = keepColor;
    }

    void DrawNamePlate(Rect box, DialogueLine line, float w)
    {
        string name = Cast.NameOf(line.speakerId);
        if (string.IsNullOrEmpty(name)) return;

        // 초상화 폭을 여기서 다시 계산하면 좁아진 화면에서 이름표가 인물 위로 겹친다.
        float pw = PortraitRect(box, w).width;
        float plateW = Mathf.Max(104f, nameStyle.CalcSize(new GUIContent(name)).x + 36f);
        var plate = new Rect(box.x + 26f + pw + 14f, box.y - 34f, plateW, 34f);

        var plateColor = Cast.ColorOf(line.speakerId);
        Fill(plate, plateColor);

        // 금색처럼 밝은 이름표 위에서는 흰 글자가 사라진다. 바탕 밝기를 보고 고른다.
        var style = new GUIStyle(nameStyle);
        style.normal.textColor = Cast.TextOn(plateColor);
        GUI.Label(plate, name, style);
    }

    void DrawText(Rect box, DialogueLine line)
    {
        string full = line.text;
        int shown = runner.RevealedCount;

        // 안 나온 글자를 지우지 않고 **투명하게** 칠한다.
        // 지워버리면 글자가 늘 때마다 줄바꿈 위치가 튀어서 글이 덜컹거린다.
        string body = shown >= full.Length
            ? full
            : full.Substring(0, shown) + "<color=#00000000>" + full.Substring(shown) + "</color>";

        var inner = new Rect(box.x + 30f, box.y + 24f, box.width - 60f, box.height - 54f);
        GUI.Label(inner, body, line.IsNarration ? narrateStyle : textStyle);
    }

    void DrawFooter(Rect box)
    {
        var foot = new Rect(box.x + 20f, box.yMax - 28f, box.width - 40f, 22f);

        // 왼쪽 — 몇 번째 줄인지
        GUI.Label(foot, $"{runner.LineNumber} / {runner.LineCount}",
                  new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleLeft });

        // 오른쪽 — 다음으로 넘어가는 법
        string hint;
        if (runner.Finished) hint = runner.allowReplay ? "장면 끝  ·  R 다시 보기" : "장면 끝";
        else if (runner.LineFullyShown) hint = "SPACE · 클릭   다음  ▼";
        else hint = "SPACE   한꺼번에 보기";

        GUI.Label(foot, hint, hintStyle);

        if (debugKeys)
            GUI.Label(new Rect(box.x, box.yMax + 6f, box.width, 20f),
                      "1~5 장면 고르기 (수집 기록도 그 장에 맞춘다)     " +
                      "F9 전시품 전부 수집 / F10 전부 지우기",
                      new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
    }
}
