using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 확률 계산 모음. 가중치 뽑기와 확률 판정을 한곳에서 한다(드롭 희귀도, 상인 방문 등).
// 상태가 없어 매니저로 두지 않고 정적 함수로 둔다. 난수는 UnityEngine.Random을 쓴다
public static class CRandom
{
    // probability(0~1) 확률로 true
    public static bool Chance(float probability)
    {
        if (probability <= 0f)
            return false;

        return UnityEngine.Random.value < probability;
    }

    // 가중치 비율로 하나를 뽑아 인덱스를 돌려준다. 가중치가 0 이하인 항목은 뽑히지 않고, 모두 0이면 -1
    public static int PickIndex<T>(IReadOnlyList<T> list, Func<T, float> getWeight)
    {
        if (list == null || getWeight == null)
            return -1;

        float total = 0f;
        for (int i = 0; i < list.Count; ++i)
        {
            total += Mathf.Max(0f, getWeight(list[i]));
        }

        if (total <= 0f)
            return -1;

        float roll = UnityEngine.Random.value * total;
        int last = -1;
        for (int i = 0; i < list.Count; ++i)
        {
            float weight = getWeight(list[i]);
            if (weight <= 0f)
                continue;

            last = i;
            roll -= weight;
            if (roll < 0f)
                return i;
        }

        return last; // Random.value가 1일 때
    }
}
