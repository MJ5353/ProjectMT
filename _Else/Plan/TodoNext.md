# 다음 작업 메모

최종 수정: 26.11.18

## 1. 다음 단위 작업

- `MoveFeature.IsTouchingActor`의 캡슐 축을 월드 Y로 고정한다.

## 2. 전체 작업 TODO 및 진행상황

- [ ] 캡슐 축 월드 Y 고정 (`Assets/Scripts/Battle/Unit/MoveFeature.cs`, `IsTouchingActor`)
- [ ] 루트 스케일이 1인지 `BattleSetup.BuildUnit`에서 확인 (미확인)

## 3. 상세 정보

- 현재: `Vector3 axis = m_Transform.up * halfAxis;`
- CharacterController 캡슐은 transform 회전과 상관없이 항상 월드 Y로 서 있다. 지금은 유닛이 Y축으로만 돌아서 문제가 없지만, 기울이는 연출이 들어오면 판정 캡슐이 실제 캡슐과 어긋난다.
- 수정안: `Vector3 axis = Vector3.up * halfAxis; // CharacterController 캡슐은 회전과 무관하게 항상 월드 Y`
- 스케일: radius·height는 `lossyScale`을 반영하지 않는다. 루트 스케일이 1이면 문제없고, 나중에 루트 스케일을 바꾸게 되면 그때 반영한다.

## 4. 결정된 사안

- 26.11.18: 축 고정만 하고 스케일 반영은 미룬다. 이유: 루트는 `BuildUnit`이 새로 만들고 모델은 자식이라 스케일 1이 유지되는 구조.
- 26.11.18: 겹침 쿼리 마스크를 `~0`에서 Actor 레이어로 바꿨다. 이유: 지형 MeshCollider까지 걸려 비용이 크고, 16칸 버퍼가 차면 옆 캐릭터를 놓쳐 올라타기가 재발한다.
