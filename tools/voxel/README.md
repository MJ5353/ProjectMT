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

- 56m x 56m. 가장자리는 4블록 높이 절벽과 거대 버섯 나무로 막았다.
- 구역: 남쪽 시작 지점 → 가운데 갈림길 → 서쪽 버끼 초원(꽃·언덕), 동쪽 버곰 숲(균사 땅·버섯 나무), 북쪽 보스 공터(바위 둘레, 남쪽 입구만 열림).
- 자식: `Map_Terrain`·`Map_Props`(MeshCollider 자동), `Map_Grass`(풀·꽃, 충돌 없음), `Spawn_Player`, `Spawn_Buggi_1~6`, `Spawn_Bugom_1~3`, `Spawn_Bursus`(빈 오브젝트, 위치만 사용).
- 맵 원점은 남서쪽 모서리, 지면 높이는 2m.

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
