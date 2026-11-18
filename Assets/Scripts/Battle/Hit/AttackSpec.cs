using System;
using UnityEngine;
using UnityEngine.Serialization;

// ==================================================

// 근접 공격 한 종류의 수치. 판정 프레임은 tools/voxel/README.md의 애니 표(30fps)를 따른다.

[Serializable]
public class AttackSpec
{
    public const float Fps = 30f;
    
    public float HitTime => m_HitFrame / Fps;

    [field: SerializeField, FormerlySerializedAs("m_Clip")]
    public string Clip { get; private set; } = "Attack"; // 재생할 애니 클립 이름

    [field: SerializeField, FormerlySerializedAs("m_Damage")]
    public int Damage { get; private set; } = 1;

    [SerializeField] 
    private int m_HitFrame = 9; // 판정이 나는 프레임(30fps 기준)

    [field: SerializeField, FormerlySerializedAs("m_Reach")]
    public float Reach { get; private set; } = 1f; // 몸 앞쪽으로 판정 중심까지 거리

    [field: SerializeField, FormerlySerializedAs("m_Radius")]
    public float Radius { get; private set; } = 0.9f; // 판정 구 반지름

    [field: SerializeField, FormerlySerializedAs("m_Height")]
    public float Height { get; private set; } = 0.6f; // 판정 중심 높이

    [field: SerializeField, FormerlySerializedAs("m_Knockback")]
    public float Knockback { get; private set; } = 6f; // 맞은 쪽을 미는 속도

    [field: SerializeField, FormerlySerializedAs("m_HitStop")]
    public float HitStop { get; private set; } = 0.06f; // 맞을 때 화면이 멈추는 시간(초)

    [field: SerializeField, FormerlySerializedAs("m_Lunge")]
    public float Lunge { get; private set; } = 0f; // 공격 시작 때 앞으로 내딛는 속도

    // ------------------------------------------------

    public AttackSpec() { }

    public AttackSpec(string clip, int damage, int hitFrame, float reach, float radius, float height, float knockback, float hitStop, float lunge)
    {
        Clip = clip;
        Damage = damage;
        m_HitFrame = hitFrame;
        Reach = reach;
        Radius = radius;
        Height = height;
        Knockback = knockback;
        HitStop = hitStop;
        Lunge = lunge;
    }
}
