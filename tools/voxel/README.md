# 복셀 에셋 생성기

최종 수정: 26.11.17

Blender 스크립트로 전투 시제품용 복셀 에셋을 만든다. 결과물은 `Assets/Art/Voxel/Models/`에 둔다.

## 다시 만들기

```
blender -b -P tools/voxel/build_voxel_assets.py -- <출력폴더> [--preview]
# 또는 pip install bpy 후: python tools/voxel/build_voxel_assets.py -- <출력폴더> [--preview]
```

- `Models/`: FBX, `VoxelPalette.png`(캐릭터·버섯·장식 공용 색표), `TerrainAtlas.png`(블록 타일)
- `Preview/`: `--preview`일 때 장면·애니 프레임·아이템 렌더
- Unity 가져오기 설정은 `Assets/Art/Voxel/Editor/VoxelImportSettings.cs`가 자동으로 건다(점 필터, 축 변환, 클립 이름·반복).

## 맵

`python tools/voxel/build_map.py -- <출력폴더> [--preview]` 하나로 캐릭터·버섯·블록까지 다시 만든 뒤 `Map_Field01.fbx`를 더한다(팔레트 색 위치를 맞추려고 같이 만든다).

- 100m x 100m 평탄한 평원, 지면 높이 0. 맵 원점은 남서쪽 모서리.
- 남쪽 시작 지점 → 가운데 갈림길 → 엘리트 자리 4곳(균사 땅, 둘레에 바위·통나무) → 가장 먼 북쪽 보스 자리(반지름 14m, 균사 고리, 큰 바위 둘레, 남쪽 입구만 열림).
- 가장자리 7m는 버섯 나무 숲 띠, 그 바깥은 보이지 않는 벽.
- 소품: 거대 버섯 나무 3종, 쓰러진 통나무, 버섯 군락, 큰 바위, 작은 바위·그루터기, 풀·꽃.
- 자식: `Map_Terrain`·`Map_Props`·`Map_Bounds`(MeshCollider 자동, Bounds는 렌더러 꺼짐), `Map_Grass`(충돌 없음), `Spawn_Player`, `Spawn_Buggi_1~15`(3마리씩 5무리), `Spawn_Bugom_1~4`(엘리트), `Spawn_Bursus`(보스).
- 새 지역: 파일 위 `REGION`(바닥 타일, 나무·소품 비율, 씨앗, 이름)만 바꿔서 만든다.

## 대형 버우르스 (부위 파괴 보스)

`python tools/voxel/build_boss.py -- <출력폴더> [--preview]` → `Bursus_Boss.fbx`, `Mush_*_Broken.fbx`(앞 단계 에셋·맵도 같이 다시 만든다).

- 키 5.8m, 길이 16m(플레이어 키의 약 3.1배). 턱·목·꼬리 3마디가 따로 움직인다.
- 파괴 부위 소켓 8곳: `Socket_Head`, `Socket_Back1~3`, `Socket_ForelegL/R`(앞다리 바깥), `Socket_Tail1`, `Socket_TailTip`(꼬리 곤봉). 소켓 위치에 히트박스를 두면 된다.
- 몸통 히트박스 기준점: `Hitbox_Head, Hitbox_Neck, Hitbox_Body, Hitbox_Tail, Hitbox_LegFL/FR/BL/BR`. 권장 반지름(m): 머리 1.0, 목 1.0, 몸 1.8(캡슐 길이 6), 꼬리 0.8, 다리 0.9.
- 깨진 버섯: 부위가 깨지면 소켓의 버섯을 `Mush_*_Broken`(꺾인 밑동)으로 바꾸고, 다시 자라면 원래 버섯으로 돌린다.

| 클립 | 길이 | 예비 동작 → 판정 |
|---|---|---|
| Idle, Walk, Run | 반복 | Run은 돌진 중 반복용 |
| Roar | 70 | 0~20 고개 젖힘 → 20~56 포효(경직·넉백) |
| Bite | 48 | 0~12 고개 젖히며 입 벌림 → 21 물기(앞으로 0.9m) |
| Stomp | 54 | 0~16 앞발 들기 → 22 내려찍기(충격파·포자) |
| TailSwipe | 50 | 0~14 몸 비틀기 → 22~26 꼬리 휩쓸기 |
| Charge | 70 | 0~20 웅크림 → 20~55 질주(이동은 코드) → 55~70 제동 |
| PartBreak | 60 | 부위 파괴 시 휘청·주저앉음, 52에 회복 |
| Down | 120 | 24~84 엎드림(약점 노출), 84~110 일어남 |
| Hit, Death | 18, 90 | Death는 마지막 자세 유지 |

## 나무 보스 (기생 버섯 나무)

`python tools/voxel/build_boss_tree.py -- <출력폴더> [--preview]` → `Boss_Tree.fbx` (앞 단계 에셋·맵·버우르스도 같이 다시 만든다).

- 키 약 7m(머리 가지 포함), 뿌리 다리 4개로 걷는 고목. 얼굴은 파인 눈 구멍에 빛나는 심지.
- 기생 버섯 소켓 9곳: `Socket_Crown`, `Socket_ShoulderL/R`, `Socket_TrunkL/R`(줄기 옆, 선반 버섯용), `Socket_Back`, `Socket_ForearmL/R`, `Socket_RootBL`.
- 히트박스 기준점: `Hitbox_Trunk, Hitbox_Face, Hitbox_ArmL/R, Hitbox_RootFL/FR/BL/BR`. 권장 반지름(m): 줄기 1.3(캡슐 길이 4), 얼굴 0.8, 팔 0.5, 뿌리 0.5.

| 클립 | 길이 | 예비 동작 → 판정 |
|---|---|---|
| Idle, Walk | 반복 | |
| Roar | 75 | 0~18 몸 젖히며 팔 벌림 → 20~60 떨림(넉백·경직) |
| Slam | 60 | 0~18 두 팔을 머리 위로 → 24 지면 강타 |
| Sweep | 54 | 0~16 몸 비틀며 오른팔 뒤로 → 22~28 앞 휩쓸기 |
| RootStrike | 60 | 0~20 웅크리며 뿌리 박기 → 24 앞쪽 뿌리 솟음(위치·판정은 코드) |
| SporeBurst | 66 | 0~24 웅크리고 부르르 → 30 버섯마다 포자 폭발 |
| PartBreak | 60 | 휘청이며 팔 늘어뜨림, 52 회복 |
| Down | 120 | 26~84 주저앉아 팔로 땅 짚음(얼굴·등 노출), 84~110 일어남 |
| Hit, Death | 18, 96 | Death는 나무처럼 앞으로 쓰러져 유지 |

## 에셋 목록

| 파일 | 내용 | 크기 |
|---|---|---|
| Buggi | 버끼, 토끼 몸 | 키 약 1m |
| Bugom | 버곰, 곰 몸(네발) | 키 1.6m, 길이 2.8m |
| Bursus | 버우르스, 공룡 몸(보스) | 키 약 2.3m, 길이 4.4m |
| Player | 채집꾼. 몸·모자·무기 메시 분리 | 키 1.85m(모자 포함) |
| Mush_Songi / Mush_Cloud / Mush_Malbul | 송이·구름·말불. 소켓 부품 겸 드랍 아이템 | 높이 0.3~0.5m |
| Block_Grass / Dirt / Stone / Mycel | 1m 블록, 원점은 중심 | 1m |
| Deco_Grass / Flower / Rock / Stump | 장식, 원점은 밑면 중심 | 0.2~0.6m |

## 애니메이션

| 대상 | 클립 | 판정 프레임(30fps) |
|---|---|---|
| Buggi | Idle, Walk(깡충), Attack(박치기), Hit, Death | Attack 13 |
| Bugom | Idle, Walk, Attack(일어서서 내려찍기), Hit, Death | Attack 19 |
| Bursus | Idle, Walk, Attack(꼬리치기), Stomp(내려찍기·포자), Hit, Death | Attack 18, Stomp 20 |
| Player | Idle, Walk, Run, Attack(내려치기), Hit, Death | Attack 9 |

Death는 마지막 자세를 유지하고, 나머지 단발 클립은 끝에서 기본 자세로 돌아온다. 이동은 제자리 애니(루트 모션 없음).

## 버섯 갈아끼우기

- 몸의 `Socket_*` 뼈가 꽂는 자리다. 버섯 프리팹을 소켓의 자식으로 넣고 위치·회전 0, 크기만 정한다.
- 버섯 원점은 밑동 중심이고, 위쪽이 소켓 뼈 방향을 따른다.
- 소켓: Buggi `Head, Back` / Bugom `Head, Back1, Back2, SideL, SideR` / Bursus `Head, Back1~3, Tail`
- 미리보기 배율: 버끼 머리 송이 0.8, 버곰 등 구름 1.3~1.6, 버우르스 등 말불 2.2~2.8

## 확인 필요

- Unity에서 소켓에 붙인 버섯이 바로 서는지(뼈 축 변환). 눕는다면 버섯 프리팹 안쪽 자식에 회전을 한 번 넣으면 된다.
- 지형은 일반적인 복셀 관례(윗면 풀, 옆면 흙)를 따르되 무늬와 색은 직접 만들었다.
