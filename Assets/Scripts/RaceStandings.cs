using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 달리는 카트들의 순위를 매긴다. 플레이어 한 명 + AI 세 대를 기준으로 만들었지만,
/// 몇 대가 있든 씬에 있는 <see cref="RaceProgress"/> 를 전부 세운다.
///
/// **등수는 필수 조건이 아니다.** 기획서 §3.3 의 필수 임무는 완주·수집·충돌·시간이고,
/// 1위는 **선택 임무**로 둔다 — AI 가 덜 완성돼도 이야기가 막히지 않게 하려는 거야.
/// 그래서 3등으로 들어와도 다음 장은 열린다. 1위는 추가 보상일 뿐.
/// </summary>
public class RaceStandings : MonoBehaviour
{
    [Tooltip("비워두면 씬에서 알아서 다 찾는다")]
    public RaceProgress[] racers;

    [Tooltip("플레이어 카트의 것. 화면에 내 등수를 띄우려고 따로 들고 있는다")]
    public RaceProgress playerRacer;

    /// <summary>1부터. 아직 계산 전이면 0.</summary>
    public int PlayerPlace { get; private set; }
    public int RacerCount => racers != null ? racers.Length : 0;

    /// <summary>플레이어가 1등으로 완주했는지 — 선택 임무 판정에 쓴다.</summary>
    public bool PlayerFinishedFirst { get; private set; }

    readonly List<RaceProgress> sorted = new();

    void Awake() => Recount();

    /// <summary>
    /// 지금 <b>켜져 있는</b> 카트만 센다. AI 가 꺼진 판에서 꺼진 카트까지 세면
    /// "4대 중 1위" 라고 뜨는데 화면에는 나 혼자다(2026-09-17).
    /// </summary>
    public void Recount()
    {
        var found = FindObjectsByType<RaceProgress>(FindObjectsSortMode.None);
        var live = new List<RaceProgress>();
        foreach (var racer in found)
            if (racer != null && racer.gameObject.activeInHierarchy) live.Add(racer);
        racers = live.ToArray();
    }

    void Update()
    {
        if (racers == null || racers.Length == 0) return;

        sorted.Clear();
        foreach (var racer in racers)
            if (racer != null) sorted.Add(racer);

        // 앞선 순서대로. 완주한 카트는 아직 달리는 카트보다 항상 앞이다.
        sorted.Sort((a, b) =>
        {
            if (a.Finished != b.Finished) return a.Finished ? -1 : 1;
            return b.RankScore.CompareTo(a.RankScore);
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i] != playerRacer) continue;
            PlayerPlace = i + 1;

            if (playerRacer != null && playerRacer.Finished && PlayerPlace == 1)
                PlayerFinishedFirst = true;
            break;
        }
    }

    /// <summary>등수를 "1위 / 2위" 처럼 읽기 좋게.</summary>
    public static string PlaceLabel(int place) => place <= 0 ? "—" : $"{place}위";

    public void ResetRace()
    {
        PlayerFinishedFirst = false;
        PlayerPlace = 0;
        foreach (var racer in racers)
            if (racer != null) racer.ResetRace();
    }
}
