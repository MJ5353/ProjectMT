using System.Collections.Generic;
using System.Linq;

// ==================================================

// 공용 확장 함수. 이름은 E_ 접두사로 시작해 Unity·.NET 기본 함수와 구분
public static class CommonExtensions
{
    // Unity 오브젝트가 Destroy된 경우에도 null을 돌려줘서 ?. 연산자를 안전하게 쓸 수 있게 한다
    public static T E_Alive<T>(this T obj) where T : UnityEngine.Object
    {
        return obj != null ? obj : null;
    }

    public static UnityEngine.Transform E_FindDeep(this UnityEngine.Transform parent, string targetName)
    {
        if (parent == null || parent.name == targetName)
            return parent;

        foreach (UnityEngine.Transform child in parent)
        {
            UnityEngine.Transform found = child.E_FindDeep(targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    // ui ---

    // null이면 넘어가고, 이미 같은 상태면 건드리지 않는다
    public static void E_SetActive(this UnityEngine.GameObject go, bool isActive)
    {
        if (go == null || go.activeSelf == isActive)
            return;

        go.SetActive(isActive);
    }

    public static void E_SetActive(this UnityEngine.Component component, bool isActive)
    {
        if (component == null)
            return;

        component.gameObject.E_SetActive(isActive);
    }

    // 비어 있어도 되는 글자 칸에 쓴다. null이면 넘어간다
    public static void E_SetText(this TMPro.TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    // 글자를 넣고 켜거나 끈다. null이면 넘어간다
    public static void E_SetTextWithActive(this TMPro.TMP_Text text, string value, bool isActive)
    {
        if (text == null)
            return;

        text.E_SetActive(isActive);
        text.text = value;
    }

    // 글자가 있으면 켜고, 비었으면 끈다
    public static void E_SetTextWithActive(this TMPro.TMP_Text text, string value)
    {
        text.E_SetTextWithActive(value, value.E_IsNullOrEmpty() == false);
    }

    // 버튼 등 누를 수 있는지. null이면 넘어가고, 이미 같으면 건드리지 않는다
    public static void E_SetInteractable(this UnityEngine.UI.Selectable selectable, bool isInteractable)
    {
        if (selectable == null || selectable.interactable == isInteractable)
            return;

        selectable.interactable = isInteractable;
    }

    // 비어 있어도 되는 그림 칸에 쓴다. null이면 넘어간다
    public static void E_SetImage(this UnityEngine.UI.Image image, UnityEngine.Sprite sprite, UnityEngine.Color color)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = color;
    }

    // 그림만 바꾸고 색은 그대로
    public static void E_SetImage(this UnityEngine.UI.Image image, UnityEngine.Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
    }

    // 글자·그림 색. null이면 넘어간다
    public static void E_SetColor(this UnityEngine.UI.Graphic graphic, UnityEngine.Color color)
    {
        if (graphic == null)
            return;

        graphic.color = color;
    }

    // 버튼 누름 연결. 기존 코드 연결은 지우고 하나만 건다(중복 연결 방지). 인스펙터 연결은 남는다
    public static void E_SetOnClick(this UnityEngine.UI.Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (onClick != null)
            button.onClick.AddListener(onClick);
    }

    // 보이기(alpha 1, 누름 받음) / 숨기기(alpha 0, 누름 통과). null이면 넘어간다
    public static void E_SetVisible(this UnityEngine.CanvasGroup group, bool isVisible)
    {
        if (group == null)
            return;

        group.alpha = isVisible ? 1f : 0f;
        group.blocksRaycasts = isVisible;
    }

    // 누름만 막거나 푼다(alpha는 연출이 맡을 때). null이면 넘어간다
    public static void E_SetBlocksRaycasts(this UnityEngine.CanvasGroup group, bool isBlock)
    {
        if (group == null)
            return;

        group.blocksRaycasts = isBlock;
    }

    // collection ---

    public static bool E_IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

    public static bool E_IsNullOrEmpty<T>(this ICollection<T> collection) => collection == null || collection.Count <= 0;

    public static bool E_IsNullOrEmpty<T>(this T[] array) => array == null || array.Length <= 0;

    public static bool E_Contains<T>(this T[] array, T item)
    {
        if (array.E_IsNullOrEmpty())
            return false;

        return System.Array.IndexOf(array, item) >= 0;
    }

    public static T E_GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
    {
        if (list == null || index < 0 || index >= list.Count)
            return defaultValue;

        return list[index];
    }

    public static T E_GetOrDefault<T>(this IEnumerable<T> list, int index, T defaultValue = default)
    {
        if (list == null || index < 0)
            return defaultValue;

        return list.Skip(index).DefaultIfEmpty(defaultValue).First();
    }
    
    public static TValue E_GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue defaultValue = default)
    {
        if (dic == null || key == null)
            return defaultValue;

        return dic.TryGetValue(key, out TValue value) ? value : defaultValue;
    }

    // 이미 있으면 추가하지 않는다. 추가했으면 true
    public static bool E_AddUnique<T>(this IList<T> list, T item)
    {
        if (list == null || list.Contains(item))
            return false;

        list.Add(item);
        return true;
    }

    // 뒤에서부터 찾아 하나만 지운다. 최근에 넣은 항목을 지울 때 빠르다
    public static bool E_RemoveBack<T>(this IList<T> list, T item)
    {
        if (list == null)
            return false;

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (comparer.Equals(list[i], item) == false)
                continue;

            list.RemoveAt(i);
            return true;
        }

        return false;
    }
}
