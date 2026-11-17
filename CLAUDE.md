# 송이 시즌 작업 규칙

최종 수정: 26.11.17


## 저장소

- GitHub 공개 저장소: `MJ5353/ProjectMT`
- Unity 버전: 6000.3.25f1
- 목적: 넷마블네오 AI 인턴십 제출용. 심사자가 코드와 AI 작업 흔적을 직접 본다.

## 올리는 것

- `Assets/` 안의 내 스크립트, 씬, 프리팹, 데이터(몬스터·버섯·구매인 표). `ThirdParty/`만 제외
- 모든 `.meta` 파일
- `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`
- 직접 만든 버섯 프랍, 이펙트
- `CLAUDE.md`, 커밋 전 훅, 테스트 봇, 디버그 도구
- `_Else/`: 기획 메모·표 원본
- `README.md`(심사자용), `docs/AI_실험_로그.md`

## 가리는 것

- `Assets/ThirdParty/` 전체: 에셋 스토어 팩, Starter Assets, Mixamo 원본 FBX
- Unity 생성 폴더: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `MemoryCaptures/`, `*.csproj`, `*.sln`
- 빌드 파일(APK·WebGL): 저장소 대신 GitHub Releases나 itch.io
- API 키, 계정 정보

## 폴더 구조

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

## 커밋·브랜치

- 기능 하나 = 브랜치 하나 = PR 하나. main에는 PR로만 합친다. (26.11.15 대장 확정, 이 저장소만 예외)
- PR마다 AI 리뷰를 받고, 지적마다 수용/거절과 이유를 PR에 남긴다.
- 커밋 메시지는 한국어 한 줄 요약.
- Claude는 파일만 수정하고, commit·push는 대장이 요청할 때만 한다.

## Git LFS

- 대상: `*.fbx`, `*.png`, `*.psd`, `*.tga`, `*.wav`, `*.ogg`, `*.mp3`
- 씬·프리팹·애니메이션(`.unity`, `.prefab`, `.anim`)은 텍스트라 LFS에 넣지 않는다.
- 무료 용량이 작으니 큰 파일은 피한다(정확한 한도는 확인 필요).

## 커밋 전 훅

커밋을 막는 경우:
- `Assets/ThirdParty/` 경로 파일
- LFS 대상인데 LFS 포인터가 아닌 파일
- 10MB 넘는 파일
- API 키처럼 보이는 문자열

## 코드 스타일

대장 기존 코드(`portfolio_mjKim/_Unity_Reference`, ProjectR 우선)에서 뽑은 규칙. (26.11.17)

- 네임스페이스는 쓰지 않는다(전역).
- private/protected 필드는 `m_PascalCase`, static은 `s_PascalCase`, 상수는 `PascalCase`. 컬렉션은 `m_DicXxx`, `m_ListXxx`.
- 접근 제한자를 항상 쓴다. `[SerializeField] private T m_Xxx = null;`
- bool 부정은 `x == false`. 가드 구문으로 먼저 return하고, 한 줄 if는 중괄호 없이 다음 줄에 쓴다. 한 줄에 문장 하나.
- 지역·out 변수는 타입을 명시한다. `var`는 `foreach (var pair ...)` 정도만.
- 수명 주기는 `Init` / `Reset` / `Clear`, 갱신은 `Update(float deltaTime)`.
- 클래스 안 필드와 함수 사이는 `// ==================================================`로 구분한다. 하이픈 구분선은 `// 제목---` 형식으로 쓴다. 가상 함수 묶음은 `// virtual---`로 표시한다. Unity 이벤트 함수(Awake, Start 등)를 먼저 둔다.
- 접미사: 전역 싱글턴 `XxxManager`, 하위 시스템 `XxxManagement`, 액터 부품 `XxxFeature`, 모음 `XxxHub`. 약칭 지역 변수 `gameM`, `skillM`.
- 열거형은 `E` 접두사, 매개변수 묶음은 `readonly struct XxxParams`. ID는 `ID`로 대문자.
- 주석은 짧은 한글, 경고는 `// !!)`, 대장 메모는 `// [mj] todo)`.

## AI 협업

- 코드는 대장, Claude는 유의점·초안·수정 제안 (ProjectTP 규칙을 이어 감).
- AI가 쓴 코드는 대장이 읽고 이해한 뒤에만 커밋한다.
- 손맛 수치(타격감, 회피 타이밍, 카메라)는 대장이 정한다.
- AI에게 맡기는 것: 데이터 표 초안, 몬스터 상태 머신 초안, 디버그 도구, 경제 시뮬 봇, 도감 문구.
- 하루 끝에 `docs/AI_실험_로그.md`에 한 줄: 맡긴 것 / 내가 한 것 / 판단.

## 문서 위치

- 진행 추적: Goals 저장소
- 지난 기록 보관: Records 저장소
- 이 저장소: README, AI 실험 로그, 기획서 링크만

## 라이선스·출처

- README에 외부 에셋 목록: 이름, 버전, 링크, 라이선스
- Meshy·Tripo 무료 플랜 결과물은 출처 표기
- AI 사용 사실은 README에 밝힌다
