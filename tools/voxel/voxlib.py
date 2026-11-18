# 복셀 에셋 생성 공통 함수 (Blender 5.x, bpy 모듈 또는 blender -b -P)
# 좌표: 복셀 단위 정수 격자. 캐릭터 정면은 -Y, 위는 +Z. 왼쪽(캐릭터 기준)은 +X.
import bpy, bmesh, math, os
from mathutils import Vector

# ---------------- 색 ----------------
COLORS = {
    # 공통
    'eye': (28, 22, 26), 'eye_hi': (250, 250, 245), 'nose': (60, 40, 40), 'pink': (232, 150, 150),
    # 토끼(버끼)
    'rab': (196, 170, 140), 'rab_dk': (160, 132, 104), 'rab_belly': (238, 226, 206),
    # 곰(버곰)
    'bear': (110, 78, 58), 'bear_dk': (82, 58, 44), 'bear_muz': (186, 150, 112), 'claw': (230, 220, 200),
    # 공룡(버우르스)
    'dino': (92, 128, 104), 'dino_dk': (64, 94, 76), 'dino_belly': (206, 196, 146), 'dino_spot': (130, 150, 90),
    'tooth': (240, 236, 220),
    # 플레이어
    'skin': (240, 198, 160), 'hair': (74, 48, 34), 'straw': (222, 184, 98), 'straw_dk': (170, 130, 66),
    'band': (150, 60, 50), 'shirt': (78, 120, 104), 'pants': (84, 76, 108), 'boot': (78, 56, 42),
    'scarf': (196, 76, 62), 'belt': (110, 78, 50), 'wicker': (176, 134, 82), 'wicker_dk': (130, 94, 56),
    'wood': (140, 98, 62), 'wood_dk': (100, 70, 44), 'cloth': (220, 210, 180),
    # 버섯
    'songi_stem': (236, 226, 204), 'songi_cap': (128, 84, 52), 'songi_cap_dk': (92, 60, 38), 'songi_cap_lt': (164, 118, 78),
    'cloud_1': (92, 78, 70), 'cloud_2': (150, 138, 124), 'cloud_3': (96, 110, 132), 'cloud_4': (226, 220, 206),
    'cloud_under': (236, 230, 214),
    'noru': (240, 232, 208), 'noru_lt': (252, 248, 236), 'noru_dk': (214, 198, 160),
    'pyogo': (120, 74, 46), 'pyogo_dk': (88, 52, 32), 'pyogo_crk': (226, 206, 176), 'pyogo_stem': (232, 220, 196),
    'paengi': (246, 242, 226), 'paengi_cap': (236, 222, 170), 'paengi_base': (190, 160, 110),
    'gwang': (214, 52, 40), 'gwang_dk': (170, 36, 30), 'gwang_dot': (248, 244, 232), 'gwang_stem': (244, 238, 220),
    'seori': (176, 214, 236), 'seori_lt': (226, 242, 252), 'seori_dk': (120, 166, 204), 'seori_stem': (214, 230, 240),
    'puff': (240, 234, 218), 'puff_wart': (214, 202, 172), 'puff_base': (196, 180, 140), 'puff_pore': (96, 84, 56),
    # 지형 장식
    'grass1': (96, 156, 78), 'grass2': (120, 178, 88), 'grass3': (70, 126, 66), 'petal_y': (240, 206, 80),
    'petal_p': (230, 140, 170), 'stone': (132, 132, 138), 'stone_dk': (100, 100, 108), 'bark': (104, 76, 54),
    'bark_dk': (76, 56, 40), 'ring': (196, 160, 110), 'leaf': (82, 140, 84),
}

def _h(i, j, k):
    return ((i * 73856093) ^ (j * 19349663) ^ (k * 83492791)) & 0x7fffffff

SHADES = (0.9, 1.0, 1.0, 1.06)

def shade(name, i, j, k):
    flat = name.startswith('!')
    base = COLORS[name.lstrip('!')]
    f = 1.0 if flat else SHADES[_h(i, j, k) % 4]
    return tuple(max(0, min(255, int(round(c * f)))) for c in base)

# ---------------- 팔레트 텍스처 ----------------
PAL_N = 32
# 색 → 팔레트 칸 번호를 파일에 고정해 둔다. 빌드 순서가 바뀌어도 이미 만든 FBX의 색이 어긋나지 않게(추가만 하고 순서는 안 바꿈).
PAL_INDEX_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'palette_index.json')
PAL = {}

def _load_pal():
    import json
    if os.path.exists(PAL_INDEX_PATH):
        with open(PAL_INDEX_PATH) as f:
            for k, v in json.load(f).items():
                PAL[tuple(int(c) for c in k.split(','))] = v
_load_pal()

def pal_uv(rgb):
    idx = PAL.setdefault(rgb, len(PAL))
    assert idx < PAL_N * PAL_N, '팔레트 칸 부족'
    x, y = idx % PAL_N, idx // PAL_N
    return ((x + 0.5) / PAL_N, 1.0 - (y + 0.5) / PAL_N)

def write_palette(path):
    import json
    from PIL import Image
    img = Image.new('RGBA', (PAL_N, PAL_N), (255, 0, 255, 255))
    for rgb, idx in PAL.items():
        img.putpixel((idx % PAL_N, idx // PAL_N), rgb + (255,))
    img.save(path)
    with open(PAL_INDEX_PATH, 'w') as f:
        json.dump({','.join(map(str, k)): v for k, v in sorted(PAL.items(), key=lambda kv: kv[1])}, f, indent=0)

# ---------------- 도형 ----------------
def _fill(v, bounds, test, col):
    (x0, x1), (y0, y1), (z0, z1) = bounds
    for i in range(math.floor(x0) - 1, math.ceil(x1) + 1):
        for j in range(math.floor(y0) - 1, math.ceil(y1) + 1):
            for k in range(math.floor(z0) - 1, math.ceil(z1) + 1):
                if test(i + .5, j + .5, k + .5):
                    if col is None:
                        v.pop((i, j, k), None)
                    else:
                        v[(i, j, k)] = col(i, j, k) if callable(col) else col

def box(v, x0, x1, y0, y1, z0, z1, col):
    _fill(v, ((x0, x1), (y0, y1), (z0, z1)),
          lambda x, y, z: x0 <= x <= x1 and y0 <= y <= y1 and z0 <= z <= z1, col)

def ell(v, c, r, col, cut=None):
    cx, cy, cz = c; rx, ry, rz = r
    def t(x, y, z):
        if cut and not cut(x, y, z):
            return False
        return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 + ((z - cz) / rz) ** 2 <= 1.0
    _fill(v, ((cx - rx, cx + rx), (cy - ry, cy + ry), (cz - rz, cz + rz)), t, col)

def cyl(v, cx, cy, r, z0, z1, col, ry=None):
    ry = ry or r
    _fill(v, ((cx - r, cx + r), (cy - ry, cy + ry), (z0, z1)),
          lambda x, y, z: z0 <= z <= z1 and ((x - cx) / r) ** 2 + ((y - cy) / ry) ** 2 <= 1.0, col)

def paint_front(v, i, k, col, depth=1):
    """(i,k) 기둥에서 가장 앞(-Y) 복셀을 칠한다."""
    js = [j for (a, j, b) in v if a == i and b == k]
    if js:
        j = min(js)
        for d in range(depth):
            if (i, j + d, k) in v:
                v[(i, j + d, k)] = col

def mirror_x(v):
    """x>=0 쪽을 x<0 쪽으로 복사(좌우 대칭)."""
    for (i, j, k), c in list(v.items()):
        if i >= 0:
            v[(-1 - i, j, k)] = c

# ---------------- 메시 ----------------
_FACES = []
for d in [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)]:
    ax = [n for n in range(3) if d[n] != 0][0]
    o = [n for n in range(3) if n != ax]
    base = [0, 0, 0]; base[ax] = 1 if d[ax] > 0 else 0
    quad = []
    for a, b in [(0, 0), (1, 0), (1, 1), (0, 1)]:
        p = list(base); p[o[0]] = a; p[o[1]] = b; quad.append(tuple(p))
    e1 = Vector(quad[1]) - Vector(quad[0]); e2 = Vector(quad[2]) - Vector(quad[1])
    if e1.cross(e2).dot(Vector(d)) < 0:
        quad.reverse()
    _FACES.append((d, quad))

def add_voxels(bm, uvl, vox, size, origin=(0, 0, 0), dvl=None, group=None):
    """bmesh에 복셀 겉면을 더한다. group이 있으면 해당 정점 그룹 가중치 1."""
    verts = {}
    def V(p):
        if p not in verts:
            vv = bm.verts.new(((p[0] - origin[0]) * size, (p[1] - origin[1]) * size, (p[2] - origin[2]) * size))
            if dvl is not None:
                vv[dvl][group] = 1.0
            verts[p] = vv
        return verts[p]
    for (i, j, k), c in vox.items():
        for (dx, dy, dz), quad in _FACES:
            if (i + dx, j + dy, k + dz) in vox:
                continue
            f = bm.faces.new([V((i + a, j + b, k + cc)) for a, b, cc in quad])
            uv = pal_uv(shade(c, i, j, k))
            for l in f.loops:
                l[uvl].uv = uv

# ---------------- 씬 ----------------
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.render.fps = 30
    s.unit_settings.scale_length = 1.0

def material(name, img_path):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get('Principled BSDF')
    bsdf.inputs['Roughness'].default_value = 1.0
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.0
    tex = nt.nodes.new('ShaderNodeTexImage')
    tex.interpolation = 'Closest'
    img = bpy.data.images.load(img_path, check_existing=True)
    tex.image = img
    nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    return m

def static_mesh(name, vox, size, mat, origin=(0, 0, 0)):
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
    add_voxels(bm, uvl, vox, size, origin)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob

# ---------------- 리그 캐릭터 ----------------
def rig_character(name, size, bones, mesh_parts, mat):
    """bones: [(이름, 머리위치(복셀), 부모, 방향벡터 또는 None)]
    mesh_parts: {메시이름: [(vox, 뼈이름), ...]}  메시마다 SkinnedMesh 하나."""
    scn = bpy.context.scene
    ad = bpy.data.armatures.new(name + '_Rig')
    arm = bpy.data.objects.new(name, ad)
    scn.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    for bn, head, parent, direction in bones:
        eb = ad.edit_bones.new(bn)
        h = Vector(head) * size
        d = Vector(direction or (0, 0, 1)).normalized()
        eb.head = h; eb.tail = h + d * size * 2
        eb.roll = 0
        if parent:
            eb.parent = ad.edit_bones[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode='OBJECT')
    for pb in arm.pose.bones:
        pb.rotation_mode = 'XYZ'
    objs = []
    for mname, parts in mesh_parts.items():
        bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
        dvl = bm.verts.layers.deform.verify()
        gnames = []
        for vox, bn in parts:
            if bn not in gnames:
                gnames.append(bn)
            add_voxels(bm, uvl, vox, size, (0, 0, 0), dvl, gnames.index(bn))
        me = bpy.data.meshes.new(mname); bm.to_mesh(me); bm.free()
        me.materials.append(mat)
        ob = bpy.data.objects.new(mname, me)
        scn.collection.objects.link(ob)
        for g in gnames:
            ob.vertex_groups.new(name=g)
        ob.parent = arm
        md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = arm
        objs.append(ob)
    return arm, objs

def make_action(arm, name, length, keys, loop, hold=False):
    """keys: [(프레임, 뼈, dict(pitch,yaw,roll,x,fwd,up,s))]  각도는 도, 거리는 m.
    pitch+ = 앞으로 숙임(매달린 팔다리는 뒤로), fwd+ = 앞으로, s=(좌우,위,앞뒤) 배율."""
    act = bpy.data.actions.new(name); act.use_fake_user = True
    arm.animation_data_create(); arm.animation_data.action = act
    per = {pb.name: {} for pb in arm.pose.bones}
    for f, bn, kw in keys:
        per[bn][f] = kw
    for bn, fk in per.items():
        if 0 not in fk:
            fk[0] = {}
        last = max(fk)
        if last != length:
            fk[length] = fk[0] if loop else (fk[last] if hold else {})
    for bn, fk in per.items():
        pb = arm.pose.bones[bn]
        for f in sorted(fk):
            kw = fk[f]
            pb.rotation_euler = tuple(math.radians(kw.get(a, 0)) for a in ('pitch', 'yaw', 'roll'))
            pb.location = (kw.get('x', 0), kw.get('up', 0), kw.get('fwd', 0))
            pb.scale = kw.get('s', (1, 1, 1))
            for dp in ('location', 'rotation_euler', 'scale'):
                pb.keyframe_insert(dp, frame=f)
    act.use_frame_range = True
    act.frame_start, act.frame_end = 0, length
    act.use_cyclic = loop
    return act

def clear_pose(arm):
    for pb in arm.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)

# Unity 명명 규칙(26.11.18): 스크립트 안 이름 → 내보낼 파일 이름. 표에 없으면 그대로 쓴다
EXPORT_NAMES = {
    'Buggi': 'Mon_001_Rabbit', 'Budeoji': 'Mon_002_Mole', 'Bugom': 'Mon_003_Bear', 'Burangi': 'Mon_004_Tiger',
    'Boss_Tree': 'Mon_005_Boss_Tree', 'Bugwin': 'Mon_006_Penguin', 'BukBuggi': 'Mon_007_ArcticHare', 'Bubeom': 'Mon_008_Seal',
    'BukBuyeou': 'Mon_009_ArcticFox', 'Boss_BukBugom': 'Mon_010_Boss_PolarBear', 'Buraptor': 'Mon_011_Raptor',
    'Bupachy': 'Mon_012_Pachycephalosaurus', 'Bursus': 'Mon_013_Saurus', 'Bukera': 'Mon_014_Triceratops', 'Boss_Bursus': 'Mon_015_Boss_Saurus',
    'Map_Field01': 'Map_001_Field', 'Map_Ice01': 'Map_002_Ice', 'Map_Dino01': 'Map_003_Dino',
}
for _i, _k in enumerate(['Songi', 'Cloud', 'Malbul', 'Noru', 'Pyogo', 'Paengi', 'Gwangdae', 'Seori'], 1):
    _key = 'Gureum' if _k == 'Cloud' else _k
    EXPORT_NAMES[f'Mush_{_k}'] = f'Mush_{_i:03d}_{_key}'
    EXPORT_NAMES[f'Mush_{_k}_Broken'] = f'Mush_{_i:03d}_{_key}_Broken'

def export_fbx(path, anim):
    d, f = os.path.split(path)
    stem, ext = os.path.splitext(f)
    path = os.path.join(d, EXPORT_NAMES.get(stem, stem) + ext)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=False, object_types={'ARMATURE', 'MESH', 'EMPTY'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', use_mesh_modifiers=False, add_leaf_bones=False,
        primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
        bake_anim=anim, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=anim, bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0, path_mode='STRIP', embed_textures=False)

# ---------------- 렌더 ----------------
def setup_render(res=320, samples=16):
    s = bpy.context.scene
    s.render.engine = 'CYCLES'
    s.cycles.device = 'CPU'
    s.cycles.samples = samples
    s.cycles.use_denoising = False
    s.render.resolution_x = s.render.resolution_y = res
    s.render.film_transparent = True
    s.view_settings.view_transform = 'Standard'
    if not s.world:
        s.world = bpy.data.worlds.new('W')
    s.world.use_nodes = True
    bg = s.world.node_tree.nodes['Background']
    bg.inputs['Color'].default_value = (0.75, 0.8, 0.9, 1); bg.inputs['Strength'].default_value = 0.9
    if 'Sun' not in bpy.data.objects:
        ld = bpy.data.lights.new('Sun', 'SUN'); ld.energy = 3.2; ld.angle = math.radians(10)
        lo = bpy.data.objects.new('Sun', ld); s.collection.objects.link(lo)
        lo.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    if 'Cam' not in bpy.data.objects:
        cd = bpy.data.cameras.new('Cam'); cd.type = 'ORTHO'
        co = bpy.data.objects.new('Cam', cd); s.collection.objects.link(co); s.camera = co

def aim_camera(center, extent, view=(-1.0, -1.3, 0.8)):
    co = bpy.data.objects['Cam']
    d = Vector(view).normalized()
    co.location = Vector(center) + d * (extent * 4 + 5)
    co.rotation_euler = (Vector(center) - co.location).to_track_quat('-Z', 'Y').to_euler()
    co.data.ortho_scale = extent * 1.25
    co.data.clip_end = 1000

def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)

def world_bbox(objs):
    import itertools
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for o in objs:
        oe = o.evaluated_get(dg)
        me = oe.to_mesh()
        pts += [oe.matrix_world @ v.co for v in me.vertices]
        oe.to_mesh_clear()
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx
