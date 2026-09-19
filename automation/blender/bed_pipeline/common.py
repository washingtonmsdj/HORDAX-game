"""Shared professional asset-generation primitives for the bed pipeline."""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Iterable

import bpy
from mathutils import Vector


HERE = Path(__file__).resolve().parent
MANIFEST_PATH = HERE / "manifest.json"
MANIFEST = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))

ASSET_ID = MANIFEST["asset"]
OX, OY, OZ = MANIFEST["origin"]

COL_FRAME = "ORDAX_BED_FRAME"
COL_MATTRESS = "ORDAX_BED_MATTRESS"
COL_PILLOWS = "ORDAX_BED_PILLOWS"
COL_BEDDING = "ORDAX_BED_BEDDING"
COL_PRESENTATION = "ORDAX_BED_PRESENTATION"

PREFIX = "ORDAX_BED_"


def manifest() -> dict:
    return MANIFEST


def dim(name: str) -> float:
    return float(MANIFEST["dimensions"][name])


def tol(name: str) -> float:
    return float(MANIFEST["tolerances"][name])


def ensure_collection(name: str, *, clear: bool = False):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collection)
    elif clear:
        for obj in list(collection.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    return collection


def cleanup_orphans() -> None:
    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for block in list(blocks):
            if block.users == 0 and not getattr(block, "use_fake_user", False):
                blocks.remove(block)


def move_to_collection(obj, collection) -> None:
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    collection.objects.link(obj)


def tag(obj, component: str, role: str, **extra) -> None:
    obj["ordax_asset"] = ASSET_ID
    obj["ordax_component"] = component
    obj["ordax_role"] = role
    for key, value in extra.items():
        obj[f"ordax_{key}"] = value


def objects_by_component(component: str) -> list:
    return [obj for obj in bpy.context.scene.objects if obj.get("ordax_asset") == ASSET_ID and obj.get("ordax_component") == component]


def object_by_role(role: str):
    matches = [obj for obj in bpy.context.scene.objects if obj.get("ordax_asset") == ASSET_ID and obj.get("ordax_role") == role]
    return matches[0] if matches else None


def set_socket(node, name: str, value) -> None:
    socket = node.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def prepare_material(name: str):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    tree = mat.node_tree
    tree.nodes.clear()
    output = tree.nodes.new("ShaderNodeOutputMaterial")
    output.location = (660, 0)
    bsdf = tree.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (390, 0)
    tree.links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    return mat, tree, bsdf


def fabric_material(name: str, color, *, roughness=0.9, weave=200.0, bump=0.10):
    mat, tree, bsdf = prepare_material(name)
    tex = tree.nodes.new("ShaderNodeTexCoord")
    tex.location = (-900, 0)
    noise = tree.nodes.new("ShaderNodeTexNoise")
    noise.location = (-680, 100)
    set_socket(noise, "Scale", 18.0)
    set_socket(noise, "Detail", 6.0)
    set_socket(noise, "Roughness", 0.7)
    wx = tree.nodes.new("ShaderNodeTexWave")
    wx.location = (-680, -100)
    wx.wave_type = "BANDS"
    wx.bands_direction = "X"
    set_socket(wx, "Scale", weave)
    wy = tree.nodes.new("ShaderNodeTexWave")
    wy.location = (-680, -270)
    wy.wave_type = "BANDS"
    wy.bands_direction = "Y"
    set_socket(wy, "Scale", weave * 0.93)
    mix = tree.nodes.new("ShaderNodeMixRGB")
    mix.location = (-430, -165)
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-190, 85)
    dark = tuple(max(0.0, float(c) * 0.82) for c in color)
    light = tuple(min(1.0, float(c) * 1.10) for c in color)
    ramp.color_ramp.elements[0].color = (*dark, 1.0)
    ramp.color_ramp.elements[0].position = 0.28
    ramp.color_ramp.elements[1].color = (*light, 1.0)
    ramp.color_ramp.elements[1].position = 0.76
    bump_node = tree.nodes.new("ShaderNodeBump")
    bump_node.location = (135, -140)
    set_socket(bump_node, "Strength", bump)
    set_socket(bump_node, "Distance", 0.02)
    tree.links.new(tex.outputs["Generated"], noise.inputs["Vector"])
    tree.links.new(tex.outputs["Generated"], wx.inputs["Vector"])
    tree.links.new(tex.outputs["Generated"], wy.inputs["Vector"])
    tree.links.new(wx.outputs["Color"], mix.inputs[1])
    tree.links.new(wy.outputs["Color"], mix.inputs[2])
    tree.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(mix.outputs["Color"], bump_node.inputs["Height"])
    tree.links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    set_socket(bsdf, "Roughness", roughness)
    set_socket(bsdf, "Sheen Weight", 0.18)
    return mat


def wood_material(name: str, base=(0.138, 0.068, 0.034)):
    mat, tree, bsdf = prepare_material(name)
    tex = tree.nodes.new("ShaderNodeTexCoord")
    tex.location = (-900, 0)
    mapping = tree.nodes.new("ShaderNodeMapping")
    mapping.location = (-710, 0)
    mapping.inputs["Scale"].default_value = (3.0, 13.0, 3.0)
    noise = tree.nodes.new("ShaderNodeTexNoise")
    noise.location = (-510, 90)
    set_socket(noise, "Scale", 4.5)
    set_socket(noise, "Detail", 6.0)
    grain = tree.nodes.new("ShaderNodeTexWave")
    grain.location = (-510, -130)
    grain.wave_type = "BANDS"
    grain.bands_direction = "Y"
    set_socket(grain, "Scale", 8.0)
    set_socket(grain, "Distortion", 6.0)
    mix = tree.nodes.new("ShaderNodeMixRGB")
    mix.location = (-285, 0)
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 0.64
    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-65, 60)
    ramp.color_ramp.elements[0].color = (base[0] * 0.42, base[1] * 0.42, base[2] * 0.42, 1.0)
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[1].color = (min(1.0, base[0] * 1.35), min(1.0, base[1] * 1.35), min(1.0, base[2] * 1.35), 1.0)
    ramp.color_ramp.elements[1].position = 0.84
    bump_node = tree.nodes.new("ShaderNodeBump")
    bump_node.location = (155, -130)
    set_socket(bump_node, "Strength", 0.12)
    tree.links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], grain.inputs["Vector"])
    tree.links.new(noise.outputs["Fac"], mix.inputs[1])
    tree.links.new(grain.outputs["Color"], mix.inputs[2])
    tree.links.new(mix.outputs["Color"], ramp.inputs["Fac"])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(mix.outputs["Color"], bump_node.inputs["Height"])
    tree.links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])
    set_socket(bsdf, "Roughness", 0.40)
    set_socket(bsdf, "Coat Weight", 0.10)
    return mat


def simple_material(name: str, color, roughness=0.6):
    mat, _tree, bsdf = prepare_material(name)
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    set_socket(bsdf, "Roughness", roughness)
    return mat


def add_cube(collection, name: str, component: str, role: str, location, dimensions, mat, *, bevel=0.02, segments=4):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    if mat is not None:
        obj.data.materials.append(mat)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = segments
    tag(obj, component, role)
    return obj


def add_tapered_leg(collection, name: str, location, height: float, mat):
    bottom = 0.070
    top = 0.052
    z0 = -height / 2
    z1 = height / 2
    verts = [
        (-bottom/2,-bottom/2,z0),(bottom/2,-bottom/2,z0),(bottom/2,bottom/2,z0),(-bottom/2,bottom/2,z0),
        (-top/2,-top/2,z1),(top/2,-top/2,z1),(top/2,top/2,z1),(-top/2,top/2,z1),
    ]
    faces = [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    obj.location = location
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Soft edges", "BEVEL")
    bevel.width = 0.007
    bevel.segments = 3
    tag(obj, "frame", "leg")
    return obj


def add_collision(obj, *, thickness=0.004, friction=8.0):
    # A Collision modifier is stable across the Blender 5.x API and activates
    # the object's collision settings for cloth.
    existing = next((m for m in obj.modifiers if m.type == "COLLISION"), None)
    if existing is None:
        obj.modifiers.new("OrdaX Collision", "COLLISION")
    try:
        obj.collision.thickness_outer = thickness
        obj.collision.thickness_inner = thickness
        obj.collision.cloth_friction = friction
    except Exception:
        pass
    obj["ordax_collision"] = True
    return obj


def apply_modifier(obj, modifier_name: str) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier_name)


def add_soft_pillow(collection, name: str, role: str, location, dimensions, mat, *, rotation_z=0.0, rotation_x=0.0):
    w, d, h = dimensions
    cols, rows = 20, 16
    verts, faces = [], []
    for layer in (-1, 1):
        for row in range(rows):
            fy = row / (rows - 1)
            py = (fy - 0.5) * d
            ey = abs(fy - 0.5) * 2.0
            for col in range(cols):
                fx = col / (cols - 1)
                px = (fx - 0.5) * w
                ex = abs(fx - 0.5) * 2.0
                edge = max(ex, ey)
                bulge = max(0.0, 1.0 - edge ** 2.15)
                pz = layer * (h * 0.31 + h * 0.19 * bulge)
                pz -= layer * h * 0.028 * math.exp(-((px/(w*0.32))**2 + (py/(d*0.32))**2))
                verts.append((px, py, pz))
    size = rows * cols
    for layer_index in range(2):
        base = layer_index * size
        for row in range(rows - 1):
            for col in range(cols - 1):
                a = base + row*cols + col
                b, c, d0 = a+1, a+cols+1, a+cols
                faces.append((a,d0,c,b) if layer_index == 0 else (a,b,c,d0))
    perimeter = list(range(cols))
    perimeter += [r*cols + cols-1 for r in range(1, rows)]
    perimeter += [(rows-1)*cols + c for c in range(cols-2,-1,-1)]
    perimeter += [r*cols for r in range(rows-2,0,-1)]
    for idx, a in enumerate(perimeter):
        b = perimeter[(idx+1) % len(perimeter)]
        faces.append((a,b,b+size,a+size))
    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    obj.location = location
    obj.rotation_euler = (math.radians(rotation_x), 0, math.radians(rotation_z))
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    sub = obj.modifiers.new("Soft pillow", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    for poly in obj.data.polygons:
        poly.use_smooth = True
    tag(obj, "pillows", role)
    return obj


def add_cloth_grid(collection, name: str, role: str, *, width: float, length: float, center_y: float, z: float, mat, cols=28, rows=40, pin_head=True):
    verts, faces = [], []
    for r in range(rows):
        fy = r / (rows - 1)
        y = center_y - length/2 + length*fy
        for c in range(cols):
            fx = c / (cols - 1)
            x = OX - width/2 + width*fx
            # tiny deterministic wrinkle seed prevents perfectly planar results
            zz = z + 0.0035 * math.sin(fx*math.pi*5.0 + fy*1.7)
            verts.append((x, y, zz))
    for r in range(rows-1):
        for c in range(cols-1):
            a = r*cols + c
            faces.append((a,a+1,a+cols+1,a+cols))
    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(PREFIX + name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    tag(obj, "bedding", role)

    if pin_head:
        group = obj.vertex_groups.new(name="PinHead")
        # last two rows = head side (+Y)
        indices = list(range((rows-2)*cols, rows*cols))
        group.add(indices, 1.0, "REPLACE")
        obj["ordax_pin_group"] = group.name
    return obj


def simulate_cloth(obj, *, end_frame: int, thickness=0.004, self_collision=True):
    mod = obj.modifiers.new("OrdaX Cloth", "CLOTH")
    settings = mod.settings
    settings.quality = 6
    settings.mass = 0.22
    settings.tension_stiffness = 18.0
    settings.compression_stiffness = 18.0
    settings.shear_stiffness = 12.0
    settings.bending_stiffness = 0.65
    settings.air_damping = 1.0
    pin = obj.get("ordax_pin_group")
    if pin:
        settings.vertex_group_mass = pin
        settings.pin_stiffness = 12.0

    collision = mod.collision_settings
    collision.use_collision = True
    collision.distance_min = thickness
    collision.collision_quality = 5
    collision.friction = 8.0
    if self_collision:
        collision.use_self_collision = True
        collision.self_distance_min = thickness
        collision.self_friction = 5.0

    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = max(scene.frame_end, end_frame)
    for frame in range(1, end_frame + 1):
        scene.frame_set(frame)
    apply_modifier(obj, mod.name)
    scene.frame_set(1)

    solid = obj.modifiers.new("Fabric thickness", "SOLIDIFY")
    solid.thickness = 0.010
    solid.offset = 0.0
    sub = obj.modifiers.new("Fabric smoothing", "SUBSURF")
    sub.levels = 1
    sub.render_levels = 1
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def world_bounds(obj) -> tuple[Vector, Vector]:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners))),
        Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners))),
    )


def point_at(obj, point) -> None:
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def mark_build(component: str) -> None:
    bpy.context.scene[f"ordax_{ASSET_ID}_{component}_built"] = True
