using System.IO;
using UnityEditor;
using UnityEngine;

// ==================================================

// 세이브 파일 메뉴. 플레이를 켜지 않고도 세이브(persistentDataPath/save.json)를 지운다.
// 플레이 중이면 DBG 패널의 세이브 삭제처럼 메모리 데이터도 비운다

public static class SaveTools
{
    [MenuItem("MushTaker/세이브/삭제", false, 200)]
    private static void DeleteSave()
    {
        string path = Path.Combine(Application.persistentDataPath, CommonPath.SaveFile);
        if (EditorUtility.DisplayDialog("세이브 삭제", $"세이브 파일을 지웁니다. 되돌릴 수 없어요.\n{path}", "삭제", "취소") == false)
            return;

        if (Application.isPlaying && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.ResetSave();
            CLogger.Log("SaveTools", "플레이 중 세이브와 메모리 데이터를 지웠습니다.");
            return;
        }

        if (File.Exists(path) == false)
        {
            CLogger.Log("SaveTools", $"지울 세이브가 없습니다: {path}");
            return;
        }

        File.Delete(path);
        CLogger.Log("SaveTools", $"세이브를 지웠습니다: {path}");
    }

    [MenuItem("MushTaker/세이브/폴더 열기", false, 201)]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
