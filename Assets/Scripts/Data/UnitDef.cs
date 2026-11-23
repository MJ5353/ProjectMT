using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 플레이어와 몬스터가 같이 쓰는 몸 수치. 몬스터는 MonsterDef, 플레이어는 PlayerDef가 이어받는다.
// 컨트롤러(UnitController)는 하나라서, 무엇을 붙일지(부품·코어·서브)·팀은 여기 가상 함수(SetFeatures·Team)로 Def가 정한다

public abstract class UnitDef : DataDef
{
    [field: SerializeField, FormerlySerializedAs("m_MaxHp")]
    public int MaxHp { get; private set; } = 3;                    // 최대 체력

    [field: SerializeField, FormerlySerializedAs("m_HitStunTime")]
    public float HitStunTime { get; private set; } = 0.35f;        // 맞고 굳는 시간(초)

    [field: SerializeField]
    public float BreakImmune { get; private set; } = 0f;           // 쌓인 경직(AttackSpec.Break)이 이 값을 넘어야 경직·넉백. 0이면 매번. 보스 3, 엘리트 1

    [field: SerializeField]
    public float BreakResetTime { get; private set; } = 2f;        // 이 시간(초) 동안 안 맞으면 쌓인 경직이 사라진다

    [field: SerializeField, FormerlySerializedAs("m_MoveSpeed")]
    public float MoveSpeed { get; private set; } = 3f;             // 기본 이동 속도. 몬스터는 추적 속도, 배회는 AIDef.WanderSpeedRate를 곱한다

    [field: SerializeField]
    public float TurnSpeed { get; private set; } = 900f;           // 초당 회전 각도. 보스는 낮춰서 옆을 노릴 틈을 준다

    [field: SerializeField]
    public float AttackBlendTime { get; private set; } = 0.08f;    // [mj] todo) 임시값. 공격 애니로 넘어갈 때 섞는 시간(초). 판정 시각은 상태 시간 기준이라 밀리지 않음

    // 피격 반응(넉백·번쩍·히트스톱·조각·무적) 수치. 여러 유닛이 같은 에셋을 같이 쓴다
    [field: SerializeField]
    public HitDef Hit { get; private set; } = null;

    // AI 프리셋(AI_Normal 등). 여러 유닛이 같은 에셋을 같이 쓴다. 비면 AI 없음(플레이어)
    [field: SerializeField]
    public AIDef AI { get; private set; } = null;

    // 공격 목록 --- 기본 공격(Kind Attack)과 스킬(Kind Skill). 몬스터는 모두 후보로 두고 거리·쿨·가중치로 고른다.
    // 플레이어는 Kind Attack 순서대로 0번 기본·1번 위로 치기, 스킬은 아직 쓰지 않는다
    [field: SerializeField]
    public List<SkillDef> ListSkill { get; private set; } = new();

    // 몸(프리팹)의 Resources 경로(폴더 + ID). 스폰 때 UnitFactory가 이 경로로 몸을 찾고 이 Def를 넘긴다
    public abstract string PrefabPath { get; }

    public AttackSpec MainAttack => GetBasicAttack(0); // 첫 기본 공격. 없으면 null
    public virtual ETeam Team => ETeam.Monster;
    public virtual bool IsUntargetable => false; // true면 노려지지도 맞지도 않는다(펫)
    public virtual bool IsSlowImmune => false;   // true면 전체 슬로우 중에도 제 속도(TimeManager.PlayerTimeRate)

    // ------------------------------------------------

    // 조립 (UnitController.Set이 부른다) ---

    // 공통 부품·StateF 다음에 붙일 Def별 부품(부위·판정 모양·대시 등)과 코어·서브(입력·AI)
    public virtual void SetFeatures(UnitController unit) { }

#if UNITY_EDITOR
    // 선택했을 때 씬 뷰에 Def별 기즈모(UnitController.OnDrawGizmosSelected가 부른다)
    public virtual void DrawGizmos(UnitController unit) { }
#endif

    // 기본 공격 index번째. ListSkill의 Kind Attack 순서
    public AttackSpec GetBasicAttack(int index)
    {
        foreach (SkillDef skill in ListSkill)
        {
            if (skill == null || skill.Kind != ESkillKind.Attack)
                continue;

            if (index == 0)
                return skill.Attack;

            index--;
        }

        return null;
    }

    // 쓸 수 있는 공격 전부(SkillDef). 판정 뼈 검사·기즈모·몬스터 후보용
    public void CollectAttacks(List<AttackSpec> results)
    {
        results.Clear();
        foreach (SkillDef skill in ListSkill)
        {
            if (skill != null)
                results.Add(skill.Attack);
        }
    }
}
