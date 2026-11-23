using System;
using UnityEngine;

// ==================================================

// 러시(퍼펙트 닷지 반격) 수치 한 벌. PlayerDef.Rush가 들고 RushSub·RushApproachCore가 읽는다. 시간은 실제 시간(플레이어 시간) 기준.
// 화면 연출 값은 Define.Hud(Rush*)

[Serializable]
public class RushSpec
{
    [field: SerializeField]
    public float SlowRate { get; private set; } = 0.05f;        // [mj] todo) 임시값. 러시 중 전체 시간 배율. 몬스터가 이만큼 느려진다(플레이어는 제 속도)

    [field: SerializeField]
    public float Time { get; private set; } = 3f;               // [mj] todo) 임시값. 빈틈 타이머 전체 길이

    [field: SerializeField]
    public float FinishWindow { get; private set; } = 1f;       // 빈틈 타이머 마지막 이 시간 안의 첫 클릭이 막타

    [field: SerializeField]
    public float ApproachSpeed { get; private set; } = 25f;     // [mj] todo) 임시값. 러시 시작 때 퍼닷몹 앞까지 돌진하는 속도

    [field: SerializeField]
    public float ApproachMaxTime { get; private set; } = 0.5f;  // 돌진이 이 시간(플레이어 시간) 안에 못 닿으면(막힘 등) 그 자리에서 시작

    [field: SerializeField]
    public SkillDef Left { get; private set; } = null;          // 러시 좌 베기. 클릭마다 좌·우를 번갈아 낸다. 비면 러시 없음

    [field: SerializeField]
    public SkillDef Right { get; private set; } = null;         // 러시 우 베기

    [field: SerializeField]
    public SkillDef Finisher { get; private set; } = null;      // 러시 막타(점프 내려찍기)
}
