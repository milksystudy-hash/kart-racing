using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <b>고른 전시품을 따로 띄워 놓고 마우스로 돌려본다.</b>
///
/// 2026-10-02 유저: *"설명은 아주 좋은데 좀 허전한 것 같아. 유저가 선택한 아이템을 보거나
/// 빙글빙글 돌리는 패널을 저기(왼쪽)에 놓을 수 있어?"*
///
/// 진열장 안의 물건은 <b>유리 너머 8 m</b> 라 아무리 잘 만들어도 안 보인다.
/// 전시실이 여덟 번 들르는 방인데 <b>볼 것이 설명 글뿐</b>이면, 모델을 만든 보람도 없고
/// 들를 이유도 없다. 돌려볼 수 있으면 그 물건이 <b>증거</b>라는 게 손에 잡힌다.
///
/// <code>
///   화면 밖(−500 m)에 작은 무대를 하나 짓고
///   → 고른 물건을 복제해 거기 세우고
///   → 전용 카메라가 <b>RenderTexture</b> 에 찍고
///   → HUD 가 그 그림을 패널에 그린다. 드래그하면 무대의 물건이 돈다.
/// </code>
///
/// ★ 무대를 <b>−500 m</b> 에 두는 이유: 레이어를 새로 만들면 <c>TagManager</c> 를 건드려야
/// 하고 그건 프로젝트 설정이다. 궤도 카메라는 피벗 (0,1.4,0)에서 11 m 안을 보니까
/// 500 m 아래는 <b>영영 안 보인다</b> — 설정 파일을 한 줄도 안 고치고 같은 결과가 난다.
///
/// ★ <b>씬에 저장되는 게 없다.</b> <see cref="GalleryHUD"/> 가 실행 중에 만들고 나갈 때 지운다 —
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」 를 다섯 번 겪은 뒤의 기본형.
/// </summary>
public class ExhibitViewer : MonoBehaviour
{
    /// <summary>무대를 세우는 자리. 궤도 카메라가 절대 못 보는 높이.</summary>
    const float StageY = -500f;

    /// <summary>그림 크기. 패널이 가상 좌표 230 쯤이라 두 배면 충분하다.</summary>
    const int Size = 512;

    /// <summary>드래그 1 px 당 몇 도. 너무 크면 손을 떼는 순간 어디를 보는지 모른다.</summary>
    const float DragSpeed = 0.42f;

    Camera stageCamera;
    Transform pivot;
    RenderTexture target;
    GameObject shown;

    GalleryCase current;
    float yaw, pitch = 12f;
    bool dragging;
    Vector2 lastMouse;

    /// <summary>지금 그림이 있나. 없으면 HUD 가 패널을 아예 안 그린다.</summary>
    public bool Ready => target != null && shown != null;

    public Texture Image => target;

    /// <summary>지금 보여주는 칸. <see cref="Show"/> 로 바꾼다.</summary>
    public GalleryCase Current => current;

    void Awake()
    {
        var stage = new GameObject("ExhibitStage").transform;
        stage.SetParent(transform, false);
        stage.position = new Vector3(0f, StageY, 0f);

        pivot = new GameObject("Pivot").transform;
        pivot.SetParent(stage, false);

        // 전용 조명. <b>점광원이라 이 무대만 밝힌다</b> — 방향광을 쓰면 전시실 전체가 밝아진다.
        var lamp = new GameObject("StageLight").AddComponent<Light>();
        lamp.transform.SetParent(stage, false);
        lamp.transform.localPosition = new Vector3(0.6f, 1.1f, -0.9f);
        lamp.type = LightType.Point;
        lamp.range = 6f;
        lamp.intensity = 4.2f;
        lamp.color = new Color(1f, 0.96f, 0.88f);
        lamp.shadows = LightShadows.None;

        var fill = new GameObject("StageFill").AddComponent<Light>();
        fill.transform.SetParent(stage, false);
        fill.transform.localPosition = new Vector3(-0.9f, 0.3f, -0.7f);
        fill.type = LightType.Point;
        fill.range = 5f;
        fill.intensity = 1.5f;
        fill.color = new Color(0.78f, 0.84f, 1f);
        fill.shadows = LightShadows.None;

        target = new RenderTexture(Size, Size, 24) { antiAliasing = 4 };

        stageCamera = new GameObject("StageCamera").AddComponent<Camera>();
        stageCamera.transform.SetParent(stage, false);
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0.06f, 0.07f, 0.11f, 1f);
        stageCamera.fieldOfView = 34f;
        stageCamera.nearClipPlane = 0.05f;
        stageCamera.farClipPlane = 12f;      // 무대 밖은 아무 것도 안 들어온다
        stageCamera.targetTexture = target;
        stageCamera.enabled = false;         // 직접 Render() 한다 — 매 프레임 공짜가 아니다
    }

    void OnDestroy()
    {
        if (target != null) { target.Release(); Destroy(target); }
    }

    /// <summary>
    /// 이 칸의 물건을 무대에 세운다. 같은 칸이면 아무 것도 안 한다.
    /// <paramref name="display"/> 가 null 이면 무대를 비운다.
    /// </summary>
    public void Show(GalleryCase display)
    {
        if (display == current) return;
        current = display;

        if (shown != null) { Destroy(shown); shown = null; }
        yaw = 0f; pitch = 12f;
        if (display == null) return;

        // 모은 것만 진짜 모델. 아직이면 실루엣이 선다 — 미리 보여주면 모으는 재미가 없다.
        var source = display.IsCollected && display.realModel != null
            ? display.realModel
            : display.placeholder;
        if (source == null) return;

        shown = Instantiate(source, pivot);
        shown.name = "Shown";
        shown.SetActive(true);
        shown.transform.localRotation = Quaternion.identity;

        // 돌리는 건 우리가 한다 — 제자리 회전이 섞이면 드래그가 안 먹는 것처럼 보인다
        foreach (var s in shown.GetComponentsInChildren<ExhibitSpin>(true)) Destroy(s);
        foreach (var c in shown.GetComponentsInChildren<Collider>(true)) Destroy(c);

        Frame();
    }

    /// <summary>
    /// 물건이 화면에 꽉 차게 카메라를 물린다. <b>크기를 손으로 적지 않는다</b> —
    /// 코인과 병풍이 같은 숫자로 맞을 리가 없다. 바운즈를 재서 거리를 거꾸로 구한다.
    /// </summary>
    void Frame()
    {
        var renderers = shown.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // 피벗을 물건 한가운데로 옮긴다 — 안 그러면 바닥 모서리를 축으로 돈다
        shown.transform.position -= b.center - pivot.position;

        float radius = Mathf.Max(b.extents.magnitude, 0.05f);
        float distance = radius / Mathf.Sin(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.25f;

        stageCamera.transform.localPosition = new Vector3(0f, 0f, -distance);
        stageCamera.transform.localRotation = Quaternion.identity;
        stageCamera.nearClipPlane = Mathf.Max(0.02f, distance - radius * 2f);
        stageCamera.farClipPlane = distance + radius * 3f;
    }

    /// <summary>
    /// 패널 안에서 끌면 돈다. <paramref name="panel"/> 은 <b>부르는 쪽의 좌표계</b>이고,
    /// <paramref name="uiScale"/> 로 마우스를 거기에 맞춘다 —
    /// <see cref="Hud"/> 를 쓰는 HUD 는 <see cref="Hud.ScaleFactor"/>, 전시실은 1 이다.
    /// 손을 떼도 천천히 계속 돈다 — 멈춰 있으면 돌릴 수 있는 줄 모른다.
    /// </summary>
    public void Handle(Rect panel, float uiScale = 1f)
    {
        if (!Ready) return;

        var m = Mouse.current;
        if (m != null)
        {
            float s = Mathf.Max(0.01f, uiScale);
            var p = m.position.ReadValue();
            var v = new Vector2(p.x / s, (Screen.height - p.y) / s);

            if (m.leftButton.wasPressedThisFrame && panel.Contains(v)) { dragging = true; lastMouse = v; }
            if (!m.leftButton.isPressed) dragging = false;

            if (dragging)
            {
                Vector2 d = v - lastMouse;
                lastMouse = v;
                yaw -= d.x * DragSpeed;
                pitch = Mathf.Clamp(pitch + d.y * DragSpeed, -60f, 70f);
            }
        }

        if (!dragging) yaw -= 16f * Time.unscaledDeltaTime;   // 가만히 두면 저절로 돈다

        pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        stageCamera.Render();
    }
}
