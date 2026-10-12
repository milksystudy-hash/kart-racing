using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ★★ <b>방이 살아난다</b> (2026-10-12).
///
/// 수연: *"레이싱을 다 하고 난 뒤에는 문을 열면 연출이 달라지게 할 수 있어?
/// 모든 재봉틀이 플레이어가 다가가는 순간 움직인다든가, 태엽이 막 돌아간다든가.
/// 지금은 다 정지한 느낌."* <b>맞는 말이고, 그게 이 게임이 이미 쓰는 문법이다</b> —
/// 캠퍼스 폐과 딱지 · 문 판자 · 전시실 먼지 덮개가 전부 «줄어드는 연출» 이었는데,
/// 실내만 <b>통째로 멈춰</b> 있었다.
///
/// ── 규칙 셋 ─────────────────────────────────────────────────────────────
/// 1. <b>결승을 이기기 전에는 아무것도 안 움직인다.</b> 그래야 «멈춰 있던 곳이
///    다시 돌아간다» 가 된다. 처음부터 돌아가면 그건 그냥 배경이다.
/// 2. <b>가까이 가야 움직인다</b>(기본 9m). 멀리서도 다 돌아가면 눈이 피곤하고,
///    «내가 들어오니까 켜졌다» 라는 느낌이 사라진다.
/// 3. <b>이름으로 찾는다.</b> 열세 관의 부품을 하나씩 적으면 소품을 하나 더할 때마다
///    여기도 고쳐야 한다. <see cref="Motions"/> 표에 낱말 하나를 더하면
///    <b>모든 관에서 같이 움직인다.</b>
///
/// ⚠ 재질을 안 건드린다. 발광 면은 <c>.mat</c> 에셋이라 런타임에 쓰면 디스크 파일이
///   바뀌고 다른 씬까지 따라간다(<see cref="GalleryLights"/> 에서 정한 규칙).
///   <b>트랜스폼만</b> 움직인다 — 카트 바퀴 · 곰 팔 · 스피커 귀와 같은 방식이다.
/// </summary>
public class RoomLife : MonoBehaviour
{
    public enum Kind { 돌기, 끄덕, 흔들, 떨기 }

    /// <summary>
    /// 이름에 이 낱말이 들어가면 그렇게 움직인다. <b>여기 한 줄이 열세 관에 다 듣는다.</b>
    /// </summary>
    /// <summary>
    /// 이름에 이 낱말이 들어가면 그렇게 움직인다.
    ///
    /// ★★ 2026-10-12 — <b>표를 절반으로 줄였다.</b> 수연: *"운전대가 움직이면 안 되잖아.
    /// 곰솥관에서 뭔가 위아래로 날아다닌다."* <b>내가 너무 넓게 잡았다.</b>
    /// <c>Wheel</c> 은 카트 바퀴와 운전대를 같이 잡고, <c>Car</c>·<c>Cart</c>·<c>Slider</c> 는
    /// 부엌 수레와 선반까지 잡는다. <b>한 낱말이 열세 관에 다 듣는다는 건
    /// 잘못 잡으면 열세 관이 다 망가진다는 뜻</b>이기도 하다.
    ///
    /// 남긴 것은 <b>그 낱말이 붙으면 반드시 움직이는 물건</b>뿐이다.
    /// 새 낱말을 더하기 전에 <b>그 이름을 쓰는 다른 물건이 없는지</b> 먼저 찾아봐라.
    /// </summary>
    static readonly (string word, Kind kind, float speed, float amount)[] Motions =
    {
        // 돌아가는 것 — 이 이름이 붙으면 돌지 않는 물건이 없다
        ("Gear",       Kind.돌기, 52f,  0f),
        ("Spool",      Kind.돌기, 140f, 0f),
        ("Bobbin",     Kind.돌기, 160f, 0f),
        ("Vent",       Kind.돌기, 120f, 0f),
        ("Fan",        Kind.돌기, 200f, 0f),
        ("Turbine",    Kind.돌기, 90f,  0f),
        ("Pinwheel",   Kind.돌기, 150f, 0f),
        ("Turntable",  Kind.돌기, 18f,  0f),

        // 위아래로 — 재봉 바늘과 발판. <b>제 크기에 비례</b>해서 움직인다
        ("Needle",     Kind.끄덕, 7.2f, 0.30f),
        ("Pedal",      Kind.끄덕, 7.2f, 0.10f),
        ("StampHandle",Kind.끄덕, 1.5f, 0.18f),

        // 흔들리는 것 — 천 · 현수막 · 등 · 풍경
        ("Banner",     Kind.흔들, 0.8f, 5.5f),
        ("Flag",       Kind.흔들, 1.1f, 7f),
        ("Cloth",      Kind.흔들, 0.9f, 6f),
        ("Towel",      Kind.흔들, 0.8f, 4.5f),
        ("Curtain",    Kind.흔들, 0.7f, 4f),
        ("Chime",      Kind.흔들, 1.6f, 9f),
        ("Lantern",    Kind.흔들, 0.9f, 5f),
        ("Pendant",    Kind.흔들, 1.4f, 8f),

        // 자잘하게 떠는 것
        ("Drum",       Kind.떨기, 3.4f, 0.016f),
        ("Bell",       Kind.떨기, 5.0f, 0.012f),
    };

    /// <summary>
    /// ★ <b>이 낱말이 들어가면 무슨 일이 있어도 안 움직인다.</b>
    /// 위 표가 먼저 걸려도 여기서 걸러낸다 — <c>Steering_Wheel</c> 처럼
    /// «움직이는 낱말 + 안 움직이는 물건» 인 이름이 실제로 있다.
    /// </summary>
    static readonly string[] Never =
    {
        "Steering", "Structure", "Frame", "Base", "Stand", "Shelf", "Cabinet",
        "Wall", "Floor", "Ceiling", "Body", "Table", "Desk", "Panel", "Board",
    };

    [Tooltip("이 거리 안에 들어오면 움직이기 시작한다")]
    public float wake = 9f;

    class Part
    {
        public Transform t;
        public Kind kind;
        public float speed, amount, phase;
        public Vector3 home;
        public Quaternion rest;
        public Vector3 scale;

        /// <summary>돌아갈 축(로컬). <b>재서 정한다</b> — 손으로 적으면 모델마다 틀린다.</summary>
        public Vector3 axis;

    }

    readonly List<Part> parts = new();
    Transform who;
    float life;          // 0 = 멈춤 · 1 = 돌아감

    void Start()
    {
        // ★ 결승을 이기기 전에는 아예 안 찾는다 — 공짜로 꺼진다.
        if (!GrandFinal.FreeRun) { enabled = false; return; }

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t == transform) continue;
            bool banned = false;
            foreach (var n in Never)
                if (t.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0) { banned = true; break; }
            if (banned) continue;

            foreach (var m in Motions)
            {
                if (t.name.IndexOf(m.word, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                parts.Add(new Part
                {
                    t = t, kind = m.kind, speed = m.speed,
                    phase = Random.value * 10f,
                    home = t.localPosition, rest = t.localRotation, scale = t.localScale,
                    axis = SpinAxis(t),
                    // ★ 흔드는 양을 <b>미터로 적지 않는다.</b> 0.05m 는 재봉 바늘에는 맞고
                    //   작은 소품에는 «날아다니는» 양이다. <b>제 크기의 몇 할</b>로 잡는다.
                    amount = m.kind == Kind.끄덕 ? m.amount * Height(t) : m.amount,
                });
                break;                      // 한 부품은 한 가지 움직임만
            }
        }

        if (parts.Count == 0) { enabled = false; return; }
    }

    void Update()
    {
        var player = PlayerHere();
        float want = 0f;
        if (player != null)
        {
            float d = Vector3.Distance(player.position, transform.position);
            want = d < wake ? 1f : 0f;
        }

        // 확 켜지지 않는다 — 1.2초에 걸쳐 돌기 시작한다. 그래야 «깨어난다» 로 읽힌다.
        life = Mathf.MoveTowards(life, want, Time.deltaTime / 1.2f);
        if (life <= 0.001f) return;

        float t = Time.time;
        foreach (var p in parts)
        {
            if (p.t == null) continue;
            switch (p.kind)
            {
                case Kind.돌기:
                    p.t.Rotate(p.axis, p.speed * life * Time.deltaTime, Space.Self);
                    break;


                case Kind.끄덕:
                    p.t.localPosition = p.home +
                        Vector3.up * (Mathf.Sin((t + p.phase) * p.speed) * p.amount * life);
                    break;

                case Kind.흔들:
                    p.t.localRotation = p.rest * Quaternion.Euler(
                        0f, 0f, Mathf.Sin((t + p.phase) * p.speed * 2f) * p.amount * life);
                    break;

                case Kind.떨기:
                    float s = 1f + Mathf.Sin((t + p.phase) * p.speed) * p.amount * life;
                    p.t.localScale = new Vector3(p.scale.x * s, p.scale.y / s, p.scale.z * s);
                    break;
            }
        }
    }

    /// <summary>
    /// ★ <b>돌아갈 축은 재서 정한다.</b> 수연: *"톱니가 천장을 통과해 마구 돌아간다."*
    ///
    /// 납작한 원반을 <b>제 축이 아닌 쪽</b>으로 돌리면 프로펠러처럼 휘저어서 벽을 뚫는다.
    /// 원반은 <b>제일 얇은 축</b>이 곧 돌아갈 축이다 — 모델이 어떻게 생겼든 안 틀린다.
    /// </summary>
    static Vector3 SpinAxis(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        Vector3 s = r != null ? t.InverseTransformVector(r.bounds.size) : Vector3.one;
        s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

        if (s.x <= s.y && s.x <= s.z) return Vector3.right;
        if (s.y <= s.x && s.y <= s.z) return Vector3.up;
        return Vector3.forward;
    }


    /// <summary>그 부품의 키(로컬). 흔드는 양을 여기서 뽑는다.</summary>
    static float Height(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        if (r == null) return 0.2f;
        return Mathf.Max(0.02f, Mathf.Abs(t.InverseTransformVector(r.bounds.size).y));
    }

    /// <summary>
    /// 걸어다니는 몸. 캠퍼스는 <see cref="FirstPersonController"/> 하나뿐이라
    /// <b>한 번 찾아서 들고 있는다</b> — 열세 관이 매 프레임 씬을 훑으면 비싸다.
    /// </summary>
    Transform PlayerHere()
    {
        if (who != null && who.gameObject.activeInHierarchy) return who;
        var fp = Object.FindFirstObjectByType<FirstPersonController>();
        who = fp != null ? fp.transform : null;
        return who;
    }
}
