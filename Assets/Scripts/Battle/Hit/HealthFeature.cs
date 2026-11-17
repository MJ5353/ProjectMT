using System;
using UnityEngine;

// 체력 반응. 피격 반응 중 가장 먼저 실행된다.
public class HealthFeature : MonoBehaviour, IHitReaction
{
    [SerializeField] private int m_MaxHp = 3;

    public int MaxHp => m_MaxHp;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public int Order => 0;

    public event Action Died;

    // ==================================================

    private void Awake()
    {
        Current = m_MaxHp;
    }

    // 데이터 값으로 덮어쓸 때 호출
    public void Init(int maxHp)
    {
        m_MaxHp = maxHp;
        Current = maxHp;
    }

    public void OnHit(in HitParams hit)
    {
        if (IsDead)
            return;

        Current = Mathf.Max(0, Current - hit.Damage);
        if (IsDead)
            Died?.Invoke();
    }
}
