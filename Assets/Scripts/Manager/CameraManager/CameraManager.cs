using UnityEngine;

// ==================================================

// 지금 씬의 메인 카메라를 들고 있다. 씬 컨트롤러가 시작할 때 등록하고 사라질 때 지운다(Camera.main 검색 대신).
// [Managers] 루트 아래에 있어 씬이 바뀌어도 남는다(SceneControllerBase가 Create). 흔들림·보스전 구도 같은 연출도 이후 여기에 모은다

public class CameraManager : MonoSingleton<CameraManager>
{
    public Camera Main { get; private set; }
    public Transform MainTransform => Main != null ? Main.transform : null;

    // ------------------------------------------------

    public void SetMain(Camera mainCamera)
    {
        Main = mainCamera;
        if (Main == null)
            CLogger.Warning("CameraManager", "씬 컨트롤러에 메인 카메라가 연결되지 않았습니다.");
    }

    // 다음 씬이 먼저 등록했으면 건드리지 않는다
    public void ClearMain(Camera mainCamera)
    {
        if (Main == mainCamera)
            Main = null;
    }

    // virtual ---

    protected override void OnClear()
    {
        Main = null;
    }
}
