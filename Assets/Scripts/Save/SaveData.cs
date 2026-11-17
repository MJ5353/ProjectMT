using System;
using System.Collections.Generic;

// ==================================================

// 디스크에 저장되는 형태. JsonUtility가 Dictionary를 못 쓰므로 리스트로 둔다.
// !!) 구조를 바꾸면 CurrentVersion을 올리고 SaveService.Migrate에 변환을 추가할 것

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

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
