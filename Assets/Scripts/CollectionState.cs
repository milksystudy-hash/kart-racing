using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이스에서 모은 수집품을 기억한다. 전시실이 이걸 읽어서 진열장을 채운다.
///
/// 기획서 §3.7 대로 PlayerPrefs 에 저장한다 — 게임을 껐다 켜도 남는다.
/// 파일을 따로 만들거나 서버를 붙일 필요가 없어서, 이 규모엔 이게 맞다.
///
/// 코인 개수 같은 "레이스 도중의 숫자"는 여기 안 넣는다. 그건 재시작할 때마다
/// 0 으로 돌아가야 하니까(§10.2 검수 항목) 레이스 쪽에서 따로 센다.
/// 여기 들어오는 건 **한 번 얻으면 영구히 남는 것**뿐이야.
/// </summary>
public static class CollectionState
{
    const string PrefsKey = "Racing.Collected";

    static HashSet<string> collected;

    static HashSet<string> Loaded
    {
        get
        {
            if (collected != null) return collected;

            collected = new HashSet<string>();
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            foreach (var id in saved.Split(','))
                if (!string.IsNullOrWhiteSpace(id)) collected.Add(id.Trim());

            return collected;
        }
    }

    public static bool Has(string id) => !string.IsNullOrEmpty(id) && Loaded.Contains(id);

    public static int Count => Loaded.Count;

    /// <summary>레이스에서 수집품을 얻었을 때 부른다. 이미 있으면 아무 일도 안 한다.</summary>
    public static void Collect(string id)
    {
        if (string.IsNullOrEmpty(id) || !Loaded.Add(id)) return;
        Save();
        Debug.Log($"[수집] {id} — 전시실에 진열됨 (총 {Loaded.Count}개)");
    }

    /// <summary>테스트용. 전부 지운다.</summary>
    /// <summary>
    /// 진행 기록을 통째로 되돌린다(로비 F10 · 전시실 디버그).
    /// <b>결승 클리어도 같이 지운다</b> — 수집품만 비우고 결승은 깬 걸로 남아 있으면
    /// 여덟 판을 다시 돌아도 결승이 안 열려서, 그 상태를 두 번 다시 못 본다.
    /// </summary>
    public static void ClearAll()
    {
        Loaded.Clear();
        Save();
        GrandFinal.Reset();

        // ★ 2026-09-28 <b>장 번호와 본 장면도 같이 되돌린다.</b> 전에는 수집품만 비워서
        // «장 4 · 수집품 0» 이라는 있을 수 없는 상태가 남았고, 그 조합에서
        // `StoryScript.CurrentScene()` 이 빈 문자열이라 <b>T 를 눌러도 아무 일도 안 났다.</b>
        // F10 은 «처음부터» 라는 뜻이니 이야기도 처음으로 가는 게 맞다.
        StoryProgress.ResetStory();
    }

    /// <summary>테스트용. 전시실을 꽉 채워서 확인할 때.</summary>
    public static void CollectAll(IEnumerable<string> ids)
    {
        foreach (var id in ids) Loaded.Add(id);
        Save();
    }

    static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(",", Loaded));
        PlayerPrefs.Save();
    }
}
