using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ★ <b>통 플레이 자가점검.</b> 배치모드 전용, 메뉴 없음. (2026-10-13)
///
/// 제작자: *"통 플레이 자가점검으로 돌려보고 버그 있음 고치고, 스토리 이상한 거 있음 고치고,
/// 떡밥이나 그런 거 잘 지키고 추리할 수 있는 요소 있는지 확인좀."*
///
/// ★★ <b>수집 기록을 빌렸다가 반드시 돌려준다.</b> 게이트를 재려면 진행 기록을 바꿔야 하는데
/// 그게 <b>제작자의 실제 진행</b>이다. 다섯 키를 처음에 적어 두고 끝에 그대로 되돌린다 —
/// 미러 프로젝트에서 「검사가 유저 기록을 날렸다」를 이미 한 번 겪었다(2026-09-17).
/// </summary>
public static class _Full
{
    static string Out => System.Environment.GetEnvironmentVariable("FULL_OUT")
                         ?? "C:/temp/full.txt";

    static readonly StringBuilder sb = new();
    static int problems;

    static void H(string s) { sb.AppendLine(); sb.AppendLine("== " + s + " =="); }
    static void L(string s) => sb.AppendLine("   " + s);
    static void Bad(string s) { sb.AppendLine("*  " + s); problems++; }

    // ==================================================================
    public static void Run()
    {
        var saved = Snapshot();
        try
        {
            Story();
            Clues();
            Portraits();
            Gates();
            Missions();
            SceneFiles();
            Audio();
        }
        catch (System.Exception e) { Bad("점검이 중간에 터졌다: " + e); }
        finally { Restore(saved); }

        sb.AppendLine();
        sb.AppendLine(problems == 0 ? "== 문제 0건 ==" : $"== 문제 {problems}건 ==");

        Directory.CreateDirectory(Path.GetDirectoryName(Out));
        File.WriteAllText(Out, sb.ToString());
        Debug.Log(sb.ToString());
    }

    // ---- 진행 기록을 빌렸다 돌려준다 -------------------------------
    //  ★ 키 이름은 각 스크립트의 const 와 <b>같아야 한다.</b> 하나라도 빠뜨리면
    //    점검이 끝난 뒤 제작자의 진행이 어긋난 채로 남는다.
    const string KCollect = "Racing.Collected";
    const string KChapter = "Racing.Chapter";
    const string KSeen = "Racing.StorySeen";
    const string KFinal = "결승클리어";
    const string KCast = "고른캐릭터";

    static (string col, int ch, string seen, int fin, string cast) Snapshot() =>
        (PlayerPrefs.GetString(KCollect, ""), PlayerPrefs.GetInt(KChapter, 0),
         PlayerPrefs.GetString(KSeen, ""), PlayerPrefs.GetInt(KFinal, 0),
         PlayerPrefs.GetString(KCast, ""));

    static void Restore((string col, int ch, string seen, int fin, string cast) s)
    {
        PlayerPrefs.SetString(KCollect, s.col);
        PlayerPrefs.SetInt(KChapter, s.ch);
        PlayerPrefs.SetString(KSeen, s.seen);
        PlayerPrefs.SetInt(KFinal, s.fin);
        PlayerPrefs.SetString(KCast, s.cast);
        PlayerPrefs.Save();
    }

    /// <summary>앞에서부터 n개를 모은 상태로 만든다 — 진행 순서가 곧 카탈로그 순서다.</summary>
    static void SetCollected(int n)
    {
        CollectionState.ClearAll();
        if (n > 0) CollectionState.CollectAll(ExhibitCatalogue.All.Take(n).Select(e => e.id));
    }

    static string TitleOf(MissionManager.Goal g)
    {
        try { return RaceVoice.Title(g, null); }
        catch { return g.ToString() + " (수치는 씬에서)"; }
    }

    // ==================================================================
    //  이야기
    // ==================================================================
    static void Story()
    {
        H("이야기");

        var ids = StoryScript.All.Select(s => s.id).ToArray();
        L($"장면 {ids.Length}개");

        foreach (var g in ids.GroupBy(x => x).Where(g => g.Count() > 1))
            Bad($"장면 id 가 겹친다: {g.Key} ({g.Count()}개)");

        foreach (var s in StoryScript.All)
        {
            var lines = StoryScript.Playable(s.id);
            if (lines == null || lines.Count == 0) Bad($"[{s.id}] 대사가 0줄이다");
        }

        // 어느 길로도 안 닿는 장면이 있나
        var reach = new HashSet<string> { "final_before", "final_after", "epilogue" };
        for (int c = 0; c <= 5; c++)
        {
            string id = StoryScript.SceneForChapter(c);
            if (!string.IsNullOrEmpty(id)) reach.Add(id);
        }
        for (int n = 0; n <= 8; n++)
        {
            SetCollected(n);
            string id = StoryScript.CurrentScene();
            if (string.IsNullOrEmpty(id)) continue;
            reach.Add(id);
            if (!ids.Contains(id)) Bad($"수집품 {n}개에서 없는 장면을 가리킨다: {id}");
        }

        var orphan = ids.Where(i => !reach.Contains(i)).ToArray();
        if (orphan.Length > 0)
            L("바로 안 걸리는 장면(앨범·후기로 나온다): " + string.Join(" · ", orphan));

        // 말하는 사람
        var known = new HashSet<string>(Cast.All.Select(c => c.id));
        var speakers = new Dictionary<string, int>();
        int narr = 0, total = 0;
        foreach (var s in StoryScript.All)
            foreach (var ln in StoryScript.Playable(s.id))
            {
                total++;
                if (string.IsNullOrEmpty(ln.speakerId)) { narr++; continue; }
                speakers[ln.speakerId] = speakers.TryGetValue(ln.speakerId, out int v) ? v + 1 : 1;
                if (!known.Contains(ln.speakerId)) Bad($"[{s.id}] Cast 에 없는 화자: {ln.speakerId}");
            }

        L($"대사 {total}줄 (해설 {narr}줄, {narr * 100f / total:0}%)");
        foreach (var kv in speakers.OrderByDescending(k => k.Value))
            L($"  {kv.Key,-6} {kv.Value,4}줄  {kv.Value * 100f / total:0}%");

        int bros = new[] { "시우", "세운", "세진" }.Sum(b => speakers.TryGetValue(b, out int v) ? v : 0);
        int gam = speakers.TryGetValue("이감", out int g2) ? g2 : 0;
        L($"  삼형제 합 {bros}줄 ({bros * 100f / total:0}%) · 이감 {gam}줄 ({gam * 100f / total:0}%)");

        // 장면마다 몇 줄인가 — 너무 길면 건너뛰고 싶어진다
        L("");
        foreach (var s in StoryScript.All)
        {
            int n = StoryScript.Playable(s.id).Count;
            // 프롤로그가 긴 건 알고 있는 것이다(123줄, 2026-10-12 에 129에서 줄였다).
            // 건너뛰기가 있으니 «문제» 가 아니라 «알아둘 것» 으로만 적는다.
            L($"  {s.id,-14} {n,3}줄{(n > 60 ? "   (길다 — 건너뛰기 있음)" : "")}");
        }
    }

    // ==================================================================
    //  단서(떡밥)
    // ==================================================================
    static void Clues()
    {
        H("단서(떡밥)");

        var notes = ClueText.All;
        L($"글 {notes.Length}개 · 캠퍼스 배치 {CampusBuilder.ClueLayout.Length}자리");

        foreach (var g in notes.GroupBy(n => n.id).Where(g => g.Count() > 1))
            Bad($"단서 id 가 겹친다: {g.Key}");

        var placed = new HashSet<string>(CampusBuilder.ClueLayout.Select(p => p.label));
        foreach (var n in notes)
        {
            if (!placed.Contains(n.id)) Bad($"[{n.id}] 글은 있는데 캠퍼스에 안 놓였다");
            if (n.lines == null || n.lines.Length == 0) Bad($"[{n.id}] 읽을 글이 비었다");
        }
        foreach (var p in CampusBuilder.ClueLayout)
            if (ClueText.Find(p.label) == null) Bad($"[{p.label}] 놓여 있는데 읽을 글이 없다");

        var byHall = CampusBuilder.ClueLayout.GroupBy(p => p.hall).OrderBy(g => g.Key);
        foreach (var hall in byHall)
            L($"  {hall.Key,-10} {string.Join(" · ", hall.Select(p => p.label))}");
    }

    // ==================================================================
    //  초상화
    // ==================================================================
    static void Portraits()
    {
        H("초상화");

        int have = 0, want = 0;
        foreach (var c in Cast.All)
            foreach (Cast.Mood m in System.Enum.GetValues(typeof(Cast.Mood)))
            {
                want++;
                if (Cast.Portrait(c.id, m) != null) have++;
                else Bad($"초상화 없음: {c.id}_{m}");
            }
        L($"{have} / {want}");
    }

    // ==================================================================
    //  판마다 나와야 할 것만 나오는가
    // ==================================================================
    static void Gates()
    {
        H("판마다 나오는 것");
        L("수집  임무                     AI  자재  광고  곰인형  결승  자유");

        for (int n = 0; n <= 8; n++)
        {
            SetCollected(n);
            GrandFinal.Reset();

            var goal = MissionManager.CurrentGoal;
            L($" {n}   {TitleOf(goal),-24}" +
              $"{(MissionManager.FinalRace ? "3" : "0"),2}" +
              $"{(MissionManager.WantsDebris ? "O" : "·"),6}" +
              $"{(MissionManager.WantsAdSigns ? "O" : "·"),6}" +
              $"{(MissionManager.WantsCargo ? "O" : "·"),7}" +
              $"{(GrandFinal.Available ? "O" : "·"),7}" +
              $"{(GrandFinal.FreeRun ? "O" : "·"),6}");

            if (n < 8 && MissionManager.FinalRace)
                Bad($"수집품 {n}개인데 AI 가 나온다 — 임무 중에는 언제나 혼자여야 한다");
            if (n < 8 && GrandFinal.Available)
                Bad($"수집품 {n}개인데 결승이 열려 있다");
        }

        SetCollected(8);
        GrandFinal.MarkCleared();
        L("");
        L($" 8 + 결승 클리어 → AI {(MissionManager.FinalRace ? 3 : 0)}대 · " +
          $"자유주행 {(GrandFinal.FreeRun ? "O" : "·")} · 결승 {(GrandFinal.Available ? "O" : "·")} · " +
          $"자재 {(MissionManager.WantsDebris ? "O" : "·")}");
        if (!GrandFinal.FreeRun) Bad("결승을 깼는데 자유 주행이 안 열린다");
        if (GrandFinal.Available) Bad("결승을 깼는데 결승이 또 열려 있다");
        GrandFinal.Reset();
    }

    // ==================================================================
    //  임무 여덟 + 결승
    // ==================================================================
    static void Missions()
    {
        H("임무 여덟 + 결승");

        var seen = new HashSet<MissionManager.Goal>();
        for (int i = 0; i < ExhibitCatalogue.All.Length; i++)
        {
            var e = ExhibitCatalogue.All[i];
            var g = MissionManager.GoalForReward(e.id);
            if (!seen.Add(g)) Bad($"{i + 1}판의 임무가 앞판과 겹친다: {g}");

            string brief = StoryScript.Briefing(g);
            var chat = StoryScript.Chatter(g);
            string after = StoryScript.TryFind($"after{i + 1}", out var sc) ? $"{StoryScript.Playable(sc.id).Count}줄" : "없음";

            L($" {i + 1}  {g,-6} {e.name,-10} 브리핑 {(string.IsNullOrEmpty(brief) ? "없음" : brief.Length + "자"),5}" +
              $" · 잡담 {(chat?.Length ?? 0),2}줄 · 후기 {after}");

            if (string.IsNullOrWhiteSpace(brief)) Bad($"{i + 1}판 브리핑이 비었다 ({g})");
            if (chat == null || chat.Length == 0) Bad($"{i + 1}판 잡담이 비었다 ({g})");
            if (after == "없음") Bad($"{i + 1}판 후기(after{i + 1})가 없다");
        }

        var fin = MissionManager.Goal.결승;
        L($" 9  {fin,-6} {"—",-10} 브리핑 " +
          $"{(string.IsNullOrEmpty(StoryScript.Briefing(fin)) ? "없음" : "있음")}");
        if (string.IsNullOrEmpty(StoryScript.Briefing(fin))) Bad("결승 브리핑이 비었다");
    }

    // ==================================================================
    //  씬
    // ==================================================================
    static void SceneFiles()
    {
        H("씬");

        var builders = new Dictionary<string, string[]>
        {
            ["Campus"] = new[] { "Assets/Editor/CampusSceneBuilder.cs", "Assets/Scripts/CampusBuilder.cs",
                                 "Assets/Scripts/HanokDoor.cs", "Assets/Scripts/RoomLife.cs",
                                 "Assets/Scripts/ClueText.cs" },
            ["Lobby"] = new[] { "Assets/Editor/LobbySceneBuilder.cs" },
            ["Gallery"] = new[] { "Assets/Editor/GallerySceneBuilder.cs" },
        };

        foreach (var kv in builders)
        {
            string scene = $"Assets/Scenes/{kv.Key}.unity";
            if (!File.Exists(scene)) { Bad($"{kv.Key}.unity 가 없다"); continue; }

            var baked = File.GetLastWriteTime(scene);
            string newest = null;
            var at = System.DateTime.MinValue;
            foreach (string b in kv.Value)
            {
                if (!File.Exists(b)) continue;
                var t = File.GetLastWriteTime(b);
                if (t > at) { at = t; newest = Path.GetFileName(b); }
            }

            bool stale = at > baked;
            L($"  {kv.Key,-8} 구운 때 {baked:MM-dd HH:mm} · 빌더 {at:MM-dd HH:mm} ({newest})" +
              (stale ? "   * 다시 구워야 한다" : ""));
            if (stale) problems++;
        }

        var reg = EditorBuildSettings.scenes.Where(s => s.enabled)
                  .Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
        L("  빌드 설정: " + string.Join(" · ", reg));
        if (reg.Length == 0) Bad("빌드 설정에 켜진 씬이 없다 — exe 가 까만 화면이 된다");
        else if (reg[0] != "Lobby") Bad($"0번 씬이 Lobby 가 아니다 ({reg[0]}) — 첫 화면이 안 뜬다");
    }

    // ==================================================================
    //  소리
    // ==================================================================
    static void Audio()
    {
        H("소리");

        string[] want =
        {
            "Sfx/Countdown", "Sfx/CountdownGo",
            "Sfx/Blimp_Engine", "Sfx/Blimp_Dive", "Sfx/Heli_Rotor",
        };
        int ok = 0;
        foreach (string w in want)
        {
            if (Resources.Load<AudioClip>(w) != null) ok++;
            else Bad($"소리 없음: Resources/{w}");
        }

        var sfx = Resources.LoadAll<AudioClip>("Sfx");
        // ★ 음악은 <b>Resources/Music</b> 이다. 처음에 "Bgm" 을 봐서 «0곡» 이 나왔다 —
        //   <b>점검이 틀린 거지 게임이 틀린 게 아니었다.</b>
        var music = Resources.LoadAll<AudioClip>("Music");
        L($"효과음 {ok}/{want.Length} 확인 · Sfx 폴더 {sfx.Length}개 · Music 폴더 {music.Length}곡");

        var have = new HashSet<string>(music.Select(m => m.name));

        // 씬 곡 — 없으면 그 씬이 조용하다
        foreach (string sc in new[] { "Lobby", "Track", "Gallery", "Campus", "Title" })
            if (!have.Contains(sc)) Bad($"씬 곡 없음: Music/{sc}");

        // 이야기 장면 — 제 곡이 없으면 씬 곡(로비)으로 되떨어진다. 그건 정상이다.
        var own = StoryScript.All.Select(x => x.id).Where(id => have.Contains(id)).ToArray();
        var fall = StoryScript.All.Select(x => x.id).Where(id => !have.Contains(id)).ToArray();
        L($"  이야기 전용곡 {own.Length}개: {string.Join(" · ", own)}");
        L($"  로비 곡으로 되떨어지는 장면 {fall.Length}개: {string.Join(" · ", fall)}");

        // «_완성» · 결승 — 없어도 되지만 있으면 전후가 갈린다
        foreach (string v in new[] { "Lobby_완성", "Campus_완성", "Gallery_완성", "Track_완성", "Track_결승", "Track_후반" })
            L($"  {v,-12} {(have.Contains(v) ? "있다" : "없다 — 기본 곡으로 되떨어진다")}");

        // 미니게임 셋
        foreach (string g in new[] { "오늘의급식", "안전점검훈련", "따라그리기" })
            if (!have.Contains(g)) Bad($"미니게임 곡 없음: Music/{g}");

        L("  Music: " + string.Join(" · ", music.Select(m => m.name).OrderBy(x => x)));
    }
}
