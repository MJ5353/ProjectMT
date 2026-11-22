using UnityEngine;

// ==================================================

// 씬 종류. GameManager.ChangeScene(ESceneType)으로 넘어간다. 이름은 씬 파일 이름과 같게 둔다(에디터 셋업이 nameof로 파일을 만든다)
public enum ESceneType
{
    None,
    Title,
    Lobby,
    Battle,
}

// ==================================================

// 씬 하나의 정의. 씬마다 에셋 하나(Resources/Data/Scenes/Scene_{종류}). GameManager가 종류로 찾아 씬 이름을 읽는다.
// !!) 씬 이름은 Build Settings에 넣은 씬 이름과 같아야 로드된다. 새 에셋을 만든 뒤에는 데이터 편집기의 DB 갱신을 실행한다

[CreateAssetMenu(menuName = "MushTaker/Data/Scene", fileName = "Scene_")]
public class SceneDef : DataDef
{
    [field: SerializeField]
    public ESceneType Type { get; private set; } = ESceneType.None;

    [field: SerializeField, Tooltip("Build Settings의 씬 이름(확장자 없이)")]
    public string SceneName { get; private set; } = null;
}
