# 나무 보스(기생 버섯이 잔뜩 자란 걷는 나무) 생성 (Blender 5.x)
# 사용: python build_boss_tree.py -- <출력 폴더> [--preview]
# 팔레트 색 위치를 맞추려고 앞 단계(캐릭터·맵·버우르스 보스)를 먼저 다시 만든 뒤 Boss_Tree.fbx를 더한다.
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from mathutils import Vector
import voxlib as L
import build_voxel_assets as B
import build_boss as BB
from voxlib import box, ell, cyl, _h

L.COLORS.update({
    'tbark': (96, 72, 54), 'tbark_dk': (66, 48, 36), 'tbark_lt': (124, 96, 70), 'moss': (104, 130, 70),
    'hollow': (30, 22, 18), 'glow': (250, 214, 96), 'tleaf': (98, 120, 64), 'tleaf_dk': (74, 94, 50),
})

S = 0.17   # 복셀 17cm. 키 약 6m

def capsule(v, p0, p1, r0, r1, col):
    """p0→p1 선분을 따라 반지름이 r0→r1로 변하는 굵은 가지."""
    a, b = Vector(p0), Vector(p1)
    d = b - a; ln2 = d.length_squared
    lo = [min(a[n], b[n]) - max(r0, r1) for n in range(3)]
    hi = [max(a[n], b[n]) + max(r0, r1) for n in range(3)]
    def t(x, y, z):
        p = Vector((x, y, z))
        u = max(0.0, min(1.0, (p - a).dot(d) / ln2))
        return (p - (a + d * u)).length <= r0 + (r1 - r0) * u
    L._fill(v, tuple(zip(lo, hi)), t, col)

def bark(i, j, k):
    g = (i * 3 + j * 5 + (k // 4)) % 7
    if _h(i, j, k) % 11 == 0:
        return 'moss'
    return 'tbark_dk' if g == 0 else ('tbark_lt' if g == 3 else 'tbark')

def body_tree():
    trunk, crown, armL, foreL, rFL, rBL = ({} for _ in range(6))
    # 줄기 몸통: 아래는 굵고 위로 갈수록 앞으로 굽음
    cyl(trunk, 0, 0, 6.2, 7, 18, bark)
    ell(trunk, (0, 0, 9), (7.2, 6.6, 3.5), bark)
    ell(trunk, (0, -0.8, 23), (6.6, 5.8, 7.5), bark)
    ell(trunk, (0, -1.5, 29), (4.8, 4.4, 3.6), bark)
    # 얼굴: 파인 눈 구멍(빛나는 심지)과 입
    for (cx, ck) in [(-2.5, 23), (2.5, 23)]:
        for i in range(int(cx) - 1, int(cx) + 1):
            for k in (ck, ck + 1):
                js = [j for (a, j, b) in trunk if a == i and b == k]
                if js:
                    j0 = min(js)
                    trunk.pop((i, j0, k), None)
                    trunk[(i, j0 + 1, k)] = '!glow' if k == ck else '!hollow'
    for i in range(-3, 3):
        for k in (17, 18):
            js = [j for (a, j, b) in trunk if a == i and b == k]
            if js:
                j0 = min(js)
                trunk.pop((i, j0, k), None); trunk[(i, j0 + 1, k)] = '!hollow'
    # 머리 위 가지와 잎 뭉치(병들어 듬성듬성)
    capsule(crown, (0, -1, 30), (-5, 1, 38), 1.8, 0.8, bark)
    capsule(crown, (0, -1, 30), (6, -1, 37), 1.6, 0.7, bark)
    capsule(crown, (0, -1, 30), (1, 5, 39), 1.5, 0.6, bark)
    for c, r in [((-5, 1, 39), 2.6), ((6, -1, 38), 2.3), ((1, 5, 40), 2.2)]:
        ell(crown, c, (r, r, r * 0.8), lambda i, j, k: 'tleaf_dk' if (i + k) % 3 == 0 else 'tleaf')
    # 왼팔(+X): 어깨 → 팔꿈치, 팔뚝 → 손가락 가지
    capsule(armL, (6, -0.5, 25), (10, -2, 15), 2.4, 1.9, bark)
    capsule(foreL, (10, -2, 15), (10.5, -5, 5), 1.9, 1.5, bark)
    for (dx, dy) in [(-1.5, -1), (1.5, -1), (0, -2.5)]:
        capsule(foreL, (10.5, -5, 5.5), (10.5 + dx * 1.6, -5 + dy * 1.4, 1.5), 0.8, 0.5, 'tbark_dk')
    # 뿌리 다리: 줄기 밑동에서 바깥 아래로
    capsule(rFL, (4, -3, 9), (9, -9, 0.8), 2.4, 1.3, bark)
    capsule(rFL, (9, -9, 0.8), (11, -11, 0.5), 1.3, 0.7, 'tbark_dk')
    capsule(rBL, (4, 3, 9), (9.5, 8.5, 0.8), 2.4, 1.3, bark)
    capsule(rBL, (9.5, 8.5, 0.8), (11.5, 10.5, 0.5), 1.3, 0.7, 'tbark_dk')
    armR, foreR, rFR, rBR = (B.sym(x, 0, 0) for x in (armL, foreL, rFL, rBL))
    bones = [
        ('Root', (0, 0, 0), None, None), ('Body', (0, 0, 8), 'Root', None), ('Crown', (0, -1, 30), 'Body', None),
        ('ArmL', (6, -0.5, 25), 'Body', None), ('ForearmL', (10, -2, 15), 'ArmL', None),
        ('ArmR', (-6, -0.5, 25), 'Body', None), ('ForearmR', (-10, -2, 15), 'ArmR', None),
        ('RootFL', (4, -3, 9), 'Root', None), ('RootFR', (-4, -3, 9), 'Root', None),
        ('RootBL', (4, 3, 9), 'Root', None), ('RootBR', (-4, 3, 9), 'Root', None),
        # 기생 버섯 소켓 9곳 (파괴 부위)
        ('Socket_Crown', (1, 5, 41.5), 'Crown', None),
        ('Socket_ShoulderL', (6.5, 0, 28.5), 'Body', (0.4, 0, 1)),
        ('Socket_ShoulderR', (-6.5, 0, 28.5), 'Body', (-0.4, 0, 1)),
        ('Socket_TrunkL', (6.4, 0.5, 18), 'Body', (1, 0, 0.3)),
        ('Socket_TrunkR', (-6.4, 0.5, 18), 'Body', (-1, 0, 0.3)),
        ('Socket_Back', (0, 5.6, 22), 'Body', (0, 1, 0.4)),
        ('Socket_ForearmL', (11.6, -3.5, 10), 'ForearmL', (1, 0, 0.4)),
        ('Socket_ForearmR', (-11.6, -3.5, 10), 'ForearmR', (-1, 0, 0.4)),
        ('Socket_RootBL', (9, 8, 2.5), 'RootBL', None),
        # 몸통 히트박스 기준점
        ('Hitbox_Trunk', (0, 0, 18), 'Body', None), ('Hitbox_Face', (0, -5, 21), 'Body', None),
        ('Hitbox_ArmL', (10, -3, 12), 'ForearmL', None), ('Hitbox_ArmR', (-10, -3, 12), 'ForearmR', None),
        ('Hitbox_RootFL', (7, -6.5, 4), 'RootFL', None), ('Hitbox_RootFR', (-7, -6.5, 4), 'RootFR', None),
        ('Hitbox_RootBL', (7, 6.5, 4), 'RootBL', None), ('Hitbox_RootBR', (-7, 6.5, 4), 'RootBR', None),
    ]
    meshes = {'Boss_Tree_Body': [(trunk, 'Body'), (crown, 'Crown'), (armL, 'ArmL'), (foreL, 'ForearmL'),
                                 (armR, 'ArmR'), (foreR, 'ForearmR'), (rFL, 'RootFL'), (rFR, 'RootFR'),
                                 (rBL, 'RootBL'), (rBR, 'RootBR')]}
    K = lambda f, b, **kw: (f, b, kw)
    def arms(f, **kw):   # 양팔 대칭(roll은 반대 부호)
        r = dict(kw); r['roll'] = -kw.get('roll', 0); r['yaw'] = -kw.get('yaw', 0)
        return [K(f, 'ArmL', **kw), K(f, 'ArmR', **r)]
    def fores(f, **kw):
        r = dict(kw); r['roll'] = -kw.get('roll', 0)
        return [K(f, 'ForearmL', **kw), K(f, 'ForearmR', **r)]
    def step(f, a, lift):
        return [K(f, 'RootFL', pitch=-a, up=lift), K(f, 'RootBR', pitch=-a, up=lift), K(f, 'RootFR', pitch=a), K(f, 'RootBL', pitch=a)]
    def step2(f, a, lift):
        return [K(f, 'RootFR', pitch=-a, up=lift), K(f, 'RootBL', pitch=-a, up=lift), K(f, 'RootFL', pitch=a), K(f, 'RootBR', pitch=a)]
    anims = {
        'Idle': (90, True, [K(45, 'Body', pitch=2, roll=1.5), K(45, 'Crown', roll=-3), *arms(45, roll=3), *fores(45, pitch=-4)]),
        'Walk': (64, True, [*step(0, 12, 0.0), *step(8, 6, 0.35), *step2(32, 12, 0.0), *step2(40, 6, 0.35),
                            K(0, 'Body', roll=3, yaw=4), K(16, 'Body', up=0.12), K(32, 'Body', roll=-3, yaw=-4), K(48, 'Body', up=0.12),
                            K(0, 'ArmL', pitch=12), K(0, 'ArmR', pitch=-12), K(32, 'ArmL', pitch=-12), K(32, 'ArmR', pitch=12),
                            K(16, 'Crown', roll=4), K(48, 'Crown', roll=-4)]),
        # 포효: 0~20 몸을 젖히며 팔 벌림 → 20~60 떨림(넉백·경직)
        'Roar': (75, False, [K(18, 'Body', pitch=-14), *arms(18, roll=55, pitch=-20), *fores(18, pitch=-25), K(18, 'Crown', pitch=-10),
                             K(26, 'Body', pitch=-12, roll=2), K(34, 'Body', pitch=-12, roll=-2), K(42, 'Body', pitch=-12, roll=2),
                             K(50, 'Body', pitch=-12, roll=-2), K(58, 'Body', pitch=-12), *arms(58, roll=55, pitch=-20), *fores(58, pitch=-25),
                             K(30, 'Crown', roll=6, pitch=-10), K(40, 'Crown', roll=-6, pitch=-10), K(50, 'Crown', roll=6, pitch=-10)]),
        # 두 팔 내려찍기: 0~18 머리 위로 들어올림(예비) → 24 지면 강타
        'Slam': (60, False, [K(18, 'Body', pitch=-12), *arms(18, pitch=-165), *fores(18, pitch=-30),
                             K(24, 'Body', pitch=22, up=-0.2), *arms(24, pitch=-55), *fores(24, pitch=-5),
                             K(32, 'Body', pitch=20, up=-0.2), *arms(32, pitch=-55), *fores(32, pitch=-5)]),
        # 한 팔 휩쓸기(오른팔): 0~16 몸을 비틀며 팔 뒤로 → 22~28 앞을 크게 휩씀
        'Sweep': (54, False, [K(16, 'Body', yaw=-40), K(16, 'ArmR', roll=-75, yaw=40), K(16, 'ForearmR', pitch=-20),
                              K(25, 'Body', yaw=45), K(25, 'ArmR', roll=-80, yaw=-50), K(25, 'ForearmR', pitch=-10),
                              K(32, 'Body', yaw=40), K(32, 'ArmR', roll=-60, yaw=-45)]),
        # 뿌리 공격: 0~20 웅크리며 뿌리를 땅에 박음 → 24 앞쪽에서 뿌리가 솟음(이펙트·판정은 코드)
        'RootStrike': (60, False, [K(20, 'Body', up=-0.5, pitch=10), K(20, 'RootFL', pitch=-25), K(20, 'RootFR', pitch=-25),
                                   *arms(20, pitch=-40, roll=20), *fores(20, pitch=-30),
                                   K(24, 'Body', up=-0.6, pitch=14), K(24, 'RootFL', pitch=10, up=-0.2), K(24, 'RootFR', pitch=10, up=-0.2),
                                   *arms(24, pitch=10, roll=10),
                                   K(40, 'Body', up=-0.6, pitch=14), K(40, 'RootFL', pitch=10, up=-0.2), K(40, 'RootFR', pitch=10, up=-0.2)]),
        # 포자 터뜨리기: 0~24 몸을 웅크리고 부르르 떪(예비) → 30 버섯마다 포자 폭발
        'SporeBurst': (66, False, [K(12, 'Body', up=-0.3, s=(1.02, 0.95, 1.02)), *arms(12, roll=-8, pitch=10),
                                   K(16, 'Body', up=-0.3, roll=2), K(20, 'Body', up=-0.3, roll=-2), K(24, 'Body', up=-0.32, roll=2),
                                   K(30, 'Body', up=0.1, s=(1.06, 1.06, 1.06)), *arms(30, roll=40, pitch=-20), K(30, 'Crown', pitch=-12),
                                   K(40, 'Body', up=0.0, s=(1, 1, 1)), *arms(40, roll=30, pitch=-10)]),
        # 부위 파괴 경직: 휘청이며 한쪽으로 기울고 팔을 늘어뜨림
        'PartBreak': (60, False, [K(4, 'Body', roll=12, pitch=-8), *arms(4, roll=35), K(4, 'Crown', roll=-15),
                                  K(14, 'Body', roll=-6, pitch=10, up=-0.3), *arms(14, pitch=20), *fores(14, pitch=20),
                                  K(40, 'Body', roll=-5, pitch=12, up=-0.35), *arms(40, pitch=22), *fores(40, pitch=22),
                                  K(52, 'Body', pitch=0, up=0)]),
        # 다운: 10~26 앞으로 무릎 꿇듯 주저앉아 팔로 땅을 짚음, 26~84 약점(얼굴·등) 노출, 84~110 일어남
        'Down': (120, False, [K(10, 'Body', pitch=-8, roll=6), *arms(10, roll=30),
                              K(26, 'Body', pitch=32, up=-1.0), *arms(26, pitch=-50), *fores(26, pitch=30),
                              K(26, 'RootFL', pitch=-30), K(26, 'RootFR', pitch=-30), K(26, 'RootBL', pitch=25), K(26, 'RootBR', pitch=25),
                              K(26, 'Crown', pitch=20),
                              K(55, 'Body', pitch=30, up=-0.95), K(84, 'Body', pitch=32, up=-1.0), *arms(84, pitch=-50), *fores(84, pitch=30),
                              K(84, 'RootFL', pitch=-30), K(84, 'RootFR', pitch=-30), K(84, 'RootBL', pitch=25), K(84, 'RootBR', pitch=25),
                              K(84, 'Crown', pitch=20),
                              K(98, 'Body', pitch=10, up=-0.4), *arms(98, pitch=-20), K(110, 'Body', pitch=0, up=0)]),
        'Hit': (18, False, [K(4, 'Body', pitch=-6, roll=3), K(4, 'Crown', pitch=-12), *arms(4, roll=10)]),
        # 쓰러짐: 나무가 넘어지듯 앞으로 천천히 → 쿵
        'Death': (96, False, [K(20, 'Body', pitch=-10), *arms(20, roll=40, pitch=-30), K(20, 'Crown', pitch=-20),
                              K(50, 'Root', pitch=55), K(62, 'Root', pitch=86, up=0.9), K(68, 'Root', pitch=80, up=1.0),
                              K(96, 'Root', pitch=86, up=0.9), *arms(96, roll=70, pitch=-80), K(96, 'Crown', pitch=10),
                              K(96, 'RootFL', pitch=-20), K(96, 'RootBR', pitch=20)]),
    }
    return 'Boss_Tree', S, bones, meshes, anims

PREVIEW_MUSH = {
    'Socket_Crown': ('Mush_Songi', 2.2), 'Socket_ShoulderL': ('Mush_Songi', 2.4), 'Socket_ShoulderR': ('Mush_Malbul', 2.4),
    'Socket_TrunkL': ('Mush_Cloud', 3.4), 'Socket_TrunkR': ('Mush_Cloud', 3.0), 'Socket_Back': ('Mush_Cloud', 3.4),
    'Socket_ForearmL': ('Mush_Malbul', 1.8), 'Socket_ForearmR': ('Mush_Cloud', 2.2), 'Socket_RootBL': ('Mush_Songi', 1.8),
}

def main():
    pv = B.PREVIEW
    B.PREVIEW = False
    BB.main()                     # 캐릭터·맵·버우르스 보스까지 (팔레트 순서 유지)
    B.PREVIEW = pv
    L.reset()
    arm, objs, anims = B.build_character(body_tree)
    L.write_palette(B.PAL_PATH)
    L.export_fbx(os.path.join(B.MOD, 'Boss_Tree.fbx'), True)
    mn, mx = L.world_bbox(objs)
    print('Boss_Tree', len(objs[0].data.polygons), 'faces', 'size', [round(v, 2) for v in (mx - mn)], list(anims))
    if B.PREVIEW:
        preview()

def preview():
    from PIL import Image, ImageDraw
    L.reset(); L.setup_render(260, 10)
    arm, objs, anims = B.build_character(body_tree)
    extra = [B.attach_mush(arm, s, m, sc) for s, (m, sc) in PREVIEW_MUSH.items()]
    bpy.data.images['VoxelPalette.png'].reload()
    mn, mx = L.world_bbox(objs + extra)
    L.aim_camera((mn + mx) / 2, (mx - mn).length * 0.95)
    keyf = {'Roar': [0, 18, 34, 75], 'Slam': [0, 18, 24, 60], 'Sweep': [0, 16, 25, 54], 'RootStrike': [0, 20, 24, 60],
            'SporeBurst': [0, 20, 30, 66], 'PartBreak': [0, 4, 40, 60], 'Down': [0, 26, 98, 120], 'Death': [0, 20, 50, 96]}
    rows = []
    for an, (length, loop, keys) in anims.items():
        arm.animation_data.action = bpy.data.actions[an]
        frames = keyf.get(an) or [round(length * t) for t in (0, .25, .5, .75)]
        row = []
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
    sheet.save(os.path.join(B.PRE, 'anim_Boss_Tree.png'))
    os.remove(os.path.join(B.PRE, '_tmp.png'))
    # 크기 비교
    L.reset(); L.setup_render(1200, 20)
    arm, objs, _ = B.build_character(body_tree, with_anims=False)
    for s, (m, sc) in PREVIEW_MUSH.items():
        B.attach_mush(arm, s, m, sc)
    parm, _, _ = B.build_character(B.body_player, with_anims=False)
    parm.location = (3.5, -6.0, 0); parm.rotation_euler = (0, 0, math.radians(150))
    for x in range(-6, 7):
        for y in range(-8, 6):
            o = L.static_mesh(f'g{x}_{y}', {(0, 0, -1): 'grass1'}, 1.0, B.mat()); o.location = (x, y, 0)
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((0, -1, 3.2)), 11, view=(-0.9, -1.3, 0.55))
    L.render(os.path.join(B.PRE, 'boss_tree.png'))

if __name__ == '__main__':
    main()
