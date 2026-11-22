using UnityEngine;

// ==================================================

// 타이틀 씬. 공용 Service만 쓰고, 화면 아무 데나 누르면(배경 버튼) 버섯이 튀며 로비로 간다(ScreenTitle)

[DefaultExecutionOrder(-1100)]
public sealed class TitleSceneController : SceneControllerBase
{
    public override EGameState State => EGameState.Title;
}
