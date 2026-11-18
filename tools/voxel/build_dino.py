# 공룡 지역 (Blender 5.x): 잡몹 버랍터(랩터)·버파키(박치기 공룡), 엘리트 버케라(뿔 공룡) + 기존 버우르스, 보스 버우르스 보스
# 맵 Map_Dino01: 고사리 밀림 + 화산(현무암·용암 웅덩이)
# 사용: python build_dino.py -- <출력 폴더> [--preview]
# 체인 맨 끝: 앞 단계(캐릭터·맵·보스·얼음·추가 몹)를 전부 다시 만든 뒤 덧붙인다(팔레트 칸은 palette_index.json으로 고정).
# 미리보기만 줄이기: DINO_ONLY=chars|map, ICE_ONLY=<함수 이름 일부,쉼표로 여럿>
import sys, os, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
import voxlib as L
import build_voxel_assets as B
import build_map as M
import build_boss as BB
import build_ice as I
import build_more_mobs as MM
from voxlib import box, ell, cyl, paint_front, _h

L.COLORS.update({
    'rap': (92, 146, 112), 'rap_dk': (66, 112, 84), 'rap_belly': (226, 214, 170), 'rap_stripe': (52, 70, 58), 'rap_crest': (228, 112, 58),
    'pachy': (172, 118, 76), 'pachy_dk': (140, 94, 60), 'pachy_belly': (228, 202, 160), 'pachy_dome': (234, 216, 182), 'pachy_knob': (118, 88, 66),
    'cera': (118, 136, 108), 'cera_dk': (94, 110, 86), 'cera_belly': (204, 198, 160), 'cera_frill': (216, 122, 64), 'cera_frill_dk': (176, 88, 50),
    'cera_horn': (238, 226, 196), 'cera_beak': (70, 64, 60),
    'cycad_trunk': (124, 94, 62), 'cycad_band': (94, 70, 48), 'frond': (72, 142, 70), 'frond_dk': (50, 110, 56), 'frond_lt': (104, 170, 84),
    'basalt_c': (58, 54, 62), 'basalt_lt': (82, 78, 88), 'lava_glow': (250, 140, 40),
    'bone': (232, 224, 200), 'bone_dk': (198, 188, 162), 'twig': (120, 90, 58), 'egg': (236, 230, 210), 'egg_spot': (150, 120, 90),
})

K = lambda f, b, **kw: (f, b, kw)
tops = lambda v, i, j: max(k for (a, b, k) in v if a == i and b == j) + 1

def dedupe(body, head, *others):
    """같은 칸을 두 부위가 차지하면 면이 겹쳐 검게 깨진다: 몸통에서 머리 칸을, 나머지 부위에서 몸통 칸을 뺀다."""
    for key in head:
        body.pop(key, None)
    for o in others:
        for key in [key for key in o if key in body or key in head]:
            del o[key]

# =====================================================================
# 버랍터: 두 발 사냥꾼(잡몹). 무리로 둘러싸고 뛰어올라 발톱 차기
# =====================================================================
def body_buraptor():
    S = 0.07
    body, head, t1, t2, armL, legL = {}, {}, {}, {}, {}, {}
    def skin(i, j, k):
        if k < 9.6:
            return 'rap_belly'
        if (j % 3 == 0) and k > 11:
            return 'rap_stripe'                                             # 등 줄무늬
        return 'rap_dk' if _h(i, j, k) % 6 == 0 else 'rap'
    ell(body, (0, -0.5, 11), (2.6, 5.0, 2.8), skin)
    ell(body, (0, -5, 12.6), (1.6, 2.0, 1.8), skin)                         # 목
    ell(head, (0, -7.8, 14.2), (2.0, 2.6, 1.9), lambda i, j, k: 'rap_belly' if k < 13.4 else 'rap')
    ell(head, (0, -10.6, 13.6), (1.4, 2.2, 1.2), lambda i, j, k: 'rap_belly' if k < 13.4 else 'rap')   # 긴 주둥이
    for i in (-2, 1):
        paint_front(head, i, 14, '!eye')
    for i in (-1, 0):
        paint_front(head, i, 13, '!tooth')
    for j in range(-9, -2, 2):                                              # 머리~목 볏 깃털
        ks = [k for (a, b, k) in head if a in (-1, 0) and b == j] or [k for (a, b, k) in body if a in (-1, 0) and b == j]
        if ks:
            for i in (-1, 0):
                (head if j <= -6 else body)[(i, j, max(ks) + 1)] = 'rap_crest'
    ell(t1, (0, 6.5, 11.4), (1.8, 3.6, 1.8), skin)
    ell(t2, (0, 12.4, 12), (1.1, 3.6, 1.0), skin)
    box(armL, 1.6, 2.8, -5.5, -4.2, 7, 10, 'rap_dk')                         # 짧은 앞발
    ell(legL, (2, 1, 9.4), (1.3, 2.0, 2.2), 'rap')                          # 허벅지
    box(legL, 1.2, 2.8, 1.5, 3.0, 1, 7, 'rap_dk')                           # 정강이
    box(legL, 1.2, 2.8, -1.5, 3.0, 0, 1, 'rap_dk')                          # 발
    paint_front(legL, 1, 1, '!claw'); paint_front(legL, 2, 0, '!claw')      # 갈고리 발톱
    dedupe(body, head, t1, armL, legL)
    for key in [key for key in t2 if key in t1]:
        del t2[key]
    armR, legR = B.sym(armL, 0, 0), B.sym(legL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 1, 10), 'Root', None),
        ('Head', (0, -5.8, 13), 'Body', None),
        ('Tail1', (0, 4, 11), 'Body', None), ('Tail2', (0, 9.5, 12), 'Tail1', None),
        ('ArmL', (2.2, -4.6, 10), 'Body', None), ('ArmR', (-2.2, -4.6, 10), 'Body', None),
        ('LegL', (2, 1, 10), 'Root', None), ('LegR', (-2, 1, 10), 'Root', None),
        ('Socket_Head', (0, -7.6, tops(head, 0, -8)), 'Head', None),
        ('Socket_Back', (0, 0, tops(body, 0, 0)), 'Body', None),
    ]
    meshes = {'Buraptor_Body': [(body, 'Body'), (head, 'Head'), (t1, 'Tail1'), (t2, 'Tail2'),
                                (armL, 'ArmL'), (armR, 'ArmR'), (legL, 'LegL'), (legR, 'LegR')]}
    legs = lambda f, a: [K(f, 'LegL', pitch=-a), K(f, 'LegR', pitch=a)]
    tail = lambda f, a, p=0: [K(f, 'Tail1', yaw=a, pitch=p), K(f, 'Tail2', yaw=a * 1.4, pitch=p)]
    anims = {
        'Idle': (48, True, [K(0, 'Head', yaw=0), K(12, 'Head', yaw=25, roll=10), K(24, 'Head', yaw=0), K(36, 'Head', yaw=-20, roll=-8),
                            K(24, 'Body', s=(1, 1.02, 1)), *tail(0, 8), *tail(24, -8)]),
        'Walk': (20, True, [*legs(0, 28), K(0, 'Body', up=0.0, roll=3), K(5, 'Body', up=0.04), *legs(10, -28), K(10, 'Body', roll=-3),
                            K(15, 'Body', up=0.04), *tail(0, 10), *tail(10, -10), K(0, 'Head', pitch=-4), K(5, 'Head', pitch=4)]),
        'Run': (12, True, [*legs(0, 50), K(0, 'Body', pitch=8), K(3, 'Root', up=0.12), *legs(6, -50), K(9, 'Root', up=0.12),
                           *tail(0, 4, -8), *tail(6, -4, -8), K(0, 'Head', pitch=-8)]),
        # 뛰어올라 갈고리 발톱 차기. 판정 14
        'Attack': (28, False, [K(7, 'Root', up=-0.05), K(7, 'Body', pitch=12), *legs(7, -20), K(7, 'Head', pitch=-15),
                               K(12, 'Root', up=0.5, fwd=0.6), K(12, 'Body', pitch=-30), K(12, 'LegL', pitch=-80), K(12, 'LegR', pitch=-70),
                               K(12, 'ArmL', pitch=-60), K(12, 'ArmR', pitch=-60), *tail(12, 0, 20),
                               K(16, 'Root', up=0.3, fwd=0.9), K(16, 'LegL', pitch=-30), K(16, 'LegR', pitch=-30),
                               K(20, 'Root', up=0, fwd=1.0), K(20, 'Body', pitch=6), K(28, 'Root', fwd=1.0), K(28, 'Body', pitch=0),
                               *legs(28, 0), K(28, 'ArmL', pitch=0), K(28, 'ArmR', pitch=0), *tail(28, 0)]),
        # 울음(무리 부르기)
        'Call': (30, False, [K(8, 'Head', pitch=-35), K(8, 'Body', pitch=-10), K(12, 'Head', pitch=-40, yaw=5), K(20, 'Head', pitch=-38, yaw=-5),
                             *tail(10, 0, 15), K(30, 'Head', pitch=0), K(30, 'Body', pitch=0)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-12), K(3, 'Head', pitch=-20), K(3, 'Root', fwd=-0.08)]),
        'Death': (36, False, [K(8, 'Body', pitch=-15), K(8, 'Head', pitch=-25), K(20, 'Root', roll=90, up=0.2),
                              K(24, 'Root', roll=84, up=0.18), K(36, 'Root', roll=90, up=0.2), *legs(36, 20), *tail(36, 15)]),
    }
    return 'Buraptor', S, bones, meshes, anims

# =====================================================================
# 버파키: 돌머리 박치기 공룡(잡몹). 둥근 머리뼈로 들이받기
# =====================================================================
def body_bupachy():
    S = 0.08
    body, head, t1, t2, armL, legL = {}, {}, {}, {}, {}, {}
    def skin(i, j, k):
        if k < 9.4:
            return 'pachy_belly'
        return 'pachy_dk' if _h(i, j, k) % 5 == 0 else 'pachy'
    ell(body, (0, 0, 11), (3.4, 5.2, 3.4), skin)
    ell(body, (0, -5, 12.8), (2.2, 2.2, 2.2), skin)
    ell(head, (0, -7.6, 14.0), (2.4, 2.6, 2.2), lambda i, j, k: 'pachy_belly' if k < 13 else 'pachy')
    ell(head, (0, -10.2, 13.2), (1.3, 1.3, 1.1), 'pachy')                  # 짧은 주둥이
    ell(head, (0, -7.4, 16.0), (2.7, 2.7, 2.4), lambda i, j, k: 'pachy_dome' if k >= 15 else 'pachy_knob')   # 돌머리
    for a in range(0, 360, 40):                                             # 머리뼈 둘레 돌기
        i, j = round(2.8 * math.cos(math.radians(a))), round(-7.4 + 2.8 * math.sin(math.radians(a)))
        head[(i, j, 15)] = '!pachy_knob'
    for i in (-2, 1):
        paint_front(head, i, 14, '!eye')
    ell(t1, (0, 7, 11.6), (2.0, 3.8, 2.0), skin)
    ell(t2, (0, 12.4, 12.2), (1.2, 3.2, 1.1), skin)
    box(armL, 2.0, 3.2, -5.2, -4.0, 8, 10.5, 'pachy_dk')
    ell(legL, (2.4, 1, 9.4), (1.5, 2.2, 2.4), 'pachy')
    box(legL, 1.2, 3.2, 1.0, 3.0, 1, 7, 'pachy_dk')
    box(legL, 1.2, 3.2, -1.0, 3.0, 0, 1, 'pachy_dk')
    dedupe(body, head, t1, armL, legL)
    for key in [key for key in t2 if key in t1]:
        del t2[key]
    armR, legR = B.sym(armL, 0, 0), B.sym(legL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 1, 10), 'Root', None),
        ('Head', (0, -5.6, 13), 'Body', None),
        ('Tail1', (0, 4.5, 11), 'Body', None), ('Tail2', (0, 10, 12), 'Tail1', None),
        ('ArmL', (2.6, -4.6, 10.5), 'Body', None), ('ArmR', (-2.6, -4.6, 10.5), 'Body', None),
        ('LegL', (2.4, 1, 10), 'Root', None), ('LegR', (-2.4, 1, 10), 'Root', None),
        ('Socket_Head', (0, -7.4, tops(head, 0, -8)), 'Head', None),
        ('Socket_Back', (0, 1, tops(body, 0, 1)), 'Body', None),
    ]
    meshes = {'Bupachy_Body': [(body, 'Body'), (head, 'Head'), (t1, 'Tail1'), (t2, 'Tail2'),
                               (armL, 'ArmL'), (armR, 'ArmR'), (legL, 'LegL'), (legR, 'LegR')]}
    legs = lambda f, a: [K(f, 'LegL', pitch=-a), K(f, 'LegR', pitch=a)]
    tail = lambda f, a, p=0: [K(f, 'Tail1', yaw=a, pitch=p), K(f, 'Tail2', yaw=a * 1.4, pitch=p)]
    anims = {
        'Idle': (56, True, [K(28, 'Body', s=(1, 1.02, 1)), K(14, 'Head', pitch=10), K(42, 'Head', pitch=-4, yaw=10), *tail(0, 6), *tail(28, -6)]),
        'Walk': (24, True, [*legs(0, 24), K(0, 'Body', roll=4), K(6, 'Body', up=0.04), *legs(12, -24), K(12, 'Body', roll=-4),
                            K(18, 'Body', up=0.04), *tail(0, 8), *tail(12, -8)]),
        'Run': (14, True, [*legs(0, 42), K(0, 'Body', pitch=12), K(3, 'Root', up=0.1), *legs(7, -42), K(10, 'Root', up=0.1),
                           K(0, 'Head', pitch=20), *tail(0, 0, -6)]),
        # 박치기 돌진: 0~12 고개 숙여 발 구르기 → 14~22 돌진(판정 20) → 30 회복(이동은 코드)
        'Attack': (32, False, [K(6, 'Body', pitch=18), K(6, 'Head', pitch=45), K(6, 'LegL', pitch=20), K(10, 'LegL', pitch=-10),
                               K(12, 'Body', pitch=20), K(12, 'Head', pitch=50), *legs(12, 0),
                               K(16, 'Root', fwd=0.6), *legs(16, 40), K(20, 'Root', fwd=1.4), *legs(20, -40), K(20, 'Body', pitch=24),
                               K(22, 'Root', fwd=1.5), K(22, 'Body', pitch=-6, s=(1.04, 0.96, 1)), K(22, 'Head', pitch=30),
                               K(32, 'Root', fwd=1.5), K(32, 'Body', pitch=0), K(32, 'Head', pitch=0), *legs(32, 0)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-12), K(3, 'Head', pitch=-20), K(3, 'Root', fwd=-0.08)]),
        'Death': (36, False, [K(8, 'Body', pitch=-12), K(8, 'Head', pitch=-20), K(20, 'Root', roll=90, up=0.26),
                              K(24, 'Root', roll=84, up=0.24), K(36, 'Root', roll=90, up=0.26), *legs(36, 20), *tail(36, 15)]),
    }
    return 'Bupachy', S, bones, meshes, anims

# =====================================================================
# 버케라: 뿔 셋 + 목 방패(엘리트). 버섯은 목 방패와 등·꼬리에
# =====================================================================
def body_bukera():
    S = 0.1
    body, head, lFL, lBL, t1, t2 = {}, {}, {}, {}, {}, {}
    def skin(i, j, k):
        if k < 6.5:
            return 'cera_belly'
        return 'cera_dk' if _h(i // 2, j // 2, k // 2) % 4 == 0 else 'cera'
    ell(body, (0, 2, 10), (5.0, 9.0, 5.0), skin)
    ell(body, (0, -5.5, 9.6), (4.4, 3.4, 4.2), skin)
    for sx in (1, -1):
        ell(body, (sx * 3.4, -4.5, 7.6), (2.4, 2.8, 3.2), skin)
        ell(body, (sx * 3.6, 7.5, 8), (2.6, 3.4, 3.6), skin)
    ell(head, (0, -10, 9.4), (3.4, 3.6, 3.2), lambda i, j, k: 'cera_belly' if k < 7.5 else 'cera')
    ell(head, (0, -13.6, 8.4), (1.7, 1.8, 1.7), lambda i, j, k: 'cera_beak' if (j <= -14 or k < 8) else 'cera')   # 부리
    # 목 방패(뒤로 젖힌 반원 판) + 가장자리 돌기
    for i in range(-7, 7):
        for k in range(8, 19):
            d = ((i + .5) / 6.6) ** 2 + ((k + .5 - 11) / 7.0) ** 2
            if d <= 1 and k >= 8:
                j = -7 + (k - 8) // 3                                       # 위로 갈수록 뒤로 기움
                head[(i, j, k)] = 'cera_frill_dk' if d > 0.72 else ('cera_frill' if (i + k) % 5 else 'cera_frill_dk')
                head[(i, j + 1, k)] = 'cera'                                # 뒷면
                if 0.82 < d and (i + k) % 3 == 0:
                    head[(i, j - 1, k)] = 'cera_horn'                       # 가장자리 돌기
    # 눈썹 뿔 둘(앞·위로 길게) + 코뿔 하나
    for sx in (2, -3):
        for t in range(7):
            for w in ((0, 0), (1, 0), (0, 1)) if t < 3 else ((0, 0),):
                head[(sx + w[0], -11 - t, 12 + round(t * 0.55) + w[1])] = 'cera_horn'
    for t in range(3):
        for i in (-1, 0):
            head[(i, -14 - t, 10 + t)] = 'cera_horn'
    for i in (-3, 2):
        paint_front(head, i, 11, '!eye')
    box(lFL, 2.0, 5.2, -6, -3, 0, 7, lambda i, j, k: 'cera_dk' if (k < 1.5 or _h(i, j, k) % 4 == 0) else 'cera')
    box(lBL, 2.0, 5.6, 6, 9.5, 0, 8, lambda i, j, k: 'cera_dk' if (k < 1.5 or _h(i, j, k) % 4 == 0) else 'cera')
    for leg in (lFL, lBL):
        for i in (2, 3, 4):
            paint_front(leg, i, 0, '!claw')
    lFR, lBR = B.sym(lFL, 0, 0), B.sym(lBL, 0, 0)
    for leg in (lFL, lFR, lBL, lBR):
        for key in [key for key in leg if key in body]:
            del leg[key]
    ell(t1, (0, 12, 9.4), (2.8, 4.2, 2.6), skin)
    ell(t2, (0, 17.6, 8.6), (1.6, 3.6, 1.5), skin)
    dedupe(body, head, t1, lFL, lFR, lBL, lBR)
    for key in [key for key in t2 if key in t1]:
        del t2[key]
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 6, 9), 'Root', None),
        ('Head', (0, -7, 9.5), 'Body', None),
        ('LegFL', (3.6, -4.5, 7.5), 'Body', None), ('LegFR', (-3.6, -4.5, 7.5), 'Body', None),
        ('LegBL', (3.8, 7.5, 8), 'Root', None), ('LegBR', (-3.8, 7.5, 8), 'Root', None),
        ('Tail1', (0, 10, 9.5), 'Body', None), ('Tail2', (0, 15.5, 9), 'Tail1', None),
        # 버섯 소켓(부위 파괴 대상): 목 방패 좌우, 등 둘, 꼬리
        ('Socket_FrillL', (4.0, -4.6, 15.0), 'Head', (1, 0.3, 0.6)),
        ('Socket_FrillR', (-4.0, -4.6, 15.0), 'Head', (-1, 0.3, 0.6)),
        ('Socket_Back1', (0, -1, tops(body, 0, -1)), 'Body', None),
        ('Socket_Back2', (0, 6, tops(body, 0, 6)), 'Body', None),
        ('Socket_Tail', (0, 12, tops(t1, 0, 12)), 'Tail1', None),
        ('Hitbox_Head', (0, -11, 10), 'Head', None), ('Hitbox_Body', (0, 2, 10), 'Body', None),
    ]
    meshes = {'Bukera_Body': [(body, 'Body'), (head, 'Head'), (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR'),
                              (t1, 'Tail1'), (t2, 'Tail2')]}
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    fronts = lambda f, **kw: [K(f, 'LegFL', **kw), K(f, 'LegFR', **kw)]
    backs = lambda f, **kw: [K(f, 'LegBL', **kw), K(f, 'LegBR', **kw)]
    tail = lambda f, a, b: [K(f, 'Tail1', yaw=a), K(f, 'Tail2', yaw=b)]
    anims = {
        'Idle': (64, True, [K(32, 'Body', s=(1.02, 1, 1)), *tail(0, 6, 10), *tail(32, -6, -10), K(20, 'Head', pitch=8), K(48, 'Head', pitch=-3, yaw=6)]),
        'Walk': (36, True, [*gait(0, 18), K(0, 'Body', roll=2), K(9, 'Body', up=0.03), *gait(18, -18), K(18, 'Body', roll=-2),
                            K(27, 'Body', up=0.03), *tail(0, 8, 12), *tail(18, -8, -12), K(0, 'Head', pitch=3), K(18, 'Head', pitch=-3)]),
        'Run': (22, True, [*gait(0, 30), K(0, 'Body', pitch=3), K(5, 'Body', up=0.08), *gait(11, -30), K(11, 'Body', pitch=-3),
                           K(16, 'Body', up=0.08), *tail(0, 5, 8), *tail(11, -5, -8), K(0, 'Head', pitch=12)]),
        # 뿔로 들어 올리기. 판정 16
        'Attack': (32, False, [K(10, 'Head', pitch=28), K(10, 'Body', pitch=6), *fronts(10, pitch=-10),
                               K(16, 'Head', pitch=-35), K(16, 'Body', pitch=-8), *fronts(16, pitch=10),
                               K(22, 'Head', pitch=-30), K(32, 'Head', pitch=0), K(32, 'Body', pitch=0)]),
        # 돌진: 0~14 고개 숙여 발 구르기 → 14~34 돌진(판정 구간) → 44 정지(이동은 코드)
        'Charge': (48, False, [K(6, 'Head', pitch=30), K(6, 'LegFL', pitch=-30), K(10, 'LegFL', pitch=10), K(14, 'Head', pitch=34),
                               K(14, 'LegFL', pitch=0), *gait(18, 34), K(18, 'Root', fwd=0.8), *gait(24, -34), K(24, 'Root', fwd=2.0),
                               *gait(30, 34), K(30, 'Root', fwd=3.2), K(34, 'Root', fwd=3.8), K(34, 'Body', pitch=6), *gait(36, 0),
                               K(40, 'Root', fwd=4.0), K(40, 'Body', pitch=-4), K(48, 'Root', fwd=4.0), K(48, 'Head', pitch=0), K(48, 'Body', pitch=0)]),
        # 앞발 들었다 내려찍기. 충격파 판정 20
        'Stomp': (40, False, [K(12, 'Body', pitch=-22), *fronts(12, pitch=-40), K(12, 'Head', pitch=-12),
                              K(20, 'Body', pitch=4, s=(1.04, 0.96, 1)), *fronts(20, pitch=-4), K(20, 'Head', pitch=14),
                              K(26, 'Body', pitch=0), K(40, 'Head', pitch=0)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-6), K(3, 'Head', pitch=-16), K(3, 'Root', fwd=-0.08)]),
        'Death': (48, False, [K(10, 'Body', pitch=6), K(10, 'Head', pitch=20), K(26, 'Root', roll=90, up=0.55),
                              K(30, 'Root', roll=85, up=0.52), K(48, 'Root', roll=90, up=0.55), K(48, 'Head', pitch=15), *tail(48, -10, -5)]),
    }
    return 'Bukera', S, bones, meshes, anims

CHARS = [body_buraptor, body_bupachy, body_bukera]
PREVIEW_MUSH = {
    'Buraptor': {'Socket_Back': ('Mush_Paengi', 0.8), 'Socket_Head': ('Mush_Gwangdae', 0.4)},
    'Bupachy': {'Socket_Head': ('Mush_Pyogo', 0.7), 'Socket_Back': ('Mush_Malbul', 0.9)},
    'Bukera': {'Socket_FrillL': ('Mush_Gwangdae', 1.1), 'Socket_FrillR': ('Mush_Gwangdae', 1.1), 'Socket_Back1': ('Mush_Malbul', 1.6),
               'Socket_Back2': ('Mush_Noru', 1.5), 'Socket_Tail': ('Mush_Pyogo', 1.1)},
}

# =====================================================================
# 공룡 맵 소품 (복셀 0.25m)
# =====================================================================
def cycad():
    """소철 나무: 마디진 줄기 + 늘어진 잎(높이 약 5m)."""
    v = {}
    for k in range(15):
        ox = round(0.08 * k * k / 6)                                         # 살짝 휜 줄기
        L._fill(v, ((ox - 1.6, ox + 1.6), (-1.6, 1.6), (k, k + 1)), lambda x, y, z, ox=ox: (x - ox) ** 2 + y * y <= 1.6 ** 2,
                'cycad_band' if k % 2 == 0 else 'cycad_trunk')
    top = (round(0.08 * 14 * 14 / 6), 0, 15)
    for a in range(0, 360, 45):
        dx, dy = math.cos(math.radians(a + 20)), math.sin(math.radians(a + 20))
        for t in range(10):
            x, y, z = top[0] + dx * t, dy * t, top[2] + 2.2 * math.sin(t / 10 * math.pi) - t * 0.35
            for w in (-0.6, 0.6) if t > 1 else (0,):
                i, j = math.floor(x - dy * w), math.floor(y + dx * w)
                v[(i, j, math.floor(z))] = 'frond_dk' if (t + i) % 3 == 0 else ('frond_lt' if t == 9 else 'frond')
    return v

def fern(r=7, n=9, seed=5):
    v = {}
    rnd = random.Random(seed)
    for m in range(n):
        a = math.radians(m * 360 / n + rnd.uniform(-12, 12))
        dx, dy = math.cos(a), math.sin(a)
        rr = r * rnd.uniform(0.8, 1.1)
        for t in range(int(rr)):
            s = t / rr
            x, y, z = dx * t, dy * t, rr * 0.9 * math.sin(s * math.pi * 0.75)
            for w in ((-1, 0, 1) if 0.2 < s < 0.8 else (0,)):
                i, j = math.floor(x - dy * w), math.floor(y + dx * w)
                v[(i, j, math.floor(z))] = 'frond_lt' if w == 0 and t % 2 else ('frond_dk' if (t + w) % 3 == 0 else 'frond')
    return v

def giant_fern():
    """사람 키보다 큰 고사리(나무 대신 가장자리 숲 띠)."""
    v = fern(16, 11, 9)
    cyl(v, 0, 0, 1.0, 0, 4, 'cycad_trunk')
    return v

def fern_bush():
    return fern(7, 9, 5)

def basalt_rock():
    v = {}
    ell(v, (0, 0, 0.5), (3.6, 3.0, 3.2),
        lambda i, j, k: 'lava_glow' if (i + 2 * j) % 7 == 0 and k <= 2 and _h(i, j, k) % 2 else ('basalt_lt' if _h(i, j, k) % 4 == 0 else 'basalt_c'),
        cut=lambda x, y, z: z >= 0)
    return v

def basalt_spire():
    """현무암 기둥(보스 자리 둘레, 높이 약 4m). 틈에 용암빛."""
    v = {}
    for k in range(16):
        r = 2.4 - k * 0.07
        L._fill(v, ((-r, r), (-r, r), (k, k + 1)), lambda x, y, z, r=r: max(abs(x), abs(y), (abs(x) + abs(y)) * 0.75) <= r,
                lambda i, j, k: 'lava_glow' if (k % 5 == 2 and (i + j) % 3 == 0) else ('basalt_lt' if (i * 3 + j) % 5 == 0 else 'basalt_c'))
    return v

def bone_pile():
    """오래된 공룡 갈비뼈(땅에 반쯤 묻힘)."""
    v = {}
    for j in range(-6, 7, 3):
        for a in range(0, 181, 15):
            x, z = 4.2 * math.cos(math.radians(a)), 4.6 * math.sin(math.radians(a)) * (1 - abs(j) / 14)
            v[(math.floor(x), j, math.floor(z))] = 'bone' if a % 30 else 'bone_dk'
    for j in range(-8, 9):
        v[(0, j, 0)] = 'bone_dk'; v[(-1, j, 0)] = 'bone'                    # 등뼈
    return v

def dino_nest():
    """잔가지 둥지 + 점박이 알 셋."""
    v = {}
    ell(v, (0, 0, 0), (4.0, 4.0, 1.6), lambda i, j, k: 'twig' if (i + j + k) % 2 else 'cycad_band',
        cut=lambda x, y, z: z >= 0 and x * x + y * y > 6)
    for (x, y) in ((-1, -1), (1.2, 0), (-0.4, 1.4)):
        ell(v, (x, y, 1.6), (1.1, 1.1, 1.5), lambda i, j, k: 'egg_spot' if _h(i, j, k) % 5 == 0 else 'egg')
    return v

def deco_fern():
    v = {}
    for (dx, dy) in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1)):
        for t in range(1, 4):
            v[(dx * t, dy * t, min(t, 4 - t) + 1)] = 'frond' if t % 2 else 'frond_lt'
    v[(0, 0, 0)] = 'frond_dk'; v[(0, 0, 1)] = 'frond_dk'
    return v

def deco_ember():
    """식어 가는 화산 돌멩이."""
    v = {}
    for (i, j, k) in ((0, 0, 0), (1, 0, 0), (0, 1, 0), (1, 1, 0), (0, 0, 1), (1, 0, 1), (-1, 1, 0)):
        v[(i, j, k)] = 'lava_glow' if (i + j + k) % 3 == 0 else 'basalt_c'
    return v

M.PROPS.update({'cycad': cycad, 'giant_fern': giant_fern, 'fern_bush': fern_bush, 'basalt_rock': basalt_rock,
                'basalt_spire': basalt_spire, 'bone_pile': bone_pile, 'dino_nest': dino_nest})
B.DECOS.update({'Deco_Fern': deco_fern, 'Deco_Ember': deco_ember})
M.SIDE.update({'jungle': 'jungle_side', 'ash_path': 'basalt', 'basalt': 'basalt', 'lava': 'basalt'})
M.TOPT.update({'jungle': 'jungle_top', 'ash_path': 'ash_path', 'basalt': 'basalt', 'lava': 'lava_top'})

DINO_REGION = {
    'name': 'Map_Dino01', 'seed': 33, 'size': 100,
    'ground': 'jungle',
    'trees': {'cycad': 5, 'giant_fern': 4},
    'small': {'fern_bush': 4, 'basalt_rock': 3, 'bone_pile': 2, 'dino_nest': 1, 'mush_cluster': 1, 'Deco_Ember': 1},
    'grass_tries': 520,
    'path': 'ash_path', 'accent': 'basalt',
    'lake': 'lava', 'lakes': [(14, 47, 3.5), (86, 47, 3.5), (18, 84, 4.5), (84, 84, 4.5)],   # 용암 웅덩이(소품 없음, 위험 지대로 쓸 수 있음)
    'deco': {'Deco_Fern': 0.85, 'Deco_Ember': 0.15},
    'mob': ['Buraptor', 'Bupachy'], 'elite': ['Bursus', 'Bukera'], 'boss': 'Boss_Bursus',
    'elite_props': ['basalt_rock', 'fern_bush', 'bone_pile'], 'boss_prop': ['basalt_spire', 'basalt_rock'],
}

def build_dino_map(preview=False):
    field = M.REGION
    M.REGION = DINO_REGION
    try:
        L.reset()
        root, spawns, ground = M.build_map()
        L.write_palette(B.PAL_PATH)
        bpy.data.images['VoxelPalette.png'].reload()
        L.export_fbx(os.path.join(B.MOD, DINO_REGION['name'] + '.fbx'), False)
        print('spawns', sorted(spawns))
        if preview:
            pm = dict(B.PREVIEW_MUSH); pm.update(PREVIEW_MUSH); pm['Boss_Bursus'] = BB.PREVIEW_MUSH
            M.preview(root, spawns, ground,
                      actors={'Player': B.body_player, 'Buraptor': body_buraptor, 'Bupachy': body_bupachy, 'Bursus': B.body_bursus,
                              'Bukera': body_bukera, 'Boss_Bursus': BB.body_boss},
                      preview_mush=pm, boss_scale=1.0)
            for f in ('map', 'map_tps'):
                os.replace(os.path.join(B.PRE, f + '.png'), os.path.join(B.PRE, f.replace('map', 'map_dino') + '.png'))
    finally:
        M.REGION = field

def main():
    pv = B.PREVIEW
    B.PREVIEW = False
    MM.main()                     # 앞 단계 전부(팔레트 칸 고정, 새 타일·블록·장식 포함)
    B.PREVIEW = pv
    for fn in CHARS:
        L.reset()
        arm, objs, anims = B.build_character(fn)
        L.write_palette(B.PAL_PATH)
        L.export_fbx(os.path.join(B.MOD, arm.name + '.fbx'), True)
        mn, mx = L.world_bbox(objs)
        print(arm.name, len(objs[0].data.polygons), 'faces', 'size', [round(v, 2) for v in (mx - mn)], list(anims))
    only = os.environ.get('DINO_ONLY')
    build_dino_map(B.PREVIEW and only in (None, 'map'))
    if B.PREVIEW and only in (None, 'chars'):
        I.CHARS, I.PREVIEW_MUSH = CHARS + [MM.body_buguri], dict(PREVIEW_MUSH, **MM.PREVIEW_MUSH)
        I.preview()

if __name__ == '__main__':
    main()
