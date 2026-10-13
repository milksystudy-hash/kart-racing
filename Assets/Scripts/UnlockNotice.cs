using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ <b>결승을 이긴 뒤 «무엇이 열렸는지» 한 번만 알려준다.</b> (2026-10-13)
///
/// 아홉 판을 다 깨면 <b>다섯 가지가 한꺼번에 열리는데</b> 그걸 아무 데서도 안 알려줬다.
/// 로비 머리줄이 「철거를 막았다」로 바뀌는 게 전부라, 플레이어는 <b>캠퍼스가 바뀐 것도
/// 설정 쪽지가 생긴 것도 모르고</b> 게임을 끈다. 한 달 만든 연출을 아무도 안 보는 셈이다.
///
/// ── 왜 «보상» 이 아니라 «안내» 인가 ────────────────────────────────────
/// 새로 주는 게 하나도 없다. <b>이미 열려 있는 것을 읽어서 적기만 한다</b> —
/// <see cref="GrandFinal.FreeRun"/> 하나로 이미 다섯 군데가 바뀌고 있고,
/// 이 창은 그 다섯을 <b>한 번</b> 모아 보여줄 뿐이다. 규칙도 밸런스도 안 건드린다.
///
/// ── 설계 ───────────────────────────────────────────────────────────────
/// · <b>스스로 씬에 들어온다</b>(<see cref="Boot"/>) — 로비를 다시 구울 필요가 없다.
///   「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」를 이 프로젝트에서 다섯 번 겪었다.
/// · <b>딱 한 번만</b>(PlayerPrefs). 로비에 들어올 때마다 뜨면 안내가 아니라 장애물이다.
/// · <b>이야기가 끝난 뒤에</b> 뜬다 — 결승을 이기면 <c>final_after</c> 가 먼저 돌고,
///   그 위에 겹치면 「큰 패널은 한 번에 한 장」 규칙을 깬다.
/// · <b>타이틀 화면과도 안 겹친다.</b> 첫 실행에 둘이 같이 뜰 수 있다.
///
/// ⚠ 적는 내용은 <b>실제로 열리는 것만</b>이다. 지금 다섯 가지가
///   <see cref="GrandFinal.FreeRun"/> 을 보고 있다:
///   <see cref="RaceProps"/>(자유 주행 AI 3대) · <see cref="CampusVictory"/>(동상·광고판) ·
///   <see cref="CampusBoarding"/>(판자) · <see cref="RoomLife"/>(건물 안) ·
///   <see cref="LobbyHUD"/>(설정 쪽지). <b>여기 줄을 더하기 전에 진짜 열리는지 확인해라</b> —
///   안 열리는 걸 적으면 그게 제일 나쁜 안내다.
/// </summary>
public class UnlockNotice : MonoBehaviour
{
    const string ShownKey = "Racing.UnlockShown";

    /// <summary>창이 떠 있나. 로비 HUD 가 이걸 보고 제 안내를 접는다.</summary>
    public static bool Open { get; private set; }

    static readonly (string where, string what)[] Lines =
    {
        ("경기장",  "형제 셋과 함께 달린다"),
        ("캠퍼스",  "판자가 걷히고 축제가 걸렸다"),
        ("건물 안", "멈춰 있던 것들이 다시 돌아간다"),
        ("중앙홀",  "설정 쪽지가 열렸다   L"),
    };

    float openedAt;

    // ── 스스로 들어온다 ──────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Install();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Install();

    static void Install()
    {
        Open = false;      // ★ static 은 씬을 넘어 살아남는다 — 들어올 때마다 지운다
        if (Object.FindFirstObjectByType<LobbyHUD>() == null) return;
        if (Object.FindFirstObjectByType<UnlockNotice>() != null) return;
        new GameObject("UnlockNotice").AddComponent<UnlockNotice>();
    }

    /// <summary>F10(처음부터)에서 같이 지운다 — 다시 깨면 다시 봐야 한다.</summary>
    public static void Forget()
    {
        PlayerPrefs.DeleteKey(ShownKey);
        PlayerPrefs.Save();
    }

    static bool AlreadyShown => PlayerPrefs.GetInt(ShownKey, 0) == 1;

    void Update()
    {
        if (!Open)
        {
            // 이길 때까지, 이야기가 끝날 때까지, 타이틀이 닫힐 때까지 기다린다.
            if (!GrandFinal.FreeRun || AlreadyShown) return;
            if (TitleScreen.Up || StoryStage.Talking) return;
            if (DeskClock.PanelOpen || StoryAlbum.Open || MinigameFlow.AnyOpen) return;

            Open = true;
            openedAt = Time.unscaledTime;
            PlayerPrefs.SetInt(ShownKey, 1);
            PlayerPrefs.Save();
            return;
        }

        // 연 프레임의 키가 그대로 넘어와 바로 닫히지 않게 — 단서 카드·브리핑과 같은 처리
        if (Time.unscaledTime - openedAt < 0.4f) return;

        var k = Keyboard.current;
        if (k != null && k.anyKey.wasPressedThisFrame) Open = false;
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Open = false;
    }

    void OnDisable() => Open = false;

    void OnGUI()
    {
        if (!Open) return;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));
        float w = screen.width, h = screen.height;

        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.03f, 0.03f, 0.05f, 0.58f));

        const float CardW = 480f, Head = 86f, RowH = 30f, Foot = 46f;
        float need = (Head + Lines.Length * RowH + Foot) / 0.89f;
        var box = new Rect(w * 0.5f - CardW * 0.5f, h * 0.5f - need * 0.5f, CardW, need);
        Hud.NoteCard(box);
        var inner = Hud.NoteInner(box);

        GUI.Label(new Rect(inner.x, inner.y + 6f, inner.width, 30f),
                  "철거를 막았다", Hud.Resize(Hud.Title, 23, TextAnchor.UpperCenter));
        GUI.Label(new Rect(inner.x, inner.y + 40f, inner.width, 22f),
                  "열린 것", Hud.Resize(Hud.Label, 13, TextAnchor.UpperCenter));
        Hud.Rule(inner.x + 16f, inner.y + 66f, inner.width - 32f);

        // 어디 · 무엇 두 칸. 한 줄에 이어 쓰면 «어디» 가 안 읽힌다(제원표에서 배운 것).
        var where = Hud.Resize(Hud.Label, 14, TextAnchor.MiddleLeft);
        var what = Hud.Resize(Hud.Text, 15, TextAnchor.MiddleLeft);
        float y = inner.y + Head - 8f;
        foreach (var (a, b) in Lines)
        {
            GUI.Label(new Rect(inner.x + 26f, y, 68f, 26f), a, where);
            GUI.Label(new Rect(inner.x + 100f, y, inner.width - 126f, 26f), b, what);
            y += RowH;
        }

        GUI.Label(new Rect(inner.x, inner.yMax - 28f, inner.width, 22f),
                  "아무 키나   닫기", Hud.Resize(Hud.Label, 13, TextAnchor.MiddleCenter));

        Hud.End();
    }
}
