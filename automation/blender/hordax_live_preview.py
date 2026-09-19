"""Idempotent visible Blender preview for the HORDAX two-lane concept.

Touches only the ORDAX_LIVE_PREVIEW collection. It is intentionally safe to run
repeatedly in the managed visible Blender session.
"""
import math

import bpy
from mathutils import Vector


COLLECTION_NAME = "ORDAX_LIVE_PREVIEW"


def material(name, color, metallic=0.0, roughness=0.5):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.metallic = metallic
    mat.roughness = roughness
    return mat


def ensure_collection():
    old = bpy.data.collections.get(COLLECTION_NAME)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        return old
    collection = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(collection)
    return collection


def move_to_collection(obj, collection):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    collection.objects.link(obj)


def add_cube(collection, name, location, scale, mat, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    obj.data.materials.append(mat)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def add_text(collection, body, location, size, mat):
    bpy.ops.object.text_add(location=location, rotation=(math.radians(72), 0, 0))
    obj = bpy.context.object
    obj.name = "ORDAX_LABEL_" + body.replace(" ", "_")
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.025
    obj.data.bevel_depth = 0.008
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    return obj


def point_camera(camera, point):
    direction = Vector(point) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


collection = ensure_collection()

arsenal = material("ORDAX Arsenal", (0.03, 0.20, 0.55), metallic=0.12, roughness=0.36)
horde = material("ORDAX Horde", (0.50, 0.035, 0.035), metallic=0.05, roughness=0.42)
cyan = material("ORDAX Cyan", (0.02, 0.85, 1.0), metallic=0.2, roughness=0.25)
player_mat = material("ORDAX Player", (0.10, 0.95, 0.55), metallic=0.08, roughness=0.32)
enemy_mat = material("ORDAX Enemy", (0.95, 0.18, 0.10), metallic=0.03, roughness=0.50)
dark = material("ORDAX Track", (0.035, 0.045, 0.065), metallic=0.0, roughness=0.65)
white = material("ORDAX White", (0.95, 0.98, 1.0), metallic=0.0, roughness=0.4)

# One long track, two parallel roles.
add_cube(collection, "Track Base", (0, 8, -0.35), (7.0, 13.0, 0.35), dark, 0.04)
add_cube(collection, "Arsenal Lane", (-3.35, 8, 0.04), (2.75, 13.0, 0.05), arsenal, 0.03)
add_cube(collection, "Horde Lane", (3.35, 8, 0.04), (2.75, 13.0, 0.05), horde, 0.03)
add_cube(collection, "Divider", (0, 8, 0.13), (0.18, 13.0, 0.10), cyan, 0.02)

# Player / weapon lane.
player = add_cube(collection, "Player", (-3.35, -1.5, 0.85), (0.55, 0.75, 0.85), player_mat, 0.16)
add_cube(collection, "Weapon", (-2.55, -0.95, 1.20), (0.55, 0.10, 0.10), cyan, 0.04)

# Horde comes only from the front, in the opposite direction.
for row in range(5):
    for col in range(3):
        x = 2.35 + col * 1.0
        y = 8.0 + row * 1.55
        add_cube(
            collection,
            f"Enemy_{row}_{col}",
            (x, y, 0.65),
            (0.32, 0.34, 0.65),
            enemy_mat,
            0.10,
        )

# Forward / reverse direction arrows using low-cost blocks.
for y in (2.5, 8.0, 13.5):
    add_cube(collection, "Arsenal Direction", (-3.35, y, 0.19), (0.09, 0.75, 0.035), cyan, 0.01)
    add_cube(collection, "Horde Direction", (3.35, y, 0.19), (0.09, 0.75, 0.035), white, 0.01)

add_text(collection, "ARSENAL >>>", (-3.35, 18.5, 0.25), 0.78, cyan)
add_text(collection, "<<< HORDA", (3.35, 18.5, 0.25), 0.78, white)
add_text(collection, "ORDAX LIVE", (0, -3.6, 0.30), 0.72, white)

# Dedicated live-preview camera.
camera_data = bpy.data.cameras.get("ORDAX_LIVE_CAMERA") or bpy.data.cameras.new("ORDAX_LIVE_CAMERA")
camera = bpy.data.objects.get("ORDAX_LIVE_CAMERA")
if camera is None:
    camera = bpy.data.objects.new("ORDAX_LIVE_CAMERA", camera_data)
    collection.objects.link(camera)
elif camera.name not in collection.objects:
    move_to_collection(camera, collection)

camera.location = (13.0, -18.0, 15.0)
camera.data.lens = 48
point_camera(camera, (0, 8.0, 0.0))
bpy.context.scene.camera = camera

# Put the visible 3D view in camera mode so changes are immediately readable.
window = bpy.context.window
screen = window.screen if window else None
if screen:
    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue
        region = next((r for r in area.regions if r.type == "WINDOW"), None)
        if region is None:
            continue
        try:
            with bpy.context.temp_override(window=window, area=area, region=region):
                bpy.ops.view3d.view_camera()
        except RuntimeError:
            pass

bpy.context.view_layer.objects.active = player
player.select_set(True)
print("OrdaX live HORDAX preview updated.")
