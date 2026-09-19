"""Show the exact generated bed reference beside the real Blender model."""

from __future__ import annotations

import base64
from pathlib import Path

import bpy
from mathutils import Vector


COLLECTION = "ORDAX_BED_REFERENCE_COMPARE"
BED_COLLECTION = "ORDAX_DETAILED_BED_TEST"
PREFIX = "ORDAX_COMPARE_"


def clear_collection():
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        return col
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col


def point_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def set_socket(node, name, value):
    sock = node.inputs.get(name)
    if sock is not None:
        sock.default_value = value


col = clear_collection()

script_path = Path(__file__).resolve()
ref_b64 = script_path.parent / "references" / "bed_reference_512.jpg.b64"
ref_jpg = script_path.parent / "references" / "bed_reference_512.jpg"

ref_jpg.write_bytes(base64.b64decode(ref_b64.read_text(encoding="utf-8")))
image = bpy.data.images.get(PREFIX + "IMAGE")
if image is not None:
    bpy.data.images.remove(image)
image = bpy.data.images.load(str(ref_jpg), check_existing=False)
image.name = PREFIX + "IMAGE"

# Comparison camera.
cam_data = bpy.data.cameras.get(PREFIX + "CAMERA") or bpy.data.cameras.new(PREFIX + "CAMERA")
cam = bpy.data.objects.get(PREFIX + "CAMERA")
if cam is None:
    cam = bpy.data.objects.new(PREFIX + "CAMERA", cam_data)
    col.objects.link(cam)
elif cam not in col.objects[:]:
    for src in list(cam.users_collection):
        src.objects.unlink(cam)
    col.objects.link(cam)

cam.location = (3.25, -13.55, 1.85)
cam.data.lens = 50
point_at(cam, (-0.72, -9.95, 1.02))
bpy.context.scene.camera = cam

# Reference image plane.
bpy.ops.mesh.primitive_plane_add(location=(-1.62, -9.88, 1.10))
plane = bpy.context.object
plane.name = PREFIX + "REFERENCE_PLANE"
for src in list(plane.users_collection):
    src.objects.unlink(plane)
col.objects.link(plane)

# 4:3 reference panel, same vertical footprint as bed.
plane.scale = (0.88, 0.66, 1.0)
# Plane local +Z normal faces camera.
direction = cam.location - plane.location
plane.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()

mat = bpy.data.materials.get(PREFIX + "REFERENCE_MAT") or bpy.data.materials.new(PREFIX + "REFERENCE_MAT")
mat.use_nodes = True
tree = mat.node_tree
tree.nodes.clear()

out = tree.nodes.new("ShaderNodeOutputMaterial")
out.location = (480, 0)
bsdf = tree.nodes.new("ShaderNodeBsdfPrincipled")
bsdf.location = (230, 0)
tex = tree.nodes.new("ShaderNodeTexImage")
tex.location = (-120, 0)
tex.image = image
tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
if bsdf.inputs.get("Emission Color") is not None:
    tree.links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
    set_socket(bsdf, "Emission Strength", 0.7)
set_socket(bsdf, "Roughness", 1.0)
tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
plane.data.materials.append(mat)

# Neutral frame behind reference so it reads cleanly.
bpy.ops.mesh.primitive_plane_add(location=(-1.62, -9.90, 1.10))
back = bpy.context.object
back.name = PREFIX + "REFERENCE_BACK"
for src in list(back.users_collection):
    src.objects.unlink(back)
col.objects.link(back)
back.scale = (0.92, 0.70, 1.0)
back.rotation_euler = plane.rotation_euler

back_mat = bpy.data.materials.get(PREFIX + "BACK_MAT") or bpy.data.materials.new(PREFIX + "BACK_MAT")
back_mat.use_nodes = True
p = back_mat.node_tree.nodes.get("Principled BSDF")
if p:
    p.inputs["Base Color"].default_value = (0.05, 0.045, 0.04, 1)
    set_socket(p, "Roughness", 1.0)
back.data.materials.append(back_mat)

# Put the back slightly behind the image plane along the camera-facing axis.
back.location += (plane.location - cam.location).normalized() * 0.025

# Text labels.
def add_label(body, location):
    bpy.ops.object.text_add(location=location)
    txt = bpy.context.object
    txt.name = PREFIX + body
    for src in list(txt.users_collection):
        src.objects.unlink(txt)
    col.objects.link(txt)
    txt.data.body = body
    txt.data.align_x = "CENTER"
    txt.data.align_y = "CENTER"
    txt.data.size = 0.16
    txt.data.extrude = 0.004
    txt.data.bevel_depth = 0.002
    txt.rotation_euler = plane.rotation_euler
    return txt

add_label("REFERENCIA", (-1.62, -9.82, 1.93))
add_label("BLENDER", (0.25, -9.76, 2.23))

scene = bpy.context.scene
scene.render.resolution_x = 1440
scene.render.resolution_y = 810
scene.render.resolution_percentage = 100

window = bpy.context.window
screen = window.screen if window else None
if screen:
    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue
        space = area.spaces.active
        try:
            space.shading.type = "MATERIAL"
            space.shading.light = "STUDIO"
            space.shading.show_shadows = True
            space.shading.show_cavity = True
            space.shading.use_scene_world = False
            space.shading.use_scene_lights = False
        except Exception:
            pass
        region = next((r for r in area.regions if r.type == "WINDOW"), None)
        if region is not None:
            try:
                with bpy.context.temp_override(window=window, area=area, region=region):
                    bpy.ops.view3d.view_camera()
            except RuntimeError:
                pass

print("Reference comparison view ready: exact reference left, real Blender model right.")
