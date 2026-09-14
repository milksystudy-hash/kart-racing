using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비의 출발문. 걸어 들어가면 카운트다운이 돌고 트랙 씬으로 넘어간다.
/// 문을 벗어나면 취소돼서, 실수로 지나가도 레이스가 시작되지 않는다.
///
/// 물리 트리거 대신 거리로 판정한다 — CharacterController 는 트리거가 가끔 안 잡혀서,
/// 거리 재는 쪽이 훨씬 확실해.
/// </summary>
public class StartGate : MonoBehaviour
{
    [Tooltip("이 거리 안에 들어오면 카운트다운 시작")]
    public float triggerRadius = 2.6f;
    [Tooltip("넘어가기까지 몇 초")]
    public float countdownSeconds = 1.5f;

    [Tooltip("넘어갈 씬 이름. 빌드 설정에 등록돼 있어야 한다")]
    public string targetScene = "Track";

    [Tooltip("캐릭터를 안 골랐으면 못 들어가게 할지")]
    public bool requireSelection = true;

    public Transform player;

    public bool PlayerInside { get; private set; }
    public float Remaining { get; private set; }
    public bool Blocked => requireSelection && !GameSelection.HasSelection;

    bool loading;

    void Awake()
    {
        if (player == null)
        {
            var fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null) player = fpc.transform;
        }
        Remaining = countdownSeconds;
    }

    void Update()
    {
        if (loading || player == null) return;

        // 높이는 무시하고 바닥 거리만 잰다 (점프해도 판정이 흔들리지 않게)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position;    b.y = 0f;
        PlayerInside = Vector3.Distance(a, b) <= triggerRadius;

        if (!PlayerInside || Blocked)
        {
            Remaining = countdownSeconds;
            return;
        }

        Remaining -= Time.deltaTime;
        if (Remaining <= 0f) Go();
    }

    void Go()
    {
        loading = true;
        CursorLock.Unlock();

        if (Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning($"[StartGate] '{targetScene}' 씬을 빌드 설정에서 못 찾았어. " +
                             "File → Build Profiles 에서 씬 목록을 확인해줘.");
            loading = false;
            Remaining = countdownSeconds;
        }
    }
}
