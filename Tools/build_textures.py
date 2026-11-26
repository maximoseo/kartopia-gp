import sys
sys.path.insert(0, "/home/ser9/.local/share/muse/skills/bundled/built-in--exact-minimal-literal-tests~v1/skills/blender/scripts")
import bpy

def make_texture(name, setup_fn, out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out_node = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Roughness"].default_value = 0.95
    nt.links.new(bsdf.outputs["BSDF"], out_node.inputs["Surface"])
    setup_fn(nt, bsdf)
    mesh = bpy.data.meshes.new(name + "Mesh")
    verts = [(-1,-1,0),(1,-1,0),(1,1,0),(-1,1,0)]
    mesh.from_pydata(verts, [], [[0,1,2,3]])
    mesh.update()
    # UVs
    uv = mesh.uv_layers.new(name="UVMap")
    for loop in mesh.loops:
        uv.data[loop.index].uv = [( -1,-1),(1,-1),(1,1),(-1,1)][loop.vertex_index] if False else uv.data[loop.index].uv
    # simpler: set per-loop from vertex coords
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            v = mesh.loops[li].vertex_index
            uv.data[li].uv = ((verts[v][0]+1)/2, (verts[v][1]+1)/2)
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    bpy.context.collection.objects.link(obj)
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = 'ORTHO'; cam_data.ortho_scale = 2.0
    cam = bpy.data.objects.new("Cam", cam_data)
    cam.location = (0, 0, 2)
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (1,1,1,1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.2
    bpy.context.scene.world = world
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1024; scene.render.resolution_y = 1024
    scene.render.filepath = out
    scene.render.image_settings.file_format = 'PNG'
    bpy.ops.render.render(write_still=True)
    print("TEXTURE_DONE", out)

def link_color(nt, node_out, bsdf, socket="Base Color"):
    nt.links.new(node_out, bsdf.inputs[socket])

def grass(nt, bsdf):
    tex = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 55.0
    noise.inputs["Detail"].default_value = 6.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (0.05,0.32,0.08,1)
    ramp.color_ramp.elements[1].position = 0.75; ramp.color_ramp.elements[1].color = (0.35,0.75,0.22,1)
    nt.links.new(tex.outputs["Generated"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    link_color(nt, ramp.outputs["Color"], bsdf)

def sand(nt, bsdf):
    tex = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 38.0
    noise.inputs["Detail"].default_value = 5.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.3; ramp.color_ramp.elements[0].color = (0.76,0.58,0.3,1)
    ramp.color_ramp.elements[1].position = 0.8; ramp.color_ramp.elements[1].color = (0.98,0.86,0.58,1)
    nt.links.new(tex.outputs["Generated"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    link_color(nt, ramp.outputs["Color"], bsdf)

def water(nt, bsdf):
    tex = nt.nodes.new("ShaderNodeTexCoord")
    wave = nt.nodes.new("ShaderNodeTexWave")
    wave.wave_type = 'BANDS'; wave.bands_direction = 'DIAGONAL'
    wave.inputs["Scale"].default_value = 14.0
    wave.inputs["Distortion"].default_value = 6.0
    wave.inputs["Detail"].default_value = 4.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35; ramp.color_ramp.elements[0].color = (0.02,0.3,0.75,1)
    ramp.color_ramp.elements[1].position = 0.8; ramp.color_ramp.elements[1].color = (0.25,0.75,0.95,1)
    nt.links.new(tex.outputs["Generated"], wave.inputs["Vector"])
    nt.links.new(wave.outputs["Color"], ramp.inputs["Fac"])
    link_color(nt, ramp.outputs["Color"], bsdf)
    bsdf.inputs["Roughness"].default_value = 0.25

base = "/home/ser9/Projects/KartopiaGP/Assets/Art/"
make_texture("GrassTex", grass, base + "grass.png")
make_texture("SandTex", sand, base + "sand.png")
make_texture("WaterTex", water, base + "water.png")
print("ALL_TEXTURES_DONE")
