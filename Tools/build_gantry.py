import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy_helpers as H
import bpy

H.reset_scene()
OUT = "/home/ser9/Projects/KartopiaGP/Assets/Models/start_gantry.glb"
post_mat = H.material_preset("GantryPost", "painted_metal", "#123A8F")
banner_mat = H.material_preset("GantryBanner", "glossy_plastic", "#E8382A")
light_g = H.emission_material("LightGreen", "#2BFF5A", 3.0)
light_r = H.emission_material("LightRed", "#FF2B2B", 3.0)
light_y = H.emission_material("LightYellow", "#FFC933", 3.0)

for sx in (-1, 1):
    H.add_cylinder("Post", radius=0.22, depth=6.4, location=(sx*6.2, 0, 3.2), mat=post_mat)
    H.add_box("PostBase", size=(0.9, 0.9, 0.4), location=(sx*6.2, 0, 0.2), bevel=0.05, mat=post_mat)
beam = H.add_box("Beam", size=(13.4, 0.5, 1.1), location=(0, 0, 6.55), bevel=0.06, mat=post_mat)
banner = H.add_box("Banner", size=(8.6, 0.18, 1.7), location=(0, 0, 5.55), bevel=0.04, mat=banner_mat)
# start lights row under banner
for i, mat in enumerate([light_r, light_r, light_y, light_g]):
    H.add_sphere("StartLight", radius=0.21, location=(-1.05 + i*0.7, 0, 4.55), mat=mat)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True, export_apply=True)
print("EXPORTED", OUT)
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
cam = H.add_camera("Cam", location=(9, -11, 5.5), target=(0, 0, 3.4), lens=50)
H.frame_objects(cam, objs, azimuth=30, elevation=12, margin=1.1)
H.studio_lighting(target=objs, intensity=1.1)
H.set_render(engine="BLENDER_EEVEE", resolution=(960, 540), view_transform="AgX")
H.render_still("/tmp/kartopia/gantry_preview.png")
print("GANTRY_PREVIEW_DONE")
