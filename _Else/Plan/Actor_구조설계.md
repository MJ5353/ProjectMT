# 액터 구조 설계

최종 수정: 26.11.19

전투 액터(플레이어·몬스터)를 어떻게 나누는지와 그 이유를 적는다. 코드를 읽기 전에 이 문서로 큰 그림을 잡는다.

## 1. 다음 단위 작업

Unity에서 컴파일 확인 후 플레이로 손맛(공격 캔슬·대시·콤보·몬스터 예고)과 전투 종료 시 정지가 예전과 같은지 본다. Feature 합치기 후보(HitBox→Hit, Melee→AttackAction, AttackChoice→AIFeature 안) 결정.

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
- [x] ① 행동 층(`ActionFeature`) 만들고 플레이어 이전. `DashFeature`는 대시 행동으로 흡수(26.11.19)
- [x] ② 몬스터를 행동 층으로: Attack·Windup·Hit 상태를 공통 행동으로 흡수(예고는 공격 행동 앞부분), AI 판단은 `AIFeature`로(26.11.19). 사망 뒤 처리(드롭·사라짐)는 판단 상태 Dead로 남김
- [x] ③ 적 찾기를 팀 기준으로(`GetValidPlayer` → `ActorService.FindNearestEnemy(team)`)(26.11.19)
- [x] 뼈 판정 모양을 `HitBoxFeature`로(26.11.19). 부위 번호는 `PartFeature.FindIndex`로
- [x] `EMonsterState` → `EAIState`(AIFeature 쪽으로), `MonsterController.MoveTo` 삭제 → `ActionFeature.SetMove(방향, 속도)`(26.11.19)
- [x] `PartFeature`가 HitFeature 연결·부위 드롭까지 맡음(26.11.19)
- [x] 제네릭 아닌 기반 `UnitController` 도입, `IActor`·`IHitTarget` 삭제(26.11.19). "아무 유닛"은 `UnitController`로 담고, 타입 있는 Def는 `UnitController<TDef>.Def`
- [x] B: 판단을 양쪽 다 `BrainFeature`로(플레이어 `PlayerInputFeature`, 몬스터 `AIFeature`). 대상·스폰 자리는 `AIFeature`로, 전투 종료 정지는 `BrainFeature.Stop`으로(26.11.19)
- [x] C: 피격·사망 → `RequestHit`·`RequestDead`는 기반이 바로 연결, 그 뒤 판단은 `BrainFeature.OnHit/OnDied/OnActionEnded` 훅. 컨트롤러의 `OnUpdate·OnHit·OnDied·OnBattleEnded·OnSpawned` 폐지, `HitParams.Attacker`는 `UnitController`(26.11.19)
- [x] 통합(26.11.19): 팀은 `Owner.Team` 하나, 사망 여부는 `UnitController.IsDead` 하나, 콤보 다음 타는 `ActionFeature.ComboNext`(플레이어·몬스터 공용), 대상·조준은 `TargetFeature`, 공통 애니 이름은 `AnimKey`, `HitFeature`의 PartFeature 필드 제거(`Owner.PartF`)
- [x] A: 층별 폴더 Unit/Core·Data·Feature·Action·Brain(+AI), 남은 Hit 폴더는 Effect로(26.11.19, PC에서 이동 스크립트 실행)

### 보류

- [ ] 대시를 스킬(SkillDef)로 통합: 대시 수치·EnableDash 경로 정리는 이때 같이(26.11.19 대장)
- [ ] 동료·펫 전용 판단 상태(주인 따라가기 등)와 AI 수치 분리(`AIDef`): 실제로 생길 때 만든다.
- [ ] 판정 모양에 부위 번호 대신 부위 종류(`EBattlePartType`)를 달고, 맞았을 때 `PartFeature`가 칸을 찾게 하기(`HitParams`·`HitFeature`·`MeleeAttackFeature`·`ActorService`). 이펙트 작업이 끝난 뒤 진행
- [ ] 부위 파괴 드롭(`MonsterController.OnPartBroken`): `PartFeature`로 옮길 수 있으나 신선도가 몬스터 생사에 달려 이득이 작다.
- [x] 플레이어 상태 머신 전환 → 행동/판단 두 층으로 대체(26.11.19)

## 3. 상세 정보

### 두 층: 판단과 행동

- **판단 층(타입별):** 무엇을 할지 정해 행동 층에 **요청만** 한다. 플레이어는 입력(입력 버퍼·길게 누르기·자동 조준·콤보 리셋), 몬스터는 AI 상태(Idle·Wander·Chase·Flee)와 공격 고르기.
- **행동 층(공통):** `ActionFeature`가 지금 행동 하나를 돌린다. 행동은 이동·공격·대시·피격·사망. 행동마다 자기 시간·시작·갱신·끝·끊길 수 있는지를 스스로 들고 있다.
  - 우선순위: 사망 > 피격 > 대시 > 공격 > 이동. 높은 행동이 요청되면 지금 행동의 `CanInterruptBy`가 허락할 때 끊는다(공격은 판정 + 캔슬 지연 뒤에만 대시·다음 공격 허용, 피격·사망은 항상 허용).
  - 끝난 행동 다음은 이동(대기). "그 뒤 무엇을 할지"는 판단 층이 정한다.
- 이렇게 나누면 상태가 Feature를 감싸는 1:1 껍데기가 생기지 않는다.

### 구성 요소와 역할

| 구성 | 역할 | 예 |
|---|---|---|
| Controller | **조립 허브.** `SetFeatures`(타입별 Feature)와 `CreateBrain`(판단)만 구현하고, 타입 전용 조회(IsValid·기즈모)만 둔다 | `PlayerController`, `MonsterController` |
| BrainFeature | **판단.** 마지막에 등록돼 다른 Feature Tick 뒤·이동 전에 돈다. 입력·AI가 행동을 **요청**하고, 피격·사망·행동 끝 훅에서 다음 판단을 정한다. 전투가 끝나면 스스로 멈춘다 | `PlayerInputFeature`, `AIFeature`(+ AI*State) |
| ActionFeature | **지금 행동 하나**를 돌리고 끊기 규칙을 지킨다 | `RequestAttack`, `RequestDash`, `RequestHit` |
| Feature | **실행과 자기 상태.** 무엇을 할지 스스로 정하지 않고 명령을 받는다 | `MoveFeature.FaceTo`, `MeleeAttackFeature.StartAttack`, `PartFeature.TryHit` |
| Def | **수치.** 읽기 전용 데이터(ScriptableObject) | `UnitDef`, `MonsterDef`, `PlayerDef`, `SkillDef` |
| UnitProperty | **프리팹 참조.** 몸·애니메이터·렌더러·부위 뼈·소켓. 프리팹 굽기가 채운다 | `UnitProperty.ListBonePart` |
| ActorService | **목록과 수명.** 생성·삭제·매 프레임 Tick을 한 곳에서 | `ActorService.Spawn/Despawn/Tick` |

### 폴더

| 폴더 | 내용 |
|---|---|
| Unit/Core | `UnitController`(조립·수명), `UnitFeature`(기반), `UnitProperty`(프리팹 참조), `AnimKey` |
| Unit/Data | 읽기 전용 값: `UnitDef`·`SkillDef`·`AttackSpec`·`HitDef`·`HitShape`·`HitParams`·`EBattlePartType` |
| Unit/Feature | 몸이 가진 상태·능력: Move·Anim·Health·Hit·HitBox·MeleeAttack·Part·Target |
| Unit/Action | 지금 하는 일 하나: `ActionFeature` + Move·Attack·Dash·Hit·Dead |
| Unit/Brain | 무엇을 할지: `BrainFeature`, `PlayerInputFeature`, AI/(`AIFeature`·`AttackChoiceFeature`·`StateMachine`·State/) |
| Player·Monster | 조립 허브 컨트롤러와 타입별 Def |
| Effect | 유닛 밖 연출 서비스: `EffectService`·`ChunkRenderer`·`HitMaterials`·`HitStopService` |

### Feature 역할

Feature는 "유닛이 가진 것(상태+능력)", Action은 "지금 하는 일(시간·끝·끊기)", Brain은 "무엇을 할지"다. 판단/행동 두 층과 별개로 Feature는 둘 다가 쓰는 도구다.

| Feature | 종류 | 담당 | 쓰는 쪽 |
|---|---|---|---|
| MoveFeature | 능력 | CharacterController 이동·중력·넉백·회전. LateTick에서 움직인다 | 행동, HitFeature(넉백) |
| AnimFeature | 능력 | 클립 재생·길이·배속 | 행동, MeleeAttack |
| HealthFeature | 상태 | 체력·`Died` 이벤트 | HitFeature, 컨트롤러, BattleService·HUD |
| HitFeature | 입구 | 맞는 쪽: 팀·무적·사망 거르기 → 부위 → 체력 → 반응(넉백·번쩍·히트스톱·조각), `Hit` 이벤트 | MeleeAttack(때리는 쪽) |
| HitBoxFeature | 상태 | 몸 판정 모양 목록·감싸는 반지름 | ActorService 판정 |
| MeleeAttackFeature | 능력 | 때리는 쪽: 클립 시작·돌진·판정 구간·맞은 대상 기록 | AttackAction만 |
| PartFeature | 상태 | 버섯 배치·내구도·파괴·재생·드롭, `Broken` 이벤트 | HitFeature, HitBox |
| TargetFeature | 상태 | 대상 기억·가장 가까운 적·거리·조준 | Brain |
| ActionFeature | 층 | 행동 하나 돌리기·끊기 규칙·`Ended`·`ComboNext` | Brain, 컨트롤러 |
| BrainFeature | 층 | 판단 기반: 전투 중에만 Think, 피격·사망·행동 끝 훅 | 컨트롤러가 마지막에 등록 |
| AttackChoiceFeature | 상태 | 몬스터 공격 후보·쿨·가중치 고르기 | AIFeature·AI State |

### 규칙

1. **판단은 요청만, 실행은 행동·Feature가.** 판단 쪽은 "언제 무엇을"만 정하고, "어떻게"는 행동과 Feature가 한다.
2. **Feature끼리는 `Owner`로만 찾는다.** 다른 Feature나 프리팹 참조를 필드로 들고 있지 않는다(`Owner.Move`, `Owner.Property`).
3. **위로는 이벤트로만 알린다.** Feature는 컨트롤러를 부르지 않는다(`PartFeature.Broken`, `HealthFeature.Died`, `HitFeature.Hit`).
4. **Def·UnitProperty는 읽기 전용.** 런타임 상태는 Feature가 갖는다.
5. **기반 컨트롤러(`UnitController`)는 조립·수명 주기와 피격/사망 → 행동 요청 연결만.** 공통 동작이 생기면 가상 함수가 아니라 Feature로 만든다.
6. **갱신은 `ActorService.Tick` 한 줄.** 유닛마다 Unity `Update`를 쓰지 않는다. 순회 중 스폰은 목록 뒤에 붙어 다음 프레임부터, 삭제는 모았다가 순회 뒤에 처리한다.

### 흐름 예: 몬스터가 공격한다

1. `AIChaseState`(판단)가 `AttackChoiceFeature.TryPick`으로 공격을 고르고 `Action.RequestAttack`으로 요청한다.
2. `AttackAction`(행동)이 예고가 있으면 움츠리며 대상 쪽으로 돌고, 그 뒤 `Melee.StartAttack`으로 클립을 튼다. 행동 중에는 `AIFeature`가 판단을 쉰다.
3. 매 프레임 `Melee.UpdateAttack`이 돌진·판정을 하고, 맞은 쪽 `HitFeature`가 체력·부위·반응을 처리한다. 맞은 쪽은 `Action.RequestHit`로 피격 행동에 들어간다.
4. 부위가 깨지면 `PartFeature.Broken` 이벤트 → `MonsterController`가 드롭을 만든다.
5. 공격이 끝나면 `Action.Ended` → `AIFeature.OnActionEnded`가 쿨을 걸고, `AttackSpec.Next`가 있으면 다음 타를 요청, 없으면 추적 판단으로 돌아간다.

## 4. 결정된 사안

| 날짜 | 결정 | 이유 |
|---|---|---|
| 26.11.18 | 상속(`UnitController<TDef>` + Player/Monster) + Feature 조합을 유지한다(A안) | 타입이 코드에 드러나 읽기 쉽고 Def를 형 변환 없이 쓴다. Unity에서 흔한 모양이라 처음 보는 사람도 바로 따라온다 |
| 26.11.18 | 몸/두뇌 분리(B안)는 보류 | AI 동료·조종 몬스터·리플레이에 유리하지만 지금 기획에 쓸 곳이 없고 명령 인터페이스 설계 비용이 든다 |
| 26.11.18 | 데이터 주도·ECS형(C안)은 쓰지 않는다 | 이 규모에 과하고 코드를 따라 읽기 어려워진다 |
| 26.11.18 | B안의 "판단과 실행 분리" 원칙만 가져온다 | 상속 구조를 유지하면서도 컨트롤러가 비대해지는 것을 막는다 |
| 26.11.18 | 타입 전용 Feature(PlayerFeature/MonsterFeature)로 컨트롤러를 합치는 안은 하지 않는다 | 컨트롤러 타입 참조처가 많고 프리팹을 다시 구워야 하는 비용에 비해 이득이 작다. 덩어리가 분명한 기능만 Feature로 뺀다 |
| 26.11.19 | 판단(타입별)과 행동(공통) 두 층으로 나눈다. 행동은 `ActionFeature` 하나가 돌린다 | 상태가 Feature를 1:1로 감싸는 구조를 없애고, 이동·공격·대시·피격·사망을 플레이어·몬스터가 같이 쓴다 |
| 26.11.19 | 판단을 `BrainFeature` 하나의 자리로 통일하고(B), 피격·사망 공통 반응은 기반에서 처리(C). 폴더 정리(A)는 그 뒤 | 두 층 구조가 코드 모양으로 드러나지 않았다(플레이어 판단이 컨트롤러 본문, AI만 Tick 규칙 밖, 알림 경로 세 가지) |
| 26.11.19 | A안을 유지하되 컨트롤러는 조립 허브로, 판단·행동은 Feature로 둔다 | A안의 "타입이 코드에 보이는 구조"와 B안의 "두뇌 교체"를 함께 얻는다. AI도 Feature라 동료·펫이 생기면 판단 세트만 바꾼다 |
