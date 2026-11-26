import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy

H.reset_scene()
OUT = "/home/ser9/Projects/KartopiaGP/Assets/Models/kartopia_kart.glb"

paint = H.material_preset("KartPaint", "car_paint", "#E8382A", coat=1.0)
white = H.material_preset("KartPaintAlt", "car_paint", "#FFFFFF", coat=1.0)
accent = H.material_preset("Accent", "glossy_plastic", "#FFC933")
dark = H.material_preset("DarkTrim", "rubber", "#17181C")

parts = []
# smooth main body: sphere scaled into a teardrop hull, cut look via boxes on top
hull = H.add_sphere("Hull", radius=0.62, location=(0, 0.05, 0.52), scale=(0.92, 1.55, 0.62), mat=paint)
H.add_modifier(hull, "SUBSURF", levels=2)
parts.append(hull)
nose = H.add_sphere("Nose", radius=0.4, location=(0, 0.98, 0.42), scale=(0.95, 0.9, 0.55), mat=paint)
H.add_modifier(nose, "SUBSURF", levels=2)
parts.append(nose)
stripe = H.add_box("Stripe", size=(0.26, 1.35, 0.07), location=(0, 0.42, 0.86), bevel=0.03, mat=white); parts.append(stripe)
cockpit_rim = H.add_torus("CockpitRim", major=0.3, minor=0.055, location=(0, -0.38, 0.86), mat=dark)
cockpit_rim.rotation_euler = (1.5708, 0, 0); parts.append(cockpit_rim)
seat = H.add_box("SeatBack", size=(0.66, 0.2, 0.52), location=(0, -0.68, 0.84), bevel=0.08, mat=dark); parts.append(seat)
wing = H.add_box("RearWing", size=(1.32, 0.15, 0.08), location=(0, -1.04, 1.16), bevel=0.03, mat=accent); parts.append(wing)
for sx in (-1, 1):
    parts.append(H.add_box("WingPost", size=(0.08, 0.08, 0.4), location=(sx*0.42, -1.0, 0.97), bevel=0.02, mat=dark))
    parts.append(H.add_cylinder("Exhaust", radius=0.05, depth=0.3, location=(sx*0.2, -1.06, 0.4), rotation=(90, 0, 0), mat=H.material_preset("Chrome3", "chrome", "#D8DEE6")))
    pod = H.add_sphere("SidePod", radius=0.3, location=(sx*0.58, -0.02, 0.48), scale=(0.55, 1.05, 0.6), mat=accent)
    H.add_modifier(pod, "SUBSURF", levels=1); parts.append(pod)
    # headlight
    lamp = H.add_sphere("Headlight", radius=0.085, location=(sx*0.27, 1.18, 0.55), scale=(1, 0.5, 1), mat=H.emission_material("Lamp", "#FFF2B0", 2.2))
    parts.append(lamp)

for wx, wy in [(-0.6, 0.76), (0.6, 0.76), (-0.62, -0.74), (0.62, -0.74)]:
    parts.append(H.add_wheel("Wheel", radius=0.3, width=0.23, location=(wx, wy, 0.3), spokes=5, tread=10))

torso = H.add_sphere("DriverTorso", radius=0.26, location=(0, -0.42, 1.0), scale=(1, 0.8, 1.0), mat=H.material_preset("Suit", "fabric", "#1450C8")); parts.append(torso)
helmet = H.add_sphere("Helmet", radius=0.23, location=(0, -0.42, 1.34), scale=(1, 1, 0.95), mat=H.material_preset("Helmet", "glossy_plastic", "#FF5A2A")); parts.append(helmet)
visor = H.add_box("Visor", size=(0.32, 0.05, 0.12), location=(0, -0.25, 1.36), bevel=0.03, mat=H.material("Visor", "#14202E", roughness=0.12, metallic=0.4)); parts.append(visor)

H.ground_contacts(parts)
H.report()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True, export_apply=True)
print("EXPORTED", OUT)
cam = H.add_camera("Cam", location=(3.1, -4.3, 2.1), target=(0, 0, 0.62), lens=50)
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
H.frame_objects(cam, objs, azimuth=35, elevation=15, margin=1.12)
H.studio_lighting(target=objs, intensity=1.15, contrast=0.55)
H.set_render(engine="BLENDER_EEVEE", resolution=(960, 540), view_transform="AgX")
H.render_still("/tmp/kartopia/kart_preview3.png")
print("PREVIEW3_DONE")
