import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy

H.reset_scene()
OUT = "/home/ser9/Projects/KartopiaGP/Assets/Models/kartopia_kart.glb"

paint = H.material_preset("KartPaint", "car_paint", "#E8382A", coat=1.0)
paint2 = H.material_preset("KartPaintAlt", "car_paint", "#FFFFFF", coat=1.0)
accent = H.material_preset("Accent", "glossy_plastic", "#FFC933")
dark = H.material_preset("DarkTrim", "rubber", "#17181C")
tyre = H.material_preset("Tyre", "rubber", "#101114")
rim = H.material_preset("Rim", "aluminum", "#C9D1DB")
suit = H.material_preset("Suit", "fabric", "#1450C8")
skin = H.material_preset("Skin", "plastic", "#F2B58C") if False else H.material("Skin", "#F2B58C", roughness=0.55, metallic=0.0)

parts = []
# tapered nose via extrusion profile (top view outline), extruded up
nose_outline = [(-0.34, 1.28), (0.34, 1.28), (0.5, 0.72), (0.55, 0.1), (0.52, -0.55), (-0.52, -0.55), (-0.5, 0.72)]
nose = H.add_extrusion("Nose", nose_outline, depth=0.34, bevel=0.07, location=(0, 0, 0.34), mat=paint)
nose.rotation_euler = (0, 0, 0)
parts.append(nose)
# main hull: lathe-ish rounded box, narrower at back
hull = H.add_box("Hull", size=(1.08, 1.15, 0.4), location=(0, -0.28, 0.5), bevel=0.15, mat=paint); parts.append(hull)
stripe = H.add_box("Stripe", size=(0.3, 1.6, 0.06), location=(0, 0.32, 0.72), bevel=0.03, mat=paint2); parts.append(stripe)
cockpit = H.add_box("Cockpit", size=(0.62, 0.6, 0.34), location=(0, -0.42, 0.74), bevel=0.1, mat=dark); parts.append(cockpit)
seat = H.add_box("SeatBack", size=(0.7, 0.22, 0.55), location=(0, -0.72, 0.82), bevel=0.08, mat=dark); parts.append(seat)
wing = H.add_box("RearWing", size=(1.3, 0.15, 0.08), location=(0, -1.02, 1.14), bevel=0.03, mat=accent); parts.append(wing)
for sx in (-1, 1):
    parts.append(H.add_box("WingPost", size=(0.08, 0.08, 0.4), location=(sx*0.42, -0.98, 0.94), bevel=0.02, mat=dark))
    parts.append(H.add_cylinder("Exhaust", radius=0.05, depth=0.3, location=(sx*0.2, -1.05, 0.4), rotation=(90, 0, 0), mat=H.material_preset("Chrome2", "chrome", "#D8DEE6")))
    parts.append(H.add_box("SidePod", size=(0.2, 0.62, 0.26), location=(sx*0.6, -0.05, 0.46), bevel=0.06, mat=accent))

for wx, wy in [(-0.6, 0.78), (0.6, 0.78), (-0.62, -0.72), (0.62, -0.72)]:
    w = H.add_wheel("Wheel", radius=0.3, width=0.23, location=(wx, wy, 0.3), spokes=5, tread=10)
    parts.append(w)
    # force explicit materials on every wheel submesh
    for child in w.children_recursive if hasattr(w, 'children_recursive') else []:
        pass

torso = H.add_sphere("DriverTorso", radius=0.26, location=(0, -0.42, 0.98), scale=(1, 0.8, 1.0), mat=suit); parts.append(torso)
helmet = H.add_sphere("Helmet", radius=0.23, location=(0, -0.42, 1.32), scale=(1, 1, 0.95), mat=H.material_preset("Helmet", "glossy_plastic", "#FF5A2A")); parts.append(helmet)
visor = H.add_box("Visor", size=(0.32, 0.05, 0.12), location=(0, -0.25, 1.34), bevel=0.03, mat=H.material("Visor", "#14202E", roughness=0.12, metallic=0.4)); parts.append(visor)

H.ground_contacts(parts)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True, export_apply=True)
print("EXPORTED", OUT)

cam = H.add_camera("Cam", location=(3.1, -4.3, 2.1), target=(0, 0, 0.62), lens=50)
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
H.frame_objects(cam, objs, azimuth=35, elevation=15, margin=1.12)
H.studio_lighting(target=objs, intensity=1.15, contrast=0.55)
H.set_render(engine="BLENDER_EEVEE", resolution=(960, 540), view_transform="AgX")
H.render_still("/tmp/kartopia/kart_preview2.png")
print("PREVIEW2_DONE")
