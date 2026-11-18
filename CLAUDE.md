# 송이 시즌 작업 규칙

최종 수정: 26.11.18

## 1. 다음 단위 작업

다음 기능 작업 전에 저장소 변경을 확인하고, 작업이 끝나면 `_Else/AI_실험_로그.md`에 맡긴 일·직접 작업한 일·판단을 기록한다.

## 2. 전체 작업 TODO 및 진행상황

이 문서는 기능 구현의 완료율을 기록하지 않는 작업 규칙이다. 다음 작업에서 반복 확인할 항목은 에셋 위치·메타 파일·명명 규칙·PR 흐름·AI 기록이다. 개별 기능의 완료 상태는 해당 작업 문서에서 관리한다.

## 3. 상세 정보

### 저장소

- GitHub 공개 저장소: `MJ5353/ProjectMT`
- Unity 버전: 6000.3.25f1
- 목적: 넷마블네오 AI 인턴십 제출용. 심사자가 코드와 AI 작업 흔적을 직접 본다.

### 올리는 것

- `Assets/` 안의 내 스크립트, 씬, 프리팹, 데이터(몬스터·버섯·구매인 표). `ThirdParty/`만 제외
- 모든 `.meta` 파일
- `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`
- 직접 만든 버섯 프랍, 이펙트
- `CLAUDE.md`, 커밋 전 훅, 테스트 봇, 디버그 도구
- `_Else/`: 기획 메모·표 원본
- `README.md`(심사자용), `_Else/AI_실험_로그.md`

### 가리는 것

- `Assets/ThirdParty/` 전체: 에셋 스토어 팩, Starter Assets, Mixamo 원본 FBX
- Unity 생성 폴더: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `MemoryCaptures/`, `*.csproj`, `*.sln`
- 빌드 파일(APK·WebGL): 저장소 대신 GitHub Releases나 itch.io
- API 키, 계정 정보

### 폴더 구조

```
Assets/
  Scripts/       내가 만든 것 (git 추적)
  Scenes/
  Prefabs/
  Data/
  Art/
  ThirdParty/    남이 만든 것 (git 제외)
```

- `_Project/` 폴더로 묶지 않고 `Assets/` 바로 아래에 둔다. (26.11.16 대장 결정)
- 외부 에셋은 반드시 `ThirdParty/` 아래로 옮긴 뒤 쓴다.
- Unity 설정: Asset Serialization = Force Text, Version Control = Visible Meta Files.

### Git LFS

- 대상: `*.fbx`, `*.png`, `*.psd`, `*.tga`, `*.wav`, `*.ogg`, `*.mp3`
- 씬·프리팹·애니메이션(`.unity`, `.prefab`, `.anim`)은 텍스트라 LFS에 넣지 않는다.
- 무료 용량이 작으니 큰 파일은 피한다(정확한 한도는 확인 필요).

### 커밋 전 훅

커밋을 막는 경우:
- `Assets/ThirdParty/` 경로 파일
- LFS 대상인데 LFS 포인터가 아닌 파일
- 10MB 넘는 파일
- API 키처럼 보이는 문자열

### 코드 스타일

대장 기존 코드(`portfolio_mjKim/_Unity_Reference`, ProjectR 우선)에서 뽑은 규칙. (26.11.17)

- 네임스페이스는 쓰지 않는다(전역).
- private/protected 필드는 `m_PascalCase`, static은 `s_PascalCase`, 상수는 `PascalCase`. 컬렉션은 `m_DicXxx`, `m_ListXxx`.
- 접근 제한자를 항상 쓴다. `[SerializeField] private T m_Xxx = null;`
- `Def`의 단순 저장 값은 `[field: SerializeField] public T Xxx { get; private set; }`로 선언한다. 기존 필드를 옮길 때는 `[field: FormerlySerializedAs("m_Xxx")]`로 에셋 값을 이어받는다.
- bool 부정은 `x == false`. 가드 구문으로 먼저 return하고, 한 줄 if는 중괄호 없이 다음 줄에 쓴다. 한 줄에 문장 하나.
- 지역·out 변수는 타입을 명시한다. `var`는 `foreach (var pair ...)` 정도만.
- `for`와 `foreach`는 본문이 한 문장이어도 중괄호 `{}`로 감싼다.
- 수명 주기는 `Init` / `Reset` / `Clear`, 갱신은 `Update(float deltaTime)`.
- 클래스 안의 필드·프로퍼티와 메서드 사이에는 앞뒤에 빈 줄을 두고 `// ------------------------------------------------`를 쓴다. 파일 최상위에서 타입을 구분할 때는 `// ==================================================`를 쓴다. 이름 있는 묶음은 `// 제목 ---`, 가상 함수 묶음은 `// virtual ---`로 표시한다. Unity 이벤트 함수(Awake, Start 등)를 먼저 둔다.
- 접미사: 전역 싱글턴 `XxxManager`, 하위 시스템 `XxxManagement`, 액터 부품 `XxxFeature`, 모음 `XxxHub`. 약칭 지역 변수 `gameM`, `skillM`.
- 열거형은 `E` 접두사, 매개변수 묶음은 `readonly struct XxxParams`. ID는 `ID`로 대문자.
- 주석은 짧은 한글, 경고는 `// !!)`, 대장 메모는 `// [mj] todo)`.

### AI 협업

- 코드는 대장, Claude는 유의점·초안·수정 제안 (ProjectTP 규칙을 이어 감).
- AI가 쓴 코드는 대장이 읽고 이해한 뒤에만 커밋한다.
- 손맛 수치(타격감, 회피 타이밍, 카메라)는 대장이 정한다.
- AI에게 맡기는 것: 데이터 표 초안, 몬스터 상태 머신 초안, 디버그 도구, 경제 시뮬 봇, 도감 문구.
- 하루 끝에 `_Else/AI_실험_로그.md`에 한 줄: 맡긴 것 / 내가 한 것 / 판단.

### 토큰 절약

26.11.18 대장 확정.

- `.unity`·`.prefab`·`.asset`은 통째로 읽지 않는다. grep으로 필드명·`m_Name`·guid를 찾고 앞뒤 몇 줄만 본다.
- C# 파일은 심볼을 grep으로 먼저 찾고 해당 범위만 읽는다. 200줄 넘는 파일 전체 읽기는 꼭 필요할 때만 한다.
- 방금 수정한 파일은 다시 읽어 확인하지 않는다.
- 로그·컴파일 결과는 error/warning 줄만 거른다(`Editor.log` 전체 출력 금지).
- 스레드는 작업 하나가 끝나면 닫는다. 이어갈 내용은 Goals md나 메모리에 남기고 새 스레드에서 시작한다.

### 문서 위치

- 진행 추적: Goals 저장소
- 지난 기록 보관: Records 저장소
- 이 저장소: README, AI 실험 로그, 기획서 링크만

### 라이선스·출처

- README에 외부 에셋 목록: 이름, 버전, 링크, 라이선스
- Meshy·Tripo 무료 플랜 결과물은 출처 표기
- AI 사용 사실은 README에 밝힌다

## 4. 결정된 사안

### 문서 정리와 AI 기록

- 작업 문서는 **1. 다음 단위 작업 → 2. 전체 작업 TODO 및 진행상황 → 3. 상세 정보 → 4. 결정된 사안** 순으로 정리한다. 진행 상태와 미정을 구분하고 기존 내용과 날짜를 보존한다.
- 승인된 전투 부위 파괴 계획서 형식을 나머지 Markdown에도 적용한다.
- AI 실험 로그는 `_Else/AI_실험_로그.md` 한 파일에만 기록한다.

### 명명 규칙

26.11.18 대장 확정. 콘텐츠마다 `NNN_Key` 하나를 키로 정하고(번호 3자리 + 이름), 에셋 종류 접두사만 바꿔 붙인다. 숫자는 모두 3자리.

| 종류 | 형식 | 예 |
|---|---|---|
| 몬스터 모델 | `Mon_NNN_Key` | `Mon_001_Rabbit`, `Mon_005_Boss_Tree` |
| 몬스터 Def·ID·프리팹 | `Monster_NNN_Key` | `Monster_001_Rabbit` |
| 애니메이터 | `AC_NNN_Key` (플레이어는 `AC_Player`) | `AC_001_Rabbit` |
| 버섯 모델 | `Mush_NNN_Key` (깨진 버전 `_Broken`) | `Mush_001_Songi` |
| 버섯 Def·ID | `Mushroom_NNN_Key` | `Mushroom_002_Gureum` |
| 버섯 픽업·아이콘 | `Pickup_NNN_Key`, `Icon_NNN_Key` | `Pickup_001_Songi` |
| 머티리얼 | `M_Xxx` (`M_`은 머티리얼 전용) | `M_Voxel` |
| 지역 Def·맵 모델 | `Map_NNN_Key` (둘이 같은 이름) | `Map_001_Field` |
| 맵 스폰 지점 | `SpawnSlot_NNN` | `SpawnSlot_001` |

- 몬스터 키는 원래 종족의 영어 이름(Rabbit, Raptor), 표시 이름(도감명)은 "버"가 들어간 이름(버끼, 버랩터). 보스는 키에 `Boss_`를 넣는다.
- 버섯 키는 한글 로마자(Cloud가 아니라 Gureum).
- 몬스터 번호는 지역 순서대로 잡몹 → 엘리트 → 보스(평원 001~005, 얼음 006~010, 공룡 011~015).
- 지역 번호: 001 Field, 002 Ice, 003 Dino.
- 버섯 Def의 `DexNo`(도감 번호)는 이름의 NNN과 같게 둔다.
- 버섯 번호: 001 Songi, 002 Gureum, 003 Malbul, 004 Noru, 005 Pyogo, 006 Paengi, 007 Gwangdae, 008 Seori.
- 복셀 생성 스크립트는 `tools/voxel/voxlib.py`의 `EXPORT_NAMES` 표로 이 이름을 내보낸다. 새 몬스터·버섯은 이 표에도 추가한다.

### 커밋·브랜치

- 기능 하나 = 브랜치 하나 = PR 하나. main에는 PR로만 합친다. (26.11.15 대장 확정, 이 저장소만 예외)
- PR마다 AI 리뷰를 받고, 지적마다 수용/거절과 이유를 PR에 남긴다.
- 커밋 메시지는 한국어 한 줄 요약.
- Claude는 파일만 수정하고, commit·push는 대장이 요청할 때만 한다.
