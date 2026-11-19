using System;
using UnityEngine;

// ==================================================

// 피격 반응 종류. HitDef가 켤 종류를 고르고, HitFeature가 아래 순서대로 처리한다.
// 새 반응은 값 하나 + HitFeature의 처리 함수 하나로 더한다

[Flags]
public enum EHitReaction
{
    None = 0,
    Knockback = 1 << 0,    // 맞은 방향으로 밀림
    Flash = 1 << 1,        // 몸이 하얗게 번쩍
    HitStop = 1 << 2,      // 화면 잠깐 멈춤
    ChunkBurst = 1 << 3,   // 복셀 조각 튀김

    Default = Knockback | Flash | HitStop | ChunkBurst,
}

// ==================================================

// 피격 반응 수치 묶음. 여러 유닛이 같은 에셋을 같이 쓴다(예: Hit_Default, Hit_Heavy, Hit_Boss).
// 맞고 굳는 시간(HitStunTime)은 상태 쪽 값이라 UnitDef에 둔다

[CreateAssetMenu(menuName = "MushTaker/Data/Hit", fileName = "Hit_")]
public class HitDef : DataDef
{
    [field: SerializeField]
    public EHitReaction Reactions { get; private set; } = EHitReaction.Default;   // 켤 반응 종류

    [field: SerializeField]
    public float KnockbackScale { get; private set; } = 1f;                       // 넉백 배율. 1이면 그대로, 0이면 안 밀림

    [field: SerializeField]
    public float InvincibleTime { get; private set; } = 0f;                       // 맞은 뒤 무적 시간(초)

    [field: SerializeField]
    public float FlashTime { get; private set; } = 0.08f;                         // 번쩍이는 시간(실제 초, 히트스톱 중에도 흐름)

    [field: SerializeField]
    public Color FlashColor { get; private set; } = Color.white;                  // 몸을 맞았을 때 번쩍 색

    [field: SerializeField]
    public Color PartFlashColor { get; private set; } = new(1f, 0.85f, 0.1f);     // 부위(버섯)를 맞았을 때 번쩍 색

    [field: SerializeField]
    public Color ChunkColor { get; private set; } = new(0.85f, 0.75f, 0.6f);      // 튀는 복셀 조각 색

    // ------------------------------------------------

    public bool Has(EHitReaction reaction)
    {
        return (Reactions & reaction) != 0;
    }
}
