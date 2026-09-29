using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>따라 그리기</b> — 재주관 미니게임의 <b>규칙과 판정</b>.
///
/// 화면(이젤 화판)에 곰발·기와·솥뚜껑 같은 윤곽이 흐리게 뜨고, <b>마우스로 그 위를
/// 덧그린다.</b> 급식은 「읽고 고르는」, 안전훈련은 「보고 반응하는」 게임이라
/// 여기는 <b>「손으로 맞추는」</b> 게임이다 — 셋 중 유일하게 키보드를 안 쓴다.
///
/// 이 파일에는 <b>그리기 판정만</b> 들어 있다. 화판·카메라·화면은 따로 붙는다 —
/// 판정은 3D 가 없어도 돌아가야 하고(그래야 숫자로 확인할 수 있다), 실제로
/// 여기 숫자들은 전부 <b>만들기 전에 전수로 돌려서</b> 정했다.
///
/// ━━ ★★ 게으른 손이 정답인지 (급식·안전훈련과 같은 절차) ━━━━━━━━━━━━━━━━━━━━━━
///
/// 그리기 게임에서 제일 게으른 짓은 <b>「마구 칠하기」</b>다. 칠을 촘촘히 하면 윤곽이
/// 전부 덮이니까, <b>덮음만 보면 낙서가 만점</b>이 된다. 그래서 점수를 둘의 <b>곱</b>으로 둔다:
///
/// <code>점수 = 100 × 덮음 × 붙음</code>
///
/// - <b>덮음</b>: 목표 윤곽의 점들 중 <b>내가 그린 선이 지나간</b> 비율 → 다 그렸나
/// - <b>붙음</b>: 내가 찍은 점들 중 <b>윤곽에 붙어 있는</b> 비율 → 딴 데 안 칠했나
///
/// 마구 칠하면 덮음은 1에 가까워도 <b>붙음이 0.13</b> 이라 13점이다. 한 점만 찍으면
/// 붙음은 높아도 덮음이 0.05 라 5점이다. <b>둘 다 해야 점수가 난다.</b>
///
/// 도형 4개 × 60회로 돌린 결과(창 0.045):
///
/// | 손 | 점수 | 덮음 | 붙음 |
/// |---|---|---|---|
/// | 성실하게 덧그림 | 100 | 1.00 | 1.00 |
/// | 보통 | 93 | 1.00 | 0.93 |
/// | 대충(삐뚤 0.045 · 15% 빼먹음) | <b>72</b> | 1.00 | 0.72 |
/// | 앞쪽 3/4 만 | 83 | 0.83 | 1.00 |
/// | 앞쪽 절반만 | 57 | 0.58 | 1.00 |
/// | 빽빽하게 칠하기 | <b>13</b> | 1.00 | 0.13 |
/// | 지그재그로 훑기 | 12 | 0.90 | 0.14 |
/// | 마구 칠하기 | 11 | 0.81 | 0.13 |
/// | 큰 동그라미 하나 | 4 | 0.22 | 0.16 |
/// | 한 점만 찍기 | 5 | 0.05 | 0.50 |
///
/// <b>게으른 손 최대 13 · 대충이라도 그린 손 72.</b> 그리고 «반만 그리면 반쯤 점수» 로
/// <b>노력에 비례</b>한다 — 이게 없으면 «어디까지 해야 하나» 를 플레이어가 모른다.
///
/// > 처음 돌렸을 때 «절반만 그린다» 가 99점이 나와서 한 번 걸렸다. 알고 보니 시뮬레이션이
/// > <b>점을 띄엄띄엄 빼먹은</b> 것이지 «한쪽만 그린 것» 이 아니었다 — 듬성듬성해도 이웃 점이
/// > 창 안에 들어와서 덮음이 1이 된다. <b>«대충 한다» 를 코드로 흉내 낼 때는 그게 진짜 그
/// > 행동인지부터 봐야 한다.</b>
/// </summary>
public static class Tracing
{
    /// <summary>한 판에 그리는 도형 수.</summary>
    public const int Rounds = 4;

    /// <summary>도형 하나에 주는 시간(초). 다 그리면 바로 넘어간다.</summary>
    public const float PerShape = 18f;

    /// <summary>
    /// 「선 위」로 쳐 주는 거리. 화판 한 변을 <b>1</b> 로 본 값이다.
    ///
    /// 0.045 는 <b>손이 떨리는 폭</b>에서 나왔다 — 마우스로 곡선을 덧그릴 때 사람 손은
    /// 0.02~0.03 쯤 흔들린다. 그보다 좁으면 «성실하게 그렸는데 점수가 안 나오는» 게임이 되고,
    /// 넓히면 낙서의 붙음이 올라가서 <b>마구 칠하기가 이기기 시작한다.</b>
    /// </summary>
    public const float Tolerance = 0.045f;

    /// <summary>덮음을 셀 때 윤곽을 이만큼 잘게 찍는다.</summary>
    const int Marks = 220;

    /// <summary>등급 자르는 점수. <see cref="MinigameFlow.Grade"/> 가 쓴다.</summary>
    public static readonly int[] Grades = { 340, 260, 170 };

    // ── 도형 ──────────────────────────────────────────────────────────────────
    //
    // ★ <b>이 세계에 있는 것만 그린다.</b> 별·하트를 그리게 하면 그건 아무 게임에나 붙는
    // 미니게임이 되고, 재주관에 있을 이유가 없어진다. 곰발·기와·솥뚜껑·음표는
    // 전부 이 캠퍼스에 실제로 있는 모양이다.
    //
    // 좌표는 화판을 <b>0~1 정사각형</b>으로 본 값이다. 화판이 커지거나 비율이 달라져도
    // 도형은 안 고친다 — 그리는 쪽에서 맞춰 늘린다.

    public readonly struct Shape
    {
        public readonly string name;
        public readonly Vector2[] path;
        public readonly bool closed;

        public Shape(string name, bool closed, params float[] xy)
        {
            this.name = name;
            this.closed = closed;
            int n = xy.Length / 2;
            path = new Vector2[closed ? n + 1 : n];
            for (int i = 0; i < n; i++) path[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            if (closed) path[n] = path[0];
        }
    }

    public static readonly Shape[] Shapes =
    {
        new Shape("곰발", true,
            .50f,.16f, .62f,.22f, .68f,.34f, .68f,.50f, .60f,.62f,
            .50f,.66f, .40f,.62f, .32f,.50f, .32f,.34f, .38f,.22f),

        new Shape("기와", true,
            .12f,.62f, .20f,.44f, .34f,.34f, .50f,.30f, .66f,.34f, .80f,.44f,
            .88f,.62f, .74f,.58f, .62f,.52f, .50f,.50f, .38f,.52f, .26f,.58f),

        new Shape("솥뚜껑", true,
            .18f,.60f, .26f,.44f, .40f,.34f, .50f,.32f, .60f,.34f,
            .74f,.44f, .82f,.60f, .50f,.66f),

        new Shape("음표", false,
            .36f,.72f, .34f,.64f, .40f,.60f, .48f,.62f, .50f,.70f,
            .50f,.26f, .70f,.20f, .70f,.30f, .50f,.36f),
    };

    // ── 판정 ──────────────────────────────────────────────────────────────────

    /// <summary>한 도형의 채점 결과.</summary>
    public readonly struct Mark
    {
        public readonly int score;       // 0~100
        public readonly float coverage;  // 다 그렸나
        public readonly float accuracy;  // 딴 데 안 칠했나

        public Mark(int score, float coverage, float accuracy)
        {
            this.score = score; this.coverage = coverage; this.accuracy = accuracy;
        }

        /// <summary>결과 화면에 뜨는 한 줄. <b>숫자만 주면 왜 그 점수인지 모른다.</b></summary>
        public string Verdict =>
            score >= 90 ? "그대로 그렸다"
          : score >= 70 ? "거의 맞다"
          : coverage < 0.55f ? "덜 그렸다"
          : accuracy < 0.45f ? "선에서 많이 벗어났다"
          : "더 붙여서";
    }

    /// <summary>
    /// 점수. <paramref name="drawn"/> 은 화판 0~1 좌표로 찍힌 <b>내가 그린 점들</b>이다.
    /// </summary>
    public static Mark Judge(in Shape shape, IReadOnlyList<Vector2> drawn)
    {
        if (drawn == null || drawn.Count < 2) return new Mark(0, 0f, 0f);

        // 덮음 — 윤곽 위 점 하나하나에 «내가 지나갔나» 를 묻는다
        int covered = 0;
        for (int i = 0; i < Marks; i++)
        {
            Vector2 m = At(shape, (float)i / (Marks - 1));
            for (int j = 0; j < drawn.Count; j++)
            {
                if ((drawn[j] - m).sqrMagnitude > Tolerance * Tolerance) continue;
                covered++;
                break;
            }
        }
        float coverage = covered / (float)Marks;

        // 붙음 — 내가 찍은 점 하나하나에 «윤곽 위인가» 를 묻는다
        int on = 0;
        for (int j = 0; j < drawn.Count; j++)
            if (Near(shape, drawn[j]) <= Tolerance) on++;
        float accuracy = on / (float)drawn.Count;

        return new Mark(Mathf.RoundToInt(100f * coverage * accuracy), coverage, accuracy);
    }

    /// <summary>윤곽을 길이 기준 <paramref name="t"/>(0~1) 만큼 간 자리.</summary>
    public static Vector2 At(in Shape shape, float t)
    {
        var p = shape.path;
        float total = 0f;
        for (int i = 0; i < p.Length - 1; i++) total += (p[i + 1] - p[i]).magnitude;

        float want = Mathf.Clamp01(t) * total, acc = 0f;
        for (int i = 0; i < p.Length - 1; i++)
        {
            float len = (p[i + 1] - p[i]).magnitude;
            if (acc + len >= want || i == p.Length - 2)
                return Vector2.Lerp(p[i], p[i + 1], len <= 0f ? 0f : (want - acc) / len);
            acc += len;
        }
        return p[p.Length - 1];
    }

    /// <summary>점에서 윤곽까지의 최단 거리. <b>꼭짓점이 아니라 «선분» 까지</b> 재야 한다 —
    /// 꼭짓점까지만 재면 긴 변의 한가운데를 정확히 그려도 멀다고 나온다.</summary>
    public static float Near(in Shape shape, Vector2 at)
    {
        var p = shape.path;
        float best = float.MaxValue;
        for (int i = 0; i < p.Length - 1; i++)
        {
            Vector2 a = p[i], d = p[i + 1] - a;
            float l2 = d.sqrMagnitude;
            float t = l2 <= 0f ? 0f : Mathf.Clamp01(Vector2.Dot(at - a, d) / l2);
            best = Mathf.Min(best, (at - (a + d * t)).magnitude);
        }
        return best;
    }

    /// <summary>
    /// 그린 점을 받을 때 <b>이만큼 움직여야 한 점으로 친다.</b>
    ///
    /// 마우스는 한 프레임에 0.001 씩도 움직이는데 그걸 다 담으면, <b>천천히 그린 사람일수록
    /// 점이 많아져서</b> 같은 그림인데 점수가 달라진다. 간격을 두면 «그린 길이» 가 기준이 된다.
    /// </summary>
    public const float StepMin = 0.008f;

    /// <summary>한 도형에 담는 점의 상한. 넘으면 오래된 것부터 버린다 — 60초 × 60프레임을
    /// 다 담으면 판정 한 번에 3,600 × 220 번을 재게 된다.</summary>
    public const int MaxPoints = 900;
}
