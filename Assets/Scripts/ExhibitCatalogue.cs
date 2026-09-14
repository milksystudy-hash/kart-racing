/// <summary>
/// 전시품 목록 한 곳. 전시실 진열장도, 트랙에 놓는 수집품도 여기서 읽는다.
///
/// 두 군데에 따로 적어두면 이름이나 순서가 어긋나기 시작한다 — 트랙에서 주운 물건이
/// 전시실에 안 들어가는 식으로. 그래서 목록은 여기 하나뿐이다.
///
/// 전부 기획서의 줄거리에서 나오는 물건들이야.
/// </summary>
public static class ExhibitCatalogue
{
    /// <summary>임시 자리표시의 생김새. 진짜 모델이 오면 의미 없어진다.</summary>
    public enum Shape { 원반, 종이, 상자 }

    public struct Entry
    {
        public string id;
        public string name;
        public string chapter;
        public string description;
        public Shape shape;
    }

    public static readonly Entry[] All =
    {
        new Entry { id = "coin", name = "기념 코인", chapter = "제1장 · 사라진 관람객", shape = Shape.원반,
            description = "박물관 입장 때 나눠주던 코인. 뒷면에 관람 일자가 찍혀 있어서, 모으면 그날 누가 다녀갔는지가 드러난다." },
        new Entry { id = "ledger", name = "관람 기록부", chapter = "제1장 · 사라진 관람객", shape = Shape.종이,
            description = "코인에서 복원한 방문객 명단. 시에서 발표한 관람객 수보다 훨씬 많은 이름이 적혀 있었다." },
        new Entry { id = "survey", name = "안전진단서 원본", chapter = "제2장 · 조작된 안전진단", shape = Shape.종이,
            description = "원본의 결론은 '보수 필요' 였다. 공개된 사본에는 '즉시 철거' 로 바뀌어 있었다." },
        new Entry { id = "marker", name = "붉은 철거 표식", chapter = "제2장 · 조작된 안전진단", shape = Shape.상자,
            description = "개발업자 측이 트랙에 세워 둔 표식. 부딪혀 뜯어보니 안쪽에 서류 조각이 접혀 있었다." },
        new Entry { id = "signature", name = "관장의 서명", chapter = "제3장 · 관장의 서명", shape = Shape.종이,
            description = "조건부 매각 문서에 남은 서명. 비리를 계획하지는 않았지만, 사실을 숨긴 대가가 여기 남았다." },
        new Entry { id = "contract", name = "비밀 계약서", chapter = "제3장 · 관장의 서명", shape = Shape.종이,
            description = "시의원과 개발업자 사이의 이면 계약. 선거 지원과 이권이 항목으로 적혀 있다." },
        new Entry { id = "recorder", name = "중계 기록 장치", chapter = "마지막 장 · 철거 전야", shape = Shape.상자,
            description = "어두워진 트랙을 가로질러 결승선까지 옮긴 장치. 이것으로 전말이 시 전역에 생중계됐다." },
        new Entry { id = "blueprint", name = "골든베어 조감도", chapter = "프롤로그 · 철거 통지서", shape = Shape.종이,
            description = "박물관 자리에 세우려던 리조트 조감도. 없애지 않고 전시실에 남겨 두기로 했다." },
    };

    public static int Count => All.Length;

    /// <summary>진열장 번호(1부터). 화면에 "전시실 3번" 처럼 보여줄 때 쓴다.</summary>
    public static int CaseNumberOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return i + 1;
        return 0;
    }

    public static string NameOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].id == id) return All[i].name;
        return id;
    }
}
