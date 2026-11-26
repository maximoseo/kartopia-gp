import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy, math

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
tyre = H.material_preset("TyreProp", "rubber", "#14151A")
parts=[]
for i in range(3):
    t = H.add_torus("Tire", major=0.42, minor=0.16, location=(0, 0, 0.22 + i*0.3), mat=tyre)
    t.rotation_euler=(1.5708,0,0); parts.append(t)
export_only(parts, BASE+"tire_stack.glb")

clear()
cone_mat = H.material_preset("Cone", "glossy_plastic", "#FF6A1A")
white = H.material_preset("ConeStripe", "glossy_plastic", "#FFFFFF")
cone = H.add_cone("Cone", radius=0.32, depth=0.85, location=(0,0,0.55), mat=cone_mat)
base = H.add_box("ConeBase", size=(0.62,0.62,0.09), location=(0,0,0.05), bevel=0.02, mat=cone_mat)
stripe = H.add_cylinder("ConeStripe", radius=0.21, depth=0.16, location=(0,0,0.55), mat=white)
export_only([cone, base, stripe], BASE+"cone.glb")

clear()
tower = H.material_preset("Lighthouse", "glossy_plastic", "#FFFFFF")
red = H.material_preset("LighthouseRed", "glossy_plastic", "#E8382A")
body=[]
for i, (z, mat) in enumerate([(1.1, tower),(2.6, red),(4.1, tower)]):
    seg = H.add_cylinder(f"Tower{i}", radius=0.85, depth=1.5, location=(0,0,z), mat=mat); body.append(seg)
roof = H.add_cone("LighthouseRoof", radius=1.05, depth=0.9, location=(0,0,5.3), mat=red); body.append(roof)
lamp = H.add_sphere("LighthouseLamp", radius=0.42, location=(0,0,4.85), mat=H.emission_material("LampGlow", "#FFE98A", 3.0)); body.append(lamp)
export_only(body, BASE+"lighthouse.glb")
print("PROPS_DONE")
