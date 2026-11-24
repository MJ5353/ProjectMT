# 버섯을 줍자! (MushTaker)
#### 7일간의 AI Limit Test - 3D 복셀 그래픽 부위 파괴 액션 & 시장 거래

## 개요

AI 도구를 활용하여 뽑아낼 수 있는 속도와 효율 한계치를 확인하고, 활용 능력을 강화하기 위해 시작한 일주일간의 스프린트 프로젝트입니다. 작업 내역/의사 결정 기록을 자동화하여 진행 중 발생했던 문제와 개선 과정을 세세히 남겼습니다.


## 게임 소개

| 항목 | 내용 |
|---|---|
| 플레이 영상 | [버섯을 쥽자! 플레이 영상](https://youtu.be/INH3VKYxHVU) |
| PC 빌드 | [Releases v1.0 다운로드](https://github.com/MJ5353/ProjectMT/releases/latest) (Windows 64bit) |
| 엔진 | Unity 6000.3.25f1 (URP, Input System) |
| 플랫폼 | Windows PC(추후 모바일 확장 예정), 가로 화면 |

| 동작 | 키보드·마우스 | 게임패드 |
|---|---|---|
| 이동 | WASD / 방향키 | 왼쪽 스틱 |
| 공격(연타 콤보) | 마우스 왼쪽 / Space / J / Enter | A / X |
| 위로 치기 | 마우스 오른쪽 / K | RB |
| 회오리 베기 | 공격 길게 누르기 | 공격 길게 누르기 |
| 달리다 베기 | 이동 중 공격 | 이동 중 공격 |
| 대시(무적) | Shift | B |
| 팝업 닫기 / 게임 종료 확인(타이틀·로비) | Esc | - |

### 실행 방법

1. [Releases](https://github.com/MJ5353/ProjectMT/releases/latest)에서 `MushTaker_PC_v1.0.zip`을 받습니다.
2. 새 폴더에 압축을 모두 풀고 `MushTaker.exe`를 실행합니다. exe만 따로 옮기면 실행되지 않습니다.
3. "Windows의 PC 보호" 창이 뜨면 `추가 정보` → `실행`을 누릅니다(서명하지 않은 개인 빌드라 뜨는 안내입니다).


## 플레이 루프

<img width="1280" height="720" alt="artplayer org_screenshot_1795510807490" src="https://github.com/user-attachments/assets/6787a912-dac1-43ab-9da8-b3279242a12d" />
<img width="1280" height="720" alt="artplayer org_screenshot_1795510898910" src="https://github.com/user-attachments/assets/47bbf5c0-d8f8-44c0-9831-f17ac38b553c" />
<img width="1280" height="720" alt="artplayer org_screenshot_1795511323833" src="https://github.com/user-attachments/assets/ab3cacec-c51b-4ff4-b302-1518012e8e0f" />

 **토벌**:  맵에 진입하면 하루(3분 타이머)가 시작됩니다. 몬스터를 잡는 방식으로도 버섯을 얻을 수 있지만, 고가의 신선한 버섯은 보스가 살아있는 동안 특정 부위를 파괴해야만 얻을 수 있습니다.
   - **부위 파괴**: 보스는 부위마다 내구도가 있으며, 해당 부위를 파괴하면 버섯을 떨구고 쓰러집니다. 이렇게 얻은 버섯은 신선도가 높아 더 비싸게 팔립니다.
      <img width="1280" height="720" alt="artplayer org_screenshot_1795510690837" src="https://github.com/user-attachments/assets/ba5858d9-a557-48b1-9123-e83da9039b2c" />
   - **퍼펙트 닷지와 러시**: 피격 직전 대시로 공격을 피하면 퍼펙트 닷지가 발동됩니다. 3초간 플레이어를 제외한 모든 생물이 느려지고, 공격자에게 돌진해 연타를 퍼부을 수 있습니다.
      <img width="1280" height="720" alt="artplayer org_screenshot_1795510768743" src="https://github.com/user-attachments/assets/cb466634-1ce4-41c6-9c25-f2040c32fe81" />

**시장**: 토벌 타이머의 남은 시간은 거래 가능 시간으로 변환됩니다. 그 시간 동안 시장에서 버섯을 판매할 수 있습니다. 팔지 못한 버섯은 그날 밤 상해버립니다. 
   - **현장 시장**: 상인마다 선호하는 버섯과 희귀도가 있으며, 30분마다 시세가 달라집니다. 시세가 바뀌기를 기다려 볼수도 있지만, 남은 시간을 잘 확인해야 합니다.
      <img width="1280" height="720" alt="artplayer org_screenshot_1795510827202" src="https://github.com/user-attachments/assets/5335e3d9-e58a-4b70-ad21-59de84c97dc3" />
      <img width="1280" height="720" alt="artplayer org_screenshot_1795510843049" src="https://github.com/user-attachments/assets/769f0a14-b18d-4705-a0a5-005179dd52a4" />

**정비**: 판매를 통해 얻은 골드로 플레이어를 강화할 수 있습니다. 강화는 노드 형식으로 구성되어 특정 노드를 강화해야만 열 수 있는 스페셜 노드도 존재합니다. 스페셜 노드를 통해 수치 변화 뿐만이 아니라, 전투 체험의 변화를 느낄 수 있습니다. 
   <img width="1280" height="720" alt="artplayer org_screenshot_1795511625758" src="https://github.com/user-attachments/assets/e3ef7fb8-1288-4849-b4be-d5e3250b24ab" />
   <img width="1280" height="720" alt="artplayer org_screenshot_1795510877599" src="https://github.com/user-attachments/assets/8b707095-3065-441a-9741-6fe24bdcaf2c" />
   - "상해 보험"노드는 피격시 골드가 드랍됩니다. 
   - "포자 재생"노드는 버섯을 얻으면 체력이 회복됩니다. 
   - 이 외에도 다양한 능력을 강화하는 노드가 존재합니다. 

(*로비 하단 탭 중 개량, 파견은 후속 기획으로 설명만 제공됩니다.)


## 기술 설계

설계/기획과 관련된 상세 내용은 [`_Else/DevDocs`](_Else/DevDocs)에 첨부하였습니다.

- 게임 흐름: ~Manager, ~Flow, ~Service
    Manager는 런타임 내내 유지되고, Service는 씬의 Flow에 귀속됩니다. 
    GameManager는 씬 전환을, FlowManager는 현재 Flow와 공용 Service(Effect·UIReuse·Popup)를 맡습니다.
  - 자세히: [Flow_구조설계.md](_Else/DevDocs/Flow_구조설계.md)

- 유닛: Unit, ~Feature
    액터는 Unit으로 일반화하였고, Unit이 가진 능력과 상태는 기능에 따라 Unit이 가진 Feature로 분리하였습니다. 
  - 주요 Feature: Passive Feature, State Feature 
  - 자세히: [Unit_구조설계.md](_Else/DevDocs/Unit_구조설계.md)

- 데이터 : ~Def
    Scriptable Object를 상속받은 Data Def를 기반으로 각종 데이터를 정의하였습니다. 전용 데이터 편집기(UI Toolkit)를 통해 편집, 관리합니다. 
    범용, 전역적인 데이터는 Define Def에 모아두었으며, Define 편집기를 통해 내용을 편집, 관리합니다. 
    이러한 Def는 GameDatabase로 묶이고, 빌드 직전 자동 갱신됩니다.

- UI : Overlay~, Screen~, Popup~ 
    Scene에 구애받지 않고 뜨는 UI는 Overlay, Scene에 하나밖에 없으며 전 화면을 덮는 UI는 Screen, 껐다 켤 수 있는 UI는 Popup으로 정리하였습니다. 
    프리팹은 에디터 UI 생성기(UI Builder)를 통해 구성하고, 내용이 바뀐 프리팹만 골라서 다시 만들 수 있도록 하였습니다. 


## AI 활용 방식

대부분의 기능 구현은 AI를 통해 이뤄졌지만, 구조와 규칙은 직접 설계하고 AI의 결과물을 검토하며 반복해서 수정 지시와 리팩토링을 진행하였습니다.

- **구조 설계**: 비대해진 전투 서비스를 Spawn·Loot·Battle로 나누고, 계층이 많아 헷갈리던 유닛의 "부품·실행기·판단" 3층 구조를 StateFeature 하나로 합치는 등 본인만의 기준을 통해 코드를 꾸준히 리팩토링하였습니다. 
- **AI 설계안 교정**: AI가 만든 구조에서 역할이 어긋나거나 확장하기 어려운 부분을 짚어 고쳤습니다. 예를 들어 퍼펙트 닷지 성공 여부를 공격하는 몬스터 쪽 코드가 판단하던 것을 회피하는 플레이어 쪽으로 옮겼고, 강화 노드의 특수 효과마다 클래스를 하나씩 만들던 방식을 "발동 시점 + 조건 + 효과"를 데이터로 조합하는 방식으로 바꿨습니다.
- **코드 기준 제시**: 런타임 GetComponent 금지, Feature 간 참조는 Owner로만, 매 프레임 검사 대신 이벤트, 재사용 객체는 Clear로 GC 회피 같은 규칙을 정하고 AI 결과물에 적용시켰습니다.
- **AI 리뷰 선별**: AI의 성능·구조 리뷰 중 실제 비용·버그·혼동이 있는 것만 골라 반영하고, AI가 원인을 잘못 판단한 버그는 다시 짚어 고쳤습니다.
- **손맛 조정**: 타격감·회피 타이밍·카메라 수치는 직접 플레이하며 정했습니다.
- **작업 방식 설계**: AI가 매 작업에 따르는 규칙 문서([`_Else/Rule`](_Else/Rule))와, 날짜별로 맡긴 일·직접 판단·채택/거절을 남기는 기록 방식([AI 실험 로그](_Else/AI_실험_로그.md))을 직접 만들었습니다.
- **리소스 제작**: 복셀 모델과 맵은 AI가 작성한 Blender Python 스크립트([`tools/voxel`](tools/voxel))로 생성하고, UI 스프라이트도 AI로 제작하였습니다. 용도와 컨셉은 직접 정했습니다.


## 폴더 구성

| 경로 | 내용 |
|---|---|
| `Assets/Scripts` | 게임 코드 (Manager·Unit·Data·UI·Editor·DevTools) |
| `Assets/Resources` | Def 데이터, 프리팹 |
| `Assets/Art` | 복셀 모델·머티리얼, UI 스프라이트, 폰트 |
| `Assets/Scenes` | Title · Lobby · Battle |
| `tools/voxel` | 복셀 모델 생성 스크립트 |
| `_Else/Plan` | 기획서 |
| `_Else/DevDocs` | 구조 설계 문서 |
| `_Else/Rule` | 작업 규칙 |


## 라이선스

이 저장소는 열람·심사 목적으로만 공개합니다. 저작권자의 사전 서면 허락 없이 코드, 아트 에셋, 데이터, 문서의 전부 또는 일부를 복제·수정·배포·사용할 수 없습니다. 자세한 내용은 [LICENSE](LICENSE)를 참고해 주십시오.

- Galmuri 폰트(`Assets/Art/Fonts/Galmuri`)는 SIL Open Font License 1.1을 따릅니다.
