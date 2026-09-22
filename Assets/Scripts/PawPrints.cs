using UnityEngine;

/// <summary>
/// <b>걸으면 발자국이 남고 곧 사라진다.</b>
///
/// 2026-09-22 유저: *"로비 곰 발자국이 너무 더러워 보인다. 곰들이 지나가서 그런 거라면
/// 두세 발자국 걷고 곧바로 사라지는 연출로 만들어 주면 안 될까."*
///
/// 맞는 판단이다. 전에는 빌더가 바닥에 <b>27개를 영구히 찍어</b> 놨는데,
/// 안 사라지는 발자국은 «누가 지나갔다» 가 아니라 <b>«바닥 무늬»</b> 이고,
/// 무늬치고는 지저분하니 결국 «얼룩» 으로 읽힌다.
/// <b>생겼다 사라져야</b> 그게 발자국이다 — 그때만 «방금 저 곰이 지나갔구나» 가 된다.
///
/// <b>사라지는 방법은 «작아지기» 다.</b> 알파로 흐리게 하려면 반투명 재질이 필요하고,
/// 그러면 재질을 인스턴스로 떠야 해서 조각마다 드로우콜이 갈라진다. 바닥에 깔린
/// 2cm 짜리가 <b>줄어들어 없어지는 것</b>과 <b>흐려지는 것</b>은 눈으로 구분이 안 되는데,
/// 줄어드는 쪽은 <b>재질을 하나도 안 건드린다.</b>
///
/// <b>미리 만들어 두고 돌려 쓴다.</b> 곰 셋이 계속 걷는데 매번 만들고 지우면 쓰레기가 쌓인다 —
/// 카트 김(<see cref="KartExhaust"/>)이 파티클을 쓰는 것과 같은 이유야.
/// </summary>
public class PawPrints : MonoBehaviour
{
    [Tooltip("이만큼 걸을 때마다 한 발 찍는다(m)")]
    public float stride = 0.52f;

    [Tooltip("찍히고 이 시간에 걸쳐 사라진다(초)")]
    public float life = 1.9f;

    [Tooltip("동시에 남아 있는 최대 개수. 2~3 발자국만 보이게 작게 둔다")]
    public int pool = 4;

    [Tooltip("발자국 하나의 폭(m)")]
    public float size = 0.22f;

    [Tooltip("바닥에서 띄우는 높이. 바닥 줄눈(0.011)보다 위여야 지지직 안 거린다")]
    public float lift = 0.02f;

    [Tooltip("발자국 색")]
    public Color tint = new Color32(0x9A, 0x8E, 0x80, 0xFF);

    Transform[] paws;
    Vector3[] baseScale;
    float[] bornAt;
    int next;
    int side = 1;

    Vector3 lastFlat;
    float walked;

    void Start()
    {
        // 발자국은 <b>곰의 자식이 아니다.</b> 자식이면 곰을 따라다녀서 «발자국» 이 아니라
        // «발에 붙은 장식» 이 된다(카트 김을 World 공간으로 두는 것과 같은 이유).
        var root = new GameObject($"{name}_Paws").transform;

        paws = new Transform[pool];
        baseScale = new Vector3[pool];
        bornAt = new float[pool];

        var mat = FlatMaterial.Get(tint);
        for (int i = 0; i < pool; i++)
        {
            var paw = new GameObject($"Paw_{i}").transform;
            paw.SetParent(root, false);

            Pad(paw, "Sole", Vector3.zero, new Vector3(size, 0.004f, size * 1.2f), mat);
            for (int k = 0; k < 3; k++)
            {
                float a = (-26f + k * 26f) * Mathf.Deg2Rad;
                Pad(paw, $"Toe_{k}",
                    new Vector3(Mathf.Sin(a) * size * 0.62f, 0f, size * 0.78f + Mathf.Cos(a) * size * 0.12f),
                    new Vector3(size * 0.34f, 0.004f, size * 0.34f), mat);
            }

            baseScale[i] = Vector3.one;
            bornAt[i] = -99f;
            paw.gameObject.SetActive(false);
            paws[i] = paw;
        }

        lastFlat = Flat(transform.position);
    }

    static void Pad(Transform parent, string n, Vector3 at, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = at;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    void Update()
    {
        if (paws == null) return;

        // ── 걸은 만큼 찍는다 ──  시간이 아니라 <b>거리</b>로 세야
        // 제자리에서 도는 동안 발자국이 안 쌓인다.
        Vector3 now = Flat(transform.position);
        Vector3 step = now - lastFlat;
        walked += step.magnitude;
        lastFlat = now;

        if (walked >= stride && step.sqrMagnitude > 1e-6f)
        {
            walked = 0f;
            Place(now, step.normalized);
        }

        // ── 사라진다 ──
        for (int i = 0; i < paws.Length; i++)
        {
            if (!paws[i].gameObject.activeSelf) continue;

            float t = (Time.time - bornAt[i]) / Mathf.Max(0.1f, life);
            if (t >= 1f) { paws[i].gameObject.SetActive(false); continue; }

            // 처음엔 또렷하게 있다가 <b>뒤로 갈수록 빨리</b> 줄어든다 —
            // 일정한 속도로 줄면 «찌그러진다» 로 보이고, 늦게 훅 줄면 «사라진다» 로 읽힌다.
            float k = 1f - t * t;
            paws[i].localScale = baseScale[i] * k;
        }
    }

    void Place(Vector3 at, Vector3 forward)
    {
        var paw = paws[next];
        bornAt[next] = Time.time;
        next = (next + 1) % paws.Length;

        // 좌우를 번갈아. 한 줄로 찍으면 «점선» 이지 발자국이 아니다.
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        paw.position = at + right * (side * 0.12f) + Vector3.up * lift;
        paw.rotation = Quaternion.LookRotation(forward, Vector3.up)
                     * Quaternion.Euler(0f, side * 6f, 0f);
        paw.localScale = baseScale[next == 0 ? paws.Length - 1 : next - 1];
        side = -side;

        paw.gameObject.SetActive(true);
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
