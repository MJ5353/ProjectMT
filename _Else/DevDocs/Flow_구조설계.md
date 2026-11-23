# 게임 흐름(Flow) 구조 설계

최종 수정: 26.11.23

씬 전환은 GameManager, 씬 안의 진행은 FlowManager와 각 Flow가 맡는다. 공용 서비스와 씬 전용 서비스를 나눠 정리 순서를 한곳에서 관리한다.

## 1. 상세 정보

### 역할과 소유권

| 구성 | 맡는 일 |
|---|---|
| `GameManager` | `SceneDef`·`ESceneType`으로 씬 전환, 로딩 동안 `UIManager` 호출, 전역 데이터 보유 |
| `FlowManager` | 현재 Flow와 공용 `EffectService → UIReuseService → PopupService` 보유, 연결·정리·Tick 전달 |
| `FlowBase` 계열 | 씬 전용 서비스와 진행 상태. 전투는 Unit·HitStop·Spawn·Loot·Battle 서비스를 보유 |
| `SceneControllerBase` | 매니저·UI 루트·카메라 등록, `CreateFlow`로 Flow 생성, 앱 일시정지·포커스 전달 |
| `UIManager` | 씬 UI 루트, `OverlayLoading`, 버튼 클릭 연출. 씬 전환에도 유지 |
| `RootManager` | [Managers] 루트 오브젝트 관리 |

- 서비스는 `FlowManager.Instance.PopupS`, `FlowManager.Instance.Battle.UnitS`처럼 접근한다. 전환 중 Flow가 없으면 공용 접근자는 null이다.
- Screen은 씬 컨트롤러, Popup은 PopupService, Overlay는 UIManager가 보유한다. 클릭 조각은 ResourceManager 풀을 쓰고 표시한 자리별로 회수한다.

### Flow 기반 클래스

- `FlowBase`: 생성자에서 전용 서비스를 등록하고 `Begin → Tick → End`로 관리한다. Init은 등록 순서, Clear는 역순이며 자식은 `OnBegin·OnEnd`를 구현한다.
- `FlowBase<TState>`: StateMachine과 상태 전환·경과 시간을 제공한다. Title·Lobby처럼 상태가 없으면 FlowBase를 직접 상속한다.
- `FlowStateBase<TFlow>`: 주인 Flow를 받아 `Enter·Update·Exit`를 구현한다.
- 서비스 Tick은 공통으로 돌고, 밤 시간·리스폰 등 진행에 따른 일은 상태에서 호출한다.

### 전투 서비스

| 서비스 | 맡는 일 |
|---|---|
| `UnitService` | 유닛 목록·플레이어, 생성·삭제와 위치 조회 |
| `SpawnService` | 맵 슬롯에서 플레이어(정비 적용)·몬스터(지역 배율) 스폰, 빈 슬롯 리스폰 |
| `LootService` | 땅 위 픽업, 주운 버섯·재화, 쓰러질 때 떨구기, 끝난 이유별 보상 정산 |
| `BattleService` | 지역, 밤 시간, 쓰러짐(`PlayerDied`)·부활, 클리어 표시와 저장 |

- Spawn·Loot·Battle은 서로 부르지 않는다. 순서가 필요한 일은 상태가 차례로 부른다(Start: 스폰 → 플레이어 구독, Result: 정산 → 저장).
- UnitService는 바탕이라 다른 서비스가 써도 된다. 그래서 먼저 등록하고, Clear는 역순이라 맨 나중에 비운다.

### 전투 상태와 팝업

| 상태 | 동작 | 밤 시계 |
|---|---|---|
| Start | 스폰·카메라 연결 후 판단을 멈추고 시작 팝업 대기. 클릭하면 Play | 멈춤 |
| Play | 전투 진행. 토벌 종료 확인 후 Market, 사망은 Down, 시간 초과는 Result | 흐름 |
| Down | 버섯을 떨군 뒤 사망 팝업. 골드 부활은 Play, 귀가는 Result | 멈춤 |
| Market | 시장 팝업. 떠나기 확인 또는 시장 종료 후 Result | 멈춤 |
| Result | `EHuntResult`에 따른 보상·결과 표시. 다시 하기 또는 로비 이동 | 멈춤 |

- 전환 요청은 BattleFlow가 상태를 검사해 처리한다. 화면은 표시·요청만 맡고 종료 버튼은 `BattleStateChanged`로 갱신한다.
- Market·Result에서는 판단을 멈춘다. Start 대기를 나갈 때는 `ResumeBrains`로 재개한다.
- 사망·시장·결과 팝업은 `PopupFlowBase<BattleFlow>`를 상속하고 `PopupService.Open<T, TFlow>(type, flow)`로 연다. Flow는 OnOpen 전에 주입하며 확인·개수 조절 팝업은 공용 PopupBase를 유지한다.
- 팝업은 종류당 하나를 재사용하고 씬 전환 때 일괄 정리한다. Result 상태는 나갈 때 결과 팝업을 직접 닫는다.

### 시간 제어

- 게임 시간 정지는 `EPauseReason`(App·Focus·Confirm·Market)을 합산한다. 하나라도 켜져 있으면 timeScale 0이며 건 쪽이 직접 해제한다.
- 밤 시계는 Play에서만 흐른다. 시장·토벌 종료 확인 중에는 게임 시간도 멈추고, UI 연출은 별도 시간으로 동작한다.
- 앱 일시정지·포커스는 SceneControllerBase가 TimeManager에 전달한다. 일시정지는 Flow 상태로 만들지 않고 별도 메뉴도 두지 않는다.

### 전환과 회수

1. **씬 진입:** SceneController가 매니저·화면을 연결하고 `FlowManager.Begin(CreateFlow())`를 호출한다. 공용 서비스 Init → Flow Begin 후 `FlowBegan`을 알린다.
2. **진행:** SceneController Update → FlowManager Tick → 공용 서비스·현재 Flow의 서비스와 상태를 갱신한다.
3. **씬 전환:** 로딩 표시 → Flow End → UI 요소 회수 → 팝업 파괴 → 이펙트 회수 → 씬 로드. 팝업 내부 요소를 먼저 회수하며 OnDestroy는 정리가 누락됐을 때의 안전망이다.
4. **다시 하기:** `GameManager.RestartFlow → FlowManager.Restart`가 같은 Flow·Service를 End → Begin으로 재사용하고 월드 이펙트를 비운다. 공용 UI·맵은 유지하고 유닛·픽업은 Clear에서 풀로 돌려보낸다.

- 맵은 BattleSceneController가 한 번 생성해 Flow에 넘긴다. 다시 시작하면 스폰을 초기화하고 `FlowBegan`으로 HUD·카메라를 새 플레이어에 연결한다.
- 정리·재스폰 동안 OverlayLoading을 최소 노출 시간만큼 표시한다. 씬 재로드는 디버그용으로 남긴다.

## 2. 결정된 사안

- **26.11.21:** 11/17의 ‘Flow 계층 없음’을 바꿨다. 진행 단계가 늘면서 씬 상태와 전투 상태가 중복돼, 씬 전환·진행·서비스 소유권을 분리하기로 했다.
- **26.11.22:** 로딩·클릭 연출은 UIManager, 진행은 Flow가 맡도록 구분했다. 다시 하기는 맵·공용 UI·Flow 인스턴스를 재사용해 재로드와 재할당을 줄이기로 했다.
- **26.11.23:** 스폰·픽업·보상까지 들고 비대해진 BattleService를 Spawn·Loot·Battle 셋으로 나눴다. 서비스 이름만 보고 고칠 곳을 찾을 수 있게 하기 위해서다. 플레이어 참조는 UnitService 하나로 통일했다.

## 3. 미정

- SceneDef에 씬 이름 외 로딩 연출 등의 값을 둘지.
- 시장 정지를 timeScale 대신 전투 서비스의 상태별 Tick으로 바꿀지.
