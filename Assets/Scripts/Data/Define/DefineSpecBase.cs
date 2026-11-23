using System;

// ==================================================

// 전역 조절값(DefineDef) 한 묶음. 성격이 같은 값끼리 스펙 하나로 모으고, DefineDef가 스펙을 하나씩 필드로 든다.
// 도구에서는 스펙 단위로 보여 준다(DisplayName)

[Serializable]
public abstract class DefineSpecBase
{
    public abstract string DisplayName { get; } // 도구에 보일 이름
}
