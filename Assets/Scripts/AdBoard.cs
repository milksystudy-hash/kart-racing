using UnityEngine;

/// <summary>
/// 골든베어 리조트 <b>광고 입간판 — 들이받으면 부서진다.</b>
///
/// 2026-09-17 에 들어왔다. 강사님 피드백이 "게임이 재미없고, 정치인을 때리는 것 같은 기믹이
/// 있어야 한다" 였고 그게 맞는 지적이야. 여덟 임무가 전부 <b>발판 밟기·안 긁기·시간 안에</b>
/// 였는데, 그 중 어느 것도 박물관이나 철거와 관계가 없었다. 그래서 진지하지도 병맛도 아니었어.
///
/// 이건 코스를 도는 것과 <b>이야기</b>를 처음으로 붙인다. 부수는 게 악당의 광고니까.
/// 기획서 §4.4 가 시킨 금색+자홍색이 드디어 장식이 아니라 표적이 된다.
///
/// <b>충돌체가 아니라 트리거다.</b> 부딪혀도 벽 충돌로 안 세지고 카트가 튕기지도 않는다 —
/// 판자를 뚫고 지나가는 느낌이어야지, 벽을 받는 느낌이면 안 된다. 그래서 경고 띠도 없다
/// (띠가 있으면 부딪히는 것, 없으면 장식 — 이건 세 번째, <b>부수는 것</b>이야).
/// </summary>
public class AdBoard : MonoBehaviour
{
    [Tooltip("이 속도보다 느리면 안 부서진다. 서서 밀면서 지우는 걸 막는다")]
    public float minSpeedKph = 12f;

    [Tooltip("부서지면 꺼질 부분. 비워두면 첫 자식")]
    public Transform visual;

    [Tooltip("부서질 때 튀는 조각 색")]
    public Color pieceColor = new Color32(0xC9, 0xA2, 0x27, 0xFF);

    public bool Broken { get; private set; }

    /// <summary>
    /// <b>이 판에서 부순 누적 개수.</b> 바퀴마다 간판이 되살아나므로 지금 부서져 있는
    /// 개수(<see cref="BrokenCount"/>)와 다르다.
    ///
    /// 2026-09-17 유저: *"한 바퀴 돌 때 8개를 미리 다 부수는 사람이 있다. 바퀴마다
    /// 되살아나되 부순 횟수는 임무가 끝날 때까지 안 지워지게, 3바퀴에 8×3=24개가 되도록."*
    /// 맞는 지적이야 — 안 그러면 첫 바퀴에 다 부수고 남은 두 바퀴는 그냥 완주가 된다.
    /// </summary>
    public static int Breaks { get; private set; }

    static AdBoard[] all;

    // 임무가 개수를 직접 세지 않게 한다 — 표를 고쳐서 간판을 늘려도 임무는 안 고친다.
    public static int CountInScene() => All().Length;

    public static int BrokenCount()
    {
        int n = 0;
        foreach (var a in All()) if (a != null && a.Broken) n++;
        return n;
    }

    /// <summary>판을 처음부터 다시 할 때. 누적 개수까지 지운다.</summary>
    public static void ResetAll()
    {
        Breaks = 0;
        foreach (var a in All()) if (a != null) a.Restore();
    }

    /// <summary>바퀴가 넘어갈 때. <b>누적 개수는 그대로 두고</b> 간판만 다시 세운다.</summary>
    public static void RestoreAll()
    {
        foreach (var a in All()) if (a != null) a.Restore();
    }

    // BoostPad 와 같은 방식. <b>OnEnable 로 모으면 에디터에서는 안 모인다</b> —
    // 에디터는 Awake/OnEnable 을 안 돌려서 씬을 구운 직후 검사가 0개로 나온다.
    static AdBoard[] All()
    {
        if (all == null || all.Length == 0 || all[0] == null)
            // 꺼진 것도 담는다 — 게이트가 꺼놓은 뒤에 되살리려면 목록에 있어야 한다.
            all = FindObjectsByType<AdBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return all;
    }

    void Reset() => Bind();

    void Awake()
    {
        Bind();

        // 게이트가 없는 씬(옛날에 구운 것)에서도 스스로 꺼진다. <b>물건이 스스로 판단할 수
        // 있어야</b> "씬을 다시 구워야만 고쳐지는" 수정이 안 된다 — 이 프로젝트에서 다섯 번째야
        // (카트 중복 · 벽 부딪힘 · AI · 장애물 · 광고판).
        if (!AdSignGate.ShouldStand && FindFirstObjectByType<AdSignGate>() == null)
            gameObject.SetActive(false);
    }

    void Bind()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Broken) return;

        // AI 가 부수면 플레이어의 표적이 사라진다. 사람만 부순다.
        var kart = other.GetComponentInParent<KartController>();
        if (kart == null || kart.GetComponent<PlayerKart>() == null) return;
        if (Mathf.Abs(kart.SpeedKph) < minSpeedKph) return;

        Break(kart.transform.forward);
    }

    void Break(Vector3 away)
    {
        Broken = true;
        Breaks++;
        Sfx.Play("AdBreak", 1f, duckMusic: false);
        if (visual != null) visual.gameObject.SetActive(false);

        var box = GetComponent<Collider>();
        if (box != null) box.enabled = false;

        Scatter(away);
    }

    void Restore()
    {
        Broken = false;
        if (visual != null) visual.gameObject.SetActive(true);

        var box = GetComponent<Collider>();
        if (box != null) box.enabled = true;
    }

    /// <summary>
    /// 조각 다섯 개가 튄다. <b>그냥 사라지면 부순 느낌이 안 난다</b> — 지나가다 못 보고
    /// 놓친 것과 구분이 안 돼. 튀는 걸 봐야 "내가 저걸 부쉈다" 가 된다.
    /// </summary>
    void Scatter(Vector3 away)
    {
        var material = FlatMaterial.Get(pieceColor);

        for (int i = 0; i < 5; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "AdPiece";
            piece.transform.SetPositionAndRotation(
                transform.position + Vector3.up * (0.6f + i * 0.18f) + Random.insideUnitSphere * 0.25f,
                Random.rotation);
            piece.transform.localScale = new Vector3(0.26f, 0.26f, 0.05f);
            piece.GetComponent<Renderer>().sharedMaterial = material;

            // 조각끼리도, 카트와도 안 부딪히게 — 부순 다음에 걸리적거리면 최악이다
            var collider = piece.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var body = piece.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.linearVelocity = away * Random.Range(3f, 7f)
                                + Vector3.up * Random.Range(2.5f, 4.5f)
                                + Random.insideUnitSphere * 1.5f;
            body.angularVelocity = Random.insideUnitSphere * 12f;

            Destroy(piece, 2.5f);
        }
    }
}
