using UnityEngine;

/// <summary>
/// 카트 뒤로 나오는 <b>태엽 김</b>. 캐릭터 색으로 나와서 <b>누가 누군지 뒤에서도 보인다.</b>
///
/// 유저 아이디어(2026-09-17): *"각 차마다 배기가스가 자기 색으로 나온다든지."*
/// 레이싱 게임에서 이게 실제로 하는 일이 두 가지야 —
/// <b>속도가 눈에 보이고</b>(뒤에 남는 자국이 길수록 빠르다), <b>순위가 눈에 보인다</b>
/// (앞차 색만 봐도 누군지 안다). 지금은 카트 넷이 뒤에서 보면 거의 똑같이 생겼거든.
///
/// 무인 모형 카트라 배기가스는 설정에 안 맞는다. <b>태엽 감긴 장난감에서 나는 김</b>으로 친다 —
/// 그래서 색이 캐릭터 색이어도 이상하지 않고, 부스트 때 확 뿜는 것도 설명이 된다.
///
/// 프리팹을 안 쓴다. <see cref="KartSkin"/> 이 카트를 갈아입힐 때 색만 바꿔 주면 되니까
/// 코드로 만드는 게 싸고, 씬에 저장될 게 없어서 씬을 다시 구울 일도 없다.
/// </summary>
[RequireComponent(typeof(KartController))]
public class KartExhaust : MonoBehaviour
{
    [Tooltip("이 속도(㎞/h)를 넘어야 김이 난다")]
    public float startsAt = 12f;

    [Tooltip("최고 속도에서 초당 몇 알")]
    public float topRate = 46f;

    [Tooltip("부스트 중에는 몇 배로")]
    public float boostMultiply = 3.2f;

    KartController kart;
    ParticleSystem puff;
    ParticleSystem.EmissionModule emission;
    ParticleSystem.MainModule main;

    void Awake()
    {
        kart = GetComponent<KartController>();
        Build();
    }

    /// <summary><see cref="KartSkin"/> 이 카트를 갈아입힐 때 불러준다.</summary>
    public void SetColor(Color color)
    {
        if (puff == null) Build();

        // 알갱이는 <b>연하게</b> 둔다. 진하면 뒷차 시야를 가려서 게임이 불친절해진다.
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(color.r, color.g, color.b, 0.55f),
            new Color(color.r * 0.7f + 0.3f, color.g * 0.7f + 0.3f, color.b * 0.7f + 0.3f, 0.18f));
    }

    void Build()
    {
        if (puff != null) return;

        var go = new GameObject("Exhaust");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.18f, -0.78f);   // 뒤 범퍼 아래
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 뒤로 뿜는다

        puff = go.AddComponent<ParticleSystem>();
        puff.Stop();

        main = puff.main;
        main.startLifetime = 0.55f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.gravityModifier = -0.12f;          // 살짝 뜬다. 김이니까
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // 카트를 안 따라다녀야 자국이 남는다
        main.maxParticles = 120;

        emission = puff.emission;
        emission.rateOverTime = 0f;

        var shape = puff.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.08f;

        var life = puff.sizeOverLifetime;
        life.enabled = true;
        life.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.6f));

        var fade = puff.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.25f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = PuffMaterial();

        SetColor(Color.white);
        puff.Play();
    }

    void LateUpdate()
    {
        if (kart == null || puff == null) return;

        float speed = Mathf.Abs(kart.SpeedKph);
        float top = Mathf.Max(1f, kart.maxSpeed * 3.6f);
        float t = Mathf.InverseLerp(startsAt, top, speed);

        float rate = topRate * t;
        if (kart.IsBoosting) rate *= boostMultiply;
        emission.rateOverTime = rate;
    }

    // 알갱이 하나짜리 재질. URP 의 기본 파티클 셰이더를 쓰고, 없으면 조용히 포기한다.
    static Material shared;

    static Material PuffMaterial()
    {
        if (shared != null) return shared;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        shared = new Material(shader) { name = "KartPuff", hideFlags = HideFlags.HideAndDontSave };
        shared.SetFloat("_Surface", 1f);            // Transparent
        shared.SetFloat("_Blend", 0f);              // Alpha
        shared.renderQueue = 3000;
        return shared;
    }
}
