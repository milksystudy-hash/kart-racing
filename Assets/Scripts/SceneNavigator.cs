using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 재생 중에 씬을 바꾼다.
/// F1 로비 · F2 트랙 · F3 전시실 · F4 테스트베드 · F6 내 맵 · F5 다시 시작.
///
/// 씬이 Build Settings 에 등록돼 있어야 동작한다.
/// (File → Build Profiles 에서 확인 — 에디터 스크립트가 이미 등록해뒀어)
/// </summary>
public class SceneNavigator : MonoBehaviour
{
    [Tooltip("씬을 넘어가도 이 오브젝트를 유지할지")]
    public bool persistAcrossScenes = false;

    public static string CurrentSceneName => SceneManager.GetActiveScene().name;
    public static int SceneCount => SceneManager.sceneCountInBuildSettings;

    void Awake()
    {
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
    }

    [Tooltip("F1~F3 으로 씬을 건너뛰는 개발용 단축키. 빌드한 게임에서는 자동으로 꺼진다")]
    public bool debugKeys = true;

    void Update()
    {
        // 플레이어는 씬을 마음대로 건너뛰면 안 된다. 에디터에서만 듣는다.
        if (!Dev.Enabled || !debugKeys) return;

        // ★★ 2026-10-06 유저: *"프롤로그에서 F2 누르면 레이싱 장면으로 이동되고
        //   프롤로그 브금으로 레이싱 할 수 있던데 못 하게 막아."* <b>맞다.</b>
        //   이야기가 도는 중에 씬을 넘기면 <see cref="StoryStage"/> 가 로비를 되돌려 놓지
        //   못해서 음악도 카메라도 이야기 상태 그대로 끌려간다.
        //   <b>큰 화면이 떠 있으면 F 키는 없다</b> — «큰 패널은 한 번에 한 장» 과 같은 규칙.
        if (StoryStage.Talking || TitleScreen.Up) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) LoadByIndex(0);
        if (keyboard.f2Key.wasPressedThisFrame) LoadByIndex(1);
        if (keyboard.f3Key.wasPressedThisFrame) LoadByIndex(2);
        if (keyboard.f4Key.wasPressedThisFrame) LoadByIndex(3);
        if (keyboard.f6Key.wasPressedThisFrame) LoadByIndex(4);
        if (keyboard.f5Key.wasPressedThisFrame) Reload();
    }

    public static void LoadByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[SceneNavigator] 빌드 설정에 {buildIndex}번 씬이 없어. " +
                             $"등록된 씬은 {SceneManager.sceneCountInBuildSettings}개야.");
            return;
        }

        CursorLock.Unlock();   // 씬을 넘어가는 동안은 커서를 풀어둔다

        // ★ 까맣게 덮었다 걷는다(2026-10-01). 캠퍼스는 3,200조각이라 로딩이 2~3초인데,
        //   <b>멈춘 화면</b>과 <b>어두워지는 화면</b>은 완전히 다르게 읽힌다 —
        //   가만히 있으면 «렉» 이고 어두워지면 «넘어가는 중» 이다.
        Fade.Load(buildIndex);
    }

    public static void Reload() => LoadByIndex(SceneManager.GetActiveScene().buildIndex);
}
