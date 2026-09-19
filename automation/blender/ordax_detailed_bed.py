"""Faithful Blender reconstruction of the generated single-bed reference.

The generator rebuilds only ORDAX_DETAILED_BED_TEST and intentionally models
the visible design language of the reference:
- single/twin proportions
- walnut frame with tapered legs and visible foot rail
- tall taupe vertically-channelled headboard
- ivory fitted/top sheet
- olive/sage duvet draped over both sides and the foot
- layered ivory, sage and terracotta cushions
- soft textile microtexture and walnut grain

It is idempotent and safe to run repeatedly in Blender Live.
"""

from __future__ import annotations

import math

import bpy
from mathutils import Vector


COLLECTION_NAME = "ORDAX_DETAILED_BED_TEST"
PREFIX = "ORDAX_BED_"
BED_Y = -10.0

MATTRESS_W = 0.88
MATTRESS_L = 1.88
FRAME_W = 1.02
FRAME_L = 2.02


# ---------------------------------------------------------------------------
# cleanup / helpers
# ---------------------------------------------------------------------------

def ensure_collection():
    old = bpy.data.collections.get(COLLECTION_NAME)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    else:
        old = bpy.data.collections.new(COLLECTION_NAME)
        bpy.context.scene.collection.children.link(old)

    for blocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for block in list(blocks):
            if block.users == 0 and not getattr(block, "use_fake_user", False):
                blocks.remove(block)
    return old


def move_to_collection(obj, collection):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    collection.objects.link(obj)


def set_socket(node, name, value):
    socket = node.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def prepare_material(name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    tree = mat.node_tree
    tree.nodes.clear()

    out = tree.nodes.new("ShaderNodeOutputMaterial")
    out.location = (660, 0)
    bsdf = tree.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (390, 0)
    tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat, tree, bsdf


def fabric_material(name, color, roughness=0.88, weave=190.0, bump=0.12):
    mat, tree, bsdf = prepare_material(name)

    tex = tree.nodes.new("ShaderNodeTexCoord")
    tex.location = (-950, 0)

    noise = tree.nodes.new("ShaderNodeTexNoise")
    noise.location = (-720, 100)
    set_socket(noise, "Scale", 17.0)
    set_socket(noise, "Detail", 7.0)
    set_socket(noise, "Roughness", 0.72)

    wx = tree.nodes.new("ShaderNodeTexWave")
    wx.location = (-720, -100)
    wx.wave_type = "BANDS"
    wx.bands_direction = "X"
    set_socket(wx, "Scale", weave)
    set_socket(wx, "Distortion", 1.7)

    wy = tree.nodes.new("ShaderNodeTexWave")
    wy.location = (-720, -285)
    wy.wave_type = "BANDS"
    wy.bands_direction = "Y"
    set_socket(wy, "Scale", weave * 0.93)
    set_socket(wy, "Distortion", 1.7)

    mix_weave = tree.nodes.new("ShaderNodeMixRGB")
    mix_weave.location = (-470, -160)
    mix_weave.blend_type = "MULTIPLY"
    mix_weave.inputs[0].default_value = 1.0

    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-225, 90)
    darker = tuple(max(0.0, c * 0.80) for c in color)
    lighter = tuple(min(1.0, c * 1.10) for c in color)
    ramp.color_ramp.elements[0].color = (*darker, 1.0)
    ramp.color_ramp.elements[0].position = 0.28
    ramp.color_ramp.elements[1].color = (*lighter, 1.0)
    ramp.color_ramp.elements[1].position = 0.76

    bump_mix = tree.nodes.new("ShaderNodeMixRGB")
    bump_mix.location = (-220, -155)
    bump_mix.blend_type = "MULTIPLY"
    bump_mix.inputs[0].default_value = 0.72

    bump_node = tree.nodes.new("ShaderNodeBump")
    bump_node.location = (145, -165)
    set_socket(bump_node, "Strength", bump)
    set_socket(bump_node, "Distance", 0.022)

    tree.links.new(tex.outputs["Generated"], noise.inputs["Vector"])
    tree.links.new(tex.outputs["Generated"], wx.inputs["Vector"])
    tree.links.new(tex.outputs["Generated"], wy.inputs["Vector"])
    tree.links.new(wx.outputs["Color"], mix_weave.inputs[1])
    tree.links.new(wy.outputs["Color"], mix_weave.inputs[2])
    tree.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    tree.links.new(mix_weave.outputs["Color"], bump_mix.inputs[1])
    tree.links.new(noise.outputs["Fac"], bump_mix.inputs[2])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(bump_mix.outputs["Color"], bump_node.inputs["Height"])
    tree.links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])

    set_socket(bsdf, "Roughness", roughness)
    set_socket(bsdf, "Sheen Weight", 0.18)
    return mat


def wood_material(name, base=(0.31, 0.12, 0.045)):
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
    set_socket(noise, "Roughness", 0.67)

    grain = tree.nodes.new("ShaderNodeTexWave")
    grain.location = (-510, -130)
    grain.wave_type = "BANDS"
    grain.bands_direction = "Y"
    set_socket(grain, "Scale", 8.0)
    set_socket(grain, "Distortion", 6.5)
    set_socket(grain, "Detail", 5.0)

    mix = tree.nodes.new("ShaderNodeMixRGB")
    mix.location = (-285, 0)
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 0.64

    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-65, 60)
    ramp.color_ramp.elements[0].color = (base[0] * 0.38, base[1] * 0.38, base[2] * 0.38, 1.0)
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[1].color = (
        min(1.0, base[0] * 1.32),
        min(1.0, base[1] * 1.32),
        min(1.0, base[2] * 1.32),
        1.0,
    )
    ramp.color_ramp.elements[1].position = 0.84

    bump_node = tree.nodes.new("ShaderNodeBump")
    bump_node.location = (155, -130)
    set_socket(bump_node, "Strength", 0.13)
    set_socket(bump_node, "Distance", 0.018)

    tree.links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], grain.inputs["Vector"])
    tree.links.new(noise.outputs["Fac"], mix.inputs[1])
    tree.links.new(grain.outputs["Color"], mix.inputs[2])
    tree.links.new(mix.outputs["Color"], ramp.inputs["Fac"])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(mix.outputs["Color"], bump_node.inputs["Height"])
    tree.links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])

    set_socket(bsdf, "Roughness", 0.38)
    set_socket(bsdf, "Coat Weight", 0.10)
    return mat


def simple_material(name, color, roughness=0.6):
    mat, _tree, bsdf = prepare_material(name)
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    set_socket(bsdf, "Roughness", roughness)
    return mat


def add_cube(collection, name, location, dimensions, mat, bevel=0.025, segments=4):
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
    return obj


def add_tapered_leg(collection, name, location, height, mat):
    bpy.ops.mesh.primitive_cone_add(
        vertices=32,
        radius1=0.045,
        radius2=0.031,
        depth=height,
        location=location,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    move_to_collection(obj, collection)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Rounded leg edges", "BEVEL")
    bevel.width = 0.007
    bevel.segments = 3
    return obj


def add_round_cylinder(collection, name, location, radius, depth, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=40,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    move_to_collection(obj, collection)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Round", "BEVEL")
    bevel.width = radius * 0.20
    bevel.segments = 3
    return obj


def add_rect_piping(collection, name, center, width, length, z, mat, thickness=0.005):
    curve = bpy.data.curves.new(PREFIX + name + "_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = thickness
    curve.bevel_resolution = 3
    spline = curve.splines.new("POLY")
    spline.points.add(3)

    x0 = center[0] - width / 2
    x1 = center[0] + width / 2
    y0 = center[1] - length / 2
    y1 = center[1] + length / 2

    for point, xyz in zip(spline.points, ((x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z))):
        point.co = (*xyz, 1.0)
    spline.use_cyclic_u = True

    obj = bpy.data.objects.new(PREFIX + name, curve)
    collection.objects.link(obj)
    curve.materials.append(mat)
    return obj


def add_soft_pillow(collection, name, location, dimensions, mat, piping, rotation_z=0.0, rotation_x=0.0):
    """Rounded upholstered pillow with slightly pinched perimeter."""
    x0, y0, z0 = location
    w, d, h = dimensions
    cols = 18
    rows = 14
    verts = []
    faces = []

    # Closed box-like mesh made from a rounded top and bottom surface.
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
                bulge = (1.0 - edge ** 2.2)
                pz = layer * (h * 0.32 + h * 0.18 * max(0.0, bulge))
                # slight central compression
                pz -= layer * h * 0.035 * math.exp(-((px / (w * 0.30)) ** 2 + (py / (d * 0.30)) ** 2))
                verts.append((x0 + px, y0 + py, z0 + pz))

    layer_size = rows * cols
    for layer_index in range(2):
        base = layer_index * layer_size
        for row in range(rows - 1):
            for col in range(cols - 1):
                a = base + row * cols + col
                b = a + 1
                c = a + cols + 1
                d_idx = a + cols
                if layer_index == 0:
                    faces.append((a, d_idx, c, b))
                else:
                    faces.append((a, b, c, d_idx))

    # perimeter sides
    perimeter = []
    for col in range(cols):
        perimeter.append(col)
    for row in range(1, rows):
        perimeter.append(row * cols + (cols - 1))
    for col in range(cols - 2, -1, -1):
        perimeter.append((rows - 1) * cols + col)
    for row in range(rows - 2, 0, -1):
        perimeter.append(row * cols)

    for idx, a in enumerate(perimeter):
        b = perimeter[(idx + 1) % len(perimeter)]
        faces.append((a, b, b + layer_size, a + layer_size))

    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(PREFIX + name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.rotation_euler = (math.radians(rotation_x), 0.0, math.radians(rotation_z))

    sub = obj.modifiers.new("Soft pillow", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    for poly in obj.data.polygons:
        poly.use_smooth = True

    # Small decorative piping loop near the pillow face.
    add_rect_piping(
        collection,
        name + "_Piping",
        (x0, y0),
        w * 0.92,
        d * 0.90,
        z0 + h * 0.48,
        piping,
        thickness=0.0035,
    )
    return obj


def add_draped_cloth(
    collection,
    name,
    *,
    width,
    length,
    y_center,
    top_z,
    mattress_half_w,
    foot_y,
    side_drop_z,
    foot_drop_z,
    mat,
    cols=34,
    rows=46,
    ripple=0.014,
    asymmetry=0.0,
):
    """Cloth sheet that lies on the mattress and falls over sides + foot."""
    verts = []
    faces = []
    x_min = -width / 2
    x_max = width / 2
    y_min = y_center - length / 2
    y_max = y_center + length / 2

    for row in range(rows):
        fy = row / (rows - 1)
        y = y_min + (y_max - y_min) * fy
        for col in range(cols):
            fx = col / (cols - 1)
            x = x_min + (x_max - x_min) * fx

            z = top_z
            z += ripple * math.sin((fx * 5.0 + fy * 0.8) * math.pi)
            z += ripple * 0.55 * math.sin((fy * 8.0 - fx * 1.2) * math.pi)

            # side drape with smooth cubic transition outside mattress top
            side_over = max(0.0, abs(x) - mattress_half_w)
            side_extent = max(0.001, width / 2 - mattress_half_w)
            if side_over > 0:
                t = min(1.0, side_over / side_extent)
                t = t * t * (3.0 - 2.0 * t)
                side_factor = 1.0 + asymmetry * (1.0 if x < 0 else -1.0)
                z = z * (1.0 - t) + side_drop_z * t * side_factor

            # front/foot drape
            if y < foot_y:
                foot_extent = max(0.001, foot_y - y_min)
                t = min(1.0, (foot_y - y) / foot_extent)
                t = t * t * (3.0 - 2.0 * t)
                z = z * (1.0 - t) + foot_drop_z * t

            verts.append((x, y, z))

    for row in range(rows - 1):
        for col in range(cols - 1):
            a = row * cols + col
            b = a + 1
            c = a + cols + 1
            d = a + cols
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(PREFIX + name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)

    solid = obj.modifiers.new("Fabric thickness", "SOLIDIFY")
    solid.thickness = 0.012
    solid.offset = 0.0

    sub = obj.modifiers.new("Fabric smoothing", "SUBSURF")
    sub.levels = 1
    sub.render_levels = 1

    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def point_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(collection, name, location, energy, size, color, target):
    data = bpy.data.lights.new(PREFIX + name + "_Data", "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(PREFIX + name, data)
    obj.location = location
    collection.objects.link(obj)
    point_at(obj, target)
    return obj


collection = ensure_collection()

# ---------------------------------------------------------------------------
# palette
# ---------------------------------------------------------------------------

walnut = wood_material(PREFIX + "Walnut", (0.34, 0.13, 0.045))
walnut_dark = wood_material(PREFIX + "Walnut_Dark", (0.19, 0.055, 0.020))
taupe = fabric_material(PREFIX + "Headboard_Taupe", (0.48, 0.40, 0.31), 0.90, 180.0, 0.13)
ivory = fabric_material(PREFIX + "Ivory_Linen", (0.91, 0.875, 0.79), 0.90, 225.0, 0.09)
ivory_bright = fabric_material(PREFIX + "Ivory_Bright", (0.97, 0.945, 0.89), 0.89, 240.0, 0.08)
sage = fabric_material(PREFIX + "Sage_Olive", (0.37, 0.39, 0.29), 0.93, 195.0, 0.15)
sage_dark = fabric_material(PREFIX + "Sage_Dark", (0.31, 0.33, 0.24), 0.92, 205.0, 0.13)
terracotta = fabric_material(PREFIX + "Terracotta", (0.60, 0.28, 0.14), 0.90, 205.0, 0.12)
mattress_mat = fabric_material(PREFIX + "Mattress", (0.90, 0.89, 0.84), 0.86, 260.0, 0.08)
piping = simple_material(PREFIX + "Piping", (0.28, 0.20, 0.14), 0.65)
floor_mat = simple_material(PREFIX + "Studio_Floor", (0.74, 0.70, 0.64), 0.95)


# ---------------------------------------------------------------------------
# studio floor only - no rug, matching reference
# ---------------------------------------------------------------------------

add_cube(
    collection,
    "Studio_Floor",
    (0.0, BED_Y - 0.10, -0.045),
    (3.8, 4.0, 0.08),
    floor_mat,
    bevel=0.01,
    segments=2,
)


# ---------------------------------------------------------------------------
# walnut frame and legs
# ---------------------------------------------------------------------------

# side rails
add_cube(collection, "Left_Side_Rail", (-0.48, BED_Y, 0.36), (0.075, 1.93, 0.22), walnut, 0.018, 4)
add_cube(collection, "Right_Side_Rail", (0.48, BED_Y, 0.36), (0.075, 1.93, 0.22), walnut, 0.018, 4)

# prominent front rail / footboard as in reference
add_cube(collection, "Foot_Rail", (0.0, BED_Y - 0.985, 0.38), (1.02, 0.095, 0.25), walnut, 0.022, 4)

# hidden mattress support
add_cube(collection, "Mattress_Support", (0.0, BED_Y, 0.40), (0.91, 1.83, 0.08), walnut_dark, 0.015, 3)

# four tapered legs, visible front legs slightly taller beneath rail
for index, (x, yoff) in enumerate(((-0.46, -0.93), (0.46, -0.93), (-0.46, 0.91), (0.46, 0.91))):
    add_tapered_leg(collection, f"Leg_{index}", (x, BED_Y + yoff, 0.17), 0.34, walnut_dark)


# ---------------------------------------------------------------------------
# mattress + fitted sheet
# ---------------------------------------------------------------------------

add_cube(
    collection,
    "Mattress",
    (0.0, BED_Y - 0.005, 0.59),
    (MATTRESS_W, MATTRESS_L, 0.30),
    mattress_mat,
    0.075,
    7,
)

add_cube(
    collection,
    "Fitted_Sheet",
    (0.0, BED_Y - 0.005, 0.755),
    (MATTRESS_W - 0.012, MATTRESS_L - 0.018, 0.055),
    ivory_bright,
    0.030,
    5,
)


# ---------------------------------------------------------------------------
# tall upholstered headboard with seven channels + walnut posts
# ---------------------------------------------------------------------------

head_y = BED_Y + 0.985
post_height = 1.88

for index, x in enumerate((-0.515, 0.515)):
    add_tapered_leg(collection, f"Headboard_Post_Leg_{index}", (x, head_y, 0.40), 0.80, walnut_dark)
    add_cube(
        collection,
        f"Headboard_Post_{index}",
        (x, head_y, 1.25),
        (0.075, 0.13, 1.70),
        walnut,
        0.025,
        5,
    )

# dark backer kept almost entirely behind upholstery
add_cube(
    collection,
    "Headboard_Back",
    (0.0, head_y + 0.015, 1.37),
    (0.99, 0.095, 1.48),
    walnut_dark,
    0.045,
    5,
)

channel_count = 7
usable_w = 0.91
channel_gap = 0.010
channel_w = usable_w / channel_count
for index in range(channel_count):
    x = -usable_w / 2 + channel_w / 2 + index * channel_w
    add_cube(
        collection,
        f"Headboard_Channel_{index}",
        (x, head_y - 0.050, 1.38),
        (channel_w - channel_gap, 0.105, 1.40),
        taupe,
        0.050,
        8,
    )

# rounded walnut top corners / thin cap
add_cube(
    collection,
    "Headboard_Top_Cap",
    (0.0, head_y, 2.095),
    (1.08, 0.13, 0.070),
    walnut,
    0.030,
    5,
)


# ---------------------------------------------------------------------------
# bedding - ivory sheet under olive duvet
# ---------------------------------------------------------------------------

# Ivory sheet visible at top and draping more on the viewer-left side.
add_draped_cloth(
    collection,
    "Ivory_Top_Sheet",
    width=1.04,
    length=1.62,
    y_center=BED_Y - 0.08,
    top_z=0.822,
    mattress_half_w=0.435,
    foot_y=BED_Y - 0.82,
    side_drop_z=0.44,
    foot_drop_z=0.48,
    mat=ivory_bright,
    cols=36,
    rows=46,
    ripple=0.010,
    asymmetry=-0.06,
)

# folded ivory band across upper bed
add_cube(
    collection,
    "Ivory_Folded_Band",
    (0.0, BED_Y + 0.33, 0.865),
    (0.94, 0.24, 0.065),
    ivory_bright,
    0.027,
    6,
)

# Main olive/sage duvet, wider than mattress and with strong foot/side drop.
add_draped_cloth(
    collection,
    "Sage_Duvet",
    width=1.12,
    length=1.54,
    y_center=BED_Y - 0.22,
    top_z=0.875,
    mattress_half_w=0.425,
    foot_y=BED_Y - 0.82,
    side_drop_z=0.35,
    foot_drop_z=0.33,
    mat=sage,
    cols=42,
    rows=54,
    ripple=0.020,
    asymmetry=0.02,
)

# top rolled/folded edge of duvet, visible just below ivory band
duvet_roll = add_round_cylinder(
    collection,
    "Duvet_Fold_Roll",
    (0.0, BED_Y + 0.18, 0.91),
    0.035,
    0.91,
    sage_dark,
    rotation=(0.0, math.radians(90.0), 0.0),
)
duvet_roll.scale.y = 0.70


# ---------------------------------------------------------------------------
# layered pillows exactly in reference color order
# ---------------------------------------------------------------------------

# large ivory back pillow leaning against headboard
add_soft_pillow(
    collection,
    "Back_Ivory_Pillow",
    (0.0, BED_Y + 0.64, 1.11),
    (0.74, 0.34, 0.30),
    ivory_bright,
    piping,
    rotation_z=0.0,
    rotation_x=-10.0,
)

# sage pillow behind terracotta, slightly left
add_soft_pillow(
    collection,
    "Sage_Pillow",
    (-0.16, BED_Y + 0.48, 1.05),
    (0.52, 0.30, 0.28),
    sage_dark,
    piping,
    rotation_z=-5.0,
    rotation_x=-5.0,
)

# terracotta rectangular accent in center
add_soft_pillow(
    collection,
    "Terracotta_Accent",
    (0.07, BED_Y + 0.39, 1.08),
    (0.52, 0.27, 0.27),
    terracotta,
    piping,
    rotation_z=3.0,
    rotation_x=-3.0,
)

# slim sage lumbar in front
add_soft_pillow(
    collection,
    "Sage_Lumbar",
    (0.10, BED_Y + 0.24, 0.99),
    (0.52, 0.20, 0.18),
    sage,
    piping,
    rotation_z=1.0,
    rotation_x=0.0,
)


# ---------------------------------------------------------------------------
# camera / studio lighting
# ---------------------------------------------------------------------------

cam_data = bpy.data.cameras.get(PREFIX + "CAMERA") or bpy.data.cameras.new(PREFIX + "CAMERA")
cam = bpy.data.objects.get(PREFIX + "CAMERA")
if cam is None:
    cam = bpy.data.objects.new(PREFIX + "CAMERA", cam_data)
    collection.objects.link(cam)
else:
    move_to_collection(cam, collection)

# Front-right 3/4 reference angle, close product framing.
cam.location = (2.30, BED_Y - 2.72, 1.72)
cam.data.lens = 56
point_at(cam, (0.0, BED_Y + 0.02, 0.98))
bpy.context.scene.camera = cam

target = (0.0, BED_Y - 0.04, 0.95)
add_area_light(collection, "Key", (1.85, BED_Y - 1.80, 2.75), 1050, 2.8, (1.0, 0.88, 0.78), target)
add_area_light(collection, "Fill", (-1.85, BED_Y - 0.35, 2.30), 620, 2.6, (0.82, 0.86, 1.0), target)
add_area_light(collection, "Head_Rim", (0.0, BED_Y + 1.75, 2.65), 500, 2.2, (1.0, 0.78, 0.62), (0.0, head_y, 1.45))

scene = bpy.context.scene
scene.render.resolution_x = 1280
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100

try:
    scene.view_settings.look = "AgX - Medium High Contrast"
except Exception:
    pass

# Neutral studio-like world.
if scene.world is not None:
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get("Background")
    if background is not None:
        background.inputs["Color"].default_value = (0.055, 0.050, 0.045, 1.0)
        background.inputs["Strength"].default_value = 0.28

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
            space.shading.cavity_type = "WORLD"
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

bpy.ops.object.select_all(action="DESELECT")
focus = bpy.data.objects.get(PREFIX + "Sage_Duvet")
if focus is not None:
    focus.select_set(True)
    bpy.context.view_layer.objects.active = focus

print("Faithful single-bed reconstruction rebuilt from reference.")
