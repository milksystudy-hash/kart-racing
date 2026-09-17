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
        if (wrote || string.IsNullOrEmpty(buildingName)) return;
        wrote = true;

        var font = Resources.Load<Font>(PlaqueFontName);
        if (font == null) return;

        var parent = transform.parent != null ? transform.parent : transform;

        // <b>크기를 현판 폭에서 뽑으면 안 된다.</b> 현판 높이는 0.78 로 고정인데 폭은 문마다
        // 달라서, 넓은 문에서는 글자가 판 높이를 넘어 아래 줄과 겹쳤다(2026-09-17 유저 제보 —
        // 흰 글씨와 노란 글씨가 포개져 있었다). 높이에 맞춘 <b>절대값</b>으로 잡는다.
        const float nameLine = 0.46f;    // 이름 한 줄의 높이(m)
        const float subLine  = 0.21f;    // 아래 작은 줄

        Label(parent, "PlaqueText", buildingName, font,
              transform.localPosition + new Vector3(0f, 0.28f, 0.24f),
              new Color32(0xF6, 0xEC, 0xD6, 0xFF), nameLine, TextAnchor.MiddleCenter);

        // 학과와 한 줄은 <b>현판 밖, 그 아래</b>로 내린다. 판 안에 같이 넣으면 자리가 안 나온다.
        // 둘을 한 줄로 이어붙이면 "조리·제빵·공예·봉제   손재주는…" 처럼 판보다 길어지니 줄을 나눈다.
        string under = department;
        if (!string.IsNullOrEmpty(motto))
            under = string.IsNullOrEmpty(department) ? motto : department + "\n" + motto;
        if (string.IsNullOrEmpty(under)) return;

        // 금색은 어두운 판 위에서도 탁하다. <b>밝은 크림</b>으로 올린다 — 색으로 구분하는 건
        // 크기와 자리가 이미 하고 있어서, 여기서까지 색을 쓰면 읽기만 나빠진다.
        Label(parent, "PlaqueSub", under, font,
              transform.localPosition + new Vector3(0f, -0.06f, 0.24f),
              new Color32(0xE6, 0xD6, 0xAE, 0xFF), subLine, TextAnchor.UpperCenter);
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
