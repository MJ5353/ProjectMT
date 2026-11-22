using System;
using UnityEngine;

// ==================================================

// 유닛 능력치 종류. StatFeature가 출처(정비 노드·엠블렘·장비 등)별 수정치를 모아 최종값을 낸다.
// 기본값은 쓰는 쪽이 Def에서 넘긴다(최대 체력=UnitDef.MaxHp, 이동 속도=MoveSpeed, 경직 저항=BreakImmune, 대시=PlayerDef). 기본값이 없는 것은 배율(1)로 쓴다.
// 표시 글은 같은 이름의 문구 키(Stat_ + 이름)를 쓴다. 능력치를 추가하면 EStringKey에 Stat_이름 한 줄만 더한다
public enum EStatType
{
    None,
    Attack,         // 주는 피해 배율
    MaxHp,          // 최대 체력
    Dodge,          // 대시 쿨 감소(줄이는 능력치)
    Defense,        // 받는 피해 감소(줄이는 능력치)
    AttackSpeed,    // 공격 빠르기 배율
    MoveSpeed,      // 이동 속도
    PartDamage,     // 버섯 부위 피해 배율
    DashDistance,   // 대시 시간(= 거리)
    BreakResist,    // 경직 저항(UnitDef.BreakImmune에 더함)
    AttackRadius,   // 공격 판정 범위 배율
}

public enum EStatModType
{
    Rate,   // 기본값에 (1 + 합)을 곱한다. 0.1 = 10%
    Add,    // 기본값에 더한다
}

// ==================================================

// 능력치 수정치 하나. 노드·엠블렘 Def가 목록으로 들고, StatFeature.AddSource로 넣는다
[Serializable]
public class StatModifier
{
    [field: SerializeField] public EStatType Stat { get; private set; } = EStatType.None;
    [field: SerializeField] public EStatModType Type { get; private set; } = EStatModType.Rate;
    [field: SerializeField] public float Value { get; private set; } = 0f;

    private static EStringKey?[] s_ArrTextKey; // EStatType 순서 → Stat_ 문구 키(없으면 null). 처음 쓸 때 한 번 만든다

    // ------------------------------------------------

    public StatModifier(EStatType stat, EStatModType type, float value)
    {
        Stat = stat;
        Type = type;
        Value = value;
    }

    // 표시 한 줄(예: 공격력 +10%). 문구는 Stat_이름 키, {0}은 Rate면 %, Add면 수치
    public string ToText()
    {
        if (Stat == EStatType.None)
            return string.Empty;

        string value = Type == EStatModType.Rate ? $"{Mathf.RoundToInt(Value * 100f)}%" : Value.ToString("0.#");
        EStringKey? key = GetTextKey(Stat);

        return key.HasValue ? StringDef.S_Format(key.Value, value) : $"{Stat} {value}";
    }

    private static EStringKey? GetTextKey(EStatType stat)
    {
        if (s_ArrTextKey == null)
        {
            Array values = Enum.GetValues(typeof(EStatType));
            s_ArrTextKey = new EStringKey?[values.Length];
            foreach (EStatType type in values)
            {
                if (Enum.TryParse($"Stat_{type}", out EStringKey key))
                    s_ArrTextKey[(int)type] = key;
                else if (type != EStatType.None)
                    CLogger.Warning("StatModifier", $"EStringKey에 Stat_{type}이 없습니다.");
            }
        }

        return s_ArrTextKey[(int)stat];
    }
}
