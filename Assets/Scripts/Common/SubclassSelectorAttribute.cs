using System;
using UnityEngine;

// ==================================================

// [SerializeReference] 칸에 "하위 클래스 고르기" 드롭다운을 붙인다(드로어는 Editor/Common/SubclassSelectorDrawer).
// 목록이면 원소마다 붙는다. 고를 클래스는 [Serializable]이고 추상이 아니어야 한다
public class SubclassSelectorAttribute : PropertyAttribute { }

// 드롭다운에 보일 이름. 없으면 클래스 이름
[AttributeUsage(AttributeTargets.Class)]
public class SubclassLabelAttribute : Attribute
{
    public string Label { get; }

    public SubclassLabelAttribute(string label) => Label = label;
}
