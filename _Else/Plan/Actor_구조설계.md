# 액터 구조 설계

최종 수정: 26.11.19

전투 액터(플레이어·몬스터)를 어떻게 나누는지와 그 이유를 적는다. 코드를 읽기 전에 이 문서로 큰 그림을 잡는다.

## 1. 다음 단위 작업

Unity에서 3단계(판단 층) 컴파일·플레이 확인: 몬스터 추적·공격 고르기·콤보, 플레이어 자동 조준, 전투 종료 정지.

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
- [x] 3층 정리 ①부품: `HitBoxFeature`를 `HitFeature`에 흡수(`SetShapes`), Health는 따로 유지(26.11.19)
- [x] 3층 정리 ②행동: `ActionFeature` → Feature 아닌 `ActionLayer`, `MeleeAttackFeature`를 `AttackAction`에 흡수, Tick 순서를 컨트롤러에 명시(26.11.19)
- [x] 3층 정리 ③판단: `BrainFeature` → Feature 아닌 `BrainLayer`(`PlayerBrain`·`MonsterBrain`), `TargetFeature` → `BrainLayer.Target`(`TargetSensor`), `AttackChoiceFeature` → `MonsterBrain.AttackChoice`(26.11.19)

### 보류

- [ ] 대시를 스킬(SkillDef)로 통합: 대시 수치·EnableDash 경로 정리는 이때 같이(26.11.19 대장)
- [ ] 동료·펫 전용 판단 상태(주인 따라가기 등)와 AI 수치 분리(`AIDef`): 실제로 생길 때 만든다.
- [ ] 판정 모양에 부위 번호 대신 부위 종류(`EBattlePartType`)를 달고, 맞았을 때 `PartFeature`가 칸을 찾게 하기(`HitParams`·`HitFeature`·`AttackAction`·`ActorService`). 이펙트 작업이 끝난 뒤 진행
- [ ] 부위 파괴 드롭(`MonsterController.OnPartBroken`): `PartFeature`로 옮길 수 있으나 신선도가 몬스터 생사에 달려 이득이 작다.
- [x] 플레이어 상태 머신 전환 → 행동/판단 두 층으로 대체(26.11.19)

## 3. 상세 정보

### 세 층: 부품 · 행동 · 판단

| 층 | 뜻 | 구성 | 컨트롤러 프로퍼티 |
|---|---|---|---|
| 부품(Feature) | 몸이 **가진 것**(상태·능력). 스스로 결정하지 않고, 위로는 이벤트로만 알린다 | Move·Anim·Health·Hit·Part(몬스터) | `MoveF`·`AnimF`·`HealthF`·`HitF`·`PartF` |
| 행동(ActionLayer) | **지금 하는 일 하나**(시간·끝·끊기). 부품을 도구로 쓴다 | Move·Attack·Dash·Hit·Dead Action | `ActionLayer` |
| 판단(BrainLayer) | **무엇을 할지.** 감지(Target)하고 행동에 요청만 한다 | `PlayerBrain`(입력), `MonsterBrain`(AI 상태·공격 고르기) | `BrainLayer` |

- 호출은 판단 → 행동 → 부품 방향으로만. 아래는 위를 부르지 않고 이벤트(`Hit`·`Died`·`Ended`·`Broken`)로 알린다.
- 매 프레임 순서는 `UnitController.Tick` 한 곳: 부품 Tick → 행동 Tick → 판단 Tick → 부품 LateTick(이동).
- 행동 우선순위: 사망 > 피격 > 대시 > 공격 > 이동. 지금 행동의 `CanInterruptBy`가 허락할 때만 끊는다(공격은 판정 + 캔슬 지연 뒤에만 대시·다음 공격, 피격·사망은 항상). 끝난 행동 다음은 이동(대기).

### 구성 요소와 역할

| 구성 | 역할 |
|---|---|
| Controller | **조립 허브.** `SetFeatures`(타입별 부품)와 `CreateBrain`(판단)만 구현하고, 타입 전용 조회(IsValid·기즈모)만 둔다 |
| UnitController(기반) | 조립·수명 주기, Tick 순서, 피격/사망 → 행동 요청과 판단 훅 연결 |
| Def·UnitProperty | 읽기 전용 수치(ScriptableObject)·프리팹 참조 |
| ActorService | 목록과 수명. 생성·삭제·매 프레임 Tick을 한 곳에서 |

### 폴더

| 폴더 | 내용 |
|---|---|
| Unit/Core | `UnitController`, `UnitFeature`(부품 기반), `UnitProperty`, `AnimKey` |
| Unit/Data | 읽기 전용 값: `UnitDef`·`SkillDef`·`AttackSpec`·`HitDef`·`HitShape`·`HitParams`·`EBattlePartType` |
| Unit/Feature | 부품: Move·Anim·Health·Hit·Part |
| Unit/Action | 행동: `ActionLayer` + `UnitAction`과 Move·Attack·Dash·Hit·Dead |
| Unit/Brain | 판단: `BrainLayer`, `TargetSensor`, `PlayerBrain`, AI/(`MonsterBrain`·`AttackChoice`·`StateMachine`·State/) |
| Player·Monster | 조립 허브 컨트롤러와 타입별 Def |
| Effect | 유닛 밖 연출 서비스: `EffectService`·`ChunkRenderer`·`HitMaterials`·`HitStopService` |

### 부품·행동·판단 담당

| 이름 | 층 | 담당 |
|---|---|---|
| MoveFeature | 부품 | CharacterController 이동·중력·넉백·회전. LateTick에서 움직인다 |
| AnimFeature | 부품 | 클립 재생·길이·배속 |
| HealthFeature | 부품 | 체력·`Died`. BattleService·HUD가 직접 본다 |
| HitFeature | 부품 | 맞는 쪽 전부: 몸 판정 모양(`SetShapes`) → 팀·무적·사망 거르기 → 부위 → 체력 → 반응(넉백·번쩍·히트스톱·조각), `Hit` |
| PartFeature | 부품 | 버섯 배치·내구도·파괴·재생·드롭, `Broken` |
| ActionLayer | 행동 | 행동 하나 돌리기·끊기 규칙·`Ended`·`ComboNext` |
| AttackAction | 행동 | 예고·클립·돌진·판정 구간·다시 맞히기·이펙트·타격(때리는 쪽 전부) |
| BrainLayer | 판단 | 전투 중에만 Think, 끝나면 Stop. 피격·사망·행동 끝 훅. `Target`(TargetSensor) 감지 |
| PlayerBrain | 판단 | 입력 버퍼·길게 누르기·자동 조준·콤보 리셋 |
| MonsterBrain | 판단 | AI 상태(Idle·Wander·Chase·Flee·Dead), `AttackChoice`(후보·쿨·가중치), 행동 뒤 쿨·콤보·도망/추적 |

### 규칙

1. **판단은 요청만, 실행은 행동·부품이.** 판단은 "언제 무엇을"만, "어떻게"는 행동과 부품이 한다.
2. **부품끼리는 `Owner`로만 찾는다.** 다른 부품이나 프리팹 참조를 필드로 들지 않는다(`Owner.MoveF`, `Owner.Property`).
3. **위로는 이벤트로만 알린다.** 부품·행동은 컨트롤러나 판단을 부르지 않는다.
4. **Def·UnitProperty는 읽기 전용.** 런타임 상태는 부품·행동·판단이 갖는다.
5. **기반 컨트롤러는 조립·수명·Tick 순서·피격/사망 연결만.** 공통 동작은 가상 함수가 아니라 부품·행동으로 만든다.
6. **갱신은 `ActorService.Tick` 한 줄.** 유닛마다 Unity `Update`를 쓰지 않는다. 순회 중 스폰은 다음 프레임부터, 삭제는 순회 뒤에 처리한다.

### 흐름 예: 몬스터가 공격한다

1. `AIChaseState`(판단)가 `MonsterBrain.AttackChoice.TryPick`으로 공격을 고르고 `ActionLayer.RequestAttack`으로 요청한다.
2. `AttackAction`(행동)이 예고가 있으면 움츠리며 대상 쪽으로 돌고, 그 뒤 클립을 튼다. 행동 중에는 `MonsterBrain`이 판단을 쉰다.
3. 매 프레임 `AttackAction`이 돌진·판정을 하고, 맞은 쪽 `HitFeature`가 체력·부위·반응을 처리한다. 맞은 쪽 컨트롤러가 `ActionLayer.RequestHit`로 피격 행동에 넣고 판단 훅(`OnHit`)을 부른다.
4. 부위가 깨지면 `PartFeature.Broken` 이벤트 → `MonsterController`가 드롭을 만든다.
5. 공격이 끝나면 `ActionLayer.Ended` → `MonsterBrain.OnActionEnded`가 쿨을 걸고, `ActionLayer.ComboNext`가 있으면 다음 타를 요청, 없으면 추적 판단으로 돌아간다.

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
| 26.11.19 | 부품(Feature)·행동(ActionLayer)·판단(BrainLayer) 3층으로 나누고 행동·판단은 Feature에서 뺀다. HitBox→Hit, Melee→AttackAction, Target→BrainLayer, AttackChoice→MonsterBrain. Health는 따로 | `UnitFeature` 하나가 능력·상태·입구·층을 다 담아 산만했다(대장 지적). Feature를 "부품" 한 뜻으로 줄이고 Tick 순서를 한 곳에 적는다 |
