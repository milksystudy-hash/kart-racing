using UnityEngine;

/// <summary>
/// 건물 현판. <b>이름과 학과를 들고 있고, 이야기에 따라 딱지가 붙는다.</b>
///
/// 2026-09-17 에 설정이 하나 붙었다: 박물관은 <b>폐교한 곰 전문대 부지</b>에 들어섰고
/// 간판만 그대로 남았다. 그래서 철거는 이 캠퍼스의 <b>두 번째 죽음</b>이야.
/// 기획서의 박물관 설정을 하나도 안 건드리면서 한옥 캠퍼스에 학과 건물이 왜 있는지가 설명된다.
///
/// 연출이 여기 걸린다. <b>기하를 하나도 안 건드리고</b> 캠퍼스 전체가 이야기에 반응한다 —
/// 현판에 빨간 딱지가 붙었다 떨어지는 것만으로 "여기 없어질 곳이구나" 가 읽힌다.
/// 건물을 새로 짓거나 부수는 것보다 백 배 싸고, 플레이어는 지나가면서 본다.
/// </summary>
public class BuildingSign : MonoBehaviour
{
    [Tooltip("현판에 적히는 이름")]
    public string buildingName = "";

    [Tooltip("무슨 학과였는지. 현판 아래 작게")]
    public string department = "";

    [Tooltip("이 건물 이름 아래 붙는 한 줄. 비워도 된다")]
    public string motto = "";

    [Tooltip("켜면 현판에 빨간 [폐과] 딱지가 붙는다")]
    public bool closed;

    Transform sticker;
    bool wrote;

    void Start()
    {
        WriteName();
        Apply();
    }

    /// <summary>
    /// 현판에 글씨.
    ///
    /// <b>TextMeshPro 를 안 쓴다.</b> TMP 는 TTF 를 그대로 못 쓰고 전용 폰트 에셋이 필요한데,
    /// 그 에셋을 만들려면 TMP 기본 리소스가 임포트돼 있어야 하고 이 프로젝트에는 없다.
    /// 현판 글씨 하나 때문에 패키지 설정을 건드릴 일이 아니야 — 옛날 TextMesh 로 충분하다.
    /// 가까이서 보면 조금 흐릿한데, 달리면서 스쳐 보는 간판이라 그게 문제가 안 된다.
    ///
    /// 폰트는 이미 있는 <c>Resources/HudFont.ttf</c> 를 그대로 쓴다. 새로 넣는 게 없다.
    ///
    /// 글자는 현판의 <b>자식이 아니라 형제</b>로 단다. 현판은 납작하게 눌린 상자라
    /// 자식으로 넣으면 글씨도 같이 눌린다.
    /// </summary>
    void WriteName()
    {
        // 안내판은 <b>이름이 비어 있고 학과만</b> 들어온다. 이름만 보고 빠지면 안 그려진다.
        if (wrote) return;
        if (string.IsNullOrEmpty(buildingName)
            && string.IsNullOrEmpty(department)
            && string.IsNullOrEmpty(motto)) return;
        wrote = true;

        var font = Resources.Load<Font>(PlaqueFontName);
        if (font == null) return;

        var parent = transform.parent != null ? transform.parent : transform;

        // <b>판마다 글자를 따로 담는다.</b> 전에는 현판과 안내판의 줄이 전부 문 루트에
        // 형제로 붙어서 어느 판 것인지 구분이 안 됐다 — 검사도 못 하고, 하나를 지우면
        // 다른 것까지 흔들린다. 판 위치에 빈 통을 하나 두고 그 밑에 넣는다.
        var holder = new GameObject(name + "_Text").transform;
        holder.SetParent(parent, false);
        holder.localPosition = transform.localPosition;
        holder.localRotation = Quaternion.identity;

        // 판의 실제 크기. 글자는 <b>이 안에 맞춰서 줄인다</b> —
        // 고정 크기로 두면 이름이 길 때 판 밖으로 넘친다(2026-09-17 유저: "글자가 차고 넘쳤다").
        float plateW = transform.localScale.x * 0.88f;
        float plateH = transform.localScale.y * 0.80f;

        // 줄을 모은다. 현판에는 이름만, 안내판에는 학과와 한 줄.
        var lines = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(buildingName)) lines.Add(buildingName);
        if (!string.IsNullOrEmpty(department)) lines.AddRange(Wrap(department, 9));
        if (!string.IsNullOrEmpty(motto)) lines.AddRange(Wrap(motto, 11));
        if (lines.Count == 0) return;

        // 한 줄 높이 — 세로로도 들어가야 하고 제일 긴 줄이 가로로도 들어가야 한다.
        float byHeight = plateH / lines.Count;
        float byWidth = plateW / Mathf.Max(1, Longest(lines)) / CharRatio;
        float line = Mathf.Min(byHeight, byWidth, maxLine);

        // 여러 줄이면 첫 줄만 크게. 이름과 설명은 크기로 구분하는 게 색으로 구분하는 것보다 낫다.
        float top = (lines.Count - 1) * line * 0.5f;

        for (int i = 0; i < lines.Count; i++)
        {
            float size = i == 0 ? line : line * 0.82f;
            // 2026-09-18 유저: *"재주관 안내판에 나무판자랑 글씨가 겹쳐 보인다."*
            // 글자를 판 앞 <b>0.24m</b> 에 띄웠는데 안내판 두께가 0.12m 라, 글자가
            // 나무 테두리와 기둥보다 앞으로 튀어나와 <b>따로 떠 있는 것처럼</b> 보였다.
            // 0.09m — 판 면에서 겨우 떨어질 만큼만. 붙이면 z-파이팅이고 띄우면 뜬다.
            Label(holder, $"SignLine_{i}", lines[i], font,
                  new Vector3(0f, top - i * line, 0.09f),
                  i == 0 ? new Color32(0xF6, 0xEC, 0xD6, 0xFF) : new Color32(0xE0, 0xCE, 0xA4, 0xFF),
                  size, TextAnchor.MiddleCenter);
        }
    }

    [Tooltip("한 줄이 이보다 커지지는 않는다(m)")]
    public float maxLine = 0.5f;

    /// <summary>
    /// 한글은 글자 하나가 대체로 정사각형이라 <b>글자 수 × 높이</b> 로 폭을 어림할 수 있다.
    /// 0.95 는 자간까지 친 값 — 로마자와 가운뎃점은 이보다 좁아서 <b>조금 작게 잡히는 쪽</b>으로
    /// 틀린다. 넘치는 것보다 작은 게 낫다.
    /// </summary>
    const float CharRatio = 0.95f;

    static int Longest(System.Collections.Generic.List<string> lines)
    {
        int most = 0;
        foreach (var line in lines) most = Mathf.Max(most, line.Length);
        return most;
    }

    /// <summary>
    /// 긴 줄을 <b>가운뎃점에서</b> 자른다. 한글은 띄어쓰기가 드물어서 공백으로 자르면
    /// 안 잘리는 줄이 생긴다 — 이 프로젝트의 학과 이름은 전부 "조리·제빵·공예·봉제" 꼴이야.
    /// </summary>
    static System.Collections.Generic.List<string> Wrap(string body, int per)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (body.Length <= per) { lines.Add(body); return lines; }

        // 가운뎃점이 없는 문장은 <b>띄어쓰기로</b> 자른다. 한 줄 문구가 그렇다 —
        // "재주는 곰이 넘고 돈은 딴 놈이 번다" 에는 가운뎃점이 하나도 없어서
        // 22자가 통짜로 한 줄이 됐고, 글자 수로 억지로 자르니 <b>낱말 가운데가 끊겼다</b>.
        char cut = body.Contains("·") ? '·' : ' ';
        var parts = body.Split(cut);
        string current = "";

        foreach (var part in parts)
        {
            string next = current.Length == 0 ? part : current + cut + part;
            if (next.Length <= per) { current = next; continue; }

            if (current.Length > 0) lines.Add(current);
            current = part;
        }
        if (current.Length > 0) lines.Add(current);

        // 가운뎃점이 없어서 못 잘랐으면 글자 수로 자른다
        if (lines.Count == 1 && lines[0].Length > per)
        {
            lines.Clear();
            for (int i = 0; i < body.Length; i += per)
                lines.Add(body.Substring(i, Mathf.Min(per, body.Length - i)));
        }
        return lines;
    }

    static void Label(Transform parent, string name, string body, Font font,
                      Vector3 local, Color color, float lineHeight, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;

        // <b>180도 돌려야 글자가 바로 보인다.</b> TextMesh 는 자기 +Z 쪽에서 읽히게 생겼는데,
        // 문의 +Z 는 <b>보는 사람 쪽</b>을 향한다. 그대로 두면 뒤에서 본 꼴이라 좌우가 뒤집힌다.
        // (2026-09-17 유저 제보 — 모든 건물 현판이 거울 글씨로 나왔다)
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var text = go.AddComponent<TextMesh>();
        text.font = font;
        text.text = body;

        // <b>크게 구워서 줄여 쓴다.</b> 64 로는 가까이 갔을 때 계단이 보였다
        // (2026-09-17 유저: "확대하면 글자가 깨져 보인다"). 동적 폰트라 아틀라스에
        // 이 크기로 새로 굽히는 거고, 쓰는 글자가 스무 자 남짓이라 아틀라스도 안 터진다.
        const int baked = 120;
        text.fontSize = baked;

        // TextMesh 한 줄의 월드 높이 = fontSize × characterSize / 10. 원하는 높이에서 거꾸로 구한다.
        text.characterSize = lineHeight * 10f / baked;
        text.anchor = anchor;
        text.alignment = TextAlignment.Center;
        text.color = color;

        go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(font);
    }

    // ------------------------------------------------------------------
    //  글자가 벽을 통과해서 보이던 것
    // ------------------------------------------------------------------
    /// <summary>
    /// 유니티 기본 폰트 재질은 <b>"GUI/Text Shader" 라서 ZTest 가 Always</b> 다 —
    /// 화면 위 UI 용이라 그게 맞지만, 월드에 세운 글자에 쓰면 <b>벽 뒤에 있어도 그려진다.</b>
    /// 그래서 캠퍼스 반대편 건물 이름이 앞 건물을 뚫고 떠 보였다(2026-09-17 유저 제보).
    ///
    /// 깊이 검사만 켠 셰이더(Racing/PlaqueText)로 갈아 끼운다. 폰트마다 한 장이면 되고,
    /// <b>아틀라스가 다시 구워지면 텍스처가 바뀌므로</b> 그때마다 다시 꽂아준다 —
    /// 안 하면 어느 순간 글자가 뭉개진다(동적 폰트의 오래된 함정).
    /// </summary>
    static Material textMaterial;
    static Font boundFont;

    static Material TextMaterial(Font font)
    {
        if (textMaterial != null && boundFont == font)
        {
            textMaterial.mainTexture = font.material.mainTexture;
            return textMaterial;
        }

        var shader = Shader.Find("Racing/PlaqueText");
        if (shader == null) return font.material;   // 셰이더가 없으면 기본으로 — 글씨는 나와야 한다

        textMaterial = new Material(shader)
        {
            name = "PlaqueText",
            mainTexture = font.material.mainTexture,
            hideFlags = HideFlags.HideAndDontSave,
        };
        boundFont = font;

        Font.textureRebuilt -= OnFontRebuilt;
        Font.textureRebuilt += OnFontRebuilt;
        return textMaterial;
    }

    static void OnFontRebuilt(Font font)
    {
        if (textMaterial != null && font == boundFont)
            textMaterial.mainTexture = font.material.mainTexture;
    }

    /// <summary>Resources 안에 이미 있는 한글 폰트. 새로 넣는 게 없다.</summary>
    public const string PlaqueFontName = "HudFont";

    /// <summary>딱지를 붙이거나 뗀다. <see cref="CampusMood"/> 가 이야기 진행에 맞춰 부른다.</summary>
    public void SetClosed(bool value)
    {
        if (closed == value && sticker != null) return;
        closed = value;
        Apply();
    }

    void Apply()
    {
        if (sticker == null) sticker = MakeSticker();
        if (sticker != null) sticker.gameObject.SetActive(closed);
    }

    /// <summary>
    /// 현판을 가로지르는 빨간 띠. <b>글씨를 지우는 게 아니라 위에 덧붙인다</b> —
    /// 이름이 남아 있어야 "원래 뭐였는지" 가 보이고, 그래야 없어진다는 게 아프다.
    /// </summary>
    Transform MakeSticker()
    {
        var plaque = transform;
        var scale = plaque.localScale;
        if (scale.x <= 0.001f) return null;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ClosedSticker";
        go.transform.SetParent(plaque, false);

        // 부모(현판)가 눌린 상자라 자식도 같이 눌린다. 나누어 되돌린다.
        go.transform.localScale = new Vector3(1.06f / 1f, 0.42f, 0.9f);
        go.transform.localPosition = new Vector3(0f, -0.1f, -0.55f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);   // 삐뚤게 — 손으로 붙인 티

        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(new Color32(0xC4, 0x3A, 0x30, 0xFF));

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }
        return go.transform;
    }
}
