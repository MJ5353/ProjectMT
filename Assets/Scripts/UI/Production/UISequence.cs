using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ==================================================

// UI 연출 묶음. 단계를 차례로 쌓아 두고(Tween·Join·Wait·Call·WaitUntil) Play로 돌린다.
// Skip은 다음 WaitUntil(입력 대기 등)까지 남은 단계를 끝 모습으로 바로 넘긴다. 끝까지 WaitUntil이 없으면 끝까지.
// 시간은 unscaled라 시장·전투 종료처럼 timeScale이 멈춰 있어도 돈다. 코루틴은 Play에 넘긴 host에서 돈다.
// 예) seq.Clear().Tween(0.25f, t => ...).Join(0.2f, t => ...).Wait(0.1f).Call(() => ...).Play(this);

public class UISequence
{
    private enum EStepType
    {
        Tween,     // duration 동안 onUpdate(0~1). 끝나야 다음 단계
        Join,      // 시작만 하고 바로 다음 단계로(같이 돈다). 시퀀스는 Join이 다 끝나야 끝난다
        Wait,      // duration만큼 쉰다
        Call,      // 한 번 부른다
        WaitUntil, // 조건이 참이 될 때까지 멈춘다. Skip도 여기서 멈춘다
    }

    private struct Step
    {
        public EStepType Type;
        public float Duration;
        public Action<float> OnUpdate;
        public Action OnCall;
        public Func<bool> Until;
    }

    private class Running
    {
        public Step Step;
        public float Time;
    }

    // ------------------------------------------------
    
    public bool IsPlaying { get; private set; }

    private readonly List<Step> m_ListStep = new();
    private readonly List<Running> m_ListJoin = new(); // 돌고 있는 Join
    
    private MonoBehaviour m_Host;
    private Coroutine m_Routine;
    private Action m_OnComplete;
    private int m_Index;
    private float m_Time;       // 지금 단계에서 흐른 시간
    private bool m_IsSkipping;  // 다음 WaitUntil까지 끝 모습으로 넘기는 중

    public bool IsWaiting => IsPlaying && m_Index < m_ListStep.Count && m_ListStep[m_Index].Type == EStepType.WaitUntil; // WaitUntil에서 멈춰 있다

    // ------------------------------------------------

    // 단계를 비운다(돌고 있으면 멈춘다)
    public UISequence Clear()
    {
        Stop();
        
        m_ListStep.Clear();
        return this;
    }

    public UISequence Tween(float duration, Action<float> onUpdate) => Add(new Step { Type = EStepType.Tween, Duration = duration, OnUpdate = onUpdate });

    public UISequence Join(float duration, Action<float> onUpdate) => Add(new Step { Type = EStepType.Join, Duration = duration, OnUpdate = onUpdate });

    public UISequence Wait(float duration) => Add(new Step { Type = EStepType.Wait, Duration = duration });

    public UISequence Call(Action onCall) => Add(new Step { Type = EStepType.Call, OnCall = onCall });

    public UISequence WaitUntil(Func<bool> until) => Add(new Step { Type = EStepType.WaitUntil, Until = until });

    // 처음부터 돌린다. 첫 프레임 모습은 바로 적용한다. host가 꺼져 있으면 끝 모습으로 바로 끝낸다(WaitUntil은 건너뜀)
    public void Play(MonoBehaviour host, Action onComplete = null)
    {
        Stop();

        m_Host = host;
        m_OnComplete = onComplete;
        m_Index = 0;
        m_Time = 0f;
        m_IsSkipping = false;
        IsPlaying = true;

        if (host == null || host.isActiveAndEnabled == false)
        {
            Complete();
            return;
        }

        if (Advance(0f))
        {
            End();
            return;
        }

        m_Routine = host.StartCoroutine(CoRun());
    }

    // 다음 WaitUntil까지 남은 단계를 끝 모습으로 넘긴다
    public void Skip()
    {
        if (IsPlaying == false)
            return;

        m_IsSkipping = true;
        
        if (Advance(0f))
            End();
    }

    // WaitUntil까지 무시하고 모두 끝 모습으로 넘긴다
    public void Complete()
    {
        if (IsPlaying == false)
            return;

        for (; m_Index < m_ListStep.Count; ++m_Index)
        {
            Finish(m_ListStep[m_Index]);
        }

        FinishJoins();
        End();
    }

    // 지금 모습에서 멈춘다(끝 모습을 적용하지 않고, onComplete도 부르지 않는다)
    public void Stop()
    {
        if (m_Routine != null && m_Host != null)
            m_Host.StopCoroutine(m_Routine);

        m_Routine = null;
        m_ListJoin.Clear();
        IsPlaying = false;
    }

    // ---

    private UISequence Add(Step step)
    {
        m_ListStep.Add(step);
        return this;
    }

    private IEnumerator CoRun()
    {
        while (true)
        {
            yield return null;

            if (Advance(Time.unscaledDeltaTime))
                break;
        }

        m_Routine = null;
        End();
    }

    // dt만큼 진행한다. 다 끝났으면 true
    private bool Advance(float dt)
    {
        TickJoins(dt);

        while (m_Index < m_ListStep.Count)
        {
            Step step = m_ListStep[m_Index];
            if (m_IsSkipping && step.Type != EStepType.WaitUntil)
            {
                Finish(step);
                NextStep();
                continue;
            }

            switch (step.Type)
            {
                case EStepType.Call:
                {
                    step.OnCall?.Invoke();
                    break;
                }
                case EStepType.Join:
                {
                    m_ListJoin.Add(new Running { Step = step });
                    step.OnUpdate?.Invoke(0f);
                    break;
                }
                case EStepType.WaitUntil:
                {
                    if (m_IsSkipping)
                    {
                        m_IsSkipping = false;
                        FinishJoins();
                    }

                    if ((step.Until?.Invoke() ?? true) == false)
                        return false;

                    break;
                }
                case EStepType.Tween:
                case EStepType.Wait:
                {
                    m_Time += dt;
                    dt = 0f;

                    if (m_Time < step.Duration)
                    {
                        step.OnUpdate?.Invoke(m_Time / step.Duration);
                        return false;
                    }

                    step.OnUpdate?.Invoke(1f);
                    break;
                }
            }

            NextStep();
        }

        if (m_IsSkipping)
            FinishJoins();

        return m_ListJoin.Count == 0;
    }

    private void NextStep()
    {
        m_Index++;
        m_Time = 0f;
    }

    private void TickJoins(float dt)
    {
        for (int i = m_ListJoin.Count - 1; i >= 0; --i)
        {
            Running running = m_ListJoin[i];
            running.Time += dt;

            float duration = running.Step.Duration;
            if (running.Time < duration)
            {
                running.Step.OnUpdate?.Invoke(running.Time / duration);
                continue;
            }

            running.Step.OnUpdate?.Invoke(1f);
            m_ListJoin.RemoveAt(i);
        }
    }

    private void FinishJoins()
    {
        foreach (Running running in m_ListJoin)
        {
            running.Step.OnUpdate?.Invoke(1f);
        }

        m_ListJoin.Clear();
    }

    // 한 단계를 끝 모습으로
    private static void Finish(Step step)
    {
        switch (step.Type)
        {
            case EStepType.Tween:
            case EStepType.Join:
            {
                step.OnUpdate?.Invoke(1f);
                break;
            }
            case EStepType.Call:
            {
                step.OnCall?.Invoke();
                break;
            }
        }
    }

    private void End()
    {
        if (m_Routine != null && m_Host != null)
            m_Host.StopCoroutine(m_Routine);

        m_Routine = null;
        IsPlaying = false;

        Action onComplete = m_OnComplete;
        m_OnComplete = null;
        onComplete?.Invoke();
    }
}
