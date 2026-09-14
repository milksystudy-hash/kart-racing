using UnityEngine;

/// <summary>
/// 로비의 캐릭터 자리 하나. 받침대 + 그 위에 선 캐릭터.
///
/// **네 캐릭터 FBX 를 넣는 곳은 `Model Anchor`야.**
/// 거기에 FBX 를 자식으로 끌어다 놓고 `Placeholder` 를 지우면 끝. 코드는 안 고쳐도 돼.
/// </summary>
public class CharacterStand : MonoBehaviour
{
    [Header("정체")]
    [Tooltip("0부터. 로비에 놓인 순서와 같게")]
    public int index;
    [Tooltip("화면에 뜰 이름. 비워두면 '#1' 처럼 번호로 나온다")]
    public string displayName = "";
    [Tooltip("기획서상 잠긴 자리 — 5번 개발업자, 6번 시의원. 고를 수 없다")]
    public bool locked;

    [Header("모델")]
    [Tooltip("★ 여기에 캐릭터 FBX 를 자식으로 넣어")]
    public Transform modelAnchor;
    [Tooltip("임시 자리표시. 진짜 모델이 오면 지우면 된다")]
    public GameObject placeholder;

    [Header("연출")]
    public Renderer baseRenderer;
    public Color baseColor     = new Color32(0x8A, 0x7E, 0x74, 0xFF);
    public Color hoverColor    = new Color32(0xC7, 0xB2, 0x8E, 0xFF);
    public Color selectedColor = new Color32(0xF0, 0xB5, 0x4A, 0xFF);
    public Color lockedColor   = new Color32(0x4A, 0x4E, 0x58, 0xFF);
    public float spinSpeed = 45f;
    public float bobHeight = 0.08f;
    public float bobSpeed = 2.2f;
    [Tooltip("커서를 올렸을 때 살짝 커지는 정도")]
    public float hoverScale = 1.06f;

    /// <summary>고를 수 있는 자리인지. 잠긴 자리(5·6번)는 클릭해도 선택되지 않는다.</summary>
    public bool Selectable => !locked;

    /// <summary>진짜 모델이 아직 안 들어온 자리인지. 임시 자리표시도 자식으로 치기 때문에
    /// 잠금 여부와는 별개다 — 잠금은 <see cref="locked"/> 로 판단한다.</summary>
    public bool IsEmpty => modelAnchor == null || modelAnchor.childCount == 0;

    public string Label => string.IsNullOrEmpty(displayName) ? $"#{index + 1}" : displayName;

    bool selected;
    bool highlighted;
    Vector3 anchorHome;
    Vector3 anchorScale;
    float bobPhase;

    void Awake()
    {
        if (modelAnchor != null)
        {
            anchorHome = modelAnchor.localPosition;
            anchorScale = modelAnchor.localScale;
        }
        ApplyBaseColor();
    }

    void Update()
    {
        if (modelAnchor == null) return;

        if (selected)
        {
            // 고른 캐릭터만 천천히 돌면서 살짝 떠오른다 — 어느 걸 골랐는지 한눈에 보이게
            modelAnchor.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
            bobPhase += Time.deltaTime * bobSpeed;
            modelAnchor.localPosition = anchorHome + Vector3.up * (Mathf.Sin(bobPhase) * 0.5f + 0.5f) * bobHeight;
        }
        else if (modelAnchor.localPosition != anchorHome)
        {
            modelAnchor.localPosition = Vector3.MoveTowards(modelAnchor.localPosition, anchorHome,
                                                            Time.deltaTime * 0.5f);
        }

        // 커서를 올리면 살짝 커진다 — 클릭할 수 있다는 신호
        Vector3 goal = anchorScale * ((highlighted && Selectable) ? hoverScale : 1f);
        modelAnchor.localScale = Vector3.Lerp(modelAnchor.localScale, goal,
                                              1f - Mathf.Exp(-12f * Time.deltaTime));
    }

    public void SetSelected(bool on)
    {
        if (selected == on) return;
        selected = on;
        bobPhase = 0f;
        ApplyBaseColor();
    }

    /// <summary>커서가 올라와 있는 상태. 선택과는 별개다.</summary>
    public void SetHighlighted(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        ApplyBaseColor();
    }

    void ApplyBaseColor()
    {
        if (baseRenderer == null) return;

        // 받침대만 색이 바뀐다. 공유 머티리얼을 건드리지 않으려고 인스턴스를 쓴다.
        Color c = locked      ? lockedColor
                : selected    ? selectedColor
                : highlighted ? hoverColor
                              : baseColor;

        var mat = baseRenderer.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }
}
