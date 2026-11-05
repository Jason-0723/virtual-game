# Blender 4.5: GLB → 焊接接縫、修法線、1.8m、腳踩地 → FBX（內嵌貼圖）給 Mixamo
import bpy, sys
src, dst = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
bpy.context.view_layer.objects.active = meshes[0]
for o in meshes: o.select_set(True)
if len(meshes) > 1: bpy.ops.object.join()
obj = bpy.context.active_object
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.remove_doubles(threshold=0.0001)   # UV 接縫焊回單一網格
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode="OBJECT")
h = obj.dimensions.z
obj.scale *= 1.8 / h
bpy.ops.object.transform_apply(scale=True)
zmin = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
obj.location.z -= zmin
bpy.ops.object.transform_apply(location=True)
for o in list(bpy.context.scene.objects):
    if o != obj: bpy.data.objects.remove(o)
print("TRIS", sum(len(p.vertices) - 2 for p in obj.data.polygons), "DIM", tuple(round(d, 3) for d in obj.dimensions))
bpy.ops.export_scene.fbx(filepath=dst, use_selection=False, path_mode="COPY", embed_textures=True, apply_unit_scale=True)
