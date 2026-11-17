using System.Collections.Generic;
using UnityEngine;

// 플레이어와 몬스터이 같이 쓰는 몸. 이동·애니·체력·피격 부품을 직접 만들고 매 프레임 갱신한다.
// 직렬화 참조 네 개는 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다
public abstract class UnitController<TDef> : PoolableMono, IActor where TDef : UnitDef
{
    [SerializeField] private TDef m_Def = null;                                  // 수치 데이터
    [SerializeField] private CharacterController m_CharacterController = null;   // 이동·충돌
    [SerializeField] private Animator m_Animator = null;                         // 모델(Model 자식)의 애니메이터
    [SerializeField] private List<Renderer> m_ListBodyRenderer = new();          // 맞을 때 하얗게 번쩍일 몸 렌더러

    public TDef Def => m_Def;
    public Transform VisualRoot => m_Animator.transform;
    public MoveFeature Move { get; private set; }
    public AnimFeature Anim { get; private set; }
    public HealthFeature Health { get; private set; }
    public HitFeature HitFeature { get; private set; }
    public CharacterController HurtBody => m_CharacterController;
    public MeleeAttackFeature Melee { get; private set; }
    public bool IsDead => Health.IsDead;

    public int ActorID { get; private set; }   // ActorManagement가 꺼낼 때 넣어 준다. 0이면 등록 안 됨

    protected FlashFeature Flash { get; private set; }
    protected abstract ETeam Team { get; }

    private readonly List<UnitFeature> m_ListFeature = new();   // 수명 주기를 함께 돌릴 부품. 등록 순서대로 실행
    private bool m_IsBattleOver;

    // ==================================================

    protected virtual void Awake()
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
        Flash = AddFeature(new FlashFeature(this, m_ListBodyRenderer));
        HitFeature = AddFeature(new HitFeature(this, Team, m_Def.InvincibleTime, Health));
        Melee = AddFeature(new MeleeAttackFeature(this));

        // 피격 반응은 넣은 순서대로 실행된다. 체력이 먼저라서 뒤 반응은 IsDead를 볼 수 있다
        HitFeature.AddReaction(Health);
        HitFeature.AddReaction(AddFeature(new KnockbackFeature(this, Move, m_Def.KnockbackScale)));
        HitFeature.AddReaction(Flash);
        HitFeature.AddReaction(AddFeature(new HitStopFeature(this)));
        HitFeature.AddReaction(AddFeature(new ChunkBurstFeature(this, m_Def.ChunkColor)));

        HitFeature.Hit += OnHit;
        Health.Died += OnDied;
    }

    protected virtual void OnDisable()
    {
        Flash?.Restore();
    }

    // 부품을 수명 주기 목록에 넣고 그대로 돌려준다
    protected T AddFeature<T>(T feature) where T : UnitFeature
    {
        m_ListFeature.Add(feature);
        return feature;
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

        // 승패가 나면 입력·AI를 멈추고 제자리에 세운다. 중력만 계속 받는다
        BattleSession battle = GameManager.Instance?.Battle;
        if (m_IsBattleOver == false && battle != null && battle.State != EBattleState.Playing)
        {
            m_IsBattleOver = true;
            Move.Stop();
            OnBattleEnded();
        }

        if (m_IsBattleOver == false)
            OnUpdate(deltaTime);

        for (int i = 0; i < m_ListFeature.Count; ++i)
        {
            UnitFeature feature = m_ListFeature[i];
            feature.LateTick(feature.IsUnscaledTime ? unscaledDeltaTime : deltaTime);
        }
    }

    // 씬 뷰에 기본 공격 판정 범위 표시
    private void OnDrawGizmosSelected()
    {
        if (m_Def == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(MeleeAttackFeature.GetCenter(transform, m_Def.Attack), m_Def.Attack.Radius);
    }

    // ActorFactory가 풀에서 꺼낸 직후. 처음 상태로 되돌린다
    public void OnSpawn(int actorID)
    {
        ActorID = actorID;
        m_IsBattleOver = false;

        for (int i = 0; i < m_ListFeature.Count; ++i)
            m_ListFeature[i].Reset();

        OnSpawned();
    }

    // 풀로 돌아가기 직전
    public void OnDespawn()
    {
        ActorID = 0;
        for (int i = 0; i < m_ListFeature.Count; ++i)
            m_ListFeature[i].Clear();
    }

    // ==================================================

    // virtual---

    protected abstract void OnSpawned();
    protected abstract void OnUpdate(float deltaTime);

    // 전투가 끝난 순간 한 번. 기본은 애니 배속을 되돌리고, 살아 있으면 Idle
    protected virtual void OnBattleEnded()
    {
        Anim.SetSpeed(1f);
        if (IsDead == false)
            Anim.Play("Idle");
    }

    protected abstract void OnHit(HitParams hit);
    protected abstract void OnDied();
}
