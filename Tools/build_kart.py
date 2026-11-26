import sys, math
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy

H.reset_scene()
OUT_GLB = "/tmp/kartopia/kartopia_kart.glb"
parts = []

body_mat = H.material_preset("KartPaint", "car_paint", "#E8382A", coat=1.0)
accent_mat = H.material_preset("Accent", "glossy_plastic", "#FFC933")
dark_mat = H.material_preset("DarkTrim", "rubber", "#17181C")
chrome_mat = H.material_preset("Chrome", "chrome", "#D8DEE6")

# main body: rounded wedge from lathe-ish boxes
body = H.add_box("KartBody", size=(1.15, 1.65, 0.42), location=(0, 0.1, 0.48), bevel=0.13, mat=body_mat); parts.append(body)
nose = H.add_box("Nose", size=(0.95, 0.55, 0.3), location=(0, 0.98, 0.42), bevel=0.1, mat=body_mat); parts.append(nose)
seat = H.add_box("SeatBack", size=(0.78, 0.28, 0.62), location=(0, -0.48, 0.78), bevel=0.09, mat=dark_mat); parts.append(seat)
wing = H.add_box("RearWing", size=(1.28, 0.16, 0.09), location=(0, -0.92, 1.08), bevel=0.03, mat=accent_mat); parts.append(wing)
for sx in (-1, 1):
    post = H.add_box("WingPost", size=(0.09, 0.09, 0.42), location=(sx*0.45, -0.88, 0.86), bevel=0.02, mat=dark_mat); parts.append(post)
    pipe = H.add_cylinder("Exhaust", radius=0.055, depth=0.34, location=(sx*0.22, -1.0, 0.42), rotation=(90, 0, 0), mat=chrome_mat); parts.append(pipe)

# wheels
for wx, wy in [(-0.62, 0.72), (0.62, 0.72), (-0.64, -0.68), (0.64, -0.68)]:
    w = H.add_wheel("Wheel", radius=0.31, width=0.24, location=(wx, wy, 0.31), spokes=5, tread=10)
    parts.append(w)

# driver: torso + helmet sphere
torso = H.add_sphere("DriverTorso", radius=0.27, location=(0, -0.34, 0.92), scale=(1, 0.82, 1.05), mat=H.material_preset("Suit", "fabric", "#1450C8"))
helmet = H.add_sphere("Helmet", radius=0.235, location=(0, -0.34, 1.28), scale=(1, 1, 0.95), mat=H.material_preset("Helmet", "glossy_plastic", "#FFFFFF"))
visor = H.add_box("Visor", size=(0.34, 0.06, 0.13), location=(0, -0.18, 1.3), bevel=0.03, mat=H.material_preset("Visor", "glass", "#18242F"))
parts += [torso, helmet, visor]

H.ground_contacts(parts)
H.report()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=OUT_GLB, export_format='GLB', use_selection=True, export_apply=True)
print("EXPORTED:", OUT_GLB)

cam = H.add_camera("Cam", location=(3.2, -4.2, 2.2), target=(0, 0, 0.65), lens=50)
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
H.frame_objects(cam, objs, azimuth=35, elevation=16, margin=1.15)
H.studio_lighting(target=objs, intensity=1.1, contrast=0.55)
H.set_render(engine="BLENDER_EEVEE", resolution=(960, 540), view_transform="AgX")
H.render_still("/tmp/kartopia/kart_preview.png")
print("PREVIEW_DONE")
