using System;
using System.Collections.Generic;

// ==================================================

// 디스크에 저장되는 형태. JsonUtility가 Dictionary를 못 쓰므로 리스트로 둔다.
// !!) 구조를 바꾸면 CurrentVersion을 올리고 SaveService.Migrate에 변환을 추가할 것

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 4;     // 3: 26.11.18 명명 규칙 적용. 이전 버전 세이브는 이전하지 않음(초기화), 4: 상인 신뢰도 추가

    public int version = CurrentVersion;
    public List<ItemStack> items = new();
    public List<string> flags = new();
    public List<MerchantTrust> merchants = new();
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

// ==================================================

[Serializable]
public struct MerchantTrust
{
    public string id;
    public int trust;

    public MerchantTrust(string merchantID, int trust)
    {
        id = merchantID;
        this.trust = trust;
    }
}
