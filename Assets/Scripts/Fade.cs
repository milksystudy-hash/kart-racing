using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬이 바뀔 때 <b>까맣게 덮었다 걷는다.</b>
///
/// 2026-10-01 유저: *"문 열리고 2~3초 딜레이가 있더라."* 재 보니 내가 넣은 0.55초가 아니라
/// <b>씬 로딩 시간</b>이었다 — 캠퍼스는 3,200조각에 지붕·건물 열셋이라 그만큼 걸린다.
///
/// 로딩은 못 줄인다. 하지만 <b>멈춘 화면</b>과 <b>어두워지는 화면</b>은 완전히 다르게 읽힌다 —
/// 가만히 있으면 «렉» 이고, 어두워지면 «넘어가는 중» 이다. 숫자를 못 바꿀 때는
/// <b>그 시간이 무엇으로 보이는지</b>를 바꾼다.
///
/// ★ 스스로 들어오고 씬을 넘어가도 산다 — 「새 컴포넌트로 고치면 씬을 다시 구워야만
/// 고쳐진다」를 다섯 번 겪은 뒤의 기본형.
/// ★ <b>늘 맨 위에 그린다.</b> <c>GUI.depth</c> 를 제일 작게 둔다 — HUD 가 검은 막 위로
/// 올라오면 «화면이 깨진 것» 으로 보인다.
/// </summary>
public class Fade : MonoBehaviour
{
    const float OutTime = 0.28f;   // 덮는 시간 — 짧아야 «답답하다» 가 안 된다
    const float InTime = 0.42f;    // 걷는 시간 — 조금 길어야 «도착했다» 가 된다

    static Fade live;
    static int wanted = -1;

    float alpha;
    bool covering;
    float until;
    float holdUntil;

    /// <summary>지금 화면이 <b>완전히</b> 덮여 있나. 뒤에서 몰래 바꿀 수 있는 순간이다.</summary>
    public static bool Covered => live != null && live.alpha >= 0.995f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (live != null) return;
        var go = new GameObject("Fade");
        DontDestroyOnLoad(go);
        live = go.AddComponent<Fade>();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    /// <summary>
    /// 씬을 안 바꾸고 <b>잠깐 캄캄해졌다 걷힌다.</b>
    ///
    /// 2026-10-02 유저: *"게임 스타트 누르고 화면이 암전됐다가 이런 표지 있고
    /// 여기서 대화하는 건 어때."* 맞다 — 첫 화면에서 프롤로그로 <b>그냥 바뀌면</b>
    /// 메뉴가 사라지고 같은 자리에 다른 그림이 뜬 것뿐이라 «장면이 시작했다» 가 안 된다.
    /// 한 번 캄캄해지면 그 사이에 무대가 바뀐 게 되고, 연극의 암전과 같은 일을 한다.
    /// </summary>
    public static void Blink(float hold = 0.35f)
    {
        if (live == null || live.covering) return;
        wanted = -1;
        live.covering = true;
        live.until = Time.unscaledTime + OutTime;
        live.holdUntil = live.until + hold;
    }

    /// <summary>덮고 나서 그 씬으로 간다. 막이 없으면 그냥 바로 간다.</summary>
    public static void Load(int buildIndex)
    {
        if (live == null) { SceneManager.LoadScene(buildIndex); return; }
        if (live.covering) return;          // 두 번 눌러도 한 번만
        wanted = buildIndex;
        live.covering = true;
        live.until = Time.unscaledTime + OutTime;
        live.holdUntil = 0f;
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (live == null) return;
        live.covering = false;
        live.alpha = 1f;                    // 새 씬은 까만 데서 밝아진다
    }

    void Update()
    {
        if (covering)
        {
            alpha = Mathf.Clamp01(1f - (until - Time.unscaledTime) / OutTime);
            if (Time.unscaledTime < until) return;

            if (wanted >= 0)
            {
                int go = wanted;
                wanted = -1;
                alpha = 1f;
                SceneManager.LoadScene(go);
                return;
            }

            // 씬을 안 바꾸는 깜빡임 — 잠깐 들고 있다가 걷는다
            alpha = 1f;
            if (Time.unscaledTime >= holdUntil) covering = false;
            return;
        }
        if (alpha > 0f) alpha = Mathf.MoveTowards(alpha, 0f, Time.unscaledDeltaTime / InTime);
    }

    void OnGUI()
    {
        if (alpha <= 0.001f) return;
        var keep = GUI.color;
        int depth = GUI.depth;
        GUI.depth = -1000;                  // 무엇보다 위에
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = keep;
        GUI.depth = depth;
    }
}
