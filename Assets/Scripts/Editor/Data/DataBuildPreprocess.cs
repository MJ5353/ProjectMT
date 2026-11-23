using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// ==================================================

// 빌드 직전 데이터베이스 갱신. 플레이 버튼(DataTools)과 같은 갱신을 빌드에도 건다. 새 SO를 만들고 플레이 없이 빌드해도 빠지지 않게

public class DataBuildPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    // ------------------------------------------------

    public void OnPreprocessBuild(BuildReport report)
    {
        DataTools.RefreshDatabase(true);
    }
}
