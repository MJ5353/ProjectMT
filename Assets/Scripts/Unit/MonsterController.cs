using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몬스터 조립 허브. 몸(UnitController)에 버섯 부위·판정 모양·사망 보상(DropFeature)을 붙이고, 판단은 AIBrain(AI 프리셋 Def.AI)으로 둔다.
// 판단 상태 클래스는 State/ 폴더(상태마다 파일 하나), 실행은 행동 층(ActionLayer), 수치는 MonsterDef

public class MonsterController : UnitController<MonsterDef>
{
    public override ETeam Team => ETeam.Monster;

    // ------------------------------------------------

    protected override void SetFeatures()
    {
        // 버섯 부위(스폰 때 소켓을 골라 꽂는다)
        PartF = AddFeature(new PartFeature(this, Def.ListPart)); 
        
        // 뼈 판정 모양. 부위 번호를 쓰므로 Parts 다음
        HitF.SetShapes(Def.ListBodyShape); 

        // 죽으면 버섯 드롭·처치 알림
        AddFeature(new DropFeature(this, Def));
    }

    protected override BrainLayer CreateBrain()
    {
        return new AIBrain(this, Def);
    }

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
    
    #region Get

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
