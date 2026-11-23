# 복셀 에셋 생성기

최종 수정: 26.11.23

Blender 스크립트로 캐릭터·버섯·맵·장식을 생성한다. 출력 이름은 [voxlib.py](voxlib.py)의 `EXPORT_NAMES`, Unity 클립 이름은 [AnimNameTable.txt](../../Assets/Art/Voxel/Editor/AnimNameTable.txt)가 기준이다.

## 실행과 출력

프로젝트 루트에서 필요한 생성기를 실행한다. 전체 지역을 포함하는 예시는 다음과 같다.

```text
blender -b -P tools/voxel/build_dino.py -- "<출력폴더>" --preview
```

`bpy`를 사용할 수 있는 Python 환경에서는 `python tools/voxel/build_dino.py -- "<출력폴더>" --preview`도 가능하다. `--preview`는 선택 사항이며 애니메이션 시트 미리보기에는 Pillow를 사용한다.

| 출력 | 내용 |
|---|---|
| `<출력폴더>/Models/` | 이름이 변환된 FBX, `VoxelPalette.png`, `TerrainAtlas.png` |
| `<출력폴더>/Preview/` | `--preview` 사용 시 해당 생성기의 장면·캐릭터·애니메이션 미리보기 |

## 생성기별 범위

각 생성기는 아래의 의존 생성기를 함께 실행한다. 따라서 특정 몬스터나 맵을 추가로 만들 때도 기초 에셋·팔레트가 다시 출력된다.

| 생성기 | 추가 출력 | 함께 호출하는 생성기 |
|---|---|---|
| `build_voxel_assets.py` | 플레이어·기본 몬스터·버섯·블록·장식 | 없음 |
| `build_map.py` | `Map_001_Field.fbx` | `build_voxel_assets` |
| `build_boss.py` | `Mon_015_Boss_Saurus.fbx`·깨진 버섯 모델 | `build_map` |
| `build_boss_tree.py` | `Mon_005_Boss_Tree.fbx` | `build_boss` |
| `build_ice.py` | 얼음 몬스터·`Mon_010_Boss_PolarBear.fbx`·`Map_002_Ice.fbx` | `build_boss_tree` |
| `build_more_mobs.py` | 평원·얼음 추가 몬스터·펫 | `build_ice` |
| `build_dino.py` | 공룡 몬스터·`Map_003_Dino.fbx` | `build_more_mobs` |

생성 코드의 내부 이름과 출력 파일 이름은 다르다. 예: `Buggi → Mon_001_Rabbit`, `Boss_Bursus → Mon_015_Boss_Saurus`, `Mush_Cloud → Mush_002_Gureum`. 나머지 이름은 EXPORT_NAMES를 따른다.

## Unity 반영

1. FBX는 `Assets/Art/Voxel/Models/`의 기존 파일 위치에 반영하고 기존 `.meta`를 보존한다. 현재 하위 폴더는 `Actor/Monster`·`Actor/Pet`·`Mushroom`·`Map`·`Block`·`Environment`이며 플레이어는 `Actor/Player.fbx`다.
2. 팔레트·지형 아틀라스는 같은 Models 루트에 반영한다. [VoxelImportSettings.cs](../../Assets/Art/Voxel/Editor/VoxelImportSettings.cs)가 점 필터·압축·축 변환·머티리얼 연결·클립 이름과 반복 설정을 처리한다.
3. 클립을 추가·변경하면 AnimNameTable에 `모델|원래 클립|표준 이름`을 기록한다. **해당 FBX Reimport 완료 후** `MushTaker/에셋/애니메이터 다시 만들기`를 실행한다.
4. 데이터 편집기에서 모델과 Def를 연결하고 필요한 프리팹을 빌드한다. 데이터는 `Assets/Resources/Data`, 프리팹은 `Assets/Resources/Prefabs`에 둔다.

### 애니메이션 이름

생성 스크립트에는 `Slam`·`Attack_Up` 같은 원래 이름을 사용한다. Unity에서는 이름 표에 따라 `Attack_01`·`Attack_02` 등으로 바뀌며, SkillDef의 클립 이름도 표준 이름에 맞춘다.

기본 공격은 `Attack_NN`, 스킬은 `Skill_NN`이고 클립 이름과 애니메이터 상태 이름을 같게 둔다. 이동·반응 클립은 기존 이름을 유지하며 판정 시점은 SkillDef의 공격 데이터에서 조절한다.

## 맵·소켓 참고

- 맵 설정은 평원 `build_map.REGION`, 얼음 `build_ice.ICE_REGION`, 공룡 `build_dino.DINO_REGION`에 둔다. 지형·프랍·내부 배치 종류를 여기서 바꾼다.
- 맵은 `Map_Terrain`·`Map_Props`·`Map_Grass`·`Map_Bounds`로 구성한다. 임포터가 지형·프랍·경계에 MeshCollider를 붙이고 경계 렌더러는 숨긴다.
- 스폰 지점은 `SpawnSlot_001~021`이다. 생성 의도는 [스폰 슬롯 표](SpawnSlots.md), 실제 플레이어 위치·몬스터 배정은 MapDef에서 관리한다.
- `Socket_*`는 버섯 부착, `Hitbox_*`는 몸 판정 기준이다. [BodyPartMap.cs](../../Assets/Scripts/Editor/Battle/BodyPartMap.cs)와 프리팹 빌더가 뼈를 연결하며 부위 배치·크기·파괴 값은 MonsterDef에서 설정한다.
- 버섯 원점은 밑동이며 소켓의 로컬 좌표를 기준으로 붙인다. 축 차이로 눕는 경우 모델 자식의 회전으로 보정한다.

세부 명명은 [콘텐츠 명명 규칙](../../_Else/Rule/콘텐츠_명명_규칙.md)을 따른다.
