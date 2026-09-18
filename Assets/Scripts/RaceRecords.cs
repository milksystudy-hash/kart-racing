using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>잘 달린 기록 세 개를 남긴다.</b>
///
/// 2026-09-18 유저: *"여러 번 완주했을 때 총 3위까지 잘한 기록이 나왔으면 해.
/// 1:3 할 때 '전에 1:3 했을 때보다 이번이 더 잘 나왔네?' 하고 느낄 수 있게끔."*
///
/// 맞는 요구다. 지금 완주 화면은 <b>이번 판 숫자만</b> 보여줘서, 잘 달렸는지 못 달렸는지를
/// 판단할 기준이 없다. 기준이 없으면 두 번째 판을 돌 이유도 없어 —
/// 레이싱 게임에서 다시 달리게 만드는 건 상대가 아니라 <b>어제의 나</b>다.
///
/// <b>수집품과 같은 방식으로 PlayerPrefs 에 남긴다.</b> 파일도 서버도 필요 없고,
/// 게임을 껐다 켜도 남는다(기획서 §3.7).
///
/// 기록은 <b>총 시간 기준</b>으로 줄을 세운다. 순위(1~4위)는 같이 적어 두지만 정렬에는
/// 안 쓴다 — AI 실력이 판마다 같아서 순위는 거의 안 바뀌는데 시간은 매번 달라지거든.
/// </summary>
public static class RaceRecords
{
    const string PrefsKey = "Racing.Records";
    const int Keep = 3;

    public struct Row
    {
        public float total;     // 총 시간(초)
        public float bestLap;   // 최고 랩(초)
        public int place;       // 들어온 순위. 혼자 달렸으면 1
        public int racers;      // 같이 달린 대수
        public string castId;   // 어느 카트로 달렸나
    }

    static List<Row> rows;

    static List<Row> Loaded
    {
        get
        {
            if (rows != null) return rows;

            rows = new List<Row>();
            foreach (var line in PlayerPrefs.GetString(PrefsKey, "").Split(';'))
            {
                var p = line.Split(',');
                if (p.Length < 5) continue;
                if (!float.TryParse(p[0], out float t)) continue;

                rows.Add(new Row
                {
                    total = t,
                    bestLap = float.TryParse(p[1], out float b) ? b : -1f,
                    place = int.TryParse(p[2], out int pl) ? pl : 0,
                    racers = int.TryParse(p[3], out int rc) ? rc : 1,
                    castId = p[4],
                });
            }
            return rows;
        }
    }

    /// <summary>빠른 순서대로. 최대 세 줄.</summary>
    public static IReadOnlyList<Row> Best => Loaded;

    /// <summary>
    /// <b>캐릭터마다 제일 잘한 기록 한 줄씩.</b> 2026-09-18 유저: *"한세운 기준으로 달렸는데
    /// 이전에 플레이한 정이감 기록만 쫘르륵 나온다. 넷 다 이름 써주고 기록 비교해 줘."*
    ///
    /// 전에는 <b>전체에서 상위 셋</b>만 남겨서, 한 번 잘 달린 캐릭터가 표를 독차지했다 —
    /// 다른 카트를 골라 아무리 잘 달려도 «잘 달린 기록» 에 자기 이름이 안 뜬다.
    /// 비교하려면 <b>줄마다 다른 사람</b>이어야 한다.
    /// </summary>
    public static List<Row> BestPerCast()
    {
        var best = new Dictionary<string, Row>();
        foreach (var r in Loaded)
        {
            string key = r.castId ?? "";
            if (!best.TryGetValue(key, out var cur) || r.total < cur.total)
                best[key] = r;
        }

        var rows = new List<Row>(best.Values);
        rows.Sort((a, b) => a.total.CompareTo(b.total));
        return rows;
    }

    /// <summary>이번 기록이 몇 번째로 좋은지. 1~3 이면 표에 올랐고, 0 이면 못 들었다.</summary>
    public static int Submit(float total, float bestLap, int place, int racers, string castId)
    {
        if (total <= 0f) return 0;

        var row = new Row
        {
            total = total,
            bestLap = bestLap,
            place = place,
            racers = racers,
            castId = string.IsNullOrEmpty(castId) ? "" : castId,
        };

        Loaded.Add(row);
        Loaded.Sort((a, b) => a.total.CompareTo(b.total));

        // ★ <b>남기는 것도 캐릭터마다 따로.</b> 전체 상위 셋만 남기면 한 번 잘 달린 캐릭터가
        // 표를 독차지하고, 다른 카트로 아무리 잘 달려도 자기 기록이 그 자리에서 밀려 사라진다.
        var kept = new Dictionary<string, int>();
        for (int i = Loaded.Count - 1; i >= 0; i--)
        {
            string key = Loaded[i].castId ?? "";
            kept.TryGetValue(key, out int n);
            if (n >= Keep) Loaded.RemoveAt(i); else kept[key] = n + 1;
        }

        // 등수도 <b>같은 캐릭터 안에서</b> 센다 — "내 기록을 내가 깼다" 가 보여야 다시 달린다.
        int rank = 0, seen = 0;
        foreach (var r in Loaded)
        {
            if ((r.castId ?? "") != row.castId) continue;
            seen++;
            if (Mathf.Approximately(r.total, row.total) && Mathf.Approximately(r.bestLap, row.bestLap))
            {
                rank = seen;
                break;
            }
        }

        Save();
        return rank <= Keep ? rank : 0;
    }

    /// <summary>테스트용. 기록을 전부 지운다.</summary>
    public static void ClearAll()
    {
        Loaded.Clear();
        Save();
    }

    static void Save()
    {
        var parts = new List<string>();
        foreach (var r in Loaded)
            parts.Add($"{r.total:F2},{r.bestLap:F2},{r.place},{r.racers},{r.castId}");

        PlayerPrefs.SetString(PrefsKey, string.Join(";", parts));
        PlayerPrefs.Save();
    }
}
