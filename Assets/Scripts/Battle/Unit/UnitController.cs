using System.Collections.Generic;
using UnityEngine;

// 플레이어와 몬스터이 같이 쓰는 몸. 이동·애니·체력·피격 부품을 직접 만들고 매 프레임 갱신한다.
// 직렬화 참조 네 개는 에디터 메뉴(BattleSetup)가 프리팹을 만들 때 채운다
public abstract class UnitController<TDef> : MonoBehaviour, IHitTarget where TDef : UnitDef
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

    internal ActorManagement Actors { get; private set; }
    internal int ActorID { get; private set; }

    protected FlashFeature Flash { get; private set; }
    protected abstract ETeam Team { get; }

    private ChunkBurstFeature m_ChunkBurst;
    private BattleSession m_Session;
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

        Move = new MoveFeature(m_CharacterController);
        Anim = new AnimFeature(m_Animator);
        Health = new HealthFeature(m_Def.MaxHp);
        Flash = new FlashFeature(m_ListBodyRenderer);

        // 피격 반응은 넣은 순서대로 실행된다. 체력이 먼저라서 뒤 반응은 IsDead를 볼 수 있다
        HitFeature = new HitFeature(Team, m_Def.InvincibleTime, Health);
        HitFeature.AddReaction(Health);
        HitFeature.AddReaction(new KnockbackFeature(Move, m_Def.KnockbackScale));
        HitFeature.AddReaction(Flash);
        HitFeature.AddReaction(new HitStopFeature());
        m_ChunkBurst = new ChunkBurstFeature(transform, m_Def.ChunkColor);
        HitFeature.AddReaction(m_ChunkBurst);

        HitFeature.Hit += OnHit;
        Health.Died += OnDied;
    }

    protected virtual void OnEnable()
    {
        if (HitFeature == null)
            return;

        BattleSession battle = GameManager.Instance?.Flow.GetBattle(gameObject.scene);
        m_Session = battle;
        Actors = battle?.Actors;
        m_ChunkBurst.SetManagement(battle?.Chunks);
        if (Actors != null)
            ActorID = Actors.Register(this);

        Melee = new MeleeAttackFeature(this, Actors);
    }

    protected virtual void OnDisable()
    {
        Actors?.Unregister(ActorID, transform);
        m_Session = null;
        Actors = null;
        ActorID = 0;
        m_ChunkBurst?.SetManagement(null);
        Flash?.Restore();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        HitFeature.Tick(deltaTime);
        Flash.Tick(Time.unscaledDeltaTime);

        // 승패가 나면 입력·AI를 멈추고 제자리에 세운다. 중력만 계속 받는다
        if (m_IsBattleOver == false && m_Session != null && m_Session.State != EBattleState.Playing)
        {
            m_IsBattleOver = true;
            Move.Stop();
            OnBattleEnded();
        }

        if (m_IsBattleOver == false)
            OnUpdate(deltaTime);

        Move.Tick(deltaTime);
    }

    // 씬 뷰에 기본 공격 판정 범위 표시
    private void OnDrawGizmosSelected()
    {
        if (m_Def == null)
            return;

        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(MeleeAttackFeature.GetCenter(transform, m_Def.Attack), m_Def.Attack.Radius);
    }

    // ==================================================

    // virtual---

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
