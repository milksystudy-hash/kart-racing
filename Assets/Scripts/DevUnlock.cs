#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ★★ <b>개발용 키를 암호로 연다.</b> (2026-10-14 수연 요청)
///
/// 수연: *"플레이어들은 못 찾게, G 키로 관리자 모드 들어간 거랑 장면 번호 눌러서 보는 거랑
/// 그런 건 없애지 말고 우리 시크릿 코드 입력하면 들어가 있게."*
///
/// ── 왜 필요한가 ────────────────────────────────────────────────────────
/// <see cref="Dev"/> 는 <b>빌드에서는</b> 이미 상수 false 라 컴파일 단계에서 사라진다.
/// 그런데 <b>에디터에서는 늘 켜져 있어서</b> 화면에 「1~5 장면 고르기 · F9 전시품 전부 수집」
/// 같은 줄이 그대로 떠 있었다 — 화면을 보여줄 때마다 개발용 장치가 같이 보인다.
///
/// ── 여는 법 ────────────────────────────────────────────────────────────
/// <b>Ctrl + Shift + D</b> → 암호 칸이 열린다 → 치고 <b>ENTER</b>. ESC 로 닫는다.
/// (칸을 안 열고 <b>그냥 타이핑</b>해도 걸린다 — 손에 익으면 그쪽이 빠르다.)
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
    const string Hash = "6173fb31d5b9cc831021d50f2df24cfdef44b70abe0b96f5ec5db20e01a92ba6";

    const int Window = 32;
    static readonly StringBuilder typed = new();   // 칸 없이 그냥 칠 때
    static readonly StringBuilder box = new();     // 암호 칸에 친 것
    static float shownAt = -99f;
    static string note = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Open = false;              // ▶ 를 다시 누르면 잠긴다
        Typing = false;
        typed.Clear();
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

        // Ctrl + Shift + D — 칸을 열고 닫는다.
        if (ctrl && shift && k.dKey.wasPressedThisFrame)
        {
            Typing = !Typing;
            box.Clear();
            note = "";
            return;
        }

        if (Typing) { TypeInBox(k); return; }
        if (Open) return;

        ListenQuietly(k);
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

    // ── 칸 없이 그냥 치기 ────────────────────────────────────────────────
    void ListenQuietly(Keyboard k)
    {
        if (!k.anyKey.wasPressedThisFrame) return;
        if (DeskClock.PanelOpen) { typed.Clear(); return; }

        Grab(k, typed);
        if (typed.Length == 0) return;

        string s = typed.ToString();
        for (int cut = 0; cut < s.Length; cut++)
            if (Sha(s.Substring(cut)) == Hash) { Unlock(); return; }
    }

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
        shownAt = Time.unscaledTime;
        typed.Clear();
        box.Clear();
        note = "";
        Debug.Log("[개발] 개발용 키가 열렸다. ▶ 를 다시 켜면 잠긴다.");
    }

    void OnGUI()
    {
        if (!Typing && (!Open || Time.unscaledTime - shownAt > 3f)) return;

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
            var chip = new Rect(16f, 16f, 196f, 30f);
            Hud.Chip(chip);
            GUI.Label(chip, "개발용 키 열림", Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));
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
