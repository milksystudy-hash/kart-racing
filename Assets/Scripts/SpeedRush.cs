using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 빨리 달릴 때 <b>화면 자체가 빨라 보이게</b> 한다. 렌즈 왜곡과 색수차를 속도에 맞춰 올린다.
///
/// 시야각(<see cref="KartCamera"/>)과 흔들림만으로는 부족했다 — 그 둘은 카트를 보는 방식이고,
/// 이건 <b>화면 가장자리</b>가 하는 일이야. 바깥쪽이 휘어지면서 색이 갈라지면 사람은 그걸
/// "빠르다" 로 읽는다. 레이싱 게임이 전부 쓰는 수법이고, 기하나 물리를 하나도 안 건드린다.
///
/// <b>일정 속도를 넘어야 켜진다.</b> 천천히 갈 때까지 화면이 휘면 멀미가 나고, 박물관을
/// 구경하는 장면이 망가진다. 지금은 최고 속도의 55% 부터.
///
/// 프로필을 <b>런타임에 새로 만든다</b> — MuseumLook.asset 을 건드리면 디스크의 파일이 바뀌어서
/// 다른 씬까지 따라간다. 여기서 만든 건 저장되지 않고 씬이 끝나면 사라진다.
/// </summary>
[DefaultExecutionOrder(60)]
public class SpeedRush : MonoBehaviour
{
    public KartController kart;

    // 2026-09-17 유저: "이펙트가 흩뿌려져서 정신이 사납다." 맞는 지적이야 —
    // 속도감 장치는 <b>느껴지되 보이면 안 된다.</b> 보이는 순간 화면 효과로 인식되고,
    // 그러면 빠른 게 아니라 눈이 피곤한 게 된다. 전부 절반 아래로 내리고 켜지는 문턱도 올렸다.
    [Tooltip("최고 속도의 몇 %부터 켜지는지. 높을수록 진짜 빠를 때만 나온다")]
    [Range(0f, 0.95f)] public float startsAt = 0.72f;

    [Tooltip("가장 셀 때의 렌즈 왜곡. 음수가 안쪽으로 빨려드는 방향")]
    public float maxDistortion = -0.11f;

    [Tooltip("가장 셀 때의 색수차. 세면 글자에 색테가 생겨서 HUD 가 지저분해진다")]
    public float maxAberration = 0.14f;

    [Tooltip("부스트 중에 더해지는 양(0~1)")]
    public float boostBonus = 0.3f;

    Volume volume;
    float rush;

    /// <summary>꺼질 때 남아 있던 효과를 지운다. 안 그러면 끈 순간의 화면이 얼어붙는다.</summary>
    public void Silence()
    {
        rush = 0f;
        if (volume != null) volume.weight = 0f;
    }

    void Awake()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;

        var distortion = profile.Add<LensDistortion>(true);
        distortion.intensity.overrideState = true;
        distortion.intensity.value = maxDistortion;

        var aberration = profile.Add<ChromaticAberration>(true);
        aberration.intensity.overrideState = true;
        aberration.intensity.value = maxAberration;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;   // MuseumLook 의 전역 볼륨(0)보다 위
        volume.profile = profile;
        volume.weight = 0f;
    }

    void OnEnable() => ScreenEffects.Apply();

    void LateUpdate()
    {
        if (kart == null || volume == null) return;

        float speed01 = Mathf.Clamp01(Mathf.Abs(kart.SpeedKph) / Mathf.Max(1f, kart.maxSpeed * 3.6f));

        float goal = Mathf.InverseLerp(startsAt, 1f, speed01);
        if (kart.IsBoosting) goal += boostBonus;
        goal = Mathf.Clamp01(goal);

        // 올라갈 때는 빠르게, 내려갈 때는 느리게. 부스트가 끝나는 순간 툭 꺼지면 어색하다.
        float rate = goal > rush ? 8f : 3.5f;
        rush = Mathf.Lerp(rush, goal, 1f - Mathf.Exp(-rate * Time.deltaTime));

        // 세기는 무게로만 조절한다 — 0 이면 효과가 아예 안 돌아서 공짜다.
        volume.weight = rush;
    }
}
