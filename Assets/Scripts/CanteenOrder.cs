using UnityEngine;

/// <summary>배식대 다섯 칸. 키 <c>1</c>~<c>5</c> 가 그대로 이 순서다.</summary>
public enum Dish { 밥 = 0, 국 = 1, 김치 = 2, 반찬 = 3, 후식 = 4 }

/// <summary>
/// <b>손님 하나의 주문표.</b> 뭘 담아야 하는지와, 담은 결과가 몇 점인지.
///
/// 담을 것은 <b>비트마스크 다섯 자리</b>다. 순서를 안 따지기 때문에 집합이면 충분하고,
/// 집합이라서 «빠뜨린 것」과 «더 넣은 것」을 XOR 한 번으로 같이 셀 수 있다.
///
/// <b>순서를 안 따지는 이유</b>(기획서 §3): 주문표가 화면에 떠 있는데 순서까지 맞추라고 하면
/// 그건 기억력 시험이 된다. 뭘 담을지만 맞으면 된다.
/// </summary>
public readonly struct CanteenOrder
{
    public const int Slots = 5;

    /// <summary>화면에 그대로 뜨는 이름. 인덱스가 <see cref="Dish"/> 이자 누를 숫자키다.</summary>
    public static readonly string[] Names = { "밥", "국", "김치", "반찬", "후식" };

    /// <summary>이 안에 내보내면 보너스.</summary>
    public const float FastSeconds = 3f;
    public const int FastBonus = 3;

    /// <summary>담아야 할 것. 비트 i 가 <see cref="Dish"/> i.</summary>
    public readonly int Wanted;

    /// <summary><b>줄 맨 앞에 선 시각.</b> 만들어진 시각이 아니다 — 아래 설명 참고.</summary>
    public readonly float ArrivedAt;

    public CanteenOrder(int wanted, float arrivedAt)
    {
        Wanted = wanted;
        ArrivedAt = arrivedAt;
    }

    public int Size => Count(Wanted);

    public bool Needs(Dish d) => (Wanted & (1 << (int)d)) != 0;

    public static int Count(int mask)
    {
        int n = 0;
        for (int i = 0; i < Slots; i++)
            if ((mask & (1 << i)) != 0) n++;
        return n;
    }

    /// <summary>"밥 · 국 · 김치". 가운뎃점은 이 프로젝트가 줄바꿈에도 쓰는 구분자다.</summary>
    public string Label
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Slots; i++)
            {
                if ((Wanted & (1 << i)) == 0) continue;
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append(Names[i]);
            }
            return sb.ToString();
        }
    }

    /// <summary>다섯 중 <paramref name="size"/> 개를 겹치지 않게 뽑는다.</summary>
    public static CanteenOrder Roll(int size, float now)
    {
        size = Mathf.Clamp(size, 1, Slots);

        // 0~4 를 섞어서 앞에서 size 개. 뽑고 또 뽑으면서 «이미 있나» 를 확인하는 방식은
        // size 가 5 에 가까울 때 헛돌이가 길어진다.
        var pool = new int[Slots];
        for (int i = 0; i < Slots; i++) pool[i] = i;
        for (int i = Slots - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int mask = 0;
        for (int i = 0; i < size; i++) mask |= 1 << pool[i];
        return new CanteenOrder(mask, now);
    }

    /// <summary>빠뜨린 것 + 더 넣은 것. 둘을 따로 셀 이유가 없어서 XOR 로 한 번에 센다.</summary>
    public static int Mistakes(int wanted, int served) => Count(wanted ^ served);

    /// <summary>
    /// 점수. <b>감점은 없다</b>(음수가 없다) — 레이싱에서 이미 여덟 번 실패했으니
    /// 여기는 쉬어가는 자리여야 한다(기획서 §3). 하지만 <b>«아무거나 줘도 점수» 는 아니다.</b>
    ///
    /// ★★ 2026-09-22 유저가 구멍 둘을 짚었다:
    /// *"메뉴 아무렇게나 줘도 점수가 올라"* · *"아무것도 선택 안 하고 ENTER 만 눌러도 점수가 올라."*
    /// 맞다 — 전에는 «어긋남» 에도 1점을 줬다. 그러면 <b>빈 식판으로 ENTER 를 연타하는 것</b>이
    /// 초당 몇 점씩 버는 최적 전략이 되고, 주문표를 읽을 이유가 통째로 사라진다.
    ///
    /// <b>실패가 없는 게임에서는 «제일 게으른 행동」이 정답인지 반드시 계산해야 한다</b> —
    /// 빠른 보너스를 «딱 맞았을 때만» 으로 묶을 때 이미 배운 것인데 한 자리를 빠뜨렸다.
    /// </summary>
    public static int Points(int wanted, int served, float held, out string verdict)
    {
        // <b>빈 식판은 «내보낸 것」이 아니다.</b> 0점이고, 손님도 빈손으로 나간다.
        if (served == 0)
        {
            verdict = "빈 식판";
            return 0;
        }

        int diff = Mistakes(wanted, served);

        if (diff == 0)
        {
            // ★ 빠른 보너스는 «정확히 맞았을 때만» 준다.
            // 아무것도 안 담고 ENTER 를 연타하면 어긋남 1점 + 빠름 3점 = 4점이 0.5초에 들어온다.
            // 그건 차분히 하나 틀린 4점과 같아져서 <b>연타가 더 이득</b>이 된다 —
            // 감점이 없는 게임에서는 보너스를 아무 데나 주면 그게 곧 구멍이야.
            if (held <= FastSeconds)
            {
                verdict = "딱 맞음 · 빠름";
                return 10 + FastBonus;
            }
            verdict = "딱 맞음";
            return 10;
        }

        if (diff == 1)
        {
            verdict = "하나 어긋남";
            return 4;
        }

        // 둘 이상 어긋나면 <b>0점</b>. 감점이 아니라 «못 받은 것」이다 —
        // 여기에 1점이라도 붙는 순간 아무거나 담고 내보내는 게 이득이 된다.
        verdict = "어긋남";
        return 0;
    }
}
