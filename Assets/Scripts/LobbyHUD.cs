using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 로비 화면 표시. 고른 드라이버, 눈앞의 안내문, 출발문 카운트다운.
///
/// 트랙 HUD 와 <b>같은 나무 판 · 같은 종이 · 같은 잉크</b>를 쓴다(<see cref="Hud"/>).
/// 로비만 어두운 반투명 판이면 방을 나갈 때마다 화면이 다른 게임처럼 보인다.
///
/// 화면 아래 조작법 띠는 없앴다(2026-09-16). 플레이어는 마우스를 움직여 보고 알아채면 되고,
/// 카메라가 마우스를 따라 살짝 움직여서 "이거 돌아가는구나" 를 먼저 알려준다
/// (<see cref="LobbyOrbitCamera"/>). 자세한 건 <b>H</b>.
///
/// 진짜 UI 를 만들 때 통째로 버릴 스크립트야.
/// </summary>
public class LobbyHUD : MonoBehaviour
{
    public LobbySelector selector;
    public StartGate gate;

    [Header("폰트 (비워 두면 OS 한글 폰트를 쓴다)")]
    public Font uiFont;

    bool showControls;

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.hKey.wasPressedThisFrame) showControls = !showControls;
    }

    void OnGUI()
    {
        Rect screen = Hud.Begin(uiFont);
        float w = screen.width, h = screen.height;

        DrawDriverPanel();
        DrawPrompt(w, h);
        DrawGate(w, h);
        DrawCorner(h);
        if (showControls) DrawControls(w, h);

        Hud.End();
    }

    // ---- 좌상단: 고른 드라이버 ----
    void DrawDriverPanel()
    {
        var p = new Rect(16f, 16f, 196f, 80f);
        Hud.Panel(p);

        float x = p.x + 14f;
        GUI.Label(new Rect(x, p.y + 12f, 160f, 26f), "로비", Hud.Resize(Hud.Value, 20));
        Hud.Rule(x, p.y + 40f, p.width - 28f);

        // 라벨과 이름을 위아래로 겹쳐 쓰다가 글자가 부딪혔다. 한 줄에 두 칸으로 나눈다.
        GUI.Label(new Rect(x, p.y + 48f, 58f, 22f), "드라이버", Hud.Label);
        GUI.Label(new Rect(p.x + 76f, p.y + 46f, p.width - 90f, 24f),
                  GameSelection.HasSelection ? GameSelection.SelectedName : "선택 안 됨",
                  Hud.Resize(Hud.Value, 19));
    }

    // ---- 화면 가운데 아래: 눈앞의 안내문 ----
    void DrawPrompt(float w, float h)
    {
        if (selector == null || string.IsNullOrEmpty(selector.Prompt)) return;

        var box = new Rect(w * 0.5f - 170f, h * 0.64f, 340f, 40f);
        Hud.Panel(box);
        GUI.Label(box, selector.Prompt, Hud.Resize(Hud.Title, 16));
    }

    // ---- 출발문 ----
    void DrawGate(float w, float h)
    {
        if (gate == null || !(gate.Hovered || gate.CountingDown)) return;

        var box = new Rect(w * 0.5f - 150f, h * 0.30f, 300f, 82f);
        Hud.Panel(box);

        var head = Hud.Resize(Hud.Title, 18);
        var sub  = Hud.Resize(Hud.Label, 14, TextAnchor.MiddleCenter);

        if (gate.Blocked)
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "먼저 드라이버를 고르세요", head);
            GUI.Label(new Rect(box.x, box.y + 44f, box.width, 22f), "받침대 위 카트를 클릭", sub);
        }
        else if (gate.CountingDown)
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "레이스 시작", head);

            float fill = 1f - Mathf.Clamp01(gate.Remaining / Mathf.Max(0.01f, gate.countdownSeconds));
            var bar = new Rect(box.x + 40f, box.y + 50f, box.width - 80f, 12f);
            GUI.DrawTexture(bar, Hud.WoodDarkTex);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), Hud.BrassTex);
        }
        else
        {
            GUI.Label(new Rect(box.x, box.y + 14f, box.width, 26f), "출발문", head);
            GUI.Label(new Rect(box.x, box.y + 44f, box.width, 22f), "클릭하면 레이스가 시작됩니다", sub);
        }
    }

    // ---- 왼쪽 아래 구석 ----
    void DrawCorner(float h)
    {
        if (showControls) return;
        var chip = new Rect(16f, h - 28f, 88f, 22f);
        Hud.Chip(chip);
        GUI.Label(new Rect(chip.x + 9f, chip.y + 3f, 78f, 18f), "H  조작법", Hud.Resize(Hud.Text, 13));
    }

    void DrawControls(float w, float h)
    {
        var box = new Rect(w * 0.5f - 190f, h * 0.5f - 92f, 380f, 184f);
        Hud.Panel(box);

        GUI.Label(new Rect(box.x, box.y + 16f, box.width, 26f), "조작법", Hud.Title);
        Hud.Rule(box.x + 20f, box.y + 46f, box.width - 40f);

        string[,] rows =
        {
            { "마우스 움직이기", "둘러보기" },
            { "마우스 끌기", "빙 돌려 보기" },
            { "휠", "가까이 · 멀리" },
            { "클릭", "카트 고르기 / 출발문 열기" },
            { "H", "이 창 닫기" },
        };

        var key = Hud.Resize(Hud.Text, 14);
        key.fontStyle = FontStyle.Bold;
        var desc = Hud.Resize(Hud.Label, 14);

        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = box.y + 58f + i * 24f;
            GUI.Label(new Rect(box.x + 24f, y, 130f, 22f), rows[i, 0], key);
            GUI.Label(new Rect(box.x + 158f, y, box.width - 180f, 22f), rows[i, 1], desc);
        }
    }
}
