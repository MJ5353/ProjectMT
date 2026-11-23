using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 공격 아이콘 칸. 큰 아이콘은 지금 쓰는 공격(쉬고 있으면 콤보 첫 타), 위 작은 아이콘들은 지금 이어 쓸 수 있는 스킬
// (콤보 다음 타·위로 치기·꾹 누르기). 이을 수 있는 구간(판정 뒤·콤보 리셋 전)엔 진하게, 아니면 흐리게 보인다.
// 작은 아이콘은 UIListView로 꺼내 쓴다(씬이 바뀌면 UIReuseService가 회수)

public class ScreenBattle_SkillChain : MonoBehaviour
{
    [SerializeField] private Image m_MainIcon = null;
    [SerializeField] private UIListView<ScreenBattle_SkillIcon> m_ChainPool = new(); // 작은 아이콘 줄(가로)

    private const float DimAlpha = 0.4f; // 아직 이을 수 없을 때 작은 아이콘 불투명도

    private readonly Dictionary<AttackSpec, SkillDef> m_DicSkill = new(); // 공격 수치 → 스킬(아이콘)
    private readonly List<SkillDef> m_ListNext = new();
    private readonly List<SkillDef> m_ListShown = new(); // 지금 줄에 보이는 스킬. m_ChainPool 순서
    
    private PlayerDef m_Def;
    private SkillDef m_MainSkill;
    private float m_ShownAlpha = -1f;

    // ------------------------------------------------

    private void Awake()
    {
        m_ChainPool.Init(EUIComponentType.ScreenBattle_SkillIcon);
    }

    private void Update()
    {
        UnitController player = FlowManager.Instance.Battle?.BattleS.Player;
        if (player == null || player.Def is not PlayerDef def)
            return;

        if (m_Def != def)
            BuildLookup(def);

        StateFeature state = player.StateF;
        bool isAttack = state.Current == ECoreState.Attack;

        SkillDef main = isAttack ? Find(state.Attack.Spec) : def.ComboStart;
        if (main != m_MainSkill)
        {
            m_MainSkill = main;
            m_MainIcon.E_SetImage(main.E_Alive()?.Icon);
        }

        // 공격 중이거나, 공격이 끝나고 콤보 리셋 시간 안이면 이어 쓸 수 있다
        bool isCombo = isAttack || (state.IsIdle && state.Attack.Spec != null && state.TimeInAction <= def.ComboResetTime);
        m_ListNext.Clear();
        
        if (isCombo)
        {
            AddNext(Find(state.ComboNext), main);
            AddNext(Find(def.UpAttack), main);
            AddNext(def.HoldAttack, main);
        }

        bool canChain = isAttack ? state.Attack.CanBufferNext : isCombo;
        RefreshChain(canChain ? 1f : DimAlpha);
    }

    // ---

    // 플레이어가 쓰는 스킬 전부: 기본 공격 목록 + 콤보 줄(Next) + 달리기·꾹 누르기
    private void BuildLookup(PlayerDef def)
    {
        m_Def = def;
        m_DicSkill.Clear();
        
        foreach (SkillDef skill in def.ListSkill)
        {
            Register(skill);
        }

        for (SkillDef skill = def.ComboStart; skill != null && m_DicSkill.ContainsKey(skill.Attack) == false; skill = skill.Attack.Next)
        {
            Register(skill);
        }

        Register(def.RunAttack);
        Register(def.HoldAttack);
    }

    private void Register(SkillDef skill)
    {
        if (skill != null && skill.Attack != null)
            m_DicSkill[skill.Attack] = skill;
    }

    private SkillDef Find(AttackSpec attack)
    {
        return attack != null && m_DicSkill.TryGetValue(attack, out SkillDef skill) ? skill : null;
    }

    private void AddNext(SkillDef skill, SkillDef main)
    {
        if (skill != null && skill != main && m_ListNext.Contains(skill) == false)
            m_ListNext.Add(skill);
    }

    // 목록이나 진하기가 바뀔 때만 다시 그린다
    private void RefreshChain(float alpha)
    {
        bool isSame = Mathf.Approximately(alpha, m_ShownAlpha) && m_ListNext.Count == m_ListShown.Count;
        for (int i = 0; isSame && i < m_ListNext.Count; i++)
        {
            isSame = m_ListNext[i] == m_ListShown[i];
        }

        if (isSame)
            return;

        m_ShownAlpha = alpha;
        m_ListShown.Clear();
        m_ListShown.AddRange(m_ListNext);
        m_ChainPool.Refresh(m_ListShown, SetChainIcon);
    }

    private void SetChainIcon(ScreenBattle_SkillIcon icon, SkillDef skill)
    {
        icon.Set(skill.Icon, m_ShownAlpha);
    }
}
