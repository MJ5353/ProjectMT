using System.Collections.Generic;
using UnityEngine;

// 클립 이름으로 애니를 트는 얇은 래퍼. 애니메이터 컨트롤러는 상태 이름 = 클립 이름으로 만든다(BattleSetup 메뉴).
public class AnimFeature : MonoBehaviour
{
    [SerializeField] private Animator m_Animator = null;
    [SerializeField] private float m_DefaultFade = 0.08f;

    private readonly Dictionary<string, float> m_DicLength = new();
    private string m_Current;

    public string Current => m_Current;

    // ==================================================

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        if (m_Animator == null)
            m_Animator = GetComponentInChildren<Animator>();

        m_DicLength.Clear();
        m_Current = null;
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
            return;

        foreach (AnimationClip clip in m_Animator.runtimeAnimatorController.animationClips)
        {
            m_DicLength[clip.name] = clip.length;
        }
    }

    // 같은 클립이 이미 재생 중이면 무시. restart면 처음부터 다시
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
            m_Animator.Play(hash, 0, 0f);
            return;
        }

        m_Animator.CrossFadeInFixedTime(hash, fade < 0f ? m_DefaultFade : fade, 0);
    }

    public float GetLength(string clip)
    {
        return m_DicLength.TryGetValue(clip, out float length) ? length : 0.5f;
    }

    public void SetSpeed(float speed)
    {
        if (m_Animator == null)
            return;

        m_Animator.speed = speed;
    }
}
