using UnityEngine;

// 모든 콘텐츠 정의(버섯, 재화, 몬스터, 레시피 등)의 기반.
// 새 콘텐츠 종류는 이 클래스를 상속한 ScriptableObject로 만들고, 에셋은 Assets/Data/ 아래에 둔다.
// !!) ID는 세이브와 표에서 쓰는 고유 키라서 한 번 정하면 바꾸지 않는다
public abstract class DataDef : ScriptableObject
{
    [SerializeField] private string m_ID = null;
    [SerializeField] private string m_DisplayName = null;
    [SerializeField, TextArea] private string m_Description = null;
    [SerializeField] private Sprite m_Icon = null;

    public string ID => m_ID;
    public string DisplayName => m_DisplayName.E_IsNullOrEmpty() ? m_ID : m_DisplayName;
    public string Description => m_Description;
    public Sprite Icon => m_Icon;

#if UNITY_EDITOR
    // 비어 있으면 에셋 이름을 ID로 쓴다. 이후에는 자동으로 바꾸지 않는다
    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(m_ID))
            m_ID = name;
    }
#endif
}
