using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 플레이어와 몬스터이 같이 쓰는 몸. 이동·애니·체력·피격 부품을 직접 만들고 매 프레임 갱신한다.
// 직렬화 참조 둘은 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다. 프리팹 안의 참조는 UnitProperty에 모은다
public abstract class UnitController<TDef> : PoolableMono, IActor where TDef : UnitDef
{
    [SerializeField] private TDef m_Def = null;                  // 수치 데이터
    [SerializeField] private UnitProperty m_Property = null;     // 프리팹 참조(몸·애니·렌더러·부위 뼈·소켓)

    // core
    public int ActorID { get; private set; }   // ActorService가 꺼낼 때 넣어 준다. 0이면 등록 안 됨
    protected abstract ETeam Team { get; }
    
    private readonly List<UnitFeature> m_ListFeature = new();   // 수명 주기를 함께 돌릴 부품. 등록 순서대로 실행
    private bool m_IsBattleOver;
    
    // feature
    public MoveFeature Move { get; private set; }
    public AnimFeature Anim { get; private set; }
    public HealthFeature Health { get; private set; }
    public HitFeature HitFeature { get; private set; }
    public MeleeAttackFeature Melee { get; private set; }
    public ActionFeature Action { get; private set; }        // 행동 층(이동·공격·대시·피격·사망). 판단 쪽은 여기에 요청만 한다
    public PartFeature Parts { get; protected set; }         // 버섯 부위. 없으면 null
    public HitBoxFeature HitBox { get; protected set; }      // 뼈 판정 모양. 없으면 null

    // get
    public TDef Def => m_Def;
    public UnitProperty Property => m_Property;
    public Transform VisualRoot => m_Property.Animator.transform;
    public CharacterController HurtBody => m_Property.CharacterController;
    public bool IsDead => Health.IsDead;

    // ------------------------------------------------
        
    protected virtual void Set()
    {
        if (m_Def == null || m_Property == null)
        {
            CLogger.Error(GetType().Name, $"Def 또는 UnitProperty가 비어 있습니다(프리팹 다시 굽기): {name}", this);
            enabled = false;

            return;
        }

        Move = AddFeature(new MoveFeature(this, m_Property.CharacterController, m_Def.TurnSpeed));
        Anim = AddFeature(new AnimFeature(this, m_Property.Animator));
        Health = AddFeature(new HealthFeature(this, m_Def.MaxHp));
        HitFeature = AddFeature(new HitFeature(this, Team, m_Def.Hit, m_Property.ListBodyRenderer));
        Melee = AddFeature(new MeleeAttackFeature(this, GetAttacks()));
        Action = AddFeature(new ActionFeature(this, m_Def));

        // 피격 반응 수치가 없으면 체력만 깎이고 반응은 없다
        if (m_Def.Hit == null)
            CLogger.Warning(GetType().Name, $"{m_Def.name}의 HitDef가 비어 있어 피격 반응이 없습니다", this);

        HitFeature.Hit += OnHit;
        Health.Died += OnDied;
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

        BattleService battle = GameManager.Instance?.BattleS;

        // 승패가 나면 입력·AI를 멈추고 제자리에 세운다. 중력만 계속 받는다
        if (m_IsBattleOver == false && battle != null && battle.State != EBattleState.Playing)
        {
            m_IsBattleOver = true;
            Action.Interrupt();
            Move.Stop();
            OnBattleEnded();
        }

        if (m_IsBattleOver == false)
            OnUpdate(deltaTime);

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
        m_IsBattleOver = false;

        foreach (var feature in m_ListFeature)
        {
            feature.Reset();
        }

        OnSpawned();
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
    
    // virtual ---

    protected abstract void OnSpawned();
    protected abstract void OnUpdate(float deltaTime);
    protected abstract void OnHit(HitParams hit);
    protected abstract void OnDied();

    // 전투가 끝난 순간 한 번. 기본은 애니 배속을 되돌리고, 살아 있으면 Idle
    protected virtual void OnBattleEnded()
    {
        Anim.SetSpeed(1f);
        
        if (IsDead == false)
            Anim.Play("Idle");
    }
    
    protected virtual void OnDisable()
    {
        HitFeature?.RestoreFlash();
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (m_Def == null || m_Property == null)
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
        m_Def.CollectAttacks(listAttack);
        return listAttack;
    }
    
    public Transform GetBattleBone(EBattlePartType part)
    {
        return m_Property.GetBattleBone(part);
    }
}
