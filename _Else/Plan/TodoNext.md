# 다음 작업 메모

최종 수정: 26.11.18

## 1. 다음 단위 작업

- `ActorHitCandidate`의 캡슐 축을 월드 Y로 고정한다.

## 2. 전체 작업 TODO 및 진행상황

- [ ] 캡슐 축 월드 Y 고정 (`Assets/Scripts/Battle/ActorService.cs`, `ActorHitCandidate` 생성자 `Axis`)
- [ ] 루트 스케일이 1인지 `BattleSetup.BuildUnit`에서 확인 (미확인)
- [ ] 타격 후보 수집 순서 최적화 (`ActorService.CollectNearbyEnemies`)
- [ ] (보류) 액터 공간 격자: 액터 수백 단위가 되면 이동·타격 검색을 같이 격자로 바꾼다

## 3. 상세 정보

- 현재: `Axis = bodyTransform.up;`
- CharacterController 캡슐은 transform 회전과 상관없이 항상 월드 Y로 서 있다. 지금은 유닛이 Y축으로만 돌아서 문제가 없지만, 기울이는 연출이 들어오면 피격 캡슐이 실제 캡슐과 어긋난다.
- 수정안: `Axis = Vector3.up; // CharacterController 캡슐은 회전과 무관하게 항상 월드 Y`
- 스케일: `ActorHitCandidate`는 `lossyScale`을 반영하고 있다. `ActorService.IsBodyNear`는 반영하지 않으며, 루트 스케일이 1이면 문제없다.

### 타격 후보 수집

- 호출 빈도: 매 프레임이 아니라 공격 판정 구간에만 돈다(`MeleeAttackFeature.TickAttack` → `Perform`).
- 현재: 적마다 `ActorHitCandidate`를 먼저 만들고(`TransformPoint`, `lossyScale`) 그다음 `IsNear`로 거리를 본다.
- 수정안: 생성 전에 `actor.transform.position`과 공격 중심의 제곱 거리로 먼저 거른다. 기준 범위는 공격 반지름 + 몸 높이 또는 `HitBoundRadius` 중 큰 값. 먼 적은 후보 생성을 건너뛴다.

### 공간 격자 (보류)

- 사각형 범위로 모으는 방식은 격자가 있어야 검사 대상 수가 줄어든다. 격자 없이 사각형 검사만 하면 원 검사와 비용이 같다.
- 격자를 매 프레임 갱신하는 비용이 N이라 액터 수십 단위에서는 이득이 없다. 수백 단위가 되면 `IsBodyNear`, `CollectNearbyEnemies`, `FindNearestEnemy`가 같이 쓰도록 만든다.

## 4. 결정된 사안

- 26.11.18: 캐릭터끼리 붙었는지 판정을 물리 쿼리(`OverlapCapsuleNonAlloc` + `s_OverlapBuffer`)에서 `ActorService.IsBodyNear` 거리 비교로 바꿨다. 이유: 자기 콜라이더를 거르려고 버퍼가 필요했고, 지형까지 걸려 비용이 컸다. 액터 수가 수십 단위라 전체 순회(N²)로 충분하다.
- 26.11.18: Actor·Terrain 레이어는 유지한다. 이유: 충돌 매트릭스와 지형 판정에 쓸 수 있다.
- 26.11.18: 사각형 범위 검색은 지금 도입하지 않는다. 이유: 격자 없이는 원 거리 비교와 비용이 같고, 액터 수가 적어 격자 갱신 비용이 더 크다.
- 26.11.18: 이동 판정의 축 문제는 캡슐 쿼리를 없애면서 사라졌다. 피격 판정(`ActorHitCandidate`)에는 남아 있다.
