using UnityEngine;

// ==================================================

// 씬마다 하나인 기본 화면(타이틀·로비·전투)의 루트. 씬의 SceneController가 m_Screen으로 들고 GetScreen<T>()로 꺼낸다.
// 씬을 넘어 뜨는 로딩 화면 같은 것은 Overlay(OverlayBase, UIManager)

public abstract class ScreenBase : MonoBehaviour
{
    public RectTransform RectTransform => transform as RectTransform;
}
