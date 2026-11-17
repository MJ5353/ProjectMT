using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MushTaker.UI
{
    /// <summary>
    /// 씬마다 하나 두는 팝업 스택. 마지막에 연 팝업이 맨 위에 오고, 뒤로 가기는 맨 위부터 닫는다.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [SerializeField] RectTransform root;

        readonly List<Popup> _stack = new();

        public int Count => _stack.Count;

        void Awake()
        {
            Instance = this;
            if (root == null) root = (RectTransform)transform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public T Open<T>(T prefab) where T : Popup
        {
            var popup = Instantiate(prefab, root);
            _stack.Add(popup);
            popup.OnOpen();
            return popup;
        }

        public void Close(Popup popup)
        {
            if (!_stack.Remove(popup)) return;
            popup.OnClose();
            Destroy(popup.gameObject);
        }

        public bool CloseTop()
        {
            if (_stack.Count == 0) return false;
            var top = _stack[^1];
            if (!top.CloseOnBack) return false;
            Close(top);
            return true;
        }

        void Update()
        {
            if (BackPressed()) CloseTop();
        }

        static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            // 안드로이드 뒤로 버튼도 Input System에서는 Escape로 들어온다.
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

#if UNITY_EDITOR
        public void EditorSetRoot(RectTransform r) => root = r;
#endif
    }
}
