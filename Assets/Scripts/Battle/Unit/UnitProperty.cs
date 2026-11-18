using System;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 프리팹과 엮인 참조 모음. 컨트롤러는 프리팹 안의 Transform·컴포넌트를 여기서만 꺼낸다.
// 프리팹 굽기(BattleSetup.BuildUnit)가 채운다. 손으로 넣은 값은 다시 구우면 덮인다

public class UnitProperty : MonoBehaviour
{
    // 전투 부위 하나: 판정 뼈(Hitbox_*)와 이 부위에 버섯을 꽂을 소켓(Socket_*)들
    [Serializable]
    public class BonePart
    {
        [field: SerializeField]
        public EBattlePartType Part { get; private set; }

        [field: SerializeField]
        public Transform Bone { get; private set; } = null;

        [field: SerializeField]
        public List<Transform> ListSocket { get; private set; } = new(); // 굽기 때 가장 가까운 Hitbox 뼈의 부위로 들어간다

        // ------------------------------------------------

        public BonePart(EBattlePartType part, Transform bone)
        {
            Part = part;
            Bone = bone;
        }
    }

    [field: SerializeField]
    public CharacterController CharacterController { get; private set; } = null;   // 이동·충돌

    [field: SerializeField]
    public Animator Animator { get; private set; } = null;                         // 모델(Model 자식)의 애니메이터

    [field: SerializeField]
    public List<Renderer> ListBodyRenderer { get; private set; } = new();          // 맞을 때 하얗게 번쩍일 몸 렌더러

    [field: SerializeField]
    public List<BonePart> ListBonePart { get; private set; } = new();              // 부위마다 한 칸

    // ------------------------------------------------

    public BonePart GetBonePart(EBattlePartType part)
    {
        if (part == EBattlePartType.None)
            return null;

        foreach (BonePart bonePart in ListBonePart)
        {
            if (bonePart.Part == part)
                return bonePart;
        }

        return null;
    }

    public Transform GetBattleBone(EBattlePartType part)
    {
        return GetBonePart(part)?.Bone;
    }

#if UNITY_EDITOR
    // BattleSetup 메뉴가 프리팹을 구울 때만 쓴다
    public void EditorSetup(CharacterController characterController, Animator animator, List<Renderer> listBodyRenderer, List<BonePart> listBonePart)
    {
        CharacterController = characterController;
        Animator = animator;
        ListBodyRenderer = listBodyRenderer;
        ListBonePart = listBonePart;
    }
#endif
}
