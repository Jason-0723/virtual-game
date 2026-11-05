# Blender 4.5：替 T-pose 角色建 Mixamo 命名骨架並自動蒙皮，匯出 FBX 給 Unity Humanoid
# 關節位置取自 Mixamo 標記（身高 1.8m，面向 -Y）
import bpy, sys
src, dst = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")

J = {  # 名稱: (頭, 尾, 父)
 "Hips":        ((0, 0, .75),  (0, 0, .85),  None),
 "Spine":       ((0, 0, .85),  (0, 0, 1.0),  "Hips"),
 "Spine1":      ((0, 0, 1.0),  (0, 0, 1.15), "Spine"),
 "Spine2":      ((0, 0, 1.15), (0, 0, 1.38), "Spine1"),
 "Neck":        ((0, 0, 1.40), (0, 0, 1.50), "Spine2"),
 "Head":        ((0, 0, 1.50), (0, 0, 1.80), "Neck"),
}
for s, x in (("Left", 1), ("Right", -1)):  # 角色左手在 +X（面向 -Y）
    J.update({
     s+"Shoulder": ((.03*x, 0, 1.38), (.16*x, 0, 1.36), "Spine2"),
     s+"Arm":      ((.16*x, 0, 1.36), (.52*x, 0, 1.34), s+"Shoulder"),
     s+"ForeArm":  ((.52*x, 0, 1.34), (.71*x, 0, 1.32), s+"Arm"),
     s+"Hand":     ((.71*x, 0, 1.32), (.86*x, 0, 1.31), s+"ForeArm"),
     s+"UpLeg":    ((.09*x, 0, .72),  (.10*x, -.01, .37), "Hips"),
     s+"Leg":      ((.10*x, -.01, .37), (.10*x, .01, .08), s+"UpLeg"),
     s+"Foot":     ((.10*x, .01, .08), (.10*x, -.10, .02), s+"Leg"),
     s+"ToeBase":  ((.10*x, -.10, .02), (.10*x, -.16, .02), s+"Foot"),
    })

bpy.ops.object.armature_add(enter_editmode=True)
arm = bpy.context.active_object
arm.name = "Armature"
eb = arm.data.edit_bones
eb.remove(eb[0])
for n, (h, t, p) in J.items():
    b = eb.new("mixamorig:" + n); b.head, b.tail = h, t
for n, (h, t, p) in J.items():
    if p: eb["mixamorig:" + n].parent = eb["mixamorig:" + p]
bpy.ops.object.mode_set(mode="OBJECT")

bpy.ops.object.select_all(action="DESELECT")
body.select_set(True); arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
print("GROUPS", len(body.vertex_groups))
bpy.ops.export_scene.fbx(filepath=dst, path_mode="COPY", embed_textures=True, add_leaf_bones=False)
