# 유닛 구조 설계

최종 수정: 26.11.23

전투 유닛(플레이어·몬스터·보스·동료)을 어떻게 나누는지와 그 이유를 적는다. 코드를 읽기 전에 이 문서로 큰 그림을 잡는다.

## 1. 상세 정보

### 층 구분

| 층 | 뜻 | 구성 |
|---|---|---|
| UnitController | 유닛 하나. 조립·수명·Tick 순서·피격/사망 연결 | 플레이어·몬스터 공통 클래스 하나 |
| Feature | 몸이 **가진 것**. 남이 읽는 데이터·자원. 스스로 결정하지 않고 이벤트로만 알린다 | Stat·Passive·Health·Move·Anim·Hit·Part·Drop·PerfectDodge |
| StateFeature | **지금 무엇을 하는지**(행동·판단). Feature 목록 밖에서 컨트롤러가 직접 Tick | CoreState 하나 + SubState 여러 개 |

- 매 프레임 순서(`UnitController.Tick`): Feature Tick → StateFeature(코어 Step → 서브 Timed → 서브 Unit → 요청 정리 → 코어 Think) → Feature LateTick(이동).
- 히트스톱(deltaTime 0)·멈춤 중에는 서브·요청 정리·Think를 쉰다.
- Feature끼리, 코어·서브에서 Feature로는 `Owner.MoveF`·`Owner.AnimF` 등으로만 찾는다.

### StateFeature: 코어 하나 + 서브 여러 개

- **CoreState**: 몸을 차지하는 일 하나. 자기 시간·끝(`IsFinished`)·끊기 규칙만 안다. AI 코어는 판단을 `Think`에서 한다.
- **SubState**: 코어와 겹쳐 도는 일(입력·대상 감지·공격 선택 등). 수명은 `Unit`(Def가 등록, 유닛과 함께) / `Timed`(끝나면 스스로 빠짐). 알림은 구독 대신 가상 훅 `OnHit`·`OnDied`·`OnPerfectDodge`·`OnCoreChanged`로 받는다.
- **즉시 API**: `RequestAttack`·`RequestDash`·`RequestHit`·`RequestDead`·`Interrupt`(기본 코어로)·`Revive`(사망 → 기본 코어). 끊기 규칙을 통과하면 바로 바꾼다.
- **미룬 요청** `Request(core)`: 서브가 쓴다. 코어 Step 뒤 요청 정리에서 처리하고, 같은 프레임에 둘이면 `pending.CanInterruptBy(new)`일 때만 새 요청으로 바꾼다.
- **예약 공격** `ReserveAttack`: 다음 타 하나(덮어씀). 미룬 요청 다음, 공격으로 끊을 수 있게 되면 그 프레임에 낸다.
- **설정**: `SetDefaultCore`(스폰·멈춤 때 들어갈 코어), `SetAfterHitCore`(피격이 끝나면 갈 코어, 없으면 복귀점), `ChangeCore`(AI 코어 사이 이동, 끊기 규칙 없음), `Stop`/`Resume`(전투 정지: 기본 코어로 두고 서브 Exit), `TryGetSub<T>`.
- 코어가 끝나면 피격은 피격 뒤 코어로, 나머지는 복귀점(마지막 Mode 코어)으로 간다.

### ECoreKind와 끊기 규칙

기준은 "**몸을 묶는 동작인가**"다. 코어는 Kind 하나를 갖고, 자기를 끊을 수 있는 Kind를 `InterruptedBy`에 모은다.

- **Mode**: 몸을 묶지 않고 이동 애니만 쓰며 다음 행동을 고르는 자리. 행동이 끝나면 돌아오는 복귀점.
- **Action**: 전용 클립이나 정해진 움직임으로 몸을 묶는 동작.
- **Dash / Hit / Dead**: 각 하나.

| 코어 | Kind | InterruptedBy |
|---|---|---|
| Free·Idle·Wander·Chase·Flee·Follow | Mode | Action·Dash·Hit·Dead |
| Attack | Action | Hit·Dead (+ 다음 공격: 판정 뒤 캔슬 지연 지나면, 대시: 판정 뒤) |
| Approach(러시 돌진) | Action | Hit·Dead |
| Burrow | Action | Hit·Dead |
| Roar·Down | Action | Dead |
| Dash | Dash | Hit·Dead |
| Hit | Hit | Hit·Dead |
| Dead | Dead | 없음 |

- `ECoreState`는 이름표다. 값을 명시하고 AIDef 에셋에 숫자로 저장되므로 새 값은 맨 뒤에만 붙인다(Move 0 · Idle 1 · Wander 2 · Chase 3 · Flee 4 · Follow 5 · Burrow 6 · Roar 7 · Down 8 · Approach 9 · Attack 10 · Dash 11 · Hit 12 · Dead 13).

### 코어 · 서브 목록

| 구분 | 이름 | 역할 |
|---|---|---|
| 공통 코어 | AttackCore | 예고 → 클립·돌진·판정. 판정·바닥 예고·퍼펙트 닷지 알림은 보조 클래스(AttackHitter·AttackTelegraph·AttackDodgeNotifier) |
| | HitCore | 피격 경직. 경직 중 또 맞으면 처음부터 |
| | DeadCore | 사망. AI 유닛은 먼지·충돌 끄기 뒤 사라짐 |
| | MoveCore | 걷는 Mode 코어의 바탕(FreeCore·AI 코어) |
| 플레이어 코어 | FreeCore | 기본 코어. 입력을 속도·걷기/달리기로 |
| | DashCore | 대시와 쿨(쿨은 코어 밖에서도 돈다). 대시를 쓰는 유닛만 |
| | RushApproachCore | 러시 때 대상 앞까지 돌진 |
| AI 코어(AICore) | Idle | 대기. 대상이 보이면 추적, 잠시 뒤 배회 |
| | Wander | 스폰 자리 근처 배회 |
| | Chase | 추적. 고른 공격을 요청, 놓치면 대기 |
| | Flee | 맞은 뒤 대상 반대로 달아났다가 추적 |
| | Follow | 주인 따라가기(펫) |
| | Burrow | 파고들기 → 땅속 이동(맞지 않음·조준 제외) → 튀어나오기 → 추적 |
| | Roar | 포효(보스 첫 발견·분노) 뒤 추적 |
| | Down | 부위가 깨지면 쓰러져 있다가 추적 |
| 서브 | PlayerInputSub (Unit) | 입력을 읽어 요청·예약만. 퍼펙트 닷지면 RushSub 시작 |
| | RushSub (Timed) | 퍼펙트 닷지 뒤 반격: 슬로우·돌진·연타·막타 |
| | TargetSenseSub (Unit) | 대상 감지·기억, 스폰 자리. `TargetSelector`를 든다 |
| | AttackPickerSub (Unit) | 공격 후보·개별/공용 쿨·가중치 선택, 콤보 다음 타 예약, 분노 |
| | BossWatchSub (Unit) | 보스 어그로·분노 판정, 행동이 없을 때 쓰러짐 > 포효를 끼움 |

### AI 조립

- `AIDef` 프리셋(SO)을 여러 몬스터가 같이 쓴다(`UnitDef.AI`). 조립 칸은 `ListCore`(붙일 AI 코어)·`DefaultCore`·`AfterHitCore`다. 공격·피격·사망은 적지 않는다.
- **서브는 데이터에 없다.** 코어마다 `const EAISub Needs`를 두고 `AICore.GetSubs`가 목록 코어들의 Needs를 합친 뒤 의존을 더한다(공격 선택 → 대상 감지, 보스 감시 → 대상 감지·공격 선택).
- `MonsterDef.SetAI` 순서: 서브 등록(공격 선택 → 대상 감지 → 보스 감시, 이 순서가 Tick 순서) → 코어 생성(생성자에서 `TryGetSub`로 서브를 찾으므로 서브가 먼저) → `SetDefaultCore` → `SetAfterHitCore`. 프리셋이 비면 오류 로그 뒤 기본값(Normal 묶음)으로 돈다.
- 데이터 검사(`DataTools.CheckAI`): AI 코어만·중복 없이, 기본 코어가 목록에 있는지, 피격 뒤 코어가 목록에 있는지(Move면 생략: 피격 전 코어로 복귀).
- 펫 판정은 `DefaultCore == Follow`(플레이어 팀·노려지지 않음·드롭 없음), 보스 판정은 BossWatch 서브 유무(보스 체력바·Occluder 레이어).

| 프리셋 | ListCore | Default | AfterHit |
|---|---|---|---|
| AI_Normal | Idle·Wander·Chase·Flee | Idle | Chase |
| AI_Elite | Idle·Wander·Chase·Flee | Idle | Chase |
| AI_Burrow | Idle·Wander·Chase·Burrow | Idle | Burrow |
| AI_Boss | Idle·Wander·Chase·Roar·Down | Idle | Chase |
| AI_Pet | Follow | Follow | Move(없음) |

### 새 코어 · 서브 추가

- 코어: `ECoreState` 맨 뒤에 값 추가 → `CoreState`(AI면 `AICore`) 상속, `Type`·필요하면 `Kind`·`InterruptedBy` 지정 → AI 코어면 `const Needs`를 두고 `AICore.TryGetNeeds`·`MonsterDef.CreateCore`에 한 줄씩.
- 서브: `SubState` 상속, `Lifetime` 지정, 필요한 훅만 덮어쓴다. AI 서브면 `EAISub`에 비트 추가 → `MonsterDef.SetAI` 등록, 의존이 있으면 `AICore.GetSubs`에 더한다.

### 컨트롤러 하나 + Def 가상 함수

- **무엇을 붙일지·팀은 Def가 정한다.** `UnitDef.Team`(기본 Monster, `PlayerDef`는 Player, 펫은 Player).
- `UnitDef.SetFeatures(unit)`: `PlayerDef`는 대시·퍼펙트 닷지·FreeCore·PlayerInputSub, `MonsterDef`는 부위·판정 모양·드롭·AI(`SetAI`).
- **Def는 스폰 때 `UnitFactory`가 넘긴다.** 프리팹은 몸(`UnitProperty`)만 갖고, Def ID로 Resources에서 찾는다(`UnitDef.PrefabPath`). 이름이 어긋나면 런타임 오류 로그와 데이터 검사에서 잡는다.
- 풀 재사용 시 Def가 같으면 Reset만, 바뀌면 다시 조립한다.

### 규칙

1. **서브·AI 코어는 요청만, 몸은 코어와 Feature가 움직인다.**
2. **Feature끼리는 `Owner`로만 찾는다.** 다른 Feature나 프리팹 참조를 필드로 들지 않는다.
3. **위로는 이벤트·훅으로만 알린다.**
4. **Def·UnitProperty는 읽기 전용.** 런타임 상태는 Feature·코어·서브가 갖는다.
5. **갱신은 `UnitService.Tick` 한 줄.** 순회 중 스폰은 다음 프레임부터, 삭제는 순회 뒤에 처리한다.

### 폴더

| 폴더 | 내용 |
|---|---|
| Scripts/Unit | `UnitController`, `UnitFeature`, `UnitProperty` |
| Unit/Data | `AttackSpec`·`HitShape`·`HitParams`·`EBattlePartType`·`DamageCalculator`·`StatModifier` |
| Unit/Feature | Feature들 |
| Unit/State | `StateFeature`·`CoreState`·`SubState`, Core/(공통·플레이어), Core/Attack/, Core/AI/, Sub/ |
| Scripts/Data | Def(SO): `UnitDef`·`PlayerDef`·`MonsterDef`·`AIDef`·`SkillDef`·`HitDef` |
| Manager/GameManager/UnitService | `UnitService`·`UnitFactory` |

## 2. 결정된 사안

| 날짜 | 결정 | 이유 |
|---|---|---|
| 26.11.18 | 데이터 주도·ECS형은 쓰지 않는다 | 이 규모에 과하고 코드를 따라 읽기 어렵다 |
| 26.11.19 | `UnitController` 하나로 합치고 차이는 Def 가상 함수로 | 타입 분기 없이 "아무 유닛"을 하나로 다룬다(대장 지시) |
| 26.11.19 | 프리팹은 Def에 직렬화하지 않고 Def ID로 찾는다. Def는 스폰 때 넘긴다 | Def↔프리팹 순환 참조 제거, 같은 몸을 여러 Def가 재사용(대장 선택) |
| 26.11.19 | (대체됨) 부품·실행기(UnitExecutor)·판단(UnitBrain/AIBrain) 3층 | 아래 26.11.23 결정으로 대체 |
| 26.11.23 | Executor + Brain을 StateFeature(코어 하나 + 서브 여러 개)로 합친다 | 계층이 많아 헷갈리고, 판단과 실행이 한 흐름이다 |
| 26.11.23 | 숫자 우선순위 대신 코어별 `InterruptedBy` | 숫자 순서는 중간 삽입 시 에셋 값과 순서가 밀린다 |
| 26.11.23 | AI 서브는 데이터 대신 코어의 `Needs`로 | 코어가 필요로 하는 것은 알아서 생겨야 한다 |
| 26.11.23 | Roar·Down·Burrow는 Action | 몸을 묶는 동작이라 복귀점(Mode)이 아니다 |

## 3. 미정

- 대시를 스킬(SkillDef)로 통합할지(`EnableDash`·`DashSpec` 경로 정리는 이때 같이).
