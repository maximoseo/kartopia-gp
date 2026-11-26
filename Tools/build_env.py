import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy, math

def export_only(objects, out):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', use_selection=True, export_apply=True)
    print("EXPORTED", out)

def clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

BASE = "/home/ser9/Projects/KartopiaGP/Assets/Models/"
trunk_mat = H.material_preset("PalmTrunk", "wood", "#7A4A26")
leaf_mat = H.material_preset("PalmLeaf", "foliage", "#1E8C3A")
hill_mat = H.material_preset("CandyHill", "plastic", "#F2789F") if False else H.material("CandyHill", "#F2789F", roughness=0.5, metallic=0.0)
cloud_mat = H.material("CloudMat", "#FFFFFF", roughness=0.9, metallic=0.0)
stand_mat = H.material_preset("Stand", "painted_metal", "#2850B0")
seat_mat = H.material_preset("Seats", "glossy_plastic", "#FFC933")

# --- palm tree: curved tube trunk + frond spheres ---
H.reset_scene()
trunk = H.add_tube("PalmTrunk", [(0, 0, 0), (0.25, 0.12, 1.6), (0.62, 0.2, 3.1), (1.05, 0.18, 4.3)], radius=0.16, mat=trunk_mat)
fronds = []
for i in range(6):
    a = i * math.tau / 6
    f = H.add_sphere(f"Frond{i}", radius=0.85, location=(1.05 + math.cos(a) * 0.95, 0.18 + math.sin(a) * 0.95, 4.55), scale=(1.25, 0.5, 0.28), mat=leaf_mat)
    fronds.append(f)
crown = H.add_sphere("Crown", radius=0.42, location=(1.05, 0.18, 4.45), mat=leaf_mat)
export_only([trunk, crown] + fronds, BASE + "palm_tree.glb")

# --- candy hill: smooth cone with subsurf ---
clear()
hill = H.add_cone("CandyHill", radius=5, depth=4.2, location=(0, 0, 2.1), mat=hill_mat)
H.add_modifier(hill, "SUBSURF", levels=2)
cap = H.add_sphere("HillCap", radius=1.1, location=(0, 0, 4.35), mat=H.material("HillCap", "#FFFFFF", roughness=0.6, metallic=0.0))
export_only([hill, cap], BASE + "candy_hill.glb")

# --- cloud cluster ---
clear()
clouds = []
for i, (x, y, s) in enumerate([(0, 0, 1.6), (1.5, 0.3, 1.15), (-1.5, -0.2, 1.05), (0.6, -0.5, 0.9)]):
    c = H.add_sphere(f"CloudPuff{i}", radius=s, location=(x, y, 0), scale=(1.35, 1, 0.62), mat=cloud_mat)
    clouds.append(c)
export_only(clouds, BASE + "cloud_cluster.glb")

# --- grandstand: stepped bleachers + roof ---
clear()
stand_parts = []
for row in range(5):
    step = H.add_box(f"Step{row}", size=(11, 1.1, 0.55), location=(0, -row * 1.05, 0.5 + row * 0.62), bevel=0.03, mat=stand_mat)
    seats = H.add_box(f"Seats{row}", size=(10.4, 0.9, 0.28), location=(0, -row * 1.05 + 0.1, 0.95 + row * 0.62), bevel=0.03, mat=seat_mat)
    stand_parts += [step, seats]
for sx in (-1, 1):
    stand_parts.append(H.add_box("RoofPost", size=(0.28, 0.28, 3.6), location=(sx * 5.2, -3.6, 4.6), bevel=0.03, mat=stand_mat))
roof = H.add_box("Roof", size=(12.2, 5.4, 0.35), location=(0, -1.9, 6.6), bevel=0.05, mat=H.material_preset("Roof", "painted_metal", "#E8382A"))
roof.rotation_euler = (0.22, 0, 0)
stand_parts.append(roof)
export_only(stand_parts, BASE + "grandstand.glb")

# preview sheet: all four in a row
clear()
print("ENV_KIT_DONE")
