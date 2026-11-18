// ==================================================

// [mj] todo) 지워도 되지 않나?

// 버섯 소켓 자리. Socket_* 뼈에 대응하고, MonsterDef의 버섯 칸이 뼈 이름 대신 이 값을 쓴다.
// 한 모델에만 있는 자리(허벅지, 프릴, 뒤 뿌리 등)는 Custom_*을 쓴다. 뼈 이름 → 자리 매핑은 Battle/Editor/BodyPartMap.cs.
// !!) 에셋에 숫자로 저장되므로 번호를 바꾸지 말고 뒤에 추가한다

public enum EMushroomPartType
{
    None = 0,           // 없음
    Head = 1,           // 머리·정수리
    Back_1 = 2,         // 등 앞쪽(등이 하나면 여기)
    Back_2 = 3,         // 등 가운데·혹
    Back_3 = 4,         // 등 뒤쪽·엉덩이 위
    LeftSide = 5,       // 왼 옆구리
    RightSide = 6,      // 오른 옆구리
    LeftShoulder = 7,   // 왼 어깨
    RightShoulder = 8,  // 오른 어깨
    LeftArm = 9,        // 왼팔·왼 앞다리
    RightArm = 10,      // 오른팔·오른 앞다리
    Tail = 11,          // 꼬리
    TailTip = 12,       // 꼬리 끝

    // 자유 칸 --- 몹마다 뜻이 다르다
    Custom_01 = 100,
    Custom_02 = 101,
    Custom_03 = 102,
    Custom_04 = 103,
    Custom_05 = 104,
}
