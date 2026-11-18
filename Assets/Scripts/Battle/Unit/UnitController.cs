using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 플레이어와 몬스터이 같이 쓰는 몸. 이동·애니·체력·피격 부품을 직접 만들고 매 프레임 갱신한다.
// 직렬화 참조 네 개는 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다
public abstract class UnitController<TDef> : PoolableMono, IActor where TDef : UnitDef
{
    [SerializeField] private TDef m_Def = null;                                  // 수치 데이터
    [SerializeField] private CharacterController m_CharacterController = null;   // 이동·충돌
    [SerializeField] private Animator m_Animator = null;                         // 모델(Model 자식)의 애니메이터
    [SerializeField] private List<Renderer> m_ListBodyRenderer = new();          // 맞을 때 하얗게 번쩍일 몸 렌더러

    // 부위 뼈 --- 프리팹 굽기(BattleSetup.BuildUnit)가 BodyPartMap 표로 채운다.
    // 손으로 넣은 값은 다시 구우면 덮이니 BodyPartMap 표에 뼈 이름을 더한다
    [SerializeField] private SerializableDictionary<EBattlePartType, Transform> m_DicBattleBone = new();       // Hitbox_* 뼈
    [SerializeField] private SerializableDictionary<EMushroomPartType, Transform> m_DicMushroomBone = new();   // Socket_* 뼈

    // core
    public int ActorID { get; private set; }   // ActorService가 꺼낼 때 넣어 준다. 0이면 등록 안 됨
    protected abstract ETeam Team { get; }
    
    private readonly List<UnitFeature> m_ListFeature = new();   // 수명 주기를 함께 돌릴 부품. 등록 순서대로 실행
    private bool m_IsBattleOver;
    
    // hit
    public IReadOnlyList<HitShape> ListHitShape => m_ListHitShape;
    private readonly List<HitShape> m_ListHitShape = new();     // 뼈 판정 모양. Set에서 한 번 만들고 풀 재사용 때도 그대로
    
    public float HitBoundRadius { get; private set; }
    private const float HitBoundMargin = 1.3f; // 애니로 뼈가 뻗는 만큼 감싸는 반지름에 더 주는 배율
    
    // feature
    public MoveFeature Move { get; private set; }
    public AnimFeature Anim { get; private set; }
    public HealthFeature Health { get; private set; }
    public HitFeature HitFeature { get; private set; }
    public MeleeAttackFeature Melee { get; private set; }

    public TDef Def => m_Def;
    public Transform VisualRoot => m_Animator.transform;
    public CharacterController HurtBody => m_CharacterController;
    public bool IsDead => Health.IsDead;

    // ------------------------------------------------

    protected virtual void Set()
    {
        if (m_Def == null)
        {
            CLogger.Error(GetType().Name, $"Def가 비어 있습니다: {name}", this);
            enabled = false;
            
            return;
        }

        Move = AddFeature(new MoveFeature(this, m_CharacterController));
        Anim = AddFeature(new AnimFeature(this, m_Animator));
        Health = AddFeature(new HealthFeature(this, m_Def.MaxHp));
        HitFeature = AddFeature(new HitFeature(this, Team, m_Def.Hit, Health, Move, m_ListBodyRenderer));
        Melee = AddFeature(new MeleeAttackFeature(this, m_Def.ListAttack, m_DicBattleBone));

        // 피격 반응 수치가 없으면 체력만 깎이고 반응은 없다
        if (m_Def.Hit == null)
            CLogger.Warning(GetType().Name, $"{m_Def.name}의 HitDef가 비어 있어 피격 반응이 없습니다", this);

        HitFeature.Hit += OnHit;
        Health.Died += OnDied;
        
        T AddFeature<T>(T feature) where T : UnitFeature
        {
            m_ListFeature.Add(feature);
            return feature;
        }
    }

    protected virtual void OnDisable()
    {
        HitFeature?.RestoreFlash();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        float unscaledDeltaTime = Time.unscaledDeltaTime;
        
        for (int i = 0; i < m_ListFeature.Count; ++i)
        {
            UnitFeature feature = m_ListFeature[i];
            feature.Tick(feature.IsUnscaledTime ? unscaledDeltaTime : deltaTime);
        }

        BattleService battle = GameManager.Instance?.Battle;

        // 승패가 나면 입력·AI를 멈추고 제자리에 세운다. 중력만 계속 받는다
        if (m_IsBattleOver == false && battle != null && battle.State != EBattleState.Playing)
        {
            m_IsBattleOver = true;
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
    
    // ActorFactory가 풀에서 꺼낸 직후. 처음 상태로 되돌린다
    public void OnSpawn(int actorID)
    {
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

    // 풀로 돌아가기 직전
    public void OnDespawn()
    {
        ActorID = 0;
        foreach (var feature in m_ListFeature)
        {
            feature.Clear();
        }
    }

    // 부위 뼈. 없으면 null
    public Transform GetBattleBone(EBattlePartType part)
    {
        return m_DicBattleBone.TryGetValue(part, out Transform bone) ? bone : null;
    }

    public Transform GetMushroomBone(EMushroomPartType part)
    {
        return m_DicMushroomBone.TryGetValue(part, out Transform bone) ? bone : null;
    }

    // 뼈 판정 모양 추가. 감싸는 반지름은 지금(Set 시점) 자세 기준
    // shortcut: 애니로 뼈가 HitBoundMargin보다 멀리 뻗으면 후보에서 빠진다. 그러면 매 프레임 재계산으로 바꾼다
    protected void AddHitShape(HitShape shape)
    {
        m_ListHitShape.Add(shape);

        shape.GetSegment(out Vector3 start, out Vector3 end);
        float reach = Mathf.Max((start - transform.position).magnitude, (end - transform.position).magnitude) + shape.Radius;
        HitBoundRadius = Mathf.Max(HitBoundRadius, reach * HitBoundMargin);
    }

    // 씬 뷰에 공격 판정 범위 표시
    protected virtual void OnDrawGizmosSelected()
    {
        if (m_Def == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        foreach (AttackSpec attack in m_Def.ListAttack)
        {
            Transform bone = GetBattleBone(attack.HitPart);
            Gizmos.DrawWireSphere(MeleeAttackFeature.GetCenter(transform, attack, bone), attack.Radius);
        }
    }
    
    // ==================================================

    // virtual ---

    // 전투가 끝난 순간 한 번. 기본은 애니 배속을 되돌리고, 살아 있으면 Idle
    protected virtual void OnBattleEnded()
    {
        Anim.SetSpeed(1f);
        if (IsDead == false)
            Anim.Play("Idle");
    }

    protected abstract void OnSpawned();
    protected abstract void OnUpdate(float deltaTime);
    protected abstract void OnHit(HitParams hit);
    protected abstract void OnDied();
}
