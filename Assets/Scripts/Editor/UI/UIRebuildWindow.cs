using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// UI 프리팹 목록에서 골라 다시 만든다. 목록·순서는 UISetup.GetPrefabs를 따른다
public sealed class UIRebuildWindow : EditorWindow
{
    private readonly HashSet<string> m_SetChecked = new();
    private Vector2 m_Scroll;

    // ------------------------------------------------

    [MenuItem("MushTaker/UI/UI 골라 다시 만들기", false, 2)]
    private static void Open()
    {
        GetWindow<UIRebuildWindow>("UI 다시 만들기");
    }

    private void OnGUI()
    {
        List<(string Path, Func<GameObject> Build)> listPrefab = UISetup.GetPrefabs();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("모두 선택"))
                m_SetChecked.UnionWith(listPrefab.Select(prefab => prefab.Path));
            if (GUILayout.Button("모두 해제"))
                m_SetChecked.Clear();
        }

        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        foreach ((string path, Func<GameObject> _) in listPrefab)
        {
            bool isChecked = EditorGUILayout.ToggleLeft(Path.GetFileNameWithoutExtension(path), m_SetChecked.Contains(path));
            if (isChecked)
                m_SetChecked.Add(path);
            else
                m_SetChecked.Remove(path);
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(m_SetChecked.Count == 0))
        {
            if (GUILayout.Button($"고른 {m_SetChecked.Count}개 다시 만들기", GUILayout.Height(28f)))
            {
                // 대화상자·프리팹 저장은 OnGUI 밖에서 해야 레이아웃 오류가 나지 않는다
                List<(string Path, Func<GameObject> Build)> listSelected = listPrefab.Where(prefab => m_SetChecked.Contains(prefab.Path)).ToList();
                EditorApplication.delayCall += () =>
                {
                    if (EditorUtility.DisplayDialog("UI 다시 만들기", "고른 프리팹에서 손본 부분은 사라집니다.", "다시 만들기", "취소"))
                        UISetup.BuildPrefabs(listSelected, true);
                };
            }
        }
    }
}
