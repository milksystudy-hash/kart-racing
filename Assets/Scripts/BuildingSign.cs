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
        float width = transform.localScale.x;

        Label(parent, "PlaqueText", buildingName, font,
              transform.localPosition + new Vector3(0f, 0.09f, 0.1f),
              new Color32(0xF6, 0xEC, 0xD6, 0xFF), width / 2.1f);

        string under = string.IsNullOrEmpty(motto) ? department
                     : (string.IsNullOrEmpty(department) ? motto : department + "   " + motto);
        if (string.IsNullOrEmpty(under)) return;

        // 학과와 한 줄은 현판 아래 작게. 이름만으로는 뭐 하는 곳인지 모르는 관이 있다.
        Label(parent, "PlaqueSub", under, font,
              transform.localPosition + new Vector3(0f, -0.24f, 0.1f),
              new Color32(0xD8, 0xB2, 0x40, 0xFF), width / 5.5f);
    }

    static void Label(Transform parent, string name, string body, Font font,
                      Vector3 local, Color color, float size)
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
        text.fontSize = 64;                  // 크게 구워서 줄여 쓴다 — 작게 구우면 계단이 보인다
        text.characterSize = size * 0.06f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;

        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
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
