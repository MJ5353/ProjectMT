# MushTaker 전투 필드 맵 생성 (Blender 5.x)
# 사용: python build_map.py -- <출력 폴더> [--preview]   (blender -b -P 도 가능)
# 캐릭터·버섯 에셋을 먼저 같은 순서로 다시 만들어 팔레트 색 위치를 유지한 뒤, 맵을 덧붙인다.
# 결과: Models/Map_Field01.fbx (평탄한 평원. Map_Terrain·Map_Props 충돌체, Map_Grass 장식, Map_Bounds 투명 벽, Spawn_*)
import sys, os, math, random
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector, Matrix
import voxlib as L
import build_voxel_assets as B
from voxlib import box, ell, cyl, _h

L.COLORS.update({
    'cap_red': (192, 66, 54), 'cap_red_dk': (156, 50, 44), 'dot': (244, 238, 224),
    'cap_brown': (156, 108, 72), 'cap_brown_dk': (124, 84, 56), 'stalk': (232, 224, 204),
    'gill': (210, 192, 166), 'cap_violet': (128, 104, 156), 'cap_violet_dk': (100, 80, 128),
})

# ---------- 지역 설정: 새 지역은 이 값(바닥·프랍 비율·씨앗)만 바꿔서 만든다 ----------
REGION = {
    'name': 'Map_Field01', 'seed': 5, 'size': 100,
    'ground': 'grass',                                   # 바닥 타일: grass / mycel
    'trees': {'tree_red': 2, 'tree_brown': 3, 'tree_violet': 2},
    'small': {'log': 3, 'mush_cluster': 4, 'rock_big': 2, 'Deco_Rock': 3, 'Deco_Stump': 2},
    'grass_tries': 550,
    'path': 'path', 'accent': 'mycel',                   # 길 타일, 엘리트·보스 자리 무늬 타일
    'lake': None, 'lakes': [],                           # 호수 타일과 (x, y, 반지름) 목록(소품 없음)
    'deco': {'Deco_Grass': 0.82, 'Deco_Flower': 0.18},   # 바닥 장식(충돌 없음)과 비율
    # 스폰 지점 이름. 잡몹은 무리마다 [첫째, 첫째, 둘째] 섞음, 엘리트는 자리마다 번갈아
    'mob': ['Buggi', 'Budeoji'], 'elite': ['Bugom', 'Burangi'], 'boss': 'Boss_Tree',
    'boss_prop': ['log', 'mush_cluster', 'log', 'tree_brown'],   # 보스 자리 둘레(쓰러진 고목·버섯 군락)
}

W = H = REGION['size']   # 블록 수(1블록 = 1m). 지면은 평탄, 높이 0
GROUND = 1               # 바닥 한 겹(윗면이 z=0이 되도록 내보낼 때 1 내림)
BORDER = 7               # 가장자리 숲 띠 두께(m), 바깥에 보이지 않는 벽
ZONES = {                # 이름: (중심 x, y, 반지름)
    'start': (50, 15, 7), 'hub': (50, 46, 5), 'boss': (50, 85, 14),
    'elite1': (22, 32, 7), 'elite2': (78, 30, 7), 'elite3': (26, 64, 7), 'elite4': (74, 64, 7),
}
GROUPS = [(38, 22), (62, 22), (36, 46), (64, 46), (50, 62)]      # 버끼 무리(3마리씩)
PATHS = [('start', 'hub'), ('hub', 'boss'), ('hub', 'elite1'), ('hub', 'elite2'), ('hub', 'elite3'), ('hub', 'elite4')]

def zdist(name, x, y):
    cx, cy, r = ZONES[name]
    return math.hypot(x + .5 - cx, y + .5 - cy) / r

def seg_dist(px, py, a, b):
    ax, ay, _ = ZONES[a]; bx, by, _ = ZONES[b]
    dx, dy = bx - ax, by - ay
    t = max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    # 길이 살짝 굽이치게
    wob = 1.4 * math.sin(t * math.pi * 2 + ax * 0.3)
    return abs(math.hypot(px - ax - t * dx, py - ay - t * dy) - 0) + 0 * wob

def elite_names():
    return [n for n in ZONES if n.startswith('elite')]

def in_lake(x, y, pad=0.0):
    return any(math.hypot(x - lx, y - ly) * (1 + 0.15 * math.sin(math.atan2(y - ly, x - lx) * 3 + lx)) < r + pad
               for lx, ly, r in REGION['lakes'])

def layout():
    hgt, top = {}, {}
    for x in range(W):
        for y in range(H):
            px, py = x + .5, y + .5
            t = REGION['ground']
            if in_lake(px, py):
                t = REGION['lake']
            if min(seg_dist(px, py, a, b) for a, b in PATHS) < 1.4 + 0.5 * math.sin(px * 0.4 + py * 0.3):
                t = REGION['path']
            if any(zdist(e, x, y) < 0.55 and _h(x, y, 2) % 4 != 0 for e in elite_names()):
                t = REGION['accent']
            d = zdist('boss', x, y)
            if 0.55 < d < 0.85 and _h(x // 2, y // 2, 3) % 3 != 0:
                t = REGION['accent']
            hgt[(x, y)] = GROUND; top[(x, y)] = t
    return hgt, top

SIDE = {'grass': 'grass_side', 'path': 'dirt', 'mycel': 'mycel_side', 'snow': 'snow_side', 'snow_path': 'snow_side', 'ice': 'snow_side'}
TOPT = {'grass': 'grass_top', 'path': 'path', 'mycel': 'mycel_top', 'snow': 'snow_top', 'snow_path': 'snow_path', 'ice': 'ice_top'}

def terrain_mesh(hgt, top, m):
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    verts = {}
    def V(p):
        if p not in verts:
            verts[p] = bm.verts.new((p[0], p[1], p[2] - GROUND))
        return verts[p]
    def face(x, y, z, d, quad, tile):
        f = bm.faces.new([V((x + q[0], y + q[1], z + q[2])) for q in quad])
        u0, u1, v0, v1 = B.tile_rect(tile)
        for l, q in zip(f.loops, quad):
            if d[2] != 0:
                a, b = q[0], q[1]
            else:
                a, b = (q[1] if d[0] != 0 else q[0]), q[2]
                if d[0] > 0 or d[1] < 0:
                    a = a if d[1] < 0 else 1 - a
            l[uvl].uv = (u0 + a * (u1 - u0), v0 + b * (v1 - v0))
    for (x, y), n in hgt.items():
        t = top[(x, y)]
        for d, quad in L._FACES:
            if d[2] > 0:
                face(x, y, n - 1, d, quad, TOPT[t])
            elif d[2] == 0:
                m2 = hgt.get((x + d[0], y + d[1]), 0)
                for z in range(m2, n):
                    tile = SIDE[t] if z == n - 1 else ('dirt' if z >= n - 3 else 'stone')
                    face(x, y, z, d, quad, tile)
    me = bpy.data.meshes.new('Map_Terrain'); bm.to_mesh(me); bm.free()
    me.materials.append(m)
    ob = bpy.data.objects.new('Map_Terrain', me); bpy.context.scene.collection.objects.link(ob)
    return ob

# ---------- 소품 (복셀 0.25m) ----------
PROP = 0.25
def tree_red():
    v = {}
    cyl(v, 0, 0, 1.8, 0, 14, 'stalk')
    ell(v, (0, 0, 1.5), (2.4, 2.4, 1.5), 'stalk', cut=lambda x, y, z: z >= 0)
    cyl(v, 0, 0, 2.4, 9, 10, 'gill')   # 턱받이
    ell(v, (0, 0, 13), (6.5, 6.5, 4.2), lambda i, j, k: 'dot' if _h(i // 2, j // 2, k // 2) % 9 == 0 else ('cap_red_dk' if k < 14 else 'cap_red'),
        cut=lambda x, y, z: z >= 12.5)
    cyl(v, 0, 0, 6.2, 12, 13, 'gill')
    return v
def tree_brown():
    v = {}
    cyl(v, 0.5, 0, 1.6, 0, 11, 'stalk')
    ell(v, (0.5, 0, 11.5), (5.5, 5.5, 2.4), lambda i, j, k: 'cap_brown_dk' if (i + j + k) % 5 == 0 else 'cap_brown',
        cut=lambda x, y, z: z >= 11)
    cyl(v, 0.5, 0, 5.2, 10.5, 11.2, 'gill')
    return v
def tree_violet():
    """작은 보라 버섯 무리."""
    v = {}
    for (ox, oy, h, r) in [(0, 0, 7, 3.2), (3.5, 1.5, 4, 2.2), (-2.5, 2.5, 3, 1.8)]:
        cyl(v, ox, oy, max(0.8, r * 0.3), 0, h, 'stalk')
        ell(v, (ox, oy, h), (r, r, r * 0.7), lambda i, j, k: 'cap_violet_dk' if k % 2 else 'cap_violet', cut=lambda x, y, z, h=h: z >= h - 0.3)
    return v
def rock_big():
    v = {}
    ell(v, (0, 0, 0.5), (3.5, 3.0, 3.4), lambda i, j, k: 'stone_dk' if _h(i, j, k) % 3 == 0 else 'stone', cut=lambda x, y, z: z >= 0)
    return v
def log():
    """쓰러진 통나무(길이 4.5m) + 위에 난 작은 버섯."""
    v = {}
    r = 2.3
    L._fill(v, ((-9, 9), (-r, r), (0, 2 * r)), lambda x, y, z: -9 <= x <= 9 and y * y + (z - r) ** 2 <= r * r,
            lambda i, j, k: 'ring' if i in (-9, 8) else ('bark_dk' if (j + k) % 3 == 0 else 'bark'))
    for x in (-5, 1, 4):
        top = 2 * r
        cyl(v, x, 0, 0.6, top, top + 1.5, 'stalk')
        ell(v, (x, 0, top + 1.5), (1.4, 1.4, 0.9), 'cap_brown', cut=lambda a, b, c, t=top: c >= t + 1.2)
    return v
def mush_cluster():
    """버섯 군락: 크기가 다른 버섯 5~6개."""
    v = {}
    rnd = random.Random(13)
    caps = [('cap_red', 'cap_red_dk'), ('cap_brown', 'cap_brown_dk'), ('songi_cap', 'songi_cap_dk'), ('cap_violet', 'cap_violet_dk')]
    for n in range(6):
        ox, oy = rnd.uniform(-4, 4), rnd.uniform(-4, 4)
        h = rnd.uniform(2, 7); r = h * 0.45 + 0.8
        a, b = rnd.choice(caps)
        cyl(v, ox, oy, max(0.6, r * 0.3), 0, h, 'stalk')
        ell(v, (ox, oy, h), (r, r, r * 0.6), lambda i, j, k, a=a, b=b: b if (i + j) % 3 == 0 else a, cut=lambda x, y, z, h=h: z >= h - 0.2)
    return v
PROPS = {'log': log, 'mush_cluster': mush_cluster, 'tree_red': tree_red, 'tree_brown': tree_brown, 'tree_violet': tree_violet, 'rock_big': rock_big}

def merged(name, items, mat):
    """items: [(vox, size, (x,y,z), 회전 0~3)] 를 메시 하나로."""
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    for vox, size, (ox, oy, oz), rot in items:
        start = len(bm.verts)
        L.add_voxels(bm, uvl, vox, size)
        bm.verts.ensure_lookup_table()
        R = Matrix.Rotation(rot * math.pi / 2, 4, 'Z')
        for vv in bm.verts[start:]:
            vv.co = (R @ vv.co) + Vector((ox, oy, oz))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    return ob

def scatter(rnd, n, ok, taken, min_d):
    out = []
    for _ in range(n * 30):
        if len(out) >= n:
            break
        x, y = rnd.uniform(1, W - 1), rnd.uniform(1, H - 1)
        if not ok(x, y) or any(math.hypot(x - a, y - b) < max(min_d, r) for a, b, r in taken):
            continue
        taken.append((x, y, min_d)); out.append((x, y))
    return out

SLOT_OF, SLOTS = {}, {}   # 생성 때 이름(Spawn_종류_n) ↔ 슬롯 이름

def build_map():
    hgt, top = layout()
    tm = L.material('M_Terrain', B.ATLAS_PATH)
    vm = B.mat()
    root = bpy.data.objects.new(REGION['name'], None); bpy.context.scene.collection.objects.link(root)
    terr = terrain_mesh(hgt, top, tm); terr.parent = root
    rnd = random.Random(REGION['seed'])
    ground = lambda x, y: 0.0
    inside = lambda x, y: min(x, y, W - x, H - y) > BORDER + 1
    def clear(x, y, pad=2.5):
        return (min(seg_dist(x, y, a, b) for a, b in PATHS) > pad and not in_lake(x, y, 1.5) and zdist('start', x, y) > 1.1 and
                zdist('boss', x, y) > 1.1 and zdist('hub', x, y) > 1.2 and
                all(zdist(e, x, y) > 1.05 for e in elite_names()) and
                all(math.hypot(x - gx, y - gy) > 5 for gx, gy in GROUPS))
    def pick(table):
        ks = list(table); return rnd.choices(ks, weights=[table[k] for k in ks])[0]
    taken, props, small = [], [], []
    # 가장자리 숲 띠
    for x, y in scatter(rnd, 110, lambda x, y: 1 < min(x, y, W - x, H - y) < BORDER and zdist('start', x, y) > 1.2 and not in_lake(x, y, 1), taken, 3.3):
        props.append((pick(REGION['trees']), x, y))
    # 들판의 작은 숲(나무 2~4그루씩)
    for cx, cy in scatter(rnd, 9, lambda x, y: inside(x, y) and clear(x, y, 6), [], 14):
        for x, y in scatter(rnd, rnd.randint(2, 4), lambda x, y: math.hypot(x - cx, y - cy) < 5 and clear(x, y), taken, 3.2):
            props.append((pick(REGION['trees']), x, y))
    # 흩어진 소품
    for x, y in scatter(rnd, 60, lambda x, y: inside(x, y) and clear(x, y), taken, 2.6):
        k = pick(REGION['small'])
        (props if k in PROPS else small).append((k, x, y))
    # 엘리트 자리: 둘레에 바위·통나무 3~4개
    for e in elite_names():
        cx, cy, r = ZONES[e]
        for a in rnd.sample(range(0, 360, 45), 4):
            x, y = cx + math.cos(math.radians(a)) * r * 1.2, cy + math.sin(math.radians(a)) * r * 1.2
            if min(seg_dist(x, y, p, q) for p, q in PATHS) > 2:
                props.append((rnd.choice(REGION.get('elite_props', ['rock_big', 'log', 'mush_cluster'])), x, y)); taken.append((x, y, 2))
    # 보스 자리: 큰 바위 둘레(남쪽 입구 제외)
    cx, cy, r = ZONES['boss']
    for a in range(0, 360, 24):
        if 245 < a < 295:
            continue
        x, y = cx + math.cos(math.radians(a)) * r * 1.12, cy + math.sin(math.radians(a)) * r * 1.12
        bp = REGION.get('boss_prop', 'rock_big')
        props.append((bp if isinstance(bp, str) else bp[(a // 24) % len(bp)], x, y)); taken.append((x, y, 2))
    cache = {k: fn() for k, fn in PROPS.items()}
    dcache = {k: fn() for k, fn in B.DECOS.items()}
    items = [(cache[k], PROP, (x, y, 0), rnd.randrange(4)) for k, x, y in props]
    items += [(dcache[k], B.DECO_SIZE * 1.6, (x, y, 0), rnd.randrange(4)) for k, x, y in small]
    merged('Map_Props', items, vm).parent = root
    # 풀·꽃 (충돌 없음)
    grass = []
    for _ in range(REGION['grass_tries']):
        x, y = rnd.uniform(BORDER - 1, W - BORDER + 1), rnd.uniform(BORDER - 1, H - BORDER + 1)
        if top[(int(x), int(y))] != REGION['ground'] or zdist('boss', x, y) < 0.9:
            continue
        if 'Deco_Grass' in REGION['deco']:            # 기존 평원과 같은 난수 순서 유지
            k = 'Deco_Flower' if rnd.random() < REGION['deco'].get('Deco_Flower', 0) else 'Deco_Grass'
        else:
            ks = list(REGION['deco']); k = rnd.choices(ks, weights=[REGION['deco'][d] for d in ks])[0]
        grass.append((dcache[k], B.DECO_SIZE, (x, y, 0), rnd.randrange(4)))
    merged('Map_Grass', grass, vm).parent = root
    # 보이지 않는 경계 벽(Unity에서 렌더러 꺼짐)
    wall = {}
    bm = bmesh.new()
    for (x0, x1, y0, y1) in [(0, W, -1, 0), (0, W, H, H + 1), (-1, 0, 0, H), (W, W + 1, 0, H)]:
        bmesh.ops.create_cube(bm, size=1, matrix=Matrix.LocRotScale(Vector(((x0 + x1) / 2, (y0 + y1) / 2, 3)), None, Vector((x1 - x0, y1 - y0, 6))))
    me = bpy.data.meshes.new('Map_Bounds'); bm.to_mesh(me); bm.free()
    bo = bpy.data.objects.new('Map_Bounds', me); bpy.context.scene.collection.objects.link(bo); bo.parent = root
    bo.hide_render = True
    # 스폰 지점 (빈 오브젝트, 위치만 사용). 엘리트 = 버곰
    spawns = {'Spawn_Player': ZONES['start'][:2]}
    as_list = lambda v: [v] if isinstance(v, str) else list(v)
    mobs, elites = as_list(REGION['mob']), as_list(REGION['elite'])
    count = {}
    def add(kind, pos):
        count[kind] = count.get(kind, 0) + 1
        spawns[f"Spawn_{kind}_{count[kind]}"] = pos
    for gx, gy in GROUPS:
        for slot, (dx, dy) in enumerate([(-1.5, -1), (1.5, -0.5), (0, 1.5)]):
            add(mobs[-1] if slot == 2 and len(mobs) > 1 else mobs[0], (gx + dx, gy + dy))
    for i, e in enumerate(elite_names()):
        add(elites[i % len(elites)], ZONES[e][:2])
    spawns[f"Spawn_{REGION['boss']}"] = ZONES['boss'][:2]
    # 오브젝트 이름은 SpawnSlot_01부터 순번(26.11.18 대장). 순서: 플레이어 → 잡몹 무리(3마리씩) → 엘리트 → 보스.
    # 어느 칸에 무엇이 서는지는 지역 데이터가 정한다. 생성 때 의도한 종류는 SLOTS(슬롯 이름 → 종류)로 남긴다.
    SLOT_OF.clear(); SLOTS.clear()
    for n, nm in enumerate(spawns, 1):
        SLOT_OF[nm] = f"SpawnSlot_{n:02d}"; SLOTS[SLOT_OF[nm]] = nm
    for nm, (x, y) in spawns.items():
        o = bpy.data.objects.new(SLOT_OF[nm], None); o.empty_display_type = 'ARROWS'
        o.location = (x, y, 0); o.parent = root
        bpy.context.scene.collection.objects.link(o)
    return root, spawns, ground

def main():
    B.main()                       # 캐릭터·버섯·블록 다시 생성(팔레트 순서 유지)
    L.reset()
    root, spawns, ground = build_map()
    L.write_palette(B.PAL_PATH)
    bpy.data.images['VoxelPalette.png'].reload()
    L.export_fbx(os.path.join(B.MOD, REGION['name'] + '.fbx'), False)
    for o in bpy.data.objects:
        if o.type == 'MESH':
            print(o.name, len(o.data.polygons), 'faces')
    if B.PREVIEW:
        preview(root, spawns, ground)

def preview(root, spawns, ground, actors=None, preview_mush=None, boss_scale=1.5):
    L.setup_render(1400, 16)
    actors = actors or {'Player': B.body_player, 'Buggi': B.body_buggi, 'Bugom': B.body_bugom, 'Bursus': B.body_bursus}
    preview_mush = preview_mush or B.PREVIEW_MUSH
    boss_spawn = f"Spawn_{REGION['boss']}"
    for n in spawns:
        fn = actors.get(n[len('Spawn_'):].rsplit('_', 1)[0] if n[-1].isdigit() else n[len('Spawn_'):])
        if fn is None:
            continue
        arm, objs, _ = B.build_character(fn, with_anims=False)
        e = bpy.data.objects[SLOT_OF[n]]
        arm.location = e.location
        arm.rotation_euler = (0, 0, math.radians(180 if n == 'Spawn_Player' else 0))
        if n == boss_spawn:
            arm.scale = (boss_scale,) * 3
        bpy.context.view_layer.update()
        for s, (m, sc) in preview_mush.get(arm.name.split('.')[0], {}).items():
            B.attach_mush(arm, s, m, sc)
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((W / 2, H / 2 - 2, 0)), 108, view=(-0.55, -1.0, 1.25))
    L.render(os.path.join(B.PRE, 'map.png'))
    # 가까이: 시작 지점에서 북쪽을 본 3인칭 느낌
    cam = bpy.data.objects['Cam']; cam.data.type = 'PERSP'; cam.data.lens = 24
    sx, sy = ZONES['start'][:2]
    cam.location = (sx, sy - 4.5, 3.0)
    cam.rotation_euler = (Vector((sx, sy + 14, 0.5)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.resolution_x = 1400; bpy.context.scene.render.resolution_y = 800
    bpy.context.scene.render.film_transparent = False
    L.render(os.path.join(B.PRE, 'map_tps.png'))

if __name__ == '__main__':
    main()
