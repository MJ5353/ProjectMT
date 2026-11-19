using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 플레이어와 몬스터가 같이 쓰는 유닛 하나. 부품(Feature 목록) · 실행기(UnitExecutor) · 판단(UnitBrain)으로 이뤄진다.
// 무엇을 붙일지·팀·판단 종류는 Def가 정한다(UnitDef.SetFeatures·CreateBrain·Team). 그래서 컨트롤러는 이 클래스 하나다.
// 조립 순서: 공통 부품 → 실행기 → Def별 부품(Def.SetFeatures) → 판단(Def.CreateBrain).
// 매 프레임 순서는 Tick 한 곳에 적는다: 부품 Tick → 실행기 → 판단 → 부품 LateTick(이동). 호출은 판단 → 실행기 → 부품 방향으로만
// 피격·사망은 여기서 실행기(RequestHit·RequestDead)에 바로 넘기고, 그 뒤 할 일은 판단 쪽 훅이 정한다.
// 직렬화 참조 둘은 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다. 프리팹 안의 참조는 UnitProperty에 모은다

public class UnitController : PoolableMono
{
    public int UnitID { get; private set; }    // UnitService가 꺼낼 때 넣어 준다. 0이면 등록 안 됨

    [SerializeField] private UnitDef m_Def = null;              // 수치 데이터(PlayerDef·MonsterDef). 직렬화 이름이 합치기 전과 같아 프리팹 데이터 유지
    [SerializeField] private UnitProperty m_Property = null;    // 프리팹 참조(몸·애니·렌더러·부위 뼈·소켓)

    // core
    private readonly List<UnitFeature> m_ListFeature = new();   // 수명 주기를 함께 돌릴 부품. 등록 순서대로 실행
    public BrainBase BrainBase { get; private set; }            // 판단(플레이어 입력·AI)
    public UnitExecutor UnitExecutor { get; private set; }      // 실행기(이동·공격·대시·피격·사망). 판단 쪽은 여기에 요청만 한다

    // feature
    public HealthFeature HealthF { get; private set; }
    public MoveFeature MoveF { get; private set; }
    public AnimFeature AnimF { get; private set; }
    public HitFeature HitF { get; private set; }
    public PartFeature PartF { get; private set; }    // 버섯 부위. 없으면 null(SetPartFeature로 붙인다)

    // get
    public UnitDef Def => m_Def;
    public ETeam Team => m_Def.Team;
    public bool IsDead => HealthF.IsDead;   // 사망 여부는 밖에서 여기로만 본다
    public UnitProperty Property => m_Property;
    public Transform VisualRoot => m_Property.Animator.transform;
    public CharacterController HurtBody => m_Property.CharacterController;

    // ------------------------------------------------

    private void Set()
    {
        if (m_Def == null || m_Property == null)
        {
            CLogger.Error(GetType().Name, $"Def 또는 UnitProperty가 비어 있습니다(프리팹 다시 굽기): {name}", this);
            enabled = false;

            return;
        }

        MoveF = AddFeature(new MoveFeature(this, m_Property.CharacterController, m_Def.TurnSpeed));
        AnimF = AddFeature(new AnimFeature(this, m_Property.Animator));
        HealthF = AddFeature(new HealthFeature(this, m_Def.MaxHp));
        HitF = AddFeature(new HitFeature(this, m_Def.Hit, m_Property.ListBodyRenderer));
        UnitExecutor = new UnitExecutor(this, m_Def);

        m_Def.SetFeatures(this);
        BrainBase = m_Def.CreateBrain(this);

        // 피격 반응 수치가 없으면 체력만 깎이고 반응은 없다
        if (m_Def.Hit == null)
            CLogger.Warning(GetType().Name, $"{m_Def.name}의 HitDef가 비어 있어 피격 반응이 없습니다", this);

        HitF.Hit += OnHit;
        HealthF.Died += OnDied;
    }

    // 수명 주기를 함께 돌릴 부품 등록. 등록 순서대로 실행. Def.SetFeatures가 부른다
    public T AddFeature<T>(T feature) where T : UnitFeature
    {
        m_ListFeature.Add(feature);
        return feature;
    }

    // 버섯 부위 등록(MonsterDef.SetFeatures). PartF는 여기서만 채운다
    public void SetPartFeature(PartFeature part)
    {
        PartF = AddFeature(part);
    }

    public void Tick(float deltaTime, float unscaledDeltaTime)
    {
        if (enabled == false)
            return;

        for (int i = 0; i < m_ListFeature.Count; ++i)
        {
            UnitFeature feature = m_ListFeature[i];
            feature.Tick(feature.IsUnscaledTime ? unscaledDeltaTime : deltaTime);
        }

        UnitExecutor.Tick(deltaTime);
        BrainBase.Tick(deltaTime);

        foreach (var feature in m_ListFeature)
        {
            feature.LateTick(feature.IsUnscaledTime ? unscaledDeltaTime : deltaTime);
        }
    }

    public void OnSpawn(int unitID)
    {
        // UnitFactory가 풀에서 꺼낸 직후. 처음 상태로 되돌린다
        if (m_ListFeature.Count == 0)
            Set();

        UnitID = unitID;
        foreach (var feature in m_ListFeature)
        {
            feature.Reset();
        }

        UnitExecutor.Reset();
        BrainBase.Reset();
    }

    public void OnDespawn()
    {
        // 풀로 돌아가기 직전
        UnitID = 0;
        BrainBase.Clear();
        UnitExecutor.Clear();
        foreach (var feature in m_ListFeature)
        {
            feature.Clear();
        }
    }

    // 맞으면 판단 쪽이 먼저 기록(공격자 등)하고, 실행기는 피격 경직으로. 죽는 타격은 사망이 먼저 처리돼 여기서 끝난다
    private void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        BrainBase.OnHit(hit);
        UnitExecutor.RequestHit();
    }

    private void OnDied()
    {
        UnitExecutor.RequestDead();
        BrainBase.OnDied();
    }

    public void Despawn()
    {
        UnitService units = GameManager.Instance?.UnitS;
        if (units != null && UnitID != 0)
        {
            // 등록돼 있으면 UnitService가 지운다
            units.Despawn(UnitID);
            return;
        }

        Destroy(gameObject);
    }

    // 몬스터가 노리거나 버섯이 빨려 들 수 있는 상태인지. 판단 기준은 여기서만 정한다
    public bool IsValid()
    {
        return IsDead == false;
    }

    // unity ---

    private void OnDisable()
    {
        HitF?.RestoreFlash();
    }

    private void OnDrawGizmosSelected()
    {
        if (m_Def == null || m_Property == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        foreach (AttackSpec attack in GetAttacks())
        {
            Transform bone = GetBattleBone(attack.HitPart);
            Gizmos.DrawWireSphere(AttackAction.GetCenter(transform, attack, bone), attack.Radius);
        }

#if UNITY_EDITOR
        m_Def.DrawGizmos(this);
#endif
    }

    // get ---

    // Def의 공격 전부(SkillDef). 판정 뼈 검사·기즈모용이라 매번 새 목록
    private List<AttackSpec> GetAttacks()
    {
        List<AttackSpec> listAttack = new();
        m_Def.CollectAttacks(listAttack);
        return listAttack;
    }

    public Transform GetBattleBone(EBattlePartType part)
    {
        return m_Property.GetBattleBone(part);
    }
}
