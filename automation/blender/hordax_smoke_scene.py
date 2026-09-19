import bpy
import math
from pathlib import Path

PROJECT_ROOT = Path(bpy.path.abspath("//")).resolve()
if PROJECT_ROOT.name.lower() == "automation":
    PROJECT_ROOT = PROJECT_ROOT.parent
if not (PROJECT_ROOT / "Assets").exists():
    # Blender launched without a .blend, so cwd is the registered project root.
    PROJECT_ROOT = Path.cwd().resolve()

OUTPUT_DIR = PROJECT_ROOT / "Artifacts" / "Blender"
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
BLEND_PATH = OUTPUT_DIR / "hordax-smoke.blend"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def make_material(name, color, metallic=0.0, roughness=0.55):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def add_cube(name, location, scale, material, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("Bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    obj.data.materials.append(material)
    return obj


def add_text(label, text, location, rotation, size, material):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = label
    obj.data.body = text
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.02
    obj.data.materials.append(material)
    return obj


def look_at(obj, point):
    direction = mathutils.Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


import mathutils

clear_scene()

arsenal = make_material("ArsenalLane", (0.035, 0.16, 0.34), metallic=0.12)
horde = make_material("HordeLane", (0.34, 0.045, 0.055), metallic=0.06)
divider = make_material("Divider", (0.03, 0.78, 0.96), metallic=0.35, roughness=0.28)
enemy = make_material("Enemy", (0.62, 0.08, 0.06), roughness=0.48)
elite = make_material("Elite", (0.95, 0.36, 0.04), roughness=0.42)
player_mat = make_material("Player", (0.08, 0.82, 1.0), metallic=0.22, roughness=0.3)
weapon_mat = make_material("Weapon", (0.18, 0.22, 0.28), metallic=0.55, roughness=0.32)
ground = make_material("Ground", (0.016, 0.022, 0.032), roughness=0.8)
white = make_material("Text", (0.9, 0.95, 1.0), metallic=0.05, roughness=0.35)

# Ground and two-lane track.
add_cube("Ground", (0, 25, -0.45), (8.0, 35.0, 0.35), ground, bevel=0)
add_cube("Arsenal Lane", (-3.35, 25, 0.0), (2.75, 35.0, 0.12), arsenal, bevel=0.03)
add_cube("Horde Lane", (3.35, 25, 0.0), (2.75, 35.0, 0.12), horde, bevel=0.03)
add_cube("Divider", (0, 25, 0.16), (0.18, 35.0, 0.10), divider, bevel=0.02)

# Direction markers.
for y in range(-2, 58, 8):
    add_cube("ArsenalArrowShaft", (-3.35, y, 0.23), (0.08, 0.9, 0.04), divider, bevel=0.01)
    add_cube("HordeArrowShaft", (3.35, y, 0.23), (0.08, 0.9, 0.04), elite, bevel=0.01)

# Player proxy.
add_cube("PlayerBody", (-3.35, 2.0, 1.0), (0.55, 0.48, 0.9), player_mat, bevel=0.12)
add_cube("Weapon", (-2.55, 2.15, 1.25), (0.7, 0.12, 0.12), weapon_mat, bevel=0.06)

# Horde proxy: visible marching wall on the opposite lane.
coords = []
for row in range(5):
    y = 18 + row * 2.1
    for col in range(4):
        x = 1.45 + col * 1.25
        coords.append((x, y))

for idx, (x, y) in enumerate(coords):
    mat = elite if idx in {7, 12} else enemy
    height = 1.35 if mat is elite else 1.05
    add_cube(f"Enemy_{idx:02d}", (x, y, height), (0.42, 0.42, height), mat, bevel=0.09)

# A boss marker further ahead.
add_cube("Boss", (3.35, 34.0, 2.0), (1.15, 1.15, 2.0), elite, bevel=0.16)

add_text(
    "ArsenalLabel",
    "ARSENAL  >>>",
    (-3.35, 9.0, 3.4),
    (math.radians(90), 0, 0),
    0.75,
    white,
)
add_text(
    "HordeLabel",
    "<<<  HORDA",
    (3.35, 9.0, 3.4),
    (math.radians(90), 0, 0),
    0.75,
    white,
)

# Camera framing both lanes.
bpy.ops.object.camera_add(location=(0.0, -14.5, 15.0))
camera = bpy.context.object
camera.name = "HORDAX Preview Camera"
camera.data.lens = 48
camera.data.sensor_width = 36
look_at(camera, (0.0, 17.0, 1.3))
bpy.context.scene.camera = camera

# Lighting.
bpy.ops.object.light_add(type="AREA", location=(0, 5, 15))
key = bpy.context.object
key.name = "Key Light"
key.data.energy = 1200
key.data.shape = "RECTANGLE"
key.data.size = 12
key.data.size_y = 18

bpy.ops.object.light_add(type="AREA", location=(-8, 18, 8))
fill = bpy.context.object
fill.name = "Fill Light"
fill.data.energy = 700
fill.data.size = 10
look_at(fill, (0, 20, 0))

bpy.ops.object.light_add(type="AREA", location=(8, 28, 10))
rim = bpy.context.object
rim.name = "Rim Light"
rim.data.energy = 900
rim.data.size = 8
look_at(rim, (2, 22, 1))

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.world.color = (0.008, 0.012, 0.02)

bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
print(f"ORDAX_BLENDER_SMOKE_OK blend={BLEND_PATH}")
print(f"objects={len(scene.objects)} camera={camera.name} engine={scene.render.engine}")
