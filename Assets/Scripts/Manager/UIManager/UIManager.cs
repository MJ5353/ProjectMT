using UnityEngine;
using UnityEngine.SceneManagement;

// ==================================================

// 씬을 넘어 사는 UI를 맡는다. 씬 UI 루트 연결·해제, 오버레이(로딩 화면), 버튼을 누르면 버섯이 튀는 연출(UIClickBurst).
// UI 분류: Screen(씬마다 하나, 씬 컨트롤러) / Popup(씬 안에서 쌓임, PopupService) / Overlay(씬을 넘어 맨 위, 여기)
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneController가 Create). 클릭 연출 Tick은 SceneController가 부른다

public sealed class UIManager : MonoSingleton<UIManager>
{
    private const string LoadingMissingHint = "MushTaker/UI/모든 UI 다시 만들기를 실행하세요.";

    private readonly UIClickBurst m_ClickBurst = new();
    private OverlayLoading m_Loading; // 처음 띄울 때 만들고, 씬 전환 동안만 켠다. 원본이 없으면 로딩 화면 없이 넘어간다
    private Scene m_UIScene;

    public RectTransform UIRoot { get; private set; } // 지금 씬의 UI 루트(팝업·클릭 연출이 여기 뜬다)

    // ------------------------------------------------

    public void SetUIRoot(Scene scene, RectTransform root)
    {
        m_UIScene = scene;
        UIRoot = root;
    }

    // 씬 UI 루트에 떠 있던 클릭 연출 조각을 돌려받고 루트를 뗀다
    public void ClearUIRoot(Scene scene)
    {
        if (m_UIScene != scene)
            return;

        m_ClickBurst.Clear(UIRoot);
        UIRoot = null;
        m_UIScene = default;
    }

    public void Tick() => m_ClickBurst.Tick();

    // 클릭 연출 ---

    // 버튼 클릭 자리에서 버섯을 튀긴다. root를 주면 그 아래(오버레이 등)에 띄우고, 없으면 씬 UI 루트에 띄운다
    public void PlayClickBurst(RectTransform root = null) => m_ClickBurst.Play(root);

    // 로딩 화면 ---

    public void ShowLoading(bool isShow)
    {
        if (isShow && m_Loading == null)
            m_Loading = CreateLoading();

        if (m_Loading == null)
            return;

        if (isShow)
            m_Loading.Show();
        else
            m_Loading.Hide();
    }

    public void SetLoadingProgress(float ratio)
    {
        if (m_Loading == null)
            return;

        m_Loading.SetProgress(ratio);
    }

    // !!) GameManager.OnInit 안에서 만들면 문구(UILabel → StringDef)가 DB를 못 찾는다. 처음 띄울 때 만든다
    private OverlayLoading CreateLoading()
    {
        OverlayLoading prefab = ResourceManager.Instance.Load<OverlayLoading>(CommonPath.OverlayLoading, LoadingMissingHint);
        if (prefab == null)
            return null;

        return ResourceManager.Instance.Create(prefab, transform);
    }

    // virtual ---

    protected override void OnInit()
    {
        m_ClickBurst.Init();
    }

    protected override void OnClear()
    {
        m_ClickBurst.Clear(null);
    }
}
