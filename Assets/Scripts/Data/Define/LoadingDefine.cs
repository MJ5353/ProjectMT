using System;
using UnityEngine;

// ==================================================

// 씬 전환

[Serializable]
public class LoadingDefine : DefineSpecBase
{
    [field: SerializeField]
    public float MinLoadingTime { get; private set; } = 1f;          // 씬 전환 때 로딩 화면(가짜 로딩)을 최소 이만큼(초) 보여 준다

    public override string DisplayName => "씬 전환";
}
