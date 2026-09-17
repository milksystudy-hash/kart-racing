using UnityEngine;

/// <summary>
/// 건물 현판. <b>이름과 학과를 들고 있고, 이야기에 따라 딱지가 붙는다.</b>
///
/// 2026-09-17 에 설정이 하나 붙었다: 박물관은 <b>폐교한 곰 전문대 부지</b>에 들어섰고
/// 간판만 그대로 남았다. 그래서 철거는 이 캠퍼스의 <b>두 번째 죽음</b>이야.
/// 기획서의 박물관 설정을 하나도 안 건드리면서 한옥 캠퍼스에 학과 건물이 왜 있는지가 설명된다.
///
/// 연출이 여기 걸린다. <b>기하를 하나도 안 건드리고</b> 캠퍼스 전체가 이야기에 반응한다 —
/// 현판에 빨간 딱지가 붙었다 떨어지는 것만으로 "여기 없어질 곳이구나" 가 읽힌다.
/// 건물을 새로 짓거나 부수는 것보다 백 배 싸고, 플레이어는 지나가면서 본다.
/// </summary>
public class BuildingSign : MonoBehaviour
{
    [Tooltip("현판에 적히는 이름")]
    public string buildingName = "";

    [Tooltip("무슨 학과였는지. 현판 아래 작게")]
    public string department = "";

    [Tooltip("이 건물 이름 아래 붙는 한 줄. 비워도 된다")]
    public string motto = "";

    [Tooltip("켜면 현판에 빨간 [폐과] 딱지가 붙는다")]
    public bool closed;

    Transform sticker;

    void Start() => Apply();

    /// <summary>딱지를 붙이거나 뗀다. <see cref="CampusMood"/> 가 이야기 진행에 맞춰 부른다.</summary>
    public void SetClosed(bool value)
    {
        if (closed == value && sticker != null) return;
        closed = value;
        Apply();
    }

    void Apply()
    {
        if (sticker == null) sticker = MakeSticker();
        if (sticker != null) sticker.gameObject.SetActive(closed);
    }

    /// <summary>
    /// 현판을 가로지르는 빨간 띠. <b>글씨를 지우는 게 아니라 위에 덧붙인다</b> —
    /// 이름이 남아 있어야 "원래 뭐였는지" 가 보이고, 그래야 없어진다는 게 아프다.
    /// </summary>
    Transform MakeSticker()
    {
        var plaque = transform;
        var scale = plaque.localScale;
        if (scale.x <= 0.001f) return null;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ClosedSticker";
        go.transform.SetParent(plaque, false);

        // 부모(현판)가 눌린 상자라 자식도 같이 눌린다. 나누어 되돌린다.
        go.transform.localScale = new Vector3(1.06f / 1f, 0.42f, 0.9f);
        go.transform.localPosition = new Vector3(0f, -0.1f, -0.55f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);   // 삐뚤게 — 손으로 붙인 티

        go.GetComponent<Renderer>().sharedMaterial = FlatMaterial.Get(new Color32(0xC4, 0x3A, 0x30, 0xFF));

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }
        return go.transform;
    }
}
