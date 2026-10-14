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
/// ── 왜 이게 필요한가 ───────────────────────────────────────────────────
/// <see cref="Dev"/> 는 <b>빌드에서는</b> 이미 상수 false 라 컴파일 단계에서 사라진다.
/// 그런데 <b>에디터에서는 늘 켜져 있어서</b> 화면 아래에 「1~5 장면 고르기 · F9 전시품 전부
/// 수집 / F10 전부 지우기」 같은 줄이 그대로 떠 있었다 — 화면을 보여줄 때마다
/// <b>개발용 장치가 같이 보인다.</b>
///
/// 이제 에디터에서도 <b>기본은 잠김</b>이고, 암호를 타이핑해야 열린다.
/// 열면 안내 문구도 같이 돌아온다 — 전부 <c>Dev.Enabled</c> 뒤에 있어서 공짜다.
///
/// ── ★★★ 암호를 코드에 적지 않는다 ────────────────────────────────────
/// 저장된 것은 <b>SHA-256 해시</b>뿐이다. 해시에서 원래 글자는 못 되돌리니
/// <b>저장소·커밋·문서 어디에도 암호가 남지 않는다.</b>
/// 바꾸려면 새 단어를 알려주고 <see cref="Hash"/> 한 줄만 갈아끼운다.
///
/// ── 쓰는 법 ────────────────────────────────────────────────────────────
/// 아무 씬에서나 <b>그냥 타이핑</b>한다(입력칸 없음). 맞으면 화면 왼쪽 위에 한 줄 뜬다.
/// 한 번 열면 <b>그 실행 동안</b> 유지되고, ▶ 를 껐다 켜면 다시 잠긴다 —
/// 저장해 두면 «지금 잠겨 있나» 를 알 수가 없어서 일부러 안 남긴다.
///
/// ★ 글자를 받는 창(<see cref="DeskClock"/>)이 떠 있으면 듣지 않는다 — 섞이면 안 된다.
/// ★ <b>이 파일 통째로 `#if UNITY_EDITOR`</b> 다. 빌드에는 존재하지도 않는다.
/// </summary>
public class DevUnlock : MonoBehaviour
{
    /// <summary>열렸나. <see cref="Dev.Enabled"/> 가 이 값을 본다.</summary>
    public static bool Open { get; private set; }

    // ★ 암호의 SHA-256 해시. <b>평문은 여기에도, 커밋에도, 문서에도 없다.</b>
    const string Hash = "f343aeefeca2df2a6f6d4b30ea8cb6572dc73f119f1f9c72732d688c55530df3";

    const int Window = 24;         // 최근 이만큼만 들고 본다 — 길게 쌓을 이유가 없다
    static readonly StringBuilder typed = new();
    static float shownAt = -99f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        Open = false;              // ▶ 를 다시 누르면 잠긴다
        typed.Clear();
        if (Object.FindFirstObjectByType<DevUnlock>() == null)
            Object.DontDestroyOnLoad(new GameObject("DevUnlock").AddComponent<DevUnlock>());
    }

    void Update()
    {
        if (Open) return;

        var k = Keyboard.current;
        if (k == null || !k.anyKey.wasPressedThisFrame) return;

        // 글자를 받는 창이 떠 있으면 거기 것이다.
        if (DeskClock.PanelOpen) { typed.Clear(); return; }

        // a~z 만 본다. 숫자·기능키는 이미 디버그 키라 섞으면 서로 방해한다.
        for (var key = Key.A; key <= Key.Z; key++)
            if (k[key].wasPressedThisFrame) typed.Append((char)('a' + (key - Key.A)));

        if (typed.Length == 0) return;
        if (typed.Length > Window) typed.Remove(0, typed.Length - Window);

        // 끝에서부터 잘라 보며 맞는 게 있나 — 앞에 뭘 더 쳤어도 걸린다.
        string s = typed.ToString();
        for (int cut = 0; cut < s.Length; cut++)
            if (Sha(s.Substring(cut)) == Hash) { Unlock(); return; }
    }

    static void Unlock()
    {
        Open = true;
        shownAt = Time.unscaledTime;
        typed.Clear();
        Debug.Log("[개발] 개발용 키가 열렸다. ▶ 를 다시 켜면 잠긴다.");
    }

    void OnGUI()
    {
        if (!Open || Time.unscaledTime - shownAt > 3f) return;

        var screen = Hud.Begin(Resources.Load<Font>("HudFont"));
        var chip = new Rect(16f, 16f, 196f, 30f);
        Hud.Chip(chip);
        GUI.Label(chip, "개발용 키 열림",
                  Hud.Resize(Hud.Text, 14, TextAnchor.MiddleCenter));
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
