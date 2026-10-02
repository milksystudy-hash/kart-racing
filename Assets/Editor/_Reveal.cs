using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>획득 카드가 종이 안에 들어가는지 <b>실제 글자로</b> 잰다. 배치모드 전용.</summary>
public static class _Reveal
{
    public static void Run()
    {
        var sb = new StringBuilder("[획득 연출 점검]\n");

        // Hud 스타일은 GUI.skin 을 안 쓰고 new GUIStyle() 로 짓는다 → OnGUI 밖에서도 잰다
        typeof(Hud).GetMethod("Build", System.Reflection.BindingFlags.NonPublic
                                     | System.Reflection.BindingFlags.Static)?.Invoke(null, null);

        var body = new GUIStyle { fontSize = 13, wordWrap = true, alignment = TextAnchor.UpperCenter };
        var title = new GUIStyle { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        var slot = new GUIStyle { fontSize = 15, alignment = TextAnchor.MiddleCenter };

        const float pw = 520f;
        float innerW = pw - 14f;            // Hud.Inner
        float innerH = 206f - 16f;
        float bodyW = innerW - 32f;

        sb.AppendLine($"  판 {pw} × 206  ·  종이 {innerW} × {innerH}");
        sb.AppendLine($"  칸: 머리말 6~28 · 이름 30~66 · 줄 72 · 설명 80~138 · 자리 144~166 · 안내 170~188");

        float worst = 0f;
        string worstName = "";
        foreach (var e in ExhibitCatalogue.All)
        {
            float hBody = body.CalcHeight(new GUIContent(e.description), bodyW);
            float hTitle = title.CalcSize(new GUIContent(e.name)).x;
            string slotText = $"전시실 9번 · 수집품 8 / 8";
            float wSlot = slot.CalcSize(new GUIContent(slotText)).x;

            bool over = hBody > 58f;
            if (hBody > worst) { worst = hBody; worstName = e.name; }

            sb.AppendLine($"  {e.name,-12} 설명 {hBody,5:0}px{(over ? "  ★넘침" : "")}"
                        + $"  이름폭 {hTitle,4:0}  자리폭 {wSlot,4:0}");
        }

        sb.AppendLine($"  제일 긴 설명: {worstName} {worst:0}px  (칸 58px)");
        sb.AppendLine(worst <= 58f ? "  ── 문제 0건" : "  ── ★ 설명 칸을 늘려야 한다");
        Debug.Log(sb.ToString());
    }
}
