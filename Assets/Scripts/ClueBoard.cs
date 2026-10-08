using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ★★ <b>다가가서 E 를 누르면 읽는 물건.</b> 2026-10-08.
///
/// 캠퍼스에 이야기 소품이 서른일곱 점 서 있는데 <b>전부 말이 없었다</b> — 기록첩 하나만
/// 읽혔다. 에셋을 열 개 더 만들면 <b>읽을 수 없는 게 마흔일곱 개</b>가 될 뿐이라,
/// 새 모델 0개로 지금 있는 것부터 읽히게 한다.
///
/// <list type="bullet">
/// <item><b>글은 <see cref="ClueText"/> 한 파일에만.</b> 수연이 고칠 자리가 하나다 —
///       <c>BearLines</c> · <c>StoryScript</c> 와 같은 꼴</item>
/// <item><b>겨루기는 <see cref="Reach"/> 점수로.</b> 문·수도꼭지·곰과 같은 자로 재야
///       「왜 이게 잡혔지」가 안 생긴다. 종류에 순서를 박으면 또 가려진다(2026-09-22)</item>
/// <item><b>제일 가까운 하나는 한 프레임 늦게 공개한다.</b> 같은 프레임에 채우고 읽으면
///       실행 순서에 따라 반쪽을 읽는다(2026-09-21 에 넷을 한꺼번에 고친 그 함정)</item>
/// <item><b>읽은 것은 남는다</b>(PlayerPrefs). 나중에 앨범이 이 값을 그대로 쓴다</item>
/// </list>
///
/// ★ 몸은 <b>스스로 찾는다.</b> 빌더가 꽂아 주지 않아도 되고, 씬을 다시 안 구워도 된다 —
///   「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 다섯 번 겪은 뒤의 기본형.
/// </summary>
public class ClueBoard : MonoBehaviour
{
    [Tooltip("ClueText 의 id 와 같아야 한다")]
    public string id;

    [Tooltip("이 거리 안에 들어와야 읽을 수 있다")]
    public float range = 2.6f;

    [Tooltip("걸어다니는 몸. 비워두면 스스로 찾는다")]
    public Transform visitor;

    // ── 제일 가까운 하나 ──────────────────────────────────────────────────────
    public static ClueBoard Nearest { get; private set; }
    public static float NearestScore { get; private set; } = float.MaxValue;

    static int frameStamp = -1;
    static ClueBoard pending;
    static float pendingScore = float.MaxValue;

    // ── 펼친 카드 ─────────────────────────────────────────────────────────────
    public static ClueBoard Shown { get; private set; }

    /// <summary>카드가 떠 있나. <see cref="CampusHUD"/> 가 이걸 보고 제 안내를 접는다.</summary>
    public static bool Open => Shown != null;

    /// <summary>읽었다는 표시. 앨범이 나중에 같은 키를 읽는다.</summary>
    static string Key(string id) => "단서_" + id;

    public static bool WasRead(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

    /// <summary>몇 개나 읽었나. 화면 구석에 «n / 10» 으로 띄운다.</summary>
    public static int ReadCount
    {
        get
        {
            int n = 0;
            foreach (var note in ClueText.All) if (WasRead(note.id)) n++;
            return n;
        }
    }

    /// <summary>F10(처음부터)에서 같이 지운다 — 수집 기록만 비우고 단서가 남으면 상태가 어긋난다.</summary>
    public static void ClearAll()
    {
        foreach (var note in ClueText.All) PlayerPrefs.DeleteKey(Key(note.id));
        PlayerPrefs.Save();
    }

    /// <summary>화면에 띄울 말. 읽은 것과 안 읽은 것을 가른다 — 다 돌았는지 알 수 있어야 한다.</summary>
    public string Action => WasRead(id) ? "다시 읽기" : "읽기";

    public void Read()
    {
        Shown = this;
        Stamp();
        if (!WasRead(id))
        {
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
            Sfx.Play("Toast", 0.7f, duckMusic: false);
        }
    }

    public static void Close() => Shown = null;

    // ── 겨루기 ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            Nearest = pending;                 // 지난 프레임에 다 끝난 결과를 이제 공개한다
            NearestScore = pending != null ? pendingScore : float.MaxValue;
            pending = null;
            pendingScore = float.MaxValue;
        }

        if (Open) return;                      // 읽는 중에는 다른 단서를 안 겨룬다

        Transform who = Who();
        if (who == null) return;

        // 거리는 <b>제일 가까운 면</b>, 각도는 <b>한가운데</b>. 바닥에 놓인 납작한 물건이라
        // 한가운데로 거리를 재면 발밑에 두고도 «멀다» 가 나온다.
        Vector3 center = transform.position;
        Vector3 near = center;
        var r = GetComponentInChildren<Renderer>();
        if (r != null) { center = r.bounds.center; near = r.bounds.ClosestPoint(who.position); }

        if (!Reach.Score(who, near, center, range, out float d)) return;
        if (d >= pendingScore) return;

        pendingScore = d;
        pending = this;
    }

    Transform body;

    Transform Who()
    {
        if (visitor != null)
            return visitor.gameObject.activeInHierarchy ? visitor : null;

        if (body == null)
        {
            var fp = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Exclude);
            if (fp != null) body = fp.transform;
        }
        if (body != null && body.gameObject.activeInHierarchy) return body;

        return Camera.main != null ? Camera.main.transform : null;
    }

    // ── 카드 ──────────────────────────────────────────────────────────────────
    //  ★ <b>자기 카드는 자기가 그린다.</b> CampusHUD 에 넣으면 그 컴포넌트가 없는 씬
    //    (전시실·로비)에서는 단서를 못 읽는다. 그리고 「큰 패널은 한 번에 한 장」이라
    //    카드가 떠 있는 동안 CampusHUD 는 통째로 접힌다(CampusHUD.OnGUI 참고).

    [Tooltip("카드 글꼴. 비워두면 HUD 기본")]
    public Font uiFont;

    void OnGUI()
    {
        if (Shown != this) return;

        Rect screen = Hud.Begin(uiFont);
        float w = screen.width, h = screen.height;

        var note = ClueText.Find(id);
        string title = note?.title ?? "읽을 수 없다";
        string[] lines = note?.lines ?? new[] { "글씨가 바래서 알아볼 수 없다." };

        // 줄 수에 따라 높이가 자란다. 세 줄까지가 설계고, 그 이상이면 알아서 늘어난다.
        float tall = 132f + lines.Length * 30f;
        var box = new Rect(w * 0.5f - 270f, h * 0.5f - tall * 0.5f, 540f, tall);
        Hud.Panel(box);
        Rect inner = Hud.Inner(box);

        GUI.Label(new Rect(inner.x, inner.y + 12f, inner.width, 30f),
                  title, Hud.Resize(Hud.Title, 24, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 18f, inner.y + 50f, inner.width - 36f);

        var body = Hud.Resize(Hud.Label, 16, TextAnchor.UpperLeft);
        body.wordWrap = true;
        for (int i = 0; i < lines.Length; i++)
            GUI.Label(new Rect(inner.x + 24f, inner.y + 64f + i * 30f, inner.width - 48f, 28f),
                      lines[i], body);

        GUI.Label(new Rect(inner.x, inner.yMax - 46f, inner.width, 20f),
                  $"읽은 단서  {ReadCount} / {ClueText.Count}",
                  Hud.Resize(Hud.Text, 13, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.yMax - 24f, inner.width, 20f),
                  "아무 키나  닫기", Hud.Resize(Hud.Text, 14, TextAnchor.UpperCenter));

        Hud.End();
    }

    void LateUpdate()
    {
        if (Shown != this) return;

        // ★ 연 프레임의 E 가 그대로 넘어와 바로 닫히지 않게 — 카드·브리핑과 같은 처리
        if (Time.unscaledTime - openedAt < 0.25f) return;

        var k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) Close();
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Close();
    }

    float openedAt;
    void OnEnable() => openedAt = Time.unscaledTime;

    /// <summary>카드를 연 시각을 다시 찍는다. <see cref="Read"/> 를 부르는 쪽이 같이 부른다.</summary>
    public void Stamp() => openedAt = Time.unscaledTime;
}
