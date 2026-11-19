# 액터 구조 설계

최종 수정: 26.11.18

전투 액터(플레이어·몬스터)를 어떻게 나누는지와 그 이유를 적는다. 코드를 읽기 전에 이 문서로 큰 그림을 잡는다.

## 1. 다음 단위 작업

보류 항목 중 플레이어 상태 머신 전환을 진행할지 정한다(콤보·입력 버퍼 정리를 함께 한다).

## 2. 전체 작업 TODO 및 진행상황

### 진행

- [x] 프리팹 참조를 `UnitProperty`로 분리(26.11.18)
- [x] 유닛 갱신을 `ActorService.Tick`으로 모음(26.11.18)
- [x] 버섯 부위를 `PartFeature` 하나로(26.11.18)
- [x] 공격 실행(시작·돌진·판정·종료)을 `MeleeAttackFeature`로 공통화(26.11.18)
- [x] 콤보를 `AttackSpec.Next`로 일반화(26.11.18)
- [x] Feature끼리는 `Owner`로만 접근(26.11.18)
- [x] 몬스터 공격 고르기 → `AttackChoiceFeature`(26.11.18)
- [x] 플레이어 대시 → `DashFeature`(26.11.18)

### 보류

- [ ] 플레이어 콤보·입력 버퍼(`m_ComboNext`, 길게 누르기 타이머, 다음 공격 고르기, `m_QueuedAttack`): 입력 처리와 엮여 있어 플레이어 상태 머신 전환 때 함께 정리한다.
- [ ] 부위 파괴 드롭(`MonsterController.OnPartBroken`): `PartFeature`로 옮길 수 있으나 신선도가 몬스터 생사에 달려 이득이 작다.
- [ ] 플레이어 상태를 `StateMachine` + 상태 클래스로(몬스터와 같은 틀). `ChangeState`의 switch todo가 여기서 풀린다.
- [ ] 컨트롤러 하나 + 두뇌(Brain) 분리: AI 동료·조종 몬스터 같은 기획이 생기면 다시 판단한다.

## 3. 상세 정보

### 구성 요소와 역할

| 구성 | 역할 | 예 |
|---|---|---|
| Controller | **판단만** 한다. 입력을 읽거나 AI 상태를 바꾼다. 직접 움직이거나 판정하지 않는다 | `PlayerController`, `MonsterController`, 몬스터 `State/` |
| Feature | **실행과 자기 상태.** 무엇을 할지 스스로 정하지 않고 명령을 받는다 | `MoveFeature.FaceTo`, `MeleeAttackFeature.StartAttack`, `PartFeature.TryHit` |
| Def | **수치.** 읽기 전용 데이터(ScriptableObject) | `UnitDef`, `MonsterDef`, `PlayerDef`, `SkillDef` |
| UnitProperty | **프리팹 참조.** 몸·애니메이터·렌더러·부위 뼈·소켓. 프리팹 굽기가 채운다 | `UnitProperty.ListBonePart` |
| ActorService | **목록과 수명.** 생성·삭제·매 프레임 Tick을 한 곳에서 | `ActorService.Spawn/Despawn/Tick` |

### 규칙

1. **Controller = 판단, Feature = 실행.** 컨트롤러는 "언제 무엇을"만 정하고, "어떻게"는 Feature가 한다.
2. **Feature끼리는 `Owner`로만 찾는다.** 다른 Feature나 프리팹 참조를 필드로 들고 있지 않는다(`Owner.Move`, `Owner.Property`).
3. **위로는 이벤트로만 알린다.** Feature는 컨트롤러를 부르지 않는다(`PartFeature.Broken`, `HealthFeature.Died`, `HitFeature.Hit`).
4. **Def·UnitProperty는 읽기 전용.** 런타임 상태는 Feature가 갖는다.
5. **기반 컨트롤러(`UnitController`)는 수명 주기만.** 공통 동작이 생기면 가상 함수가 아니라 Feature로 만든다.
6. **갱신은 `ActorService.Tick` 한 줄.** 유닛마다 Unity `Update`를 쓰지 않는다. 순회 중 스폰은 목록 뒤에 붙어 다음 프레임부터, 삭제는 모았다가 순회 뒤에 처리한다.

### 흐름 예: 몬스터가 공격한다

1. `MonsterChaseState`(판단)가 거리를 보고 공격을 고른다.
2. `MonsterAttackState.Enter`가 `Melee.StartAttack(spec)`(실행)을 부른다.
3. 매 프레임 `Melee.UpdateAttack(time)`이 돌진·판정을 하고, 맞은 쪽 `HitFeature`가 체력·부위·반응을 처리한다.
4. 부위가 깨지면 `PartFeature.Broken` 이벤트 → `MonsterController`가 드롭을 만든다.
5. 공격이 끝나면 `AttackSpec.Next`가 있으면 다음 타, 없으면 추적으로 돌아간다.

## 4. 결정된 사안

| 날짜 | 결정 | 이유 |
|---|---|---|
| 26.11.18 | 상속(`UnitController<TDef>` + Player/Monster) + Feature 조합을 유지한다(A안) | 타입이 코드에 드러나 읽기 쉽고 Def를 형 변환 없이 쓴다. Unity에서 흔한 모양이라 처음 보는 사람도 바로 따라온다 |
| 26.11.18 | 몸/두뇌 분리(B안)는 보류 | AI 동료·조종 몬스터·리플레이에 유리하지만 지금 기획에 쓸 곳이 없고 명령 인터페이스 설계 비용이 든다 |
| 26.11.18 | 데이터 주도·ECS형(C안)은 쓰지 않는다 | 이 규모에 과하고 코드를 따라 읽기 어려워진다 |
| 26.11.18 | B안의 "판단과 실행 분리" 원칙만 가져온다 | 상속 구조를 유지하면서도 컨트롤러가 비대해지는 것을 막는다 |
| 26.11.18 | 타입 전용 Feature(PlayerFeature/MonsterFeature)로 컨트롤러를 합치는 안은 하지 않는다 | 컨트롤러 타입 참조처가 많고 프리팹을 다시 구워야 하는 비용에 비해 이득이 작다. 덩어리가 분명한 기능만 Feature로 뺀다 |
