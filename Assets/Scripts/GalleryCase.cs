using UnityEngine;

/// <summary>
/// 전시실의 진열장 하나. 수집품 한 점을 올려놓는다.
///
/// 아직 못 모은 것은 **검은 실루엣**으로 서 있다 — 비어 있는 것보다 "찾아야 할 게 남았다" 는
/// 신호가 훨씬 잘 보인다. 모으면 색이 들어오고 천천히 돈다.
///
/// **네 모델을 넣는 곳은 `ItemAnchor`야.** 거기 자식으로 넣고 `Placeholder` 를 지우면 끝.
/// </summary>
public class GalleryCase : MonoBehaviour
{
    [Header("전시품")]
    [Tooltip("CollectionState 가 쓰는 식별자. 레이스에서 이 id 로 수집한다")]
    public string itemId = "";
    public string displayName = "";
    [TextArea(2, 4)]
    [Tooltip("클릭하면 뜨는 설명. 이야기 조각을 여기 넣으면 된다")]
    public string description = "";
    [Tooltip("어느 장에서 얻는지. 화면에 같이 뜬다")]
    public string chapter = "";

    [Header("모델")]
    [Tooltip("★ 여기에 수집품 FBX 를 자식으로 넣어")]
    public Transform itemAnchor;
    [Tooltip("임시 자리표시. 진짜 모델이 오면 지우면 된다")]
    public GameObject placeholder;
    public Renderer itemRenderer;

    /// <summary>
    /// 유저가 만든 진짜 전시품(FBX). 있으면 <b>모았을 때만</b> 보이고,
    /// 아직 못 모았을 때는 <see cref="placeholder"/> 실루엣이 대신 선다.
    ///
    /// ★ 진짜 모델은 <b>색칠하지 않는다.</b> <see cref="Paint"/> 는 재질 하나를 통째로
    /// 덮어쓰는데, 유저 모델은 황동·나무·녹청처럼 <b>재질이 여럿</b>이라
    /// 그걸 쓰면 공들여 만든 물건이 단색 덩어리가 된다.
    /// </summary>
    public GameObject realModel;

    /// <summary>
    /// ★★ 2026-10-06 <b>돋보기에서 쓸 자세</b> — 진열장에 세울 때 기울이기 <b>전</b>의 회전.
    ///
    /// 진열장 안에서는 납작한 서류를 68도 눕혀 세운다(옆에서 봐도 보이라고).
    /// 그런데 <b>돋보기 카메라는 눈높이</b>라, 그 자세를 그대로 복제하면 종이가
    /// 거의 천장을 보고 누워서 <b>글씨가 아래로 돌아간다</b>
    /// (유저: *"마우스를 오른쪽으로 돌리면 글씨가 완전 아래로 돌려져 있다"*).
    ///
    /// 옛날에 구운 씬에서는 (0,0,0,0) 이라 <see cref="ExhibitViewer"/> 가
    /// 그걸 보고 예전 방식으로 되떨어진다 — 씬을 다시 안 구워도 안 터진다.
    /// </summary>
    public Quaternion viewRotation;

    /// <summary>
    /// 이 전시품이 <b>납작한 서류</b>인가. 돋보기에서 세우는 방향이 달라진다 —
    /// 서류는 <b>윗면</b>이 보여야 하고(눕혀 두면 종이 옆면만 보인다),
    /// 서 있는 물건은 <b>앞면</b>이 보여야 한다.
    /// </summary>
    public bool itemIsFlat;

    [Header("연출")]
    [Tooltip("아직 못 모은 진열장을 덮고 있는 흰 천. 모으면 걷힌다")]
    public GameObject dustCover;
    public Renderer plaqueRenderer;
    public Color plaqueIdle     = new Color32(0xB4, 0xCD, 0xBC, 0xFF);
    public Color plaqueHover    = new Color32(0xF0, 0xB5, 0x4A, 0xFF);
    public Color plaqueLocked   = new Color32(0x5A, 0x5E, 0x62, 0xFF);
    [Tooltip("아직 못 모은 전시품의 색")]
    public Color silhouetteColor = new Color32(0x2A, 0x2C, 0x30, 0xFF);
    public Color revealedColor   = new Color32(0xD8, 0xB4, 0x6C, 0xFF);
    public float spinSpeed = 28f;

    public bool IsCollected => CollectionState.Has(itemId);
    public string Label => string.IsNullOrEmpty(displayName) ? itemId : displayName;

    bool highlighted;
    [Header("진열 조명")]
    [Tooltip("이 진열장 위의 스포트. 모은 것만 불이 켜진다 — 방을 보면 몇 개 모았는지 바로 보인다")]
    public Light caseLight;
    public float litIntensity = 26f;
    public float unlitIntensity = 3f;

    Color revealed;

    void Awake()
    {
        revealed = revealedColor;
        ApplyLook();
        ApplyLight();
    }

    /// <summary>
    /// 모은 진열장에만 불을 켠다. 노란 불빛은 원래 여기 있어야 하는 것 —
    /// 달리는 중이 아니라 전시실에서, 모은 개수만큼 방이 밝아진다.
    /// </summary>
    void ApplyLight()
    {
        if (caseLight == null) return;
        caseLight.intensity = IsCollected ? litIntensity : unlitIntensity;
    }

    void Update()
    {
        // 로비의 F9/F10 로 수집을 지웠다 채웠다 할 수 있으니 상태가 바뀌면 따라간다.
        if (dustCover != null && dustCover.activeSelf == IsCollected) ApplyLook();

        // 모은 전시품만 천천히 돈다
        if (itemAnchor != null && IsCollected)
            itemAnchor.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    public void SetHighlighted(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        ApplyLook();
    }

    /// <summary>수집 상태가 바뀌었을 때 다시 칠한다.</summary>
    public void Refresh() => ApplyLook();

    void ApplyLook()
    {
        bool collected = IsCollected;

        // 아직 못 모은 진열장은 <b>천을 덮어 둔다.</b> 비어 있는 것과 덮여 있는 것은
        // 읽히는 게 다르다 — 빈 진열장은 "아직 안 만들었나" 지만, 덮인 진열장은
        // "여긴 아직 못 열었다" 가 된다. 여덟 장이 한 장씩 걷히는 게 이 방의 진행도야.
        if (dustCover != null) dustCover.SetActive(!collected);

        if (plaqueRenderer != null)
        {
            Color c = !collected ? plaqueLocked : (highlighted ? plaqueHover : plaqueIdle);
            Paint(plaqueRenderer, c);
        }

        // 진짜 모델이 있으면 «모았을 때만» 그게 서고, 아니면 실루엣이 선다.
        if (realModel != null)
        {
            realModel.SetActive(collected);
            if (placeholder != null) placeholder.SetActive(!collected);
            if (!collected && itemRenderer != null) Paint(itemRenderer, silhouetteColor);
            return;
        }

        if (itemRenderer != null)
            Paint(itemRenderer, collected ? revealed : silhouetteColor);
    }

    static void Paint(Renderer renderer, Color color)
    {
        var mat = renderer.material;   // 인스턴스를 써서 공유 머티리얼을 안 건드린다
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }
}
