using System;
using System.Collections.Generic;

// ==================================================

// 디스크에 저장되는 형태. JsonUtility가 Dictionary를 못 쓰므로 리스트로 둔다.
// !!) 구조를 바꾸면 CurrentVersion을 올리고 SaveService.Migrate에 변환을 추가할 것

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 3;     // 2: 26.11.18 명명 규칙으로 바뀐 ID 이전, 3: 클리어 플래그 Region_Clear_ → Clear_

    public int version = CurrentVersion;
    public List<ItemStack> items = new();
    public List<string> flags = new();
}

// ==================================================

[Serializable]
public struct ItemStack
{
    public string id;
    public int count;

    public ItemStack(string itemID, int count)
    {
        id = itemID;
        this.count = count;
    }
}
