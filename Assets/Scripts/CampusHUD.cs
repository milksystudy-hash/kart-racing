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

        if (k.hKey.wasPressedThisFrame) showControls = !showControls;

        if (k.eKey.wasPressedThisFrame)
        {
            // 문이 먼저다. 문 앞에 곰이 서 있을 때 말을 걸다가 못 들어가면 답답하다.
            // 순서: 씬 문 → 미니게임 자리 → 건물 문 → 곰.
            // 미니게임 자리는 <b>건물 안</b>에 있어서 문보다 먼저 잡혀야 안 헷갈린다 —
            // 이미 열고 들어온 문을 또 여는 것보다 안에 있는 걸 집는 게 맞다.
            if (SceneDoor.Nearest != null) SceneDoor.Nearest.Enter();
            else if (MinigameSpot.Nearest != null) MinigameSpot.Nearest.Enter();
            else if (HingedDoor.Nearest != null) HingedDoor.Nearest.Toggle();
            else if (BearNpc.Nearest != null) BearNpc.Nearest.Talk();
        }
    }

    bool showControls;

    void OnGUI()
    {
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

        // 미니게임 자리가 제일 먼저. 잠겼거나 준비 중이면 <b>그 이유</b>가 한 줄로 뜬다 —
        // "안 된다" 만 뜨면 플레이어는 왜 안 되는지 모른다.
        if (MinigameSpot.Nearest != null)
        {
            what = MinigameSpot.Nearest.Line;
            // 눌러도 아무 일이 안 되는데 키를 보여주면 <b>고장으로 읽힌다.</b>
            if (!MinigameSpot.Nearest.Actionable) key = "";
        }
        else if (SceneDoor.Nearest != null)
            what = string.IsNullOrEmpty(SceneDoor.Nearest.label)
                 ? "들어가기" : $"{SceneDoor.Nearest.label} 들어가기";
        else if (HingedDoor.Nearest != null)
            // 상태에 맞는 말이 떠야 한다 — 열린 문에 "문 열기" 가 뜨면 닫는 법을 모른다.
            what = string.IsNullOrEmpty(HingedDoor.Nearest.label)
                 ? HingedDoor.Nearest.Action
                 : $"{HingedDoor.Nearest.label} {HingedDoor.Nearest.Action}";
        else if (BearNpc.Nearest != null)
            what = "말 걸기";

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

        var box = new Rect(w * 0.5f - 230f, h - 172f, 460f, 38f);
        Hud.Panel(box);
        GUI.DrawTexture(new Rect(box.x + 7f, box.y + 7f, 6f, box.height - 14f), Hud.RibbonTex);
        GUI.Label(box, Toast.Message, Hud.Resize(Hud.Title, 17));
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
