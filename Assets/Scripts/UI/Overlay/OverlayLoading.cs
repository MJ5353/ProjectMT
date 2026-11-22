using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 씬 전환 때 모든 화면을 덮는 로딩 화면(가짜 로딩). 씬에 두지 않고 UIManager가 [Managers] 아래에 하나 만들어 두고 전환 동안만 켠다(Show·Hide).
// 프리팹 루트가 맨 위 정렬 캔버스라 어느 씬 위에서나 뜬다. 진행바는 SceneLoader가 채우고, 화면 전체 배경 버튼을 누르면 버섯이 튄다.
// 프리팹은 TitleUISetup.Prefabs 표로 만든다

public class OverlayLoading : OverlayBase
{
    [SerializeField] private Button m_BurstButton = null;
    [SerializeField] private UIGauge m_Progress = null;

    // ------------------------------------------------

    private void Awake() => m_BurstButton.E_SetOnClick(OnClickBurst);

    public void SetProgress(float ratio) => m_Progress.SetRatio(ratio, true);

    // 버섯은 이 화면(캔버스) 위에 띄운다. 씬과 상관없이 늘 터진다
    private void OnClickBurst() => UIManager.Instance.PlayClickBurst(RectTransform);

    // virtual ---

    // 지난 전환 때 차 있던 진행바가 잠깐 보이지 않게 비우고 시작한다
    protected override void OnShow() => m_Progress.SetRatio(0f, true);

#if UNITY_EDITOR
    public void EditorSetup(Button burstButton, UIGauge progress)
    {
        m_BurstButton = burstButton;
        m_Progress = progress;
    }
#endif
}
