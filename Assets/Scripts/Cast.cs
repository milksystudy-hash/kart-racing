using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이야기 장면에 나오는 사람들. 이름표 색과 2D 초상화 자리를 여기 한 군데서 정한다.
///
/// 대사는 사람을 **id** 로 가리킨다("세진"). 화면에 뜨는 이름("한세진")과 이름표 색은
/// 여기서만 바꾸면 모든 대사에 한꺼번에 반영된다 — 대사 파일을 뒤질 필요가 없다.
///
/// 초상화는 <b>Assets/Resources/Portraits/</b> 에 넣으면 자동으로 잡힌다.
/// 파일 이름 규칙은 <c>{id}_{표정}.png</c> — 예: <c>세진_기쁨.png</c>, <c>이감_기본.png</c>.
/// 아직 그림이 없으면 HUD 가 이 사람 색의 회색 네모를 대신 그린다. 그림이 들어오는 순간
/// 코드는 한 줄도 안 고치고 바뀐다.
///
/// 기획서 §3.6: 캐릭터당 <b>기본 · 기쁨 · 당황</b> 세 장을 우선한다.
/// </summary>
public static class Cast
{
    /// <summary>표정 세 장. 악당의 '당황'은 분노 표정으로 그려도 된다 — 쓰이는 자리가 같다.</summary>
    public enum Mood { 기본, 기쁨, 당황 }

    /// <summary>speakerId 가 빈 문자열이면 나레이션 — 이름표 없이 가운데 글로 나온다.</summary>
    public const string Narrator = "";

    public struct Member
    {
        /// <summary>대사에서 부르는 짧은 이름. 이걸 바꾸면 대사도 같이 고쳐야 한다.</summary>
        public string id;
        /// <summary>화면 이름표에 뜨는 이름.</summary>
        public string name;
        /// <summary>이름표 색. 초상화가 없을 때 자리표시 네모 색으로도 쓴다.</summary>
        public Color color;
        /// <summary>참고용 한 줄 설정. 대사 쓸 때 말투가 흔들리지 말라고 적어둔 거야.</summary>
        public string note;
    }

    // ── 색은 카트 색에 맞춘다 (2026-09-15 유저 확정) ─────────────────────────
    //   한세진 초록 · 한세운 하늘 · 한시우 파랑 · 정이감 빨강
    //
    // 이름표는 흰 글자를 얹기 때문에 카트 색 그대로 쓰면 글자가 안 읽힌다
    // (카트 라임 #67A829 는 흰 글자 대비 2.9 로 기준 미달). 같은 색조에서 한 단계
    // 어둡게 잡아 대비 4 이상을 맞췄다 — 색은 같은 계열로 읽히고 글자는 살아남는다.
    public static readonly Member[] All =
    {
        new Member { id = "이감", name = "정이감",  color = Hex(0xC2403C),
            note = "인간 주인공. 관찰하고 증거를 연결한다. 곰 삼 형제를 가족으로 대한다." },
        new Member { id = "시우", name = "한시우",  color = Hex(0x2B4E9B),
            note = "장남. 책임감이 강하고 안전을 우선한다 (ENFJ). 말리는 쪽." },
        new Member { id = "세운", name = "한세운",  color = Hex(0x357FB0),
            note = "둘째. 다정하고 분위기를 살린다 (ENFP). 농담으로 공기를 푼다. 카트 WOON_CART_FIN(하늘)." },
        new Member { id = "세진", name = "한세진",  color = Hex(0x4F8C24),
            note = "막내. 행동이 빠르고 위험을 즐긴다 (ESTP). 사고를 친다. 카트 JIN_FIN_CART(초록)." },
        new Member { id = "관장", name = "박물관장", color = Hex(0x8C7B9E),
            note = "조건부 매각에 서명했다. 비리를 계획하진 않았지만 사실을 숨겼다." },
        // 악당 둘은 기획서 §4.4 대로 금색·자홍색 — 박물관 색조와 일부러 부딪히게
        new Member { id = "개발업자", name = "개발업자", color = Hex(0xC9A227),
            note = "골든베어 리조트를 밀어붙인다. 대회를 철거 홍보로 쓴다. 5번 잠긴 자리." },
        new Member { id = "시의원", name = "시의원",   color = Hex(0xB0407F),
            note = "카메라 앞에서만 문화 보존을 말한다. 6번 잠긴 자리의 배후." },
    };

    /// <summary>
    /// 로비 캐릭터 자리 순서 (기획서 §3.5 — 5·6번은 잠긴 자리).
    ///
    /// 자리 번호만 알아도 누구인지 알 수 있게 여기 둔다. 예전 씬이라 자리에 id 가
    /// 안 박혀 있어도 이걸로 메울 수 있어서, 씬을 다시 굽기 전에도 카트가 맞게 나온다.
    /// </summary>
    public static readonly string[] RacerIds = { "이감", "시우", "세운", "세진", "개발업자", "시의원" };

    public static string RacerIdAt(int index) =>
        index >= 0 && index < RacerIds.Length ? RacerIds[index] : "";

    static readonly Color NarratorColor = new Color(0.72f, 0.70f, 0.66f);

    /// <summary>0xRRGGBB 를 색으로. 클립스튜디오에서 고른 색을 그대로 옮겨 적을 수 있게.</summary>
    static Color Hex(int rgb) => new Color32(
        (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 0xFF);

    public static Member Of(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return All[i];

        // 목록에 없는 이름이라도 대사는 멈추지 않는다. 오타를 눈으로 바로 잡으라고 회색으로 나온다.
        return new Member { id = id, name = id, color = NarratorColor, note = "" };
    }

    public static string NameOf(string id) => string.IsNullOrEmpty(id) ? "" : Of(id).name;

    public static Color ColorOf(string id) =>
        string.IsNullOrEmpty(id) ? NarratorColor : Of(id).color;

    /// <summary>
    /// 그 색 위에 글자를 얹을 때 쓸 글자 색. 밝은 바탕이면 검정, 어두우면 흰색.
    ///
    /// 이름표를 늘 흰 글자로 그렸더니 개발업자의 금색(#C9A227) 위에서 글자가 사라졌다.
    /// 금색은 기획서 §4.4 가 정한 악당 색이라 바꿀 게 아니라 글자 쪽을 바꿔야 한다.
    /// 나중에 네가 색을 바꿔도 이 판정이 알아서 따라간다.
    /// </summary>
    public static Color TextOn(Color background)
    {
        float luminance = 0.2126f * ToLinear(background.r)
                        + 0.7152f * ToLinear(background.g)
                        + 0.0722f * ToLinear(background.b);

        // 흰 글자 대비가 4 를 넘으면 흰색, 아니면 검정이 낫다
        return (1.05f / (luminance + 0.05f)) >= 4f
            ? Color.white
            : new Color(0.10f, 0.08f, 0.06f);
    }

    static float ToLinear(float v) =>
        v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);

    // ------------------------------------------------------------------
    //  초상화 — 있으면 쓰고, 없으면 null (HUD 가 자리표시를 그린다)
    // ------------------------------------------------------------------

    const string PortraitFolder = "Portraits";

    // Resources.Load 는 싸지 않은데 OnGUI 는 매 프레임 돈다. 없다는 사실까지 기억해둔다.
    static readonly Dictionary<string, Texture2D> cache = new();

    public static Texture2D Portrait(string id, Mood mood)
    {
        if (string.IsNullOrEmpty(id)) return null;

        string key = $"{id}_{mood}";
        if (cache.TryGetValue(key, out var hit)) return hit;

        var tex = Resources.Load<Texture2D>($"{PortraitFolder}/{key}");

        // 그 표정만 아직 없으면 기본 표정으로 대신한다. 세 장을 한꺼번에 그릴 필요가 없게.
        if (tex == null && mood != Mood.기본)
            tex = Resources.Load<Texture2D>($"{PortraitFolder}/{id}_{Mood.기본}");

        cache[key] = tex;
        return tex;
    }

    /// <summary>초상화 파일을 새로 넣었을 때 에디터에서 다시 읽게 한다.</summary>
    public static void ClearPortraitCache() => cache.Clear();
}
