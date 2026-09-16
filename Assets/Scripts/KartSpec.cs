using UnityEngine;

/// <summary>
/// 카트 네 대의 제원표. 레이싱 게임이 차 고르는 화면에 붙여두는 그 숫자들이야.
///
/// <b>어느 쪽이 유리한지 적지 않는다.</b> 네 대 전부 <b>무언가를 내주고 무언가를 얻는다</b> —
/// 가벼우면 빨리 붙고 빨리 밀리고, 무거우면 늦게 붙고 안 밀린다. 그래서 "정답 카트" 가 없고,
/// 플레이어가 숫자를 보고 스스로 고르게 된다. 설명을 붙이는 순간 그 재미가 사라져.
///
/// 차이는 기준값의 <b>±10% 안쪽</b>으로 묶었다. 더 벌리면 한 대만 정답이 되고,
/// 네 대 균형을 다시 잡는 데 대사 쓸 시간을 다 쓰게 된다.
///
/// 차 이름·소개글은 아직 안 넣는다(2026-09-16 유저). 인물 모델링이 끝나고
/// 성격이 정해진 뒤에 붙일 자리만 비워뒀어 — <see cref="Spec.tagline"/>.
/// </summary>
public static class KartSpec
{
    public struct Spec
    {
        public string castId;
        /// <summary>차체 중량(kg). 부딪혔을 때 누가 밀리는지를 정한다.</summary>
        public float mass;
        /// <summary>최고 속도(m/s).</summary>
        public float topSpeed;
        /// <summary>가속(m/s²) — 멈춘 데서 붙는 속도.</summary>
        public float acceleration;
        /// <summary>접지력. 높으면 코너에서 안 미끄러지고, 낮으면 드리프트가 쉽게 걸린다.</summary>
        public float grip;
        /// <summary>나중에 넣을 한 줄 소개. 지금은 전부 비어 있다.</summary>
        public string tagline;
    }

    // 기준값(KartController 기본값): 중량 13.0 · 최고 17.0 · 가속 22.0 · 접지 16.0
    static readonly Spec[] All =
    {
        new Spec { castId = "이감", mass = 12.4f, topSpeed = 17.3f, acceleration = 22.8f, grip = 15.4f },
        new Spec { castId = "시우", mass = 14.2f, topSpeed = 16.3f, acceleration = 20.6f, grip = 17.5f },
        new Spec { castId = "세운", mass = 13.1f, topSpeed = 17.0f, acceleration = 21.7f, grip = 16.3f },
        new Spec { castId = "세진", mass = 11.7f, topSpeed = 18.0f, acceleration = 23.4f, grip = 14.6f },
    };

    public static bool TryGet(string castId, out Spec spec)
    {
        foreach (var s in All)
            if (s.castId == castId) { spec = s; return true; }

        spec = default;
        return false;
    }

    /// <summary>제원을 0~1 막대로. 화면에 그릴 때만 쓴다 — 판정에는 안 쓴다.</summary>
    public static float Bar(float value, float low, float high)
        => Mathf.InverseLerp(low, high, value);

    public static float MassBar(Spec s)  => Bar(s.mass, 11.0f, 15.0f);
    public static float SpeedBar(Spec s) => Bar(s.topSpeed, 15.8f, 18.5f);
    public static float AccelBar(Spec s) => Bar(s.acceleration, 19.8f, 24.0f);
    public static float GripBar(Spec s)  => Bar(s.grip, 14.0f, 18.2f);

    /// <summary>
    /// 이 카트를 해당 캐릭터의 것으로 만든다. <see cref="KartSkin"/> 이 모델을 고를 때 같이 부른다.
    /// 제원이 없는 캐릭터(악당 둘)는 기본값 그대로 둔다.
    /// </summary>
    public static void ApplyTo(KartController kart, string castId)
    {
        if (kart == null || !TryGet(castId, out var spec)) return;

        kart.maxSpeed = spec.topSpeed;
        kart.acceleration = spec.acceleration;
        kart.gripNormal = spec.grip;

        // 중량은 리지드바디 쪽이다. 카트를 미는 힘은 ForceMode.Acceleration 이라 질량과 무관하고,
        // 그래서 무게가 실제로 바뀌는 건 **부딪혔을 때 누가 밀리느냐** 뿐이야. 그게 맞기도 하고.
        var rb = kart.GetComponent<Rigidbody>();
        if (rb != null) rb.mass = spec.mass;
    }
}
