using UnityEngine;

/// <summary>
/// 효과음 한 방씩. <see cref="Music"/> 과 같은 꼴로 <b>스스로 씬에 들어오고 씬을 넘어가도 산다.</b>
///
/// 2026-10-01 유저: *"문 열어주는 효과음을 네가 만들어주거나."* 받은 음원이 없어서
/// <b>합성해서 만들었다</b>(스크래치패드 <c>door_sfx.py</c>) — 나무 비비는 소리 + 문틀 저역 +
/// 끝에서 «탁». 하나만 있으면 잡음이고 셋이 겹쳐야 «물건이 움직였다» 로 들린다.
///
/// 크기는 <b>음악 슬라이더 하나로 같이</b> 움직인다. 슬라이더를 둘로 나누면 첫 화면 메뉴가
/// 길어지고, 지금 효과음이 하나뿐이라 따로 맞출 일이 없다. 다만 효과음은 음악보다
/// <b>위에 떠 있어야</b> 들리니 1.7배를 먹인다 — 음악은 깔리는 것이고 효과음은 사건이다.
/// </summary>
public class Sfx : MonoBehaviour
{
    static Sfx live;
    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (live != null) return;
        var go = new GameObject("Sfx");
        DontDestroyOnLoad(go);
        live = go.AddComponent<Sfx>();
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;    // 2D — 카메라가 바뀌어도 소리는 그대로
    }

    /// <summary><c>Assets/Resources/Sfx/</c> 의 파일 이름. 없으면 <b>조용히 넘어간다.</b></summary>
    public static void Play(string name, float gain = 1f, bool duckMusic = true)
    {
        if (live == null || live.source == null) return;
        var clip = Resources.Load<AudioClip>("Sfx/" + name);
        if (clip == null) return;

        // ★ 말소리·알림이 나는 동안 음악이 비켜 준다(2026-10-02).
        //   소리 길이 + 0.35초 — 꼬리가 사라지기 전에 음악이 올라오면 묻힌다.
        if (duckMusic) Music.Duck(clip.length + 0.35f);

        live.source.PlayOneShot(clip, Mathf.Clamp01(Music.Volume * 1.7f) * Mathf.Clamp01(gain));
    }
}
