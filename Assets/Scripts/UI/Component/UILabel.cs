using TMPro;
using UnityEngine;

// ==================================================

// 고정 문구 라벨. 키(EStringKey 이름)를 넣어 두면 켜질 때 StringDef 문구로 채운다. 숫자가 들어가는 문구는 화면 코드가 StringDef.S_Format으로 직접 쓴다.
// !!) SceneController가 있는 씬 전제. 씬에 놓인 라벨도 SceneController(-1100) Awake 뒤에 켜지므로 DB가 준비돼 있다
// 에디터에서는 키를 고르면 문구를 미리 보여 준다

public class UILabel : MonoBehaviour
{
    [SerializeField] private TMP_Text m_Text = null;
    [SerializeField, StringKey] private string m_Key = null;

    private object[] m_Args; // SetFormat 인자. 없으면 문구 그대로

    // ------------------------------------------------

    private void OnEnable()
    {
        Apply();
    }

    public void SetKey(EStringKey key)
    {
        m_Key = StringDef.S_GetName(key);
        m_Args = null;
        Apply();
    }

    public void SetFormat(EStringKey key, params object[] args)
    {
        m_Key = StringDef.S_GetName(key);
        m_Args = args;
        Apply();
    }

    private void Apply()
    {
        if (m_Key.E_IsNullOrEmpty())
            return;

        string value = StringDef.S_Get(m_Key);
        m_Text.E_SetText(m_Args.E_IsNullOrEmpty() ? value : string.Format(value, m_Args));
    }

#if UNITY_EDITOR
    // 키를 바꾸면 에디터에서 바로 문구를 보여 준다. OnValidate 안에서 글자를 바꾸면 경고가 나서 한 틱 미룬다
    private void OnValidate()
    {
        if (Application.isPlaying || m_Text == null || m_Key.E_IsNullOrEmpty())
            return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || m_Text == null)
                return;

            string value = StringDef.EditorGet(m_Key);
            if (m_Text.text == value)
                return;

            m_Text.text = value;
            UnityEditor.EditorUtility.SetDirty(m_Text);
        };
    }

    public void EditorSetup(TMP_Text text, EStringKey key)
    {
        m_Text = text;
        m_Key = StringDef.S_GetName(key);
    }
#endif
}
