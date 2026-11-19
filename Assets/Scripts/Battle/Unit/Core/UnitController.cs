using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 플레이어와 몬스터가 같이 쓰는 몸. 조립 순서: 공통 Feature → 타입별 Feature(SetFeatures) → 판단(CreateBrain).
// 판단(BrainFeature)을 마지막에 등록해서 매 프레임 "다른 Feature Tick → 판단 → LateTick(이동)" 순서가 된다.
// 피격·사망은 여기서 행동 층(RequestHit·RequestDead)에 바로 넘기고, 그 뒤 할 일은 판단 쪽 훅이 정한다.
// 직렬화 참조 둘은 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다. 프리팹 안의 참조는 UnitProperty에 모은다
// 제네릭이 아니라서 "아무 유닛"을 담을 때(Feature의 Owner, ActorService 목록, 타깃, 판정 후보) 이 타입을 쓴다

public abstract class UnitController : PoolableMono
{
    [SerializeField] private UnitProperty m_Property = null;     // 프리팹 참조(몸·애니·렌더러·부위 뼈·소켓)
    private readonly List<UnitFeature> m_ListFeature = new();   // 수명 주기를 함께 돌릴 부품. 등록 순서대로 실행

    public int ActorID { get; private set; }   // ActorService가 꺼낼 때 넣어 준다. 0이면 등록 안 됨
    protected abstract UnitDef BaseDef { get; }             // 몸 수치. 타입 있는 Def는 UnitController<TDef>.Def
    public abstract ETeam Team { get; }

    // feature
    public HealthFeature HealthF { get; private set; }
    public MoveFeature MoveF { get; private set; }
    public AnimFeature AnimF { get; private set; }
    public HitFeature HitF { get; private set; }
    public ActionFeature ActionF { get; private set; }      // 행동 층(이동·공격·대시·피격·사망). 판단 쪽은 여기에 요청만 한다
    public MeleeAttackFeature MeleeF { get; private set; }
    public TargetFeature TargetF { get; private set; }      // 대상 찾기·기억·거리
    public BrainFeature BrainF { get; private set; }        // 판단 층(플레이어 입력·몬스터 AI). 마지막에 등록
    public PartFeature PartF { get; protected set; }        // 버섯 부위. 없으면 null
    public HitBoxFeature HitBoxF { get; protected set; }    // 뼈 판정 모양. 없으면 null

    // get
    public bool IsDead => HealthF.IsDead;                   // 사망 여부는 밖에서 여기로만 본다
    public UnitProperty Property => m_Property;
    public Transform VisualRoot => m_Property.Animator.transform;
    public CharacterController HurtBody => m_Property.CharacterController;

    // ------------------------------------------------

    private void Set()
    {
        if (BaseDef == null || m_Property == null)
        {
            CLogger.Error(GetType().Name, $"Def 또는 UnitProperty가 비어 있습니다(프리팹 다시 굽기): {name}", this);
            enabled = false;

            return;
        }

        MoveF = AddFeature(new MoveFeature(this, m_Property.CharacterController, BaseDef.TurnSpeed));
        AnimF = AddFeature(new AnimFeature(this, m_Property.Animator));
        HealthF = AddFeature(new HealthFeature(this, BaseDef.MaxHp));
        HitF = AddFeature(new HitFeature(this, BaseDef.Hit, m_Property.ListBodyRenderer));
        MeleeF = AddFeature(new MeleeAttackFeature(this, GetAttacks()));
        TargetF = AddFeature(new TargetFeature(this));
        ActionF = AddFeature(new ActionFeature(this, BaseDef));

        SetFeatures();
        BrainF = AddFeature(CreateBrain());

        // 피격 반응 수치가 없으면 체력만 깎이고 반응은 없다
        if (BaseDef.Hit == null)
            CLogger.Warning(GetType().Name, $"{BaseDef.name}의 HitDef가 비어 있어 피격 반응이 없습니다", this);

        HitF.Hit += OnHit;
        HealthF.Died += OnDied;
    }

    protected T AddFeature<T>(T feature) where T : UnitFeature
    {
        // 수명 주기를 함께 돌릴 부품 등록. 등록 순서대로 실행
        m_ListFeature.Add(feature);
        return feature;
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

        foreach (var feature in m_ListFeature)
        {
            feature.LateTick(feature.IsUnscaledTime ? unscaledDeltaTime : deltaTime);
        }
    }

    public void OnSpawn(int actorID)
    {
        // ActorFactory가 풀에서 꺼낸 직후. 처음 상태로 되돌린다
        if (m_ListFeature.Count == 0)
            Set();

        ActorID = actorID;
        foreach (var feature in m_ListFeature)
        {
            feature.Reset();
        }
    }

    public void OnDespawn()
    {
        // 풀로 돌아가기 직전
        ActorID = 0;
        foreach (var feature in m_ListFeature)
        {
            feature.Clear();
        }
    }

    public void Despawn()
    {
        ActorService actors = GameManager.Instance?.ActorS;
        if (actors != null && ActorID != 0)
        {
            // 등록돼 있으면 ActorService가 지운다
            actors.Despawn(ActorID);
            return;
        }

        Destroy(gameObject);
    }

    // 맞으면 판단 쪽이 먼저 기록(공격자 등)하고, 행동 층은 피격 경직으로. 죽는 타격은 사망이 먼저 처리돼 여기서 끝난다
    private void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        BrainF.OnHit(hit);
        ActionF.RequestHit();
    }

    private void OnDied()
    {
        ActionF.RequestDead();
        BrainF.OnDied();
    }

    // virtual ---

    protected abstract void SetFeatures();          // 타입별 Feature 등록(부위·공격 고르기·대시 등)
    protected abstract BrainFeature CreateBrain();  // 판단(입력·AI)

    protected virtual void OnDisable()
    {
        HitF?.RestoreFlash();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (BaseDef == null || m_Property == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        foreach (AttackSpec attack in GetAttacks())
        {
            Transform bone = GetBattleBone(attack.HitPart);
            Gizmos.DrawWireSphere(MeleeAttackFeature.GetCenter(transform, attack, bone), attack.Radius);
        }
    }

    // get ---

    // Def의 공격 전부(SkillDef). 판정 뼈 검사·기즈모용이라 매번 새 목록
    private List<AttackSpec> GetAttacks()
    {
        List<AttackSpec> listAttack = new();
        BaseDef.CollectAttacks(listAttack);
        return listAttack;
    }

    public Transform GetBattleBone(EBattlePartType part)
    {
        return m_Property.GetBattleBone(part);
    }
}

// ==================================================

// Def를 타입 있게 쓰는 유닛(PlayerController·MonsterController가 이어받는다)
public abstract class UnitController<TDef> : UnitController where TDef : UnitDef
{
    [SerializeField] private TDef m_Def = null;                  // 수치 데이터

    public TDef Def => m_Def;
    protected override UnitDef BaseDef => m_Def;
}
