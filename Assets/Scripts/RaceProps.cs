using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ★★ 2026-10-06 <b>판마다 트랙 위·바깥의 풍경이 바뀐다.</b>
///
/// 강사님들의 «트랙이 똑같다» 는 <b>코스 모양</b> 얘기가 아니라 <b>화면이 안 변한다</b>는 뜻이다.
/// 길을 고치는 건 비싸고 위험하지만(코스를 바꾸면 발판·체크포인트·랩타임·건물 간격이 전부 따라온다),
/// <b>하늘과 갓길은 공짜로 바꿀 수 있다.</b> 임무 내용은 한 줄도 안 건드린다.
///
/// 구조는 이 프로젝트가 이미 네 번 쓴 그 꼴이다 —
/// <b>수집 기록만 보고 스스로 켜고 끈다.</b> 컴포넌트도 씬도 필요 없고,
/// 「새 컴포넌트로 고치면 씬을 다시 구워야만 고쳐진다」 를 또 겪지 않는다.
///
/// 모델은 <b>유저가 만든 FBX 열 종</b>(<c>Assets/Resources/RaceProps/</c>, 6,663쿼드 ·
/// 100% 쿼드 · 전부 사양서대로 들어왔다). 한 판에 한두 종만 켜지니 §7.6 예산 안이다.
/// </summary>
public static class RaceProps
{
    const string Folder = "RaceProps/";

    /// <summary>프롭 전부가 들어가는 통. 판이 바뀌면 통째로 갈아엎는다.</summary>
    static Transform root;

    /// <summary>판이 바뀌면 통째로 지워지는 통. 떨어진 자재가 여기 쌓인다.</summary>
    public static Transform Root => root;
    static int builtFor = -999;

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
        root = null;
        builtFor = -999;

        // 트랙 씬에서만. 코스가 없으면 둘 자리를 못 잡는다
        if (Object.FindFirstObjectByType<TrackBuilder>() == null) return;

        var go = new GameObject("RaceProps");
        go.AddComponent<RacePropHost>();
    }

    /// <summary>지금 판에 맞는 풍경을 세운다. 이미 그 판이면 아무 것도 안 한다.</summary>
    public static void Refresh()
    {
        var track = Object.FindFirstObjectByType<TrackBuilder>();
        if (track == null) return;

        // ★ 자유 주행은 «1판» 이 아니다. 전부 모으면 NextReward() 가 비어서
        //   CurrentGoal 이 Goal.완주 로 떨어지는데, 그 판의 풍경은 «아무 것도 없음» 이다 —
        //   아홉 판을 다 깬 보상이 <b>텅 빈 트랙</b>이면 이긴 티가 안 난다.
        bool free = GrandFinal.FreeRun;
        int goal = free ? 9999 : (int)MissionManager.CurrentGoal;
        if (goal == builtFor && root != null) return;
        builtFor = goal;

        if (root != null) Object.Destroy(root.gameObject);
        RaceShock.Clear();
        SkyDrop.Clear();
        root = new GameObject("RacePropSet").transform;

        if (free)
        {
            // 다 끝난 뒤 — 관중이 가득 차 있고 하늘은 비어 있다.
            // <b>비행선도 헬기도 없다</b>: 감시하던 것들이 사라진 게 이 판의 연출이야.
            Crowd(track, 30);
            return;
        }

        // 처처 2026-10-12 제작자: *"게임 깨기 전에 캠퍼스 나올 때 한정으로 헬기에게 빔을 쏘지 마.
        //   미리 빔이 나오면 레이싱에서 나오기도 전에 스포일러야. 속도도 줄여서 느긋하게 산책."*
        //   <b>맞다.</b> 캠퍼스는 걸어 다니는 씬인데 <c>TrackBuilder</c> 가 길로 남아 있어서
        //   레이스용 풍경이 그대로 따라 들어왔다 — 점검등도 급강하도 자재도.
        //
        //   여기서는 <b>하늘에 한 대만, 느리게</b>. 보이기는 하되 아무 일도 안 한다.
        if (SceneManager.GetActiveScene().name == "Campus")
        {
            // ★ 2026-10-12 제작자: *"캠퍼스에서 BGM 이 안 들리고 헬기 소리만 들린다."*
            //   레이스는 엔진 소리가 사건이지만 <b>캠퍼스는 걸어 다니며 음악을 듣는 곳</b>이다.
            //   0.5 → <b>0.14</b>, 들리기 시작하는 거리도 190 → 90m 로 줄였다.
            Patrol(track, "R02_Safety_Helicopter", 13f, 64f, lateral: -6f, bob: 0.5f, shadow: 6f,
                   noseBack: true, engine: "Heli_Rotor", engineVolume: 0.14f, engineFar: 90f);
            return;
        }

        switch (MissionManager.CurrentGoal)
        {
            // 1판 — <b>아무 것도 없다.</b> 기준점이 있어야 나머지가 «달라졌다» 가 된다
            case MissionManager.Goal.완주: break;

            // 2판 — 관장 얼굴 황금 비행선이 <b>경기장 위를 순회한다.</b> 제 갈 길을 일정하게
            // 도는 것이 «감시» 고, 뒤를 졸졸 따라오는 건 반려동물이다(2026-10-06 유저 지적)
            case MissionManager.Goal.발판전부:
                // ★ 높이 17.5 로 뒀다가 <b>천장을 뚫었다</b> — 플레이 모드에서 재니 꼭대기가
                //   y 26.3 인데 캠퍼스 반자널이 26 이다. 비행선 꼭대기 = 높이 + 8.8 이라
                //   15.5 면 24.3 으로 1.7m 가 남는다. <b>지붕 위에 뭘 올릴 때는 건물이 아니라
                //   캠퍼스 천장을 봐라</b>(지붕 상징물에서 이미 한 번 겪었다).
                // ★★ 2026-10-06 유저: *"비행선 움직임이 너무 느리고 어디 가는지 모르겠다."*
                //   52초는 <b>카트보다 느려서</b> 늘 뒤에 처져 있었다 — 그러면 «날아가는 것» 이
                //   아니라 «떠 있는 것» 으로 보인다. <b>24초</b>면 카트(29초)보다 빨라서
                //   레이스 중 서너 번 <b>앞질러 지나간다</b> — 움직이는 게 눈에 들어온다.
                //   높이도 15.5 → 11.5 로 낮췄다. 멀면 작아 보이고 작으면 안 움직여 보인다.
                //
                // ★★ 2026-10-12 유저: *"2번째 임무에 비행선이 안 나옴."* <b>재 보니 맞다.</b>
                //   비행선 24초 · 카트 29초면 <b>한 바퀴를 따라잡는 데 139초</b>가 걸리는데
                //   레이스가 87초다. 게다가 시작 자리가 무작위라, 반 바퀴 떨어진 데서
                //   시작하면 <b>레이스 내내 한 번도 안 마주친다.</b> 「서너 번 지나간다」고
                //   적어 둔 건 틀린 계산이었다.
                //
                //   <b>반대로 돌린다.</b> 그러면 상대 속도가 1/24 + 1/29 = 0.076 바퀴/초라
                //   87초에 <b>6~7번 정면으로 스쳐 지나간다.</b> 속도를 올리는 것보다
                //   이쪽이 낫다 — 비행선이 시속 130km 로 나는 건 말이 안 되고,
                //   <b>마주 오는 것</b>이 따라오는 것보다 훨씬 위협적이다.
                //
                //   소리도 같이 붙였다(유저 요청). 3D 음원이라 <b>가까워지면 커지고
                //   멀어지면 사라진다</b> — 도플러까지 유니티가 해 준다. 보이기 전에
                //   먼저 들리는 게 이 연출의 핵심이야.
                // ★★★ 2026-10-12 — 제작자가 다섯 번 신고한 것을 그제야 제대로 읽었다:
                //   *"앞으로 나아가고는 있는데 <b>꼬리쪽을 기준으로</b> 뒤로 나는 것처럼 보인다."*
                //   그건 <b>경로가 거꾸로</b>인 게 아니라 <b>기수가 거꾸로</b>인 것이다.
                //   내가 «경로를 뒤집기»(reverse)로 풀려고 해서 두 번 더 틀렸다.
                //
                //   축을 제대로 세우면: 이 FBX 들은 <b>블렌더 +Y 가 유니티 −Z</b> 로 온다.
                //   비행선 코(Light_Lens)가 블렌더 +Y → 유니티 <b>−Z</b>,
                //   헬기 꼬리(Rotor_Tail)가 블렌더 −Y → 유니티 <b>+Z</b>.
                //   <see cref="FlyingProp"/> 는 <c>LookRotation(dir)</c> 로 <b>+Z 를 진행 방향</b>에
                //   맞추니, 둘 다 <b>코가 −Z</b> 라 <c>noseBack: true</c> 가 맞다.
                //   헬기에만 그걸 안 줬던 것이고, 비행선은 2026-10-06 에 이미 그래서 맞았다.
                //
                //   역주행(reverse)은 뺀다 — 제작자 지시: «경로를 역주행시키는 게 아니라
                //   기존 경로를 따라가면서 기수가 진행 방향을 보게». 대신 속도를 늦춰
                //   <b>플레이어가 따라잡아 나란히 가는</b> 구간을 만든다(24 → 34초).
                // ★ 역주행을 뺀 뒤 <b>마주치는 횟수를 다시 셌다.</b> 34초면
                //   1/29 − 1/34 = 0.0051바퀴/초 × 87초 = <b>0.44바퀴</b> — 한 번도 안 마주친다.
                //   <b>58초</b>면 1.5바퀴라 레이스 중 한두 번 <b>천천히 따라잡아 지나간다.</b>
                //   느린 비행선을 추월하는 그림이 «감시당하는 느낌» 에도 더 맞는다.
                // ★★★ 2026-10-15 — <b>«거꾸로 돌린다» 고 적어 놓고 안 돌리고 있었다.</b>
                //
                // 제작자: *"1랩에 안 나오고, 2랩엔 안 보이고, 3랩엔 쫓아오지도 급강하하지도 않는다."*
                // 넷이 전부 <b>한 원인</b>이다. 비행선 58초 · 카트 29초로 <b>같은 방향</b>이니
                // 카트가 한 바퀴 더 돌 때마다 한 번 따라잡는데, 그 «따라잡는» 그림은
                // <b>3인칭 백뷰에서 카메라 뒤</b>다 — 지나칠 때조차 화면에 안 들어온다.
                //
                // 급강하도 같은 이유로 안 걸렸다: <see cref="FlyingProp.TryDive"/> 는 비행선이
                // <b>카트 앞 57도 안</b>에 있어야 시작하는데, 뒤에서 따라잡히는 배는 그 조건에
                // 거의 안 든다. 소리도 늘 같은 거리라 «다가온다» 가 안 느껴졌다.
                //
                // <b>마주 오게 돌린다.</b> 상대속도가 1/29 + 1/58 이라 <b>19초마다 한 번</b>,
                // 한 바퀴에 1.5번 <b>정면에서</b> 들어온다 — 코부터 보이고, 급강하 조건이
                // 저절로 맞고, 엔진 소리가 커졌다 작아지며 지나간다.
                // (헬기도 같은 처방이다. 둘 다 2026-10-12 주석에만 있고 인자가 없었다.)
                Patrol(track, "R01_Director_Blimp", 11.5f, 58f, lateral: 0f, bob: 0.3f, shadow: 11f,
                       noseBack: true, reverse: true, diveFromLap: 2,
                       engine: "Blimp_Engine", engineFar: 120f);
                break;

            // 3판 — 안전점검 헬기가 낮게 돈다 + 담장 위에 곰 관중이 처음 몇 마리
            case MissionManager.Goal.무충돌:
                //
                // ★★ 2026-10-12 유저: *"헬기가 꼬리쪽을 중심으로 앞서 나가서 뒤로 걷는 것처럼
                //   보인다."* <b>기수 방향은 멀쩡했다</b> — 블렌더로 재니 꼬리 로터가 −Y(= 유니티 −Z)
                //   라 코가 +Z 고, 그건 <see cref="FlyingProp"/> 가 바라보게 하는 방향 그대로다.
                //
                //   진짜 원인은 <b>방향이 아니라 속도</b>였다. 19초/바퀴 · 카트 29초면 헬기가
                //   <b>뒤에서 따라와 추월해 멀어진다</b> — 레이스 내내 <b>뒷모습만 보인다.</b>
                //   꼬리만 보이니 «뒤로 걷는 것» 으로 읽힌 거야.
                //   <b>거꾸로 돌린다</b>(비행선과 같은 처방): 마주 오니 코부터 보이고,
                //   상대 속도가 1/19 + 1/29 라 87초에 <b>7~8번</b> 스쳐 간다.
                //
                // ★ 유저: *"지켜보기만 하고 아무것도 안 하니 좀 그렇다. 레이저빔이라도 쏴서
                //   범위 안에 들어가면 느려지든가."* <b>점검등</b>으로 넣었다 — 안전점검 헬기라
                //   서치라이트가 설정에 맞고, 동아줄과 달리 <b>어디가 위험한지 바닥에 보인다.</b>
                //
                //   ⚠ <b>밀거나 비틀지 않는다.</b> 이 판이 «담장 긁지 않기» 라, 조향을 건드리면
                //   내가 플레이어를 벽에 밀어 놓고 실패시키는 꼴이 된다. <b>느려지기만</b> 한다.
                // ★ 24초는 0.62바퀴라 안 마주칠 수도 있다. <b>16초</b>면 3.2번 스쳐 간다 —
                //   헬기는 빨라야 헬기답고, 점검등도 그만큼 자주 온다.
                // ★★★ 2026-10-15 — 비행선과 <b>같은 누락</b>이었다. 주석에는 거꾸로 돌린다고
                //   적혀 있는데 <c>reverse</c> 를 안 넘기고 있었다.
                //   같은 방향 16초 · 카트 29초면 상대속도가 1/16 − 1/29 이라 <b>36초에 한 번</b>,
                //   한 바퀴에 1.2번뿐이고 그마저 <b>뒤에서 추월해 가는</b> 그림이다.
                //   거꾸로 돌리면 1/13 + 1/29 → <b>9초마다</b>, 한 바퀴에 3.2번 정면으로 온다.
                //   제작자가 원한 «한 바퀴에 네 번쯤» 에 제일 가깝다.
                // ★ 2026-10-15 — 13초는 <b>너무 빨라서 읽을 틈이 없었다.</b>
                //   제작자: *"너무 빠르고 범위도 넓은데 담장도 안 긁어야 된다."*
                //   ★ 17초도 빨랐다(제작자: *"어려워"*). <b>24초</b>면 상대속도가
                //   1/24 + 1/29 → <b>12.9초마다, 한 바퀴에 2.2번.</b>
                //   빛이 기회를 깎는 판이니 <b>오는 걸 보고 비킬 수 있어야</b> 한다.
                Patrol(track, "R02_Safety_Helicopter", 9.5f, 32f, lateral: -4.5f, bob: 0.6f, shadow: 6f,
                       noseBack: true, reverse: true,
                       engine: "Heli_Rotor", engineVolume: 0.8f, beam: true, engineFar: 110f);
                Crowd(track, 8);
                break;

            // 4판 — 거대 철거 크레인 둘. 코스 <b>바깥</b>에서 쇠구슬이 느리게 흔들린다
            case MissionManager.Goal.전시품:
                // ★★ 2026-10-12 유저: *"포크레인이 2대인데 4대로. 2대는 그대로 두고 나머지는
                //   전투용이어서 달고 있는 회색 공이 플레이어에게 위협적이게 지나가거나."*
                //
                //   <b>먼 둘은 그대로 풍경</b>이고(26~28m 밖), 새 둘은 <b>갓길 9m</b> 에 세워
                //   쇳덩이가 길을 가로질러 지나간다. 맞으면 <b>매단 곰인형 하나가 떨어진다</b> —
                //   그 판에서만 통하는 위협이라 풍경이 처음으로 운전을 바꾼다.
                //   목표가 여덟 중 여섯이라 <b>두 개는 잃어도 되고</b>, 떨어진 건
                //   다음 바퀴에 주울 수 있다.
                // ★★★ 2026-10-15 제작자: *"4대가 전부 가만히 있고, 다 똑같아 보인다."*
                //   재 보니 넷이 <b>같은 사인파</b>였다 — 빠르기와 거리만 달랐다.
                //   곰 동작에서 배운 그대로다: <b>속도만 바꾸면 «빠름/느림» 두 가지로만
                //   읽힌다. 쓰는 축이 달라야 한 프레임만 봐도 갈린다.</b>
                //
                //   먼 둘은 <b>풍경</b>이라 길에 안 닿게 반경을 따로 준다(안 그러면
                //   26m 짜리 쇳덩이가 길을 가로질러 온다). 가까운 둘만 <b>위협</b>이다.
                //
                //   | 자리 t | 움직임 | 무엇이 다른가 |
                //   | 0.17   | 회전   | 팔 끝에서 한 바퀴 — 멀어도 눈에 띈다 |
                //   | 0.63   | 붕괴   | 들었다가 내려찍는다 — 가로가 아니라 <b>세로</b> |
                //   | 0.40   | 길막   | 나가서 <b>버틴다</b> — 박자를 보고 지나가는 판단 |
                //   | 0.84   | 거칠게 | 끝에서 튕긴다 — 휘두르는 것처럼 |
                //   ★★★ 2026-10-15 — <b>자리를 재서 옮겼다.</b> 제작자: *"다 땅이나 벽에 박혀 있다."*
                //   구운 Campus 씬에서 네 대의 기둥(Body 6.0 × 7.5m)이 무엇과 겹치는지 쟀다:
                //
                //   | 전 | 무엇에 박혀 있었나 | 후 |
                //   |---|---|---|
                //   | −28 | <b>곰누리관</b> 벽·지붕·안의 전시물까지 — 건물 <b>속</b>이었다 | <b>−14.5</b> |
                //   | 26 | 바위(Rock_17) | <b>27.5</b> |
                //   | 13 · −13.5 | 기둥은 안 겹침 — 그대로 둔다 | — |
                //
                //   ★ 붐(팔)은 밑이 2.43m 라 <b>공중</b>이다. 건물 위를 지나가는 건
                //     철거 크레인다운 그림이라 겹침 검사에서 뺐다 — 기둥만 본다.
                WreckingCrane(track, 0.17f, 29.0f, 5.8f, WreckingBall.Style.회전, reach: 6f);
                // ★★ 곰누리관(74, 80) — <b>자리는 그대로, 돌려만 세운다.</b> (2026-10-15)
                //   재 보니 지금 자리가 <b>겹치지 않는 한계선</b>이다:
                //   처마 반지름 12.25 + 본체 반대각선 4.81 = <b>17.06m</b>, 지금 17.0m.
                //   옛 −28 은 중심에서 5.1m — <b>건물 속</b>이라 벽·지붕·전시물을 뚫었다.
                //
                //   못 보이던 진짜 이유는 거리가 아니라 <b>방향</b>이었다. 코스를 보고
                //   서 있어서 붐이 건물 반대쪽을 향했다. <b>건물을 등지게</b> 세우면
                //   붐이 지붕 가장자리 위로 뻗고, 줄이 8.8m 안쪽 — <b>처마 바로 안</b>,
                //   철구가 <b>지붕 높이(y 8.97)</b> 에 대롱대롱 매달린다.
                //   옛 구도를 그대로 얻으면서 <b>본체는 건물 밖 땅 위</b>다.
                WreckingCrane(track, 0.60f, -16.0f, 6.6f, WreckingBall.Style.붕괴,
                              reach: 5f, boomTo: new Vector3(74f, 0f, 80f));

                // ★ 2026-10-12 제작자: *"포크레인에 자동차가 바로 통과된다. 막아줘.
                //   그리고 진로를 왔다 갔다 타이밍 맞게 막게 해줘 — 슈퍼마리오 파이프처럼."*
                //   몸통에 콜라이더를 달고(장식이 아니라 <b>구조물</b>이다) 갓길로 더 물렸다.
                //   쇳덩이는 길을 가로질러 <b>건너갔다 돌아온다</b> — 박자를 보고 지나가면 된다.
                // ★ 곰머리관(−90, 42) 옆 — 넷 중 그 건물에 제일 가깝다(40.8m).
                //   이 한 대만 <b>건물을 보게</b> 세워서, 쇳덩이가 코스가 아니라
                //   <b>간판 쪽으로</b> 넘어가게 한다. 주기도 곰누리관(6.6초)과 다르게 줘서
                //   둘이 같은 박자로 안 움직인다.
                WreckingCrane(track, 0.40f, 13f, 5.2f, WreckingBall.Style.붕괴,
                              boomTo: new Vector3(-90f, 0f, 42f));
                WreckingCrane(track, 0.84f, -13.5f, 3.4f, WreckingBall.Style.거칠게);
                break;

            // 5판 — 서류 폭풍. 관장 도장이 <b>몇 초마다 쿵 찍고</b> 땅울림이 코스를 쓸고 간다.
            //   <see cref="StampSlam"/> · <see cref="RaceShock"/> 참고 — SPACE 로 넘는다.
            case MissionManager.Goal.제한시간:
                // ★★ 2026-10-12 — <b>맞으면 3초 멈춘다</b>(유저 요청). 그러면 개수를 줄여야 한다.
                //   세어 보면: 땅울림이 28m 까지 퍼지고 도장이 갓길 14m 니 코스를 47m 쓸고 간다.
                //   18m/s 로 지나가면 <b>그 구간에 2.6초</b> 있는다. 주기 5.6초면 <b>맞을 확률이
                //   절반</b>이라, 여덟 개 × 세 바퀴 = 24번 중 <b>열두 번</b> 맞는다.
                //   3초씩이면 36초를 길에 버리는 셈이라 어떤 제한시간으로도 못 깬다.
                //
                //   <b>다섯 개 · 주기 7.2초</b>로 줄였다 → 15번 중 <b>다섯 번</b>(36% × 15).
                //   SPACE 로 넘으면 0 번이고, 그래서 뛰는 게 <b>처음으로 이득</b>이 된다.
                //   제한시간도 94 → 108초로 같이 옮겼다(<see cref="MissionManager.timeLimit"/>).
                for (int i = 0; i < 5; i++)
                {
                    var go = Side(track, "R04_Paper_Storm", 0.07f + i * 0.19f,
                                  (i % 2 == 0 ? 14f : -15f), drift: true);
                    if (go == null) continue;

                    var slam = go.AddComponent<StampSlam>();
                    // 주기를 조금씩 다르게 준다. 똑같으면 다섯이 <b>한 박자로</b> 찍혀서
                    // «쿵 쿵 쿵» 이 아니라 «쿵» 한 번이 된다.
                    slam.period = 7.2f + i * 0.47f;
                    slam.phase  = i * 1.63f;
                }
                break;

            // 6판 — 골든베어 리조트 모형. 황금 곰 동상이 회전 받침 위에서 돈다
            case MissionManager.Goal.광고판:
                Side(track, "R05_Resort_Model", 0.30f, 22f);
                Side(track, "R05_Resort_Model", 0.78f, -23f);
                break;

            // 7판 — 중계 차량이 바깥을 따라 달리고 드론 셋이 플레이어를 찍는다
            case MissionManager.Goal.장애물:
                Chase(track, "R06_Broadcast_Van", 19f, 0.0f);
                Fly("R07_Camera_Drone", 6.5f, 13f, 1.4f, shadow: 0f, side: -6f, sway: 4f, follow: 3.4f);
                Fly("R07_Camera_Drone", 7.8f, 16f, 1.1f, shadow: 0f, side: 7f, sway: 4f, follow: 3.1f);
                Fly("R07_Camera_Drone", 5.4f, 19f, 1.7f, shadow: 0f, side: 0f, sway: 6f, follow: 2.8f);
                break;

            // 8판 — ★ <b>사람이 처음 나타난다.</b> 철거 심사단이 서서 보고 있다.
            //   곰은 사람 앞에서 움직일 수 없다 — 그래서 카트만 나간다. 그 규칙이 눈앞에서 증명된다
            case MissionManager.Goal.완벽:
                // ★ 2026-10-16 — <b>여기만 기믹이 없었다.</b> 세워 둔 차와 사람뿐이라
                //   제일 중요한 판이 제일 조용했다. <see cref="PressFlash"/> 로
                //   지나갈 때 사진이 찍힌다 — <b>판정에는 손대지 않는다.</b>
                //   8판은 이미 담장과 시간 둘을 동시에 요구해서, 피할 것을 더하면 벌이 된다.
                PressFlash.Forget();
                Watcher(Side(track, "R08_Inspection_Car", 0.02f, 14f));
                Watcher(Side(track, "R08_Inspection_Car", 0.05f, 16.5f));
                Watcher(Side(track, "R08_Inspection_Car", 0.52f, -15f));
                Watcher(People(track, 0.03f, 12.5f, 3));
                Watcher(People(track, 0.50f, -13f, 3));
                break;

            // 9판 결승 — 담장이 곰인형 관중으로 가득 차고 비행선 둘이 뜬다
            case MissionManager.Goal.결승:
                Crowd(track, 26);
                Patrol(track, "R01_Director_Blimp", 12.5f, 30f, lateral: 7f, bob: 0.25f, shadow: 10f,
                       noseBack: true, engine: "Blimp_Engine", engineVolume: 0.7f);
                Patrol(track, "R02_Safety_Helicopter", 10.5f, 17f, lateral: -12f, bob: 0.7f, shadow: 6f);
                break;
        }
    }

    // ──────────────────────────────────────────────────────────── 만들기
    /// <summary>
    /// ★★★ 이름으로 부품 찾기 — <b>손자까지</b> 본다.
    ///
    /// <c>Transform.Find</c> 는 <b>직계 자식만</b> 본다. FBX 는 루트 빈 통 아래에 메시가
    /// 들어오는 구조라 <c>Find("Ball")</c> 이 <b>조용히 null</b> 이었고, 그래서
    /// 크레인 쇳덩이가 한 번도 안 흔들렸다(2026-10-12 제작자: "포크레인이 안 움직여").
    /// 콘솔에 경고가 둘 떠 있었는데 그게 이거였다.
    ///
    /// 이 프로젝트에서 <c>Find</c> 가 조용히 null 이었던 게 <b>두 번째</b>다
    /// (2026-09-18 문짝 · 이번). <b>FBX 안의 부품은 이걸로 찾아라.</b>
    /// </summary>
    static Transform Deep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name == name) return t;
        return null;
    }

    static GameObject Load(string file)
    {
        var prefab = Resources.Load<GameObject>(Folder + file);
        if (prefab == null) { Debug.LogWarning($"[풍경] {file} 을 못 찾았다 — Resources/RaceProps/"); return null; }

        var go = Object.Instantiate(prefab, root);
        go.name = file;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        Spin(go);
        return go;
    }

    /// <summary>
    /// 돌아가야 하는 부품을 이름으로 찾아 돌린다. <b>애니메이션 클립을 안 쓴다</b> —
    /// 카트 바퀴 · 곰 팔 · 스피커 귀와 같은 방식이고, 사양서가 이름을 약속해 준 덕이다.
    /// </summary>
    static void Spin(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.StartsWith("Rotor_Main")) Add(t, Vector3.up, 900f);
            else if (n.StartsWith("Rotor_Tail")) Add(t, Vector3.right, 1400f);
            else if (n.StartsWith("Rotor_")) Add(t, Vector3.up, 1600f);
            else if (n.StartsWith("Prop_")) Add(t, Vector3.up, 220f);
            else if (n == "Dish") Add(t, Vector3.up, 26f);
            else if (n == "Turntable" || n == "Bear_Statue") Add(t, Vector3.up, 14f);
            else if (n.StartsWith("Wheel_")) Add(t, Vector3.right, 420f);
        }
    }

    static void Add(Transform t, Vector3 axis, float speed)
    {
        var s = t.gameObject.AddComponent<SpinPart>();
        s.axis = axis; s.degreesPerSecond = speed;
    }

    /// <summary>하늘에서 <b>플레이어를 따라오는</b> 것. 바닥에 그림자를 깐다.</summary>
    /// <summary><paramref name="distance"/> 는 카트 <b>앞쪽</b> 거리다(백뷰라 뒤는 안 보인다).</summary>
    static void Fly(string file, float height, float distance, float bob, float shadow,
                    float side = 0f, float sway = 0f, float follow = 2.6f)
    {
        var go = Load(file);
        if (go == null) return;

        var f = go.AddComponent<FlyingProp>();
        f.height = height; f.distance = distance; f.bob = bob; f.sideways = side;
        f.shadowRadius = shadow; f.sway = sway; f.follow = follow;
    }

    /// <summary>
    /// 코스 위를 <b>일정하게 도는</b> 비행체 — 감시 비행. 플레이어를 안 쫓는다.
    /// <paramref name="lapSeconds"/> 가 카트 한 바퀴(약 29초)보다 길면 주기적으로 추월당하고,
    /// 짧으면 뒤에서 따라잡힌다. <b>어느 쪽이든 가끔 마주치는 게 요점</b>이다.
    /// </summary>
    static void Patrol(TrackBuilder track, string file, float height, float lapSeconds,
                       float lateral, float bob, float shadow, bool noseBack = false,
                       bool reverse = false, int diveFromLap = 0,
                       string engine = null, float engineVolume = 0.9f, bool beam = false,
                       float engineFar = 190f)
    {
        var go = Load(file);
        if (go == null) return;

        var f = go.AddComponent<FlyingProp>();
        f.patrol = track; f.height = height; f.lapSeconds = lapSeconds;
        f.lateral = lateral; f.bob = bob; f.shadowRadius = shadow; f.noseBack = noseBack;
        f.reverse = reverse; f.diveFromLap = diveFromLap;

        if (beam)
        {
            var b = go.AddComponent<SearchBeam>();
            b.flyer = f;
        }

        if (string.IsNullOrEmpty(engine)) return;
        var e = go.AddComponent<PropEngine>();
        e.clipName = engine; e.volume = engineVolume; e.far = engineFar;
        f.engine = e;
    }

    /// <summary>
    /// ★ <b>전투용 크레인</b> — 쇳덩이가 길을 가로질러 지나간다.
    /// <paramref name="period"/> 를 둘이 다르게 줘야 «한 박자로» 지나가지 않는다.
    /// </summary>
    static void WreckingCrane(TrackBuilder track, float t, float offset, float period,
                              WreckingBall.Style style = WreckingBall.Style.거칠게,
                              float reach = 0f, Vector3? faceWorld = null,
                              Vector3? boomTo = null)
    {
        var go = Side(track, "R03_Demolition_Crane", t, offset);
        if (go == null) return;

        // ★ 2026-10-15 — <b>무엇을 보고 서 있을지 고를 수 있다.</b>
        //   제작자: *"곰머리관 옆 포크레인만 건물 간판을 부수는 느낌으로."*
        //   <see cref="Side"/> 는 기본으로 <b>코스</b>를 보게 세운다. 쇳덩이는
        //   <c>transform.right</c> 를 축으로 흔들리니, <b>보는 쪽으로 넘어간다.</b>
        //   건물을 보게 돌리면 그 한 대만 «건물을 때리는» 그림이 된다.
        // ★★ <paramref name="boomTo"/> — <b>붐을 저쪽으로 뻗는다.</b> (2026-10-15)
        //   쉬는 자리에서 공은 크레인의 <b>−Z</b> 쪽에 매달려 있다. 즉 <b>붐은 등 쪽</b>이다.
        //   그래서 붐을 건물 위로 뻗으려면 <b>건물을 등지게</b> 세워야 한다.
        //   그러면 흔들림(<c>transform.right</c> 축, <b>양수</b>)이 곧 <b>건물 쪽</b>이 된다 —
        //   네 대가 같은 부호 규칙을 쓰게 되어 동작을 섞어 써도 안 틀린다.
        Vector3? look = boomTo.HasValue
            ? go.transform.position * 2f - boomTo.Value      // 반대편을 본다
            : faceWorld;
        if (look.HasValue)
        {
            Vector3 toward = look.Value - go.transform.position;
            toward.y = 0f;
            if (toward.sqrMagnitude > 0.01f)
                go.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up);
        }

        var arm = Deep(go.transform, "Boom");

        // ★★★ 2026-10-15 — <b>움직이는 것은 Cable 이다.</b>
        //   계층이 Root > Boom > Cable > (Ball · Ball_Paw) 라, 공만 옮기면
        //   줄과 발바닥이 제자리에 남아 <b>끊어져 보인다.</b> 줄을 옮기면 셋이 같이 간다.
        var ball = Deep(go.transform, "Cable") ?? Deep(go.transform, "Ball");
        if (arm == null || ball == null)
        {
            Debug.LogWarning("[풍경] 크레인 부품(Boom/Ball)을 못 찾았다 — R03 의 이름을 봐라.");
            return;
        }

        var w = go.AddComponent<WreckingBall>();
        w.arm = arm; w.ball = ball; w.period = period; w.style = style;
        w.hitPoint = Deep(go.transform, "Ball") ?? ball;   // 맞음 판정은 쇳덩이 자리에서

        // ★ 기본은 «길 가운데를 조금 넘어간다» 지만, <b>멀리 선 풍경 크레인</b>은
        //   그러면 쇳덩이가 26m 를 가로질러 길까지 온다. 그런 자리는 반경을 직접 준다.
        w.reach = reach > 0f ? reach : Mathf.Abs(offset) + 3.0f;

        // ★ 몸통은 <b>막는다.</b> 통과되면 그건 그림이지 장비가 아니다.
        //   CLAUDE.md 규칙대로 <b>띠를 둘러</b> «부딪히는 것» 이라고 알린다.
        Block(go, "Body", 0.9f);
    }

    /// <summary>
    /// 이름이 맞는 부품에 <b>콜라이더와 경고 띠</b>를 단다.
    /// 띠가 있으면 부딪히는 것, 없으면 장식 — 이 프로젝트의 규칙이다(2026-09-16).
    /// </summary>
    static void Block(GameObject go, string partName, float shrink)
    {
        Transform part = null;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(partName)) { part = t; break; }
        if (part == null) return;

        var ren = part.GetComponent<Renderer>();
        if (ren == null) return;

        var box = part.gameObject.AddComponent<BoxCollider>();
        var local = part.InverseTransformVector(ren.bounds.size);
        box.size = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)) * shrink;

        // 경고 띠 — 몸통 바닥 둘레에 빨강·크림. 콜라이더는 안 단다(두 번 세지면 안 된다).
        var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(stripe.GetComponent<Collider>());
        stripe.name = "WarnStripe";
        stripe.transform.SetParent(part, false);
        stripe.transform.localPosition = new Vector3(0f, -Mathf.Abs(local.y) * 0.42f, 0f);
        stripe.transform.localScale = new Vector3(Mathf.Abs(local.x) * 1.02f, 0.26f,
                                                  Mathf.Abs(local.z) * 1.02f);
        stripe.GetComponent<Renderer>().sharedMaterial =
            FlatMaterial.Get(new Color32(0xC4, 0x45, 0x3E, 0xFF));
    }

    /// <summary>코스 <b>바깥</b>에 고정. 자리를 손으로 안 찍는다 — 코스 t 와 옆거리로 적는다.</summary>
    static GameObject Side(TrackBuilder track, string file, float t, float offset,
                           bool swing = false, bool drift = false)
    {
        var go = Load(file);
        if (go == null) return null;

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 at = track.transform.position + track.PointOnPath(t)
                   + Vector3.Cross(Vector3.up, fwd) * offset;

        go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-Vector3.Cross(Vector3.up, fwd)));

        // ★★★ 2026-10-15 제작자: *"포크레인이 다 땅이나 벽에 박혀 있다."*
        //
        //   <b>세우는 높이를 코스 중앙선에서 가져오고 있었다.</b> 26m 옆은 길이 아니라
        //   캠퍼스 땅이고, 거기는 높이가 다르다 — 땅이 높으면 <b>묻히고</b> 낮으면 뜬다.
        //   길 옆에 세우는 <b>모든 소품</b>이 같은 함정에 있었다.
        //
        //   <see cref="Ground"/> 로 <b>그 자리의 실제 바닥</b>을 재서 앉힌다.
        //   제 콜라이더는 빼고 재니 «제 윗면을 바닥으로 읽는» 사고도 안 난다.
        go.transform.position = Ground.Sit(go.transform, at);

        if (swing)
        {
            var ball = Deep(go.transform, "Ball") ?? Deep(go.transform, "Boom");
            if (ball != null)
            {
                var sw = ball.gameObject.AddComponent<SwingPart>();
                // ★ 11도는 <b>멈춰 있는 것처럼</b> 보였다(2026-10-12 제작자).
                //   36도면 멀리서도 «움직인다» 가 보인다.
                sw.degrees = 36f; sw.period = 3.6f;
            }
        }
        if (drift) go.AddComponent<PaperDrift>();
        return go;
    }

    /// <summary>코스 바깥을 <b>같이 달리는</b> 차.</summary>
    static void Chase(TrackBuilder track, string file, float offset, float lead)
    {
        var go = Load(file);
        if (go == null) return;
        var c = go.AddComponent<ChaseVehicle>();
        c.track = track; c.offset = offset; c.lead = lead;
    }

    /// <summary>담장 위 곰인형 관중. 판이 올라갈수록 늘어난다.</summary>
    static void Crowd(TrackBuilder track, int count)
    {
        var prefab = Resources.Load<GameObject>(Folder + "R09_Spectator_Bear");
        if (prefab == null) return;

        string[] pose = { "Bear_Sit", "Bear_Watch", "Bear_Wave" };

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            float side = (i % 3 == 0) ? -1f : 1f;

            Vector3 fwd = track.TangentOnPath(t);
            Vector3 at = track.transform.position + track.PointOnPath(t)
                       + Vector3.Cross(Vector3.up, fwd) * (side * (track.WidthOnPath(t) * 0.5f + 1.1f))
                       + Vector3.up * 4.25f;                 // 담장 꼭대기

            var go = Object.Instantiate(prefab, root);
            go.name = $"Bear_{i}";
            go.transform.SetPositionAndRotation(
                at, Quaternion.LookRotation(-Vector3.Cross(Vector3.up, fwd) * side));
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

            // 포즈 셋 중 하나만 켠다
            string keep = pose[i % pose.Length];
            foreach (var p in pose)
            {
                var child = go.transform.Find(p);
                if (child != null) child.gameObject.SetActive(p == keep);
            }
        }
    }

    /// <summary>
    /// 지나갈 때 사진이 찍히게 한다. <see cref="PressFlash"/> 를 붙이는 한 줄 —
    /// 붙일 게 없으면(프리팹을 못 찾았으면) 아무 일도 안 한다.
    /// </summary>
    static void Watcher(GameObject go)
    {
        if (go != null && go.GetComponent<PressFlash>() == null) go.AddComponent<PressFlash>();
    }

    /// <summary>철거 심사단. 서서 보고 있기만 한다.</summary>
    static GameObject People(TrackBuilder track, float t, float offset, int count)
    {
        var prefab = Resources.Load<GameObject>(Folder + "R10_Inspector_Figure");
        if (prefab == null) return null;

        string[] pose = { "Person_Stand", "Person_Clipboard", "Person_Point" };
        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * offset;

        GameObject last = null;
        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(prefab, root);
            last = go;
            go.name = $"Inspector_{i}";
            // 코스를 보게 세운다 — 보고 있다는 게 이 판의 전부야
            go.transform.SetPositionAndRotation(
                at + fwd * (i * 1.6f - count * 0.5f) + side * (i % 2 == 0 ? 0f : 1.3f),
                Quaternion.LookRotation(-side * Mathf.Sign(offset)));
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);

            string keep = pose[i % pose.Length];
            foreach (var p in pose)
            {
                var child = go.transform.Find(p);
                if (child != null) child.gameObject.SetActive(p == keep);
            }
        }

        // 무리의 자리로 쓴다 — 셋이 1.6m 간격이라 어느 하나를 잡아도 같은 자리다
        return last;
    }
}

/// <summary>프롭을 세우고, 판이 바뀌면 다시 세운다.</summary>
public class RacePropHost : MonoBehaviour
{
    void Start() => RaceProps.Refresh();

    // 판은 ENTER 재시작으로도 바뀐다. 매 프레임 세는 게 아니라 <b>달라졌을 때만</b> 다시 짓는다
    void Update()
    {
        RaceProps.Refresh();
        RaceShock.Tick();
        if (track == null) track = Object.FindFirstObjectByType<TrackBuilder>();
        if (track != null) SkyDrop.Tick(track);
    }

    TrackBuilder track;
}

/// <summary>부품 하나를 제자리에서 돌린다. 클립도 리그도 없다.</summary>
public class SpinPart : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 360f;

    void Update() { if (!RacePause.On) transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
}

/// <summary>크레인 쇠구슬처럼 <b>천천히 흔들린다.</b></summary>
public class SwingPart : MonoBehaviour
{
    public float degrees = 10f;
    public float period = 4f;

    Quaternion home;
    void Awake() => home = transform.localRotation;

    void Update()
    {
        if (RacePause.On) return;
        float a = Mathf.Sin(Time.time * Mathf.PI * 2f / period) * degrees;
        transform.localRotation = Quaternion.Euler(a, 0f, 0f) * home;
    }
}

/// <summary>서류가 바람에 천천히 뒤척인다.</summary>
public class PaperDrift : MonoBehaviour
{
    readonly List<Transform> sheets = new();
    readonly List<Vector3> home = new();

    float gustAt = -99f;

    /// <summary>
    /// 도장이 찍힐 때 서류가 <b>확 솟았다 제자리로</b> 돌아온다.
    /// 유저 요청 중 «종이를 날린 뒤 원상복귀» 가 이것 — <c>home</c> 을 들고 있어서 공짜다.
    /// </summary>
    public void Gust() => gustAt = Time.time;

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("Paper_")) { sheets.Add(t); home.Add(t.localPosition); }
    }

    void Update()
    {
        if (RacePause.On) return;

        // 0.15초 만에 솟고 1.7초에 걸쳐 가라앉는다. 올라가는 게 빨라야 «맞아서 튄 것» 으로 보인다
        float age = Time.time - gustAt;
        float blow = age < 0.15f ? age / 0.15f
                   : age < 1.85f ? 1f - (age - 0.15f) / 1.7f
                   : 0f;
        blow *= blow;

        for (int i = 0; i < sheets.Count; i++)
        {
            float p = Time.time * 0.6f + i * 1.7f;
            float a = i * 2.3f;
            // ★ 유저: «날아오는 게 잘 안 보인다» → 멀리·높이 던지고 <b>크기도 키운다</b>.
            //   A4 한 장은 20m 밖에서 안 보인다 — 서류 뭉치로 읽히려면 커야 한다.
            Vector3 gust = blow * new Vector3(Mathf.Cos(a) * 6.5f,
                                              4.2f + (i % 3) * 1.6f,
                                              Mathf.Sin(a) * 6.5f);
            sheets[i].localScale = Vector3.one * (1f + blow * 1.6f);

            sheets[i].localPosition = home[i] + gust + new Vector3(Mathf.Sin(p) * 0.5f,
                                                                   Mathf.Sin(p * 1.3f) * 0.35f,
                                                                   Mathf.Cos(p * 0.8f) * 0.4f);
            sheets[i].Rotate(Vector3.up, (24f + blow * 260f) * Time.deltaTime, Space.Self);
        }
    }
}

/// <summary>
/// ★ <b>플레이어를 따라오는 비행체 + 바닥 그림자.</b>
///
/// 고정 궤도로 돌면 배경이고 <b>따라오면 감시</b>다 — 그게 공짜로 찝찝해진다.
/// 그림자는 <b>바닥에 까는 판</b>이지 실시간 그림자가 아니다(§7.6: 실시간 그림자는 태양 하나).
/// 낮아질수록 그림자가 <b>작고 진해진다</b> = 가까워진다.
/// </summary>
public class FlyingProp : MonoBehaviour
{
    public float height = 17f;
    public float distance = 26f;
    public float bob = 0.2f;
    public float sideways = 0f;
    public float shadowRadius = 8f;

    /// <summary>좌우로 천천히 건너간다 — <b>보일 듯 말 듯</b> 하라는 유저 요청이 이것이다.</summary>
    public float sway = 0f;
    public float swayRate = 0.17f;

    // ──────────────────────────────── 순회 비행(감시)
    /// <summary>
    /// 꽂혀 있으면 <b>플레이어를 안 따라가고 코스 위를 돈다.</b>
    ///
    /// ★★ 2026-10-06 유저: *"왜 비행기가 저따구로 날아. 일직선으로 날아서 경기장 안을
    /// 빙글빙글 돌아야지 감시하듯이. 왜 꼬리 뒤로 나냐고."* 둘 다 맞는 지적이다.
    ///
    /// ① <b>추종은 «감시» 로 안 읽힌다.</b> 내 뒤를 졸졸 따라오면 그건 반려동물이고,
    ///    <b>제 갈 길을 일정하게 도는 것</b>이 감시다. 코스를 따라 날면 긴 직선에서는
    ///    곧게 날고 코너에서만 선회해서 «정찰 비행» 모양이 저절로 나온다.
    /// ② <b>꼬리가 앞으로 가고 있었다.</b> 기수는 로컬 <c>+Z</c> 인데(블렌더에서 재니
    ///    프로펠러가 −Y = 유니티 −Z 쪽이다) 옛 코드가 <b>플레이어를 바라보게</b> 돌렸다.
    ///    앞쪽에 띄워 놓고 뒤를 보게 했으니 <b>거꾸로 날 수밖에 없었다.</b>
    ///    이제 <b>진행 방향(접선)</b>을 본다.
    /// </summary>
    public TrackBuilder patrol;
    public float lapSeconds = 40f;
    public float lateral = 0f;

    /// <summary>
    /// ★★ 2026-10-06 유저: *"비행선이 앞으로 가긴 하는데 꼬리 쪽을 기준으로 이동해서
    /// 뒤로 가는 느낌이다. 뭉툭한 부분이 앞에 있어야 하는데."* <b>맞다 — 비행선만 거꾸로였다.</b>
    ///
    /// 전에는 프로펠러 자리로 기수를 짐작했는데 그게 틀렸다. <b>몸통 반폭을 길이 방향으로
    /// 재니</b> 결정적이다 — 비행선은 −Y 끝이 <b>반폭 3.74(뭉툭)</b>, +Y 끝이 <b>2.06(뾰족)</b> 이라
    /// <b>기수가 −Z</b> 다. 헬기는 반대로 −Y 가 0.13(테일붐), +Y 가 1.52(기수)라 <b>이쪽은 맞았다.</b>
    ///
    /// > <b>«어느 쪽이 앞인가» 는 부품 자리가 아니라 «모양» 으로 재라.</b>
    /// > 프로펠러·조명은 앞뒤 어디에나 붙지만, <b>뭉툭한 쪽이 앞</b>인 건 날아가는 물건의 성질이다.
    /// </summary>
    public bool noseBack;

    float lap;

    /// <summary>따라붙는 빠르기. 시간상수가 <c>1/follow</c> 초다 — 작으면 뒤처져 딴 데로 간다.</summary>
    public float follow = 2.6f;

    Transform target, shade;
    Renderer shadeRenderer;
    float seed;

    /// <summary>코스를 <b>거꾸로</b> 돈다 — 마주 오면 자주 마주치고 더 위협적이다.</summary>
    public bool reverse;

    /// <summary>지금 코스 어디쯤(0~1). <see cref="SearchBeam"/> 이 이걸 보고 앞을 비춘다.</summary>
    public float Lap => lap;

    /// <summary>이 바퀴부터 <b>가끔 급강하</b>한다. 0 이면 안 내려온다.</summary>
    public int diveFromLap;

    /// <summary>엔진 소리. 급강하할 때 한 방 더 울린다.</summary>
    public PropEngine engine;

    /// <summary>급강하 한 번의 길이. <c>Blimp_Dive.wav</c> 와 같은 2.6초다.</summary>
    const float DiveLen = 2.6f;

    /// <summary>제일 낮게 내려왔을 때의 높이. 선체 밑이 여기 오니 카트 위로 지나간다.</summary>
    const float DiveFloor = 6f;

    float diveAt = -99f, nextDive;
    bool jolted;
    RaceProgress mine;
    KartCamera shakeCam;

    /// <summary>
    /// ★★★ <b>코 방향은 손으로 적는다</b>(2026-10-12, 되돌림).
    ///
    /// 부품 이름으로 재서 자동으로 정하게 해 봤다가 <b>전부 뒤로 날았다.</b>
    /// 제작자: *"왜 모든 비행선과 헬리콥터가 다 뒤로 이동하지."* 내가 만든 회귀다.
    ///
    /// 재는 게 틀린 게 아니라 <b>부호를 어디에 맞출지가 추론이었던 게</b> 틀렸다.
    /// 2026-10-06 플레이테스트로 확인된 값은 «비행선은 안 뒤집는다» 하나뿐이고,
    /// 지금은 거기에 맞춰 <b>둘 다 안 뒤집는다.</b>
    ///
    /// ⚠ <b>자동으로 정하지 마라.</b> 모델이 두 개뿐이라 손으로 적는 게 싸고 안 틀린다.
    /// 거꾸로 보이면 <see cref="noseBack"/> 을 뒤집고 <b>달려서 확인</b>해라 —
    /// 마주 오는 쪽에서만 드러난다.
    /// </summary>
    void Start()
    {
        seed = Random.value * 10f;

        // 순회 비행은 <b>코스 어딘가에서 시작</b>한다. 0 에서 시작하면 출발선 바로 위에
        // 떠 있다가 플레이어와 같은 속도로 붙어 가서 «따라온다» 로 보인다.
        if (patrol != null) { lap = Random.value; Patrol(seed); }

        foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
            if (k.GetComponent<PlayerKart>() != null)
            {
                target = k.transform;
                mine = k.GetComponent<RaceProgress>();
                break;
            }
        if (target == null && Camera.main != null) target = Camera.main.transform;
        if (Camera.main != null) shakeCam = Camera.main.GetComponent<KartCamera>();

        if (shadowRadius <= 0f) return;

        // 그림자 — 납작한 원기둥. Quad 는 한쪽 면만 있어서 «어느 쪽이 앞인가» 가
        // 또 하나의 짐작거리가 된다(이 프로젝트에서 방향으로 두 번 틀렸다)
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(disc.GetComponent<Collider>());
        disc.name = "Shadow";
        shade = disc.transform;
        shadeRenderer = disc.GetComponent<Renderer>();

        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);                               // Transparent
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.08f, 0.34f));
        shadeRenderer.sharedMaterial = m;
    }

    void LateUpdate()
    {
        if (RacePause.On) return;
        if (target == null && patrol == null) return;

        float t = Time.time + seed;

        if (patrol != null) { TryDive(); Patrol(t); Shadow(); return; }

        // ★★ <b>앞쪽 비스듬히</b> 떠 있는다. 카메라가 <b>3인칭 백뷰</b>라 카트 뒤에 두면
        // 그건 곧 «카메라 뒤» 라서 <b>한 번도 화면에 안 들어온다</b> — 유저가 «비행선이 안 쫓아온다»
        // 고 한 게 이거였다. 앞에 두면 그림자가 길을 쓸고 지나가는 것까지 같이 보인다.
        float lane = sideways + Mathf.Sin(t * swayRate) * sway;
        Vector3 want = target.position
                     + target.forward * distance
                     + target.right * lane
                     + Vector3.up * (height + Mathf.Sin(t * 0.8f) * bob * 3f);

        // 시간상수 1/follow 초. 전에는 0.9초라 22m/s 짜리 카트에 20m 씩 뒤처져 딴 데로 샜다
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-follow * Time.deltaTime));

        Vector3 look = target.position + Vector3.up * 2f - transform.position;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(look.normalized, Vector3.up),
                                                  1f - Mathf.Exp(-1.6f * Time.deltaTime));

        Shadow();
    }

    /// <summary>코스를 따라 일정한 속도로 돈다. 기수는 <b>진행 방향</b>.</summary>
    void Patrol(float t)
    {
        lap += (reverse ? -Time.deltaTime : Time.deltaTime) / Mathf.Max(4f, lapSeconds);
        lap -= Mathf.Floor(lap);

        Vector3 fwd = patrol.TangentOnPath(lap);

        // ★ <b>가는 방향</b>과 코스의 방향은 다를 수 있다. 기수는 가는 쪽을 봐야 한다 —
        //   거꾸로 돌면서 코스 방향을 보면 그대로 «뒤로 걷는 비행선» 이 된다.
        Vector3 dir = reverse ? -fwd : fwd;

        Vector3 at = patrol.transform.position + patrol.PointOnPath(lap)
                   + Vector3.Cross(Vector3.up, fwd) * lateral
                   + Vector3.up * (height + Mathf.Sin(t * 0.5f) * bob * 3f - DiveDrop());

        transform.position = at;

        // 선회할 때 <b>안쪽으로 기운다.</b> 수평으로 미끄러지면 종이비행기로 보인다 —
        // 각속도에서 기울기를 뽑으니 코스 모양을 바꿔도 저절로 맞는다.
        float turn = Vector3.SignedAngle(lastFwd == Vector3.zero ? dir : lastFwd, dir, Vector3.up);
        lastFwd = dir;
        bank = Mathf.Lerp(bank, Mathf.Clamp(turn / Mathf.Max(0.0001f, Time.deltaTime) * 0.45f, -22f, 22f),
                          1f - Mathf.Exp(-2.5f * Time.deltaTime));

        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(dir, Vector3.up)
                                                  * Quaternion.Euler(Mathf.Clamp(DiveTilt(), -14f, 14f),
                                                                     noseBack ? 180f : 0f, -bank),
                                              1f - Mathf.Exp(-4f * Time.deltaTime));
    }

    /// <summary>
    /// ★ <b>가끔 확 내려온다</b>(2026-10-12 유저 요청). 1바퀴째는 얌전히 돌고,
    /// 2바퀴째부터 «위협하듯이» 내려오며, 3바퀴째는 더 자주 온다.
    ///
    /// <b>앞쪽에서만 시작한다</b> — 3인칭 백뷰라 뒤에서 내려오면 화면에 한 번도 안 들어온다.
    /// 그건 비행선이 꼬리부터 날던 것과 같은 실수다.
    /// </summary>
    void TryDive()
    {
        if (diveFromLap <= 0 || target == null) return;
        if (Time.time - diveAt < DiveLen + 0.4f) return;
        if (Time.time < nextDive) return;

        int lap = mine != null ? mine.Lap : 1;
        if (lap < diveFromLap) return;

        Vector3 to = transform.position - target.position;
        to.y = 0f;
        float far = to.magnitude;
        if (far < 42f || far > 125f) return;
        if (Vector3.Dot(to.normalized, target.forward) < 0.55f) return;

        diveAt = Time.time;
        jolted = false;

        // 바퀴가 늘수록 자주. 3바퀴째가 제일 사납다.
        float gap = lap > diveFromLap ? Random.Range(4.5f, 7.5f) : Random.Range(8f, 13f);
        nextDive = Time.time + DiveLen + gap;

        if (engine != null) engine.Boom("Blimp_Dive");
    }

    /// <summary>급강하한 만큼. 가운데에서 제일 낮고, 제곱이라 바닥에 조금 머문다.</summary>
    float DiveDrop()
    {
        float p = (Time.time - diveAt) / DiveLen;
        if (p <= 0f || p >= 1f) return 0f;

        float s = Mathf.Sin(p * Mathf.PI);
        float drop = (height - DiveFloor) * s * s;

        // 제일 낮은 데를 지날 때 화면이 한 번 들린다 — 가까이 있을 때만.
        if (!jolted && p > 0.46f && shakeCam != null && target != null &&
            (transform.position - target.position).sqrMagnitude < 40f * 40f)
        {
            jolted = true;
            shakeCam.Jolt(0.22f);
        }
        if (engine != null) engine.Strain(s);
        return drop;
    }

    /// <summary>내려올 때 기수를 숙이고 올라갈 때 든다. 수평으로 내려오면 엘리베이터다.</summary>
    float DiveTilt()
    {
        float p = (Time.time - diveAt) / DiveLen;
        if (p <= 0f || p >= 1f) return 0f;
        return Mathf.Sin(p * 2f * Mathf.PI) * 13f;
    }

    Vector3 lastFwd;
    float bank;

    void Shadow()
    {
        if (shade == null) return;

        // 바닥을 찾아 그 위 5cm 에 깐다. 높을수록 크고 옅다
        Vector3 ground = transform.position;
        float up = transform.position.y;
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, 60f, ~(1 << 2)))
        { ground = hit.point; up = transform.position.y - hit.point.y; }
        else ground.y = 0f;

        float k = Mathf.Clamp01(up / 24f);
        float r = shadowRadius * (0.55f + k * 0.9f);
        shade.position = ground + Vector3.up * 0.05f;
        shade.localScale = new Vector3(r, 0.01f, r);
        shade.rotation = Quaternion.identity;

        if (shadeRenderer != null)
        {
            var c = shadeRenderer.sharedMaterial.GetColor("_BaseColor");
            c.a = Mathf.Lerp(0.42f, 0.14f, k);
            shadeRenderer.sharedMaterial.SetColor("_BaseColor", c);
        }
    }
}

/// <summary>
/// ★★ <b>철거 크레인의 쇳덩이</b>(2026-10-12). 길을 가로질러 왔다 갔다 하고,
/// 맞으면 <b>매단 곰인형 하나가 떨어진다.</b>
///
/// <b>카트를 날려 보내지 않는다.</b> 코스 밖으로 날아가면 맞은 쪽이 레이스를 포기하게 되고
/// 그건 재미가 아니라 벌이다(2026-09-17 카트 충돌에서 정한 선). 휘청하고 느려질 뿐이다.
///
/// 피할 수 있어야 기믹이다 — <b>일정한 박자</b>로 돈다. 둘의 주기를 다르게 줘서
/// 한 박자로 지나가지 않게 하고, 그래서 외우는 게 아니라 보고 피하게 된다.
/// </summary>
public class WreckingBall : MonoBehaviour
{
    /// <summary>
    /// ★★ <b>네 대가 각자 다른 움직임을 한다.</b> (2026-10-15 제작자 요청)
    ///
    /// 전에는 넷이 전부 <b>같은 사인파</b>였다 — 빠르기와 거리만 달라서, 곰 동작에서
    /// 겪은 것과 똑같이 «빠름 / 느림» 두 가지로만 읽혔다.
    /// <b>쓰는 축이 달라야</b> 한 프레임만 봐도 갈린다: 가로(거칠게·길막) · 세로(붕괴) · 원(회전).
    /// </summary>
    public enum Style { 거칠게, 길막, 붕괴, 회전 }

    public Style style = Style.거칠게;

    public Transform arm, ball;
    public float period = 4.2f;

    /// <summary>쇳덩이가 기둥에서 얼마나 멀리까지 나가나.</summary>
    public float reach = 12f;

    /// <summary>맞았다고 치는 거리.</summary>
    public float hitRadius = 2.6f;

    /// <summary>맞았는지 재는 자리 — 줄이 아니라 <b>쇳덩이</b>다.</summary>
    public Transform hitPoint;

    Vector3 armHome, ballHome;
    KartCamera cam;
    float lastHit = -9f;

    /// <summary>
    /// ★★★ 2026-10-15 — <b>쇳덩이를 땅속으로 보내고 있었다.</b>
    ///
    /// 제작자: *"포크레인이 움직이지도 않고 땅에 박혀 있다."*
    ///
    /// 계층이 <c>Root &gt; Boom &gt; Cable &gt; Ball</c> 인데, 여기서 <c>localPosition</c> 을
    /// 적어 두고 <c>루트.TransformPoint(그것)</c> 으로 되돌리고 있었다.
    /// <b>그 둘은 같은 공간이 아니다</b> — 적은 값은 <c>Cable</c> 기준
    /// <c>(0, −8.57, 0)</c> 인데 루트 기준으로 읽으니 <b>땅속 8.5m</b> 가 된다.
    /// 박혀 있으니 아무리 흔들어도 화면에는 아무 일도 안 일어난다.
    ///
    /// 부모가 몇 겹이든 안 틀리는 방법은 <b>월드 좌표를 그대로 적어 두는 것</b>이다.
    /// 크레인은 제자리에 서 있으니 기준점이 움직일 일도 없다.
    /// </summary>
    /// <summary>
    /// ★★★★ 2026-10-15 (네 번째) — <b>붐은 아예 안 건드린다. 줄과 공만 휘두른다.</b>
    ///
    /// 제작자: *"왜 몸통이랑 팔 부분이 떨어져 있니. 부착한 상태로 공과 줄만 휘두르면 안 될까."*
    /// <b>그게 정답이고, 내가 먼 길을 돌았다.</b>
    ///
    /// ── 왜 붐을 돌리면 떨어졌나 ────────────────────────────────────────
    /// 회전축을 «붐 기하의 제일 낮은 점» 으로 잡았는데, 이 붐은 <b>비스듬히 누운 팔</b>이라
    /// 제일 낮은 점이 기둥 쪽이 아니라 <b>팔 끝</b>이다. 팔 끝을 축으로 돌렸으니
    /// <b>몸통 쪽이 통째로 날아갔다.</b> 축을 어디로 잡든 같은 종류의 사고가 또 난다 —
    /// <b>모델이 알려주지 않는 값을 내가 짐작하고 있었기 때문</b>이다.
    ///
    /// ── 모델이 이미 알려주는 값 ───────────────────────────────────────
    /// <c>Ball.localPosition = (0, −8.57, 0)</c> — 공은 <c>Cable</c> 원점에서
    /// <b>8.57m 아래</b>에 매달려 있다. 즉 <b>Cable 의 원점이 곧 붐 끝</b>이다.
    /// 짐작할 게 하나도 없다.
    ///
    /// 그래서 <b>Cable 을 제 원점에서 회전시키기만</b> 한다:
    /// <list type="bullet">
    /// <item>붐은 <b>한 번도 안 건드리니</b> 몸통에서 떨어질 수가 없다</item>
    /// <item>줄 윗끝은 <b>늘 붐 끝</b>이다 — 위치를 아예 안 바꾸니까</item>
    /// <item>공은 줄의 자식이라 <b>알아서 따라온다</b></item>
    /// </list>
    /// 이게 실제 철거 크레인이 하는 동작이기도 하다 — 붐은 서 있고 쇳덩이가 흔들린다.
    /// </summary>
    Transform rig;              // Cable — 붐 끝에 달린 줄
    Quaternion rigRest;
    Vector3 rigTop;             // 붐 끝. 끝까지 안 움직인다

    void Start()
    {
        rig = ball;                       // 빌더가 Cable 을 넣어 준다
        if (rig != null) { rigRest = rig.rotation; rigTop = rig.position; }
        cam = Object.FindFirstObjectByType<KartCamera>();

#if UNITY_EDITOR
        Probe("준비", $"{style} · 줄 {Name(rig)} · 공 {Name(hitPoint)} · 붐끝 {Fmt(rigTop)}");
#endif
    }

#if UNITY_EDITOR
    float probeAt = -1f;
    bool probed;
    Quaternion probeRot;
    Vector3 probeBall;

    static string Name(Transform t) => t == null ? "★null" : t.name;
    static string Fmt(Vector3 v) => $"({v.x:F1}, {v.y:F1}, {v.z:F1})";

    static void Probe(string tag, string body)
    {
        string line = $"[크레인·{tag}] {body}";
        Debug.Log(line);
        try
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "crane_live.txt"),
                line + System.Environment.NewLine);
        }
        catch { }
    }

    /// <summary>줄이 붐 끝에 붙어 있는지 · 공이 실제로 움직이는지 2.5초 재서 한 번만 적는다.</summary>
    void Watch()
    {
        if (probed) return;
        if (probeAt < 0f)
        {
            probeAt = Time.time;
            probeRot = rig != null ? rig.rotation : Quaternion.identity;
            probeBall = hitPoint != null ? hitPoint.position : Vector3.zero;
            return;
        }
        if (Time.time - probeAt < 2.5f) return;
        probed = true;

        float deg = rig != null ? Quaternion.Angle(probeRot, rig.rotation) : -1f;
        float moved = hitPoint != null ? Vector3.Distance(probeBall, hitPoint.position) : -1f;
        float drift = rig != null ? Vector3.Distance(rigTop, rig.position) : -1f;

        string where = "—";
        if (hitPoint != null)
        {
            float g = Physics.Raycast(hitPoint.position + Vector3.up * 40f, Vector3.down,
                                      out var hit, 120f, ~(1 << 2)) ? hit.point.y : float.NaN;
            where = $"공 바닥에서 {hitPoint.position.y - g:+0.00;-0.00}m";
        }

        Probe(deg < 1f && moved < 0.2f ? "★안움직임" : "움직임",
              $"{style} · 줄 {deg:F1}° · 공 {moved:F2}m · 줄끝 어긋남 {drift:F3}m · {where}");
    }
#endif

    void Update()
    {
        if (RacePause.On || RaceCountdown.Blocked || rig == null) return;

        float p = Mathf.Repeat(Time.time / Mathf.Max(0.5f, period), 1f);
        float swing, spin = 0f;        // swing = 앞뒤(길 쪽), spin = 수직축 회전

        switch (style)
        {
            // ④ 곰테크관 — 줄이 비스듬히 선 채 <b>수직축으로 한 바퀴</b>.
            //    공이 넓은 원을 그린다. 붐은 그대로 서 있다.
            default:
            case Style.회전:
                swing = 34f;
                spin = p * 360f;
                break;

            // ② 곰머리관 — 길 쪽으로 <b>크게 넘어와서 버틴다.</b>
            //    공이 길 위에 머무는 동안 지나가면 안 되는 구간이 생긴다.
            case Style.길막:
                swing = p < 0.20f ? Mathf.SmoothStep(0f, 62f, p / 0.20f)
                      : p < 0.56f ? 62f
                      : p < 0.76f ? Mathf.SmoothStep(62f, 0f, (p - 0.56f) / 0.20f)
                      : 0f;
                break;

            // ① 곰밥마당 — <b>일하는 중장비.</b> 크게 한 번 넘겼다가 반대로 때린다.
            //    단순 진자가 아니라 <b>되돌아올 때 더 빠르게</b> — 끝에서 꺾인다.
            case Style.거칠게:
                {
                    float sw = Mathf.Sin(p * Mathf.PI * 2f);
                    sw = Mathf.Sign(sw) * Mathf.Pow(Mathf.Abs(sw), 0.6f);
                    swing = sw * 46f;
                    spin = Mathf.Sin(p * Mathf.PI * 4f) * 16f;   // 좌우로도 약간 비튼다
                }
                break;

            // ③ 곰누리관 — <b>네 대 중 제일 과격하게.</b> 크게 들어 올렸다가
            //    건물 쪽으로 <b>때려 박는다.</b> 때린 뒤 되튕기는 잔진동까지.
            case Style.붕괴:
                // ★ <b>양수가 건물 쪽</b>이다(<c>boomTo</c> 로 등을 돌려세웠으니).
                //   천천히 뒤로 들었다가 → 때리고 → <b>되튕긴다.</b>
                //   벽에 닿는 순간 <b>멈췄다 반대로 밀리는</b> 구간(0.58~0.66)이 있어야
                //   «통과했다» 가 아니라 «맞고 돌아왔다» 로 보인다.
                swing = p < 0.50f ? Mathf.SmoothStep(0f, -58f, p / 0.50f)          // 뒤로 든다
                      : p < 0.58f ? Mathf.Lerp(-58f, 34f, (p - 0.50f) / 0.08f)      // 때린다
                      : p < 0.66f ? Mathf.Lerp(34f, 9f, (p - 0.58f) / 0.08f)        // 되튕긴다
                      : Mathf.Lerp(9f, 0f, (p - 0.66f) / 0.34f)
                        + Mathf.Sin((p - 0.66f) * 30f) * Mathf.Lerp(7f, 0f, (p - 0.66f) / 0.34f);
                spin = Mathf.Sin(p * Mathf.PI * 2f) * 10f;
                Slam(p);
                break;
        }

        // ★ 줄의 <b>원점은 그대로 두고 회전만</b> 준다 — 붐 끝에서 절대 안 떨어진다.
        rig.rotation = Quaternion.AngleAxis(spin, Vector3.up)
                     * Quaternion.AngleAxis(swing, transform.right)
                     * rigRest;
        rig.position = rigTop;

#if UNITY_EDITOR
        Watch();
#endif

#if UNITY_EDITOR
        Watch();
#endif

        // 멀리 있는 크레인은 길에 안 닿는다 — 풍경이다. 맞음 판정은 그대로 거리로 걸러진다.

        var kart = PlayerKart.Current;
        if (kart == null || Time.time - lastHit < 1.2f) return;

        var body = kart.GetComponent<KartController>();
        if (body == null) return;

        Vector3 gap = kart.transform.position - (hitPoint != null ? hitPoint.position : ball.position);
        gap.y = 0f;
        if (gap.sqrMagnitude > hitRadius * hitRadius) return;

        lastHit = Time.time;
        body.Douse(0.55f, 0f);
        if (cam != null) { cam.Jolt(0.4f); cam.Wobble(0.35f); }

        if (ExhibitCargo.DropLast()) Toast.Show("쇳덩이에 맞았다 — 하나 떨어졌다");
        else Toast.Show("쇳덩이에 맞았다");
    }

    bool slammed;

    /// <summary>
    /// 내려찍는 순간 화면을 한 번 흔든다. ★ <b>가까이 있을 때만</b> —
    /// 28m 밖 풍경이 매번 화면을 흔들면 그건 연출이 아니라 «화면 상태» 가 된다
    /// (주행 진동을 늘 켜두면 안 되는 것과 같은 규칙).
    /// </summary>
    void Slam(float p)
    {
        if (p < 0.58f) { slammed = false; return; }
        if (slammed) return;
        slammed = true;

        Dust();          // ★ 맞은 자리에 먼지. 카메라가 멀어도 먼지는 난다

        var kart = PlayerKart.Current;
        if (kart == null || cam == null) return;

        float d = Vector3.Distance(kart.transform.position, transform.position);
        if (d > 70f) return;

        cam.Wobble(Mathf.Lerp(0.5f, 0.05f, d / 70f));
    }

    ParticleSystem dust;

    /// <summary>
    /// 때린 자리에 먼지 한 번. ★ <see cref="SmokePuff"/> 가 이미 있어서 <b>새 에셋이 0개</b>다
    /// (카트 배기 · 급식 김과 같은 재질). 재질을 못 찾으면 조용히 포기한다.
    /// </summary>
    void Dust()
    {
        if (hitPoint == null) return;

        if (dust == null)
        {
            var mat = SmokePuff.Material();
            if (mat == null) return;

            var go = new GameObject("CraneDust");
            go.transform.SetParent(transform, false);
            dust = go.AddComponent<ParticleSystem>();

            var main = dust.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 2.4f;
            main.startSize = 1.7f;
            main.startColor = new Color(0.74f, 0.70f, 0.62f, 0.50f);
            main.gravityModifier = -0.08f;                 // 살짝 떠오른다
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 60;

            var em = dust.emission; em.enabled = false;     // 터뜨릴 때만
            var sh = dust.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.9f;

            dust.GetComponent<ParticleSystemRenderer>().material = mat;
            dust.Stop();
        }

        dust.transform.position = hitPoint.position;
        dust.Emit(16);
    }
}

/// <summary>
/// ★★ <b>점검등</b>(2026-10-12). 안전점검 헬기가 코스를 비추고, <b>그 안에 있으면 느려진다.</b>
///
/// 유저: *"헬기가 지켜보기만 하고 아무것도 안 하니 좀 그렇다."* 맞다 — 풍경은 판을 바꾸지만
/// <b>기믹은 운전을 바꾼다.</b> 다만 이 판은 «담장 긁지 않기» 라 <b>밀거나 비틀면 안 된다</b> —
/// 그건 내가 플레이어를 벽에 밀어 놓고 실패시키는 것이다. <b>느려지기만</b> 한다.
///
/// 피할 수 있어야 벌이 아니라 기믹이다. 그래서 셋을 지킨다:
/// <list type="bullet">
/// <item><b>바닥에 고리가 보인다.</b> 빛줄기만 있으면 어디가 닿는 데인지 모른다.</item>
/// <item><b>기체보다 앞을 비춘다.</b> 마주 오는 헬기라, 고리가 먼저 와야 피할 틈이 생긴다.</item>
/// <item><b>일정한 박자로 좌우로 쓴다.</b> 무작위면 외울 수가 없고, 외울 수 없으면 운이다.</item>
/// </list>
/// </summary>
public class SearchBeam : MonoBehaviour
{
    public FlyingProp flyer;

    /// <summary>
    /// 잡히는 반지름. ★ 2026-10-12 제작자: *"점검등이 너무 크고 요란하다."*
    /// 4.4 → <b>2.8</b>. 길 반폭이 3.5~6m 라 2.8 이면 <b>한쪽으로 비키면 피해진다.</b>
    /// </summary>
    /// <summary>
    /// ★★★ 2026-10-15 (세 번째) — <b>5.5m 로 넓혔다가 되돌린다.</b>
    ///   제작자: *"불빛이 왜 이리 커."* 맞다 — 길을 통째로 덮으면 피할 데가 없고,
    ///   피할 수 없는 건 기믹이 아니라 벌이다. <b>3.0m</b> — 레이저 점이다.
    /// </summary>
    /// <summary>
    /// ★★★ 2026-10-15 — <b>재 보니 피할 데가 구조적으로 없었다.</b>
    ///
    /// 제일 좁은 길이 폭 7m(반폭 3.5)인데, 고리가 <c>반폭 × 0.52</c> 만큼 좌우로
    /// 쓸고 반지름이 2.2 였다. 합치면 <b>1.82 + 2.2 = 4.02</b> — <b>길 반폭보다 넓다.</b>
    /// 한쪽 끝에 왔을 때 그쪽은 통째로 막히고, 2.8초마다 반대쪽으로 넘어온다.
    /// <b>카트 폭이 1.1m 인데 남는 틈이 그만큼도 안 됐다.</b>
    ///
    /// 셋을 같이 줄인다 — 고리 1.6 · 흔들림 0.30 · 왕복 4.6초.
    /// 최대로 뻗어도 <b>2.65 &lt; 3.5</b> 라 <b>어느 순간에도 길 한쪽은 열려 있고</b>,
    /// 가운데 있을 때는 양쪽에 1.9m 씩 남는다.
    /// </summary>
    public float radius = 1.6f;

    /// <summary>기체보다 얼마나 앞을 비추나. 마주 오니 이만큼 먼저 온다.</summary>
    public float leadMetres = 34f;   // 20 → 34. 더 멀리서 보여야 비킬 틈이 생긴다

    /// <summary>길 반폭의 몇 배까지 좌우로 쓸까. 1 이면 길을 통째로 막는다.</summary>
    public float sweep = 0.30f;      // 0.52 → 0.30. 길 한쪽은 늘 열려 있어야 한다

    /// <summary>좌우로 한 번 왕복하는 데 걸리는 시간.</summary>
    public float period = 4.6f;      // 2.8 → 4.6. 느려야 어디로 가는지 읽힌다

    /// <summary>안에 있을 때 1초에 남는 속도.</summary>
    public float keepPerSecond = 0.42f;

    /// <summary>
    /// ★ 2026-10-12 제작자: *"빔 맞으면 속도가 반으로 줄게 만들까."* <b>좋다 — 한 번에 반이면
    /// 느려진 게 손에 잡힌다.</b> 조금씩 끌리는 것(<see cref="keepPerSecond"/>)은
    /// «빛 안에 있는 동안» 이고, 이건 <b>들어가는 순간</b> 한 방이다.
    /// </summary>
    public float enterCut = 0.5f;

    /// <summary>
    /// ★★ 2026-10-12 제작자: *"속도 느려지는 게 체감이 안 된다. 차라리 빔 맞았을 때
    /// 잠시 휘청거리는 건 어떨까 — 눈뽕이라는 의미에서."* <b>맞는 요구인데 조건이 있다.</b>
    ///
    /// 이 판이 «담장 긁지 않기» 라, <b>조향을 건드리면 내가 플레이어를 벽에 밀어 놓고
    /// 실패시키는 꼴</b>이 된다. 그래서 <b>화면만 때린다</b> — 하얗게 번쩍, 카메라가 기울고,
    /// 속도는 그대로 깎는다. 눈은 놀라는데 차는 제 길을 간다.
    /// </summary>
    public float flash;
    float flashAt = -9f;

    /// <summary>마지막으로 걸린 시각.</summary>
    float lastCaught = -99f;

    /// <summary>한 번 걸리면 이 동안은 다시 안 센다 — 한 번 지나갈 때 한 번만.</summary>
    const float CaughtCooldown = 5.0f;   // 3.5 → 5.0. 한 바퀴에 한 번도 안 깎일 수 있다

    Transform shaft, ring;
    Material shaftMat, ringMat;
    KartController player;
    TrackBuilder track;
    bool toldOnce;

    static readonly Color Lamp = new Color(1.00f, 0.90f, 0.58f);
    static readonly Color Caught = new Color(0.86f, 0.33f, 0.28f);

    void Start()
    {
        if (flyer != null) track = flyer.patrol;
        foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
            if (k.GetComponent<PlayerKart>() != null) { player = k; break; }

        shaft = Make("BeamShaft", 0.30f, out shaftMat);
        ring = Make("BeamRing", 0.62f, out ringMat);
    }

    Transform Make(string name, float alpha, out Material mat)
    {
        // 원기둥이다. Quad 는 한쪽 면만 있어서 «어느 쪽이 앞인가» 가 또 하나의 짐작거리가 된다.
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(go.GetComponent<Collider>());
        go.name = name;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);   // 더하기 — 빛이다
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3100;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_BaseColor", new Color(Lamp.r, Lamp.g, Lamp.b, alpha));
        mat.SetColor("_EmissionColor", Lamp * 1.6f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    void LateUpdate()
    {
        if (shaft == null || flyer == null || track == null) return;

        bool off = RacePause.On || RaceCountdown.Blocked;
        shaft.gameObject.SetActive(!off);
        ring.gameObject.SetActive(!off);
        if (off) return;

        // 기체보다 <b>앞</b>을 비춘다. 미터를 바퀴 비율로 바꾸니 코스 길이를 바꿔도 안 고친다.
        float lapLen = Mathf.Max(50f, track.LapLength);
        float ahead = leadMetres / lapLen * (flyer.reverse ? -1f : 1f);
        float t01 = Mathf.Repeat(flyer.Lap + ahead, 1f);

        Vector3 fwd = track.TangentOnPath(t01);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        float half = track.WidthOnPath(t01) * 0.5f;
        float lateral = Mathf.Sin(Time.time * (2f * Mathf.PI / Mathf.Max(0.4f, period))) * half * sweep;

        Vector3 ground = track.transform.position + track.PointOnPath(t01) + side * lateral;
        if (Physics.Raycast(ground + Vector3.up * 40f, Vector3.down, out var hit, 90f, ~(1 << 2)))
            ground = hit.point;

        ring.position = ground + Vector3.up * 0.06f;
        ring.rotation = Quaternion.identity;
        ring.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        // 빛줄기 — 기체에서 바닥까지. 유니티 원기둥은 Y 로 2 단위라 길이의 절반을 준다.
        Vector3 from = transform.position;
        Vector3 to = ground;
        Vector3 dir = to - from;
        float len = dir.magnitude;
        if (len < 0.5f) return;

        shaft.position = (from + to) * 0.5f;
        shaft.rotation = Quaternion.FromToRotation(Vector3.up, dir / len);
        shaft.localScale = new Vector3(radius * 1.35f, len * 0.5f, radius * 1.35f);

        if (player == null) return;

        Vector3 gap = player.transform.position - ground;
        gap.y = 0f;
        bool inside = gap.sqrMagnitude < radius * radius;

        if (inside)
        {
            // ★ <b>매 프레임 조금씩</b> 끌린다. 한 번에 깎으면 «벽에 부딪힌 것» 이고,
            //   그러면 담장을 안 긁었는데도 긁은 것 같은 손해로 느껴진다.
            player.Wade(keepPerSecond);

            // ★ 들어간 <b>그 순간</b>에만 때린다. 계속 번쩍이면 그건 연출이 아니라 고장이다.
            if (Time.time - flashAt > 1.1f)
            {
                flashAt = Time.time;
                player.Douse(enterCut, 0f);          // 들어가는 순간 속도 반
                var cam = Object.FindFirstObjectByType<KartCamera>();
                if (cam != null) { cam.Jolt(0.24f); cam.Wobble(0.5f); }
                ScreenFlash.Hit(0.5f, new Color(1f, 0.97f, 0.86f));
                Sfx.Play("WallHit", 0.35f, duckMusic: false);
            }

            if (!toldOnce && player.SpeedKph > 12f)
            {
                toldOnce = true;
                Toast.Show("점검등 — 빛에 닿으면 기회가 깎인다");
            }

            // ★★★ 2026-10-15 제작자: *"헬기가 빛만 비추고 해를 안 끼친다."*
            //
            //   <b>스치는 건 그대로 느려지기만</b> 한다 — 순간 걸린 걸 벌하면
            //   «랜덤으로 깎인다» 가 되고, 이 판은 「담장 긁지 않기」라
            //   벽을 안 긁었는데 실패하는 게 제일 억울하다.
            //
            //   대신 <b>1.5초 넘게 머무는 건 명백히 플레이어 선택</b>이다.
            //   피할 수 있고, 배울 수 있고, 왜 깎였는지도 안다.
            //   깎고 나서 시계를 다시 재니 <b>계속 있으면 1.5초마다 하나</b>다.
            // ★★ 2026-10-15 (두 번째) — <b>«연속 1.5초» 는 영영 안 채워진다.</b>
            //   제작자: *"헬기가 슉 하고 지나가서 1.5초 머무를 시간이 없다."* 맞다 —
            //   헬기는 13초에 한 바퀴를 거꾸로 도니 <b>스쳐 지나가는 게 전부</b>고,
            //   한 번에 1.5초를 머무는 건 애초에 불가능하다.
            //
            //   그래서 <b>쌓아서</b> 센다. 빛을 벗어나도 안 지워지고, 합이 1.5초를
            //   넘을 때마다 하나. <b>한 번 스친 건 공짜지만 계속 잡히면 값을 치른다</b> —
            //   의도는 같고, 이쪽은 실제로 일어난다.
            // ★★★ 2026-10-15 (네 번째) — <b>닿으면 그 자리에서 하나.</b>
            //
            //   제작자: *"똑같이 맞아도 어떨 땐 깎이고 어떨 땐 안 깎인다. 판단 기준이 뭐냐."*
            //   <b>맞는 지적이고 내 설계가 틀렸다.</b> 「합이 1초를 넘으면」은
            //   플레이어에게 <b>보이지 않는 저금통</b>이라, 같은 행동의 결과가 매번 다르다.
            //   보이지 않는 규칙은 규칙이 아니라 운이다.
            //
            //   <b>고양이 레이저 포인터</b>로 간다(제작자 제안) — <b>닿으면 그 자리에서 하나.</b>
            //   대신 고리를 3m 로 줄여 피할 수 있게 하고, 한 번 물리면
            //   <see cref="CaughtCooldown"/> 동안은 다시 안 센다 — <b>한 번 지나갈 때
            //   한 번만</b>이다(벽 부딪힘을 0.7초로 묶은 것과 같은 판단).
            if (Time.time - lastCaught >= CaughtCooldown)
            {
                lastCaught = Time.time;
                player.TakeChance();
                Toast.Show("점검등에 걸렸다 — 기회 하나");
                Sfx.Play("MissionFail", 0.30f, duckMusic: false);
            }
        }

        var c = inside ? Caught : Lamp;
        ringMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, inside ? 0.80f : 0.62f));
        ringMat.SetColor("_EmissionColor", c * (inside ? 2.4f : 1.6f));
        shaftMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, inside ? 0.40f : 0.30f));
        shaftMat.SetColor("_EmissionColor", c * (inside ? 2.1f : 1.6f));
    }

    void OnDestroy()
    {
        if (shaft != null) Object.Destroy(shaft.gameObject);
        if (ring != null) Object.Destroy(ring.gameObject);
    }
}

/// <summary>
/// ★★ <b>하늘에 뜬 것의 엔진 소리</b>(2026-10-12). 유저: *"가까이 올 때 쿠와앙 내고,
/// 멀어지면 멀어지는 소리가 났으면."*
///
/// <b>다가오고 멀어지는 소리를 따로 만들지 않는다.</b> 3D 음원 하나를 물체에 붙이면
/// 유니티가 거리 감쇠와 <b>도플러</b>를 둘 다 해 준다 — 마주 오면 음이 올라가고
/// 지나가면 뚝 떨어진다. 손으로 만든 «멀어지는 소리» 는 플레이어가 어디 있느냐에 따라
/// 늘 어긋나지만, 이건 <b>언제나 맞는다.</b>
///
/// 소리는 합성해서 만들었다(스크래치패드 <c>blimp_sfx.py</c>) —
/// 프로펠러 둘을 2.5Hz 어긋나게 돌려 맥놀이를 만들고, 날개 지나가는 변조를 얹고,
/// 선체 저역을 깔았다. <b>4초에 정수 주기로만</b> 만들어서 이음매가 안 들린다
/// (측정: 이음매 단차 0.0088 &lt; 보통 단차 최대 0.0208).
///
/// 크기는 <see cref="Music.Volume"/> 을 따라간다 — 슬라이더 하나로 같이 움직여야 한다.
/// </summary>
public class PropEngine : MonoBehaviour
{
    public string clipName = "Blimp_Engine";
    public float volume = 0.9f;

    /// <summary>이 안에서는 안 작아진다. 선체가 20m 가 넘으니 그만큼은 줘야 한다.</summary>
    public float near = 16f;

    /// <summary>여기서부터는 더 안 작아진다 — 로그 감쇠라 0 이 되지는 않는다(16/190 = 8%).</summary>
    public float far = 190f;

    AudioSource src;
    float strain;

    void Start()
    {
        var clip = Resources.Load<AudioClip>("Sfx/" + clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[풍경] 엔진 소리 {clipName} 을 못 찾았다 — Resources/Sfx/");
            return;
        }

        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 1f;                       // 3D — 자리로 들린다
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = near;
        src.maxDistance = far;
        src.dopplerLevel = 2.2f;                     // 1 이 현실. 조금 과장해야 레이스에서 읽힌다
        src.time = Random.value * clip.length;       // 둘 이상 떠 있어도 겹쳐 들리지 않게
        src.Play();
    }

    void Update()
    {
        if (src == null) return;

        // 멈춰 있으면 소리도 멈춘다 — «멈춤» 은 그림도 소리도 멈추는 것이어야 한다.
        if (RacePause.On || RaceCountdown.Blocked) { if (src.isPlaying) src.Pause(); return; }
        if (!src.isPlaying) src.UnPause();

        strain = Mathf.Lerp(strain, 0f, 1f - Mathf.Exp(-3f * Time.deltaTime));
        src.volume = Mathf.Clamp01(Music.Volume * 1.5f) * volume * (1f + strain * 0.55f);
        src.pitch = 1f + strain * 0.16f;
    }

    /// <summary>급강하 같은 사건에 한 방. <b>같은 음원에서</b> 나야 자리가 맞는다.</summary>
    public void Boom(string name, float gain = 1f)
    {
        if (src == null) return;
        var clip = Resources.Load<AudioClip>("Sfx/" + name);
        if (clip == null) return;
        src.PlayOneShot(clip, Mathf.Clamp01(Music.Volume * 1.9f) * gain);
    }

    /// <summary>엔진이 힘쓰는 중 — 0~1. 커지고 음이 조금 올라간다.</summary>
    public void Strain(float amount) => strain = Mathf.Max(strain, Mathf.Clamp01(amount));
}

/// <summary>코스 <b>바깥</b> 길을 따라 같이 달리는 차. 코스 모양을 바꿔도 안 고친다.</summary>
public class ChaseVehicle : MonoBehaviour
{
    public TrackBuilder track;
    public float offset = 18f;
    public float lead = 0f;

    Transform player;
    float t;

    void Start()
    {
        foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
            if (k.GetComponent<PlayerKart>() != null) { player = k.transform; break; }
    }

    /// <summary>
    /// 플레이어가 코스 어디쯤인지. <b>64칸만 훑는다</b> — <see cref="KartAi"/> 와 같은 생각이고,
    /// 코스 모양을 바꿔도 이 코드는 안 고친다.
    /// </summary>
    float NearestT(Vector3 from)
    {
        float best = 0f, bestD = float.MaxValue;
        for (int i = 0; i < 64; i++)
        {
            float u = i / 64f;
            float d = (track.transform.position + track.PointOnPath(u) - from).sqrMagnitude;
            if (d < bestD) { bestD = d; best = u; }
        }
        return best;
    }

    void Update()
    {
        if (track == null || RacePause.On) return;

        float want = player != null ? Mathf.Repeat(NearestT(player.position) + lead, 1f)
                                    : Mathf.Repeat(Time.time * 0.04f, 1f);

        t = Mathf.Repeat(t + Mathf.DeltaAngle(t * 360f, want * 360f) / 360f * 3f * Time.deltaTime, 1f);

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * offset;

        transform.position = Vector3.Lerp(transform.position, at, 1f - Mathf.Exp(-4f * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}

// ======================================================================
//  5판 — 관장 도장이 쿵 찍는다
// ======================================================================
/// <summary>
/// ★★ 2026-10-06 유저 제안: *"미션 5에 스탬프 말인데 몇 초마다 한 번씩 세게 쿵쿵
/// 찍어내리면 어때. 그러다가 레이싱할 때 진동 울리는 거지. 그때 점프 기능 누르면 어때.
/// 점프 기능 쓸 일 없잖아 지금. 아님 종이를 조금 가져와서 플레이 화면에 날린 뒤
/// 원상복귀 시키거나."*
///
/// 둘 다 넣었다. <b>SPACE(호핑)에 처음으로 쓸 일이 생긴다</b> — 2026-09-16 에
/// 태엽(SHIFT)과 호핑(SPACE)을 가른 뒤로 호핑은 할 일이 없었다.
///
/// ★ <b>임무 내용은 안 바꾼다.</b> 5판은 그대로 «94초 안에 완주» 고, 땅울림은
/// 속도에만 영향을 준다 — 넘으면 조금 빨라지고 맞으면 조금 느려진다.
/// 판정에 끼어들면 그건 새 임무지 연출이 아니다.
/// </summary>
public class StampSlam : MonoBehaviour
{
    [Tooltip("몇 초마다 한 번 찍나")]
    public float period = 5.2f;
    [Tooltip("도장끼리 어긋나게 — 다 같이 찍으면 쿵쿵이 아니라 한 번이다")]
    public float phase = 0f;
    [Tooltip("들어 올리는 높이 — 솟았다 내려찍는 연출이라 높을수록 예고가 잘 보인다")]
    public float lift = 4.6f;

    Transform stamp;
    Vector3 home;
    PaperDrift papers;
    float lastU = 1f;

    /// <summary>내리꽂기가 끝나는 지점(주기 안의 비율). 여기서 «쿵» 이 난다.</summary>
    const float Hit = 0.70f;

    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == "Stamp") { stamp = t; break; }

        if (stamp == null)
        {
            Debug.LogWarning("[풍경] 도장(Stamp)을 못 찾았다 — R04_Paper_Storm 의 부품 이름을 봐라.");
            enabled = false; return;
        }
        home = stamp.localPosition;
        papers = GetComponent<PaperDrift>();

        // ★ 2026-10-12 — <b>빨간 경고 기둥을 뺐다</b>(유저: "빨간색 오브제가 촌스럽다").
        //   대신 <b>도장 자신이 예고한다</b> — 양옆으로 흔들리다가 하늘로 쑥 솟고 내려찍는다.
        //   경고를 다른 물건에 맡기면 그 물건을 또 설명해야 하지만, 동작은 설명이 필요 없다.
    }

    void Update()
    {
        if (RacePause.On || RaceCountdown.Blocked) return;

        float u = Mathf.Repeat(Time.time + phase, period) / period;

        // 천천히 올라갔다가 <b>짧게 내리꽂는다.</b> 올라가는 데 오래 걸려야
        // «지금 찍는다» 가 예고되고, 그래야 뛰어넘을 준비를 할 수 있다.
        // ★★ 2026-10-12 — <b>쿵쿵이처럼</b>(유저 요청). 셋으로 나뉜다:
        //   <b>흔들림</b>(0~0.50) 양옆으로 비틀거린다 — "곧 찍는다"
        //   <b>솟음</b>(0.50~Hit−0.09) 하늘로 쑥. 높이가 경고고, 높을수록 눈에 띈다
        //   <b>내리꽂기</b>(마지막 0.09) 짧고 빠르게. 떨어지는 시간이 짧아야 «쾅» 이다
        float h, sway = 0f;
        if (u < 0.50f)
        {
            h = u / 0.50f * 0.10f;                                   // 살짝 뜬 채로
            sway = Mathf.Sin(u * 34f) * (u / 0.50f) * 0.55f;         // 점점 크게 비틀거린다
        }
        else if (u < Hit - 0.09f)
        {
            float k = (u - 0.50f) / (Hit - 0.09f - 0.50f);
            h = 0.10f + Mathf.Sin(k * Mathf.PI * 0.5f) * 0.90f;      // 하늘로
            sway = Mathf.Sin(u * 34f) * (1f - k) * 0.55f;
        }
        else if (u < Hit)
        {
            float k = (u - (Hit - 0.09f)) / 0.09f;
            h = 1f - k * k;                                          // 떨어진다 — 끝에서 제일 빠르다
        }
        else h = 0f;

        stamp.localPosition = home + Vector3.up * (lift * h) + Vector3.right * sway;

        // 주기를 넘어가며 Hit 를 지나는 순간에만 한 번
        if (lastU < Hit && u >= Hit)
        {
            RaceShock.Boom(stamp.position);
            Sfx.Play("StampSlam", 0.8f, duckMusic: false);
            if (papers != null) papers.Gust();
        }
        lastU = u;
    }
}

/// <summary>
/// 도장이 찍힐 때 퍼지는 <b>땅울림</b>. 보이는 고리가 바닥을 쓸고 지나가고,
/// 지나갈 때 <b>땅에 붙어 있으면</b> 속도를 깎인다 — <b>SPACE 로 떠 있으면 넘어간다.</b>
///
/// ★ 고리를 <b>눈에 보이게</b> 만드는 게 핵심이다. 안 보이면 «랜덤으로 느려진다» 가 되고,
/// 이 프로젝트는 그 민원을 이미 두 번 받았다(벽 부딪힘 · 자재).
/// </summary>
public static class RaceShock
{
    /// <summary>퍼지는 속도. 도장이 갓길 15m 에 있으니 코스까지 0.6초쯤 걸린다.</summary>
    const float Speed = 24f;
    // ★ 44m 로 뒀다가 다시 계산했다. 도장 다섯이 평균 1.1초마다 찍는데 고리가 44m 면
    //   <b>코스 전체가 늘 땅울림 안</b>이라 랩마다 열 번씩 맞는다 — 그러면 «94초 안에 완주» 가
    //   사실상 다른 임무가 된다. <b>28m 면 도장 근처 20m 쯤만 쓸고 지나간다.</b>
    const float Reach = 28f;
    const int Puffs = 26;

    /// <summary>
    /// ★★ 2026-10-06 유저: *"어느 타이밍에 찍히는지도 몰라서 점프 회피가 불가능하며,
    /// 도장 위력이 약하다. 빨간색 길이나 레이저 같은 걸로 표시해 주면 안 되나."*
    /// <b>경고가 없으면 회피는 운이고, 회피가 운이면 위력을 올릴 수가 없다.</b>
    /// 그래서 셋을 같이 바꿨다 — 빨간 고리 · 도장 위 레이저 · 깎이는 양.
    /// </summary>
    static readonly Color Warn = new Color32(0xC4, 0x45, 0x3E, 0xFF);

    class Ring
    {
        public Vector3 at;
        public float born, last;
        public bool used;
        public Transform[] puff;
    }

    static readonly List<Ring> rings = new();
    static KartController player;
    static KartCamera cam;
    static bool toldOnce;
    static int dodged;

    public static void Clear()
    {
        foreach (var r in rings)
            if (r.puff != null && r.puff.Length > 0 && r.puff[0] != null)
                Object.Destroy(r.puff[0].parent.gameObject);
        rings.Clear();
        dodged = 0;
    }

    public static void Boom(Vector3 at)
    {
        // 바닥을 찾아 거기에 깐다. 못 찾으면 y 0
        float y = 0f;
        if (Physics.Raycast(at + Vector3.up * 2f, Vector3.down, out var hit, 40f, ~(1 << 2))) y = hit.point.y;

        var holder = new GameObject("Shock").transform;
        holder.position = new Vector3(at.x, y + 0.06f, at.z);

        // ★ 찍는 순간 <b>화면이 양옆으로 물결친다</b>(2026-10-12 유저 요청).
        //   가까울수록 세다 — 멀리서 찍힌 것까지 화면을 흔들면 그건 «화면 상태» 가 된다.
        if (cam == null) cam = Object.FindFirstObjectByType<KartCamera>();
        if (cam != null && player != null)
        {
            float d = Vector3.Distance(player.transform.position, holder.position);
            if (d < 46f) cam.Wobble(Mathf.Lerp(0.42f, 0.04f, d / 46f));
        }

        var mat = FlatMaterial.Get(Warn);
        var puff = new Transform[Puffs];
        for (int i = 0; i < Puffs; i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(holder, false);
            d.GetComponent<Renderer>().sharedMaterial = mat;
            d.transform.localScale = new Vector3(2.2f, 0.02f, 2.2f);
            puff[i] = d.transform;
        }
        rings.Add(new Ring { at = holder.position, born = Time.time, puff = puff });
    }

    public static void Tick()
    {
        if (rings.Count == 0) return;

        if (player == null)
            foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
                if (k.GetComponent<PlayerKart>() != null) { player = k; cam = Object.FindFirstObjectByType<KartCamera>(); break; }

        for (int i = rings.Count - 1; i >= 0; i--)
        {
            var r = rings[i];
            float age = Time.time - r.born;
            float rad = age * Speed;

            if (rad > Reach)
            {
                if (r.puff != null && r.puff.Length > 0 && r.puff[0] != null)
                    Object.Destroy(r.puff[0].parent.gameObject);
                rings.RemoveAt(i);
                continue;
            }

            // 고리를 그린다 — 점점 커지고 옅어진다
            float fade = 1f - rad / Reach;
            fade *= fade;
            for (int p = 0; p < r.puff.Length; p++)
            {
                if (r.puff[p] == null) continue;
                float a = p / (float)r.puff.Length * Mathf.PI * 2f;
                r.puff[p].localPosition = new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad);
                float s = (2.0f + rad * 0.16f) * fade;
                r.puff[p].localScale = new Vector3(s, 0.02f, s);
            }

            // 플레이어를 지나가는 그 프레임에 한 번만
            if (!r.used && player != null)
            {
                Vector3 d = player.transform.position - r.at; d.y = 0f;
                float dist = d.magnitude;
                if (r.last < dist && dist <= rad)
                {
                    r.used = true;
                    Resolve(player);
                }
            }
            r.last = rad;
        }
    }

    static void Resolve(KartController kart)
    {
        if (kart.IsHopping || !kart.IsGrounded)
        {
            // 넘었다 — 작은 보상. 벌만 있고 상이 없으면 아무도 안 뛴다.
            // ★ 2026-10-12 — <b>«넘었다» 알림은 뺐다</b>(유저 요청). 부스트가 손에 느껴지니
            //   글자가 필요 없고, 레이스 중에 뜨는 글자는 읽느라 한 박자를 잡아먹는다.
            kart.ApplyBoost(2.5f, 0.7f);
            if (cam != null) cam.Jolt(0.06f);
            dodged++;
            return;
        }

        // ★★ 2026-10-12 — <b>3초 멈춘다</b>(유저 요청). 전에는 속도를 14% 깎고 말아서
        //   «맞아도 되고 안 맞아도 되는» 상태였다 — 그러면 뛰어넘을 이유가 없다.
        kart.Stall(3f);
        if (cam != null) { cam.Jolt(0.5f); cam.Wobble(0.6f); }

        if (!toldOnce) { toldOnce = true; Toast.Show("깔렸다 — SPACE 로 뛰어넘는다"); }
    }
}

// ======================================================================
//  7판(드론 판) — 위에서 철거 자재가 떨어진다
// ======================================================================
/// <summary>
/// ★★ 2026-10-06 유저: *"드론 있는 임무 말인데 그냥 날아다니기만 하는 거야?
/// 그 임무 한정으로 자재를 랜덤으로 위에서 떨어뜨린다든가 그런 기믹 추가해도 돼?
/// 기획에는 맞나. 치이카와 같은 불쾌한 은유는 있나."*
///
/// <b>기획에 맞는다.</b> 이 판(<c>Goal.장애물</c>)의 이름이 이미 «철거 자재» 고,
/// 바닥에 굴러다니는 <see cref="RoadDebris"/> 가 그 자재다. <b>위에서 떨어지는 쪽이
/// 오히려 원래 그림</b>이야 — 철거는 위에서부터 한다.
///
/// 은유도 공짜로 생긴다. <b>중계 드론이 찍고 있는 바로 그 위에서 자재가 떨어진다.</b>
/// 치는 쪽과 찍는 쪽이 같은 하늘에 있고, 아무도 «그만» 이라고 안 한다 —
/// 방송은 사고를 막는 게 아니라 <b>사고를 기다린다.</b> 설명하는 대사가 한 줄도 없어서
/// 더 불쾌하다. 그게 치이카와식이다(§4.4 의 광고판과 같은 수법).
///
/// ★ <b>임무 판정에는 안 들어간다.</b> 이 판의 «기회 3» 은 <see cref="RoadDebris"/> 를
/// 센 것이고, 낙하 자재는 속도만 깎는다. 판정에 끼어들면 그건 새 임무지 연출이 아니다.
/// </summary>
public static class SkyDrop
{
    /// <summary>몇 초마다 하나 떨어지나.</summary>
    const float Every = 3.4f;
    /// <summary>빨간 표적이 떠 있는 시간 — 이만큼 보고 피한다.</summary>
    const float Telegraph = 1.45f;
    /// <summary>표적 반지름. 이 안에 있으면 맞는다.</summary>
    const float Radius = 3.2f;

    class Drop
    {
        public Vector3 at;
        public float born;
        public bool landed;
        public Transform mark, outline, block;
    }

    static readonly List<Drop> live = new();

    /// <summary>
    /// ★★★ 2026-10-14 — <b>떨어진 슬래브를 여기 모은다.</b>
    ///
    /// 제작자: *"자재가 공중에 뜬다"* · *"나보고 어떻게 지나가라고. 계속 전에 임무 실패한 게 쌓인다."*
    ///
    /// <b>원인:</b> 착지한 슬래브는 «길에 쌓이는 게 연출» 이라 일부러 남겼는데
    /// (2026-10-06), 그때 <see cref="live"/> 목록에서 <b>빼</b> 버렸다.
    /// <see cref="Clear"/> 는 그 목록만 비우니 <b>이미 떨어진 것은 손이 안 닿는다</b> —
    /// 다시 하기를 누를 때마다 길 위에 한 겹씩 더 쌓였다. 결국 벽이 된다.
    ///
    /// 이제 전용 통에 담는다. 판이 바뀌거나 다시 시작하면 <b>통째로 치운다.</b>
    /// 한 판 안에서도 <see cref="MaxLanded"/> 개를 넘으면 오래된 것부터 지운다 —
    /// 세 바퀴에 스물다섯 개가 떨어지는데 그게 다 남으면 그 판만 길이 좁아진다.
    /// </summary>
    static readonly List<Transform> landed = new();
    static Transform landedRoot;

    /// <summary>길에 남길 수 있는 슬래브 수. 이걸 넘으면 오래된 것부터 지운다.</summary>
    const int MaxLanded = 12;

    static float nextAt;
    static KartController player;
    static KartCamera cam;
    static bool told;

    public static void Clear()
    {
        foreach (var d in live) if (d.mark != null) Object.Destroy(d.mark.parent.gameObject);
        live.Clear();

        // ★ 떨어져서 길에 남아 있던 것까지 치운다 — 여기가 빠져서 판마다 쌓였다.
        foreach (var t in landed) if (t != null) Object.Destroy(t.gameObject);
        landed.Clear();
        if (landedRoot != null) Object.Destroy(landedRoot.gameObject);
        landedRoot = null;

        nextAt = 0f;
    }

    public static void Tick(TrackBuilder track)
    {
        bool want = MissionManager.CurrentGoal == MissionManager.Goal.장애물;
        if (!want) { if (live.Count > 0) Clear(); return; }

        if (player == null)
        {
            foreach (var k in Object.FindObjectsByType<KartController>(FindObjectsSortMode.None))
                if (k.GetComponent<PlayerKart>() != null) { player = k; break; }
            cam = Object.FindFirstObjectByType<KartCamera>();
        }

        if (!RaceCountdown.Blocked && !RacePause.On && player != null && Time.time >= nextAt)
        {
            nextAt = Time.time + Every * Random.Range(0.75f, 1.3f);
            Spawn(track);
        }

        for (int i = live.Count - 1; i >= 0; i--) Step(live[i], i);
    }

    static void Spawn(TrackBuilder track)
    {
        // ★ <b>플레이어 앞</b>에 떨군다. 뒤나 옆에 떨어지면 못 보고, 못 보면 연출이 아니라 잡음이다.
        //   62~96m 앞이면 22m/s 로 3~4초 — 표적을 보고 피할 시간이 난다.
        float t = NearestT(track, player.transform.position);
        t = Mathf.Repeat(t + Random.Range(62f, 96f) / Mathf.Max(1f, track.LapLength), 1f);

        Vector3 fwd = track.TangentOnPath(t);
        Vector3 side = Vector3.Cross(Vector3.up, fwd);
        Vector3 at = track.transform.position + track.PointOnPath(t) + side * Random.Range(-3.4f, 3.4f);

        // ★★ 2026-10-06 유저: *"공중에 자재가 떨어뜨려지는 경우도 있는데 그럼 안 돼.
        //   땅에 박히게 해야지."* <b>바닥을 못 찾으면 아예 안 떨군다.</b>
        //   전에는 못 찾으면 코스 높이를 그대로 썼는데, 그 값은 길 표면이 아니라
        //   <b>중심선의 높이</b>라 둔덕·다리 위에서는 허공이 된다.
        if (!Physics.Raycast(at + Vector3.up * 8f, Vector3.down, out var hit, 60f, ~(1 << 2))) return;
        at.y = hit.point.y;

        var holder = new GameObject("SkyDrop").transform;
        holder.position = at;

        var red = FlatMaterial.Get(new Color32(0xC4, 0x45, 0x3E, 0xFF));
        var cream = FlatMaterial.Get(new Color32(0xFD, 0xF8, 0xEC, 0xFF));
        var stone = FlatMaterial.Get(new Color32(0xA8, 0xA4, 0x9A, 0xFF));

        // 바깥 테 — 크기가 고정이라 «여기가 범위» 를 알려준다
        var ring = Disc(holder, red, Radius * 2f, 0.03f);
        // 안쪽 원 — 줄어든다. 다 줄면 떨어진다
        var mark = Disc(holder, cream, Radius * 2f, 0.05f);

        // 떨어지는 자재 — 콜라이더 없음. 물리로 치면 카트가 날아가 레이스가 망가진다
        // ★ 2026-10-12 제작자: *"드론이 떨어뜨린 자재가 카트를 통과해."* 맞다 —
        //   콜라이더를 떼고 <b>떨어지는 순간 반경으로만</b> 판정했더니, 쌓인 뒤로는 그림이었다.
        //   <b>콜라이더를 남긴다.</b> 길에 떨어진 자재와 같은 물건이어야 규칙이 하나가 된다.
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Slab";
        cube.GetComponent<Renderer>().sharedMaterial = stone;
        cube.transform.SetParent(holder, false);
        cube.transform.localScale = new Vector3(2.3f, 1.1f, 1.7f);
        cube.transform.localPosition = new Vector3(0f, 34f, 0f);
        // ★ 2026-10-12 — 콜라이더를 남기면서 <b>마구잡이 회전은 위험해졌다.</b>
        //   모서리가 위로 서면 카트를 띄우는 램프가 된다. <b>요만 돌린다.</b>
        cube.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        live.Add(new Drop { at = at, born = Time.time, mark = mark, outline = ring, block = cube.transform });
    }

    static Transform Disc(Transform parent, Material mat, float dia, float y)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(d.GetComponent<Collider>());
        d.GetComponent<Renderer>().sharedMaterial = mat;
        d.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(0f, y, 0f);
        d.transform.localScale = new Vector3(dia, 0.01f, dia);
        return d.transform;
    }

    static void Step(Drop d, int index)
    {
        if (RacePause.On) return;

        float age = Time.time - d.born;

        if (!d.landed)
        {
            float k = Mathf.Clamp01(age / Telegraph);

            // 안쪽 원이 줄어든다 — 다 줄어드는 순간이 착탄이다
            float dia = Mathf.Lerp(Radius * 2f, 0.4f, k);
            if (d.mark != null) d.mark.localScale = new Vector3(dia, 0.01f, dia);

            // 바깥 테는 깜빡인다. 점점 빨라져서 <b>리듬만 봐도</b> 남은 시간을 안다
            if (d.outline != null)
                d.outline.gameObject.SetActive(Mathf.Repeat(age * (4f + k * 12f), 1f) > 0.35f);

            if (d.block != null)
            {
                // 마지막 0.42초에 떨어진다. 처음부터 내려오면 어디 떨어질지가 안 보인다
                float fall = Mathf.Clamp01((age - (Telegraph - 0.42f)) / 0.42f);
                // ★ 끝 높이 0.25 — 슬래브 두께의 절반쯤이라 <b>바닥에 반쯤 박힌다.</b>
                //   0.6 으로 두면 떠 있고, 0 으로 두면 바닥과 같은 평면이라 지지직거린다.
                d.block.localPosition = new Vector3(0f, Mathf.Lerp(34f, 0.25f, fall * fall), 0f);
                d.block.Rotate(60f * Time.deltaTime, 95f * Time.deltaTime, 0f, Space.Self);
            }

            if (age >= Telegraph) { d.landed = true; Land(d); }
            return;
        }

        // ★★ 2026-10-06 유저: *"자재 떨어지면 사라지게 하지 말고 냅둬. 그러나 레이스에는
        //   지장 없게."* <b>맞다 — 사라지면 «내가 본 게 맞나» 가 되고, 길에 쌓이면
        //   «여기 철거가 진행 중» 이 눈에 남는다.</b> 콜라이더가 없으니 지장은 0 이다.
        //   목록에서만 빼고 <b>슬래브는 그 자리에 둔다</b>(판이 바뀌면 Clear 가 치운다).
        if (age > Telegraph + 0.9f)
        {
            if (d.block != null)
            {
                // ★ 한 번 더 앉힌다. 착지 뒤 0.9초 사이에 뭔가에 밀렸을 수 있다.
                //   <see cref="Ground"/> 가 제 몸을 빼고 재니 «제 윗면을 바닥으로 읽고
                //   제 키만큼 솟는» 사고가 안 난다 — 그게 자재가 뜨던 원인이었다.
                d.block.position = Ground.Sit(d.block, d.block.position, sink: 0.1f);

                if (landedRoot == null)
                    landedRoot = new GameObject("SkyDropLanded").transform;
                d.block.SetParent(landedRoot, true);
                landed.Add(d.block);

                // 너무 쌓이면 길이 막힌다 — 오래된 것부터 치운다.
                while (landed.Count > MaxLanded)
                {
                    if (landed[0] != null) Object.Destroy(landed[0].gameObject);
                    landed.RemoveAt(0);
                }
            }
            if (d.mark != null) Object.Destroy(d.mark.parent.gameObject);
            live.RemoveAt(index);
        }
    }

    static void Land(Drop d)
    {
        if (d.outline != null) d.outline.gameObject.SetActive(false);
        if (d.mark != null) d.mark.gameObject.SetActive(false);

        // ★★★ 2026-10-15 — <b>착지하는 «그 순간» 바닥에 앉힌다.</b>
        //
        // 전에는 0.9초 뒤 목록에서 뺄 때만 바닥을 쟀다. 그 0.9초 동안 슬래브가
        // <b>공중에 떠 있는 게 그대로 보인다</b> — 떨어지는 걸 보고 있던 사람에게는
        // 제일 눈에 띄는 순간이다(제작자: *"자재가 공중에 떠 있다"*).
        //
        // 떨어지는 높이(0.25)는 <b>표적을 놓은 자리</b> 기준인데, 코스는 평평하지 않아서
        // 표적이 도로보다 조금 높거나 낮으면 그 차이가 그대로 «뜬 것»·«박힌 것» 이 된다.
        // 실제 바닥을 재서 앉히면 <b>원인이 무엇이든</b> 안 뜬다.
        //
        // ★ 재는 기준점은 슬래브가 아니라 <b>표적 자리</b>(<c>d.at</c>)다 — 슬래브에서
        //   위로 쏘면 제 윗면을 맞고 <b>제 키만큼 솟아오른다.</b>
        // ★ <b>아래 «맞았나» 검사보다 먼저</b> 한다. 거기 <c>player == null</c> 조기 반환이
        //   있어서, 뒤에 두면 플레이어가 없는 판에서는 영영 안 앉는다.
        // ★ <see cref="Ground"/> 가 <b>제 콜라이더를 빼고</b> 재고, 올리는 양도
        //   원점이 아니라 <b>메시 맨 밑</b>에서 뽑는다 — 슬래브 원점이 가운데여도 맞는다.
        //   조금(0.1) 묻어서 앉힌다: 딱 0 이면 바닥과 같은 평면이라 지지직거린다.
        if (d.block != null) d.block.position = Ground.Sit(d.block, d.block.position, sink: 0.1f);

        if (player == null) return;

        Vector3 v = player.transform.position - d.at; v.y = 0f;
        if (v.magnitude > Radius + 1.1f) return;

        // 맞았다 — 떨어지는 콘크리트라 땅울림보다 세다. 대신 <b>표적이 1.45초 동안 보였다.</b>
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity *= 0.72f;
        player.CancelBoost();
        if (cam != null) cam.Jolt(0.6f);
        RoadDebris.CountSkyHit();   // ★ 길에 떨어진 자재와 같은 것으로 센다

        if (!told) { told = true; Toast.Show("위를 봐라 — 빨간 표적은 비켜라"); }
    }

    /// <summary>코스에서 제일 가까운 지점의 t. <see cref="ChaseVehicle"/> 와 같은 방식이다.</summary>
    static float NearestT(TrackBuilder track, Vector3 world)
    {
        Vector3 local = world - track.transform.position;
        float best = 0f, bestD = float.MaxValue;
        for (int i = 0; i < 64; i++)
        {
            float t = i / 64f;
            float dd = (track.PointOnPath(t) - local).sqrMagnitude;
            if (dd < bestD) { bestD = dd; best = t; }
        }
        return best;
    }
}
