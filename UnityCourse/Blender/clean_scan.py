"""
第 6 週 Blender 急診室 —— 自動版
做的事跟投影片一樣：刪飛面 → 補小洞 → Decimate 減面 → 匯出 GLB，並印出面數報告。

用法（終端機）：
  blender -b --python clean_scan.py -- 輸入檔 輸出.glb [目標三角面數=50000] [補洞最大邊數=40]
輸入支援 .glb .gltf .obj .fbx .ply
"""
import sys
import os
import bpy
import bmesh

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if len(argv) < 2:
    print(__doc__)
    sys.exit(1)

src, dst = argv[0], argv[1]
target = int(argv[2]) if len(argv) > 2 else 50000
max_hole_sides = int(argv[3]) if len(argv) > 3 else 40

# 開一個空白場景
bpy.ops.wm.read_factory_settings(use_empty=True)

ext = os.path.splitext(src)[1].lower()
if ext in (".glb", ".gltf"):
    bpy.ops.import_scene.gltf(filepath=src)
elif ext == ".obj":
    bpy.ops.wm.obj_import(filepath=src)
elif ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=src)
elif ext == ".ply":
    bpy.ops.wm.ply_import(filepath=src)
else:
    sys.exit("不支援的格式: " + ext)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not meshes:
    sys.exit("檔案裡沒有網格")

# 散裝合併成單一網格（= Ctrl + J）
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active


def tri_count(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


before = tri_count(obj)

bm = bmesh.new()
bm.from_mesh(obj.data)
bm.faces.ensure_lookup_table()

# ── 第一刀：刪飛面（= 懸停主體按 L → Ctrl+I → X）──
# 找出所有「相連」的面群，只留最大那一群
seen = set()
islands = []
for f in bm.faces:
    if f.index in seen:
        continue
    stack, island = [f], []
    seen.add(f.index)
    while stack:
        cur = stack.pop()
        island.append(cur)
        for e in cur.edges:
            for nf in e.link_faces:
                if nf.index not in seen:
                    seen.add(nf.index)
                    stack.append(nf)
    islands.append(island)

islands.sort(key=len, reverse=True)
floaters = [f for isl in islands[1:] for f in isl]
bmesh.ops.delete(bm, geom=floaters, context="FACES")
loose = [v for v in bm.verts if not v.link_faces]
bmesh.ops.delete(bm, geom=loose, context="VERTS")

# ── 第二刀：補洞（= Alt+點洞邊 → F），只補小洞，大洞請重掃 ──
holes_before = sum(1 for e in bm.edges if e.is_boundary)
bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=max_hole_sides)
holes_after = sum(1 for e in bm.edges if e.is_boundary)

bm.to_mesh(obj.data)
bm.free()
obj.data.update()
after_clean = tri_count(obj)

# ── 第三刀：Decimate 減面，記得 Apply ──
if after_clean > target:
    mod = obj.modifiers.new("Decimate", "DECIMATE")
    mod.ratio = target / after_clean
    bpy.ops.object.modifier_apply(modifier=mod.name)
after = tri_count(obj)

bpy.ops.export_scene.gltf(filepath=dst, export_format="GLB")

print("\n========== 面數報告 ==========")
print(f"刪掉飛面群數   : {len(islands) - 1}")
print(f"洞口邊數       : {holes_before} → {holes_after}（剩下的是大洞，建議重掃）")
print(f"清理前面數     : {before:,}")
print(f"清理後面數     : {after:,}")
print(f"減幅           : {(1 - after / before) * 100:.1f}%")
print(f"已匯出         : {dst}")
print("==============================\n")
