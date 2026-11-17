using UnityEngine;

namespace MushTaker.UI
{
    /// <summary>
    /// 하단 탭 하나가 보여 주는 화면. 탭 화면 스크립트는 이 클래스를 상속해 OnOpen/OnClose를 채운다.
    /// </summary>
    public class TabView : MonoBehaviour
    {
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            gameObject.SetActive(visible);
            if (visible) OnOpen();
            else OnClose();
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
    }
}
