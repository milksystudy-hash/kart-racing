using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ★★ <b>곰 기억 앨범</b> — 지나온 이야기를 다시 본다. (2026-10-12)
///
/// 수연: *"모든 대화씬을 앨범으로 만들어서, 보고 싶은 챕터를 클릭하면 그 챕터를 볼 수 있게.
/// 지난 챕터와 지금 깨고 있는 챕터만. 다 완료하면 다시 게임하지 않아도 골라서 볼 수 있음."*
///
/// ★ <b>본 것만 열린다.</b> 아직 안 나온 장은 자물쇠로 보인다 —
///   목록에 제목이 다 보이면 그게 스포일러다. <see cref="StoryProgress.HasSeen"/> 하나로 판단하고,
///   거기에 <b>지금 틀 수 있는 장면</b>(<see cref="StoryScript.CurrentScene"/>)을 하나 더 연다.
///
/// ★ <b>T 와 겹치지 않는다.</b> T 는 «지금 장을 다시» 고, 이건 «아무 장이나 골라서» 다.
///   T 를 없애지 않은 이유는 손이 이미 그 키에 익었기 때문이다.
///
/// ★ 걷기 모드에서만 잡힌다. 둘러보기(궤도 카메라)에서는 다가갈 몸이 없다 —
///   접수대 시계와 같은 규칙이다(2026-09-18).
/// </summary>
public class StoryAlbum : MonoBehaviour
{
    /// <summary>이 거리 안에 들어오면 안내가 뜬다.</summary>
    public float range = 2.6f;

    /// <summary>제일 가까운 앨범 하나. 로비에 하나뿐이지만 규칙은 곰·문과 같다.</summary>
    public static StoryAlbum Nearest { get; private set; }
    static StoryAlbum pending;
    static int frame = -1;

    /// <summary>목록이 떠 있나. 로비 HUD 가 이걸 보고 다른 패널을 접는다.</summary>
    public static bool Open { get; private set; }

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
    Transform visitor;

    void Update()
    {
        // ★ «제일 가까운 것» 을 실행 순서에 안 맡긴다 — 한 프레임 늦게 공개한다.
        //   (2026-09-21 칸 문에서 겪은 그 버그다.)
        if (frame != Time.frameCount) { Nearest = pending; pending = null; frame = Time.frameCount; }

        if (Open) { Keys_(); return; }

        var who = Walker();
        if (who == null) return;
        if (Vector3.Distance(who.position, transform.position) > range) return;
        if (pending == null) pending = this;

        var k = Keyboard.current;
        if (Nearest == this && k != null && k.eKey.wasPressedThisFrame) OpenBook();
    }

    Transform Walker()
    {
        if (visitor != null && visitor.gameObject.activeInHierarchy) return visitor;
        var fp = Object.FindFirstObjectByType<FirstPersonController>();
        visitor = fp != null && fp.gameObject.activeInHierarchy ? fp.transform : null;
        return visitor;
    }

    void OpenBook()
    {
        openPages.Clear();

        string now = StoryScript.CurrentScene();
        for (int i = 0; i < Book.Length; i++)
            if (StoryProgress.HasSeen(Book[i].id) || Book[i].id == now)
                openPages.Add(i);

        if (openPages.Count == 0) { Toast.Show("아직 펼칠 쪽이 없다"); return; }

        pick = 0;
        Open = true;
    }

    void Keys_()
    {
        var k = Keyboard.current;
        if (k == null) return;

        if (Keys.Up(k))   pick = (pick - 1 + openPages.Count) % openPages.Count;
        if (Keys.Down(k)) pick = (pick + 1) % openPages.Count;

        if (k.escapeKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame) { Open = false; return; }

        if (Keys.Confirm(k))
        {
            Open = false;
            if (StoryStage.Live != null) StoryStage.Live.Enter(Book[openPages[pick]].id);
        }
    }

    void OnDisable() { if (Nearest == this) Nearest = null; Open = false; }

    /// <summary>안내 한 줄. 로비 HUD 가 부른다.</summary>
    public static string Prompt => "E   곰 기억 앨범";

    void OnGUI()
    {
        if (!Open) return;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));
        float w = screen.width, h = screen.height;

        Hud.Fill(new Rect(0f, 0f, w, h), new Color(0.03f, 0.03f, 0.05f, 0.62f));

        float rows = Book.Length;
        var box = new Rect(w * 0.5f - 230f, h * 0.5f - (60f + rows * 26f) * 0.5f,
                           460f, 60f + rows * 26f);
        Hud.NoteCard(box);
        var inner = Hud.NoteInner(box);

        GUI.Label(new Rect(inner.x, inner.y + 6f, inner.width, 26f), "곰 기억 앨범", Hud.Title);
        Hud.Rule(inner.x + 14f, inner.y + 36f, inner.width - 28f);

        float y = inner.y + 46f;
        for (int i = 0; i < Book.Length; i++)
        {
            bool unlocked = openPages.Contains(i);
            bool on = unlocked && openPages[pick] == i;

            if (on) Hud.Fill(new Rect(inner.x + 10f, y - 2f, inner.width - 20f, 24f),
                             new Color(Hud.Brass.r, Hud.Brass.g, Hud.Brass.b, 0.22f));

            // ★ 아직 안 본 장은 <b>제목을 안 보여준다.</b> 목록이 곧 스포일러가 된다.
            var style = Hud.Resize(unlocked ? Hud.Text : Hud.Label, 14, TextAnchor.MiddleLeft);
            if (!unlocked) style.normal.textColor = new Color(Hud.InkSoft.r, Hud.InkSoft.g, Hud.InkSoft.b, 0.45f);
            GUI.Label(new Rect(inner.x + 22f, y, inner.width - 44f, 24f),
                      unlocked ? Book[i].title : "— 아직", style);
            y += 26f;
        }

        GUI.Label(new Rect(inner.x, inner.yMax - 26f, inner.width, 20f),
                  "↑↓ 고르기 · ENTER 보기 · ESC 닫기",
                  Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter));

        Hud.End();
    }
}
