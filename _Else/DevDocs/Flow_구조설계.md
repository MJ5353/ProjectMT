# 게임 흐름(Flow) 구조 설계

최종 수정: 26.11.21

씬 전환과 씬 안의 진행 단계를 어떻게 나누는지 적는다. 지금 코드(`EGameState`·`BattleService.EBattleState`)를 이 구조로 바꾸기 위한 설계이며, 코드는 아직 바뀌지 않았다.

## 1. 상세 정보

### 지금 구조와 문제

- `GameManager`가 `EGameState`(None·Lobby·Battle·Result)로 씬 전환과 씬 안의 단계를 함께 관리한다. Result는 씬이 없는 상태라 두 개념이 섞여 있다.
- `BattleService`는 따로 `EBattleState`(Playing·Market·Exited·TimedOut·GaveUp)를 가진다. 진행 단계(Market)와 끝난 이유(Exited·TimedOut·GaveUp)가 섞였고, 쓰러짐은 상태가 아니라 `IsPlayerDown` 플래그다.
- 일시정지는 `BattleService`가 `TimeManager.SetPause(bool)`를 직접 부른다. 이유가 하나뿐이라 둘이 겹치면 하나만 풀려도 재개된다.
- 씬 정리 입구가 두 갈래다(`SceneUnloading` 이벤트와 `SceneControllerBase.OnDestroy`). `PopupService.Clear`가 두 번 불린다.
- 씬 진입 완료를 알리는 시점이 없다.

### 역할 나누기

| 클래스 | 종류 | 맡는 것 |
|---|---|---|
| `GameManager` | Manager | 씬 전환(`ChangeScene`), 전환 직전 `FlowManager.End()`, 전역 데이터(`Database`·`Define`) |
| `FlowManager` | Manager | 현재 Flow 하나, 공용 Service(`UIFactory`·`PopupService`), 일시정지 이유 묶음, Tick 전달 |
| `FlowBase` 계열 | 일반 클래스 | 씬 하나의 진행 상태 머신과 그 씬 전용 Service |
| `SceneControllerBase` | 씬 Mono | 매니저 생성, UI 루트·카메라 등록, 어떤 Flow인지 정해 `Begin`, 앱 일시정지·포커스 전달 |

- `GameManager`는 "씬을 넘어 살아남는 것 + 씬 갈아끼우기"만 맡는다. `EGameState`와 전환 허용 표는 없앤다.
- Service 접근자는 `GameManager`에 남기지 않는다. 호출부를 `FlowManager.Instance.PopupS`, `FlowManager.Instance.Battle.UnitS`처럼 바꾼다(26.11.21 대장).

### Flow 기반 클래스

- **`FlowBase`**: Service 목록과 `Register<T>`, 수명 `Enter → Tick → Exit`, 일시정지 통지 `OnPause(bool)`. `Exit`는 상태 머신을 멈춘 뒤 Service를 등록 역순으로 `Clear`한다. `FlowManager`는 이 타입으로만 다룬다.
- **`FlowBase<TState>`**: 기존 `StateMachine<TState>`(Common/Util)를 들고 `Change(TState)`·`Current`를 제공한다.
- **`FlowStateBase<TFlow>`**: `IState` 구현. owner Flow를 받고, **상태의 Update가 이번 프레임에 어떤 Service를 Tick할지 정한다.** Service가 스스로 "지금 진행 중인가"를 검사하지 않는다.

### LobbyFlow

- 상태: Play 하나. 로비 전용 Service가 생기면 여기에 등록한다.

### BattleFlow

| 상태 | 하는 일 | 밤 시계 |
|---|---|---|
| Start | 맵·플레이어·몬스터 스폰, 카메라 대상 지정 후 Play로 | 멈춤 |
| Play | 유닛·이펙트·히트스톱·전투 Service Tick | 흐름 |
| Down | 버섯 떨구는 연출(2초) 뒤 사망 팝업. 부활=Play, 귀가=Result | 멈춤 |
| Market | 현장 시장 팝업. 닫히면 Result | 멈춤 |
| Result | 끝난 이유에 따라 보상 반영, 결과 팝업. 다시 하기=Retry, 로비=씬 전환 | 멈춤 |

- 끝난 이유(나가기·시간 초과·귀가)는 상태가 아니라 Result에 넘기는 값(`EHuntResult` 가칭)이다. `EBattleState`는 없앤다.
- `BattleService`는 스폰·리스폰, 밤 시간, 주운 버섯, 보상 계산 같은 데이터와 처리만 남고, 단계 전환은 BattleFlow 상태가 한다.
- 전용 Service: Unit·Effect·HitStop·Battle. 지역(`MapDef`)은 `BattleSceneController`가 Flow를 만들 때 넘긴다.

### 일시정지

멈추는 대상이 두 가지라 나눈다.

- **게임 시간 정지**(timeScale 0): 앱 비활성, 포커스 끊김, 일시정지 메뉴 등. `EPauseReason` 플래그로 모으고 하나라도 켜져 있으면 멈춘다. `TimeManager.SetPause(bool)`를 `SetPause(EPauseReason, bool)`로 바꾸고, 히트스톱은 지금처럼 따로 둔다.
- **밤 시계 정지**: Down·Market·Result. 게임 시간은 흐르고(연출·팝업 동작) 밤만 멈춘다. Flow 상태가 정한다.
- 일시정지는 FSM 상태로 두지 않는다. 이유가 겹치거나(Down 중 앱 비활성) 복귀할 상태를 따로 기억해야 하는 문제를 피하기 위해서다.
- 앱 일시정지·포커스는 `SceneControllerBase`가 받아 `FlowManager`에 이유로 넘기고, `FlowManager`가 현재 Flow에 `OnPause`를 알린다.

### 전환 순서

1. **씬 진입**: `SceneControllerBase.Awake`(매니저 생성·UI 루트·카메라) → `Start`에서 `FlowManager.Begin(CreateFlow())` → Flow `Enter`(Service 등록·Init, 첫 상태 진입).
2. **진행**: `SceneControllerBase.Update` → `FlowManager.Tick` → 공용 Service Tick + 현재 Flow 상태 Update.
3. **씬 전환**: `GameManager.ChangeScene` → `FlowManager.End()`(Flow `Exit` → `UIFactory` UI 요소 회수 → `PopupService` 팝업 파괴) → 씬 로드. 요소 회수가 팝업 파괴보다 먼저여야 한다(UI 스레드 26.11.21). 정리 입구는 여기 한 곳이고 `SceneUnloading` 이벤트는 이 순서로 대체한다. `OnDestroy`는 정리가 안 된 경우의 안전망만 맡는다.
4. **다시 하기(Retry)**: 씬을 다시 로드하지 않는다. 현재 BattleFlow를 `Exit`로 정리하고 같은 지역으로 새 BattleFlow를 `Begin`한다.

### Retry에서 챙길 것

- 유닛·이펙트·주운 버섯 픽업은 Flow `Exit`의 Service `Clear`에서 모두 풀로 돌려보낸다. 하나라도 남으면 다음 판에 겹친다.
- 맵 프리팹은 지역이 같으므로 두고 재사용할지, 매번 다시 스폰할지 정해야 한다(미정).
- 결과 팝업은 공용 `PopupService`가 들고 있어 Flow `Exit`로는 닫히지 않는다. Result 상태 `Exit`에서 직접 닫는다. 팝업은 닫으면 꺼지고 종류당 하나를 재사용하므로, 다시 열 때 내용을 새로 채워야 한다.
- 전투 HUD처럼 씬에 남는 UI는 새 판 시작을 알아야 한다. BattleFlow `Enter`가 시작 이벤트를 내고 HUD가 이를 받아 초기화한다.
- 카메라 추적 대상은 Start 상태에서 새 플레이어로 다시 지정한다.

## 2. 결정된 사안

- 26.11.21 대장: `EGameState` 방식 대신 `GameManager`=씬 전환, `FlowManager`=전환된 씬의 상태 관리로 나눈다. 11/17 "Flow 계층 없음" 결정을 바꾼다. 이유: 씬 안의 단계가 늘었고 상태 머신이 두 곳(`EGameState`·`EBattleState`)으로 나뉘었다.
- 26.11.21 대장: 씬 전용 Service는 각 Flow가, 공용 Service는 `FlowManager`가 갖는다.
- 26.11.21 대장: Flow 기반 클래스를 둔다.
- 26.11.21 대장: Service 접근자를 `GameManager`에 남기지 않고 호출부를 바꾼다.
- 26.11.21 대장: 일시정지 이유는 앱 비활성·포커스 끊김·Down 등 여러 가지를 고려한다.
- 26.11.21 대장: Retry는 씬 재로드가 아니라 BattleFlow를 정리하고 새로 시작한다.
- 26.11.21 대장(이전 결정 유지): 팝업은 씬이 바뀔 때만 일괄로 날린다.

## 3. 미정

- 일시정지를 FSM 상태가 아닌 이유 플래그로 두는 안(위 "일시정지")은 Claude 제안이며 대장 확인 전이다.
- Retry 때 맵 프리팹 재사용 여부.
- 일시정지 메뉴(UI)를 만들지 여부.
- `SceneNames`의 `[mj] todo)`(씬 이름을 데이터로 연결)와 `ChangeScene`의 키 형태(enum 또는 데이터).
- `GameManager`의 `internal` 메서드 3곳은 이 개편에서 사라지거나 `public`으로 바뀐다(코드 스타일: asmdef 없으니 internal 대신 public).
