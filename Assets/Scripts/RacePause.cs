using UnityEngine;

/// <summary>
/// <b>레이스 일시정지.</b> 2026-09-18 유저: *"ESC 누르면 속력이 0으로 줄어드는데 일시정지
/// 같은 느낌이니까 속력 그대로 유지하고 시간도 계속 흐르게 두지 말고."*
///
/// ★ 처음에 <c>Time.timeScale = 0</c> 으로 했다가 <b>카트가 트랙 밑으로 빠졌다.</b>
/// 이 카트는 바닥을 <see cref="KartController.ApplySuspension"/> 의 레이캐스트로 «밀어 올려»
/// 떠 있는 거라, 물리가 멈추면 받쳐주던 힘도 같이 멈춘다. 게다가 timeScale 은
/// <b>씬 전체</b>에 걸려서 곰 NPC·문·연출까지 다 얼어붙고, 푸는 자리를 하나라도 빠뜨리면
/// 게임이 멈춘 채로 남는다.
///
/// 그래서 <b>물리를 멈추는 대신 카트를 재운다</b>: 속도를 적어 두고 리지드바디를
/// 키네마틱으로 바꾼다. 키네마틱은 중력도 안 받고 그 자리에 가만히 있으니
/// <b>떨어질 수가 없고</b>, 풀 때 적어둔 속도를 그대로 돌려주니 <b>속력이 보존된다.</b>
///
/// 이 프로젝트의 <see cref="RaceCountdown"/> 과 같은 사고방식이야 —
/// 엔진을 세우는 게 아니라 <b>값이 들어가는 자리</b>를 막는다.
/// </summary>
public static class RacePause
{
    public static bool On { get; private set; }

    struct Sleeping
    {
        public Rigidbody rb;
        public Vector3 velocity;
        public Vector3 spin;
        public bool wasKinematic;
    }

    static readonly System.Collections.Generic.List<Sleeping> sleeping =
        new System.Collections.Generic.List<Sleeping>();

    public static void Set(bool paused)
    {
        if (paused == On) return;
        On = paused;

        if (paused) Sleep();
        else Wake();
    }

    static void Sleep()
    {
        sleeping.Clear();

        // <b>꺼진 카트까지 찾을 필요는 없다</b> — 안 달리는 카트는 멈출 것도 없으니까.
        foreach (var kart in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
        {
            var rb = kart.GetComponent<Rigidbody>();
            if (rb == null) continue;

            sleeping.Add(new Sleeping
            {
                rb = rb,
                velocity = rb.linearVelocity,
                spin = rb.angularVelocity,
                wasKinematic = rb.isKinematic,
            });

            // ★ <b>키네마틱으로 바꾸는 순간 유니티가 속도를 0 으로 지운다.</b> 그 전에
            // 카트에게 «네 속도는 이거였다» 를 알려줘야 속도계가 0 으로 안 떨어진다.
            kart.RememberVelocityForPause(rb.linearVelocity);
            rb.isKinematic = true;
        }
    }

    static void Wake()
    {
        foreach (var s in sleeping)
        {
            if (s.rb == null) continue;          // 그 사이에 씬이 바뀌었을 수 있다
            s.rb.isKinematic = s.wasKinematic;
            if (!s.wasKinematic)
            {
                s.rb.linearVelocity = s.velocity;
                s.rb.angularVelocity = s.spin;
            }
        }
        sleeping.Clear();
    }

    /// <summary>씬을 옮길 때 안전망. 멈춘 채로 넘어가면 다음 씬이 얼어 있다.</summary>
    public static void Clear()
    {
        On = false;
        sleeping.Clear();
    }
}
