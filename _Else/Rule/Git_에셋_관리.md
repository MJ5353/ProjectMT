# Git · 에셋 관리

최종 수정: 26.11.18
적용: 브랜치·PR·커밋, 에셋 반입·이동, 저장소 정리, 빌드 배포 또는 라이선스 처리 시 참고한다.

## Git · PR

- **기능 하나 = 브랜치 하나 = PR 하나.** `main`에는 PR로만 병합한다. (26.11.15 확정, ProjectMT 예외 규칙)
- PR마다 AI 리뷰를 받고, 지적 사항별 수용·거절과 이유를 PR에 기록한다.
- 커밋 메시지는 한국어 한 줄 요약으로 쓴다.
- AI(Claude 포함)는 요청된 파일만 수정한다. 커밋·푸시는 대장이 명시적으로 요청할 때만 한다.

## 폴더 · 추적 대상

```text
Assets/
  Scripts/       직접 작성한 스크립트
  Scenes/
  Prefabs/
  Data/
  Art/
  ThirdParty/    외부 에셋 (Git 제외)
```

- `_Project/`로 묶지 않고 `Assets/` 바로 아래에 둔다. (26.11.16 확정)
- 외부 에셋(에셋 스토어 팩, Starter Assets, Mixamo 원본 FBX 등)은 **사용 전에** `Assets/ThirdParty/`로 옮긴다.
- 직접 만든 스크립트·씬·프리팹·데이터(몬스터·버섯·구매인 표)·버섯 프랍·이펙트와 이들의 **모든 `.meta`**를 추적한다.
- `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/`, `CLAUDE.md`, 커밋 전 훅, 테스트 봇, 디버그 도구, `_Else/` 기획 메모·표 원본, `README.md`, `_Else/AI_실험_로그.md`를 추적한다.
- `Assets/ThirdParty/` 전체와 Unity 생성 폴더·파일(`Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Build*/`, `MemoryCaptures/`, `*.csproj`, `*.sln`)은 제외한다.
- API 키와 계정 정보는 저장소에 넣지 않는다. APK·WebGL 빌드는 저장소 대신 GitHub Releases나 itch.io에 배포한다.
- Unity 설정: Asset Serialization = `Force Text`, Version Control = `Visible Meta Files`.

## Git LFS · 커밋 전 훅

- LFS 대상: `*.fbx`, `*.png`, `*.psd`, `*.tga`, `*.wav`, `*.ogg`, `*.mp3`.
- `.unity`, `.prefab`, `.anim`은 텍스트이므로 LFS 대상이 아니다.
- LFS 무료 한도는 확실하지 않으므로 대형 파일을 피한다.
- 커밋 전 훅은 `Assets/ThirdParty/` 파일, LFS 대상의 일반 바이너리, 10MB 초과 파일, API 키 의심 문자열을 차단한다.

## 출처 표기

- README에 외부 에셋의 **이름·버전·링크·라이선스**를 명시한다.
- Meshy·Tripo 무료 플랜 결과물은 출처를 표기한다.
- AI 사용 사실을 README에 명시한다.
