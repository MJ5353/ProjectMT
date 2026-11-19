using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 몸 판정 모양(뼈마다 구·캡슐). "어디를 맞을 수 있나"만 맡는다. ActorService가 공격 판정 때 주인의 HitBox로 읽는다.
// 목록이 비면 HurtBody 캡슐로 맞는다(플레이어·잡몹). 버섯 부위가 걸린 뼈는 부위 번호(Owner.Parts)를 달아 부위 판정이 된다

public class HitBoxFeature : UnitFeature
{
    public float BoundRadius { get; private set; }       // 몸 중심(transform)에서 모양들을 감싸는 반지름. 후보 거르기용
    private const float BoundMargin = 1.3f; // 애니로 뼈가 뻗는 만큼 감싸는 반지름에 더 주는 배율

    public IReadOnlyList<HitShape> ListShape => m_ListShape;
    private readonly List<HitShape> m_ListShape = new(); // 만들 때 한 번 정하고 풀 재사용 때도 그대로

    public bool HasShape => m_ListShape.Count > 0;

    // ------------------------------------------------

    // 감싸는 반지름은 지금(만들 때) 자세 기준
    // shortcut: 애니로 뼈가 BoundMargin보다 멀리 뻗으면 후보에서 빠진다. 그러면 매 프레임 재계산으로 바꾼다
    public HitBoxFeature(UnitController owner, IReadOnlyList<HitShapeDef> listShapeDef) : base(owner)
    {
        Vector3 center = owner.transform.position;
        foreach (HitShapeDef shapeDef in listShapeDef)
        {
            Transform bone = owner.Property.GetBattleBone(shapeDef.Part);
            if (bone == null)
            {
                CLogger.Warning("HitBoxFeature", $"판정 부위 뼈가 프리팹에 없음: {shapeDef.Part} ({owner.transform.name})", owner.transform);
                continue;
            }

            int partIndex = owner.Parts != null ? owner.Parts.FindIndex(shapeDef.Part) : -1;
            HitShape shape = new HitShape(bone, shapeDef, partIndex);
            m_ListShape.Add(shape);

            shape.GetSegment(out Vector3 start, out Vector3 end);
            float reach = Mathf.Max((start - center).magnitude, (end - center).magnitude) + shape.Radius;
            BoundRadius = Mathf.Max(BoundRadius, reach * BoundMargin);
        }
    }
}
