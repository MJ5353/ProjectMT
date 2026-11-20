# 얼음 지역 에셋 (Blender 5.x): 버귄(펭귄)·버범(물범) 잡몹, 북버곰(북극곰) 보스. 얼음 맵은 이어서 추가
# 사용: python build_ice.py -- <출력 폴더> [--preview]
# 팔레트 색 위치를 맞추려고 앞 단계(캐릭터·맵·보스 2종)를 먼저 다시 만든 뒤 덧붙인다.
import sys, os, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector
import voxlib as L
import build_voxel_assets as B
import build_boss_tree as BT
import build_map as M
from voxlib import box, ell, paint_front, _h

L.COLORS.update({
    'peng_black': (40, 46, 62), 'peng_black_lt': (58, 66, 86), 'peng_white': (238, 240, 244),
    'peng_yellow': (246, 196, 72), 'peng_orange': (236, 132, 48),
    'seal': (150, 158, 170), 'seal_dk': (112, 120, 134), 'seal_belly': (204, 208, 214),
    'snow': (240, 245, 250), 'snow_dk': (214, 224, 238), 'pine': (54, 104, 86), 'pine_dk': (40, 82, 70),
    'ice_c': (168, 216, 240), 'ice_c_lt': (222, 244, 255), 'ice_c_dk': (124, 180, 220),
    'pbear': (238, 238, 228), 'pbear_dk': (204, 208, 206), 'pbear_sh': (176, 186, 196), 'pbear_claw': (70, 72, 80),
})

def top_k(parts, i, j):
    """(i, j) 칸에서 가장 위 복셀 높이 + 1 (소켓을 표면에 붙이려고)."""
    ks = [k for v in parts for (a, b, k) in v if a == i and b == j]
    return max(ks) + 1 if ks else 0

# ---------- 버귄: 펭귄 (잡몹) ----------
def body_bugwin():
    S = 0.07
    body, head, flipL, footL = {}, {}, {}, {}
    def coat(i, j, k):
        if j + .5 < -0.6 and ((i + .5) / 3.0) ** 2 + ((k + .5 - 5.5) / 4.4) ** 2 <= 1:
            return '!peng_white'
        return '!peng_black'            # 얼룩 없이 단색(검은 점처럼 보이지 않게)
    ell(body, (0, 0, 5.8), (3.7, 3.3, 5.2), coat)
    ell(body, (0, 2.6, 1.6), (1.6, 1.4, 1.0), '!peng_black')            # 꽁지
    ell(head, (0, -0.4, 12), (3.0, 2.8, 2.8), '!peng_black')
    for i in (-2, 1):                                                   # 눈(검정, 흰자 없음)
        paint_front(head, i, 12, '!eye')
    for i in (-3, -2, 1, 2):                                            # 볼터치(핑크)
        paint_front(head, i, 11, '!pink') if i in (-3, 2) else None
    box(head, -1, 1, -4.8, -2.4, 10.3, 11.6, '!peng_orange')            # 부리
    box(flipL, 2.9, 3.9, -1.8, 1.4, 3.0, 8.6, '!peng_black_lt')   # 몸보다 살짝 밝게(그늘에서 구멍처럼 안 보이게)
    box(footL, 0.6, 2.6, -2.9, 0.6, 0, 1, '!peng_orange')
    flipR, footR = B.sym(flipL, 0, 0), B.sym(footL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 0, 1), 'Root', None), ('Head', (0, -0.4, 9.5), 'Body', None),
        ('FlipL', (3.5, 0, 8.5), 'Body', None), ('FlipR', (-3.5, 0, 8.5), 'Body', None),
        ('FootL', (1.6, -0.5, 1), 'Root', None), ('FootR', (-1.6, -0.5, 1), 'Root', None),
        ('Socket_Head', (0, -0.4, 14.5), 'Head', None),
        ('Socket_Back', (0, 3.0, 8.0), 'Body', (0, 1, 0.8)),
    ]
    meshes = {'Bugwin_Body': [(body, 'Body'), (head, 'Head'), (flipL, 'FlipL'), (flipR, 'FlipR'), (footL, 'FootL'), (footR, 'FootR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def flips(f, out, pitch=0):
        return [K(f, 'FlipL', roll=out, pitch=pitch), K(f, 'FlipR', roll=-out, pitch=pitch)]
    anims = {
        'Idle': (48, True, [K(12, 'Body', roll=3), K(36, 'Body', roll=-3), K(24, 'Head', yaw=12), *flips(24, 6)]),
        # 뒤뚱걸음
        'Walk': (16, True, [K(0, 'Body', roll=9), K(0, 'FootL', fwd=0.06, up=0.03), K(0, 'FootR', fwd=-0.04),
                            K(8, 'Body', roll=-9), K(8, 'FootR', fwd=0.06, up=0.03), K(8, 'FootL', fwd=-0.04),
                            *flips(0, 18), *flips(8, 18), K(4, 'Body', up=0.02), K(12, 'Body', up=0.02)]),
        # 부리 쪼기: 0~7 몸 젖힘(예비) → 11 판정
        'Attack': (20, False, [K(7, 'Body', pitch=-15), K(7, 'Head', pitch=-20), *flips(7, 30),
                               K(11, 'Body', pitch=25), K(11, 'Head', pitch=25), K(11, 'Root', fwd=0.15), *flips(11, 10),
                               K(14, 'Body', pitch=20), K(14, 'Root', fwd=0.15)]),
        # 배 미끄럼 돌진: 0~10 웅크림 → 14~34 엎드려 미끄러짐(이동은 코드, 부딪히면 판정) → 34~44 일어남
        'Slide': (44, False, [K(8, 'Body', pitch=18, up=-0.03), *flips(8, 20, pitch=-20),
                              K(14, 'Root', pitch=82, up=0.24), *flips(14, 60), K(14, 'FootL', pitch=-60), K(14, 'FootR', pitch=-60),
                              K(14, 'Head', pitch=-35),
                              K(34, 'Root', pitch=82, up=0.24), *flips(34, 60), K(34, 'FootL', pitch=-60), K(34, 'FootR', pitch=-60),
                              K(34, 'Head', pitch=-35),
                              K(40, 'Root', pitch=20, up=0.05), *flips(40, 25)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-15), K(3, 'Head', pitch=-12), *flips(3, 35), K(3, 'Root', fwd=-0.06)]),
        'Death': (30, False, [K(8, 'Root', roll=20, up=0.05), *flips(8, 40), K(18, 'Root', roll=90, up=0.26),
                              K(22, 'Root', roll=84, up=0.24), K(30, 'Root', roll=90, up=0.26), *flips(30, 50), K(30, 'Head', roll=10)]),
    }
    return 'Bugwin', S, bones, meshes, anims

# ---------- 버범: 물범 (잡몹) ----------
def body_bubeom():
    S = 0.07
    body, hips, head, flipL, tail = {}, {}, {}, {}, {}
    def coat(i, j, k):
        if k < 2:
            return 'seal_belly'
        return 'seal_dk' if _h(i // 2, j // 2, k // 2) % 6 == 0 else 'seal'
    ell(body, (0, -1, 3.9), (3.6, 5.6, 3.6), coat)
    ell(hips, (0, 6, 2.8), (2.8, 4.2, 2.6), coat)
    ell(head, (0, -7.4, 6.4), (3.0, 3.0, 2.9), coat)
    ell(head, (0, -10, 5.6), (1.7, 1.1, 1.2), '!seal_belly')          # 주둥이
    paint_front(head, -1, 6, '!nose'); paint_front(head, 0, 6, '!nose')
    for i in (-3, 2):
        paint_front(head, i, 7, '!eye')                                 # 큰 검은 눈
    for i in (-3, 2):
        paint_front(head, i, 5, '!pink')                                # 볼터치
    box(flipL, 2.6, 4.6, -4.4, -1.8, 0, 1.4, 'seal_dk')                 # 앞지느러미
    box(tail, 0.2, 2.8, 9.6, 12.6, 0, 1, 'seal_dk')                     # 뒷지느러미 (좌우 한 덩어리)
    for (i, j, k), c in list(tail.items()):
        tail[(-1 - i, j, k)] = c
    flipR = B.sym(flipL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 2, 3), 'Root', None),
        ('Head', (0, -5.5, 5.5), 'Body', None),
        ('FlipL', (3.4, -3, 1.4), 'Body', None), ('FlipR', (-3.4, -3, 1.4), 'Body', None),
        ('Hips', (0, 3, 2.6), 'Root', None), ('TailFlip', (0, 9.6, 0.8), 'Hips', None),
        ('Socket_Head', (0, -7.4, top_k([head], 0, -8)), 'Head', None),
        ('Socket_Back', (0, 0, top_k([body], 0, 0)), 'Body', None),
    ]
    meshes = {'Bubeom_Body': [(body, 'Body'), (hips, 'Hips'), (head, 'Head'), (flipL, 'FlipL'), (flipR, 'FlipR'), (tail, 'TailFlip')]}
    K = lambda f, b, **kw: (f, b, kw)
    flips = lambda f, **kw: [K(f, 'FlipL', **kw), K(f, 'FlipR', **{**kw, 'roll': -kw.get('roll', 0)})]
    anims = {
        'Idle': (60, True, [K(30, 'Body', s=(1.03, 1, 1)), K(20, 'Head', yaw=12), K(40, 'Head', yaw=-12),
                            K(30, 'TailFlip', pitch=-20), *flips(30, roll=10)]),
        # 꿈틀 전진: 앞몸 들기 → 뒷몸 당기기
        'Walk': (20, True, [K(5, 'Body', pitch=-12, up=0.03), K(5, 'Hips', pitch=8), *flips(5, pitch=-30),
                            K(10, 'Body', pitch=4), K(10, 'Head', pitch=6),
                            K(15, 'Hips', pitch=-12, up=0.03), K(15, 'Body', pitch=6), K(15, 'TailFlip', pitch=-25), *flips(15, pitch=20)]),
        # 머리 박치기: 0~8 젖힘 → 12 판정
        'Attack': (24, False, [K(8, 'Body', pitch=-22), K(8, 'Head', pitch=-15), K(8, 'Hips', pitch=8),
                               K(12, 'Root', fwd=0.25), K(12, 'Body', pitch=14), K(12, 'Head', pitch=22),
                               K(16, 'Root', fwd=0.25), K(16, 'Body', pitch=8)]),
        # 배치기: 0~10 상체 세움 → 16 뜀 → 20 엎어지며 판정(주변 충격) → 34 회복
        'Flop': (34, False, [K(10, 'Body', pitch=-38, up=0.05), K(10, 'Head', pitch=-20), K(10, 'Hips', pitch=10), *flips(10, roll=40),
                             K(16, 'Root', up=0.25, fwd=0.2), K(16, 'Body', pitch=-10), *flips(16, roll=60),
                             K(20, 'Root', fwd=0.4), K(20, 'Body', pitch=6, s=(1.12, 0.88, 1)), K(20, 'Hips', s=(1.1, 0.9, 1)), *flips(20, roll=-10),
                             K(26, 'Root', fwd=0.4), K(26, 'Body', pitch=2)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-14), K(3, 'Head', pitch=-14), K(3, 'Root', fwd=-0.06), *flips(3, roll=30)]),
        'Death': (30, False, [K(8, 'Root', roll=25, up=0.04), K(18, 'Root', roll=90, up=0.25), K(22, 'Root', roll=84, up=0.23),
                              K(30, 'Root', roll=90, up=0.25), *flips(30, roll=40), K(30, 'Head', pitch=10)]),
    }
    return 'Bubeom', S, bones, meshes, anims

# ---------- 북버곰: 북극곰 (보스, 부위 파괴) ----------
BS = 0.17   # 버우르스 보스와 같은 복셀 크기. 어깨 높이 약 5m

def body_bukbugom():
    S = BS
    body, neck, head, jaw, lFL, lBL = ({} for _ in range(6))
    def fur(i, j, k):
        if k < 9:
            return 'pbear_dk'                                         # 배 아래 그늘
        return 'pbear_dk' if _h(i // 2, j // 2, k // 2) % 9 == 0 else 'pbear'
    def leg_fur(i, j, k):
        return 'pbear_sh' if k < 2 else ('pbear_dk' if _h(i // 2, j // 2, k // 2) % 9 == 0 else 'pbear')
    ell(body, (0, 0, 15), (9.5, 17, 9), fur)
    ell(body, (0, -8, 18.5), (9, 8, 9), fur)              # 어깨 혹
    ell(body, (0, 10, 14.5), (9, 8, 8.6), fur)            # 엉덩이
    ell(body, (0, 17.8, 17), (2.2, 1.6, 2.2), 'pbear_dk') # 꼬리
    ell(neck, (0, -19, 17), (6.2, 6.5, 6.2), fur)
    ell(head, (0, -27, 18.2), (5.8, 6.2, 5.2), fur)
    ell(head, (0, -33.4, 16.6), (3.1, 3.2, 2.7), fur)     # 주둥이
    for i in (-1, 0):
        paint_front(head, i, 17, '!nose'); paint_front(head, i, 18, '!nose')
    for i in (-3, 2):
        paint_front(head, i, 20, '!eye')
    for sx in (1, -1):
        ell(head, (sx * 3.8, -25.5, 23), (1.6, 1.2, 1.6), 'pbear_dk')   # 귀
    ell(jaw, (0, -31.5, 14.2), (2.7, 3.6, 1.3), 'pbear_dk')
    # 어깨·허벅지 살은 몸통에 붙인다(다리와 겹쳐 생기는 검은 틈 방지). 다리는 상자만.
    for sx in (1, -1):
        ell(body, (sx * 7, -11, 13), (4.8, 5.2, 5.6), fur)
        ell(body, (sx * 7, 11, 12.5), (5, 5.8, 6), fur)
    box(lFL, 3.5, 10.5, -14.5, -7.5, 0, 16, leg_fur)
    box(lBL, 3.5, 10.5, 7.5, 14.5, 0, 15, leg_fur)
    for leg, y0 in ((lFL, -14.5), (lBL, 7.5)):
        for i in (4, 6, 8):
            paint_front(leg, i, 0, '!pbear_claw'); paint_front(leg, i + 1, 0, '!pbear_claw')
    lFR, lBR = B.sym(lFL, 0, 0), B.sym(lBL, 0, 0)
    for leg in (lFL, lFR, lBL, lBR):            # 몸통과 겹친 칸은 다리에서 뺀다(같은 자리 겹친 면이 검게 깨지는 것 방지)
        for key in [key for key in leg if key in body]:
            del leg[key]
    allp = [body, neck, head]
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 10, 14), 'Root', None),
        ('Neck', (0, -14, 17), 'Body', None), ('Head', (0, -23, 17.5), 'Neck', None), ('Jaw', (0, -29, 14.5), 'Head', None),
        ('LegFL', (7, -11, 14), 'Body', None), ('LegFR', (-7, -11, 14), 'Body', None),
        ('LegBL', (7, 11, 13), 'Root', None), ('LegBR', (-7, 11, 13), 'Root', None),
        # 파괴 부위 소켓 8곳
        ('Socket_Head', (0, -26, top_k([head], 0, -26)), 'Head', None),
        ('Socket_Hump', (0, -8, top_k(allp, 0, -8)), 'Body', None),
        ('Socket_Back', (0, 2, top_k(allp, 0, 2)), 'Body', None),
        ('Socket_Rump', (0, 12, top_k(allp, 0, 12)), 'Body', None),
        ('Socket_ShoulderL', (10, -10, 16), 'Body', (1, -0.2, 0.7)),
        ('Socket_ShoulderR', (-10, -10, 16), 'Body', (-1, -0.2, 0.7)),
        ('Socket_ThighL', (10.5, 11, 12), 'Body', (1, 0.2, 0.5)),
        ('Socket_ThighR', (-10.5, 11, 12), 'Body', (-1, 0.2, 0.5)),
        # 몸통 히트박스 기준점
        ('Hitbox_Head', (0, -28, 18), 'Head', (0, -1, 0)), ('Hitbox_Neck', (0, -19, 17), 'Neck', (0, -1, 0)),
        ('Hitbox_Body', (0, 0, 15), 'Body', (0, 1, 0)),
        ('Hitbox_PawFL', (7, -11, 4), 'LegFL', None), ('Hitbox_PawFR', (-7, -11, 4), 'LegFR', None),
        ('Hitbox_PawBL', (7, 11, 4), 'LegBL', None), ('Hitbox_PawBR', (-7, 11, 4), 'LegBR', None),
    ]
    meshes = {'BukBugom_Body': [(body, 'Body'), (neck, 'Neck'), (head, 'Head'), (jaw, 'Jaw'),
                                (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    def gallop(f, a, **body):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegFR', pitch=-a), K(f, 'LegBL', pitch=a), K(f, 'LegBR', pitch=a), K(f, 'Body', **body)]
    fronts = lambda f, **kw: [K(f, 'LegFL', **kw), K(f, 'LegFR', **kw)]
    backs = lambda f, **kw: [K(f, 'LegBL', **kw), K(f, 'LegBR', **kw)]
    anims = {
        'Idle': (80, True, [K(40, 'Body', s=(1.015, 1, 1)), K(40, 'Neck', pitch=5), K(20, 'Head', yaw=8), K(60, 'Head', yaw=-8)]),
        'Walk': (64, True, [*gait(0, 14), K(0, 'Body', roll=2.5), K(0, 'Neck', yaw=4, pitch=4),
                            K(16, 'Body', up=0.06), *gait(32, -14), K(32, 'Body', roll=-2.5), K(32, 'Neck', yaw=-4, pitch=4),
                            K(48, 'Body', up=0.06)]),
        'Run': (24, True, [*gallop(0, 26, pitch=4), K(0, 'Neck', pitch=12), *gallop(6, 0, up=0.15), *gallop(12, -26, pitch=-4),
                           K(12, 'Neck', pitch=8), *gallop(18, 0, up=0.15)]),
        # 일어서서 포효. 판정(경직·넉백) 24~56
        'Roar': (80, False, [K(18, 'Body', pitch=-48), *fronts(18, pitch=-25), K(18, 'Neck', pitch=-10), *backs(18, pitch=-10),
                             K(24, 'Body', pitch=-52), K(24, 'Neck', pitch=-25), K(24, 'Head', pitch=-10), K(24, 'Jaw', pitch=35),
                             *fronts(24, pitch=-40, roll=10),
                             K(40, 'Head', pitch=-10, yaw=5), K(56, 'Body', pitch=-52), K(56, 'Neck', pitch=-25), K(56, 'Jaw', pitch=35),
                             *fronts(56, pitch=-40, roll=10), *backs(56, pitch=-10),
                             K(66, 'Body', pitch=-5), K(66, 'Jaw', pitch=0), *fronts(66, pitch=-5)]),
        # 앞발 할퀴기(오른발). 판정 22
        'Swipe': (48, False, [K(14, 'Body', yaw=18, roll=-5), K(14, 'LegFR', pitch=-80, roll=-25), K(14, 'Neck', yaw=-10),
                              K(22, 'Body', yaw=-22, roll=4), K(22, 'LegFR', pitch=-25, roll=30), K(22, 'Neck', yaw=12),
                              K(28, 'Body', yaw=-18), K(28, 'LegFR', pitch=-10, roll=20)]),
        # 두 앞발 내려찍기(얼음 충격파). 판정 26
        'Slam': (60, False, [K(18, 'Body', pitch=-40, up=0.1), *fronts(18, pitch=-50), K(18, 'Neck', pitch=-15), *backs(18, pitch=-8),
                             K(26, 'Body', pitch=4, s=(1.03, 0.97, 1)), *fronts(26, pitch=-6), K(26, 'Neck', pitch=18), K(26, 'Head', pitch=6),
                             K(36, 'Body', pitch=2), K(36, 'Neck', pitch=10)]),
        # 광폭 연타(분노): 0~12 반쯤 일어섬 → 20 오른발 할퀴기 → 34 왼발 할퀴기 → 42 크게 일어섬 → 48 두 앞발 내려찍기 → 60~76 회복
        'Frenzy': (76, False, [K(12, 'Body', pitch=-30, yaw=14), K(12, 'LegFR', pitch=-85, roll=-25), *backs(12, pitch=-8), K(12, 'Neck', pitch=-12, yaw=-8),
                               K(12, 'Jaw', pitch=25), K(12, 'LegFL', pitch=-30),
                               K(20, 'Body', pitch=-24, yaw=-20), K(20, 'LegFR', pitch=-20, roll=30), K(20, 'LegFL', pitch=-70, roll=25),
                               K(20, 'Neck', yaw=12), K(20, 'Jaw', pitch=30),
                               K(34, 'Body', pitch=-24, yaw=20), K(34, 'LegFL', pitch=-20, roll=-30), K(34, 'LegFR', pitch=-50, roll=-10),
                               K(34, 'Neck', yaw=-12), K(34, 'Jaw', pitch=10),
                               K(42, 'Body', pitch=-50, yaw=0, up=0.15), *fronts(42, pitch=-55, roll=0), K(42, 'Neck', pitch=-22, yaw=0),
                               K(42, 'Head', pitch=-10), K(42, 'Jaw', pitch=35), *backs(42, pitch=-10),
                               K(48, 'Body', pitch=5, up=0.0, s=(1.04, 0.96, 1)), *fronts(48, pitch=-4), K(48, 'Neck', pitch=20), K(48, 'Head', pitch=6),
                               K(48, 'Jaw', pitch=10), *backs(48, pitch=0),
                               K(60, 'Body', pitch=3, s=(1, 1, 1)), K(60, 'Neck', pitch=12), *fronts(60, pitch=-4), K(60, 'Jaw', pitch=0)]),
        # 돌진: 0~20 준비, 20~55 질주(이동은 코드), 55~70 제동
        'Charge': (70, False, [K(14, 'Body', pitch=6, up=-0.12), K(14, 'Neck', pitch=18), *backs(14, pitch=20),
                               *gallop(20, 26, pitch=4), *gallop(26, -26, pitch=-4, up=0.15), *gallop(32, 26, pitch=4),
                               *gallop(38, -26, pitch=-4, up=0.15), *gallop(44, 26, pitch=4), *gallop(50, -26, pitch=-4, up=0.15),
                               K(20, 'Neck', pitch=20), K(50, 'Neck', pitch=20),
                               K(58, 'Body', pitch=-10), *fronts(58, pitch=-28), K(58, 'Neck', pitch=0), *backs(58, pitch=0)]),
        # 물기: 12 예비, 21 판정
        'Bite': (48, False, [K(12, 'Neck', pitch=-18), K(12, 'Head', pitch=-12), K(12, 'Jaw', pitch=30), K(12, 'Body', pitch=-6),
                             K(18, 'Root', fwd=0.8), K(18, 'Body', pitch=8), K(18, 'Neck', pitch=25), K(18, 'Jaw', pitch=35),
                             K(21, 'Root', fwd=0.8), K(21, 'Jaw', pitch=0), K(21, 'Neck', pitch=25), K(21, 'Body', pitch=8),
                             K(30, 'Root', fwd=0.7), K(30, 'Neck', pitch=12), K(30, 'Body', pitch=4)]),
        # 다운: 10~24 무너짐, 24~84 엎드림(약점 노출), 84~110 일어남
        'Down': (120, False, [K(10, 'Body', roll=8, pitch=-6), K(10, 'Neck', pitch=-18), K(10, 'Jaw', pitch=25),
                              K(24, 'Body', up=-0.95, roll=3), *fronts(24, pitch=-70), *backs(24, pitch=70),
                              K(24, 'Neck', pitch=28), K(24, 'Head', pitch=10), K(24, 'Jaw', pitch=10),
                              K(54, 'Body', up=-0.92, roll=3, s=(1, 1.02, 1)), K(54, 'Neck', pitch=26),
                              K(84, 'Body', up=-0.95, roll=3), *fronts(84, pitch=-70), *backs(84, pitch=70), K(84, 'Neck', pitch=28),
                              K(84, 'Head', pitch=10),
                              K(96, 'Body', up=-0.4, pitch=-6), *fronts(96, pitch=-20), *backs(96, pitch=25), K(96, 'Neck', pitch=-10),
                              K(110, 'Body', up=0)]),
        # 부위 파괴 경직
        'PartBreak': (60, False, [K(3, 'Body', pitch=-10, roll=10), K(3, 'Neck', pitch=-28, yaw=15), K(3, 'Jaw', pitch=25),
                                  K(10, 'Body', roll=-6), K(10, 'Neck', pitch=-14),
                                  K(18, 'Body', up=-0.26), *fronts(18, pitch=15), K(18, 'Neck', pitch=20), K(18, 'Head', pitch=10),
                                  K(28, 'Neck', pitch=20, yaw=6), K(38, 'Neck', pitch=20, yaw=-6),
                                  K(44, 'Body', up=-0.26), *fronts(44, pitch=15), K(44, 'Head', pitch=10),
                                  K(52, 'Body', up=0.0), K(52, 'Neck', pitch=0)]),
        'Hit': (18, False, [K(4, 'Body', pitch=-4), K(4, 'Neck', pitch=-12), K(4, 'Jaw', pitch=15)]),
        'Death': (90, False, [K(15, 'Neck', pitch=-28), K(15, 'Jaw', pitch=30), K(40, 'Root', roll=90, up=1.55),
                              K(46, 'Root', roll=84, up=1.48), K(90, 'Root', roll=90, up=1.55), K(90, 'Neck', pitch=20),
                              K(90, 'Jaw', pitch=20)]),
    }
    return 'Boss_BukBugom', S, bones, meshes, anims

PREVIEW_MUSH = {
    'Bugwin': {'Socket_Head': ('Mush_Songi', 0.7), 'Socket_Back': ('Mush_Malbul', 0.9)},
    'Bubeom': {'Socket_Head': ('Mush_Malbul', 0.7), 'Socket_Back': ('Mush_Cloud', 0.9)},
    'Boss_BukBugom': {'Socket_Head': ('Mush_Songi', 2.2), 'Socket_Hump': ('Mush_Malbul', 5.0), 'Socket_Back': ('Mush_Cloud', 4.0),
                      'Socket_Rump': ('Mush_Malbul', 4.0), 'Socket_ShoulderL': ('Mush_Cloud', 2.6), 'Socket_ShoulderR': ('Mush_Cloud', 2.6),
                      'Socket_ThighL': ('Mush_Songi', 2.4), 'Socket_ThighR': ('Mush_Songi', 2.4)},
}
CHARS = [body_bugwin, body_bubeom, body_bukbugom]

# =====================================================================
# 얼음 맵: 토벌 평원과 같은 구성(시작·중앙·엘리트 4·보스), 눈밭 + 얼음 호수
# =====================================================================
from voxlib import cyl

def snow_cap(v, cols=None):
    """기둥마다 가장 위 칸을 눈으로 덮는다(cols가 있으면 그 색 위에만)."""
    tops = {}
    for (i, j, k) in v:
        tops[(i, j)] = max(k, tops.get((i, j), -1))
    for (i, j), k in tops.items():
        if cols is None or v[(i, j, k)] in cols:
            v[(i, j, k)] = 'snow' if _h(i, j, k) % 5 else 'snow_dk'
    return v

def pine_snow():
    """눈 덮인 침엽수(높이 약 4.5m)."""
    v = {}
    cyl(v, 0, 0, 1.2, 0, 5, 'bark')
    for z0, r in [(3, 6.0), (7, 4.8), (10.5, 3.6), (13.5, 2.2)]:
        L._fill(v, ((-r, r), (-r, r), (z0, z0 + 4.5)),
                lambda x, y, z, z0=z0, r=r: x * x + y * y <= (r * (1 - (z - z0) / 4.5)) ** 2,
                lambda i, j, k: 'pine_dk' if _h(i, j, k) % 4 == 0 else 'pine')
    v[(0, 0, 18)] = 'snow'; v[(-1, 0, 18)] = 'snow'
    return snow_cap(v, ('pine', 'pine_dk'))

def ice_crystal():
    """비스듬히 솟은 얼음 결정 무리."""
    v = {}
    for (x, y, w, h, lean) in [(0, 0, 1.0, 16, 0.12), (2.5, 1, 0.8, 11, 0.3), (-2.5, 0.5, 0.8, 9, -0.3), (0.5, -2.5, 0.8, 8, 0.05), (-1.5, 2.5, 0.6, 6, -0.2)]:
        for k in range(h):
            ox = x + lean * k
            ww = w if k < h * 0.65 else w * 0.5          # 끝으로 갈수록 뾰족하게
            for i in range(int(math.floor(ox - ww)), int(math.ceil(ox + ww))):
                for j in range(int(math.floor(y - ww)), int(math.ceil(y + ww))):
                    c = 'ice_c_lt' if k >= h - 2 or (i + k) % 5 == 0 else ('ice_c_dk' if (j + k) % 4 == 0 else 'ice_c')
                    v[(i, j, k)] = c
    return v

def snow_rock():
    v = {}
    ell(v, (0, 0, 0.5), (3.5, 3.0, 3.4), lambda i, j, k: 'stone_dk' if _h(i, j, k) % 3 == 0 else 'stone', cut=lambda x, y, z: z >= 0)
    return snow_cap(v)

def frozen_log():
    return snow_cap(M.log(), ('bark', 'bark_dk', 'ring'))

def snow_drift():
    """낮은 눈더미(높이 약 0.6m)."""
    v = {}
    ell(v, (0, 0, 0), (5, 3.4, 2.6), lambda i, j, k: 'snow_dk' if _h(i, j, k) % 6 == 0 else 'snow', cut=lambda x, y, z: z >= 0)
    return v

def seori_cluster():
    """서리버섯 군락."""
    v = {}
    rnd = random.Random(29)
    for ox, oy in [(0, 0), (3, 1.5), (-2.5, 2), (1, -3), (-3, -2)]:
        h = rnd.uniform(3, 7); r = h * 0.3 + 0.8
        cyl(v, ox, oy, 1.0, 0, h + 1, 'seori_stem')
        L._fill(v, ((ox - r, ox + r), (oy - r, oy + r), (h, h + r * 1.6)),
                lambda x, y, z, ox=ox, oy=oy, h=h, r=r: (x - ox) ** 2 + (y - oy) ** 2 <= (r * (1 - (z - h) / (r * 1.6))) ** 2,
                lambda i, j, k: 'seori_lt' if _h(i, j, k) % 4 == 0 else 'seori')
    return v

def deco_snow_tuft():
    """눈 쌓인 마른 풀 한 포기."""
    v = {}
    for (x, y, h) in [(0, 0, 4), (1, 1, 3), (-1, 1, 3), (1, -1, 2), (-1, -1, 3)]:
        for k in range(h):
            v[(x, y, k)] = 'straw_dk'                                   # 마른 풀
        v[(x, y, h)] = 'snow'
    ell(v, (0, 0, 0), (2.6, 2.6, 1.2), 'snow', cut=lambda x, y, z: z >= 0)
    return v

def deco_ice_shard():
    v = {}
    for k in range(5):
        for (i, j) in [(0, 0), (-1, 0)] if k < 3 else [(0, 0)]:
            v[(i + (k // 2), j, k)] = 'ice_c_lt' if k == 4 else 'ice_c'
    for k in range(3):
        v[(-2, 1, k)] = 'ice_c_dk' if k < 2 else 'ice_c_lt'
    return v

M.PROPS.update({'pine_snow': pine_snow, 'ice_crystal': ice_crystal, 'snow_rock': snow_rock,
                'frozen_log': frozen_log, 'snow_drift': snow_drift, 'seori_cluster': seori_cluster})
B.DECOS.update({'Deco_SnowTuft': deco_snow_tuft, 'Deco_IceShard': deco_ice_shard})

ICE_REGION = {
    'name': 'Map_Ice01', 'seed': 21, 'size': 100,
    'ground': 'snow',
    'trees': {'pine_snow': 6, 'ice_crystal': 2},
    'small': {'snow_rock': 3, 'frozen_log': 2, 'ice_crystal': 2, 'snow_drift': 3, 'seori_cluster': 2, 'Deco_IceShard': 2},
    'grass_tries': 450,
    'path': 'snow_path', 'accent': 'ice',
    'lake': 'ice', 'lakes': [(14, 47, 4), (86, 47, 4), (20, 82, 5), (82, 82, 5)],   # 얼음 호수(소품 없음, 미끄럼 지대로 쓸 수 있음)
    'deco': {'Deco_SnowTuft': 0.75, 'Deco_IceShard': 0.25},
    'mob': ['Bugwin', 'BukBuggi'], 'elite': ['Bubeom', 'BukBuyeou'], 'boss': 'BukBugom',   # 26.11.17 대장 지역 구성
    'elite_props': ['snow_rock', 'frozen_log', 'ice_crystal'], 'boss_prop': 'ice_crystal',
}

def build_ice_map(preview=False, actors=None, preview_mush=None):
    field = M.REGION
    M.REGION = ICE_REGION
    try:
        L.reset()
        root, spawns, ground = M.build_map()
        L.write_palette(B.PAL_PATH)
        bpy.data.images['VoxelPalette.png'].reload()
        L.export_fbx(os.path.join(B.MOD, ICE_REGION['name'] + '.fbx'), False)
        for o in bpy.data.objects:
            if o.type == 'MESH':
                print(o.name, len(o.data.polygons), 'faces')
        if preview:
            M.preview(root, spawns, ground,
                      actors=actors or {'Player': B.body_player, 'Bugwin': body_bugwin, 'Bubeom': body_bubeom, 'BukBugom': body_bukbugom},
                      preview_mush=preview_mush or PREVIEW_MUSH, boss_scale=1.0)
            for f in ('map', 'map_tps'):
                os.replace(os.path.join(B.PRE, f + '.png'), os.path.join(B.PRE, f.replace('map', 'map_ice') + '.png'))
    finally:
        M.REGION = field

def main():
    pv = B.PREVIEW
    B.PREVIEW = False
    BT.main()                     # 앞 단계 전부 (팔레트 순서 유지)
    B.PREVIEW = pv
    for fn in CHARS:
        L.reset()
        arm, objs, anims = B.build_character(fn)
        L.write_palette(B.PAL_PATH)
        L.export_fbx(os.path.join(B.MOD, arm.name + '.fbx'), True)
        mn, mx = L.world_bbox(objs)
        print(arm.name, len(objs[0].data.polygons), 'faces', 'size', [round(v, 2) for v in (mx - mn)], list(anims))
    build_ice_map(B.PREVIEW and os.environ.get('ICE_ONLY') in (None, 'map'))
    if B.PREVIEW and os.environ.get('ICE_ONLY') != 'map':
        preview()

def preview():
    from PIL import Image, ImageDraw
    only = os.environ.get('ICE_ONLY')
    for fn in CHARS:
        if only and not any(o in fn.__name__ for o in only.split(',')):
            continue
        L.reset(); L.setup_render(240, 12)
        arm, objs, anims = B.build_character(fn)
        extra = [B.attach_mush(arm, s, m, sc) for s, (m, sc) in PREVIEW_MUSH.get(arm.name, {}).items()]
        bpy.data.images['VoxelPalette.png'].reload()
        mn, mx = L.world_bbox(objs + extra)
        big = arm.name.startswith('Boss')
        L.aim_camera((mn + mx) / 2, max((mx - mn).length, 1.0) * (0.8 if big else 1.2))
        keyf = {'Attack': [0, 7, 11, 20], 'Slide': [0, 8, 20, 44], 'Flop': [0, 10, 16, 20], 'Roar': [0, 18, 24, 66],
                'Swipe': [0, 14, 22, 48], 'Slam': [0, 18, 26, 60], 'Charge': [0, 14, 26, 58], 'Bite': [0, 12, 21, 48],
                'PartBreak': [0, 3, 18, 60], 'Down': [0, 24, 96, 120],
                'Death': [0, 15, 40, 90] if big else [0, 8, 18, 30]}
        rows = []
        for an, (length, loop, keys) in anims.items():
            arm.animation_data.action = bpy.data.actions[an]
            row = []
            for f in keyf.get(an) or [round(length * t) for t in ((0, .25, .5, .75) if loop else (0, .3, .55, 1))]:
                bpy.context.scene.frame_set(f)
                p = os.path.join(B.PRE, '_tmp.png'); L.render(p)
                row.append((Image.open(p).copy(), f'{an} {f}'))
            rows.append(row)
        Wd = 240
        sheet = Image.new('RGBA', (Wd * 4, Wd * len(rows)), (232, 236, 242, 255))
        d = ImageDraw.Draw(sheet)
        for r, row in enumerate(rows):
            for c, (im, label) in enumerate(row):
                sheet.alpha_composite(im, (c * Wd, r * Wd)); d.text((c * Wd + 6, r * Wd + 4), label, fill=(40, 40, 50, 255))
        sheet.save(os.path.join(B.PRE, f'anim_{arm.name}.png'))
        os.remove(os.path.join(B.PRE, '_tmp.png'))
        # 단독 + 플레이어 크기 비교
        L.reset(); L.setup_render(900, 20)
        arm, objs, _ = B.build_character(fn, with_anims=False)
        for s, (m, sc) in PREVIEW_MUSH.get(arm.name, {}).items():
            B.attach_mush(arm, s, m, sc)
        parm, _, _ = B.build_character(B.body_player, with_anims=False)
        mn, mx = L.world_bbox(objs)
        size = mx - mn
        parm.location = (mx.x + 0.6, mn.y + size.y * 0.3, 0); parm.rotation_euler = (0, 0, math.radians(200))
        bpy.data.images['VoxelPalette.png'].reload()
        c = Vector(((mn.x + mx.x + 0.6) / 2, (mn.y + mx.y) / 2, size.z / 2))
        L.aim_camera(c, max(size.length, 3.0) * 1.15, view=(-0.8, -1.3, 0.5))
        L.render(os.path.join(B.PRE, f'{arm.name.lower()}.png'))
        parm.hide_render = True
        for o in parm.children: o.hide_render = True
        L.aim_camera(Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, size.z * 0.55)), max(size.z, size.x) * 1.5, view=(-0.35, -1.3, 0.25))
        L.render(os.path.join(B.PRE, f'{arm.name.lower()}_front.png'))

if __name__ == '__main__':
    main()
