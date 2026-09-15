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

    GUIStyle textStyle, nameStyle, titleStyle, hintStyle, narrateStyle, slotStyle;
    bool stylesReady;

    void Reset() => runner = GetComponent<DialogueRunner>();

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
            if (digits[i].wasPressedThisFrame) runner.Play(StoryScript.All[i].id);

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

    void OnGUI()
    {
        if (runner == null) return;
        if (!stylesReady) BuildStyles();

        float w = Screen.width, h = Screen.height;

        DrawSceneTitle(w);

        var line = runner.Current;
        if (string.IsNullOrEmpty(line.text)) return;

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

        var r = new Rect(20, 18, Mathf.Min(360f, w * 0.4f), 30);
        Fill(r, TitleColor);
        GUI.Label(new Rect(r.x + 14, r.y, r.width - 20, r.height),
                  title, new GUIStyle(titleStyle) { alignment = TextAnchor.MiddleLeft });
    }

    /// <summary>초상화. 그림이 있으면 그리고, 없으면 그 사람 색의 자리표시 네모.</summary>
    void DrawPortrait(Rect box, DialogueLine line, float w)
    {
        float pw = Mathf.Clamp(w * 0.17f, 132f, 224f);
        float ph = pw * 1.22f;
        var r = new Rect(box.x + 26f, box.y - ph + 10f, pw, ph);

        var tex = Cast.Portrait(line.speakerId, line.mood);
        if (tex != null)
        {
            // 그림 비율을 지키면서 이 칸 안에 맞춘다 — 세로로 긴 그림이든 정사각이든 안 찌그러진다
            float scale = Mathf.Min(r.width / tex.width, r.height / tex.height);
            float dw = tex.width * scale, dh = tex.height * scale;
            GUI.DrawTexture(new Rect(r.center.x - dw * 0.5f, r.yMax - dh, dw, dh), tex);
            return;
        }

        var c = Cast.ColorOf(line.speakerId);
        Fill(r, new Color(c.r, c.g, c.b, 0.20f));
        Outline(r, new Color(c.r, c.g, c.b, 0.85f));

        var s = new GUIStyle(slotStyle);
        s.normal.textColor = new Color(1f, 1f, 1f, 0.82f);
        // 꺾쇠(< >)는 쓰지 않는다 — 리치 텍스트 태그로 먹혀서 글자가 사라질 수 있다
        GUI.Label(new Rect(r.x + 8, r.center.y - 34, r.width - 16, 68),
                  $"{Cast.NameOf(line.speakerId)}\n{line.mood} 표정\n\n초상화 자리", s);
    }

    void DrawNamePlate(Rect box, DialogueLine line, float w)
    {
        string name = Cast.NameOf(line.speakerId);
        if (string.IsNullOrEmpty(name)) return;

        float pw = Mathf.Clamp(w * 0.17f, 132f, 224f);
        float plateW = Mathf.Max(104f, nameStyle.CalcSize(new GUIContent(name)).x + 36f);
        var plate = new Rect(box.x + 26f + pw + 14f, box.y - 34f, plateW, 34f);

        Fill(plate, Cast.ColorOf(line.speakerId));
        GUI.Label(plate, name, nameStyle);
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
                      "1~5 장면 고르기     F9 전시품 전부 수집 / F10 전부 지우기 — 대사가 어떻게 갈리는지 비교용",
                      new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
    }
}
