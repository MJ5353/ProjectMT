using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 모든 콘텐츠 정의(버섯, 재화, 몬스터, 레시피 등)의 기반.
// 새 콘텐츠 종류는 이 클래스를 상속한 ScriptableObject로 만들고, 에셋은 Assets/Data/ 아래에 둔다.
// !!) ID는 세이브와 표에서 쓰는 고유 키라서 한 번 정하면 바꾸지 않는다

public abstract class DataDef : ScriptableObject
{
    public string DisplayName => m_DisplayName.E_IsNullOrEmpty() ? ID : m_DisplayName;
    [SerializeField] private string m_DisplayName = null;

    [field: SerializeField, FormerlySerializedAs("m_ID")]
    public string ID { get; private set; } = null;

    [field: SerializeField, TextArea, FormerlySerializedAs("m_Description")]
    public string Description { get; private set; } = null;

    [field: SerializeField, FormerlySerializedAs("m_Icon")]
    public Sprite Icon { get; private set; } = null;

    // ------------------------------------------------

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // 비어 있으면 에셋 이름을 ID로 쓴다. 이후에는 자동으로 바꾸지 않는다
        if (string.IsNullOrWhiteSpace(ID))
            ID = name;
    }

    public void EditorSetIdentity(string dataID, string displayName, string description = null)
    {
        ID = dataID;
        m_DisplayName = displayName;
        Description = description;
    }
#endif
}
