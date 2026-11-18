# MushTaker 복셀 에셋 생성 (Blender 5.x)
# 사용: blender -b -P build_voxel_assets.py -- <출력 폴더> [--preview]
#   또는 bpy 모듈: python build_voxel_assets.py -- <출력 폴더> [--preview]
# 결과: Models/*.fbx, Models/VoxelPalette.png, Models/TerrainAtlas.png, (옵션) Preview/*.png
import sys, os, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector, Matrix
import voxlib as L
from voxlib import box, ell, cyl, paint_front, mirror_x, _h

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['out']
OUT = os.path.abspath(args[0])
PREVIEW = '--preview' in args
MOD = os.path.join(OUT, 'Models'); os.makedirs(MOD, exist_ok=True)
PRE = os.path.join(OUT, 'Preview'); os.makedirs(PRE, exist_ok=True)
PAL_PATH = os.path.join(MOD, 'VoxelPalette.png')
ATLAS_PATH = os.path.join(MOD, 'TerrainAtlas.png')

def mat():
    if not os.path.exists(PAL_PATH):
        L.write_palette(PAL_PATH)
    return L.material('M_Voxel', PAL_PATH)

# =====================================================================
# 버섯 (갈아끼우는 부품 겸 드랍 아이템). 원점 = 밑동 중심, 위 = +Z
# =====================================================================
MUSH_SIZE = 0.05

def mush_songi():
    v = {}
    cyl(v, 0, 0, 1.9, 0, 8, 'songi_stem')
    ell(v, (0, 0, 1.2), (2.4, 2.4, 1.6), 'songi_stem', cut=lambda x, y, z: z >= 0)
    def cap(i, j, k):
        a = int((math.atan2(j + .5, i + .5) + math.pi) / (2 * math.pi) * 14)
        return ('songi_cap_dk', 'songi_cap', 'songi_cap', 'songi_cap_lt')[_h(a, 0, k // 2) % 4]
    ell(v, (0, 0, 7.4), (3.5, 3.5, 3.1), cap, cut=lambda x, y, z: z >= 6.2)
    return v

def mush_cloud():
    """구름버섯: 띠무늬 부채꼴 갓이 겹친 송이."""
    v = {}
    cyl(v, 0, 0, 1.2, 0, 6, 'cloud_1')
    bands = ['cloud_4', 'cloud_3', 'cloud_1', 'cloud_2', 'cloud_3', 'cloud_1', 'cloud_2']
    tiers = [(0.0, 1.8, 6.0, 0.4), (0.9, 4.0, 4.8, 2.3), (-0.6, 6.0, 3.4, 4.1)]
    for ox, z0, R, rot in tiers:
        def t(x, y, z, ox=ox, z0=z0, R=R, rot=rot):
            dx, dy = x - ox, y
            r = math.hypot(dx, dy); th = math.atan2(dy, dx)
            edge = R * (1 + 0.13 * math.sin(5 * th + rot))
            top = z0 + 1.3 - 0.35 * (r / R) * 1.5
            return r <= edge and z0 <= z <= top
        def c(i, j, k, ox=ox, z0=z0, R=R, rot=rot):
            x, y = i + .5 - ox, j + .5
            r = math.hypot(x, y); th = math.atan2(y, x)
            edge = R * (1 + 0.13 * math.sin(5 * th + rot))
            return bands[min(len(bands) - 1, int((edge - r) / 0.9))]
        L._fill(v, ((ox - R * 1.2, ox + R * 1.2), (-R * 1.2, R * 1.2), (z0, z0 + 2)), t, c)
    return v

def mush_malbul():
    v = {}
    cyl(v, 0, 0, 2.2, 0, 2.5, 'puff_base')
    def c(i, j, k):
        return 'puff_wart' if _h(i, j, k) % 5 == 0 else 'puff'
    ell(v, (0, 0, 4.6), (3.7, 3.7, 3.5), c)
    for (i, j, k) in list(v):
        if k <= 1:
            v[(i, j, k)] = 'puff_base'
    top = max(k for (i, j, k) in v if i in (-1, 0) and j in (-1, 0))
    for i in (-1, 0):
        for j in (-1, 0):
            v.pop((i, j, top), None); v[(i, j, top - 1)] = '!puff_pore'
    return v

def mush_noru():
    """노루궁뎅이: 하얀 털뭉치 공 + 아래로 늘어진 가시."""
    v = {}
    cyl(v, 0, 0, 1.4, 0, 2, 'noru_dk')
    def c(i, j, k):
        return 'noru_lt' if _h(i, j, 1) % 3 == 0 else ('noru_dk' if _h(i, j, k) % 7 == 0 else 'noru')
    ell(v, (0, 0, 5.6), (3.8, 3.8, 3.4), c)
    bottom = {}
    for (i, j, k) in v:
        if k >= 3:
            bottom[(i, j)] = min(k, bottom.get((i, j), 99))
    for (i, j), k in bottom.items():
        if (i + .5) ** 2 + (j + .5) ** 2 >= 4 and _h(i, j, 5) % 2 == 0:        # 가장자리 가시
            for d in range(1, 2 + _h(i, j, 6) % 3):
                v.setdefault((i, j, k - d), 'noru_lt' if _h(i, j, 1) % 3 == 0 else 'noru')
    return v

def mush_pyogo():
    """표고: 납작한 갈색 갓 + 흰 갈라짐 무늬."""
    v = {}
    cyl(v, 0, 0, 1.3, 0, 5, 'pyogo_stem')
    def cap(i, j, k):
        if k <= 4:
            return 'pyogo_stem'                                          # 갓 아래 주름
        if _h(i // 2, j // 2, 3) % 5 == 0 and _h(i, j, 4) % 2 == 0:
            return 'pyogo_crk'
        return 'pyogo_dk' if (i + .5) ** 2 + (j + .5) ** 2 > 12 else 'pyogo'
    ell(v, (0, 0, 5.0), (4.4, 4.4, 2.8), cap, cut=lambda x, y, z: z >= 4.2)
    return v

def mush_paengi():
    """팽이: 가늘고 긴 흰 줄기 다발 + 작은 갓."""
    v = {}
    cyl(v, 0, 0, 2.2, 0, 1.5, 'paengi_base')
    for n, (x, y, h) in enumerate([(0, 0, 10), (-2, 0, 9), (1, 1, 9), (1, -2, 8), (-1, 2, 8), (2, 0, 7), (-2, -2, 7), (-1, -1, 9)]):
        lean = (n % 3) - 1
        for k in range(1, h):
            v[(x + (lean if k > h * 0.6 else 0), y, k)] = 'paengi'
        cx = x + lean
        for (a, b) in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            v[(cx + a, y + b, h)] = 'paengi_cap'
        v[(cx, y, h + 1)] = 'paengi_cap'
    return v

def mush_gwangdae():
    """광대버섯: 빨간 갓 + 흰 점, 흰 줄기와 턱받이."""
    v = {}
    cyl(v, 0, 0, 1.3, 0, 7, 'gwang_stem')
    ell(v, (0, 0, 1.0), (2.0, 2.0, 1.4), 'gwang_stem', cut=lambda x, y, z: z >= 0)
    cyl(v, 0, 0, 2.0, 4.5, 5.2, 'gwang_dot')                           # 턱받이
    def cap(i, j, k):
        if k <= 6:
            return 'gwang_dot'
        if _h(i // 2, j // 2, k // 2) % 4 == 0 and _h(i, j, k) % 2 == 0:
            return '!gwang_dot'
        return 'gwang_dk' if k <= 7 else 'gwang'
    ell(v, (0, 0, 7.4), (4.2, 4.2, 3.4), cap, cut=lambda x, y, z: z >= 6.6)
    return v

def mush_seori():
    """서리버섯(얼음 지역용 가상 버섯): 옅은 푸른 고깔 갓 + 고드름."""
    v = {}
    cyl(v, 0, 0, 1.2, 0, 6, 'seori_stem')
    def cap(i, j, k):
        if k >= 10:
            return 'seori_lt'
        return 'seori_dk' if _h(i, j, k) % 6 == 0 else 'seori'
    L._fill(v, ((-4, 4), (-4, 4), (5, 12)),
            lambda x, y, z: x * x + y * y <= (3.8 * (1 - (z - 5.5) / 6.5)) ** 2 and z >= 5.5, cap)
    for (i, j) in [(-4, 0), (3, 0), (0, -4), (0, 3), (-3, -3), (2, 2), (2, -3), (-3, 2)]:
        if _h(i, j, 2) % 3:
            for k in range(3, 6):
                v.setdefault((i, j, k), '!seori_lt')
    return v

MUSHROOMS = {'Mush_Songi': mush_songi, 'Mush_Cloud': mush_cloud, 'Mush_Malbul': mush_malbul,
             'Mush_Noru': mush_noru, 'Mush_Pyogo': mush_pyogo, 'Mush_Paengi': mush_paengi,
             'Mush_Gwangdae': mush_gwangdae, 'Mush_Seori': mush_seori}

# =====================================================================
# 몸 3종 + 플레이어
# 뼈 이름 Socket_* = 버섯을 꽂는 자리. 뼈 방향(+Y) = 버섯이 자라는 방향.
# =====================================================================
def sym(parts_l, name_l, name_r):
    """왼쪽(+X) 부위 복셀을 오른쪽(-X)으로 뒤집어 만든다."""
    r = {(-1 - i, j, k): c for (i, j, k), c in parts_l.items()}
    return r

# ---------- 버끼: 토끼 ----------
def body_buggi():
    S = 0.07
    body, head, earL, legFL, legBL = {}, {}, {}, {}, {}
    ell(body, (0, 1, 4.5), (3.3, 4.3, 3.4), lambda i, j, k: 'rab_belly' if (j <= -2 and k <= 5) else 'rab')
    ell(body, (0, 5.4, 5.6), (1.7, 1.5, 1.7), 'rab_belly')
    ell(head, (0, -3, 8), (3.1, 2.9, 2.8), 'rab')
    ell(head, (0, -5.4, 7.0), (1.6, 1.1, 1.2), 'rab_belly')
    for i in (-1, 0):
        paint_front(head, i, 8, '!pink')
    for i in (-3, 2):
        paint_front(head, i, 8, '!eye')
        paint_front(head, i, 9, '!eye_hi' if i > 0 else '!eye')
    box(earL, 0.6, 2.4, -3.6, -2.2, 9.5, 15.5, 'rab')
    for k in range(11, 15):
        paint_front(earL, 1, k, '!pink')
    box(legFL, 0.8, 2.6, -2.4, -0.8, 0, 3.2, 'rab_belly')
    ell(legBL, (2.5, 2.2, 2.6), (1.4, 2.0, 2.0), 'rab')
    box(legBL, 1.6, 3.4, -0.8, 3.6, 0, 1, 'rab_dk')
    earR, legFR, legBR = sym(earL, 0, 0), sym(legFL, 0, 0), sym(legBL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 1, 2), 'Root', None),
        ('Head', (0, -2.5, 6.5), 'Body', None),
        ('EarL', (1.5, -2.9, 10), 'Head', None), ('EarR', (-1.5, -2.9, 10), 'Head', None),
        ('LegFL', (1.7, -1.6, 3), 'Body', None), ('LegFR', (-1.7, -1.6, 3), 'Body', None),
        ('LegBL', (2.5, 2.2, 3), 'Root', None), ('LegBR', (-2.5, 2.2, 3), 'Root', None),
        ('Socket_Head', (0, -3, 10.3), 'Head', None),
        ('Socket_Back', (0, 1.5, 7.5), 'Body', None),
    ]
    meshes = {'Buggi_Body': [(body, 'Body'), (head, 'Head'), (earL, 'EarL'), (earR, 'EarR'),
                             (legFL, 'LegFL'), (legFR, 'LegFR'), (legBL, 'LegBL'), (legBR, 'LegBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    ears = lambda f, **kw: [K(f, 'EarL', **kw), K(f, 'EarR', **kw)]
    legs = lambda f, pre, **kw: [K(f, pre + 'L', **kw), K(f, pre + 'R', **kw)]
    anims = {
        'Idle': (48, True, [K(24, 'Body', s=(1, 1.04, 1)), K(30, 'EarL', roll=-14), K(36, 'EarL')]),
        'Walk': (18, True, [K(4, 'Body', pitch=10), K(4, 'Root', up=-0.01),
                            K(9, 'Root', up=0.16), K(9, 'Body', pitch=-12), *legs(9, 'LegB', pitch=55),
                            *legs(9, 'LegF', pitch=-35), *ears(9, pitch=-25),
                            K(14, 'Root', up=0.02), K(14, 'Body', pitch=8), *legs(14, 'LegF', pitch=10)]),
        'Attack': (20, False, [K(6, 'Body', pitch=15), K(6, 'Root', up=-0.02), K(6, 'Head', pitch=10), *ears(6, pitch=-20),
                               K(10, 'Root', fwd=0.3, up=0.1), K(10, 'Body', pitch=-20), *legs(10, 'LegB', pitch=60), K(10, 'Head', pitch=-15),
                               K(13, 'Root', fwd=0.32), K(13, 'Body', pitch=25), K(13, 'Head', pitch=25), *ears(13, pitch=30)]),
        'Hit': (12, False, [K(2, 'Body', pitch=-20), K(2, 'Head', pitch=-15), K(2, 'Root', fwd=-0.08), *ears(2, pitch=-35)]),
        'Death': (30, False, [K(8, 'Root', up=0.1, roll=30), K(18, 'Root', up=0.22, roll=90), K(22, 'Root', up=0.2, roll=84),
                              K(30, 'Root', up=0.22, roll=90), *legs(30, 'LegB', pitch=30), *ears(30, pitch=-40)]),
    }
    return 'Buggi', S, bones, meshes, anims

# ---------- 버곰: 곰 ----------
def body_bugom():
    S = 0.1
    body, head, lFL, lBL = {}, {}, {}, {}
    ell(body, (0, 1, 9.5), (5.6, 9.6, 5.0), lambda i, j, k: 'bear_dk' if k < 6 else 'bear')
    ell(body, (0, -4, 12), (5.0, 4.6, 4.0), 'bear')
    ell(body, (0, 10.6, 10), (1.3, 1.1, 1.3), 'bear_dk')
    ell(head, (0, -10.5, 11), (4.1, 4.0, 3.7), 'bear')
    ell(head, (0, -14.0, 9.8), (2.3, 2.0, 1.8), 'bear_muz')
    for i in (-1, 0):
        paint_front(head, i, 10, '!nose')
    for i in (-3, 2):
        paint_front(head, i, 12, '!eye')
    for sx in (1, -1):
        ell(head, (sx * 3.1, -10, 14.4), (1.3, 1.0, 1.3), 'bear_dk')
    box(lFL, 2, 5.6, -8, -4.4, 0, 7.5, 'bear')
    box(lBL, 2, 5.6, 5, 8.6, 0, 7.5, 'bear')
    for leg in (lFL, lBL):
        for i in range(2, 6):
            paint_front(leg, i, 0, '!claw')
    lFR, lBR = sym(lFL, 0, 0), sym(lBL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 6.8, 7), 'Root', None),
        ('Head', (0, -8, 10), 'Body', None),
        ('LegFL', (3.8, -6.2, 7), 'Body', None), ('LegFR', (-3.8, -6.2, 7), 'Body', None),
        ('LegBL', (3.8, 6.8, 7), 'Root', None), ('LegBR', (-3.8, 6.8, 7), 'Root', None),
        ('Socket_Head', (0, -10.5, 14.2), 'Head', None),
        ('Socket_Back1', (0, -4, 15.6), 'Body', None),
        ('Socket_Back2', (0, 3, 14.0), 'Body', None),
        ('Socket_SideL', (5.2, 1, 10.5), 'Body', (1, 0, 0.5)),
        ('Socket_SideR', (-5.2, 1, 10.5), 'Body', (-1, 0, 0.5)),
    ]
    meshes = {'Bugom_Body': [(body, 'Body'), (head, 'Head'), (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    anims = {
        'Idle': (60, True, [K(30, 'Body', s=(1, 1.02, 1)), K(30, 'Head', pitch=6)]),
        'Walk': (32, True, [*gait(0, 20), K(0, 'Body', roll=3), K(8, 'Body', up=0.03), *gait(16, -20), K(16, 'Body', roll=-3),
                            K(24, 'Body', up=0.03), K(8, 'Head', yaw=4), K(24, 'Head', yaw=-4)]),
        'Attack': (32, False, [K(10, 'Body', pitch=-28), K(10, 'LegFL', pitch=-45), K(10, 'LegFR', pitch=-45), K(10, 'Head', pitch=-10),
                               K(15, 'Body', pitch=-32), K(15, 'LegFL', pitch=-50), K(15, 'LegFR', pitch=-50),
                               K(19, 'Body', pitch=5), K(19, 'Head', pitch=15), K(19, 'LegFL', pitch=-8), K(19, 'LegFR', pitch=-8), K(19, 'Root', fwd=0.15),
                               K(23, 'Body', pitch=5), K(23, 'Root', fwd=0.15)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-8), K(3, 'Head', pitch=-20), K(3, 'Root', fwd=-0.08)]),
        'Death': (44, False, [K(10, 'Body', pitch=8), K(10, 'Head', pitch=20), K(24, 'Root', roll=90, up=0.55),
                              K(28, 'Root', roll=84, up=0.5), K(44, 'Root', roll=90, up=0.55), K(44, 'Head', pitch=20),
                              K(44, 'LegFL', pitch=-20), K(44, 'LegBL', pitch=20)]),
    }
    return 'Bugom', S, bones, meshes, anims

# ---------- 버우르스: 공룡(보스) ----------
def body_bursus():
    S = 0.12
    body, head, t1, t2, lFL, lBL = {}, {}, {}, {}, {}, {}
    def skin(i, j, k):
        if k < 9:
            return 'dino_belly'
        if k > 15 and _h(i // 2, j // 2, 7) % 6 == 0:
            return 'dino_spot'
        return 'dino'
    ell(body, (0, 0, 13), (6.6, 12, 7.5), skin)
    ell(body, (0, -11, 13.5), (3.6, 4.2, 4.0), skin)
    ell(head, (0, -16.5, 13.5), (3.3, 4.6, 3.1), lambda i, j, k: 'dino_belly' if (k < 13 and j < -16) else 'dino')
    for i in (2, -3):
        head[(i, -18, 14)] = '!eye'
    for i in (-2, -1, 0, 1):
        paint_front(head, i, 12, '!tooth')
    ell(t1, (0, 15, 12.6), (4.2, 6.6, 4.2), skin)
    ell(t2, (0, 25, 11), (2.5, 6.2, 2.3), skin)
    # 등줄기 돌기
    for j in range(-12, 26, 3):
        for src in (body, t1, t2):
            ks = [k for (i, jj, k) in src if i == 0 and jj == j]
            if ks:
                src[(0, j, max(ks) + 1)] = 'dino_dk'
                break
    ell(lFL, (4.5, -6, 9), (2.2, 2.5, 3.0), 'dino')
    box(lFL, 3, 6.4, -7.6, -4.4, 0, 8, 'dino')
    ell(lBL, (5, 6, 10), (2.8, 3.6, 4.0), 'dino')
    box(lBL, 3, 7, 4, 8, 0, 9, 'dino')
    for leg in (lFL, lBL):
        for i in range(3, 7):
            paint_front(leg, i, 0, '!tooth')
    lFR, lBR = sym(lFL, 0, 0), sym(lBL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 6, 11), 'Root', None),
        ('Head', (0, -13, 13), 'Body', None),
        ('Tail1', (0, 10, 13), 'Body', None), ('Tail2', (0, 20, 12), 'Tail1', None),
        ('LegFL', (4.7, -6, 10), 'Body', None), ('LegFR', (-4.7, -6, 10), 'Body', None),
        ('LegBL', (5, 6, 11), 'Root', None), ('LegBR', (-5, 6, 11), 'Root', None),
        ('Socket_Head', (0, -16.5, 16.2), 'Head', None),
        ('Socket_Back1', (0, -7, 18.8), 'Body', None),
        ('Socket_Back2', (0, -1, 20.2), 'Body', None),
        ('Socket_Back3', (0, 5, 19.5), 'Body', None),
        ('Socket_Tail', (0, 16, 16.0), 'Tail1', None),
    ]
    meshes = {'Bursus_Body': [(body, 'Body'), (head, 'Head'), (t1, 'Tail1'), (t2, 'Tail2'),
                              (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    anims = {
        'Idle': (60, True, [K(30, 'Body', s=(1, 1.02, 1)), K(30, 'Tail1', yaw=6), K(30, 'Tail2', yaw=10), K(30, 'Head', pitch=4)]),
        'Walk': (40, True, [*gait(0, 18), K(0, 'Tail1', yaw=8), K(0, 'Tail2', yaw=-6), K(0, 'Body', roll=3),
                            K(10, 'Body', up=0.04), *gait(20, -18), K(20, 'Tail1', yaw=-8), K(20, 'Tail2', yaw=6),
                            K(20, 'Body', roll=-3), K(30, 'Body', up=0.04)]),
        # 꼬리 휘두르기. 판정 프레임 18 전후
        'Attack': (36, False, [K(10, 'Body', yaw=22), K(10, 'Tail1', yaw=-20), K(10, 'Tail2', yaw=-25),
                               K(18, 'Body', yaw=-28), K(18, 'Tail1', yaw=40), K(18, 'Tail2', yaw=45),
                               K(24, 'Body', yaw=-24), K(24, 'Tail1', yaw=35), K(24, 'Tail2', yaw=50)]),
        # 앞발 들었다 내려찍기. 포자 터뜨리는 프레임 20
        'Stomp': (44, False, [K(14, 'Body', pitch=-24), K(14, 'LegFL', pitch=-35), K(14, 'LegFR', pitch=-35), K(14, 'Head', pitch=-15),
                              K(20, 'Body', pitch=4, s=(1.04, 0.96, 1)), K(20, 'Head', pitch=15), K(20, 'LegFL', pitch=-5), K(20, 'LegFR', pitch=-5),
                              K(26, 'Body', pitch=2)]),
        'Hit': (14, False, [K(3, 'Body', pitch=-6), K(3, 'Head', pitch=-18), K(3, 'Tail1', yaw=8)]),
        'Death': (60, False, [K(12, 'Head', pitch=-25), K(12, 'Body', pitch=-6), K(30, 'Root', roll=90, up=0.8),
                              K(34, 'Root', roll=85, up=0.75), K(60, 'Root', roll=90, up=0.8), K(60, 'Tail1', yaw=-10), K(60, 'Head', pitch=20)]),
    }
    return 'Bursus', S, bones, meshes, anims

# ---------- 플레이어: 버섯 채집꾼 ----------
def body_player():
    S = 0.09
    legL, body, head, hat, armL, weapon = {}, {}, {}, {}, {}, {}
    box(legL, 0.3, 2.7, -1.2, 1.2, 0, 4.5, lambda i, j, k: 'boot' if k < 2 else 'pants')
    box(body, -3, 3, -1.6, 1.6, 4.5, 9.5, lambda i, j, k: 'belt' if k == 4 else 'shirt')
    box(body, -2.4, 2.4, 1.6, 4.2, 5, 9.6, lambda i, j, k: 'wicker' if (i + k) % 2 == 0 else 'wicker_dk')
    box(body, -1.6, 1.6, 2.2, 3.6, 8.5, 9.6, None)
    box(body, -0.8, 0.8, 2.2, 3.6, 8, 10.6, 'songi_stem')
    ell(body, (0, 2.9, 10.8), (1.8, 1.4, 1.0), 'songi_cap')
    box(body, -3.4, 3.4, -2, 2, 8.6, 10, 'scarf')
    box(body, 0.6, 2, 1.9, 2.6, 6, 8.6, 'scarf')
    box(head, -4, 4, -3.6, 3.6, 10, 17, lambda i, j, k: 'hair' if (k >= 15 or j >= 1 or (k == 14 and j <= -3 and i % 3 == 0)) else 'skin')
    for i in (-3, 2):
        for k in (12, 13):
            paint_front(head, i, k, '!eye')
        paint_front(head, i - 1 if i < 0 else i + 1, 11, '!pink')
    cyl(hat, 0, 0, 6.3, 17, 18, lambda i, j, k: 'straw_dk' if math.hypot(i + .5, j + .5) > 5.4 else 'straw')
    cyl(hat, 0, 0, 3.7, 17, 20.6, 'straw')
    cyl(hat, 0, 0, 3.8, 18, 19, 'band')
    box(armL, 3, 5, -1, 1, 5, 9.6, lambda i, j, k: 'skin' if k < 6 else 'shirt')
    armR = sym(armL, 0, 0)
    box(weapon, -4.6, -3.4, -7, 0.4, 5, 6, 'wood')
    box(weapon, -5.6, -2.4, -10, -6.6, 3.6, 7.4, 'wood_dk')
    box(weapon, -5.7, -2.3, -8.6, -8.0, 3.5, 7.5, 'cloth')
    legR = sym(legL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 0, 4.5), 'Root', None), ('Head', (0, 0, 10), 'Body', None),
        ('ArmL', (4, 0, 9), 'Body', None), ('ArmR', (-4, 0, 9), 'Body', None),
        ('LegL', (1.5, 0, 4.5), 'Root', None), ('LegR', (-1.5, 0, 4.5), 'Root', None),
        ('Weapon', (-4, 0, 5.5), 'ArmR', None),
        ('Socket_Pet', (0, 2.9, 9.6), 'Body', None),     # 등 바구니 위: 펫(버구리) 태우는 자리
    ]
    meshes = {'Player_Body': [(body, 'Body'), (head, 'Head'), (armL, 'ArmL'), (armR, 'ArmR'), (legL, 'LegL'), (legR, 'LegR')],
              'Player_Hat': [(hat, 'Head')], 'Player_Weapon': [(weapon, 'Weapon')]}
    K = lambda f, b, **kw: (f, b, kw)
    def stride(f, a, arm, **body):
        return [K(f, 'LegL', pitch=-a), K(f, 'LegR', pitch=a), K(f, 'ArmL', pitch=arm), K(f, 'ArmR', pitch=-arm), K(f, 'Body', **body)]
    anims = {
        'Idle': (48, True, [K(24, 'Body', s=(1, 1.03, 1)), K(24, 'ArmL', roll=-4), K(24, 'ArmR', roll=4), K(24, 'Head', pitch=3)]),
        'Walk': (24, True, [*stride(0, 25, 20), K(6, 'Body', up=0.03), *stride(12, -25, -20), K(18, 'Body', up=0.03)]),
        'Run': (16, True, [*stride(0, 45, 40, pitch=10), K(4, 'Body', up=0.05, pitch=10), *stride(8, -45, -40, pitch=10),
                           K(12, 'Body', up=0.05, pitch=10)]),
        # 내려치기. 판정 프레임 9
        'Attack': (18, False, [K(5, 'ArmR', pitch=-165), K(5, 'Body', pitch=-8, yaw=10),
                               K(9, 'ArmR', pitch=-45), K(9, 'Body', pitch=12, yaw=-10),
                               K(12, 'ArmR', pitch=-50), K(12, 'Body', pitch=10)]),
        # 아래 공격들: 무기 뼈 pitch로 팔 pitch를 상쇄해 칼날을 수평으로 눕힌다. yaw+ = 캐릭터 왼쪽
        # 위로베기. 판정 프레임 7
        'Attack_Up': (18, False, [K(4, 'ArmR', pitch=40), K(4, 'Body', pitch=10), K(4, 'Root', up=-0.05),
                                  K(7, 'ArmR', pitch=-150), K(7, 'Body', pitch=-12), K(7, 'Root', up=0.05),
                                  K(12, 'ArmR', pitch=-140), K(12, 'Body', pitch=-10)]),
        # 좌로베기(오른쪽 → 왼쪽 가로베기). 판정 프레임 8
        'Attack_SlashL': (20, False, [K(4, 'Body', yaw=-40), K(4, 'ArmR', pitch=-70, yaw=-50), K(4, 'Weapon', pitch=70),
                                      K(8, 'Body', yaw=35), K(8, 'ArmR', pitch=-75, yaw=70), K(8, 'Weapon', pitch=70),
                                      K(13, 'Body', yaw=30), K(13, 'ArmR', pitch=-60, yaw=60), K(13, 'Weapon', pitch=60)]),
        # 우로베기(왼쪽 → 오른쪽 역베기). 판정 프레임 8
        'Attack_SlashR': (20, False, [K(4, 'Body', yaw=40), K(4, 'ArmR', pitch=-70, yaw=80), K(4, 'Weapon', pitch=70),
                                      K(8, 'Body', yaw=-40), K(8, 'ArmR', pitch=-75, yaw=-60), K(8, 'Weapon', pitch=70),
                                      K(13, 'Body', yaw=-35), K(13, 'ArmR', pitch=-65, yaw=-50), K(13, 'Weapon', pitch=60)]),
        # 막타: 웅크렸다 뛰어올라 내려찍기. 판정 프레임 17
        'Attack_Finish': (30, False, [K(6, 'Root', up=-0.08), K(6, 'Body', pitch=-15), K(6, 'ArmR', pitch=-175),
                                      K(11, 'Root', up=0.35), K(11, 'Body', pitch=-18), K(11, 'ArmR', pitch=-180),
                                      K(17, 'Root', up=-0.05), K(17, 'Body', pitch=25), K(17, 'ArmR', pitch=-30), K(17, 'Head', pitch=10),
                                      K(24, 'Root', up=-0.04), K(24, 'Body', pitch=22), K(24, 'ArmR', pitch=-35)]),
        # 휠윈드: 팔을 옆으로 뻗고 두 바퀴. 판정 8~26
        'Attack_Whirlwind': (32, False, [K(4, 'Root', yaw=-30, up=-0.03),
                                         *[K(f, 'Root', yaw=y, up=-0.03) for f, y in ((8, 90), (12, 210), (16, 330), (20, 450), (24, 570), (28, 690))],
                                         K(32, 'Root', yaw=720),
                                         *[K(f, b, **kw) for f in (4, 28) for b, kw in
                                           (('ArmR', dict(roll=-90)), ('Weapon', dict(pitch=90)), ('ArmL', dict(roll=70)), ('Body', dict(pitch=8)))]]),
        # 달리다가 베기: 달리는 자세로 대각선 내려베기. 판정 프레임 7
        'Attack_RunSlash': (20, False, [K(2, 'LegL', pitch=-40), K(2, 'LegR', pitch=40), K(2, 'Body', pitch=20),
                                        K(2, 'ArmR', pitch=-160, yaw=-20), K(2, 'ArmL', pitch=40),
                                        K(7, 'LegL', pitch=30), K(7, 'LegR', pitch=-30), K(7, 'Body', pitch=30, yaw=20),
                                        K(7, 'ArmR', pitch=-20, yaw=40), K(7, 'ArmL', pitch=-30), K(7, 'Root', up=-0.05, fwd=0.1),
                                        K(12, 'Body', pitch=25, yaw=15), K(12, 'ArmR', pitch=-25, yaw=35), K(12, 'Root', up=-0.03, fwd=0.1)]),
        'Hit': (12, False, [K(3, 'Body', pitch=-15), K(3, 'Head', pitch=-12), K(3, 'Root', fwd=-0.08)]),
        'Death': (40, False, [K(10, 'Body', pitch=-10), K(10, 'Head', pitch=-10), K(22, 'Root', pitch=88, up=0.3),
                              K(26, 'Root', pitch=80, up=0.33), K(40, 'Root', pitch=88, up=0.3), K(40, 'ArmL', pitch=-20)]),
    }
    return 'Player', S, bones, meshes, anims

CHARACTERS = [body_buggi, body_bugom, body_bursus, body_player]

# 미리보기용 기본 버섯 배치: 소켓 -> (버섯, 배율)
PREVIEW_MUSH = {
    'Buggi': {'Socket_Head': ('Mush_Songi', 0.8)},
    'Bugom': {'Socket_Back1': ('Mush_Cloud', 1.6), 'Socket_Back2': ('Mush_Cloud', 1.3), 'Socket_SideL': ('Mush_Cloud', 1.1)},
    'Bursus': {'Socket_Back1': ('Mush_Malbul', 2.2), 'Socket_Back2': ('Mush_Malbul', 2.8), 'Socket_Back3': ('Mush_Malbul', 2.2),
               'Socket_Tail': ('Mush_Malbul', 1.6)},
}

# =====================================================================
# 지형: 블록(1m, 원점=중심) + 장식(복셀, 원점=밑면 중심)
# =====================================================================
TILE, CELL, AT_N = 10, 16, 4   # 10px 타일, 16px 칸(가장자리 3px 늘림), 4x4 칸 = 64px
TILES = ['grass_top', 'grass_side', 'dirt', 'stone', 'mycel_top', 'mycel_side', 'path',
         'snow_top', 'snow_side', 'snow_path', 'ice_top',
         'jungle_top', 'jungle_side', 'ash_path', 'basalt', 'lava_top']     # 뒤에만 추가(앞 타일 위치 유지)

def make_atlas():
    from PIL import Image
    rnd = random.Random(7)
    GB, GD, GL = (104, 164, 84), (82, 138, 70), (134, 188, 100)
    DB, DD, DL = (142, 98, 66), (118, 80, 54), (160, 116, 80)
    SB, SD, SL = (120, 126, 140), (92, 96, 110), (144, 150, 162)
    SOIL = [(96, 76, 84), (84, 66, 74), (108, 88, 94)]
    THREAD = (232, 226, 214)
    def dirt_px(x, y):
        # 가로 지층 무늬
        if y % 4 == 3 and (x + y // 4 * 3) % 7 not in (0, 1):
            return DD
        return DL if rnd.random() < 0.12 else DB
    def tile(name):
        px = [[None] * TILE for _ in range(TILE)]
        for y in range(TILE):
            for x in range(TILE):
                if name == 'grass_top':
                    c = GB
                    if (x + 2 * (y // 3)) % 5 == 0 and y % 3 == 1: c = GD      # 풀잎 자국
                    elif (x + 2 * (y // 3)) % 5 == 1 and y % 3 == 0: c = GL
                elif name in ('grass_side', 'dirt', 'mycel_side'):
                    c = dirt_px(x, y)
                elif name == 'path':
                    c = (176, 140, 100) if rnd.random() < 0.1 else ((150, 116, 82) if (x * 3 + y * 5) % 7 else (134, 102, 72))
                elif name == 'stone':
                    row = y // 3
                    edge = y % 3 == 2 or (x + row * 4) % 6 == 0
                    c = SD if edge else (SL if rnd.random() < 0.15 else SB)
                elif name == 'snow_top':
                    c = (250, 252, 255) if rnd.random() < 0.08 else ((222, 232, 244) if (x * 5 + y * 3) % 11 == 0 else (238, 244, 250))
                elif name == 'snow_side':
                    c = (110, 104, 112) if y % 4 == 3 and (x + y // 4 * 3) % 7 not in (0, 1) else ((150, 140, 140) if rnd.random() < 0.12 else (128, 118, 120))
                elif name == 'snow_path':
                    c = (198, 208, 222) if (x // 3 + y // 2) % 4 == 0 and y % 2 == 0 else ((216, 224, 236) if rnd.random() < 0.2 else (206, 216, 230))
                elif name == 'ice_top':
                    c = (236, 248, 255) if (x + y) % 7 == 0 or (x - y) % 9 == 0 else ((150, 200, 230) if rnd.random() < 0.15 else (172, 216, 240))
                elif name == 'jungle_top':                                  # 짙은 밀림 이끼(고사리 잎 자국)
                    c = (60, 112, 62)
                    if (x - y) % 6 == 0 and x % 2 == 0: c = (44, 88, 50)
                    elif (x + y) % 6 == 3 and y % 2 == 1: c = (84, 140, 70)
                    elif rnd.random() < 0.06: c = (96, 150, 74)
                elif name == 'jungle_side':
                    c = (84, 62, 50) if y % 4 == 3 and (x + y // 4 * 3) % 7 not in (0, 1) else ((112, 84, 64) if rnd.random() < 0.12 else (98, 72, 56))
                elif name == 'ash_path':                                    # 화산재 자갈길
                    c = (70, 66, 70) if (x * 3 + y * 5) % 7 == 0 else ((112, 106, 108) if rnd.random() < 0.14 else (90, 86, 90))
                elif name == 'basalt':                                      # 현무암(틈에 용암빛)
                    row = y // 3
                    edge = y % 3 == 2 or (x + row * 4) % 5 == 0
                    c = ((236, 120, 40) if (x + y) % 9 == 0 else (40, 36, 42)) if edge else ((72, 66, 74) if rnd.random() < 0.15 else (56, 52, 60))
                elif name == 'lava_top':                                    # 용암(굳은 껍질 조각)
                    c = (52, 40, 40) if ((x // 3) * 7 + (y // 3) * 5) % 6 == 0 and (x + y) % 3 else (
                        (255, 214, 90) if rnd.random() < 0.12 else ((244, 140, 40) if (x + 2 * y) % 5 else (220, 84, 30)))
                else:
                    c = rnd.choice(SOIL)
                px[y][x] = c
        if name == 'grass_side':
            for x in range(TILE):
                depth = 3 + round(1.2 * math.sin(x * 1.3))
                for y in range(depth):
                    px[y][x] = GL if y == 0 else (GD if y == depth - 1 else GB)
        if name == 'jungle_side':
            for x in range(TILE):
                depth = 3 + round(1.2 * math.sin(x * 1.5 + 2))
                for y in range(depth):
                    px[y][x] = (84, 140, 70) if y == 0 else ((44, 88, 50) if y == depth - 1 else (60, 112, 62))
        if name == 'snow_side':
            for x in range(TILE):
                depth = 3 + round(1.2 * math.sin(x * 1.1 + 1))
                for y in range(depth):
                    px[y][x] = (250, 252, 255) if y == 0 else ((214, 226, 240) if y == depth - 1 else (238, 244, 250))
        if name in ('mycel_top', 'mycel_side'):
            if name == 'mycel_side':
                for x in range(TILE):
                    for y in range(2 + x % 2):
                        px[y][x] = rnd.choice(SOIL)
            for _ in range(3 if name == 'mycel_top' else 2):
                x, y = rnd.randrange(TILE), 0 if name == 'mycel_side' else rnd.randrange(TILE)
                for _ in range(9):
                    if 0 <= x < TILE and 0 <= y < TILE:
                        px[y][x] = THREAD
                    if name == 'mycel_side':
                        y += 1; x += rnd.choice((-1, 0, 1))
                    else:
                        x += rnd.choice((-1, 0, 1)); y += rnd.choice((-1, 0, 1))
        return px
    img = Image.new('RGBA', (CELL * AT_N, CELL * AT_N), (0, 0, 0, 0))
    pad = (CELL - TILE) // 2
    for n, name in enumerate(TILES):
        px = tile(name)
        cx, cy = (n % AT_N) * CELL, (n // AT_N) * CELL
        for y in range(CELL):
            for x in range(CELL):
                sx = min(TILE - 1, max(0, x - pad)); sy = min(TILE - 1, max(0, y - pad))
                img.putpixel((cx + x, cy + y), px[sy][sx] + (255,))
    img.save(ATLAS_PATH)

def tile_rect(name):
    n = TILES.index(name); pad = (CELL - TILE) // 2; W = CELL * AT_N
    x0 = (n % AT_N) * CELL + pad; y0 = (n // AT_N) * CELL + pad
    return x0 / W, (x0 + TILE) / W, 1 - (y0 + TILE) / W, 1 - y0 / W   # u0,u1,v0(아래),v1(위)

BLOCKS = {
    'Block_Grass': ('grass_top', 'grass_side', 'dirt'),
    'Block_Dirt': ('dirt', 'dirt', 'dirt'),
    'Block_Stone': ('stone', 'stone', 'stone'),
    'Block_Mycel': ('mycel_top', 'mycel_side', 'dirt'),
    'Block_Snow': ('snow_top', 'snow_side', 'dirt'),
    'Block_Ice': ('ice_top', 'ice_top', 'ice_top'),
    'Block_Jungle': ('jungle_top', 'jungle_side', 'dirt'),
    'Block_Basalt': ('basalt', 'basalt', 'basalt'),
    'Block_Lava': ('lava_top', 'basalt', 'basalt'),
}

def block_mesh(name, tiles, m):
    import bmesh
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    verts = {}
    def V(p):
        if p not in verts:
            verts[p] = bm.verts.new((p[0] - .5, p[1] - .5, p[2] - .5))
        return verts[p]
    for d, quad in L._FACES:
        f = bm.faces.new([V(q) for q in quad])
        t = tiles[0] if d[2] > 0 else tiles[2] if d[2] < 0 else tiles[1]
        u0, u1, v0, v1 = tile_rect(t)
        for l, q in zip(f.loops, quad):
            if d[2] != 0:
                a, b = q[0], q[1]
            else:
                a, b = (q[1] if d[0] != 0 else q[0]), q[2]
                if d[0] > 0 or d[1] < 0:
                    a = a if d[1] < 0 else 1 - a
            l[uvl].uv = (u0 + a * (u1 - u0), v0 + b * (v1 - v0))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(m)
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    return ob

DECO_SIZE = 0.1
def deco_grass():
    rnd = random.Random(3); v = {}
    for i in range(-2, 3):
        for j in range(-2, 3):
            if rnd.random() < 0.55:
                h = rnd.randint(1, 4)
                for k in range(h):
                    v[(i, j, k)] = rnd.choice(['grass1', 'grass2', 'grass3'])
    return v
def deco_flower():
    v = {}
    for k in range(4): v[(0, 0, k)] = 'grass3'
    v[(1, 0, 1)] = v[(-1, 0, 2)] = 'grass2'
    for di in (-1, 0, 1):
        for dj in (-1, 0, 1):
            v[(di, dj, 4)] = '!petal_y' if (di, dj) == (0, 0) else '!petal_p'
    return v
def deco_rock():
    v = {}
    ell(v, (0, 0, 0.6), (3.0, 2.4, 2.2), lambda i, j, k: 'stone_dk' if _h(i, j, k) % 3 == 0 else 'stone', cut=lambda x, y, z: z >= 0)
    return v
def deco_stump():
    v = {}
    cyl(v, 0, 0, 3.2, 0, 4, lambda i, j, k: 'bark_dk' if (i + j) % 3 == 0 else 'bark')
    cyl(v, 0, 0, 2.6, 3, 4, lambda i, j, k: 'ring' if int(math.hypot(i + .5, j + .5)) % 2 == 0 else 'bark')
    for (x0, x1, y0, y1) in [(3, 4.2, -0.5, 0.5), (-4.2, -3, 0.5, 1.5), (-0.5, 0.5, 3, 4.2), (0.5, 1.5, -4.2, -3)]:
        box(v, x0, x1, y0, y1, 0, 1, 'bark_dk')
    return v
DECOS = {'Deco_Grass': deco_grass, 'Deco_Flower': deco_flower, 'Deco_Rock': deco_rock, 'Deco_Stump': deco_stump}

# =====================================================================
# 빌드
# =====================================================================
HOLD_CLIPS = {'Death', 'Burrow'}   # 끝 자세를 유지하는 클립

def build_character(fn, with_anims=True):
    name, S, bones, meshes, anims = fn()
    # 다리가 몸통과 같은 칸을 차지하면 면이 겹쳐 검게 깨진다 → 겹친 칸을 다리에서 뺀다
    for parts in meshes.values():
        body = parts[0][0]
        for vox, bone in parts[1:]:
            if bone.startswith(('Leg', 'Paw', 'Foot')):
                for key in [key for key in vox if key in body]:
                    del vox[key]
    arm, objs = L.rig_character(name, S, bones, meshes, mat())
    if with_anims:
        for an, (length, loop, keys) in anims.items():
            L.clear_pose(arm)
            L.make_action(arm, an, length, keys, loop, hold=(an in HOLD_CLIPS))
        arm.animation_data.action = None
        L.clear_pose(arm)
    return arm, objs, anims

def attach_mush(arm, socket, mname, scale):
    ob = L.static_mesh(f'{arm.name}_{socket}_{mname}', MUSHROOMS[mname](), MUSH_SIZE, mat())
    ob.parent = arm; ob.parent_type = 'BONE'; ob.parent_bone = socket
    bpy.context.view_layer.update()
    pb = arm.pose.bones[socket]
    ob.matrix_world = arm.matrix_world @ pb.matrix @ Matrix.Rotation(math.radians(-90), 4, 'X') @ Matrix.Scale(scale, 4)
    return ob

def main():
    report = []
    # 1) 버섯 (팔레트 색을 먼저 채우기 위해 한 번 다 만든다)
    for mname, fn in MUSHROOMS.items():
        L.reset(); ob = L.static_mesh(mname, fn(), MUSH_SIZE, mat())
        L.export_fbx(os.path.join(MOD, mname + '.fbx'), False)
        report.append(f'{mname}: {len(ob.data.polygons)} faces')
    for dname, fn in DECOS.items():
        L.reset(); ob = L.static_mesh(dname, fn(), DECO_SIZE, mat())
        L.export_fbx(os.path.join(MOD, dname + '.fbx'), False)
        report.append(f'{dname}: {len(ob.data.polygons)} faces')
    make_atlas()
    for bname, tiles in BLOCKS.items():
        L.reset(); atl = L.material('M_Terrain', ATLAS_PATH)
        block_mesh(bname, tiles, atl)
        L.export_fbx(os.path.join(MOD, bname + '.fbx'), False)
    # 2) 캐릭터
    for fn in CHARACTERS:
        L.reset()
        arm, objs, anims = build_character(fn)
        L.export_fbx(os.path.join(MOD, arm.name + '.fbx'), True)
        report.append(f'{arm.name}: ' + ', '.join(f'{o.name} {len(o.data.polygons)}f' for o in objs) + ' | ' + ', '.join(anims))
    L.write_palette(PAL_PATH)
    print('\n'.join(report))
    if PREVIEW:
        previews()

# =====================================================================
# 미리보기 렌더
# =====================================================================
def previews():
    from PIL import Image, ImageDraw
    L.write_palette(PAL_PATH)
    # 애니 프레임 시트
    for fn in CHARACTERS:
        L.reset(); L.setup_render(220, 12)
        arm, objs, anims = build_character(fn)
        extra = [attach_mush(arm, s, m, sc) for s, (m, sc) in PREVIEW_MUSH.get(arm.name, {}).items()]
        bpy.data.images['VoxelPalette.png'].reload()
        mn, mx = L.world_bbox(objs + extra)
        ext = max((mx - mn).length, 1.0) * 1.15
        L.aim_camera((mn + mx) / 2 + Vector((0, 0, 0)), ext)
        rows = []
        for an, (length, loop, keys) in anims.items():
            arm.animation_data.action = bpy.data.actions[an]
            row = []
            for t in [0, .25, .5, .75] if loop else [0, .3, .55, 1.0]:
                f = round(length * t)
                bpy.context.scene.frame_set(f)
                p = os.path.join(PRE, f'_tmp.png'); L.render(p)
                row.append((Image.open(p).copy(), f'{an} {f}'))
            rows.append(row)
        W = 220
        sheet = Image.new('RGBA', (W * 4, W * len(rows)), (232, 236, 242, 255))
        d = ImageDraw.Draw(sheet)
        for r, row in enumerate(rows):
            for c, (im, label) in enumerate(row):
                sheet.alpha_composite(im, (c * W, r * W)); d.text((c * W + 6, r * W + 4), label, fill=(40, 40, 50, 255))
        sheet.save(os.path.join(PRE, f'anim_{arm.name}.png'))
    os.remove(os.path.join(PRE, '_tmp.png'))
    # 버섯·장식·블록 모음
    L.reset(); L.setup_render(900, 16)
    dm = mat(); atl = L.material('M_Terrain', ATLAS_PATH)
    x = 0.0
    for m, fn in MUSHROOMS.items():
        o = L.static_mesh(m, fn(), MUSH_SIZE * 2, dm); o.location = (x, 0, 0); x += 1.3
    for d, fn in DECOS.items():
        o = L.static_mesh(d, fn(), DECO_SIZE, dm); o.location = (x, 0, 0); x += 1.0
    x = 0.5
    for b, t in BLOCKS.items():
        o = block_mesh(b, t, atl); o.location = (x, 2.2, 0.5); x += 1.6
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((3.4, 1.0, 0.3)), 7.0, view=(-0.6, -1.4, 0.9))
    L.render(os.path.join(PRE, 'items.png'))
    # 전체 장면
    L.reset(); L.setup_render(1100, 24)
    atl = L.material('M_Terrain', ATLAS_PATH)
    protos = {b: block_mesh(b, t, atl) for b, t in BLOCKS.items()}
    rnd = random.Random(11)
    for x in range(-9, 10):
        for y in range(-7, 8):
            h = 0 if abs(x) < 7 or y > -3 else 1
            if x <= -7 and y <= -4: h = 2
            for z in range(-2, h):
                kind = 'Block_Grass' if z == h - 1 else ('Block_Dirt' if z >= -1 else 'Block_Stone')
                if z == h - 1 and (x, y) in [(5, 4), (6, 4), (6, 5), (5, 5), (7, 5)]:
                    kind = 'Block_Mycel'
                o = bpy.data.objects.new(f'b{x}_{y}_{z}', protos[kind].data)
                o.location = (x + .5, y + .5, z - .5)
                bpy.context.scene.collection.objects.link(o)
    for p in protos.values():
        p.hide_render = True
    dm = mat()
    deco_protos = {d: L.static_mesh(d, fn(), DECO_SIZE, dm) for d, fn in DECOS.items()}
    for d in deco_protos.values():
        d.hide_render = True
    for _ in range(40):
        d = rnd.choice(['Deco_Grass', 'Deco_Grass', 'Deco_Flower', 'Deco_Rock', 'Deco_Stump'])
        x, y = rnd.uniform(-9, 9), rnd.uniform(-6.5, 7)
        if abs(x) < 6 and -4 < y < 3:
            continue
        o = bpy.data.objects.new('d', deco_protos[d].data); o.location = (x, y, 0)
        o.rotation_euler = (0, 0, rnd.choice([0, 1.57, 3.14, 4.71])); bpy.context.scene.collection.objects.link(o)
    places = {'Player': ((0.5, 1.5, 0), 200), 'Buggi': ((-3, -1.5, 0), 20), 'Bugom': ((3.6, -1.0, 0), -25), 'Bursus': ((-0.5, -5.0, 0), 5)}
    for fn in CHARACTERS:
        arm, objs, _ = build_character(fn, with_anims=False)
        (loc, rot) = places[arm.name]
        arm.location = loc; arm.rotation_euler = (0, 0, math.radians(rot))
        bpy.context.view_layer.update()
        for s, (m, sc) in PREVIEW_MUSH.get(arm.name, {}).items():
            attach_mush(arm, s, m, sc)
    for (x, y, m) in [(-1.6, 0.4, 'Mush_Songi'), (-1.2, 0.0, 'Mush_Cloud'), (-0.8, 0.5, 'Mush_Malbul')]:
        o = L.static_mesh('drop', MUSHROOMS[m](), MUSH_SIZE * 0.7, dm); o.location = (x, y, 0)
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((0, -1.8, 0.6)), 11.5, view=(-0.9, -1.4, 1.1))
    L.render(os.path.join(PRE, 'scene.png'))

if __name__ == "__main__":
    main()
