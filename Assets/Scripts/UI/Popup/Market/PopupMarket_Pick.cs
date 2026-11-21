using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 시장에서 고른 버섯(묶음 → 팔 개수). PopupMarket이 들고 담기·빼기·개수 바꾸기를 맡긴다.
// 표시 순서(ListKey)와 HuntData 요청 목록(BuildRequest)도 여기서 만든다. 판정·계산은 HuntData가 한다
public class PopupMarket_Pick
{
    private readonly Dictionary<LootKey, int> m_DicCount = new(); // 고른 버섯 → 팔 개수
    private readonly List<LootKey> m_ListKey = new();             // 표시 순서. Sync 때 다시 만든다
    private readonly List<MarketItem> m_ListItem = new();         // HuntData 요청용. BuildRequest 때 다시 만든다

    public int Count => m_DicCount.Count;
    public IReadOnlyList<LootKey> ListKey => m_ListKey;

    // ------------------------------------------------

    public void Clear()
    {
        m_DicCount.Clear();
        m_ListKey.Clear();
    }

    public int GetCount(LootKey key)
    {
        return m_DicCount.TryGetValue(key, out int count) ? count : 0;
    }

    // 0 이하면 뺀다
    public void Set(LootKey key, int count)
    {
        if (count <= 0)
            m_DicCount.Remove(key);
        else
            m_DicCount[key] = count;
    }

    public void Remove(LootKey key)
    {
        m_DicCount.Remove(key);
    }

    // 고른 개수를 남은 버섯에 맞추고(거래·기다리기 뒤 사라지거나 줄어든 버섯은 빼거나 줄인다) 표시 순서를 다시 만든다
    public void Sync(IReadOnlyDictionary<LootKey, int> dicHarvest, Comparison<LootKey> compare)
    {
        m_ListKey.Clear();
        foreach (LootKey key in m_DicCount.Keys)
        {
            m_ListKey.Add(key);
        }

        for (int i = m_ListKey.Count - 1; i >= 0; --i)
        {
            LootKey key = m_ListKey[i];
            dicHarvest.TryGetValue(key, out int have);
            
            int count = Mathf.Min(m_DicCount[key], have);
            if (count > 0)
            {
                m_DicCount[key] = count;
                continue;
            }

            m_DicCount.Remove(key);
            m_ListKey.RemoveAt(i);
        }

        m_ListKey.Sort(compare);
    }

    // HuntData 요청 목록. 같은 목록을 다시 쓰니 다음 BuildRequest 전까지만 유효하다
    public IReadOnlyList<MarketItem> BuildRequest()
    {
        m_ListItem.Clear();
        foreach (var pair in m_DicCount)
        {
            m_ListItem.Add(new MarketItem(pair.Key, pair.Value));
        }

        return m_ListItem;
    }

    // 요청 목록(listItem)대로 다시 고른다. 거래가 실패했을 때 되돌리는 용도
    public void Restore(IReadOnlyList<MarketItem> listItem)
    {
        foreach (MarketItem item in listItem)
        {
            m_DicCount[item.Key] = item.Count;
        }
    }
}
