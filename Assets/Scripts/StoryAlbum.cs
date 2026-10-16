using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ <b>곰 기억 앨범</b> — 지나온 이야기와 단서를 다시 본다. (2026-10-12)
///
/// 제작자: *"모든 대화씬을 앨범으로 만들어서, 보고 싶은 챕터를 클릭하면 그 챕터를 볼 수 있게.
/// 지난 챕터와 지금 깨고 있는 챕터만."*
///
/// ★★ <b>물건이 아니라 버튼이다.</b> 처음에는 로비에 책 모형을 놓고 다가가서 E 를 누르게
/// 만들었는데, 제작자가 바로 잡았다 — *"따로 넣으면 어딨지 하고 찾기 어려울 것 같은데.
/// 그 떡밥 10개 뭐였더라 하고 찾을 때 불편할 것 같거든."* <b>맞다.</b>
/// 다시 보기는 <b>걸어가서 찾는 기능이 아니다.</b> 로비 왼쪽 아래 발바닥 버튼(<b>B</b>)으로
/// 어디서든 연다. 책 모형은 벤치 옆 장식으로 남는다.
///
/// ★ <b>두 쪽이다 — 이야기와 단서.</b> 둘 다 «지나온 것을 다시 보는 것» 이라, 패널을 둘로
/// 나누면 어느 쪽에 있었는지를 또 외워야 한다.
///
/// ★ <b>본 것만 열린다.</b> 아직 안 나온 장은 제목도 안 보인다 — 목록이 곧 스포일러가 된다.
/// 안 읽은 단서는 <b>어느 관에 있는지만</b> 알려준다: 제목까지 보이면 찾으러 갈 이유가 없고,
/// 자리도 안 알려주면 열 개를 다시 뒤져야 한다.
/// </summary>
public class StoryAlbum : MonoBehaviour
{
    /// <summary>로비에 하나. 발바닥 버튼이 이걸 통해 연다.</summary>
    public static StoryAlbum Live { get; private set; }

    /// <summary>목록이 떠 있나. 로비 HUD 가 이걸 보고 다른 패널을 접는다.</summary>
    public static bool Open { get; private set; }

    /// <summary>발바닥 버튼에 적는 말.</summary>
    public static string Prompt => "곰 기억 앨범   B";

    static readonly (string id, string title)[] Book =
    {
        ("prologue",     "프롤로그 · 박물관 철거 통지서"),
        ("ch1",          "제1장 · 사라진 관람객"),
        ("ch2",          "제2장 · 조작된 안전진단"),
        ("ch3",          "제3장 · 관장의 서명"),
        ("ch4",          "제4장 · 마지막 실사"),
        ("final_before", "결승 · 여덟 개의 증거"),
        ("final_after",  "결승 · 그 다음"),
        ("epilogue",     "에필로그 · 다시 여는 박물관"),
    };

    readonly List<int> openPages = new();
    int pick;
    bool clueTab;

    /// <summary>책 모형이 없어도 돈다 — 스스로 씬에 들어온다.</summary>
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
        Open = false;
        if (Object.FindFirstObjectByType<LobbyHUD>() == null) return;
        if (Object.FindFirstObjectByType<StoryAlbum>() != null) return;
        new GameObject("StoryAlbum").AddComponent<StoryAlbum>();
    }

    void Awake() => Live = this;

    void Update()
    {
        if (Dev.Typing) return;   // 암호 치는 중 — 키를 건드리지 않는다
        var k = Keyboard.current;
        if (k == null) return;

        if (Open) { Keys_(k); return; }

        // B — 어디서든 연다. 걷는 중이든 둘러보는 중이든.
        if (k.bKey.wasPressedThisFrame) OpenBook();
    }

    /// <summary>
    /// ★ 열려 있는 동안 <b>걷는 몸을 묶는다</b>(2026-10-13 제작자 요청).
    /// 안 묶으면 ↑↓ 로 쪽을 넘기는 동안 <b>뒤에서 몸이 같이 걸어간다.</b>
    ///
    /// 푸는 자리는 <see cref="Close"/> 한 군데다 — ESC · B · 장면 고르기 · 꺼짐이
    /// 전부 거기를 지난다. <b>못 돌려주면 로비에 갇힌다</b>(미니게임에서 정한 규칙).
    /// </summary>
    FirstPersonController held;

    void Hold(bool on)
    {
        if (on && held == null) held = Object.FindFirstObjectByType<FirstPersonController>();
        if (held != null) held.ControlEnabled = !on;
        if (!on) held = null;
    }

    void Close()
    {
        Open = false;
        Hold(false);
    }

    void OnDestroy() => Hold(false);

    public void OpenBook()
    {
        openPages.Clear();

        string now = StoryScript.CurrentScene();
        for (int i = 0; i < Book.Length; i++)
            if (StoryProgress.HasSeen(Book[i].id) || Book[i].id == now)
                openPages.Add(i);

        pick = 0;
        clueTab = openPages.Count == 0;      // 볼 이야기가 없으면 단서 쪽으로 연다
        Open = true;
        Hold(true);
    }

    void Keys_(Keyboard k)
    {
        // ★ 2026-10-13 제작자: *"TAB 말고 화살표로. 왼쪽이 이야기, 오른쪽이 단서."*
        //   <b>맞다 — 쪽이 좌우로 놓여 있으니 손도 좌우로 가야 한다.</b>
        //   TAB 은 그대로 두되(손이 익었으면 쓰라고) 화살표가 본길이다.
        if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame)
        { if (clueTab) { clueTab = false; pick = 0; } }
        if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame)
        { if (!clueTab) { clueTab = true; pick = 0; } }
        if (k.tabKey.wasPressedThisFrame) { clueTab = !clueTab; pick = 0; }

        int rows = clueTab ? ClueText.All.Length : openPages.Count;
        if (rows > 0)
        {
            if (Keys.Up(k)) pick = (pick - 1 + rows) % rows;
            if (Keys.Down(k)) pick = (pick + 1) % rows;
        }

        if (k.escapeKey.wasPressedThisFrame || k.bKey.wasPressedThisFrame) { Close(); return; }

        // 단서 쪽은 읽기만 한다 — 틀 장면이 없다.
        if (!clueTab && Keys.Confirm(k) && openPages.Count > 0)
        {
            Close();
            if (StoryStage.Live != null) StoryStage.Live.Enter(Book[openPages[pick]].id);
        }
    }

    void OnDisable() { if (Live == this) Live = null; Close(); }

    static void Row(Rect inner, ref float y, bool on, bool open, string text)
    {
        if (on) Hud.Fill(new Rect(inner.x + 12f, y - 2f, inner.width - 24f, 26f),
                         new Color(Hud.Brass.r, Hud.Brass.g, Hud.Brass.b, 0.22f));

        var style = Hud.Resize(open ? Hud.Text : Hud.Label, 14, TextAnchor.MiddleLeft);
        if (!open) style.normal.textColor = new Color(Hud.InkSoft.r, Hud.InkSoft.g, Hud.InkSoft.b, 0.45f);
        GUI.Label(new Rect(inner.x + 24f, y, inner.width - 48f, 26f), text, style);
        y += 28f;
    }

    /// <summary>그 단서가 어느 관에 있나. 안 읽은 것에는 <b>자리만</b> 알려준다.</summary>
    static string Where(string id)
    {
        foreach (var p in CampusBuilder.ClueLayout)
            if (p.label == id) return p.hall;
        return "어딘가";
    }

    void OnGUI()
    {
        if (!Open) return;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));
        float w = screen.width, h = screen.height;

        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.03f, 0.03f, 0.05f, 0.62f));

        // ★ 2026-10-13 — 아래 안내가 <b>마지막 줄을 덮고 있었다.</b> 칸 높이를
        //   머리(제목 + 쪽 이름 + 가로줄) · 줄 · 발(안내) <b>셋을 더해서</b> 잡는다.
        //   <see cref="Hud.NoteInner"/> 가 테두리로 5.5% 씩 먹으니 그만큼 되돌려 키운다.
        const float Head = 92f, RowH = 28f, Foot = 40f;
        int rows = clueTab ? ClueText.All.Length : Book.Length;
        float need = Head + rows * RowH + Foot;
        var box = new Rect(w * 0.5f - 270f, h * 0.5f - need / 0.89f * 0.5f,
                           540f, need / 0.89f);
        Hud.NoteCard(box);
        var inner = Hud.NoteInner(box);

        GUI.Label(new Rect(inner.x, inner.y + 4f, inner.width, 28f), "곰 기억 앨범", Hud.Title);

        // 쪽 두 개 — 이야기 · 단서. 지금 보고 있는 쪽이 진하다.
        var tabStory = Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter);
        var tabClue = Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter);
        var dim = new Color(Hud.InkSoft.r, Hud.InkSoft.g, Hud.InkSoft.b, 0.5f);
        if (clueTab) tabStory.normal.textColor = dim; else tabClue.normal.textColor = dim;

        // 왼쪽이 이야기, 오른쪽이 단서 — 화살표 방향과 자리가 같아야 헷갈리지 않는다.
        GUI.Label(new Rect(inner.x, inner.y + 40f, inner.width * 0.5f, 24f),
                  (clueTab ? "" : "◀  ") + "이야기", tabStory);
        GUI.Label(new Rect(inner.x + inner.width * 0.5f, inner.y + 40f, inner.width * 0.5f, 24f),
                  $"단서  {ClueBoard.ReadCount} / {ClueText.Count}" + (clueTab ? "" : "  ▶"), tabClue);
        Hud.Rule(inner.x + 14f, inner.y + 70f, inner.width - 28f);

        float y = inner.y + Head - 12f;
        if (!clueTab)
            for (int i = 0; i < Book.Length; i++)
            {
                bool unlocked = openPages.Contains(i);
                bool on = unlocked && openPages.Count > 0 && openPages[pick] == i;
                Row(inner, ref y, on, unlocked, unlocked ? Book[i].title : "— 아직");
            }
        else
            for (int i = 0; i < ClueText.All.Length; i++)
            {
                var note = ClueText.All[i];
                bool read = ClueBoard.WasRead(note.id);
                Row(inner, ref y, pick == i, read,
                    read ? $"{i + 1:00}   {note.title}" : $"{i + 1:00}   — {Where(note.id)}에 있다");
            }

        GUI.Label(new Rect(inner.x, inner.yMax - 24f, inner.width, 20f),
                  clueTab ? "◀▶ 쪽 넘기기 · ↑↓ 고르기 · ESC 닫기"
                          : "◀▶ 쪽 · ↑↓ 고르기 · ENTER 보기 · T 지금 장 · ESC 닫기",
                  Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter));

        Hud.End();
    }
}
