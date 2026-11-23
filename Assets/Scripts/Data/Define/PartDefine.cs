using System;
using UnityEngine;

// ==================================================

// 부위(버섯) 다시 돋기. 깨질 때 튀는 값은 DropDefine.PartDrop

[Serializable]
public class PartDefine : DefineSpecBase
{
    [field: SerializeField]
    public float RegrowTime { get; private set; } = 1.5f;       // [mj] todo) 임시값. 다시 돋은 뒤 원래 크기까지 자라는 시간(초)

    [field: SerializeField]
    public float RegrowStartScale { get; private set; } = 0.3f; // 다시 돋을 때 처음 크기 배율

    public override string DisplayName => "부위";
}
