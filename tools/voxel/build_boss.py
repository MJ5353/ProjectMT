# 대형 버우르스(부위 파괴 보스) 생성 (Blender 5.x)
# 사용: python build_boss.py -- <출력 폴더> [--preview]
# 팔레트 색 위치를 맞추려고 캐릭터·맵을 먼저 다시 만든 뒤 Bursus_Boss.fbx를 더한다.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector
import voxlib as L
import build_voxel_assets as B
import build_map as M
from voxlib import box, ell, paint_front, _h

S = 0.17   # 복셀 17cm. 키 약 5.8m(플레이어의 약 3.1배), 길이 약 16m

def body_boss():
    body, neck, head, jaw, t1, t2, t3, lFL, lBL = ({} for _ in range(9))
    def skin(i, j, k):
        if k < 15:
            return 'dino_belly'
        if k > 26 and _h(i // 3, j // 3, 7) % 5 == 0:
            return 'dino_spot'
        return 'dino_dk' if _h(i, j, k) % 7 == 0 else 'dino'
    ell(body, (0, 0, 21), (10.5, 19, 12), skin)
    ell(neck, (0, -18, 22.5), (5.6, 7.5, 6.2), skin)
    ell(head, (0, -27, 23), (5.2, 7.2, 4.4), lambda i, j, k: 'dino_dk' if k >= 26 and abs(i + .5) > 2.5 else 'dino')
    for i in (3, -4):                       # 눈 + 눈두덩
        head[(i, -29, 23)] = '!eye'; head[(i, -29, 24)] = '!eye'
        head[(i, -29, 25)] = 'dino_dk'
    for i in range(-3, 3):                  # 윗니
        paint_front(head, i, 20, '!tooth')
    ell(jaw, (0, -27, 18.6), (4.3, 6.6, 2.0), lambda i, j, k: 'dino_belly' if k < 18 else 'dino')
    for i in range(-3, 3):
        paint_front(jaw, i, 19, '!tooth')
    ell(t1, (0, 24, 20), (6.6, 10, 6.6), skin)
    ell(t2, (0, 39, 16.5), (4.2, 9, 4.0), skin)
    ell(t3, (0, 52, 13.5), (2.6, 7.5, 2.4), skin)
    ell(t3, (0, 58, 13.5), (3.6, 3.0, 3.2), 'dino_dk')          # 꼬리 곤봉
    # 등줄기 돌기
    for j in range(-30, 60, 3):
        for src in (head, neck, body, t1, t2, t3):
            ks = [k for (i, jj, k) in src if i == 0 and jj == j]
            if ks:
                src[(0, j, max(ks) + 1)] = 'dino_dk'
                break
    ell(lFL, (7.5, -10, 13), (3.6, 4.2, 5.2), 'dino')
    box(lFL, 5, 10, -12.5, -7.5, 0, 12, 'dino')
    ell(lBL, (8, 10, 15), (4.6, 5.8, 6.8), 'dino')
    box(lBL, 5, 11, 7, 13, 0, 13, 'dino')
    for leg in (lFL, lBL):
        for i in range(5, 11):
            paint_front(leg, i, 0, '!tooth'); paint_front(leg, i, 1, '!tooth')
    lFR, lBR = B.sym(lFL, 0, 0), B.sym(lBL, 0, 0)
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 10, 18), 'Root', None),
        ('Neck', (0, -13, 22), 'Body', None), ('Head', (0, -22, 22), 'Neck', None), ('Jaw', (0, -22, 19), 'Head', None),
        ('Tail1', (0, 15, 21), 'Body', None), ('Tail2', (0, 32, 18), 'Tail1', None), ('Tail3', (0, 46, 14.5), 'Tail2', None),
        ('LegFL', (7.5, -10, 16), 'Body', None), ('LegFR', (-7.5, -10, 16), 'Body', None),
        ('LegBL', (8, 10, 18), 'Root', None), ('LegBR', (-8, 10, 18), 'Root', None),
        # 파괴 부위 소켓 8곳
        ('Socket_Head', (0, -26, 26.6), 'Head', None),
        ('Socket_Back1', (0, -10, 30.8), 'Body', None),
        ('Socket_Back2', (0, -2, 32.4), 'Body', None),
        ('Socket_Back3', (0, 7, 31.7), 'Body', None),
        ('Socket_ForelegL', (10.6, -10, 13.5), 'LegFL', (1, -0.3, 0.6)),
        ('Socket_ForelegR', (-10.6, -10, 13.5), 'LegFR', (-1, -0.3, 0.6)),
        ('Socket_Tail1', (0, 24, 26.0), 'Tail1', None),
        ('Socket_TailTip', (0, 58, 16.3), 'Tail3', None),
        # 몸통 히트박스 기준점(버섯 없는 부위). 반지름은 README 참고
        ('Hitbox_Head', (0, -27, 22), 'Head', (0, -1, 0)), ('Hitbox_Neck', (0, -18, 22.5), 'Neck', (0, -1, 0)),
        ('Hitbox_Body', (0, 0, 21), 'Body', (0, 1, 0)), ('Hitbox_Tail', (0, 39, 16.5), 'Tail2', (0, 14, -3.5)),
        ('Hitbox_LegFL', (7.5, -10, 8), 'LegFL', None), ('Hitbox_LegFR', (-7.5, -10, 8), 'LegFR', None),
        ('Hitbox_LegBL', (8, 10, 9), 'LegBL', None), ('Hitbox_LegBR', (-8, 10, 9), 'LegBR', None),
    ]
    meshes = {'Bursus_Boss_Body': [(body, 'Body'), (neck, 'Neck'), (head, 'Head'), (jaw, 'Jaw'),
                                   (t1, 'Tail1'), (t2, 'Tail2'), (t3, 'Tail3'),
                                   (lFL, 'LegFL'), (lFR, 'LegFR'), (lBL, 'LegBL'), (lBR, 'LegBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def gait(f, a):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegBR', pitch=-a), K(f, 'LegFR', pitch=a), K(f, 'LegBL', pitch=a)]
    def gallop(f, a, **body):
        return [K(f, 'LegFL', pitch=-a), K(f, 'LegFR', pitch=-a), K(f, 'LegBL', pitch=a), K(f, 'LegBR', pitch=a), K(f, 'Body', **body)]
    def tail(f, a, b, c):
        return [K(f, 'Tail1', yaw=a), K(f, 'Tail2', yaw=b), K(f, 'Tail3', yaw=c)]
    anims = {
        'Idle': (80, True, [K(40, 'Body', s=(1, 1.015, 1)), K(40, 'Neck', pitch=3), *tail(40, 5, 8, 10), K(40, 'Jaw', pitch=4)]),
        'Walk': (60, True, [*gait(0, 15), K(0, 'Body', roll=2.5), *tail(0, 6, -4, -6), K(0, 'Neck', yaw=3),
                            K(15, 'Body', up=0.08), *gait(30, -15), K(30, 'Body', roll=-2.5), *tail(30, -6, 4, 6),
                            K(30, 'Neck', yaw=-3), K(45, 'Body', up=0.08)]),
        # 돌진 중 반복용 질주
        'Run': (24, True, [*gallop(0, 28, pitch=4), K(0, 'Neck', pitch=15), *gallop(6, 0, up=0.15), K(6, 'Neck', pitch=12),
                           *gallop(12, -28, pitch=-4), K(12, 'Neck', pitch=15), *gallop(18, 0, up=0.15), K(18, 'Neck', pitch=12)]),
        # 포효. 판정(경직·넉백) 프레임 20~56
        'Roar': (70, False, [K(12, 'Body', pitch=-8), K(12, 'Neck', pitch=-25), K(12, 'Head', pitch=-10),
                             K(20, 'Body', pitch=-4), K(20, 'Neck', pitch=-5), K(20, 'Head', pitch=10), K(20, 'Jaw', pitch=35),
                             K(30, 'Head', pitch=10, yaw=4), K(38, 'Head', pitch=10, yaw=-4), K(46, 'Head', pitch=10, yaw=4),
                             K(54, 'Head', pitch=10, yaw=-2), K(56, 'Jaw', pitch=35), K(56, 'Neck', pitch=-5), K(56, 'Body', pitch=-4),
                             K(62, 'Jaw', pitch=0)]),
        # 앞발 내려찍기. 판정 프레임 22 (충격파·포자)
        'Stomp': (54, False, [K(16, 'Body', pitch=-22), K(16, 'LegFL', pitch=-40), K(16, 'LegFR', pitch=-40), K(16, 'Neck', pitch=-15),
                              K(22, 'Body', pitch=3, s=(1.03, 0.97, 1)), K(22, 'LegFL', pitch=-5), K(22, 'LegFR', pitch=-5),
                              K(22, 'Neck', pitch=15), K(22, 'Head', pitch=5), K(30, 'Body', pitch=1)]),
        # 꼬리 휘두르기. 판정 프레임 22~26
        'TailSwipe': (50, False, [K(14, 'Body', yaw=25), *tail(14, -20, -25, -30), K(14, 'Neck', yaw=-10),
                                  K(24, 'Body', yaw=-35), *tail(24, 45, 45, 40), K(24, 'Neck', yaw=12),
                                  K(30, 'Body', yaw=-30), *tail(30, 40, 50, 50)]),
        # 돌진: 0~20 준비, 20~55 질주(이동은 코드로), 55~70 제동
        'Charge': (70, False, [K(14, 'Body', pitch=6, up=-0.12), K(14, 'Neck', pitch=20), K(14, 'LegBL', pitch=20), K(14, 'LegBR', pitch=20),
                               *gallop(20, 28, pitch=4), *gallop(26, -28, pitch=-4, up=0.15), *gallop(32, 28, pitch=4),
                               *gallop(38, -28, pitch=-4, up=0.15), *gallop(44, 28, pitch=4), *gallop(50, -28, pitch=-4, up=0.15),
                               K(20, 'Neck', pitch=22), K(50, 'Neck', pitch=22),
                               K(58, 'Body', pitch=-10), K(58, 'LegFL', pitch=-30), K(58, 'LegFR', pitch=-30), K(58, 'Neck', pitch=0),
                               K(58, 'LegBL', pitch=0), K(58, 'LegBR', pitch=0)]),
        # 물기: 0~12 고개 젖히며 입 벌림(예비), 18 돌진, 21 판정
        'Bite': (48, False, [K(12, 'Neck', pitch=-20), K(12, 'Head', pitch=-15), K(12, 'Jaw', pitch=30), K(12, 'Body', pitch=-6),
                             K(18, 'Root', fwd=0.9), K(18, 'Body', pitch=8), K(18, 'Neck', pitch=25), K(18, 'Head', pitch=5), K(18, 'Jaw', pitch=35),
                             K(21, 'Root', fwd=0.9), K(21, 'Jaw', pitch=0), K(21, 'Neck', pitch=25), K(21, 'Body', pitch=8),
                             K(30, 'Root', fwd=0.8), K(30, 'Neck', pitch=15), K(30, 'Body', pitch=4)]),
        # 다운: 10~24 무너짐, 24~84 엎드림(약점 노출), 84~110 일어남
        'Down': (120, False, [K(10, 'Body', roll=8, pitch=-6), K(10, 'Neck', pitch=-20), K(10, 'Jaw', pitch=25),
                              K(24, 'Body', up=-1.3, roll=3), K(24, 'LegFL', pitch=-70), K(24, 'LegFR', pitch=-70),
                              K(24, 'LegBL', pitch=70), K(24, 'LegBR', pitch=70), K(24, 'Neck', pitch=30), K(24, 'Head', pitch=10), K(24, 'Jaw', pitch=10),
                              *tail(24, 10, 5, 0),
                              K(54, 'Body', up=-1.27, roll=3, s=(1, 1.02, 1)), K(54, 'Neck', pitch=28),
                              K(84, 'Body', up=-1.3, roll=3), K(84, 'LegFL', pitch=-70), K(84, 'LegFR', pitch=-70),
                              K(84, 'LegBL', pitch=70), K(84, 'LegBR', pitch=70), K(84, 'Neck', pitch=30), K(84, 'Head', pitch=10),
                              K(96, 'Body', up=-0.45, pitch=-6), K(96, 'LegFL', pitch=-20), K(96, 'LegFR', pitch=-20),
                              K(96, 'LegBL', pitch=25), K(96, 'LegBR', pitch=25), K(96, 'Neck', pitch=-10),
                              K(110, 'Body', up=0)]),
        # 부위 파괴 경직: 휘청 → 주저앉아 머리 늘어뜨림 → 회복
        'PartBreak': (60, False, [K(3, 'Body', pitch=-10, roll=10), K(3, 'Neck', pitch=-30, yaw=15), K(3, 'Jaw', pitch=25), *tail(3, 20, 15, 10),
                               K(10, 'Body', roll=-6), K(10, 'Neck', pitch=-15),
                               K(18, 'Body', up=-0.28), K(18, 'LegFL', pitch=15), K(18, 'LegFR', pitch=15), K(18, 'Neck', pitch=20), K(18, 'Head', pitch=10),
                               K(28, 'Neck', pitch=20, yaw=6), K(38, 'Neck', pitch=20, yaw=-6),
                               K(44, 'Body', up=-0.28), K(44, 'LegFL', pitch=15), K(44, 'LegFR', pitch=15), K(44, 'Head', pitch=10),
                               K(52, 'Body', up=0.0), K(52, 'Neck', pitch=0)]),
        'Hit': (18, False, [K(4, 'Body', pitch=-4), K(4, 'Neck', pitch=-12), K(4, 'Jaw', pitch=15)]),
        'Death': (90, False, [K(15, 'Neck', pitch=-30), K(15, 'Jaw', pitch=30), K(40, 'Root', roll=90, up=1.79),
                              K(46, 'Root', roll=84, up=1.7), K(90, 'Root', roll=90, up=1.79), K(90, 'Neck', pitch=20),
                              K(90, 'Jaw', pitch=20), *tail(90, -10, -5, 0)]),
    }
    return 'Bursus_Boss', S, bones, meshes, anims

PREVIEW_MUSH = {
    'Socket_Head': ('Mush_Songi', 2.2), 'Socket_Back1': ('Mush_Malbul', 4.0), 'Socket_Back2': ('Mush_Malbul', 5.0),
    'Socket_Back3': ('Mush_Malbul', 4.0), 'Socket_ForelegL': ('Mush_Cloud', 2.6), 'Socket_ForelegR': ('Mush_Cloud', 2.6),
    'Socket_Tail1': ('Mush_Malbul', 3.2), 'Socket_TailTip': ('Mush_Songi', 2.4),
}

def broken(fn, cut):
    if cut is None:                       # 따로 만든 깨짐 모양
        return fn()
    """버섯 갓이 꺾여 나간 모습: 밑동만 남기고 윗면을 들쭉날쭉하게."""
    v = fn()
    out = {}
    for (i, j, k), c in v.items():
        lim = cut + (_h(i, j, 3) % 3) - 1
        if k <= lim:
            out[(i, j, k)] = c
    # 밑동과 이어지지 않은 조각(고드름·턱받이 등)은 버린다
    keep, stack = set(), [p for p in out if p[2] == 0]
    while stack:
        p = stack.pop()
        if p in keep:
            continue
        keep.add(p)
        for d in ((1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)):
            q = (p[0] + d[0], p[1] + d[1], p[2] + d[2])
            if q in out and q not in keep:
                stack.append(q)
    out = {p: out[p] for p in keep}
    tops = {}
    for (i, j, k) in out:
        tops[(i, j)] = max(k, tops.get((i, j), -1))
    for (i, j), k in tops.items():
        out[(i, j, k)] = 'songi_cap_dk' if _h(i, j, 9) % 2 else 'puff_pore'
    return out

def broken_pyogo():
    """표고 깨짐: 갓은 남기고 윗면에 X자로 파인 홈(바닥에 흰 속살)."""
    v = {p: ('pyogo' if c == 'pyogo_crk' else c) for p, c in B.mush_pyogo().items()}   # 원래 잔무늬는 지워 X자가 보이게
    tops = {}
    for (i, j, k) in v:
        tops[(i, j)] = max(k, tops.get((i, j), -1))
    for (i, j), k in tops.items():
        x, y = i + .5, j + .5
        if k >= 5 and (i - j in (0, 1) or i + j in (-1, 0)):            # 계단식으로 이어진 X자
            v.pop((i, j, k), None); v.pop((i, j, k - 1), None)            # 두 칸 파냄
            if (i, j, k - 2) in v:
                v[(i, j, k - 2)] = '!pyogo_crk'                         # 홈 바닥 흰 속살
    return v

BROKEN = {'Mush_Songi_Broken': (B.mush_songi, 3), 'Mush_Cloud_Broken': (B.mush_cloud, 2), 'Mush_Malbul_Broken': (B.mush_malbul, 3),
          'Mush_Noru_Broken': (B.mush_noru, 3), 'Mush_Pyogo_Broken': (broken_pyogo, None), 'Mush_Paengi_Broken': (B.mush_paengi, 3),
          'Mush_Gwangdae_Broken': (B.mush_gwangdae, 3), 'Mush_Seori_Broken': (B.mush_seori, 3)}

def main():
    pv = B.PREVIEW
    B.PREVIEW = False
    M.main()                      # 캐릭터·버섯·블록·맵 (팔레트 순서 유지)
    B.PREVIEW = pv
    for n, (fn, cut) in BROKEN.items():
        L.reset(); L.static_mesh(n, broken(fn, cut), B.MUSH_SIZE, B.mat())
        L.export_fbx(os.path.join(B.MOD, n + '.fbx'), False)
    L.reset()
    arm, objs, anims = B.build_character(body_boss)
    L.write_palette(B.PAL_PATH)
    L.export_fbx(os.path.join(B.MOD, 'Boss_Bursus.fbx'), True)   # Unity에서 대장이 Boss_Bursus로 이름 바꿈
    mn, mx = L.world_bbox(objs)
    print('Bursus_Boss', len(objs[0].data.polygons), 'faces', 'size', [round(v, 2) for v in (mx - mn)], list(anims))
    if B.PREVIEW:
        preview()

def preview():
    from PIL import Image, ImageDraw
    # 애니 시트
    L.reset(); L.setup_render(260, 10)
    arm, objs, anims = B.build_character(body_boss)
    extra = [B.attach_mush(arm, s, m, sc) for s, (m, sc) in PREVIEW_MUSH.items()]
    bpy.data.images['VoxelPalette.png'].reload()
    mn, mx = L.world_bbox(objs + extra)
    L.aim_camera((mn + mx) / 2, (mx - mn).length * 1.05)
    rows = []
    for an, (length, loop, keys) in anims.items():
        arm.animation_data.action = bpy.data.actions[an]
        row = []
        ts = {'Charge': [0, 14, 26, 58], 'Roar': [0, 12, 20, 38], 'Stomp': [0, 16, 22, 54], 'TailSwipe': [0, 14, 24, 50],
              'PartBreak': [0, 3, 18, 60], 'Bite': [0, 12, 21, 48], 'Down': [0, 24, 96, 120], 'Death': [0, 15, 40, 90]}.get(an)
        frames = ts or ([round(length * t) for t in (0, .25, .5, .75)] if loop else [round(length * t) for t in (0, .3, .55, 1)])
        for f in frames:
            bpy.context.scene.frame_set(f)
            p = os.path.join(B.PRE, '_tmp.png'); L.render(p)
            row.append((Image.open(p).copy(), f'{an} {f}'))
        rows.append(row)
    Wd = 260
    sheet = Image.new('RGBA', (Wd * 4, Wd * len(rows)), (232, 236, 242, 255))
    d = ImageDraw.Draw(sheet)
    for r, row in enumerate(rows):
        for c, (im, label) in enumerate(row):
            sheet.alpha_composite(im, (c * Wd, r * Wd)); d.text((c * Wd + 6, r * Wd + 4), label, fill=(40, 40, 50, 255))
    sheet.save(os.path.join(B.PRE, 'anim_Bursus_Boss.png'))
    os.remove(os.path.join(B.PRE, '_tmp.png'))
    # 크기 비교 + 소켓 표시
    L.reset(); L.setup_render(1200, 20)
    arm, objs, _ = B.build_character(body_boss, with_anims=False)
    for s, (m, sc) in PREVIEW_MUSH.items():
        if s in ('Socket_Back1', 'Socket_ForelegL'):
            B.MUSHROOMS[m + '_Broken'] = lambda m=m: broken(*BROKEN[m + '_Broken'])
            m = m + '_Broken'
        B.attach_mush(arm, s, m, sc)
    parm, _, _ = B.build_character(B.body_player, with_anims=False)
    parm.location = (5.0, -7.5, 0); parm.rotation_euler = (0, 0, math.radians(150))
    for x in range(-8, 9):
        for y in range(-10, 12):
            o = L.static_mesh(f'g{x}_{y}', {(0, 0, -1): 'grass1'}, 1.0, B.mat()); o.location = (x, y, 0)
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((0, 0, 2.8)), 17, view=(-1.0, -1.1, 0.6))
    L.render(os.path.join(B.PRE, 'boss.png'))

if __name__ == '__main__':
    main()
