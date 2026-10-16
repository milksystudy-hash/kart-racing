using UnityEngine;

/// <summary>
/// ★ <b>심사단 카메라 플래시.</b> 8판(마지막 실사)에만 선다. (2026-10-16)
///
/// 제작자: *"실사차 3대의 임무에서 기믹이 없는 것 같은데 괜찮을까."*
/// <b>맞는 지적이다.</b> 다른 판에는 전부 움직이는 게 하나씩 있었다 —
/// 2판 비행선 · 3판 헬기 불빛 · 4판 포크레인 · 5판 도장 · 7판 드론과 자재.
/// 그런데 <b>마지막 판만 세워 둔 차와 사람뿐</b>이라, 제일 중요한 판이 제일 조용했다.
///
/// ── 왜 «해를 끼치지 않는» 기믹인가 ────────────────────────────────
/// 8판은 이미 <b>담장 안 긁고 + 시간 안에</b> 둘을 동시에 요구한다.
/// 여기에 피해야 할 것을 더하면 어려운 게 아니라 <b>벌</b>이 된다.
/// 그래서 <b>긴장만 올리고 판정에는 손대지 않는다</b> —
/// 지나갈 때 사진이 찍힌다. 보는 눈이 있다는 걸 몸으로 알려주는 것이 전부다.
///
/// 이야기와도 맞는다: 이 판에 <b>사람이 처음 나타난다.</b> 곰은 사람 앞에서
/// 움직일 수 없어서 카트만 나가는데, 그 규칙이 플래시 한 번에 증명된다.
/// </summary>
public class PressFlash : MonoBehaviour
{
    [Tooltip("이 거리 안에 들어오면 찍는다")]
    public float range = 17f;

    [Tooltip("화면이 번쩍이는 세기. 0.2 를 넘으면 담장이 안 보여서 벌이 된다")]
    public float strength = 0.16f;

    static Transform player;
    static bool toldOnce;

    bool armed = true;

    void Update()
    {
        if (RacePause.On || RaceCountdown.Blocked) return;

        if (player == null)
        {
            var kart = FindFirstObjectByType<PlayerKart>();
            player = kart != null ? kart.transform : null;
            if (player == null) return;
        }

        float d = Vector3.Distance(player.position, transform.position);

        // ★ 한 번 찍고 <b>멀어지면 다시 장전한다.</b> 한 판에 한 번만 찍으면
        //   두세 바퀴째가 조용해지고, 매 프레임 찍으면 화면이 하얘진다.
        if (armed && d < range)
        {
            armed = false;
            ScreenFlash.Hit(strength, new Color(1f, 0.98f, 0.92f), 0.14f);

            if (!toldOnce)
            {
                toldOnce = true;
                Toast.Show("실사단이 사진을 찍는다");
            }
        }
        else if (!armed && d > range * 1.6f) armed = true;
    }

    /// <summary>판이 바뀌면 안내를 다시 할 수 있게. <see cref="RaceProps"/> 가 부른다.</summary>
    public static void Forget() { player = null; toldOnce = false; }
}
