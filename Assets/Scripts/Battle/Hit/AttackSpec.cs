using System;
using UnityEngine;

// 근접 공격 한 종류의 수치. 판정 프레임은 tools/voxel/README.md의 애니 표(30fps)를 따른다.
[Serializable]
public class AttackSpec
{
    public const float Fps = 30f;

    [SerializeField] private string m_Clip = "Attack";   // 재생할 애니 클립 이름
    [SerializeField] private int m_Damage = 1;
    [SerializeField] private int m_HitFrame = 9;          // 판정이 나는 프레임(30fps 기준)
    [SerializeField] private float m_Reach = 1f;          // 몸 앞쪽으로 판정 중심까지 거리
    [SerializeField] private float m_Radius = 0.9f;       // 판정 구 반지름
    [SerializeField] private float m_Height = 0.6f;       // 판정 중심 높이
    [SerializeField] private float m_Knockback = 6f;      // 맞은 쪽을 미는 속도
    [SerializeField] private float m_HitStop = 0.06f;     // 맞을 때 화면이 멈추는 시간(초)
    [SerializeField] private float m_Lunge = 0f;          // 공격 시작 때 앞으로 내딛는 속도

    public string Clip => m_Clip;
    public int Damage => m_Damage;
    public float HitTime => m_HitFrame / Fps;
    public float Reach => m_Reach;
    public float Radius => m_Radius;
    public float Height => m_Height;
    public float Knockback => m_Knockback;
    public float HitStop => m_HitStop;
    public float Lunge => m_Lunge;

    public AttackSpec() { }

    public AttackSpec(string clip, int damage, int hitFrame, float reach, float radius, float height, float knockback, float hitStop, float lunge)
    {
        m_Clip = clip;
        m_Damage = damage;
        m_HitFrame = hitFrame;
        m_Reach = reach;
        m_Radius = radius;
        m_Height = height;
        m_Knockback = knockback;
        m_HitStop = hitStop;
        m_Lunge = lunge;
    }
}
