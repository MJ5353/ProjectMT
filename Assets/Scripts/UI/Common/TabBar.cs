using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 하단 탭바. 버튼과 화면을 짝지어 두고 누른 탭의 화면만 켠다. 바뀌면 TabChanged를 발행한다
public class TabBar : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [SerializeField] private Button m_Button = null;
        [SerializeField] private TabView m_View = null;
        [SerializeField, Tooltip("선택 표시(없어도 됨)")] private GameObject m_Highlight = null;

        public Button Button => m_Button;
        public TabView View => m_View;
        public GameObject Highlight => m_Highlight;

        public Entry(Button button, TabView view, GameObject highlight)
        {
            m_Button = button;
            m_View = view;
            m_Highlight = highlight;
        }
    }

    [SerializeField] private List<Entry> m_ListTab = new();
    [SerializeField] private int m_DefaultIndex = 0;

    public int CurrentIndex { get; private set; } = -1;

    // ==================================================

    private void Awake()
    {
        for (int i = 0; i < m_ListTab.Count; i++)
        {
            int index = i;
            Entry entry = m_ListTab[i];

            entry.Button.onClick.AddListener(() => Select(index));
            entry.View.gameObject.SetActive(false);
            SetHighlight(entry, false);
        }
    }

    private void Start()
    {
        Select(m_DefaultIndex);
    }

    public void Select(int index)
    {
        if (index == CurrentIndex || index < 0 || index >= m_ListTab.Count)
            return;

        int from = CurrentIndex;
        if (from >= 0)
        {
            m_ListTab[from].View.SetVisible(false);
            SetHighlight(m_ListTab[from], false);
        }

        CurrentIndex = index;
        m_ListTab[index].View.SetVisible(true);
        SetHighlight(m_ListTab[index], true);

        SEventManager.Publish(new TabChanged(from, index));
    }

    private void SetHighlight(Entry entry, bool isOn)
    {
        if (entry.Highlight == null)
            return;

        entry.Highlight.SetActive(isOn);
    }

#if UNITY_EDITOR
    public void EditorSetTabs(List<Entry> listEntry) => m_ListTab = listEntry;
#endif
}
