using UnityEngine;

namespace MushTaker.Data
{
    /// <summary>
    /// 모든 콘텐츠 정의(버섯, 재화, 몬스터, 레시피 등)의 기반.
    /// 새 콘텐츠 종류는 이 클래스를 상속한 ScriptableObject로 만들고, 에셋은 Assets/Data/ 아래에 둔다.
    /// id는 세이브와 표에서 쓰는 고유 키라서 한 번 정하면 바꾸지 않는다.
    /// </summary>
    public abstract class DataDef : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField, TextArea] string description;
        [SerializeField] Sprite icon;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? id : displayName;
        public string Description => description;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        // 비어 있으면 에셋 이름을 id로 쓴다. 이후에는 자동으로 바꾸지 않는다.
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
#endif
    }
}
