using UnityEngine;

namespace MushTaker.UI
{
    /// <summary>
    /// 팝업 기반. 프리팹으로 만들고 PopupManager.Open으로 띄운다.
    /// </summary>
    public class Popup : MonoBehaviour
    {
        /// 뒤로 가기(Esc, 안드로이드 뒤로)로 닫을 수 있는지.
        public virtual bool CloseOnBack => true;

        public virtual void OnOpen() { }
        public virtual void OnClose() { }

        public void Close() => PopupManager.Instance.Close(this);
    }
}
