#if UNITY_EDITOR || DEV_BUILD
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ★★ <b>개발용 키를 암호로 연다.</b> (2026-10-14 제작자 요청)
///
/// 제작자: *"플레이어들은 못 찾게, G 키로 관리자 모드 들어간 거랑 장면 번호 눌러서 보는 거랑
/// 그런 건 없애지 말고 우리 시크릿 코드 입력하면 들어가 있게."*
///
/// ── 왜 필요한가 ────────────────────────────────────────────────────────
/// <see cref="Dev"/> 는 <b>빌드에서는</b> 이미 상수 false 라 컴파일 단계에서 사라진다.
/// 그런데 <b>에디터에서는 늘 켜져 있어서</b> 화면에 「1~5 장면 고르기 · F9 전시품 전부 수집」
/// 같은 줄이 그대로 떠 있었다 — 화면을 보여줄 때마다 개발용 장치가 같이 보인다.
///
/// ── 여는 법 ────────────────────────────────────────────────────────────
/// <b>Ctrl + Shift + D</b> → 암호 칸이 열린다 → 치고 <b>ENTER</b>. ESC 로 닫는다.
/// ★ <b>Ctrl+Shift+D 가 유일한 입구다.</b> 칸 없이 그냥 치는 길은 2026-10-14 에 걷어냈다 —
/// 치는 글자가 게임에도 먹혀서 혼자 문이 열리고 경기장으로 넘어갔다.
///
/// ★★ <b>영문으로만 받는다.</b> 한글은 IME 를 거쳐서 <b>키 이벤트로 안 들어온다</b> —
///   이 프로젝트는 Input System 전용이라 <c>GUI.TextField</c> 도 글자를 못 받는다
///   (2026-09-18 에 접수대 시계에서 이미 겪은 함정). 그래서 <b>A~Z 와 숫자를 직접 읽는다.</b>
///
/// ── ★★★ 암호를 코드에 적지 않는다 ────────────────────────────────────
/// 저장된 것은 <b>SHA-256 해시</b>뿐이다. 해시에서 원래 글자는 못 되돌리니
/// <b>저장소·커밋·문서 어디에도 암호가 남지 않는다.</b>
/// 바꾸려면 새 단어를 알려주고 <see cref="Hash"/> 한 줄만 갈아끼운다.
///
/// ★ 한 번 열면 <b>그 실행 동안</b> 유지되고 ▶ 를 껐다 켜면 잠긴다 —
///   저장해 두면 «지금 잠겨 있나» 를 알 수가 없어서 일부러 안 남긴다.
/// ★ <b>이 파일 통째로 `#if UNITY_EDITOR`</b> 다. 빌드에는 존재하지도 않는다.
/// </summary>
public class DevUnlock : MonoBehaviour
{
    /// <summary>열렸나. <see cref="Dev.Enabled"/> 가 이 값을 본다.</summary>
    public static bool Open { get; private set; }

    /// <summary>암호 칸이 떠 있나.</summary>
    public static bool Typing { get; private set; }

    // ★ 암호의 SHA-256 해시. <b>평문은 여기에도, 커밋에도, 문서에도 없다.</b>
    const string Hash = "ff4666180671cb78abcb8381b62a9e36b984e8d712e0847c28bab637c5466a7a";

    const int Window = 32;
    static readonly StringBuilder box = new();     // 암호 칸에 친 것
    static float shownAt = -99f;
    static string note = "";
    static string banner = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Open = false;              // ▶ 를 다시 누르면 잠긴다
        Typing = false;

        box.Clear();
        note = "";
        if (Object.FindFirstObjectByType<DevUnlock>() == null)
            Object.DontDestroyOnLoad(new GameObject("DevUnlock").AddComponent<DevUnlock>());
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        bool ctrl = k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed;
        bool shift = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;

        // Ctrl + Shift + X — 도로 잠근다. 다시 열려면 암호를 또 넣어야 한다.
        if (ctrl && shift && k.xKey.wasPressedThisFrame)
        {
            if (Open)
            {
                Open = false;
                Typing = false;
                box.Clear();

                Say("그래 이감아, 거기서 좋은 시간 보내.");
            }
            return;
        }

        // Ctrl + Shift + D — 칸을 열고 닫는다.
        if (ctrl && shift && k.dKey.wasPressedThisFrame)
        {
            // ★ 이미 열려 있으면 칸을 또 띄우지 않는다. 두 번 넣을 이유가 없고,
            //   열린 줄 모르고 다시 치다가 뒤에서 뭐가 또 열리는 게 더 나쁘다.
            if (Open) { Say("이감아 이미 열려져 있어. 그만해도 돼."); return; }

            Typing = !Typing;
            box.Clear();
            note = "";
            return;
        }

        if (Typing) TypeInBox(k);
    }

    // ── 암호 칸 ──────────────────────────────────────────────────────────
    void TypeInBox(Keyboard k)
    {
        if (k.escapeKey.wasPressedThisFrame) { Typing = false; box.Clear(); return; }

        if (k.backspaceKey.wasPressedThisFrame && box.Length > 0)
            box.Remove(box.Length - 1, 1);

        if (Keys.Confirm(k))
        {
            if (Sha(box.ToString().ToLowerInvariant()) == Hash) { Unlock(); Typing = false; }
            else { note = "틀렸다 — 한/영을 영문으로"; box.Clear(); }
            return;
        }

        Grab(k, box);
    }

    // ★★★ 2026-10-14 — <b>「칸 없이 그냥 치기」를 걷어냈다.</b>
    //
    // 제작자: *"키 입력할 때 저절로 레이싱으로 들어오고 혼자 조작이 돼."*
    //
    // <b>그럴 수밖에 없었다.</b> 칸을 안 열고 치면 그 글자가 <b>게임에도 그대로 먹힌다</b> —
    // <c>E</c> 는 남쪽 문을 열어 경기장으로 보내고, <c>A</c>·<c>D</c> 는 걷고,
    // <c>R</c> 은 카트를 되돌리고, <c>T</c> 는 이야기를 튼다.
    // <see cref="Dev.Typing"/> 가 다른 Update 를 멈추긴 하는데 그건 <b>칸이 열려 있을 때만</b>이라
    // 이 경로는 처음부터 보호 밖이었다.
    //
    // 「손에 익으면 그쪽이 빠르다」고 넣었던 편의인데, <b>치는 동안 게임이 제멋대로 움직이는</b>
    // 대가를 치른다. 여는 길은 <b>Ctrl+Shift+D 하나</b>면 충분하다.

    /// <summary>A~Z 와 0~9 만 모은다. 한글은 여기까지 안 온다(IME 가 먹는다).</summary>
    static void Grab(Keyboard k, StringBuilder into)
    {
        for (var key = Key.A; key <= Key.Z; key++)
            if (k[key].wasPressedThisFrame) into.Append((char)('a' + (key - Key.A)));
        for (var key = Key.Digit1; key <= Key.Digit9; key++)
            if (k[key].wasPressedThisFrame) into.Append((char)('1' + (key - Key.Digit1)));
        if (k.digit0Key.wasPressedThisFrame) into.Append('0');
        if (into.Length > Window) into.Remove(0, into.Length - Window);
    }

    static void Unlock()
    {
        Open = true;

        box.Clear();
        note = "";
        Say("개발용 키 열림");
        Debug.Log("[개발] 개발용 키가 열렸다. Ctrl+Shift+X 로 잠그고, ▶ 를 다시 켜도 잠긴다.");
    }

    /// <summary>화면 왼쪽 위에 3초 띄운다. 눌렀는데 아무 말이 없으면 고장과 구분이 안 된다.</summary>
    static void Say(string line)
    {
        banner = line;
        shownAt = Time.unscaledTime;
    }

    void OnGUI()
    {
        if (!Typing && Time.unscaledTime - shownAt > 3f) return;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));

        if (Typing)
        {
            Hud.Fill(new Rect(0f, 0f, screen.width, screen.height),
                     new Color(0.03f, 0.03f, 0.05f, 0.55f));

            var card = new Rect(screen.width * 0.5f - 190f, screen.height * 0.5f - 64f, 380f, 128f);
            Hud.NoteCard(card);
            var inner = Hud.NoteInner(card);

            GUI.Label(new Rect(inner.x, inner.y + 4f, inner.width, 26f), "관리자 입력",
                      Hud.Resize(Hud.Title, 18, TextAnchor.UpperCenter));

            var slot = new Rect(inner.x + 18f, inner.y + 40f, inner.width - 36f, 30f);
            Hud.Chip(slot);
            string shown = box.ToString().ToUpperInvariant();
            if (Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f) shown += "_";
            GUI.Label(slot, shown, Hud.Resize(Hud.Value, 17, TextAnchor.MiddleCenter));

            GUI.Label(new Rect(inner.x, inner.yMax - 28f, inner.width, 22f),
                      string.IsNullOrEmpty(note) ? "ENTER 확인 · ESC 닫기   (영문만)" : note,
                      Hud.Resize(Hud.Label, 12, TextAnchor.MiddleCenter));
        }
        else
        {
            var style = Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter);
            float wide = Mathf.Max(196f, style.CalcSize(new GUIContent(banner)).x + 28f);
            var chip = new Rect(16f, 16f, wide, 30f);
            Hud.Chip(chip);
            GUI.Label(chip, banner, style);
        }

        Hud.End();
    }

    static string Sha(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
#endif
