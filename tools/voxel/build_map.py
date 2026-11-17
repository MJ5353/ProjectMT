# MushTaker 전투 필드 맵 생성 (Blender 5.x)
# 사용: python build_map.py -- <출력 폴더> [--preview]   (blender -b -P 도 가능)
# 캐릭터·버섯 에셋을 먼저 같은 순서로 다시 만들어 팔레트 색 위치를 유지한 뒤, 맵을 덧붙인다.
# 결과: Models/Map_Field01.fbx (Map_Terrain·Map_Props는 충돌체, Map_Grass는 장식, Spawn_* 빈 오브젝트)
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

W = H = 56           # 블록 수(1블록 = 1m)
GROUND = 2           # 기본 지면 높이(블록)
ZONES = {            # 이름: (중심 x, y, 반지름)
    'start': (28, 8, 6), 'meadow': (14, 25, 10), 'grove': (42, 26, 10), 'arena': (28, 45, 9), 'hub': (28, 25, 4),
}
PATHS = [('start', 'hub'), ('hub', 'meadow'), ('hub', 'grove'), ('hub', 'arena')]

def zdist(name, x, y):
    cx, cy, r = ZONES[name]
    return math.hypot(x + .5 - cx, y + .5 - cy) / r

def seg_dist(px, py, a, b):
    ax, ay, _ = ZONES[a]; bx, by, _ = ZONES[b]
    dx, dy = bx - ax, by - ay
    t = max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(px - ax - t * dx, py - ay - t * dy)

def layout():
    rnd = random.Random(21)
    hgt, top = {}, {}
    bumps = [(rnd.uniform(6, 50), rnd.uniform(6, 50), rnd.uniform(2.5, 4.5)) for _ in range(14)]
    for x in range(W):
        for y in range(H):
            px, py = x + .5, y + .5
            edge = min(x, y, W - 1 - x, H - 1 - y)
            onpath = min(seg_dist(px, py, a, b) for a, b in PATHS) < 1.6
            n = GROUND
            # 테두리 절벽(바깥으로 갈수록 높게, 들쭉날쭉)
            if edge < 4:
                n = GROUND + 4 + (1 if edge == 0 and _h(x, y, 1) % 3 == 0 else 0)
            else:
                flat = onpath or zdist('start', x, y) < 1 or zdist('arena', x, y) < 1.05 or zdist('hub', x, y) < 1
                if not flat:
                    hill = sum(1.2 * math.exp(-((px - bx) ** 2 + (py - by) ** 2) / (2 * r * r)) for bx, by, r in bumps)
                    n += int(hill + 0.25)
                # 보스 공터 둘레 둔덕(남쪽 입구는 열어 둠)
                d = zdist('arena', x, y)
                if 1.05 <= d < 1.3 and not onpath:
                    n = max(n, GROUND + 1)
            t = 'grass'
            if onpath and edge >= 4:
                t = 'path'
            elif zdist('grove', x, y) < 1 and _h(x // 3, y // 3, 5) % 3 == 0:
                t = 'mycel'
            hgt[(x, y)] = n; top[(x, y)] = t
    return hgt, top

SIDE = {'grass': 'grass_side', 'path': 'dirt', 'mycel': 'mycel_side'}
TOPT = {'grass': 'grass_top', 'path': 'path', 'mycel': 'mycel_top'}

def terrain_mesh(hgt, top, m):
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    verts = {}
    def V(p):
        if p not in verts:
            verts[p] = bm.verts.new(p)
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
PROPS = {'tree_red': tree_red, 'tree_brown': tree_brown, 'tree_violet': tree_violet, 'rock_big': rock_big}

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

def build_map():
    hgt, top = layout()
    tm = L.material('M_Terrain', B.ATLAS_PATH)
    vm = B.mat()
    root = bpy.data.objects.new('Map_Field01', None); bpy.context.scene.collection.objects.link(root)
    terr = terrain_mesh(hgt, top, tm); terr.parent = root
    rnd = random.Random(5)
    ground = lambda x, y: hgt[(min(W - 1, max(0, int(x))), min(H - 1, max(0, int(y))))]
    def clear(x, y):
        return (min(seg_dist(x, y, a, b) for a, b in PATHS) > 2.5 and zdist('start', x, y) > 1.1 and
                zdist('arena', x, y) > 1.35 and zdist('hub', x, y) > 1.2)
    def flat_under(x, y, r=1):
        hs = {ground(x + dx, y + dy) for dx in (-r, 0, r) for dy in (-r, 0, r)}
        return len(hs) == 1
    taken = []
    props = []
    # 테두리 숲
    for x, y in scatter(rnd, 46, lambda x, y: 1.5 < min(x, y, W - x, H - y) < 3.5, taken, 3.0):
        k = rnd.choice(['tree_red', 'tree_brown', 'tree_brown', 'tree_violet'])
        props.append((k, x, y))
    # 버곰 숲(안쪽)
    for x, y in scatter(rnd, 10, lambda x, y: zdist('grove', x, y) < 1.1 and clear(x, y) and flat_under(x, y), taken, 4.5):
        props.append((rnd.choice(['tree_red', 'tree_brown', 'tree_violet']), x, y))
    # 그 밖의 들판에 드문드문
    for x, y in scatter(rnd, 8, lambda x, y: min(x, y, W - x, H - y) > 6 and clear(x, y) and zdist('meadow', x, y) > 0.8 and flat_under(x, y), taken, 6):
        props.append((rnd.choice(['tree_red', 'tree_violet']), x, y))
    # 보스 공터 둘레 큰 바위(남쪽 입구 제외)
    cx, cy, r = ZONES['arena']
    for a in range(0, 360, 30):
        if 240 < a < 300:
            continue
        x, y = cx + math.cos(math.radians(a)) * r * 1.15, cy + math.sin(math.radians(a)) * r * 1.15
        props.append(('rock_big', x, y)); taken.append((x, y, 2))
    # 작은 바위·그루터기
    small = []
    for x, y in scatter(rnd, 18, lambda x, y: min(x, y, W - x, H - y) > 4.5 and clear(x, y), taken, 2):
        small.append((rnd.choice(['Deco_Rock', 'Deco_Stump']), x, y))
    cache = {k: fn() for k, fn in PROPS.items()}
    dcache = {k: fn() for k, fn in B.DECOS.items()}
    items = [(cache[k], PROP, (x, y, ground(x, y)), rnd.randrange(4)) for k, x, y in props]
    items += [(dcache[k], B.DECO_SIZE, (x, y, ground(x, y)), rnd.randrange(4)) for k, x, y in small]
    merged('Map_Props', items, vm).parent = root
    # 풀·꽃 (충돌 없음)
    grass = []
    for _ in range(600):
        x, y = rnd.uniform(4.5, W - 4.5), rnd.uniform(4.5, H - 4.5)
        if top[(int(x), int(y))] == 'path' or zdist('arena', x, y) < 0.9:
            continue
        dens = 1.0 if zdist('meadow', x, y) < 1.1 else 0.3
        if rnd.random() > dens:
            continue
        k = 'Deco_Flower' if rnd.random() < (0.35 if dens == 1.0 else 0.12) else 'Deco_Grass'
        grass.append((dcache[k], B.DECO_SIZE, (x, y, ground(x, y)), rnd.randrange(4)))
    merged('Map_Grass', grass, vm).parent = root
    # 스폰 지점 (빈 오브젝트, 위치만 사용)
    spawns = {'Spawn_Player': ZONES['start'][:2]}
    mx, my, _ = ZONES['meadow']
    for i, (dx, dy) in enumerate([(-3, -2), (-2, 0), (-4, 0), (3, 3), (4, 5), (2, 5)]):
        spawns[f'Spawn_Buggi_{i + 1}'] = (mx + dx, my + dy)
    gx, gy, _ = ZONES['grove']
    for i, (dx, dy) in enumerate([(-2, -3), (3, 1), (-1, 4)]):
        spawns[f'Spawn_Bugom_{i + 1}'] = (gx + dx, gy + dy)
    spawns['Spawn_Bursus'] = ZONES['arena'][:2]
    for n, (x, y) in spawns.items():
        e = bpy.data.objects.new(n, None); e.empty_display_type = 'ARROWS'
        e.location = (x, y, ground(x, y)); e.parent = root
        bpy.context.scene.collection.objects.link(e)
    return root, spawns, ground

def main():
    B.main()                       # 캐릭터·버섯·블록 다시 생성(팔레트 순서 유지)
    L.reset()
    root, spawns, ground = build_map()
    L.write_palette(B.PAL_PATH)
    bpy.data.images['VoxelPalette.png'].reload()
    L.export_fbx(os.path.join(B.MOD, 'Map_Field01.fbx'), False)
    for o in bpy.data.objects:
        if o.type == 'MESH':
            print(o.name, len(o.data.polygons), 'faces')
    if B.PREVIEW:
        preview(root, spawns, ground)

def preview(root, spawns, ground):
    L.setup_render(1400, 16)
    who = {'Spawn_Player': ('body_player', None), 'Spawn_Bursus': ('body_bursus', None)}
    for n in spawns:
        if n.startswith('Spawn_Buggi'): who[n] = ('body_buggi', None)
        if n.startswith('Spawn_Bugom'): who[n] = ('body_bugom', None)
    for n, (fn, _) in who.items():
        arm, objs, _ = B.build_character(getattr(B, fn), with_anims=False)
        e = bpy.data.objects[n]
        arm.location = e.location
        arm.rotation_euler = (0, 0, math.radians(180 if n == 'Spawn_Player' else 0))
        if n == 'Spawn_Bursus':
            arm.scale = (1.5, 1.5, 1.5)
        bpy.context.view_layer.update()
        for s, (m, sc) in B.PREVIEW_MUSH.get(arm.name.split('.')[0], {}).items():
            B.attach_mush(arm, s, m, sc)
    bpy.data.images['VoxelPalette.png'].reload()
    L.aim_camera(Vector((W / 2, H / 2 - 2, 2)), 62, view=(-0.55, -1.0, 1.25))
    L.render(os.path.join(B.PRE, 'map.png'))
    # 가까이: 시작 지점에서 북쪽을 본 3인칭 느낌
    cam = bpy.data.objects['Cam']; cam.data.type = 'PERSP'; cam.data.lens = 24
    sx, sy = ZONES['start'][:2]
    cam.location = (sx, sy - 3, ground(sx, sy) + 2.6)
    cam.rotation_euler = (Vector((sx, sy + 12, ground(sx, sy) + 0.5)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.resolution_x = 1400; bpy.context.scene.render.resolution_y = 800
    bpy.context.scene.render.film_transparent = False
    L.render(os.path.join(B.PRE, 'map_tps.png'))

if __name__ == '__main__':
    main()
