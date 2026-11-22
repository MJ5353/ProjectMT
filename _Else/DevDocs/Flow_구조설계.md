# 게임 흐름(Flow) 구조 설계

최종 수정: 26.11.22

씬 전환과 씬 안의 진행 단계를 어떻게 나누는지 적는다. 지금 코드(`EGameState`·`BattleService.EBattleState`)를 이 구조로 바꾸기 위한 설계다. 26.11.22 기준 일시정지 이유 플래그(묶음 1), UI 오버레이 정리, Flow 뼈대와 씬 전환 분리(묶음 2), BattleFlow 상태 나누기(묶음 3), 씬 재로드 없는 다시 하기(묶음 4)가 코드에 반영됐다. 아래 "지금 구조와 문제"는 묶음 2 이전 기준이다.

## 1. 상세 정보

### 지금 구조와 문제

- `GameManager`가 `EGameState`(None·Title·Lobby·Battle·Result)로 씬 전환과 씬 안의 단계를 함께 관리한다. Result는 씬이 없는 상태라 두 개념이 섞여 있다.
- `BattleService`는 따로 `EBattleState`(Playing·Market·Exited·TimedOut·GaveUp)를 가진다. 진행 단계(Market)와 끝난 이유(Exited·TimedOut·GaveUp)가 섞였고, 쓰러짐은 상태가 아니라 `IsPlayerDown` 플래그다.
- (26.11.22 해결) 일시정지가 bool 하나라 토벌 종료 확인 팝업과 시장이 같은 값을 걸고 풀었다. 이유 플래그로 바꿨다(아래 "일시정지").
- 씬 정리 입구가 두 갈래다(`SceneUnloading` 이벤트와 `SceneControllerBase.OnDestroy`). `PopupService.Clear`가 두 번 불린다.
- 씬 진입 완료를 알리는 시점이 없다.
- 26.11.22 추가된 것: Title 씬, 씬 전환 로딩 화면(`ScreenLoading`, GameManager 전환 코루틴), 공용으로 올라간 `EffectService`(씬 옵션 `UseClickBurst`), `UIFactory` → `UIReuseService` 이름 변경, 토벌 종료 확인 팝업(`ScreenBattle` → `PopupConfirm`), 정비 강화 적용(플레이어 스폰 때 `Gear.ApplyTo`).

### 역할 나누기

| 클래스 | 종류 | 맡는 것 |
|---|---|---|
| `GameManager` | Manager | 씬 전환(`ChangeScene`), 전환 동안 `UIManager`에 로딩 화면 켜기·끄기, 전환 직전 `FlowManager.End()`, 전역 데이터(`Database`·`Define`) |
| `UIManager` | Manager | 씬 UI 루트 연결·해제, 오버레이(`OverlayLoading`), 버튼 클릭 버섯 연출(`UIClickBurst`). 씬 전환에 지워지지 않는다(26.11.22 반영) |
| `RootManager` | Manager | [Managers] 루트 오브젝트만(26.11.22, UI 루트는 UIManager로) |
| `FlowManager` | Manager | 현재 Flow 하나, 공용 Service(`EffectService` → `UIReuseService` → `PopupService`)를 한 번 만들어 씬마다 Init·Clear, Tick 전달. 공용 접근자는 Flow가 없을 때(전환 사이) null(26.11.22 반영) |
| `FlowBase` 계열 | 일반 클래스 | 씬 하나의 진행 상태 머신과 그 씬 전용 Service |
| `SceneControllerBase` | 씬 Mono | 매니저 생성, UI 루트·카메라 등록, `CreateFlow()`로 Flow를 정해 `Start`에서 `Begin`, 앱 일시정지·포커스를 `TimeManager`에 전달 |

- `GameManager`는 "씬을 넘어 살아남는 것 + 씬 갈아끼우기"만 맡는다. `EGameState`와 전환 허용 표는 없앤다.
- Service 접근자는 `GameManager`에 남기지 않는다. 호출부를 `FlowManager.Instance.PopupS`, `FlowManager.Instance.Battle.UnitS`처럼 바꾼다(26.11.21 대장).

### UI 분류와 오버레이(26.11.22 반영)

| 분류 | 뜻 | 들고 있는 곳 |
|---|---|---|
| Screen | 씬마다 하나인 기본 화면(`ScreenTitle`·`ScreenLobby`·`ScreenBattle`) | 씬 컨트롤러 |
| Popup | 씬 안에서 쌓이는 창 | `PopupService`(Flow 공용) |
| Overlay | 씬을 넘어 맨 위에 뜨는 것(`OverlayLoading`). 기반 `OverlayBase`(Show·Hide, 자식은 OnShow·OnHide) | `UIManager` |

- 버튼 클릭 버섯 연출(`UIClickBurst`)은 3D 월드 이펙트가 아니라 UI 연출이라 EffectService에서 UIManager로 옮겼다. 조각은 UIReuseService가 아니라 ResourceManager 풀을 직접 쓰고, 띄운 자리(씬 UI 루트·오버레이)별로 회수한다. 씬별 켜기(`UseClickBurst`)는 없앴다.
- 그래서 로딩 중에도 클릭 연출이 끊기지 않고, Flow 공용 Service(Effect·UIReuse·Popup)는 Flow가 끝날 때 전부 회수하면 된다. 이펙트 수명 enum(`ELifetime`)은 필요 없어져 넣지 않는다.

### Flow 기반 클래스

- **`FlowBase`**(26.11.22 반영): 생성자에서 `Register<T>`로 전용 Service를 넣고, 수명은 `Begin → Tick → End`. `Begin`은 등록 순서로 Init 후 `OnBegin`, `End`는 상태 머신을 멈추고 `OnEnd` 뒤 등록 역순으로 `Clear`. 자식은 `OnBegin`·`OnEnd`만 덮어쓴다. `FlowManager`는 이 타입으로만 다룬다.
- **`FlowBase<TState>`**: 기존 `StateMachine<TState>`(Common/Util)를 들고 `AddState`(생성자)·`ChangeState`·`State`·`TimeInState`를 제공한다. 상태가 바뀌면 자식 훅 `OnStateChanged`를 부른다(BattleFlow는 `BattleStateChanged` 이벤트로 알림). 상태가 하나뿐인 Flow는 이걸 쓰지 않고 `FlowBase`를 바로 상속한다.
- **`FlowStateBase<TFlow>`**: `IState` 구현. 주인 Flow를 받고 `Enter`·`Update`·`Exit`를 덮어쓴다.
- (묶음 3 반영) Flow는 전용 Service를 늘 Tick하고(유닛·히트스톱은 모든 상태에서 돈다), 단계에 묶인 일(밤 흐르기·리스폰)은 상태 Update가 `BattleService.AdvanceNight`처럼 직접 부른다. Service는 스스로 "지금 진행 중인가"를 검사하지 않는다. 앱 일시정지를 Flow에 알리는 `OnPause`는 필요해지면 넣는다.

### TitleFlow · LobbyFlow

- 상태 없음(`FlowBase` 바로 상속, 26.11.22 정리). 씬 전용 Service가 생기면 생성자에서 등록하고, 단계가 생기면 `FlowBase<TState>`로 바꾼다.

### BattleFlow

| 상태 | 하는 일 | 밤 시계 |
|---|---|---|
| Start | 맵·플레이어·몬스터 스폰, 카메라 대상 지정 후 Play로 | 멈춤 |
| Play | 유닛·히트스톱·전투 Service Tick. 토벌 종료 버튼은 "토벌을 종료할까요?" 확인 팝업을 거친다(구현됨, 묻는 동안 Confirm 이유로 게임 시간 정지). 예=Market | 흐름 |
| Down | 버섯 떨구는 연출(2초) 뒤 사망 팝업. 부활=Play, 귀가=Result | 멈춤 |
| Market | 현장 시장 팝업. 닫히면 Result | 멈춤 |
| Result | 끝난 이유에 따라 보상 반영, 결과 팝업. 다시 하기=Retry, 로비=씬 전환 | 멈춤 |

- (묶음 3 반영) 사망(Down, `DownPopupDelay` 뒤)·시장(Market)·결과(Result) 팝업은 각 상태가 연다. 결과 팝업은 Result `Exit`가 닫는다. `ScreenBattle`은 표시와 토벌 종료 요청(`BattleFlow.ExitHunt`)만 하고, 종료 버튼은 Play 상태에서만 눌린다. `PlayerDowned`·`BattleEnded` 이벤트는 없앴다.
- 끝난 이유는 `EHuntResult`(Exited·TimedOut·GaveUp)로 `BattleFlow.HuntResult`에 두고 Result가 읽는다. `EBattleState`는 없앴다.
- 요청 입구는 `BattleFlow`(ExitHunt·TryRevive·GiveUp·Retry·TryGoLobby)이고, 현재 상태가 맞지 않으면 무시한다. Market·Result에 들어갈 때 `UnitService.StopBrains`로 모든 유닛 판단을 한 번에 멈춘다(유닛이 매 프레임 Flow 상태를 보지 않는다). 종료 버튼은 `BattleStateChanged`로 켜고 끈다.
- `BattleService`는 스폰(`StartHunt`, Start 상태)·리스폰·밤 시간(`AdvanceNight`, Play 상태)·떨구기(`DropLoot`, Down 상태)·보상(`SetMarketReward`·`Finish`, Result 상태)만 남았다. 쓰러짐은 C# 이벤트 `PlayerDied`로 Flow에 알린다.
- 전용 Service: Unit·HitStop·Battle(이펙트는 공용). 지역(`MapDef`)은 `BattleSceneController`가 Flow를 만들 때 넘긴다.
- (팝업 귀속 반영) Flow에 붙는 팝업은 `PopupFlowBase<TFlow>`를 상속하고 `PopupService.Open<T, TFlow>(type, flow)`로만 열린다(Flow가 OnOpen 전에 들어감, 그냥 `Open<T>`로 열면 에러). 사망·결과·시장 = `PopupFlowBase<BattleFlow>`. 확인·개수 조절은 Flow와 무관한 공용 팝업이라 `PopupBase` 그대로, 값은 연 쪽이 Set으로 넣는다.
- (추가 정리 반영) 토벌 종료 확인 팝업과 Confirm 멈춤도 `BattleFlow.ExitHunt`가 맡는다(화면은 요청만, 확인 팝업 프리팹이 없을 때의 갈래 삭제). 사망·결과 팝업은 BattleFlow 상태만 열기 때문에 `FlowManager.Battle`이 없을 때의 빈 화면 처리를 지웠다.
- (정리안 6~8 반영) 토벌 종료는 늘 Market을 거친다(시장 팝업이 없을 때의 개발용 갈래 삭제). 땅의 픽업은 각자 Update 대신 `BattleService.Tick`이 돌린다. 상태 클래스는 `BattleFlow.States.cs`(partial)로 나눴다.

### 일시정지

멈추는 대상이 두 가지라 나눈다.

- **게임 시간 정지**(timeScale 0): `EPauseReason` 플래그(App·Focus·Confirm·Market)로 모으고 하나라도 켜져 있으면 멈춘다. `TimeManager.SetPause(EPauseReason, bool)`, 이유는 건 쪽이 직접 푼다. 히트스톱은 따로 둔다. 일시정지 메뉴는 두지 않는다(26.11.22 반영).
- **밤 시계 정지**: Down·Market·Result. 게임 시간은 흐르고(연출·팝업 동작) 밤만 멈춘다. Flow 상태가 정한다.
- 일시정지는 FSM 상태로 두지 않는다. 이유가 겹치거나(Down 중 앱 비활성) 복귀할 상태를 따로 기억해야 하는 문제를 피하기 위해서다.
- 앱 일시정지·포커스는 `SceneControllerBase`가 받아 `TimeManager`에 바로 넘긴다. 앱 종료 중(매니저가 먼저 파괴될 수 있음) 판단이 거기 있어서 묶음 2에서도 옮기지 않았다.
- 시장은 지금 Market 이유로 게임 시간 전체를 멈춘다. 상태별 Tick으로 바꿀지는 미정.

### 전환 순서

1. **씬 진입**: `SceneControllerBase.Awake`(매니저 생성·UI 루트·카메라) → `Start`에서 `FlowManager.Begin(CreateFlow())` → 공용 Service Init(Effect → UIReuse → Popup) → Flow `Begin`(전용 Service Init, 첫 상태 진입) → `FlowBegan` 이벤트.
2. **진행**: `SceneControllerBase.Update` → `FlowManager.Tick` → 공용 Service Tick + 현재 Flow 상태 Update.
3. **씬 전환**: `GameManager.ChangeScene(ESceneType)` → 로딩 오버레이 → `FlowManager.End()`(Flow `End` → `UIReuseService` UI 요소 회수 → `PopupService` 팝업 파괴 → `EffectService` 회수) → 씬 로드. 요소 회수가 팝업 파괴보다 먼저여야 한다(UI 스레드 26.11.21). 정리 입구는 여기 한 곳이고 `SceneUnloading` 이벤트는 이 순서로 대체한다. `OnDestroy`는 정리가 안 된 경우의 안전망만 맡는다.
4. **다시 하기(Retry)**(묶음 4 반영): 씬을 다시 로드하지 않는다. `BattleFlow.Retry` → `GameManager.RestartFlow`(로딩 오버레이, 다음 프레임에) → `FlowManager.Restart`: 같은 Flow 인스턴스로 `End` → `EffectService.Clear` → `Begin` → `FlowBegan`. Flow·Service를 새로 만들지 않고(할당 없음) 각 Service의 `Clear`가 처음 상태로 되돌린다. 공용 UIReuse·Popup은 연결을 유지한다(화면이 든 UI 요소가 회수되지 않게). 씬 컨트롤러 `CreateFlow`는 씬마다 한 번만 불리고, 맵도 그때 한 번 만든다.

### Retry에서 챙길 것

- 유닛·이펙트·주운 버섯 픽업은 Flow `Exit`의 Service `Clear`에서 모두 풀로 돌려보낸다. 하나라도 남으면 다음 판에 겹친다.
- 맵 프리팹은 지역이 같으므로 재사용한다. 맵은 BattleFlow가 아니라 그 바깥(씬 또는 BattleSceneController)이 들고, 스폰 지점 점유 상태만 Flow 시작 때 초기화한다.
- 결과 팝업은 공용 `PopupService`가 들고 있어 Flow `Exit`로는 닫히지 않는다. Result 상태 `Exit`에서 직접 닫는다. 팝업은 닫으면 꺼지고 종류당 하나를 재사용하므로, 다시 열 때 내용을 새로 채워야 한다.
- 전투 HUD처럼 씬에 남는 UI는 새 판 시작을 알아야 한다. BattleFlow `Enter`가 시작 이벤트를 내고 HUD가 이를 받아 초기화한다.
- 카메라 추적 대상은 Start 상태에서 새 플레이어로 다시 지정한다. 정비 강화(`Gear.ApplyTo`)도 스폰 때 다시 적용된다.
- (묶음 4 반영) 정리·재스폰 사이는 `OverlayLoading`이 `MinLoadingTime` 동안 덮는다.
- (묶음 4 반영) 맵은 `BattleSceneController`가 처음 `CreateFlow` 때 한 번 만들어 들고 `BattleFlow(mapDef, map)`로 넘긴다.
- (묶음 4 반영) 새 판 시작은 따로 `BattleStarted`를 두지 않고 기존 `FlowBegan`을 쓴다. `ScreenBattle`(체력·주운 목록), `ScreenBattle_UnitHpBars`(남은 바 회수, 새 UnitService 구독), `FollowCamera`(새 플레이어 추적)가 이걸 받는다.
- (묶음 4 반영) 유닛은 `UnitService.Clear`가 풀로 돌려보내고(이전엔 씬과 함께 지워지길 기대해 목록만 비웠다), 땅의 픽업은 `BattleService.SpawnPickup`으로만 꺼내 기억했다가 `Clear`에서 돌려받는다.

### 적용 순서

각 묶음이 끝나면 컴파일되고 지금과 같게 돌아가도록 나눈다.

1. 일시정지 이유 플래그(26.11.22 반영)
2. (26.11.22 반영) Flow 뼈대 + 씬 전환 분리(UI 오버레이 정리는 먼저 반영): FlowManager·Flow 기반, Title·Lobby·BattleFlow(Play 하나), 공용·전용 Service 이전, `SceneDef`, GameManager 씬 전환 전용, 정리 입구 하나, 호출부 교체
3. (26.11.22 반영) BattleFlow 상태 나누기: `EBattleState` 폐지, 팝업을 상태 Enter로
4. (26.11.22 반영) 씬 재로드 없는 Retry
5. (26.11.22 반영) 이름 정리(`MonsterBalanceTable` → `MonsterFormula`)

묶음 2·3은 `ScreenBattle`·`PopupResult`·`PopupDeath` 등 UI 파일도 바꾸므로 UI 작업과 겹치지 않을 때 한다.

## 2. 결정된 사안

- 26.11.21 대장: `EGameState` 방식 대신 `GameManager`=씬 전환, `FlowManager`=전환된 씬의 상태 관리로 나눈다. 11/17 "Flow 계층 없음" 결정을 바꾼다. 이유: 씬 안의 단계가 늘었고 상태 머신이 두 곳(`EGameState`·`EBattleState`)으로 나뉘었다.
- 26.11.21 대장: 씬 전용 Service는 각 Flow가, 공용 Service는 `FlowManager`가 갖는다.
- 26.11.21 대장: Flow 기반 클래스를 둔다.
- 26.11.21 대장: Service 접근자를 `GameManager`에 남기지 않고 호출부를 바꾼다.
- 26.11.21 대장: 일시정지 이유는 앱 비활성·포커스 끊김·Down 등 여러 가지를 고려한다.
- 26.11.21 대장: Retry는 씬 재로드가 아니라 BattleFlow를 정리하고 새로 시작한다.
- 26.11.21 대장(이전 결정 유지): 팝업은 씬이 바뀔 때만 일괄로 날린다.
- 26.11.21 대장: 일시정지는 FSM 상태가 아니라 이유 플래그로 둔다. 일시정지 메뉴는 넣지 않는다.
- 26.11.21 대장: Retry 때 맵은 재사용한다.
- 26.11.21 대장: 토벌 종료 버튼은 확인 팝업("토벌을 종료할까요?" 예/아니오)을 거친다.
- 26.11.21 대장: 씬 이름은 `SceneDef`(DataDef)로 데이터에 연결한다. `SceneNames` 상수와 `[mj] todo)`는 이걸로 대체한다.
- 26.11.21 대장: Table은 편집기용 공식값이라 Def와 헷갈리지 않게 이름을 바꾼다(`MonsterBalanceTable`). `PopupTable`은 `PopupDef`로 대체돼 삭제.
- 26.11.22 대장: 다시 하기는 Flow·Service를 새로 만들지 않고 같은 인스턴스를 Clear 후 다시 Begin한다(GC 방지). 정리안 1~4 반영: Title·Lobby는 상태 없는 `FlowBase`, `GameManager`의 로딩 덮기를 `Loading(work)` 하나로(가짜 로딩은 `SceneLoader.Wait`), `FlowManager.StartCurrent`로 시작·알림 묶기, 유닛 판단 정지·종료 버튼을 상태 변경 시점 처리로.
- 26.11.22 대장: 팝업이 스스로 `FlowManager.Battle`을 찾지 않게 Flow 귀속 기반(`PopupFlowBase<TFlow>`)을 두고, 다른 팝업도 같은 기준(Flow에 붙는가)으로 나눈다.
- 26.11.22 대장: 추가 검토 반영. 쓰러지면 빨려 들던 픽업은 멈추고 다시 떨어진다, 토벌 종료 확인을 BattleFlow로, 사망·결과 팝업 null 처리 삭제, 낡은 주석 정리. 상태 안 `PopupS?.`는 그대로 둔다. Start 대기용 판단 재개(ResumeBrains)는 "시장·맵 진입 플레이 다듬기" 작업으로 넘김.
- 26.11.22 대장: 정리안 6~8 반영. 픽업 움직임은 `BattleService.Tick`이 돌리고(`MushroomPickup.Tick`), `ExitHunt`는 늘 Market으로(시장 없이 Exited로 끝나는 갈래와 `FillReward`의 Exited 처리 삭제), BattleFlow 상태는 partial 파일 `BattleFlow.States.cs`로 분리.
- 26.11.22 묶음 5 반영: `MonsterBalanceTable` → `MonsterFormula`(편집기 자동 배정 공식값, 에셋 `Data/Balance/MonsterFormula.asset`). 옛 에셋은 처음 불러올 때 새 이름으로 옮긴다.
- 26.11.22 묶음 4 반영: 다시 하기는 `GameManager.RestartFlow` → `FlowManager.Restart`(씬 유지, 공용 팝업·UI 요소 유지, 이펙트만 비움). 새 판 알림은 `FlowBegan` 재사용. `ReloadScene`은 DebugPanel 전용으로 남김.
- 26.11.22 묶음 3 반영: BattleFlow 상태 Start·Play·Down·Market·Result, 끝난 이유 `EHuntResult`, 팝업은 상태가 열고 화면은 요청만. Start→Play는 스폰 다음 프레임(상태 Enter 안에서 상태를 바꾸지 않는다).
- 26.11.22: 토벌 종료 확인 팝업이 떠 있는 동안 게임 시간 전체를 멈춘다(구현 기준, Confirm 이유).
- 26.11.22 대장: 위 적용 순서로 진행. 묶음 1 반영.
- 26.11.22 대장: 공용 Service는 FlowManager가 한 번만 만들어 계속 들고 씬마다 연결·해제만 한다.
- 26.11.22 대장: UI 분류에 Overlay를 둔다. `ScreenLoading` → `OverlayLoading`. 클릭 연출은 EffectService에서 빼고 이름(`UIClickBurst`)은 그대로 둔다.
- 26.11.22 대장: 오버레이도 기반 클래스(`OverlayBase`)로 묶는다. 종류가 늘면 `EOverlayType` 표로 넓힌다.
- 26.11.22 대장: `RootManager`는 [Managers] 루트만 맡고, 씬 UI 루트·오버레이·클릭 연출은 새 `UIManager`가 맡는다.
- 26.11.22 대장: 묶음 2 결정 — SceneDef는 씬마다 에셋 + `ESceneType`, `GameManager.ChangeScene(ESceneType)`, Flow 종류는 씬 컨트롤러 `CreateFlow`가 정함. 접근자 `FlowManager.Instance.EffectS/UIReuseS/PopupS`, `.Battle.UnitS/HitStopS/BattleS`. 묶음 2 동안 결과는 `BattleEnded` 이벤트로만, Retry는 씬 재로드 유지. `GameStateChanged` → `FlowBegan`, `SceneUnloading` 폐지.
- 26.11.22 묶음 2 반영: `EGameState`·전환 허용 표·`SceneNames` 삭제. SceneDef 에셋 `Resources/Data/Scenes/Scene_Title·Lobby·Battle`(씬 이름 = `ESceneType` 이름, 에디터 씬 경로도 `nameof(ESceneType.X)`). Retry는 `GameManager.ReloadScene()`. 공용 접근자는 Flow가 없을 때 null을 돌려줘 호출부의 `?.`가 의미를 갖게 했다. DebugPanel 이동 버튼은 Title·Lobby·Battle(지금 씬은 다시 불러오기).

## 3. 미정

- `SceneDef`에 씬 이름 외에 무엇을 둘지(로딩 연출 등). 키는 `ESceneType`으로 정해졌다.
- 시장 정지를 timeScale(Market 이유)로 둘지, Market 상태가 전투 Service를 Tick하지 않는 방식으로 바꿀지.
