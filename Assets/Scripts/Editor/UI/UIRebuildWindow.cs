using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ==================================================

// UI 프리팹 목록에서 골라 다시 만든다. 목록은 이름 머리(Screen·Popup·Overlay·UI)로 묶어 보여 주고, 만드는 순서는 UISetup.GetPrefabs를 따른다
public sealed class UIRebuildWindow : EditorWindow
{
    // 이름 머리 → 묶음 제목. 위에서부터 그린다. 부품(소속_이름)은 소속의 머리를 따라간다(PopupMarket_MerchantRow → Popup)
    private static readonly (string Prefix, string Title)[] s_Groups =
    {
        ("Screen", "Screen"),
        ("Popup", "Popup"),
        ("Overlay", "Overlay"),
        ("UI", "Component"),
    };
    private const string EtcTitle = "기타";
    private const float ToggleWidth = 18f;

    private readonly HashSet<string> m_SetChecked = new();
    private readonly HashSet<string> m_SetFolded = new();
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
            {
                m_SetChecked.UnionWith(listPrefab.Select(prefab => prefab.Path));
            }
            if (GUILayout.Button("모두 해제"))
            {
                m_SetChecked.Clear();
            }
        }

        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        foreach (IGrouping<string, string> group in listPrefab.Select(prefab => prefab.Path).GroupBy(GetGroupTitle).OrderBy(group => GetGroupOrder(group.Key)))
        {
            DrawGroup(group.Key, group.ToList());
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
                    {
                        UISetup.BuildPrefabs(listSelected, true);
                    }
                };
            }
        }
    }

    // 묶음 머리: 앞에 묶음 전체 선택 토글(일부만 골랐으면 섞임 표시), 뒤에 접기. 항목은 한 단 들여 쓴다
    private void DrawGroup(string title, List<string> listPath)
    {
        int checkedCount = listPath.Count(path => m_SetChecked.Contains(path));
        bool isAllChecked = checkedCount == listPath.Count;
        bool isFolded = m_SetFolded.Contains(title);

        EditorGUILayout.Space(4f);
        Rect rect = EditorGUILayout.GetControlRect();
        Rect toggleRect = new Rect(rect.x, rect.y, ToggleWidth, rect.height);
        Rect foldoutRect = new Rect(rect.x + ToggleWidth, rect.y, rect.width - ToggleWidth, rect.height);

        EditorGUI.showMixedValue = checkedCount > 0 && isAllChecked == false;
        bool isToggled = EditorGUI.Toggle(toggleRect, isAllChecked);
        EditorGUI.showMixedValue = false;
        if (isToggled != isAllChecked)
        {
            if (isToggled)
            {
                m_SetChecked.UnionWith(listPath);
            }
            else
            {
                m_SetChecked.ExceptWith(listPath);
            }
        }

        bool isOpen = EditorGUI.Foldout(foldoutRect, isFolded == false, $"{title} ({checkedCount}/{listPath.Count})", true);
        if (isOpen)
        {
            m_SetFolded.Remove(title);
        }
        else
        {
            m_SetFolded.Add(title);
        }

        if (isFolded)
        {
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (string path in listPath)
            {
                bool isChecked = EditorGUILayout.ToggleLeft(Path.GetFileNameWithoutExtension(path), m_SetChecked.Contains(path));
                if (isChecked)
                {
                    m_SetChecked.Add(path);
                }
                else
                {
                    m_SetChecked.Remove(path);
                }
            }
        }
    }

    private static string GetGroupTitle(string path)
    {
        string prefabName = Path.GetFileNameWithoutExtension(path);
        foreach ((string prefix, string title) in s_Groups)
        {
            if (prefabName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return title;
            }
        }
        return EtcTitle;
    }

    private static int GetGroupOrder(string title)
    {
        int index = Array.FindIndex(s_Groups, group => group.Title == title);
        return index < 0 ? s_Groups.Length : index;
    }
}
