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
        if (!debugKeys || !Application.isEditor) return;

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
        SceneManager.LoadScene(buildIndex);
    }

    public static void Reload() => LoadByIndex(SceneManager.GetActiveScene().buildIndex);
}
