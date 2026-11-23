using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// 빌드 직전에 DB를 갱신한다. 플레이 없이 빌드해도 새 SO가 빠지지 않게
public class DataBuildPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    // ------------------------------------------------

    public void OnPreprocessBuild(BuildReport report)
    {
        DataTools.RefreshDatabase(true);
    }
}
