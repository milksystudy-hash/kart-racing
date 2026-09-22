using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 캠퍼스 씬의 화면 표시. <b>걷는 것 말고는 아무것도 안 한다</b> — 랩도 속도도 임무도 없다.
///
/// 트랙 HUD 를 재활용하지 않는 이유: 그쪽은 레이스 계기판이라 끌 것이 켤 것보다 많다.
/// 여기서 필요한 건 셋뿐이야 — <b>어디로 들어가는지 · 곰에게 말 걸기 · 조작법</b>.
/// </summary>
public class CampusHUD : MonoBehaviour
{
    public Font uiFont;

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        // 배식 중에는 캠퍼스 조작을 안 받는다. 큰 패널이 떠 있으면 그 패널만 듣는다 —
        // 안 막으면 ESC 한 번에 급식도 끝나고 조작법 카드도 같이 열린다
        // (레이스 ESC 패널에서 세운 «큰 패널은 한 번에 한 장» 규칙과 같다).
        if (Canteen.Open) return;

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        if (k.eKey.wasPressedThisFrame)
        {
            switch (Pick())
            {
                case Target.씬문:     SceneDoor.Nearest.Enter();    break;
                case Target.미니게임: MinigameSpot.Nearest.Enter(); break;
                case Target.수도:     Faucet.Nearest.Toggle();      break;
                case Target.건물문:   HingedDoor.Nearest.Toggle();  break;
                case Target.곰:       BearNpc.Nearest.Talk();       break;
            }
        }
    }

    enum Target { 없음, 씬문, 미니게임, 수도, 건물문, 곰 }

    /// <summary>
    /// ★ <b>«무엇을 집을지» 는 종류 순서가 아니라 점수로 고른다</b>(2026-09-22).
    ///
    /// 전에는 «씬 문 → 미니게임 → 수도꼭지 → 건물 문 → 곰» 순서를 코드에 박아 놨다.
    /// 그래서 유저가 이렇게 말했다: *"화장실 칸막이 문 열 때에도 계속 세면대에 물 끄기만
    /// 보인다. 문 열고 싶은데 자꾸 물 끄기 버튼밖에 없고."* — 맞는 말이다.
    /// 8 × 7m 짜리 방에서는 넷이 전부 사정권이라 <b>순서가 곧 답</b>이 돼 버린다.
    ///
    /// <see cref="Reach"/> 가 <b>거리 × 각도 벌점</b>으로 채점하니 종류가 달라도 비교된다.
    /// 세면대를 보고 서면 수도꼭지가, 칸 문을 보고 서면 그 문이 이긴다.
    ///
    /// 곰만 <b>맨 뒤</b>다 — 로비의 궤도 카메라를 위해 «화면 가운데에 가까운 각도» 라는
    /// 다른 잣대를 쓰고 있어서 점수를 같이 줄 세울 수가 없다.
    /// </summary>
    static Target Pick()
    {
        Target best = Target.없음;
        float bestScore = float.MaxValue;

        void Try(Target t, bool has, float score)
        {
            if (!has || score >= bestScore) return;
            bestScore = score;
            best = t;
        }

        Try(Target.씬문,     SceneDoor.Nearest != null,    SceneDoor.NearestScore);
        Try(Target.미니게임, MinigameSpot.Nearest != null, MinigameSpot.NearestScore);
        Try(Target.수도,     Faucet.Nearest != null,       Faucet.NearestScore);
        Try(Target.건물문,   HingedDoor.Nearest != null,   HingedDoor.NearestScore);

        if (best == Target.없음 && BearNpc.Nearest != null) best = Target.곰;
        return best;
    }

    bool showControls;

    void OnGUI()
    {
        // 급식 화면이 떠 있으면 캠퍼스 안내는 한 장도 안 그린다.
        if (Canteen.Open) return;

        Rect screen = Hud.Begin(uiFont);
        float w = screen.width, h = screen.height;

        DrawPrompt(w, h);
        DrawToast(w, h);
        DrawCorner(h);
        if (showControls) DrawControls(w, h);

        Hud.End();
    }

    /// <summary>화면 아래 가운데 한 줄. 들어갈 문이 먼저, 없으면 곰.</summary>
    void DrawPrompt(float w, float h)
    {
        string key = "E", what = null;

        // ★ <b>안내와 E 는 같은 함수가 고른다</b>(2026-09-21 → 22).
        // 뜨는 것과 집히는 것이 다르면 «눌러도 안 되는 안내» 가 된다 —
        // 화장실 칸 문에서 이미 한 번 그렇게 헤맸다. 이제 `Pick()` 하나뿐이라 어긋날 수가 없다.
        switch (Pick())
        {
            case Target.씬문:
                what = string.IsNullOrEmpty(SceneDoor.Nearest.label)
                     ? "들어가기" : $"{SceneDoor.Nearest.label} 들어가기";
                break;

            case Target.미니게임:
                what = MinigameSpot.Nearest.Line;
                // 눌러도 아무 일이 안 되는데 키를 보여주면 <b>고장으로 읽힌다.</b>
                if (!MinigameSpot.Nearest.Actionable) key = "";
                break;

            case Target.수도:
                what = Faucet.Nearest.Action;
                break;

            case Target.건물문:
                // 상태에 맞는 말이 떠야 한다 — 열린 문에 "문 열기" 가 뜨면 닫는 법을 모른다.
                what = string.IsNullOrEmpty(HingedDoor.Nearest.label)
                     ? HingedDoor.Nearest.Action
                     : $"{HingedDoor.Nearest.label} {HingedDoor.Nearest.Action}";
                if (!HingedDoor.Nearest.Actionable) key = "";
                break;

            case Target.곰:
                what = "말 걸기";
                break;
        }

        if (what == null) return;

        string label = string.IsNullOrEmpty(key) ? what : $"{key}   {what}";

        var text = Hud.Resize(Hud.Text, 16);
        float wide = text.CalcSize(new GUIContent(label)).x + 34f;

        var chip = new Rect(w * 0.5f - wide * 0.5f, h - 116f, wide, 30f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 16f, chip.y + 5f, chip.width - 24f, 20f), label, text);
    }

    void DrawToast(float w, float h)
    {
        if (!Toast.Visible) return;

        // 로비와 같은 병이 여기도 있었다(2026-09-21) — 줄바꿈이 꺼져 있어서 긴 문구가 칸 밖으로 샌다.
        // 미니게임 «준비 중» 문구가 이미 30자를 넘는다.
        const float wide = 460f;
        var text = Hud.Resize(Hud.Title, 17, TextAnchor.MiddleCenter);
        text.wordWrap = true;

        float need = text.CalcHeight(new GUIContent(Toast.Message), wide - 26f);
        float tall = Mathf.Clamp(need + 24f, 38f, 96f);

        var box = new Rect(w * 0.5f - wide * 0.5f, h - 134f - tall, wide, tall);
        Hud.Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 14f), Hud.RibbonTex);

        Rect area = Hud.Inner(box);
        GUI.Label(new Rect(area.x + 12f, area.y, area.width - 16f, area.height), Toast.Message, text);
    }

    public FirstPersonController player;

    /// <summary>
    /// 왼쪽 아래. 조작법 칩 위에 <b>개발용 키를 늘 띄운다</b> —
    /// 키가 있어도 화면에 없으면 없는 것과 같다.
    /// </summary>
    void DrawCorner(float h)
    {
        if (player == null) player = FindFirstObjectByType<FirstPersonController>();

        var dev = new Rect(16f, h - 54f, 214f, 22f);
        Hud.Chip(dev);

        string state = player != null && player.IsGhost ? "유령 ON" : "G 벽 통과";
        var tiny = Hud.Resize(Hud.Text, 12);
        if (player != null && player.IsGhost) tiny.normal.textColor = Hud.Brass;
        GUI.Label(new Rect(dev.x + 9f, dev.y + 4f, dev.width - 16f, 16f),
                  $"{state}  ·  F 날기  ·  F1 로비", tiny);

        if (showControls) return;
        var chip = new Rect(16f, h - 28f, 88f, 22f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 9f, chip.y + 3f, 78f, 18f), "H  조작법", Hud.Resize(Hud.Text, 13));
    }

    void DrawControls(float w, float h)
    {
        var box = new Rect(w * 0.5f - 190f, h * 0.5f - 130f, 380f, 260f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 16f, box.width, 26f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 46f, box.width - 40f);

        string[,] rows =
        {
            { "WASD", "걷기 (SHIFT 뛰기)" },
            { "마우스", "둘러보기" },
            { "SPACE", "뛰어넘기" },
            { "E", "문으로 들어가기 · 곰에게 말 걸기" },
            { "G", "벽 통과 (개발용)" },
            { "F", "날기 (개발용)" },
            { "H", "이 창 닫기" },
        };

        var key = Hud.Resize(Hud.Text, 14);
        key.fontStyle = FontStyle.Bold;
        var desc = Hud.Resize(Hud.Label, 14);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = box.y + 56f + i * 26f;
            GUI.Label(new Rect(box.x + 24f, y, 110f, 22f), rows[i, 0], key);
            GUI.Label(new Rect(box.x + 142f, y, box.width - 164f, 22f), rows[i, 1], desc);
        }
    }
}
