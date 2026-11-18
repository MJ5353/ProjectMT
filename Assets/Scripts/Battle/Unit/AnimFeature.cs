using System.Collections.Generic;
using UnityEngine;

// ==================================================

// 클립 이름으로 애니를 트는 얇은 래퍼. 애니메이터 컨트롤러는 상태 이름 = 클립 이름으로 만든다(BattleSetup 메뉴).
public class AnimFeature : UnitFeature
{
    private const float DefaultFade = 0.08f;
    private const float UnknownLength = 0.5f;   // 클립이 없을 때 돌려주는 길이

    private readonly Animator m_Animator;
    private readonly Dictionary<string, float> m_DicLength = new();
    
    public string Current => m_Current;
    private string m_Current;

    // ------------------------------------------------
    
    public AnimFeature(IActor owner, Animator animator) : base(owner)
    {
        m_Animator = animator;
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        foreach (AnimationClip clip in m_Animator.runtimeAnimatorController.animationClips)
        {
            m_DicLength[clip.name] = clip.length;
        }
    }

    public override void Reset()
    {
        m_Current = null;
        SetSpeed(1f);
    }

    // 같은 클립이 이미 재생 중이면 무시. restart면 처음부터 다시(fade가 0보다 크면 섞으며 처음부터)
    public void Play(string clip, bool restart = false, float fade = -1f)
    {
        if (m_Animator == null)
            return;

        if (restart == false && m_Current == clip)
            return;

        int hash = Animator.StringToHash(clip);
        if (m_Animator.HasState(0, hash) == false)
            return;

        m_Current = clip;
        if (restart)
        {
            if (fade > 0f)
                m_Animator.CrossFadeInFixedTime(hash, fade, 0, 0f);
            else
                m_Animator.Play(hash, 0, 0f);

            return;
        }

        m_Animator.CrossFadeInFixedTime(hash, fade < 0f ? DefaultFade : fade, 0);
    }

    public float GetLength(string clip)
    {
        return m_DicLength.TryGetValue(clip, out float length) ? length : UnknownLength;
    }

    public void SetSpeed(float speed)
    {
        if (m_Animator.E_Alive() == false)
            return;

        m_Animator.speed = speed;
    }
}
