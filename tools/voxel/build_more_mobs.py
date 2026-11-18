# 추가 몹 (Blender 5.x): 북버끼(눈토끼, 얼음 잡몹), 버더지(두더지, 평원 잡몹), 버랑이(호랑이, 평원 엘리트), 버구리(너구리 펫), 북버여우(북극여우, 얼음 엘리트)
# 사용: python build_more_mobs.py -- <출력 폴더> [--preview]
# 체인 맨 끝: 앞 단계(캐릭터·맵·보스·얼음)를 전부 다시 만든 뒤 덧붙인다(팔레트 칸은 palette_index.json으로 고정).
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector
import voxlib as L
import build_voxel_assets as B
import build_map as M
import build_boss_tree as BT
import build_ice as I
from voxlib import box, ell, paint_front, _h

L.COLORS.update({
    'srab': (240, 242, 246), 'srab_dk': (212, 218, 228), 'srab_belly': (252, 252, 254), 'srab_tip': (62, 60, 68),
    'mole': (88, 78, 84), 'mole_dk': (70, 62, 68), 'mole_pink': (236, 150, 160),
    'racn': (150, 102, 62), 'racn_dk': (114, 74, 44), 'racn_belly': (214, 174, 124), 'racn_mask': (66, 42, 30),
    'racn_white': (222, 184, 136), 'racn_ring': (78, 50, 34), 'racn_tail': (204, 160, 108), 'racn_glove': (62, 40, 28),
    'rac': (146, 142, 146), 'rac_dk': (96, 90, 96), 'rac_belly': (214, 208, 202), 'rac_mask': (104, 92, 92), 'rac_white': (244, 240, 234),
    'fox': (236, 239, 244), 'fox_dk': (210, 216, 228), 'fox_belly': (250, 251, 253), 'fox_ear': (176, 184, 204), 'fox_paw': (196, 204, 220),
    'tiger': (226, 132, 50), 'tiger_dk': (200, 110, 40), 'tiger_stripe': (44, 34, 34), 'tiger_white': (242, 234, 218),
})

K = lambda f, b, **kw: (f, b, kw)

# ---------- 북버끼: 눈토끼 (얼음 잡몹). 버끼 뼈·동작 그대로, 색과 귀 끝만 바꿈 ----------
def body_bukbuggi():
    name, S, bones, meshes, anims = B.body_buggi()
    remap = {'rab': 'srab', 'rab_dk': 'srab_dk', 'rab_belly': 'srab_belly'}
    parts = []
    for vox, bone in meshes['Buggi_Body']:
        nv = {}
        for (i, j, k), c in vox.items():
            c = remap.get(c, c)
            if bone in ('EarL', 'EarR') and k >= 14 and not c.startswith('!'):
                c = '!srab_tip'                                       # 검은 귀 끝
            nv[(i, j, k)] = c
        parts.append((nv, bone))
    body = parts[0][0]
    for nv, bone in parts[1:]:                                          # 몸통과 겹친 칸 제거(흰 몸에 검은 틈이 보여서)
        if bone.startswith('Leg'):
            for key in [key for key in nv if key in body]:
                del nv[key]
    return 'BukBuggi', S, bones, {'BukBuggi_Body': parts}, anims

# ---------- 버더지: 두더지 (평원 잡몹). 땅속에선 등 버섯만 보임 ----------
def body_budeoji():
    S = 0.07
    body, head, pawL, footL = {}, {}, {}, {}
    def fur(i, j, k):
        return 'mole_dk' if _h(i, j, k) % 5 == 0 else 'mole'
    ell(body, (0, 0.5, 3.6), (3.6, 5.0, 3.4), fur)
    ell(body, (0, 5.6, 2.2), (0.8, 1.0, 0.8), 'mole_pink')                 # 꼬리
    ell(head, (0, -4.6, 3.3), (2.7, 2.6, 2.3), fur)
    ell(head, (0, -7.2, 2.9), (1.3, 1.2, 1.1), '!mole_pink')               # 코
    for i in (-2, 1):
        paint_front(head, i, 4, '!eye')                                     # 작은 눈
    box(pawL, 2.8, 5.2, -4.4, -1.6, 0, 2.2, 'mole_pink')                   # 큰 앞발(삽)
    for i in (3, 4):
        paint_front(pawL, i, 0, '!claw'); paint_front(pawL, i, 1, '!claw')
    box(footL, 1.4, 2.8, 3.0, 4.6, 0, 1.2, 'mole_pink')
    pawR, footR = B.sym(pawL, 0, 0), B.sym(footL, 0, 0)
    for leg in (pawL, pawR, footL, footR):                                  # 몸통과 겹친 칸 제거(검은 틈 방지)
        for key in [key for key in leg if key in body]:
            del leg[key]
    tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 0.5, 2.5), 'Root', None),
        ('Head', (0, -3.4, 3), 'Body', None),
        ('PawL', (3.6, -2.6, 2.2), 'Body', None), ('PawR', (-3.6, -2.6, 2.2), 'Body', None),
        ('FootL', (2, 3.6, 1.2), 'Root', None), ('FootR', (-2, 3.6, 1.2), 'Root', None),
        ('Socket_Back', (0, 0.5, tops(body, 0, 0)), 'Body', None),          # 땅속 이동 때 이 버섯만 땅 위로
        ('Socket_Head', (0, -4.6, tops(head, 0, -5)), 'Head', None),
    ]
    meshes = {'Budeoji_Body': [(body, 'Body'), (head, 'Head'), (pawL, 'PawL'), (pawR, 'PawR'), (footL, 'FootL'), (footR, 'FootR')]}
    paws = lambda f, **kw: [K(f, 'PawL', **kw), K(f, 'PawR', **kw)]
    UG = -0.45   # 땅속 깊이(m): 몸은 묻히고 등 버섯만 보임
    anims = {
        'Idle': (48, True, [K(24, 'Body', s=(1.03, 1, 1)), K(12, 'Head', yaw=12, pitch=-6), K(36, 'Head', yaw=-12, pitch=-6)]),
        'Walk': (16, True, [K(0, 'PawL', pitch=-30), K(0, 'PawR', pitch=30), K(0, 'Body', roll=4),
                            K(8, 'PawL', pitch=30), K(8, 'PawR', pitch=-30), K(8, 'Body', roll=-4),
                            K(0, 'FootL', fwd=0.03), K(8, 'FootR', fwd=0.03)]),
        # 앞발 할퀴기: 8 일어섬 → 12 판정
        'Attack': (22, False, [K(8, 'Body', pitch=-25), *paws(8, pitch=-60), K(8, 'Head', pitch=-10),
                               K(12, 'Body', pitch=15), *paws(12, pitch=10), K(12, 'Root', fwd=0.12), K(16, 'Root', fwd=0.12)]),
        # 파고들기: 끝 자세(땅속) 유지
        'Burrow': (24, False, [K(6, 'Body', pitch=20), *paws(6, pitch=-40), K(12, 'Body', pitch=24), *paws(12, pitch=35),
                               K(18, 'Root', up=UG * 0.6), *paws(18, pitch=-40), K(24, 'Root', up=UG), K(24, 'Body', pitch=0)]),
        # 땅속 이동(반복): 등 버섯만 흔들리며 이동(이동은 코드)
        'Underground': (20, True, [K(0, 'Root', up=UG), K(5, 'Root', up=UG, roll=4), K(10, 'Root', up=UG + 0.02),
                                   K(15, 'Root', up=UG, roll=-4)]),
        # 튀어나오기: 8 판정(띄우기) → 14 착지
        'Emerge': (26, False, [K(0, 'Root', up=UG), K(4, 'Root', up=UG + 0.1), K(8, 'Root', up=0.18), K(8, 'Body', pitch=-20),
                               *paws(8, pitch=-50), K(14, 'Root', up=0), K(14, 'Body', s=(1.08, 0.92, 1)), K(20, 'Body', pitch=0)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-15), K(3, 'Head', pitch=-15), K(3, 'Root', fwd=-0.06)]),
        'Death': (30, False, [K(8, 'Root', roll=25, up=0.04), K(18, 'Root', roll=90, up=0.25), K(22, 'Root', roll=84, up=0.23),
                              K(30, 'Root', roll=90, up=0.25), *paws(30, pitch=-30)]),
    }
    return 'Budeoji', S, bones, meshes, anims

# ---------- 버랑이: 호랑이 (평원 엘리트) ----------
def body_burangi():
    S = 0.1
    body, head, lFL, lBL, t1, t2 = {}, {}, {}, {}, {}, {}
    def coat(i, j, k):
        if k < 6.5:
            return 'tiger_white'
        if (j + (k // 2)) % 4 == 0 and abs(i + .5) > 1:
            return 'tiger_stripe'
        return 'tiger_dk' if _h(i, j, k) % 6 == 0 else 'tiger'
    ell(body, (0, 1.5, 9), (4.6, 9.5, 4.2), coat)
    ell(body, (0, -5, 10), (4.9, 5, 4.6), coat)                             # 가슴
    for sx in (1, -1):                                                      # 어깨·허벅지 살(다리와 안 겹치게 몸통에)
        ell(body, (sx * 3.2, -5, 8), (2.4, 3, 3.2), coat)
        ell(body, (sx * 3.2, 7.5, 8), (2.4, 3.2, 3.4), coat)
    def face(i, j, k):
        if k >= 13 and abs(i + .5) < 2.5 and (i + k) % 3 == 0:
            return 'tiger_stripe'                                           # 이마 줄무늬
        return 'tiger_white' if (k < 11 or abs(i + .5) > 3) else 'tiger'
    ell(head, (0, -11, 12), (4.0, 3.8, 3.6), face)
    ell(head, (0, -14.2, 10.8), (2.2, 1.6, 1.6), 'tiger_white')            # 주둥이
    paint_front(head, -1, 11, '!nose'); paint_front(head, 0, 11, '!nose')
    for i in (-3, 2):
        paint_front(head, i, 13, '!eye')
    for sx in (1, -1):
        ell(head, (sx * 2.6, -10.5, 15.2), (1.1, 0.8, 1.1), 'tiger_stripe')  # 귀
    box(lFL, 1.6, 4.6, -7, -3.5, 0, 8, lambda i, j, k: 'tiger_white' if k < 2 else coat(i, j, k + 3))
    box(lBL, 1.6, 4.6, 6, 9.5, 0, 8, lambda i, j, k: 'tiger_white' if k < 2 else coat(i, j, k + 3))
    for leg in (lFL, lBL):
        for i in (2, 3):
            paint_front(leg, i, 0, '!claw')
    lFR, lBR = B.sym(lFL, 0, 0), B.sym(lBL, 0, 0)
    for leg in (lFL, lFR, lBL, lBR):
        for key in [key for key in leg if key in body]:
            del leg[key]
    box(t1, -1, 1, 10, 16, 9, 11, lambda i, j, k: 'tiger_stripe' if j % 3 == 0 else 'tiger')
    box(t2, -1, 1, 16, 22, 9, 11, lambda i, j, k: 'tiger_stripe' if (j % 3 == 0 or j >= 20) else 'tiger')
    tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 7, 8), 'Root', None),
        ('Head', (0, -8.5, 11), 'Body', None),
        ('LegFL', (3.1, -5, 8), 'Body', None), ('LegFR', (-3.1, -5, 8), 'Body', None),
        ('LegBL', (3.1, 7.5, 8), 'Root', None), ('LegBR', (-3.1, 7.5, 8), 'Root', None),
        ('Tail1', (0, 10, 10), 'Body', None), ('Tail2', (0, 16, 10), 'Tail1', None),
        # 버섯 소켓(부위 파괴 대상)
        ('Socket_Head', (0, -10.5, tops(head, 0, -11)), 'Head', None),
        ('Socket_Back1', (0, -4, tops(body, 0, -4)), 'Body', None),
        ('Socket_Back2', (0, 5, tops(body, 0, 5)), 'Body', None),
        ('Socket_ShoulderL', (5.0, -5, 10.5), 'Body', (1, 0, 0.6)),
        ('Socket_ShoulderR', (-5.0, -5, 10.5), 'Body', (-1, 0, 0.6)),
        ('Hitbox_Head', (0, -12, 12), 'Head', None), ('Hitbox_Body', (0, 0, 9), 'Body', None),
    ]
    meshes = {'Burangi_Body': [(body, 'Body'), (head, 'Head'), (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR'),
                               (t1, 'Tail1'), (t2, 'Tail2')]}
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    def gallop(f, a, **body):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegFR', pitch=-a), K(f, 'LegBL', pitch=a), K(f, 'LegBR', pitch=a), K(f, 'Body', **body)]
    fronts = lambda f, **kw: [K(f, 'LegFL', **kw), K(f, 'LegFR', **kw)]
    backs = lambda f, **kw: [K(f, 'LegBL', **kw), K(f, 'LegBR', **kw)]
    tail = lambda f, a, b: [K(f, 'Tail1', yaw=a, pitch=-10), K(f, 'Tail2', yaw=b, pitch=-10)]
    anims = {
        'Idle': (60, True, [K(30, 'Body', s=(1.02, 1, 1)), *tail(0, 10, 15), *tail(30, -10, -15), K(20, 'Head', yaw=8), K(45, 'Head', yaw=-6)]),
        'Walk': (32, True, [*gait(0, 20), K(0, 'Body', roll=2), K(8, 'Body', up=0.03), *gait(16, -20), K(16, 'Body', roll=-2),
                            K(24, 'Body', up=0.03), *tail(0, 8, 12), *tail(16, -8, -12)]),
        'Run': (20, True, [*gallop(0, 30, pitch=4), *gallop(5, 0, up=0.12), *gallop(10, -30, pitch=-4), *gallop(15, 0, up=0.12),
                           *tail(0, 0, -10), *tail(10, 0, 10)]),
        # 오른 앞발 할퀴기. 판정 15
        'Attack': (28, False, [K(10, 'LegFR', pitch=-75, roll=-20), K(10, 'Body', yaw=12, pitch=-6), K(10, 'Head', yaw=-8),
                               K(15, 'LegFR', pitch=-10, roll=20), K(15, 'Body', yaw=-15, pitch=4), K(15, 'Head', yaw=10),
                               K(20, 'LegFR', pitch=-5)]),
        # 덮치기: 0~12 웅크림 → 18 도약 → 24 착지 판정 → 34 회복(이동은 코드)
        'Pounce': (44, False, [K(12, 'Body', up=-0.15, pitch=8), *backs(12, pitch=25), *fronts(12, pitch=10), K(12, 'Head', pitch=10),
                               *tail(12, 0, 20),
                               K(18, 'Root', up=0.6, fwd=1.2), K(18, 'Body', pitch=-15), *fronts(18, pitch=-60), *backs(18, pitch=45),
                               K(24, 'Root', fwd=2.2), K(24, 'Body', pitch=10, s=(1.04, 0.96, 1)), *fronts(24, pitch=-15), *backs(24, pitch=10),
                               K(34, 'Root', fwd=2.2), K(34, 'Body', pitch=0)]),
        # 위협 포효. 판정(경직) 14~30
        'Roar': (44, False, [K(10, 'Body', pitch=-8), K(10, 'Head', pitch=-25), *fronts(10, pitch=-8), *tail(10, 0, 25),
                             K(30, 'Body', pitch=-8), K(30, 'Head', pitch=-25), K(20, 'Head', pitch=-28, yaw=5)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-8), K(3, 'Head', pitch=-20), K(3, 'Root', fwd=-0.08)]),
        'Death': (44, False, [K(10, 'Body', pitch=8), K(10, 'Head', pitch=20), K(24, 'Root', roll=90, up=0.45),
                              K(28, 'Root', roll=84, up=0.42), K(44, 'Root', roll=90, up=0.45), K(44, 'Head', pitch=20),
                              *fronts(44, pitch=-20), *tail(44, 20, 10)]),
    }
    return 'Burangi', S, bones, meshes, anims

# ---------- 버구리 옛 버전(강아지 같다는 평으로 교체, 대장 요청으로 Buguri_Old로 남김) ----------
def body_old_buguri():
    S = 0.06
    body, head, armL, legL, tail = {}, {}, {}, {}, {}
    ell(body, (0, 1, 4.6), (3.6, 4.8, 3.5), lambda i, j, k: 'rac_belly' if (k < 3.6 and j < 2) else ('rac_dk' if _h(i, j, k) % 6 == 0 else 'rac'))
    def face(i, j, k):
        if 7 <= k <= 8 and abs(i + .5) >= 1:
            return 'rac_mask'                                               # 눈가 가면 무늬
        if k == 9 and 1 <= abs(i + .5) <= 2.5 and j < -5:
            return 'rac_white'                                              # 흰 눈썹
        return 'rac'
    ell(head, (0, -4.6, 8), (3.4, 3.0, 3.0), face)
    ell(head, (0, -7.4, 6.8), (1.6, 1.1, 1.1), 'rac_white')               # 주둥이
    paint_front(head, -1, 7, '!nose'); paint_front(head, 0, 7, '!nose')
    for i in (-2, 1):
        paint_front(head, i, 8, '!eye')
    for i in (-3, 2):
        paint_front(head, i, 6, '!pink')                                    # 볼터치
    for sx in (1, -1):
        ell(head, (sx * 2.3, -4.2, 11), (1.1, 0.7, 1.2), lambda i, j, k: 'rac_dk' if k >= 11 else 'rac')   # 귀
    ell(tail, (0, 8.2, 5.4), (1.9, 3.6, 1.9), lambda i, j, k: 'rac_dk' if (j % 2 == 0 or j >= 11) else 'rac')  # 줄무늬 꼬리
    box(armL, 1.2, 2.8, -3.2, -1.6, 0, 3, 'rac_dk')
    box(legL, 1.4, 3.0, 3.0, 4.6, 0, 3, 'rac_dk')
    armR, legR = B.sym(armL, 0, 0), B.sym(legL, 0, 0)
    tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 4, 3), 'Root', None),        # 엉덩이 축(앉기)
        ('Head', (0, -3.4, 7), 'Body', None),
        ('PawL', (2, -2.4, 3), 'Body', None), ('PawR', (-2, -2.4, 3), 'Body', None),
        ('LegL', (2.2, 3.8, 3), 'Root', None), ('LegR', (-2.2, 3.8, 3), 'Root', None),
        ('Tail', (0, 5.2, 5), 'Body', None),
        ('Socket_Head', (0, -4.6, tops(head, 0, -5)), 'Head', None),              # 버섯 모자
        ('Socket_Hold', (0, -5.5, 4), 'Body', (0, -1, 0.3)),                       # 앞발로 드는 버섯(Nibble)
    ]
    meshes = {'Buguri_Old_Body': [(body, 'Body'), (head, 'Head'), (armL, 'PawL'), (armR, 'PawR'), (legL, 'LegL'), (legR, 'LegR'), (tail, 'Tail')]}
    paws = lambda f, **kw: [K(f, 'PawL', **kw), K(f, 'PawR', **kw)]
    legs = lambda f, **kw: [K(f, 'LegL', **kw), K(f, 'LegR', **kw)]
    SIT = lambda f, **head: [K(f, 'Body', pitch=-32), K(f, 'Head', **{'pitch': 28, **head}), *paws(f, pitch=-20), *legs(f, pitch=-40)]
    anims = {
        # 앉아서 꼬리 흔들기
        'Idle': (60, True, [*SIT(0), K(0, 'Tail', yaw=15, pitch=30), *SIT(15, yaw=10), K(30, 'Tail', yaw=-15, pitch=30),
                            *SIT(45, yaw=-10), K(30, 'Body', pitch=-34, s=(1.03, 1, 1))]),
        'Walk': (16, True, [K(0, 'PawL', pitch=-25), K(0, 'LegR', pitch=-25), K(0, 'PawR', pitch=25), K(0, 'LegL', pitch=25),
                            K(8, 'PawL', pitch=25), K(8, 'LegR', pitch=25), K(8, 'PawR', pitch=-25), K(8, 'LegL', pitch=-25),
                            K(0, 'Body', roll=4), K(8, 'Body', roll=-4), K(0, 'Tail', yaw=12), K(8, 'Tail', yaw=-12)]),
        'Run': (12, True, [*paws(0, pitch=-35), *legs(0, pitch=35), K(0, 'Body', pitch=5), K(3, 'Root', up=0.06),
                           *paws(6, pitch=35), *legs(6, pitch=-35), K(6, 'Body', pitch=-5), K(9, 'Root', up=0.06)]),
        # 등 바구니에 탄 자세(반복): 몸은 바구니 속, 앞발은 테두리에
        'Ride': (60, True, [K(0, 'Root', up=-0.26), *SIT(0), *paws(0, pitch=-75), K(0, 'Tail', pitch=60),
                            K(30, 'Root', up=-0.26), *SIT(30, yaw=12), *paws(30, pitch=-70)]),
        # 기뻐서 폴짝
        'Happy': (30, False, [*SIT(0), K(6, 'Root', up=-0.02), *SIT(6), K(12, 'Root', up=0.22), K(12, 'Body', pitch=-55),
                              *paws(12, pitch=-130), K(12, 'Tail', yaw=25, pitch=40), K(18, 'Root', up=0), *SIT(18),
                              K(22, 'Tail', yaw=-25, pitch=30), *SIT(30)]),
        # 웅크려 자기(반복)
        'Sleep': (80, True, [K(0, 'Root', up=-0.06), K(0, 'Head', pitch=35, yaw=-30), K(0, 'Tail', yaw=70), *legs(0, pitch=-60), *paws(0, pitch=60),
                             K(40, 'Root', up=-0.06), K(40, 'Body', s=(1.05, 1.03, 1)), K(40, 'Head', pitch=35, yaw=-30)]),
        # 버섯 오물오물(반복): 앉아서 앞발을 입으로
        'Nibble': (24, True, [*SIT(0), *paws(0, pitch=-95), *SIT(6, pitch=36), *paws(6, pitch=-100), *SIT(12), *paws(12, pitch=-95),
                              *SIT(18, pitch=36), *paws(18, pitch=-100)]),
    }
    return 'Buguri_Old', S, bones, meshes, anims

# ---------- 버구리: 너구리 펫. 평소 플레이어 옆을 따라다니고, 스킬로 공격 ----------
# 너구리다움(관념적 너구리, 갈색·연갈색 배색): 진갈색 눈 가면 + 연갈색 눈썹·주둥이, 옆으로 뻗은 볼털, 굵은 줄무늬 꼬리, 진갈색 앞발, 통통하고 엉덩이가 높은 몸.
def body_buguri():
    S = 0.06
    C = 0                                                                   # 좌우 대칭 축(칸 -1|0 경계, sym: i -> -1-i)
    body, head, armL, legL, tail = {}, {}, {}, {}, {}
    def fur(i, j, k):
        if k >= 7.2:
            return 'racn_dk' if _h(i, j, k) % 5 else 'racn'                 # 등은 어둡게
        if j <= -1 and k <= 5:
            return 'racn_belly'
        return 'racn_dk' if _h(i, j, k) % 7 == 0 else 'racn'
    ell(body, (C, 1.4, 4.9), (4.2, 4.8, 3.8), fur)
    ell(body, (C, 4.0, 5.6), (3.6, 2.4, 3.4), fur)                          # 엉덩이 볼륨
    def face(i, j, k):
        a = abs(i + .5 - C)
        if j <= -5:
            if 8 <= k <= 9 and a >= 1.5:
                return 'racn_mask'                                          # 눈 가면
            if k == 7 and a >= 2.5:
                return 'racn_mask'                                          # 가면 아래로 처진 끝
            if a < 1.5 and k >= 8:
                return 'racn_dk'                                            # 콧등 줄
            if k >= 10:
                return 'racn_white'                                         # 흰 눈썹
            if k <= 7:
                return 'racn_white'                                         # 흰 뺨·턱
        return 'racn'
    ell(head, (C, -4.8, 8.6), (4.1, 3.0, 3.0), face)
    for sx in (1, -1):
        ell(head, (C + sx * 3.9, -5.0, 7.2), (1.2, 1.3, 1.4), 'racn_white')    # 옆으로 뻗은 볼털
        ell(head, (C + sx * 2.8, -4.0, 11.3), (1.2, 0.7, 1.3),
            lambda i, j, k: 'racn_white' if (k >= 12 or abs(i + .5 - C) >= 3.5) else 'racn_mask')   # 둥근 귀(흰 테두리)
    ell(head, (C, -7.7, 7.2), (1.6, 1.3, 1.2), 'racn_white')                # 뾰족한 주둥이
    paint_front(head, -1, 7, '!nose'); paint_front(head, 0, 7, '!nose')
    for i in (-2, 1):
        paint_front(head, i, 8, '!eye')                                     # 검은 눈(코 쪽으로 모으고 한 칸 내림)
    # 굵은 줄무늬 꼬리: 뒤로 뻗다 위로 휨
    def ring(i, j, k):
        t = j + 0.7 * (k - 5)
        if t >= 12.2:
            return 'racn_ring'                                              # 검은 꼬리 끝
        return 'racn_ring' if int(t / 1.6) % 2 == 0 else 'racn_tail'
    for c, r in (((C, 7.0, 5.8), 2.1), ((C, 8.8, 6.8), 2.5), ((C, 10.4, 8.4), 2.3), ((C, 11.4, 10.2), 1.8)):
        ell(tail, c, (r, r, r), ring)
    box(armL, 1.5, 3.0, -3.0, -1.5, 0, 3, lambda i, j, k: 'racn_glove' if k <= 1 else 'racn_dk')   # 검은 장갑
    box(legL, 1.5, 3.4, 2.6, 4.6, 0, 3, lambda i, j, k: 'racn_glove' if k <= 0 else 'racn_dk')
    armR, legR = B.sym(armL, 0, 0), B.sym(legL, 0, 0)
    tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 4, 3), 'Root', None),        # 엉덩이 축(일어서기·앉기)
        ('Head', (0, -3.4, 7), 'Body', None),
        ('PawL', (2.5, -2.2, 3.5), 'Body', None), ('PawR', (-3.5, -2.2, 3.5), 'Body', None),
        ('LegL', (2.5, 3.6, 3), 'Root', None), ('LegR', (-3.5, 3.6, 3), 'Root', None),
        ('Tail', (0, 5.8, 5.6), 'Body', None),
        ('Socket_Head', (0, -4.8, tops(head, 0, -5)), 'Head', None),              # 버섯 모자
        ('Socket_Hold', (-3.5, -3.4, 0.5), 'PawR', (0, -1, 0.3)),                  # 오른 앞발: 먹기·던지기용 버섯
    ]
    meshes = {'Buguri_Body': [(body, 'Body'), (head, 'Head'), (armL, 'PawL'), (armR, 'PawR'), (legL, 'LegL'), (legR, 'LegR'), (tail, 'Tail')]}
    paws = lambda f, **kw: [K(f, 'PawL', **kw), K(f, 'PawR', **kw)]
    legs = lambda f, **kw: [K(f, 'LegL', **kw), K(f, 'LegR', **kw)]
    SIT = lambda f, **head: [K(f, 'Body', pitch=-34), K(f, 'Head', **{'pitch': 30, **head}), *legs(f, pitch=-40)]
    UP = lambda f, **head: [K(f, 'Root', up=0.04), K(f, 'Body', pitch=-62), K(f, 'Head', **{'pitch': 55, **head}), *legs(f, pitch=-20)]
    anims = {
        # 앉아서 앞발 비비기(너구리 손 씻기), 꼬리 살랑
        'Idle': (64, True, [*SIT(0), K(0, 'PawL', pitch=-80, yaw=12), K(0, 'PawR', pitch=-70, yaw=-12), K(0, 'Tail', yaw=12, pitch=20),
                            *SIT(16, yaw=6), K(16, 'PawL', pitch=-70, yaw=-12), K(16, 'PawR', pitch=-80, yaw=12),
                            K(32, 'PawL', pitch=-80, yaw=12), K(32, 'PawR', pitch=-70, yaw=-12), K(32, 'Tail', yaw=-12, pitch=20),
                            *SIT(48, yaw=-6), K(48, 'PawL', pitch=-70, yaw=-12), K(48, 'PawR', pitch=-80, yaw=12)]),
        # 엉덩이를 들고 뒤뚱 걷기
        'Walk': (16, True, [K(0, 'Body', pitch=6, roll=5), K(8, 'Body', pitch=6, roll=-5),
                            K(0, 'PawL', pitch=-25), K(0, 'LegR', pitch=-25), K(0, 'PawR', pitch=25), K(0, 'LegL', pitch=25),
                            K(8, 'PawL', pitch=25), K(8, 'LegR', pitch=25), K(8, 'PawR', pitch=-25), K(8, 'LegL', pitch=-25),
                            K(0, 'Tail', yaw=10, pitch=10), K(8, 'Tail', yaw=-10, pitch=10)]),
        'Run': (12, True, [*paws(0, pitch=-40), *legs(0, pitch=40), K(0, 'Body', pitch=10), K(0, 'Tail', pitch=-10), K(3, 'Root', up=0.07),
                           *paws(6, pitch=40), *legs(6, pitch=-40), K(6, 'Body', pitch=-4), K(6, 'Tail', pitch=10), K(9, 'Root', up=0.07)]),
        # 기본 공격: 일어서서 양 앞발 번갈아 할퀴기
        'Attack': (24, False, [*UP(0), *paws(0, pitch=-60), K(6, 'PawL', pitch=-160), K(6, 'PawR', pitch=-100),
                               K(10, 'PawL', pitch=-20), K(10, 'Body', pitch=-50), K(13, 'PawR', pitch=-160),
                               K(17, 'PawR', pitch=-20), K(17, 'Body', pitch=-48), K(24, 'Body', pitch=0), *paws(24, pitch=0)]),
        # 스킬 1: 꼬리 회오리(제자리 한 바퀴, 꼬리를 쭉 펴서 휘두름)
        'Skill_Spin': (32, False, [K(0, 'Root', yaw=0), K(4, 'Body', pitch=12), K(4, 'Tail', pitch=-25, yaw=0), K(4, 'Root', yaw=-20),
                                   K(10, 'Root', yaw=120, up=0.06), K(16, 'Root', yaw=240, up=0.08), K(22, 'Root', yaw=360, up=0.04),
                                   K(10, 'Tail', pitch=-30, yaw=-25), K(22, 'Tail', pitch=-30, yaw=-25), K(26, 'Root', yaw=360, up=0),
                                   K(26, 'Body', pitch=0), K(32, 'Tail', pitch=0, yaw=0), K(32, 'Root', yaw=360)]),
        # 스킬 2: 버섯 던지기(Socket_Hold의 버섯을 머리 위로 들었다가 던짐)
        'Skill_Throw': (30, False, [*SIT(0), K(0, 'PawR', pitch=-60), K(10, 'Body', pitch=-55), K(10, 'Head', pitch=40),
                                    K(10, 'PawR', pitch=-200), K(10, 'PawL', pitch=-90), K(14, 'PawR', pitch=-30),
                                    K(14, 'Body', pitch=-15), K(14, 'Head', pitch=10), K(16, 'PawL', pitch=-40), *SIT(30), *paws(30, pitch=0)]),
        # 스킬 3: 몸통 박치기(웅크렸다 앞으로 튀어나감)
        'Skill_Pounce': (28, False, [K(0, 'Root', fwd=0), K(8, 'Root', up=-0.04, fwd=-0.04), K(8, 'Body', pitch=18), *legs(8, pitch=-30),
                                     K(8, 'Head', pitch=-10), K(13, 'Root', up=0.16, fwd=0.35), K(13, 'Body', pitch=-15), *paws(13, pitch=-70),
                                     *legs(13, pitch=50), K(13, 'Tail', pitch=-30), K(18, 'Root', up=0, fwd=0.55), K(18, 'Body', pitch=10),
                                     *paws(18, pitch=20), *legs(18, pitch=0), K(28, 'Root', fwd=0.55), K(28, 'Body', pitch=0), K(28, 'Tail', pitch=0)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-20), K(3, 'Head', pitch=-18), K(3, 'Root', fwd=-0.06), K(3, 'Tail', pitch=40)]),
        # 기뻐서 폴짝(일어서서 만세)
        'Happy': (30, False, [*SIT(0), K(6, 'Root', up=-0.02), K(12, 'Root', up=0.22), K(12, 'Body', pitch=-60),
                              *paws(12, pitch=-150), K(12, 'Tail', yaw=25, pitch=30), K(18, 'Root', up=0), *SIT(18),
                              K(22, 'Tail', yaw=-25, pitch=20), *SIT(30), *paws(30, pitch=0)]),
        # 꼬리로 몸을 감고 자기(반복)
        'Sleep': (80, True, [K(0, 'Root', up=-0.06), K(0, 'Head', pitch=35, yaw=-30), K(0, 'Tail', yaw=75, pitch=15), *legs(0, pitch=-60), *paws(0, pitch=60),
                             K(40, 'Root', up=-0.06), K(40, 'Body', s=(1.05, 1.03, 1)), K(40, 'Head', pitch=35, yaw=-30)]),
        # 버섯 오물오물(반복)
        'Nibble': (24, True, [*SIT(0), *paws(0, pitch=-95), *SIT(6, pitch=38), *paws(6, pitch=-100), *SIT(12), *paws(12, pitch=-95),
                              *SIT(18, pitch=38), *paws(18, pitch=-100)]),
    }
    return 'Buguri', S, bones, meshes, anims

# ---------- 북버여우: 북극여우 (얼음 엘리트). 뾰족 귀, 긴 주둥이, 몸만 한 솜꼬리 ----------
def body_bukbuyeou():
    S = 0.08
    body, head, lFL, lBL, t1, t2 = {}, {}, {}, {}, {}, {}
    def coat(i, j, k):
        if k < 6.5 and j < 3:
            return 'fox_belly'
        return 'fox_dk' if _h(i, j, k) % 5 == 0 else 'fox'
    ell(body, (0, 1, 8.5), (3.2, 7.5, 3.2), coat)
    ell(body, (0, -4.5, 9.2), (3.6, 3.4, 3.6), coat)                        # 가슴 털
    for sx in (1, -1):
        ell(body, (sx * 2.4, -4.5, 7.4), (1.8, 2.4, 2.6), coat)            # 어깨
        ell(body, (sx * 2.4, 6, 7.6), (1.8, 2.6, 2.8), coat)               # 허벅지
    ell(head, (0, -9, 11.6), (3.2, 2.8, 2.8), lambda i, j, k: 'fox_belly' if k < 10.5 else 'fox')
    for sx in (1, -1):
        ell(head, (sx * 3.2, -8.6, 10.6), (1.0, 1.4, 1.3), 'fox_belly')    # 볼 갈기
    ell(head, (0, -12.6, 10.4), (1.2, 2.6, 1.0), 'fox')                     # 길고 뾰족한 주둥이
    paint_front(head, -1, 11, '!nose'); paint_front(head, 0, 11, '!nose')
    for i in (-3, 2):
        paint_front(head, i, 12, '!eye')
    for sx in (1, -1):                                                      # 뾰족한 귀
        cx = sx * 2.2
        for w, z in ((1.9, 13), (1.6, 14), (1.1, 15), (0.6, 16)):
            box(head, cx - w, cx + w, -9.6, -7.6, z, z + 1,
                lambda i, j, k: 'fox_ear' if (j == -10 and abs(i + .5 - cx) < 1 and k < 16) else 'fox')
    box(lFL, 1.4, 3.2, -6, -4, 0, 7, lambda i, j, k: 'fox_paw' if k < 2 else coat(i, j, k + 3))
    box(lBL, 1.4, 3.2, 5, 7, 0, 7, lambda i, j, k: 'fox_paw' if k < 2 else coat(i, j, k + 3))
    lFR, lBR = B.sym(lFL, 0, 0), B.sym(lBL, 0, 0)
    for leg in (lFL, lFR, lBL, lBR):
        for key in [key for key in leg if key in body]:
            del leg[key]
    tailc = lambda i, j, k: 'fox_dk' if _h(i, j, k) % 4 == 0 else 'fox'
    ell(t1, (0, 10, 9.6), (2.0, 3.0, 2.0), tailc)
    ell(t2, (0, 15.2, 10.4), (3.0, 3.8, 3.0), tailc)                        # 몸만 한 솜꼬리
    tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 5, 8), 'Root', None),
        ('Head', (0, -7, 11), 'Body', None),
        ('LegFL', (2.3, -5, 7), 'Body', None), ('LegFR', (-2.3, -5, 7), 'Body', None),
        ('LegBL', (2.3, 6, 7), 'Root', None), ('LegBR', (-2.3, 6, 7), 'Root', None),
        ('Tail1', (0, 7.5, 9.5), 'Body', None), ('Tail2', (0, 12.5, 10), 'Tail1', None),
        # 버섯 소켓(부위 파괴 대상)
        ('Socket_Head', (0, -9, tops(head, 0, -9)), 'Head', None),
        ('Socket_Back1', (0, -3, tops(body, 0, -3)), 'Body', None),
        ('Socket_Back2', (0, 4, tops(body, 0, 4)), 'Body', None),
        ('Socket_Tail', (0, 15, tops(t2, 0, 15)), 'Tail2', None),
        ('Hitbox_Head', (0, -10, 11.5), 'Head', None), ('Hitbox_Body', (0, 0, 8.5), 'Body', None),
    ]
    meshes = {'BukBuyeou_Body': [(body, 'Body'), (head, 'Head'), (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR'),
                                 (t1, 'Tail1'), (t2, 'Tail2')]}
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    def gallop(f, a, **body):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegFR', pitch=-a), K(f, 'LegBL', pitch=a), K(f, 'LegBR', pitch=a), K(f, 'Body', **body)]
    fronts = lambda f, **kw: [K(f, 'LegFL', **kw), K(f, 'LegFR', **kw)]
    backs = lambda f, **kw: [K(f, 'LegBL', **kw), K(f, 'LegBR', **kw)]
    tail = lambda f, a, b, p=0: [K(f, 'Tail1', yaw=a, pitch=p), K(f, 'Tail2', yaw=b, pitch=p)]
    anims = {
        'Idle': (60, True, [K(30, 'Body', s=(1.02, 1, 1)), *tail(0, 12, 18, 10), *tail(30, -12, -18, 10), K(15, 'Head', yaw=12, roll=8),
                            K(40, 'Head', yaw=-8, roll=-6)]),
        'Walk': (28, True, [*gait(0, 22), K(7, 'Body', up=0.03), *gait(14, -22), K(21, 'Body', up=0.03), *tail(0, 8, 14, 5), *tail(14, -8, -14, 5)]),
        'Run': (16, True, [*gallop(0, 32, pitch=4), *gallop(4, 0, up=0.1), *gallop(8, -32, pitch=-4), *gallop(12, 0, up=0.1),
                           *tail(0, 0, -6, -10), *tail(8, 0, 6, -10)]),
        # 물기: 판정 12
        'Attack': (24, False, [K(7, 'Body', pitch=-6, up=-0.04), K(7, 'Head', pitch=-20), *backs(7, pitch=15),
                               K(12, 'Root', fwd=0.25), K(12, 'Body', pitch=8), K(12, 'Head', pitch=20), *fronts(12, pitch=-25),
                               K(16, 'Root', fwd=0.25), K(24, 'Root', fwd=0.2), K(24, 'Body', pitch=0)]),
        # 눈밭 다이빙(여우 사냥 점프): 0~10 웅크림 → 18 높이 도약 → 26 머리부터 꽂힘 판정 → 40 빠져나옴(이동은 코드)
        'Pounce': (48, False, [K(10, 'Body', up=-0.12, pitch=6), *backs(10, pitch=25), K(10, 'Head', pitch=-15), *tail(10, 0, 0, -15),
                               K(18, 'Root', up=1.1, fwd=1.0), K(18, 'Body', pitch=-35), *fronts(18, pitch=-50), *backs(18, pitch=40), *tail(18, 0, 0, -30),
                               K(26, 'Root', up=0.05, fwd=2.0), K(26, 'Body', pitch=65), K(26, 'Head', pitch=25), *fronts(26, pitch=-120), *tail(26, 0, 0, -40),
                               K(34, 'Root', up=0.0, fwd=2.0), K(34, 'Body', pitch=55), K(34, 'Head', pitch=20, yaw=15),
                               K(40, 'Head', pitch=10, yaw=-15), K(48, 'Root', fwd=2.0), K(48, 'Body', pitch=0), K(48, 'Head', pitch=0, yaw=0),
                               *fronts(48, pitch=0), *backs(48, pitch=0), *tail(48, 0, 0, 0)]),
        # 꼬리 휘두르기(몸을 돌려 솜꼬리로 쓸기): 판정 14
        'TailSwipe': (32, False, [K(8, 'Root', yaw=-40), *tail(8, 30, 35, 5), K(8, 'Body', roll=-6),
                                  K(14, 'Root', yaw=110), *tail(14, -45, -55, 8), K(14, 'Body', roll=8),
                                  K(20, 'Root', yaw=150), *tail(20, -20, -30, 5), K(32, 'Root', yaw=0), *tail(32, 0, 0, 0), K(32, 'Body', roll=0)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-10), K(3, 'Head', pitch=-20), K(3, 'Root', fwd=-0.08), *tail(3, 0, 0, 25)]),
        'Death': (40, False, [K(8, 'Body', pitch=8), K(8, 'Head', pitch=20), K(22, 'Root', roll=90, up=0.32),
                              K(26, 'Root', roll=84, up=0.3), K(40, 'Root', roll=90, up=0.32), K(40, 'Head', pitch=20),
                              *fronts(40, pitch=-20), *tail(40, 20, 15, 0)]),
    }
    return 'BukBuyeou', S, bones, meshes, anims

CHARS = [body_bukbuggi, body_budeoji, body_burangi, body_old_buguri, body_buguri, body_bukbuyeou]
PREVIEW_MUSH = {
    'BukBuggi': {'Socket_Head': ('Mush_Seori', 0.8)},
    'Buguri': {'Socket_Head': ('Mush_Gwangdae', 0.5)},
    'Buguri_Old': {'Socket_Head': ('Mush_Gwangdae', 0.55)},
    'Budeoji': {'Socket_Back': ('Mush_Songi', 1.0), 'Socket_Head': ('Mush_Pyogo', 0.5)},
    'BukBuyeou': {'Socket_Head': ('Mush_Seori', 0.5), 'Socket_Back1': ('Mush_Seori', 1.0), 'Socket_Back2': ('Mush_Noru', 0.9),
                  'Socket_Tail': ('Mush_Seori', 0.8)},
    'Burangi': {'Socket_Back1': ('Mush_Gwangdae', 1.3), 'Socket_Back2': ('Mush_Noru', 1.2),
                'Socket_ShoulderL': ('Mush_Pyogo', 0.9), 'Socket_ShoulderR': ('Mush_Pyogo', 0.9)},
}
KEYF = {'Attack': None, 'Pounce': [0, 12, 18, 24], 'Burrow': [0, 6, 18, 24], 'Emerge': [0, 4, 8, 14], 'Roar': [0, 10, 20, 44]}

def main():
    pv = B.PREVIEW
    B.PREVIEW = False
    I.main()                      # 앞 단계 전부(평원 맵 포함, 팔레트 칸 고정)
    B.PREVIEW = pv
    for fn in CHARS:
        L.reset()
        arm, objs, anims = B.build_character(fn)
        L.write_palette(B.PAL_PATH)
        L.export_fbx(os.path.join(B.MOD, arm.name + '.fbx'), True)
        mn, mx = L.world_bbox(objs)
        print(arm.name, len(objs[0].data.polygons), 'faces', 'size', [round(v, 2) for v in (mx - mn)], list(anims))
    if B.PREVIEW:
        only = os.environ.get('MOBS_ONLY')
        if only in (None, 'chars'):
            I.CHARS, I.PREVIEW_MUSH = CHARS, PREVIEW_MUSH       # 얼음 몹 미리보기 함수 재사용
            I.preview()
        if only in (None, 'map'):
            field_preview()
        if only in (None, 'ice'):
            ice_preview()

def field_preview():
    """평원 맵을 다시 만들어 새 몹 배치까지 미리보기(내보내기는 체인에서 이미 함)."""
    L.reset()
    root, spawns, ground = M.build_map()
    pm = dict(B.PREVIEW_MUSH); pm.update(PREVIEW_MUSH); pm.update(BT.PREVIEW_MUSH if hasattr(BT, 'PREVIEW_MUSH') else {})
    M.preview(root, spawns, ground,
              actors={'Player': B.body_player, 'Buggi': B.body_buggi, 'Bugom': B.body_bugom,
                      'Budeoji': body_budeoji, 'Burangi': body_burangi, 'Boss_Tree': BT.body_tree},
              preview_mush=pm, boss_scale=1.0)

def ice_preview():
    """얼음 맵을 새 스폰(잡몹 버귄+북버끼, 엘리트 버범+북버여우)으로 미리보기."""
    pm = dict(I.PREVIEW_MUSH); pm.update(PREVIEW_MUSH)
    I.build_ice_map(True, actors={'Player': B.body_player, 'Bugwin': I.body_bugwin, 'BukBuggi': body_bukbuggi,
                                  'Bubeom': I.body_bubeom, 'BukBuyeou': body_bukbuyeou, 'BukBugom': I.body_bukbugom},
                    preview_mush=pm)

if __name__ == '__main__':
    main()
