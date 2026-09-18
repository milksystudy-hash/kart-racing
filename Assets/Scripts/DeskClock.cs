using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// <b>접수대 위 전자시계.</b> 2026-09-18 유저: *"로비씬 이 책상이 아무것도 없으니 허전한데
/// 한옥 곰박물관 스타일에 전자시계 하나 만들 수 있니? 시간은 사용자의 컴퓨터 시계,
/// 날씨 정보에 맞춰 알려주는."*
///
/// 박물관 접수대에 시계가 있는 건 당연하고, <b>진짜 시간이 뜨면 그 방이 살아 있는 것처럼 보인다</b> —
/// 로비는 이야기 사이사이 계속 돌아오는 방이라 그 효과가 크다.
///
/// 한옥 결을 잃지 않으려고 «검은 디지털 시계» 가 아니라 <b>나무 갑 + 한지 창 + 작은 처마</b>로 짰다.
/// 숫자는 <see cref="GallerySceneBuilder"/> 의 진열장 번호와 같은 <b>막대 일곱 개(7세그먼트)</b> —
/// 새로 넣는 에셋이 0개고, 켜고 끄기만 하면 되니까 매초 갱신이 공짜다.
///
/// <b>기하는 Awake 에 짓는다.</b> 로비는 구워서 저장하는 씬이라 빌더가 여기까지 구워 두면
/// «두 벌» 함정(2026-09-17)이 딱 생기는 자리야. 빌더는 빈 오브젝트와 이 컴포넌트만 놓고,
/// 실제 모양은 실행할 때 만든다 — 트랙의 발판·지붕과 같은 방식이다.
/// <b>그래서 에디터 씬 뷰에는 안 보이고 ▶ 를 눌러야 보인다. 그게 맞는 동작이야.</b>
/// </summary>
public class DeskClock : MonoBehaviour
{
    [Header("크기")]
    // 2026-09-18 유저: *"시계 크기도 너무 작아."* 접수대가 5.2m 라 0.44m 짜리는 점으로 보인다.
    // 치수를 하나하나 고치지 않고 <b>통째로 키운다</b> — 비례가 안 깨지고 되돌리기도 쉽다.
    [Tooltip("1 이면 가로 0.44m. 접수대에 놓고 궤도 카메라에서 읽히려면 2 이상")]
    public float scale = 2.4f;

    [Header("돋보기")]
    [Tooltip("걷기 모드로 이만큼 다가가면 E 로 열 수 있다")]
    public float range = 3.2f;

    [Header("날씨")]
    [Tooltip("끄면 날짜만 뜬다. 켜면 인터넷에서 날씨를 한 번 받아온다")]
    public bool useOnlineWeather = true;

    [Tooltip("비워두면 접속 위치로 알아서 잡는다. 'Seoul' 처럼 적으면 그 도시")]
    public string city = "";

    [Tooltip("몇 분마다 다시 받아올지")]
    public float refreshMinutes = 20f;

    // ── 색 ────────────────────────────────────────────────────────────
    static readonly Color Wood   = new Color32(0x6B, 0x4A, 0x32, 0xFF);   // 갑
    static readonly Color Trim   = new Color32(0x8A, 0x5E, 0x3C, 0xFF);   // 테두리
    static readonly Color Tile   = new Color32(0x3E, 0x6B, 0x63, 0xFF);   // 처마 청기와
    static readonly Color Paper  = new Color32(0xF2, 0xE6, 0xCC, 0xFF);   // 한지 창
    static readonly Color Glow   = new Color32(0xFF, 0xC9, 0x6B, 0xFF);   // 숫자 — 발광
    static readonly Color Dim    = new Color32(0x5A, 0x4A, 0x36, 0xFF);   // 꺼진 획

    // ── 치수 ──────────────────────────────────────────────────────────
    const float BarLong = 0.052f, BarThin = 0.014f, BarDeep = 0.008f;
    const float DigitW = 0.070f;     // 숫자 하나가 차지하는 가로
    // ★ <b>정면은 +Z 다.</b> 처음에 −0.082 로 붙였다가 숫자판이 <b>케이스 뒤쪽</b>에 달렸고,
    // 보는 사람은 그 뒤통수를 통해 본 꼴이라 <b>시각이 거울상</b>으로 나왔다
    // (2026-09-18 유저: *"시계가 망가졌나봐"* — 17:13 이 뒤집혀 보였다).
    // 케이스가 z −0.075~+0.075 니까 0.082 면 그 앞에 1mm 띄워 붙는다.
    const float PanelZ = 0.082f;     // 한지 창 앞면
    const float GlyphZ = PanelZ + 0.006f;

    GameObject[,] segments;          // [자리 4][획 7]
    GameObject[] colonDots;
    TextMesh strip;
    string weather = "";
    int shownMinute = -1;

    void Awake()
    {
        Build();
        Refresh(force: true);
        if (useOnlineWeather) StartCoroutine(WeatherLoop());
    }

    void Update()
    {
        // 분이 바뀔 때만 갱신한다. 매 프레임 재질을 28번씩 갈 이유가 없다.
        if (System.DateTime.Now.Minute != shownMinute) Refresh(force: false);

        TickInteraction();
    }

    // ══════════════════════════════════════════════════ 돋보기 (E)
    //
    // 2026-09-18 유저: *"상호작용으로 돋보기 기능 넣어서 거기에 도시 입력하고 볼 수 없을까."*
    // 시계에 다가가서 <b>E</b> 를 누르면 큰 글씨로 시간·날짜·날씨가 뜨고 도시를 적을 수 있다.
    // 접수대 위 물건은 작아서 궤도 카메라로는 못 읽는데, <b>다가가서 들여다보는 동작</b>이
    // 그걸 자연스럽게 풀어준다 — 박물관에서 안내판을 들여다보는 것과 같아.

    /// <summary>제일 가까운 시계 하나에만 표시가 뜬다 — 곰·문과 같은 방식.</summary>
    public static DeskClock Nearest { get; private set; }

    static int frameStamp = -1;
    static float nearestDistance;

    public string Action => "시계 보기";

    FirstPersonController walker;
    bool open;
    string typed = "";

    void TickInteraction()
    {
        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            nearestDistance = float.MaxValue;
            Nearest = null;
        }

        // <b>걷는 몸이 있을 때만 잡힌다.</b> 둘러보기(궤도 카메라)에서는 다가간다는 게
        // 없으니 문·곰과 같은 규칙으로 둔다 — 걸어가서 들여다보는 물건이야.
        if (walker == null) walker = FindFirstObjectByType<FirstPersonController>();
        if (walker != null && walker.gameObject.activeInHierarchy)
        {
            float d = Vector3.Distance(walker.transform.position, transform.position);
            if (d <= range && d < nearestDistance) { nearestDistance = d; Nearest = this; }
        }

        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k == null) return;

        if (open)
        {
            // 도시를 적는 중에는 <b>E 가 글자다.</b> 닫는 건 ESC 로만 — 안 그러면
            // "Seoul" 을 치다가 창이 닫힌다.
            if (k.escapeKey.wasPressedThisFrame) Close();
            else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame) Apply();
        }
        else if (Nearest == this && k.eKey.wasPressedThisFrame)
        {
            open = true;
            typed = city;
            GUI.FocusControl(null);
        }
    }

    void Close() => open = false;

    void Apply()
    {
        city = typed.Trim();
        weather = "";
        if (useOnlineWeather) StartCoroutine(Fetch());
        open = false;
    }

    void OnGUI()
    {
        if (!open) return;

        var screen = Hud.Begin(null);
        var panel = new Rect(screen.width * 0.5f - 190f, screen.height * 0.5f - 96f, 380f, 192f);
        Hud.Panel(panel);

        var now = System.DateTime.Now;
        string[] days = { "일", "월", "화", "수", "목", "금", "토" };

        GUI.Label(new Rect(panel.x, panel.y + 44f, panel.width, 44f),
                  now.ToString("HH:mm"), Hud.Resize(Hud.Value, 38, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(panel.x, panel.y + 86f, panel.width, 20f),
                  $"{now.Year}년 {now.Month}월 {now.Day}일 ({days[(int)now.DayOfWeek]})",
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(panel.x, panel.y + 106f, panel.width, 20f),
                  string.IsNullOrEmpty(weather) ? "날씨 정보 없음" : weather,
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));

        var inner = Hud.Inner(panel);
        GUI.Label(new Rect(inner.x, panel.y + 132f, 52f, 22f), "도시",
                  Hud.Resize(Hud.Label, 12, TextAnchor.MiddleLeft));
        typed = GUI.TextField(new Rect(inner.x + 54f, panel.y + 132f, inner.width - 54f, 22f),
                              typed ?? "", 24);

        GUI.Label(new Rect(panel.x, panel.y + 158f, panel.width, 18f),
                  "ENTER 적용 · ESC 닫기 · 비우면 현재 위치",
                  Hud.Resize(Hud.Label, 11, TextAnchor.MiddleCenter));

        Hud.End();
    }

    // ══════════════════════════════════════════════════════════ 모양
    void Build()
    {
        // 갑 — 뒤로 살짝 기울여 세운다. 접수대에 놓인 탁상시계는 정면으로 안 서 있다.
        transform.localRotation *= Quaternion.Euler(-8f, 0f, 0f);
        transform.localScale = Vector3.one * Mathf.Max(0.2f, scale);

        // ★ 폭을 <b>숫자가 차지하는 자리에서 거꾸로</b> 잡는다. 전에는 케이스 0.44 에
        // 숫자를 ±0.183 까지 늘어놓고 창틀을 ±0.177 에 세워서 <b>창틀이 바깥 두 자리를
        // 깎아먹었다</b> — 유저가 «숫자가 이상하다» 고 한 게 이거야(2026-09-18).
        // 숫자 바깥 끝 0.185 < 창틀 안쪽 0.218 < 케이스 끝 0.25. 이제 아무 데도 안 닿는다.
        Box("Case",     new Vector3(0f, 0.095f, 0f),      new Vector3(0.50f, 0.19f, 0.15f), Wood);
        Box("Base",     new Vector3(0f, 0.012f, 0.012f),  new Vector3(0.56f, 0.024f, 0.19f), Trim);
        Box("Panel",    new Vector3(0f, 0.105f, PanelZ),  new Vector3(0.42f, 0.115f, 0.012f), Paper,
            Finish.발광);

        // 창틀 — 한지 창은 살이 있어야 창으로 읽힌다(로비 살창과 같은 이유).
        // <b>숫자 바깥</b>에 세운다. 안쪽에 세우면 그게 획처럼 보여서 숫자가 깨져 보인다.
        for (int s = -1; s <= 1; s += 2)
            Box($"Mullion_{s}", new Vector3(s * 0.225f, 0.105f, PanelZ + 0.002f),
                new Vector3(0.014f, 0.135f, 0.016f), Trim);

        // 작은 처마 — 이게 있어야 «디지털 시계» 가 아니라 «한옥 물건» 이 된다.
        // 처마는 <b>정면(+Z)으로</b> 나와야 창을 덮는다.
        Box("Eave",     new Vector3(0f, 0.198f, 0.012f),  new Vector3(0.58f, 0.018f, 0.20f), Tile);
        Box("EaveLip",  new Vector3(0f, 0.208f, 0.104f),  new Vector3(0.58f, 0.030f, 0.022f), Tile);
        Box("Ridge",    new Vector3(0f, 0.216f, -0.010f), new Vector3(0.34f, 0.016f, 0.05f), Tile);

        // ── 숫자 네 자리 ──────────────────────────────────────────────
        // ★ <b>−X 가 보는 사람의 오른쪽</b>이다. 시계는 +Z 로 서 있고 보는 사람은 +Z 쪽에 있어서,
        //   카메라의 오른쪽 벡터가 −X 가 된다. 그냥 +X 순서로 늘어놓으면 «12:34» 가 «43:21» 로
        //   거울상이 된다 — 진열장 숫자가 뒤집혔던 것과 <b>똑같은 병</b>이야(2026-09-17).
        segments = new GameObject[4, 7];
        // 숫자 한 자가 가로 0.066(획 0.052 + 두께 0.014)이라 간격을 0.076 으로 벌렸다 —
        // 0.070 이면 자리 사이가 4mm 뿐이라 «두 자리» 가 «한 덩어리» 로 붙어 보인다.
        float[] slotX = { 0.152f, 0.076f, -0.076f, -0.152f };   // 시10 시1 : 분10 분1
        for (int d = 0; d < 4; d++)
            for (int s = 0; s < 7; s++)
                segments[d, s] = Segment($"D{d}_{"abcdefg"[s]}", slotX[d], s);

        colonDots = new GameObject[2];
        for (int i = 0; i < 2; i++)
            colonDots[i] = Box($"Colon_{i}", new Vector3(0f, 0.105f + (i == 0 ? 0.026f : -0.026f), GlyphZ),
                               new Vector3(0.014f, 0.014f, BarDeep), Glow, Finish.발광);

        // ── 종이 띠 : 날짜·요일·날씨 ─────────────────────────────────
        // 한지 창 바닥이 y 0.0475 라 그보다 아래에 둔다 — 겹치면 같은 평면에서 지지직거린다.
        Box("Strip", new Vector3(0f, 0.026f, PanelZ + 0.003f),
            new Vector3(0.40f, 0.036f, 0.010f), Paper);
        strip = MakeLabel();
    }

    /// <summary>획 하나. 일곱 개를 다 만들어 두고 <b>켜고 끄기만</b> 한다.</summary>
    GameObject Segment(string name, float cx, int index)
    {
        // a 위 · b 오른위 · c 오른아래 · d 아래 · e 왼아래 · f 왼위 · g 가운데
        // 오른쪽이 −X 라 b·c 가 음수, e·f 가 양수다.
        Vector3[] offset =
        {
            new Vector3( 0f,     0.039f, 0f),
            new Vector3(-0.026f, 0.020f, 0f),
            new Vector3(-0.026f,-0.020f, 0f),
            new Vector3( 0f,    -0.039f, 0f),
            new Vector3( 0.026f,-0.020f, 0f),
            new Vector3( 0.026f, 0.020f, 0f),
            new Vector3( 0f,     0f,     0f),
        };
        bool flat = index == 0 || index == 3 || index == 6;
        Vector3 size = flat ? new Vector3(BarLong, BarThin, BarDeep)
                            : new Vector3(BarThin, BarLong * 0.78f, BarDeep);

        return Box(name, new Vector3(cx + offset[index].x, 0.105f + offset[index].y, GlyphZ),
                   size, Glow, Finish.발광);
    }

    GameObject Box(string name, Vector3 at, Vector3 size, Color color,
                   Finish? finish = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = at;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial =
            finish.HasValue ? FlatMaterial.Get(color, finish.Value) : FlatMaterial.Get(color);

        // 시계는 장식이다 — 걸어다니다 부딪히면 짐이 된다.
        var collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go;
    }

    TextMesh MakeLabel()
    {
        var go = new GameObject("StripText");
        go.transform.SetParent(transform, false);
        // TextMesh 는 자기 +Z 쪽에서 읽히게 생겼는데 시계 정면은 −Z 쪽이라 180도 돌린다
        // (현판 글씨가 거울상으로 나왔던 것과 같은 이유 — 2026-09-17).
        go.transform.localPosition = new Vector3(0f, 0.026f, PanelZ + 0.012f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var font = Resources.Load<Font>("HudFont");
        var text = go.AddComponent<TextMesh>();
        text.font = font;
        text.fontSize = 120;                       // 크게 굽고 characterSize 로 줄인다 — 작게 구우면 계단이 진다
        text.characterSize = 0.0022f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color32(0x4A, 0x3A, 0x2C, 0xFF);   // 종이 위 먹색 (Hud.Ink 와 같은 값)

        if (font != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = BuildingSign.TextMaterial(font);
        return text;
    }

    // ══════════════════════════════════════════════════════════ 표시
    void Refresh(bool force)
    {
        var now = System.DateTime.Now;
        shownMinute = now.Minute;

        if (segments != null)
        {
            ShowDigit(0, now.Hour / 10);
            ShowDigit(1, now.Hour % 10);
            ShowDigit(2, now.Minute / 10);
            ShowDigit(3, now.Minute % 10);
        }

        if (strip != null)
        {
            string[] days = { "일", "월", "화", "수", "목", "금", "토" };
            string date = $"{now.Month}월 {now.Day}일 ({days[(int)now.DayOfWeek]})";
            strip.text = string.IsNullOrEmpty(weather) ? date : $"{date}  ·  {weather}";
        }
    }

    static readonly bool[][] Glyph =
    {                  //  a      b      c      d      e      f      g
        new[] { true,  true,  true,  true,  true,  true,  false },   // 0
        new[] { false, true,  true,  false, false, false, false },   // 1
        new[] { true,  true,  false, true,  true,  false, true  },   // 2
        new[] { true,  true,  true,  true,  false, false, true  },   // 3
        new[] { false, true,  true,  false, false, true,  true  },   // 4
        new[] { true,  false, true,  true,  false, true,  true  },   // 5
        new[] { true,  false, true,  true,  true,  true,  true  },   // 6
        new[] { true,  true,  true,  false, false, false, false },   // 7
        new[] { true,  true,  true,  true,  true,  true,  true  },   // 8
        new[] { true,  true,  true,  true,  false, true,  true  },   // 9
    };

    void ShowDigit(int slot, int value)
    {
        var on = Glyph[Mathf.Clamp(value, 0, 9)];
        for (int s = 0; s < 7; s++)
        {
            var seg = segments[slot, s];
            if (seg == null) continue;
            // <b>끄는 대신 어둡게 한다.</b> 안 쓰는 획이 사라지면 «숫자» 가 아니라
            // «막대 몇 개» 로 보인다 — 실제 전자시계도 꺼진 획이 흐리게 남아 있다.
            seg.GetComponent<Renderer>().sharedMaterial =
                on[s] ? FlatMaterial.Get(Glow, Finish.발광) : FlatMaterial.Get(Dim);
        }
    }

    // ══════════════════════════════════════════════════════════ 날씨
    /// <summary>
    /// <b>wttr.in</b> 에서 한 줄만 받아온다. API 키가 필요 없는 게 이걸 고른 이유야 —
    /// 키가 들어가면 제출본에 비밀이 섞이고, 키가 만료되면 조용히 망가진다.
    ///
    /// <b>인터넷이 없어도 시계는 멀쩡히 돈다.</b> 실패하면 날씨 칸만 비고 날짜는 그대로다 —
    /// 심사 자리에서 네트워크가 막혀도 아무 일도 안 일어나게.
    /// </summary>
    IEnumerator WeatherLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(1f, refreshMinutes) * 60f);
        while (true)
        {
            yield return Fetch();
            yield return wait;
        }
    }

    /// <summary>
    /// <b>«플레이어 사는 곳» 을 알아내는 정직한 방법.</b> 2026-09-18 유저가 물었다:
    /// *"도시는 플레이어 사는 곳 해킹 안 되나. 개인정보 위반인가."*
    ///
    /// 해킹은 필요 없고 해서도 안 된다. 대신 <b>운영체제가 이미 알려주는 것</b>을 쓴다 —
    /// <b>표준 시간대</b>는 인터넷도 권한도 없이 읽히고, 바깥으로 나가는 정보가 0이다.
    /// 나라 단위라 «서울 사는 사람에게 부산 날씨» 가 뜨는 일은 없지만 도시 단위로는 대충이야.
    /// 정확히 보고 싶으면 <b>플레이어가 직접 적는다</b> — 그게 제일 정확하고 제일 안전하다.
    /// </summary>
    static string GuessCityFromTimeZone()
    {
        string id = System.TimeZoneInfo.Local.Id ?? "";
        if (id.Contains("Korea")) return "Seoul";
        if (id.Contains("Tokyo")) return "Tokyo";
        if (id.Contains("China") || id.Contains("Taipei")) return "Taipei";
        return "";   // 모르면 비워 둔다 — 그때만 wttr.in 이 접속 위치로 짐작한다
    }

    IEnumerator Fetch()
    {
        string pick = string.IsNullOrEmpty(city) ? GuessCityFromTimeZone() : city;
        string where = string.IsNullOrEmpty(pick) ? "" : UnityWebRequest.EscapeURL(pick);
        string url = $"https://wttr.in/{where}?format=%C+%t&lang=ko";

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 6;                      // 오래 붙들면 로비가 느려진 것처럼 보인다
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[시계] 날씨를 못 받아왔다 ({req.error}). 날짜만 띄운다.", this);
                yield break;
            }

            string line = req.downloadHandler.text.Trim();
            // 한 줄만 쓴다. 가끔 여러 줄이 오는데 종이 띠에는 한 줄만 들어간다.
            int cut = line.IndexOf('\n');
            if (cut > 0) line = line.Substring(0, cut).Trim();
            if (line.Length > 24) line = line.Substring(0, 24);

            weather = line;
            Refresh(force: true);
        }
    }
}
