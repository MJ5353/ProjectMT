// ==================================================

// 전투(판정·부위 파괴) 부위 종류. Hitbox_* 뼈에 대응하고, Def는 뼈 이름 대신 이 값을 쓴다. 네발 짐승은 앞다리 = Arm, 뒷다리 = Leg.
// 표에 없는 여분 부위(목, 등 2·3번, 꼬리 끝 등)는 Custom_*을 쓴다. 모델 뼈 이름 → 부위 매핑은 Battle/Editor/BodyPartMap.cs.
// !!) 에셋에 숫자로 저장되므로 번호를 바꾸지 말고 뒤에 추가한다

public enum EBattlePartType
{
    None = 0,       // 없음(공격은 몸 앞)
    Body = 1,       // 몸통
    Head = 2,       // 머리
    Back = 3,       // 등
    Tail = 4,       // 꼬리
    LeftArm = 5,    // 왼팔(네발은 왼 앞다리)
    RightArm = 6,   // 오른팔(네발은 오른 앞다리)
    LeftLeg = 7,    // 왼 다리(네발은 왼 뒷다리)
    RightLeg = 8,   // 오른 다리(네발은 오른 뒷다리)
    Weapon = 9,     // 손에 든 것

    // 자유 칸 --- 몹마다 뜻이 다르다
    Custom_01 = 100,
    Custom_02 = 101,
    Custom_03 = 102,
    Custom_04 = 103,
    Custom_05 = 104,
}
