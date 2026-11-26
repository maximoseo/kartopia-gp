import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy

BASE = "/home/ser9/Projects/KartopiaGP/Assets/Models/"
def export_only(objects, out):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', use_selection=True, export_apply=True)
    print("EXPORTED", out)
def clear():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

H.reset_scene()
wall = H.material_preset("HutWall", "wood", "#C98A4B")
roofm = H.material_preset("HutRoof", "glossy_plastic", "#2EA8C8")
parts = [H.add_box("Hut", size=(2.6, 2.2, 1.9), location=(0, 0, 0.95), bevel=0.05, mat=wall)]
roof = H.add_cone("HutRoof", radius=2.3, depth=1.3, location=(0, 0, 2.6), mat=roofm)
roof.scale = (1.25, 1.05, 1)
parts.append(roof)
parts.append(H.add_box("HutDoor", size=(0.7, 0.08, 1.25), location=(0, 1.12, 0.65), bevel=0.02, mat=H.material_preset("HutDoor", "wood", "#6B3A1E")))
export_only(parts, BASE + "beach_hut.glb")

clear()
rockm = H.material_preset("Rock", "stone", "#8A8F98")
rocks = []
for i, (x, y, s) in enumerate([(0, 0, 1.4), (1.1, 0.4, 0.95), (-1.0, 0.5, 0.8), (0.4, -0.8, 0.7)]):
    r = H.add_sphere(f"Rock{i}", radius=s, location=(x, y, s * 0.55), scale=(1.15, 0.95, 0.75), mat=rockm)
    H.add_modifier(r, "SUBSURF", levels=1)
    rocks.append(r)
export_only(rocks, BASE + "rocks.glb")
print("PROPS2_DONE")
