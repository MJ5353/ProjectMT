using System;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 근접 공격 한 종류의 수치. 판정 프레임은 tools/voxel/README.md의 애니 표(30fps)를 따른다.

[Serializable]
public class AttackSpec
{
    public const float Fps = 30f;
    
    public float HitTime => m_HitFrame / Fps; // 판정 시작 시각
    public float HitEndTime => Mathf.Max(m_HitFrame, HitEndFrame) / Fps; // 판정 끝 시각

    [field: SerializeField, FormerlySerializedAs("m_Clip")]
    public string Clip { get; private set; } = "Attack"; // 재생할 애니 클립 이름

    [field: SerializeField, FormerlySerializedAs("m_Damage")]
    public int Damage { get; private set; } = 1;

    [SerializeField] 
    private int m_HitFrame = 9; // 판정 시작 프레임(30fps 기준)

    [field: SerializeField]
    public int HitEndFrame { get; private set; } = 0; // 판정 끝 프레임. 시작보다 작거나 같으면 한 프레임만

    [field: SerializeField, FormerlySerializedAs("m_Reach")]
    public float Reach { get; private set; } = 1f; // 몸 앞쪽으로 판정 중심까지 거리

    [field: SerializeField, FormerlySerializedAs("m_Radius")]
    public float Radius { get; private set; } = 0.9f; // 판정 구 반지름

    [field: SerializeField, FormerlySerializedAs("m_Height")]
    public float Height { get; private set; } = 0.6f; // 판정 중심 높이

    [field: SerializeField, FormerlySerializedAs("m_Knockback")]
    public float Knockback { get; private set; } = 6f; // 맞은 쪽을 미는 속도

    [field: SerializeField, FormerlySerializedAs("m_HitStop")]
    public float HitStop { get; private set; } = 0.06f; // 맞을 때 화면이 멈추는 시간(초)

    [field: SerializeField, FormerlySerializedAs("m_Lunge")]
    public float Lunge { get; private set; } = 0f; // 공격 시작 때 앞으로 내딛는 속도

    [field: SerializeField]
    public float ActiveMoveSpeed { get; private set; } = 0f; // 판정 구간 동안 앞으로 가는 속도(돌진). 0이면 제자리

    [field: SerializeField]
    public int RehitFrame { get; private set; } = 0; // 판정 구간 안에서 이 프레임마다 같은 대상을 다시 맞힐 수 있다(회오리 두 바퀴). 0이면 한 번만

    public float RehitTime => RehitFrame / Fps;

    [field: SerializeField]
    public EBattlePartType HitPart { get; private set; } = EBattlePartType.None; // 판정 중심 부위 뼈. None이면 몸 앞(Height·Reach)

    [field: SerializeField]
    public EffectController Effect { get; private set; } = null; // 판정이 처음 열릴 때 판정 자리에 터지는 이펙트 프리팹. 판정 반지름만큼 커진다. 없으면 안 띄움

    // 몬스터 공격 고르기 --- 플레이어는 쓰지 않는다
    [field: SerializeField]
    public float MinRange { get; private set; } = 0f; // 대상이 이 거리 이상일 때만 고른다

    [field: SerializeField]
    public float MaxRange { get; private set; } = 1.4f; // 대상이 이 거리 이하일 때만 고른다

    [field: SerializeField]
    public float Cooldown { get; private set; } = 0f; // 이 공격만의 쿨(초). 공용 쿨(AIDef.AttackCooldown)과 따로 돈다

    [field: SerializeField]
    public float Weight { get; private set; } = 1f; // 고를 때 가중치. 0이면 안 고른다

    [field: SerializeField]
    public float WindupTime { get; private set; } = 0f; // 움츠리는 예고 시간(초). 0이면 움츠림 없이 클립 자체 예비 동작

    // 콤보 --- 플레이어는 리셋 시간 안에 누르면, 몬스터는 끝나자마자 바로 이어 낸다
    [field: SerializeField]
    public SkillDef Next { get; private set; } = null; // 이어지는 다음 타. 비면 콤보 끝

    public AttackSpec NextAttack => Next != null ? Next.Attack : null;

    // ------------------------------------------------

    public AttackSpec() { }

    public AttackSpec(string clip, int damage, int hitFrame, float reach, float radius, float height, float knockback, float hitStop, float lunge)
    {
        Clip = clip;
        Damage = damage;
        m_HitFrame = hitFrame;
        Reach = reach;
        Radius = radius;
        Height = height;
        Knockback = knockback;
        HitStop = hitStop;
        Lunge = lunge;
    }
}
