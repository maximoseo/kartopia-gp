import subprocess, json, time, sys

P = subprocess.Popen(["/home/ser9/.local/bin/charge-blender-mcp"], stdin=subprocess.PIPE,
                     stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, bufsize=1)
_id = 0

def send(method, params=None):
    global _id
    _id += 1
    P.stdin.write(json.dumps({"jsonrpc": "2.0", "id": _id, "method": method, "params": params or {}}) + "\n")
    P.stdin.flush()
    return _id

def wait(want, timeout=240):
    end = time.time() + timeout
    while time.time() < end:
        line = P.stdout.readline()
        if not line:
            break
        try:
            d = json.loads(line)
        except Exception:
            continue
        if d.get("id") == want:
            return d
    return None

def call(name, args, timeout=240):
    wid = send("tools/call", {"name": name, "arguments": args})
    d = wait(wid, timeout)
    if d is None:
        print("TIMEOUT", name); return None
    res = d.get("result", {})
    texts = [c.get("text", "") for c in res.get("content", []) if c.get("type") == "text"]
    print(name, "->", (" ".join(texts))[:300])
    return res

send("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "kartopia", "version": "1"}})
wait(1)
P.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n"); P.stdin.flush()

BUILD = r'''
import bpy, math

def mat(name, color, rough=0.6, metal=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    return m

def box(name, loc, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object; o.name = name
    o.scale = (size[0]/2, size[1]/2, size[2]/2)
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0:
        mod = o.modifiers.new("bev", 'BEVEL'); mod.width = bevel; mod.segments = 2
    if material: o.data.materials.append(material)
    return o

def cyl(name, loc, radius, depth, material, rot=(0,0,0), verts=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object; o.name = name
    if material: o.data.materials.append(material)
    return o

def sphere(name, loc, scale, material):
    bpy.ops.mesh.primitive_uv_sphere_add(location=loc, segments=24, ring_count=16)
    o = bpy.context.active_object; o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    if material: o.data.materials.append(material)
    return o

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

trunk_m = mat("PalmTrunkV2", (0.42, 0.26, 0.13), 0.8)
leaf_m = mat("PalmLeafV2", (0.08, 0.5, 0.18), 0.65)
coco_m = mat("Coconut", (0.35, 0.22, 0.1), 0.7)

# palm v2: segmented leaning trunk
for i in range(5):
    t = i / 4
    cyl(f"TrunkSeg{i}", (math.sin(t*0.7)*0.9, 0, 0.45 + i*0.85), 0.17 - i*0.012, 0.95, trunk_m, rot=(0, 0.22, 0))
top = (0.75, 0, 4.6)
for i in range(7):
    a = i * math.tau / 7
    f = sphere(f"Frond{i}", (top[0] + math.cos(a)*1.05, math.sin(a)*1.05, top[2] + 0.25), (1.35, 0.42, 0.16), leaf_m)
    f.rotation_euler = (0, 0.35, a)
for i in range(3):
    sphere(f"Coconut{i}", (top[0] + 0.25*math.cos(i*2.2), 0.25*math.sin(i*2.2), top[2] - 0.3), (0.17, 0.17, 0.17), coco_m)
bpy.ops.export_scene.gltf(filepath="/home/ser9/Projects/KartopiaGP/Assets/Models/palm_tree_v2.glb", export_format='GLB')

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

stand_m = mat("StandBlue", (0.1, 0.25, 0.65), 0.5, 0.2)
seat_ms = [mat(f"Seat{i}", c, 0.45) for i, c in enumerate([(1,0.25,0.2),(1,0.8,0.15),(0.25,0.6,1),(0.35,0.9,0.45)])]
roof_m = mat("StandRoof", (0.9, 0.12, 0.1), 0.45, 0.1)
for row in range(6):
    box(f"Step{row}", (0, -row*1.0, 0.45 + row*0.6), (12, 1.05, 0.5), stand_m, 0.03)
    for seat in range(10):
        box(f"Seat{row}_{seat}", (-4.9 + seat*1.08, -row*1.0, 0.85 + row*0.6), (0.82, 0.75, 0.3), seat_ms[(row+seat) % 4], 0.04)
for sx in (-1, 1):
    cyl(f"RoofPost{sx}", (sx*5.6, -4.4, 5.4), 0.14, 3.4, stand_m)
roof = box("Roof", (0, -2.4, 7.3), (13, 5.6, 0.3), roof_m, 0.05)
roof.rotation_euler = (0.16, 0, 0)
bpy.ops.export_scene.gltf(filepath="/home/ser9/Projects/KartopiaGP/Assets/Models/grandstand_v2.glb", export_format='GLB')
print("MCP_BUILD_OK")
'''

call("blender_python_exec", {"code": BUILD, "transport": "headless"}, timeout=280)
call("blender_scene_list_objects", {"transport": "headless"}, timeout=60)
call("blender_render_still", {"output_path": "/tmp/kartopia/mcp_stand.png", "transport": "headless"}, timeout=280)
P.kill()
print("MCP_DRIVER_DONE")
