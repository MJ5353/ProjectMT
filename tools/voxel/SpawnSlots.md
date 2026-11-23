# 맵 스폰 슬롯 표

최종 수정: 26.11.23

생성 코드의 기본 배치 의도를 비교하는 참고 표다. 실제 플레이어 위치·몬스터 배정은 MapDef의 PlayerSlot·ListSpawn에서 정한다.

- 001은 플레이어, 002~016은 잡몹 5무리, 017~020은 엘리트, 021은 보스 자리다.
- 종류 뒤 번호는 생성기 내부 배치 구분이며 몬스터 Def ID가 아니다. 맵 재생성·지역 데이터 변경 시 같은 슬롯 번호를 기준으로 연결한다.

| 슬롯 | Map_001_Field | Map_002_Ice | Map_003_Dino |
|---|---|---|---|
| SpawnSlot_001 | Player | Player | Player |
| SpawnSlot_002 | Buggi_1 | Bugwin_1 | Buraptor_1 |
| SpawnSlot_003 | Buggi_2 | Bugwin_2 | Buraptor_2 |
| SpawnSlot_004 | Budeoji_1 | BukBuggi_1 | Bupachy_1 |
| SpawnSlot_005 | Buggi_3 | Bugwin_3 | Buraptor_3 |
| SpawnSlot_006 | Buggi_4 | Bugwin_4 | Buraptor_4 |
| SpawnSlot_007 | Budeoji_2 | BukBuggi_2 | Bupachy_2 |
| SpawnSlot_008 | Buggi_5 | Bugwin_5 | Buraptor_5 |
| SpawnSlot_009 | Buggi_6 | Bugwin_6 | Buraptor_6 |
| SpawnSlot_010 | Budeoji_3 | BukBuggi_3 | Bupachy_3 |
| SpawnSlot_011 | Buggi_7 | Bugwin_7 | Buraptor_7 |
| SpawnSlot_012 | Buggi_8 | Bugwin_8 | Buraptor_8 |
| SpawnSlot_013 | Budeoji_4 | BukBuggi_4 | Bupachy_4 |
| SpawnSlot_014 | Buggi_9 | Bugwin_9 | Buraptor_9 |
| SpawnSlot_015 | Buggi_10 | Bugwin_10 | Buraptor_10 |
| SpawnSlot_016 | Budeoji_5 | BukBuggi_5 | Bupachy_5 |
| SpawnSlot_017 | Bugom_1 | Bubeom_1 | Bursus_1 |
| SpawnSlot_018 | Burangi_1 | BukBuyeou_1 | Bukera_1 |
| SpawnSlot_019 | Bugom_2 | Bubeom_2 | Bursus_2 |
| SpawnSlot_020 | Burangi_2 | BukBuyeou_2 | Bukera_2 |
| SpawnSlot_021 | Boss_Tree | BukBugom | Boss_Bursus |

위치 생성은 [build_map.py](build_map.py), 지역 설정은 [build_ice.py](build_ice.py)·[build_dino.py](build_dino.py), 실행·출력 이름은 [사용 설명](README.md)을 참고한다.
