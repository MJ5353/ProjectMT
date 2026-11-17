using UnityEngine;

// ==================================================

// 팝업 기반. 프리팹으로 만들고 PopupService.Open으로 띄운다
public class Popup : MonoBehaviour
{
    // 뒤로 가기(Esc, 안드로이드 뒤로)로 닫을 수 있는지
    public virtual bool IsCloseOnBack => true;

    // ------------------------------------------------
    
    public void Close() => GameManager.Instance?.Popup?.Close(this);

    // virtual ---

    public virtual void OnOpen() { }

    public virtual void OnClose() { }
}
