using System;
using System.Collections.Generic;
using MushTaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MushTaker.UI
{
    /// <summary>
    /// 하단 탭바. 버튼과 화면을 짝지어 두고 누른 탭의 화면만 켠다. 바뀌면 TabChanged를 발행한다.
    /// </summary>
    public class TabBar : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public Button button;
            public TabView view;
            public GameObject highlight;  // 선택 표시(없어도 됨)
        }

        [SerializeField] List<Entry> tabs = new();
        [SerializeField] int defaultIndex;

        public int CurrentIndex { get; private set; } = -1;

        void Awake()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                tabs[i].button.onClick.AddListener(() => Select(index));
                tabs[i].view.gameObject.SetActive(false);
                if (tabs[i].highlight) tabs[i].highlight.SetActive(false);
            }
        }

        void Start() => Select(defaultIndex);

        public void Select(int index)
        {
            if (index == CurrentIndex || index < 0 || index >= tabs.Count) return;

            int from = CurrentIndex;
            if (from >= 0)
            {
                tabs[from].view.SetVisible(false);
                if (tabs[from].highlight) tabs[from].highlight.SetActive(false);
            }

            CurrentIndex = index;
            tabs[index].view.SetVisible(true);
            if (tabs[index].highlight) tabs[index].highlight.SetActive(true);

            EventBus.Publish(new TabChanged(from, index));
        }

#if UNITY_EDITOR
        public void EditorSetTabs(List<Entry> entries) => tabs = entries;
#endif
    }
}
