/// <summary>
/// ★★ 개발용 키를 한 군데서 끈다.
///
/// 2026-10-06 유저: *"제출할 때 아무도 못 보고 플레이어도 못하게 사라지게 못하나."*
///
/// 전에는 컴포넌트마다 <c>debugKeys</c> 를 <b>인스펙터에서 하나씩 꺼야</b> 했다.
/// 그 방식이 위험한 이유는 둘이다:
/// <list type="number">
/// <item><b>끌 것이 여덟 개</b>다(TestHUD · StoryStage · DialogueHUD · HingedDoor ·
///       CampusVictory · ItemReveal · RaceBriefing · SceneNavigator · GalleryHUD).
///       제출 직전에 여덟 군데를 손으로 끄는 일은 <b>반드시 하나를 빠뜨린다.</b></item>
/// <item><b>인스펙터 값은 씬에 구워진다.</b> 코드에서 기본값을 false 로 바꿔도
///       이미 저장된 씬에는 true 가 박혀 있다 — 이 프로젝트에서 이미 겪은 함정이야
///       (<c>DeskClock.range</c>, 2026-09-22).</item>
/// </list>
///
/// 그래서 <b>컴파일 시점에</b> 막는다. 빌드에서는 <c>Enabled</c> 가 상수 false 라
/// 컴파일러가 그 분기를 통째로 지운다 — 씬에 뭐가 저장돼 있든 <b>개발용 키는 없다.</b>
///
/// 에디터에서는 그대로 다 쓸 수 있다. <b>제출 전에 끌 것이 이제 없다.</b>
/// </summary>
public static class Dev
{
#if UNITY_EDITOR
    public const bool Enabled = true;
#else
    public const bool Enabled = false;
#endif
}
