using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>환웅의 발자취</b> — 대충기념관 기록첩 전시대(<c>01_Footprints_Album</c>)를
/// <b>넘겨 보는 책</b>으로 만든다. 제작사 낙서 수첩 자리다.
///
/// 2026-09-29 유저: *"기록첩 전시대에는 내가 나중에 캐릭터 낙서하거나 하는 제작사 낙서
/// 수첩으로 쓸거라서 버튼 누르면 가상의 페이지가 자꾸넘어가는 연출로 바꿔줘.
/// 몇장 넣을지는 나도 모르니까."*
///
/// ★★ <b>«몇 장인지 모른다» 가 이 설계의 전부다.</b> 장수를 코드에 박으면 그림을 한 장
/// 넣거나 뺄 때마다 코드를 고쳐야 하고, 그러면 «나중에 낙서한다» 가 «나중에 프로그래머를
/// 부른다» 가 된다. 그래서 <b>폴더에 있는 만큼이 곧 장수</b>다:
///
/// <code>Assets/Resources/Album/ 에 PNG 를 떨군다 → 이름 순서가 곧 책 순서</code>
///
/// 한 장도 없어도 <b>빈 종이로 그냥 돌아간다</b>(쪽 번호가 올라간다). 그래야 그림이 오기
/// 전에도 «되는 것» 을 확인할 수 있다 — 이 프로젝트에서 «에셋이 없어서 안 보인다» 와
/// «코드가 고장났다» 를 구분 못 해 헤맨 적이 여러 번이다.
///
/// 책장은 <b>진짜로 넘어간다</b>: 오른쪽 장을 복제해 책등을 축으로 180도 돌린다.
/// 앞뒷면 두 장을 등 맞대어 붙여서, 넘어가는 동안 <b>앞면은 지금 장 · 뒷면은 다음 장</b>이
/// 보인다 — 실제 종이가 그렇다.
///
/// <see cref="CampusHUD"/> 가 E 로 집는다. 애셋 제공자가 같이 준 <c>HwanungAlbum.cs</c> 는
/// uGUI 캔버스 위에 그림을 띄우는 방식인데, 이 프로젝트에는 캔버스가 없고 <b>전시대 자체가
/// 3D 로 서 있으니</b> 책 위에 직접 얹는 쪽이 맞다(애셋 README 도 그 방법을 같이 적어 뒀다).
/// </summary>
public class AlbumBook : MonoBehaviour
{
    /// <summary>낙서를 넣는 자리. 이름 순서가 책 순서다.</summary>
    public const string PageFolder = "Album";

    /// <summary>그림이 한 장도 없을 때 넘겨 볼 빈 장 수. <b>되는지 확인하라고</b> 있는 값.</summary>
    const int BlankPages = 12;

    /// <summary>책장 한 장이 넘어가는 시간.</summary>
    const float TurnSeconds = 0.5f;

    [Tooltip("이 거리 안에 들어와야 안내가 뜬다")]
    public float range = 3.2f;

    [Tooltip("걸어다니는 몸. 비워두면 카메라")]
    public Transform visitor;

    // ── «제일 가까운 것» — MinigameSpot 과 같은 한 프레임 늦은 공개 ─────────────
    public static AlbumBook Nearest { get; private set; }
    public static float NearestScore { get; private set; } = float.MaxValue;

    static int frameStamp = -1;
    static float nearestDistance;
    static float pendingScore = float.MaxValue;
    static AlbumBook pending;

    Transform pageL, pageR;
    Renderer skinL, skinR;
    Texture2D[] art = new Texture2D[0];
    readonly Dictionary<Texture2D, Material> skins = new Dictionary<Texture2D, Material>();
    Material blank;

    /// <summary>지금 펼친 쪽. 0 이 첫 장. 왼쪽이 <c>spread*2</c>, 오른쪽이 <c>spread*2+1</c>.</summary>
    public int Spread { get; private set; }

    public int PageCount => Mathf.Max(2, art.Length > 0 ? art.Length : BlankPages);
    public int Spreads => Mathf.Max(1, Mathf.CeilToInt(PageCount / 2f));

    /// <summary>화면에 뜨는 한 줄.</summary>
    public string Action => turning != null
        ? "넘기는 중"
        : $"환웅의 발자취  {Spread + 1} / {Spreads}";

    Transform turning;
    float turnAt = -99f;
    Vector3 spine, hinge;

    void Start()
    {
        pageL = Find("Page_Left");
        pageR = Find("Page_Right");
        if (pageL == null || pageR == null)
        {
            // ★ 여기서 조용히 죽지 않는다. 부품 이름이 바뀌면 «버튼이 안 먹는» 것으로만
            // 보이는데, 그건 이 프로젝트에서 제일 오래 헤매는 증상이야.
            Debug.LogWarning("[기록첩] Page_Left / Page_Right 를 못 찾았다 — "
                           + "01_Footprints_Album.fbx 의 부품 이름이 바뀌었는지 확인해라.");
            enabled = false;
            return;
        }

        skinL = pageL.GetComponent<Renderer>();
        skinR = pageR.GetComponent<Renderer>();

        // 폴더에 있는 만큼이 장수. 이름 순서대로 — 파일을 01_, 02_ 로 지으면 그대로 순서다
        art = Resources.LoadAll<Texture2D>(PageFolder);
        System.Array.Sort(art, (a, b) => string.CompareOrdinal(a.name, b.name));

        // ★ <b>페이지 전용 재질</b>. 종이 재질(Memorial_E4D8BC)을 그대로 쓰면 책상·기둥까지
        // 같이 바뀐다 — 애셋 README 도 같은 것을 경고하고 있다.
        blank = new Material(Surface.Lit()) { name = "AlbumPage_Blank" };
        if (blank.HasProperty("_BaseColor")) blank.SetColor("_BaseColor", new Color32(0xFA, 0xF3, 0xE2, 0xFF));
        if (blank.HasProperty("_Smoothness")) blank.SetFloat("_Smoothness", 0.06f);

        MeasureSpine();
        Show(0);

        Debug.Log($"[기록첩] 낙서 {art.Length}장 · {Spreads}쪽 · 책등 {spine} · 넘김축 {hinge}");
    }

    void OnDestroy()
    {
        if (Nearest == this) Nearest = null;
    }

    Transform Find(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>
    /// 책등과 넘김축을 <b>잰다.</b> 두 장의 한가운데가 책등이고, 넘김축은 거기서
    /// <b>수평으로 가로지르는</b> 방향이다 — 모델이 어느 쪽을 보고 서 있든 맞는다.
    /// </summary>
    void MeasureSpine()
    {
        Vector3 l = skinL != null ? skinL.bounds.center : pageL.position;
        Vector3 r = skinR != null ? skinR.bounds.center : pageR.position;

        spine = (l + r) * 0.5f;

        Vector3 across = r - l;
        across.y = 0f;
        if (across.sqrMagnitude < 1e-6f) across = transform.right;
        hinge = Vector3.Cross(Vector3.up, across.normalized).normalized;
    }

    Material SkinFor(int page)
    {
        if (art.Length == 0) return blank;
        var tex = art[((page % art.Length) + art.Length) % art.Length];

        if (skins.TryGetValue(tex, out var hit) && hit != null) return hit;

        var m = new Material(Surface.Lit()) { name = "AlbumPage_" + tex.name };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.06f);
        skins[tex] = m;
        return m;
    }

    void Show(int spread)
    {
        Spread = ((spread % Spreads) + Spreads) % Spreads;
        if (skinL != null) skinL.sharedMaterial = SkinFor(Spread * 2);
        if (skinR != null) skinR.sharedMaterial = SkinFor(Spread * 2 + 1);
    }

    /// <summary>E 를 눌렀을 때. 넘기는 중에는 안 받는다 — 연타로 책이 겹치면 그건 고장이야.</summary>
    public void Turn()
    {
        if (turning != null) return;
        StartTurn();
    }

    /// <summary>
    /// 오른쪽 장을 복제해 책등을 축으로 180도 돌린다.
    ///
    /// ★ <b>등 맞대어 두 장</b>을 붙인다. 쿼드는 한쪽 면만 있어서 한 장만 돌리면
    /// <b>중간에 사라진다</b> — 뒤집히는 순간 뒷면을 보게 되니까. 두 장이면 앞면에 지금 장,
    /// 뒷면에 다음 장이 붙어서 <b>실제 종이와 같은 순서</b>로 보인다.
    /// </summary>
    void StartTurn()
    {
        var pivot = new GameObject("AlbumTurn").transform;
        pivot.SetParent(transform, true);
        pivot.position = spine;
        pivot.rotation = Quaternion.identity;

        var front = Instantiate(pageR.gameObject, pageR.position, pageR.rotation, pivot);
        front.name = "Leaf_Front";
        Strip(front);
        var fr = front.GetComponent<Renderer>();
        if (fr != null) fr.sharedMaterial = SkinFor(Spread * 2 + 1);          // 지금 오른쪽 장

        // 뒷면 — 같은 자리에서 <b>가로축으로 뒤집어</b> 반대쪽을 보게 한다
        var back = Instantiate(pageR.gameObject, pageR.position, pageR.rotation, pivot);
        back.name = "Leaf_Back";
        Strip(back);
        back.transform.RotateAround(back.transform.position, hinge, 180f);
        var br = back.GetComponent<Renderer>();
        if (br != null) br.sharedMaterial = SkinFor((Spread + 1) * 2);        // 다음 왼쪽 장이 될 것

        turning = pivot;
        turnAt = Time.time;

        // 넘어가는 동안 오른쪽 장은 <b>비워 둔다</b> — 안 그러면 종이가 두 장으로 보인다
        if (skinR != null) skinR.sharedMaterial = blank;
    }

    static void Strip(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
    }

    void Update()
    {
        Reaching();
        if (turning == null) return;

        float t = Mathf.Clamp01((Time.time - turnAt) / TurnSeconds);
        // 시작과 끝을 눅여야 «종이» 지, 등속으로 돌면 그건 «판때기» 다
        float angle = Mathf.SmoothStep(0f, 180f, t);
        turning.rotation = Quaternion.AngleAxis(angle, hinge);

        if (t < 1f) return;

        Destroy(turning.gameObject);
        turning = null;
        Show(Spread + 1);
    }

    /// <summary>
    /// «제일 가까운 것» 겨루기. <see cref="MinigameSpot"/> 과 <b>같은 한 프레임 늦은 공개</b>다 —
    /// 스크립트 실행 순서에 기대면 HUD 가 반쪽 결과를 읽는다(2026-09-21 화장실 칸 문).
    /// </summary>
    void Reaching()
    {
        if (frameStamp != Time.frameCount)
        {
            frameStamp = Time.frameCount;
            Nearest = pending;
            NearestScore = pending != null ? pendingScore : float.MaxValue;
            pending = null;
            pendingScore = float.MaxValue;
            nearestDistance = float.MaxValue;
        }

        Transform who = visitor != null ? visitor
                      : (Camera.main != null ? Camera.main.transform : null);
        if (who == null) return;

        Vector3 at = skinR != null ? skinR.bounds.center : transform.position;
        if (!Reach.Score(who, at, range, out float d)) return;
        if (d >= nearestDistance) return;

        nearestDistance = d;
        pendingScore = d;
        pending = this;
    }
}
