using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몬스터 조립 허브. 몸(UnitController)에 판단(AIFeature)·공격 고르기·버섯 부위를 붙이고, 행동이 끝났을 때 다음 판단을 정한다.
// 판단 상태 클래스는 State/ 폴더(상태마다 파일 하나), 실행은 행동 층(ActionFeature), 수치는 MonsterDef
public class MonsterController : UnitController<MonsterDef>
{
    public UnitController TargetActor { get; private set; }         // 노리는 적(다른 팀). 죽거나 사라지면 시야 안에서 다시 찾는다
    public Transform Target => IsTargetValid() ? TargetActor.transform : null;
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastHitDirection { get; private set; }

    // feature
    public AttackChoiceFeature AttackChoice { get; private set; } // 공격 고르기·쿨·콤보
    public AIFeature AI { get; private set; }                   // 판단(대기·배회·추적·도망·사망 뒤)
    
    // get
    public EAIState State => AI.State;
    public float TimeInState => AI.TimeInState;
    protected override ETeam Team => ETeam.Monster;

    // ------------------------------------------------

    protected override void Set()
    {
        base.Set();
        
        if (enabled == false)
            return;

        PartF = AddFeature(new PartFeature(this, Def.ListPart));       // 버섯 부위(스폰 때 소켓을 골라 꽂는다)
        HitBoxF = AddFeature(new HitBoxFeature(this, Def.ListBodyShape)); // 뼈 판정 모양. 부위 번호를 쓰므로 Parts 다음

        AttackChoice = AddFeature(new AttackChoiceFeature(this, Def, Def.AttackCooldown));

        AI = AddFeature(new AIFeature(this));
        AI.Add(EAIState.Idle, new AIIdleState(this));
        AI.Add(EAIState.Wander, new AIWanderState(this));
        AI.Add(EAIState.Chase, new AIChaseState(this));
        AI.Add(EAIState.Flee, new AIFleeState(this));
        AI.Add(EAIState.Dead, new AIDeadState(this));

        ActionF.Ended += OnActionFEnded;
    }

    protected override void OnSpawned()
    {
        HomePosition = transform.position;
        TargetActor = null;
        LastHitDirection = Vector3.zero;
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (IsTargetValid() == false)
            TargetActor = GameManager.Instance?.ActorS?.FindNearestEnemy(transform.position, Def.SightRange, Team);

        AI.UpdateAI(deltaTime);
    }
    
    protected override void OnHit(HitParams hit)
    {
        if (IsDead)
            return;

        LastHitDirection = hit.Direction;
        if (hit.Attacker != null && hit.Attacker.TryGetComponent(out UnitController attacker))
            TargetActor = attacker;

        ActionF.RequestHit();
    }

    protected override void OnDied()
    {
        ActionF.RequestDead();
        AI.Change(EAIState.Dead);
    }
    
    // 진행 중인 상태를 Exit로 정리(애니 배속 복구)하고 AI를 끈다
    protected override void OnBattleEnded()
    {
        AI.Stop();
        base.OnBattleEnded();
    }
    
    // 행동이 끝난 뒤 판단. 공격은 끝날 때마다 쿨을 걸고, 끝까지 했으면 콤보 다음 타. 피격 경직이 풀리면 도망 또는 추적
    private void OnActionFEnded(EActionType action, bool isFinished)
    {
        if (IsDead)
            return;

        switch (action)
        {
            case EActionType.Attack:
                AttackChoice.StartCooldown();
                if (isFinished && AttackChoice.TryContinueCombo())
                    ActionF.RequestAttack(AttackChoice.Current, Vector3.zero, Target);

                break;
            case EActionType.Hit:
                if (isFinished)
                    AI.Change(Def.FleeTime > 0f ? EAIState.Flee : EAIState.Chase);

                break;
        }
    }
    
    public void ChangeState(EAIState next)
    {
        AI.Change(next);
    }

    // ------------------------------------------------
    
    #region Gizmo
    
    // 씬 뷰에 몸 판정 모양(구·캡슐)과 부위 판정 구를 그린다. 선이 뼈 축 방향
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (Def == null)
            return;

        DrawPartGizmos();

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        foreach (HitShapeDef shapeDef in Def.ListBodyShape)
        {
            Transform bone = GetBattleBone(shapeDef.Part);
            if (bone == null)
                continue;

            new HitShape(bone, shapeDef, -1).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);
        }
    }
    
    // 깨지는 부위의 몸 판정 모양 위에 상태색(초록 멀쩡, 빨강 깨짐)과 남은 내구도를 그린다
    private void DrawPartGizmos()
    {
        IReadOnlyList<MonsterDef.PartEntry> listEntry = Def.ListPart;
        for (int i = 0; i < listEntry.Count; i++)
        {
            MonsterDef.PartEntry entry = listEntry[i];
            if (entry.IsBreakable == false)
                continue;

            Transform bone = GetBattleBone(entry.BattlePart);
            HitShapeDef shapeDef = FindBodyShape(entry.BattlePart);
            if (bone == null || shapeDef == null)
                continue;

            PartFeature.Part part = PartF?.GetPart(i);
            Gizmos.color = part != null && part.IsBroken ? Color.red : Color.green;
            new HitShape(bone, shapeDef, i).GetSegment(out Vector3 start, out Vector3 end);
            Gizmos.DrawWireSphere(start, shapeDef.Radius);
            Gizmos.DrawWireSphere(end, shapeDef.Radius);
            Gizmos.DrawLine(start, end);

#if UNITY_EDITOR
            string durability = part != null ? $"{part.Durability}/{entry.Durability}" : $"{entry.Durability}";
            UnityEditor.Handles.Label(end + Vector3.up * shapeDef.Radius, $"{i}:{entry.BattlePart} {durability}");
#endif
        }
    }

    #endregion
    
    # region Get

    // target ---
    
    public Vector3 GetTargetDirection()
    {
        if (Target == null)
            return transform.forward;

        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        return diff.sqrMagnitude > 0.0001f ? diff.normalized : transform.forward;
    }
    
    public float GetTargetDistance()
    {
        if (Target == null)
            return float.MaxValue;

        // 내 캡슐 표면부터 잰다. 피벗 기준이면 반지름이 MaxRange보다 큰 몬스터(보스)가 공격 거리에 못 들어온다
        Vector3 diff = Target.position - transform.position;
        diff.y = 0f;
        float bodyRadius = HurtBody != null ? HurtBody.radius : 0f;
        return Mathf.Max(0f, diff.magnitude - bodyRadius);
    }

    public bool CanSeeTarget()
    {
        if (Target == null)
            return false;

        return GetTargetDistance() <= Def.SightRange;
    }
    
    // 노리는 적이 아직 살아 있는지(풀로 돌아갔거나 죽었으면 false)
    private bool IsTargetValid()
    {
        if (TargetActor == null)
            return false;

        return TargetActor.HitF.IsDead == false && TargetActor.HitF.Team != Team;
    }

    // part ---
    
    private HitShapeDef FindBodyShape(EBattlePartType battlePart)
    {
        foreach (HitShapeDef shapeDef in Def.ListBodyShape)
        {
            if (shapeDef.Part == battlePart)
                return shapeDef;
        }

        return null;
    }
    
    #endregion
}
