// ==================================================

// 공격 하나의 바닥 예고(AttackAction이 들고 부른다). 몬스터 공격만, 시작부터 판정 시작까지 범위(AttackArea)를 바닥에 그린다.
// 예고 중 대상 쪽으로 도니 매 프레임 따라간다. 채움이 꽉 차는 순간 판정. 꺼내고 돌려놓는 일은 EffectService가 한다
// shortcut: 시작 돌진(Lunge)으로 밀리는 거리와 판정 뼈(HitPart)의 공격 자세 위치는 반영 안 함, 범위가 어긋나 보이면 그때 더한다

public class AttackTelegraph
{
    private TelegraphController m_Telegraph; // 판정 시작까지만 있다

    // ------------------------------------------------

    // isLine이면 돌진 사각형, 아니면 원
    public void Show(bool isLine)
    {
        m_Telegraph = FlowManager.Instance.EffectS?.ShowTelegraph(isLine);
    }

    // progress: 0~1(1이면 판정). 1 이상이면 끈다
    public void Refresh(in AttackArea area, float progress)
    {
        if (m_Telegraph == null)
            return;

        if (progress >= 1f)
        {
            Hide();
            return;
        }

        float diameter = area.Radius * 2f;
        if (m_Telegraph.IsLine)
            m_Telegraph.SetShape(area.Ground - area.Forward * area.Radius, area.Forward, diameter, diameter + area.Length);
        else
            m_Telegraph.SetShape(area.Ground, area.Forward, diameter, diameter);

        m_Telegraph.SetProgress(progress);
    }

    public void Hide()
    {
        FlowManager.Instance.EffectS?.HideTelegraph(m_Telegraph);
        m_Telegraph = null;
    }
}
