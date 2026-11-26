import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy, math, random

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
trunk_m = H.material_preset("TreeTrunk", "wood", "#6B4226")
greens = [H.material(f"Canopy{i}", c, roughness=0.7, metallic=0.0) for i, c in enumerate(["#1F7A33", "#2E9E44", "#57B83A"])]
parts = [H.add_cylinder("TreeTrunk", radius=0.14, depth=1.7, location=(0, 0, 0.85), mat=trunk_m)]
for i, (x, y, z, s) in enumerate([(0, 0, 2.2, 1.15), (0.55, 0.2, 1.8, 0.8), (-0.55, -0.15, 1.85, 0.85), (0.1, -0.45, 2.5, 0.7)]):
    parts.append(H.add_sphere(f"Canopy{i}", radius=s, location=(x, y, z), scale=(1, 1, 0.85), mat=greens[i % 3]))
export_only(parts, BASE + "round_tree.glb")

clear()
rock_m = H.material_preset("MountainRock", "stone", "#7C6A72")
snow_m = H.material("SnowCap", "#FFFFFF", roughness=0.75, metallic=0.0)
random.seed(7)
ridge = []
for peak in range(5):
    x = -16 + peak * 8
    h = 7 + random.uniform(0, 4.5)
    m = H.add_cone(f"Peak{peak}", radius=4.6, depth=h, location=(x, 0, h/2), mat=rock_m)
    H.add_modifier(m, "SUBSURF", levels=1)
    cap = H.add_cone(f"Cap{peak}", radius=1.7, depth=h*0.32, location=(x, 0, h*0.86), mat=snow_m)
    ridge += [m, cap]
export_only(ridge, BASE + "mountain_ridge.glb")
print("NATURE_DONE")
