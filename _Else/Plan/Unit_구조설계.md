# 유닛 구조 설계

최종 수정: 26.11.19

전투 유닛(플레이어·몬스터·보스·동료)을 어떻게 나누는지와 그 이유를 적는다. 코드를 읽기 전에 이 문서로 큰 그림을 잡는다.

## 1. 다음 단위 작업

Unity에서 컴파일 확인 후 플레이 확인: 프리팹 16개의 컨트롤러가 `UnitController`로 붙어 Def·Property 참조가 유지되는지, 플레이어 대시·몬스터 부위·드롭·보스 포효/다운이 그대로 도는지.

## 2. 전체 작업 TODO 및 진행상황

### 진행

- [x] 프리팹 참조 `UnitProperty`, 갱신 `UnitService.Tick`, 부위 `PartFeature`, Feature끼리는 `Owner`로만(26.11.18)
- [x] 부품·실행기·판단 3층 정리: 행동·판단을 Feature에서 빼고 Tick 순서를 컨트롤러 한 곳에(26.11.19)
- [x] AI를 `AIBrain` 하나 + `AIStateBase` 하나 + `AIDef` 프리셋으로(몬스터·보스·동료 공통)(26.11.19)
- [x] 사망 보상 `DropFeature`(`HealthF.Died`), 숨기 `HitF.IsHidden`(26.11.19)
- [x] 명명 정리: `UnitExecutor`·`UnitBrain`·`TargetSelector`·`AttackSelector`·`UnitService`·`UnitFactory`·`UnitID`(26.11.19)
- [x] 컨트롤러 합치기: `PlayerController`·`MonsterController` 삭제, `UnitController` 하나 + Def 가상 함수(26.11.19, 컴파일 전)

### 보류

- [ ] 대시를 스킬(SkillDef)로 통합: `DashAction`·`EnableDash` 경로 정리는 이때 같이(26.11.19 대장)
- [ ] 판정 모양에 부위 번호 대신 부위 종류(`EBattlePartType`)를 달고 `PartFeature`가 칸을 찾게 하기. 이펙트 작업 뒤 진행
- [ ] 동료 팀: 지금 `MonsterDef.Team`은 Monster라 펫도 몬스터 팀이다. 동료가 실제로 싸울 때 Def에서 팀을 정한다

## 3. 상세 정보

### 세 층: 부품 · 실행기 · 판단

| 층 | 뜻 | 구성 | 컨트롤러 프로퍼티 |
|---|---|---|---|
| 부품(Feature) | 몸이 **가진 것**. 스스로 결정하지 않고 위로는 이벤트로만 알린다 | Move·Anim·Health·Hit·Part·Drop | `MoveF`·`AnimF`·`HealthF`·`HitF`·`PartF` |
| 실행기(UnitExecutor) | **지금 하는 행동 하나**(시간·끝·끊기). 부품을 도구로 쓴다 | Move·Attack·Dash·Hit·Dead Action | `UnitExecutor` |
| 판단(UnitBrain) | **무엇을 할지.** 대상을 고르고 실행기에 요청만 한다 | `PlayerBrain`(입력), `AIBrain`(AI 상태·공격 고르기) | `UnitBrain` |

- 호출은 판단 → 실행기 → 부품 방향으로만. 아래는 위를 이벤트(`Hit`·`Died`·`Ended`·`Broken`)로만 알린다.
- 매 프레임 순서는 `UnitController.Tick` 한 곳: 부품 Tick → 실행기 → 판단 → 부품 LateTick(이동).
- 행동 우선순위: 사망 > 피격 > 대시 > 공격 > 이동. 지금 행동의 `CanInterruptBy`가 허락할 때만 끊는다.

### 컨트롤러 하나 + Def 가상 함수

- `UnitController`는 하나다. 조립·수명·Tick 순서·피격/사망 연결만 하고, **무엇을 붙일지·팀·판단 종류는 Def가 정한다.**
- `UnitDef.Team`: 기본 Monster, `PlayerDef`는 Player.
- `UnitDef.SetFeatures(unit)`: `PlayerDef`는 대시·공격 끊기 시간, `MonsterDef`는 `PartFeature`(`SetPartFeature`)·판정 모양(`HitF.SetShapes`)·`DropFeature`.
- `UnitDef.CreateBrain(unit)`: 기본 `AIBrain`(AI 프리셋), `PlayerDef`는 `PlayerBrain`. AI가 비면 `AIBrain`이 오류 로그 후 기본값으로 돈다.
- `UnitDef.DrawGizmos(unit)`(에디터 전용): `MonsterDef`가 몸 판정 모양·부위 내구도를 그린다.

### AI: AIBrain + AIDef 프리셋

- `AIBrain` 하나가 몬스터·보스·동료를 모두 돌린다. 상태는 `AIStateBase`를 이은 State/ 파일 하나씩이다.
- 수치·종류는 `AIDef`(SO 프리셋 `AI_Normal`·`AI_Burrow`·`AI_Elite`·`AI_Boss`·`AI_Pet`)에 두고, 여러 몬스터가 같은 프리셋을 쓴다(`UnitDef.AI`).
- `EAIType`이 상태 묶음을 고른다: 공통 Idle·Wander·Chase·Dead + Normal/Elite는 Flee, Burrow는 Hide, Boss는 Roar·Down, Pet은 Follow만.
- 보스: 부위가 깨지면 다운, 체력이 `EnrageHpRate` 이하면 광폭화(공용 쿨 감소·분노 스킬 추가) 뒤 포효.
- 숨기(Hide): `HitF.IsHidden`이 켜지면 맞지 않고 대상 찾기·조준(`UnitService`)에서도 빠진다.

### 구성 요소와 역할

| 구성 | 역할 |
|---|---|
| UnitController | 조립·수명 주기, Tick 순서, 피격/사망 → 실행기 요청과 판단 훅 연결, `IsValid` |
| UnitDef·UnitProperty | 읽기 전용 수치와 조립 규칙(가상 함수)·프리팹 참조 |
| UnitService | 유닛 목록과 수명. 생성·삭제·매 프레임 Tick·주변 적 검색(`UnitID`로 관리) |
| UnitFactory | ResourceManager 풀에서 꺼내고(OnSpawn) 돌려보낸다(OnDespawn). `UnitService`만 쓴다 |
| TargetSelector | 판단의 대상 찾기·기억·거리·자동 조준(`UnitBrain.TargetSelector`) |
| AttackSelector | AI의 공격 고르기: 후보·거리·쿨·가중치(`AIBrain.AttackSelector`) |
| DropFeature | 몬스터가 죽으면(`HealthF.Died`) 버섯 드롭·처치 알림 |

### 폴더

| 폴더 | 내용 |
|---|---|
| Scripts/Unit | `UnitController`, `UnitFeature`, `UnitProperty` |
| Unit/Data | `AttackSpec`·`HitShape`·`HitParams`·`EBattlePartType` |
| Unit/Feature | 부품: Move·Anim·Health·Hit·Part·Drop |
| Unit/Action | 실행기: `UnitExecutor` + `UnitAction`과 Move·Attack·Dash·Hit·Dead |
| Unit/Brain | 판단: `UnitBrain`, `TargetSelector`, `PlayerBrain`, `AIBrain`, AI/(`AIStateBase`·`AttackSelector`·State/) |
| Scripts/Data | Def(SO): `UnitDef`·`PlayerDef`·`MonsterDef`·`AIDef`·`SkillDef`·`HitDef` |
| Core/GameManager/UnitService | `UnitService`·`UnitFactory` |

### 규칙

1. **판단은 요청만, 실행은 실행기·부품이.** 판단은 "언제 무엇을"만 정한다.
2. **부품끼리는 `Owner`로만 찾는다.** 다른 부품이나 프리팹 참조를 필드로 들지 않는다.
3. **위로는 이벤트로만 알린다.** 부품·실행기는 컨트롤러나 판단을 부르지 않는다.
4. **Def·UnitProperty는 읽기 전용.** 런타임 상태는 부품·실행기·판단이 갖는다.
5. **갱신은 `UnitService.Tick` 한 줄.** 순회 중 스폰은 다음 프레임부터, 삭제는 순회 뒤에 처리한다.

### 흐름 예: 몬스터가 공격한다

1. `AIChaseStateBase`가 `AttackSelector.TryPick`으로 공격을 고르고 `UnitExecutor.RequestAttack`으로 요청한다.
2. `AttackAction`이 예고·클립·돌진·판정을 하고, 맞은 쪽 `HitFeature`가 체력·부위·반응을 처리한다.
3. 맞은 쪽 컨트롤러가 `UnitExecutor.RequestHit`로 피격 행동에 넣고 판단 훅(`OnHit`)을 부른다.
4. 공격이 끝나면 `UnitExecutor.Ended` → `AIBrain`이 쿨을 걸고, `ComboNext`가 있으면 다음 타를 요청한다.

## 4. 결정된 사안

| 날짜 | 결정 | 이유 |
|---|---|---|
| 26.11.18 | 데이터 주도·ECS형은 쓰지 않는다 | 이 규모에 과하고 코드를 따라 읽기 어렵다 |
| 26.11.19 | 부품·실행기·판단 3층으로 나누고 실행기·판단은 Feature에서 뺀다 | `UnitFeature` 하나가 능력·상태·층을 다 담아 산만했다(대장 지적) |
| 26.11.19 | 몬스터 종류별 상속 계층을 버리고 `AIBrain` 하나 + `AIDef` 프리셋으로 | 상태는 어느 종류에나 끼울 수 있어 클래스가 늘지 않고, 프리셋을 여러 몬스터가 재사용한다 |
| 26.11.19 | 명명을 역할 기준으로(`UnitExecutor`·`UnitBrain`·`TargetSelector`·`AttackSelector`·`Unit*`) | "층"·"액터"가 섞여 읽기 어려웠다(대장 지시) |
| 26.11.19 | `PlayerController`·`MonsterController`를 `UnitController` 하나로 합치고 차이는 Def 가상 함수로 | 두 컨트롤러가 조립 함수 두 개뿐이라 Def에 두는 편이 짧고, 타입 분기 없이 "아무 유닛"을 하나로 다룬다(대장 지시). 이전(26.11.18)의 "상속 유지" 결정을 대체 |
