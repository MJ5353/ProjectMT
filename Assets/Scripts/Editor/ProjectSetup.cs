using UnityEditor;

// 최초 설정만 모은다. 데이터 작업은 데이터 편집기에서 한다.
public static class ProjectSetup
{
    [MenuItem("MushTaker/초기 설정/UI 기반 준비", false, 10)]
    public static void PrepareUI()
    {
        UISetup.SetupBase();
        if (UIBuilder.Font != null)
            UISetup.CreateCommonPrefabs();
    }
}
