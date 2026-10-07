using UnityEngine;

/// <summary>
/// ★★ 2026-10-07 — <b>이 게임에 엔진 소리가 없었다.</b> 음악과 단발 효과음은 있는데
/// 달리는 동안 나는 소리가 없어서, 레이스가 <b>조용한 화면</b>이었다.
///
/// <see cref="Sfx.Play"/> 로는 안 된다 — 그건 <b>한 번 나고 끝나는</b> 소리고,
/// 엔진은 <b>계속 돌면서 속도를 따라가야</b> 한다. 그래서 전용 <c>AudioSource</c> 를 둔다.
///
/// <list type="bullet">
/// <item><b>소리를 안 만들고 «바꾼다».</b> 2초짜리 한 바퀴를 무한 반복하면서
///       <b>음높이와 크기만</b> 속도에 맞춰 움직인다. 음원이 하나로 끝난다.</item>
/// <item><b>음높이가 크기보다 중요하다.</b> 귀는 «커진 것» 보다 «높아진 것» 을
///       속도로 읽는다 — 크기만 올리면 가까워진 것으로 들린다.</item>
/// <item><b>부스트 중에는 한 단 더 올린다.</b> 태엽이 풀리는 순간이 귀에도 잡혀야 한다.</item>
/// <item><b>멈추면 같이 멈춘다</b>(<see cref="RacePause"/> · <see cref="RaceCountdown"/>).
///       물리를 세워 놓고 엔진만 돌면 그게 제일 어색하다.</item>
/// </list>
///
/// ★ <b>파일이 없으면 아무 일도 안 한다.</b> <c>Resources/Sfx/Engine.wav</c> 를 지우면
///   조용히 꺼지고, 넣으면 다시 난다 — 씬을 다시 구울 필요가 없다.
///
/// ★★ 2026-10-07 유저: *"엔진음이 비명 울리는 것 같다. 휴대폰 진동음이면 될 듯."* <b>맞다.</b>
///   처음엔 녹음에서 제일 <b>밝은 구간</b>(2500Hz)을 골라 놓고 음높이를 0.62~1.55 로
///   크게 훑었다 — 그러면 모터가 아니라 <b>비명</b>이 된다. 그리고 «휴대폰 진동» 같은 소리는
///   그 녹음에 아예 없었다.
///
///   그래서 <b>만들었다.</b> 100Hz 톱니에 <b>25Hz 떨림</b>을 입힌 1초짜리다 —
///   휴대폰 진동의 그 박자고, <b>무인 모형 카트</b>라는 설정에도 이쪽이 맞는다(2026-09-16).
///   음높이 폭도 0.80~1.34 로 좁혔다. <b>넓게 훑을수록 비명에 가까워진다.</b>
///
///   ★ 이음새를 겹쳐 지울 필요도 없다 — 쓰인 주파수(100·25·7·37·53·71Hz)가 전부
///   <b>1초에 정수 바퀴</b>라 <b>구조적으로</b> 완전 반복된다.
/// </summary>
[RequireComponent(typeof(KartController))]
public class KartEngine : MonoBehaviour
{
    [Tooltip("멈춰 있을 때의 음높이")]
    public float idlePitch = 0.80f;
    [Tooltip("최고 속도에서의 음높이")]
    public float topPitch = 1.34f;
    [Tooltip("부스트 중에 얹는 음높이")]
    public float boostPitch = 0.12f;

    [Tooltip("멈춰 있을 때의 크기")]
    public float idleVolume = 0.22f;
    [Tooltip("최고 속도에서의 크기")]
    public float topVolume = 0.78f;

    /// <summary>이 속도를 1.0 으로 친다(km/h). 카트 최고속이 대략 여기쯤이다.</summary>
    public float topSpeed = 95f;

    KartController kart;
    AudioSource src;

    void Awake()
    {
        kart = GetComponent<KartController>();

        var clip = Resources.Load<AudioClip>("Sfx/Engine");
        if (clip == null) { enabled = false; return; }

        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;      // 2D — 카메라가 바뀌어도 소리는 그대로(음악과 같은 규칙)
        src.volume = 0f;
        src.Play();
    }

    void Update()
    {
        if (src == null) return;

        // 멈춘 동안에는 소리도 멈춘다. 물리를 세워 놓고 엔진만 돌면 제일 어색하다.
        bool frozen = RacePause.On || RaceCountdown.Blocked;
        if (frozen)
        {
            if (src.isPlaying) src.Pause();
            return;
        }
        if (!src.isPlaying) src.UnPause();

        float v = Mathf.Clamp01(Mathf.Abs(kart.SpeedKph) / Mathf.Max(1f, topSpeed));

        float pitch = Mathf.Lerp(idlePitch, topPitch, v) + (kart.IsBoosting ? boostPitch : 0f);
        float vol = Mathf.Lerp(idleVolume, topVolume, v) * Mathf.Clamp01(Music.Volume * 1.7f);

        // 값을 그대로 쓰면 톡톡 끊긴다 — 귀가 그걸 «지직» 으로 듣는다
        src.pitch = Mathf.Lerp(src.pitch, pitch, 1f - Mathf.Exp(-9f * Time.deltaTime));
        src.volume = Mathf.Lerp(src.volume, vol, 1f - Mathf.Exp(-7f * Time.deltaTime));
    }

    /// <summary>
    /// 카트마다 붙인다. <b>씬을 다시 구울 필요가 없게</b> 스스로 들어온다 —
    /// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 다섯 번 겪은 뒤의 기본형.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnLoaded;
        Place();
    }

    static void OnLoaded(UnityEngine.SceneManagement.Scene s,
                         UnityEngine.SceneManagement.LoadSceneMode m) => Place();

    static void Place()
    {
        foreach (var k in FindObjectsByType<KartController>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
        {
            // ★ <b>플레이어 카트에만</b> 붙인다. AI 세 대까지 울리면 네 소리가 겹쳐
            //   어느 게 내 속도인지 알 수가 없다 — 속도계를 소리로 읽는 게 이것의 쓸모다.
            if (k.GetComponent<PlayerKart>() == null) continue;
            if (k.GetComponent<KartEngine>() == null) k.gameObject.AddComponent<KartEngine>();
        }
    }
}
