using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 그림 한 장짜리 공용 UI 요소. 연출 조각처럼 그림만 띄울 때 UIReuseService로 꺼내 쓴다(EUIComponentType.UICommonImage)

public class UICommonImage : PoolableMonoUI
{
    [SerializeField] private Image m_Image = null;

    public Image Image => m_Image;
}
