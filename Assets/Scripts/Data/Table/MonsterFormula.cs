using System;
using UnityEngine;

// ==================================================

public enum EMonsterTier
{
    Normal, // 잡몹
    Elite, // 엘리트
    Boss,
}

// ==================================================

// 몬스터 자동 배정 공식의 튜닝값. 데이터 편집기 > 몬스터 > 자동 배정이 읽어서 MonsterDef에 기입한다.
// 시야·추적 포기 거리·공용 쿨·배회 속도 배율은 여기 말고 AI 프리셋(AIDef: AI_Normal·AI_Elite·AI_Boss)에서 정한다.
// 순서: 등급, 같은 등급 안 번호로 수치를 키운다. 지역 난이도(체력·공격 배율)는 굽지 않고 MapDef.ListMonsterModifier가 스폰 때 능력치 출처로 넣는다.
//   체력 = 기본 체력 × 등급 배율 × (1 + 번호 가산 × 등급 안 순번)
//   공격 = 기본 공격 × 등급 배율 × (1 + 번호 가산 × 등급 안 순번)
// 데이터 하나(Def)가 아니라 편집기 자동 배정이 쓰는 공식값이라 Def와 헷갈리지 않게 Formula로 부른다(26.11.22 이름 변경).
// 에셋: Assets/Resources/Data/Balance/MonsterFormula.asset (없으면 자동 배정 때 기본값으로 만든다)

[CreateAssetMenu(menuName = "MushTaker/Data/Monster Formula", fileName = "MonsterFormula")]
public class MonsterFormula : ScriptableObject
{
    // 등급 하나의 수치
    [Serializable]
    public class TierStat
    {
        [field: SerializeField, Tooltip("체력 배율")]
        public float HpRate { get; private set; } = 1f;

        [field: SerializeField, Tooltip("공격력 배율")]
        public float DamageRate { get; private set; } = 1f;

        [field: SerializeField, Tooltip("맞고 굳는 시간(초)")]
        public float HitStunTime { get; private set; } = 0.35f;

        [field: SerializeField, Tooltip("추적 속도(첫 지역 기준)")]
        public float MoveSpeed { get; private set; } = 3f;

        [field: SerializeField, Tooltip("공격 전 움츠리는 시간(예고)")]
        public float WindupTime { get; private set; } = 0.45f;

        [field: SerializeField, Tooltip("버섯을 꽂을 소켓 수 최소")]
        public int SocketMin { get; private set; } = 1;

        [field: SerializeField, Tooltip("버섯을 꽂을 소켓 수 최대. 소켓이 더 적으면 전부")]
        public int SocketMax { get; private set; } = 2;

        [field: SerializeField, Tooltip("몸에 꽂는 버섯 크기 범위(최소~최대)")]
        public Vector2 MushroomScale { get; private set; } = new(0.6f, 0.9f);

        [field: SerializeField, Tooltip("대표 버섯 드랍 개수 최소")]
        public int DropMin { get; private set; } = 1;

        [field: SerializeField, Tooltip("대표 버섯 드랍 개수 최대")]
        public int DropMax { get; private set; } = 2;

        [field: SerializeField, Range(0f, 1f), Tooltip("두 번째 버섯을 떨굴 확률. 0이면 대표 버섯만")]
        public float ExtraDropChance { get; private set; } = 0f;

        // ------------------------------------------------

        public TierStat()
        {
        }

        public TierStat(float hpRate, float damageRate, float hitStunTime, float moveSpeed,
            float windupTime, int socketMin, int socketMax, Vector2 mushroomScale, int dropMin,
            int dropMax, float extraDropChance)
        {
            HpRate = hpRate;
            DamageRate = damageRate;
            HitStunTime = hitStunTime;
            MoveSpeed = moveSpeed;
            WindupTime = windupTime;
            SocketMin = socketMin;
            SocketMax = socketMax;
            MushroomScale = mushroomScale;
            DropMin = dropMin;
            DropMax = dropMax;
            ExtraDropChance = extraDropChance;
        }
    }

    public const int MonstersPerMap = 5; // 명명 규칙: 지역마다 잡몹 2 → 엘리트 2 → 보스 1 (001~005 평원)
    public const int NormalPerMap = 2;

    // 기준 ---
    [field: SerializeField, Tooltip("첫 지역 잡몹 체력")]
    public int BaseHp { get; private set; } = 3;

    [field: SerializeField, Tooltip("첫 지역 잡몹 공격력")]
    public int BaseDamage { get; private set; } = 1;

    // 성장 ---
    [field: SerializeField, Tooltip("지역이 하나 뒤로 갈 때마다 이동 속도 가산(0.1 = +10%)")]
    public float MapSpeedAdd { get; private set; } = 0.08f;

    [field: SerializeField, Tooltip("같은 등급 안에서 뒷번호일수록 체력·공격 가산(0.2 = +20%)")]
    public float OrderStepRate { get; private set; } = 0.2f;

    // 파생 ---
    [field: SerializeField, Tooltip("소켓 버섯 기울기 무작위 범위(도). 0이면 똑바로")]
    public float MushroomTilt { get; private set; } = 12f;

    [field: SerializeField, Range(0f, 1f), Tooltip("소켓마다 대표 버섯을 고를 확률. 나머지는 지역의 다른 드랍 버섯")]
    public float MainMushroomWeight { get; private set; } = 0.7f;

    // 등급 ---
    [field: SerializeField]
    public TierStat Normal { get; private set; } = new(1f, 1f, 0.35f, 3f, 0.45f, 1, 2, new Vector2(0.6f, 0.9f), 1, 2, 0f);

    [field: SerializeField]
    public TierStat Elite { get; private set; } = new(2.5f, 2f, 0.2f, 2.4f, 0.65f, 2, 3, new Vector2(0.9f, 1.3f), 2, 3, 0.5f);

    [field: SerializeField]
    public TierStat Boss { get; private set; } = new(15f, 2f, 0.1f, 2.2f, 0.8f, 99, 99, new Vector2(1.2f, 1.6f), 4, 6, 1f);

    // ------------------------------------------------

    public TierStat GetTier(EMonsterTier tier)
    {
        switch (tier)
        {
            case EMonsterTier.Elite:
                return Elite;
            case EMonsterTier.Boss:
                return Boss;
            default:
                return Normal;
        }
    }

    // orderInTier: 같은 등급 안 순번(0부터). 지역 배율은 MapDef 쪽
    public int CalcHp(EMonsterTier tier, int orderInTier)
    {
        float value = BaseHp * GetTier(tier).HpRate * (1f + OrderStepRate * orderInTier);
        return Mathf.Max(1, Mathf.RoundToInt(value));
    }

    public int CalcDamage(EMonsterTier tier, int orderInTier)
    {
        float value = BaseDamage * GetTier(tier).DamageRate * (1f + OrderStepRate * orderInTier);
        return Mathf.Max(1, Mathf.RoundToInt(value));
    }

    // mapIndex: 0부터(첫 지역 0)
    public float CalcMoveSpeed(EMonsterTier tier, int mapIndex)
    {
        return GetTier(tier).MoveSpeed * (1f + MapSpeedAdd * mapIndex);
    }
}