using UnityEngine;

// ==================================================

// 화면 하나(로비·전투·타이틀·로딩)의 루트. 씬의 SceneController가 m_Screen으로 들고 GetScreen<T>()로 꺼낸다

public abstract class ScreenBase : MonoBehaviour
{
    public RectTransform RectTransform => transform as RectTransform;
}
