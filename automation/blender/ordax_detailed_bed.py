"""Premium single-bed generation test for OrdaX Blender Live.

Rebuilds only ORDAX_DETAILED_BED_TEST. The HORDAX scene and every object
outside that collection are left untouched.

Design target:
- Brazilian single mattress proportions: 0.88 x 1.88 m
- warm walnut frame with visible procedural grain
- upholstered channel headboard
- ivory fitted/top sheets
- sage duvet with soft folds
- layered pillows and terracotta accent cushion
- procedural woven-fabric micro texture (no external assets/licenses)
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
FRAME_W = 0.99
FRAME_L = 2.02


def _clamp_color(color):
    return tuple(max(0.0, min(1.0, float(c))) for c in color)


def _color_mul(color, factor):
    return _clamp_color(tuple(c * factor for c in color))


def _socket(node, name):
    return node.inputs.get(name)


def _set_socket(node, name, value):
    socket = _socket(node, name)
    if socket is not None:
        socket.default_value = value


def ensure_collection():
    old = bpy.data.collections.get(COLLECTION_NAME)
    if old:
        # Remove only objects owned by this generated test collection.
        # Their datablocks are then eligible for the safe orphan pass below.
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        _cleanup_unused_geometry()
        return old

    collection = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(collection)
    _cleanup_unused_geometry()
    return collection


def _cleanup_unused_geometry():
    """Drop geometry datablocks with no users and no fake-user protection.

    This prevents repeated live generations from accumulating old Cube/Cylinder
    mesh datablocks while preserving every datablock still referenced anywhere
    in the HORDAX scene.
    """
    datablocks = (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.cameras,
        bpy.data.lights,
    )
    for blocks in datablocks:
        for block in list(blocks):
            if block.users == 0 and not getattr(block, "use_fake_user", False):
                blocks.remove(block)


def move_to_collection(obj, collection):
    for source in list(obj.users_collection):
        source.objects.unlink(obj)
    collection.objects.link(obj)


def _prepare_material(name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    tree = mat.node_tree
    tree.nodes.clear()

    output = tree.nodes.new("ShaderNodeOutputMaterial")
    output.location = (720, 0)

    bsdf = tree.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (430, 0)
    tree.links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    return mat, tree, bsdf


def fabric_material(name, color, *, roughness=0.82, weave_scale=210.0, bump_strength=0.12):
    """Procedural woven fabric, intentionally license-free and portable."""
    mat, tree, bsdf = _prepare_material(name)
    color = _clamp_color(color)

    texcoord = tree.nodes.new("ShaderNodeTexCoord")
    texcoord.location = (-1000, 20)

    noise = tree.nodes.new("ShaderNodeTexNoise")
    noise.location = (-780, 130)
    _set_socket(noise, "Scale", 18.0)
    _set_socket(noise, "Detail", 7.0)
    _set_socket(noise, "Roughness", 0.72)

    weave_x = tree.nodes.new("ShaderNodeTexWave")
    weave_x.location = (-780, -100)
    weave_x.wave_type = "BANDS"
    weave_x.bands_direction = "X"
    _set_socket(weave_x, "Scale", weave_scale)
    _set_socket(weave_x, "Distortion", 2.1)
    _set_socket(weave_x, "Detail", 2.0)

    weave_y = tree.nodes.new("ShaderNodeTexWave")
    weave_y.location = (-780, -310)
    weave_y.wave_type = "BANDS"
    weave_y.bands_direction = "Y"
    _set_socket(weave_y, "Scale", weave_scale * 0.92)
    _set_socket(weave_y, "Distortion", 2.0)
    _set_socket(weave_y, "Detail", 2.0)

    mix_weave = tree.nodes.new("ShaderNodeMixRGB")
    mix_weave.location = (-510, -190)
    mix_weave.blend_type = "MULTIPLY"
    mix_weave.inputs["Fac"].default_value = 1.0

    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-230, 120)
    ramp.color_ramp.elements[0].position = 0.28
    ramp.color_ramp.elements[0].color = (*_color_mul(color, 0.72), 1.0)
    ramp.color_ramp.elements[1].position = 0.77
    ramp.color_ramp.elements[1].color = (*_color_mul(color, 1.13), 1.0)

    bump_mix = tree.nodes.new("ShaderNodeMixRGB")
    bump_mix.location = (-235, -160)
    bump_mix.blend_type = "MULTIPLY"
    bump_mix.inputs["Fac"].default_value = 0.72

    bump = tree.nodes.new("ShaderNodeBump")
    bump.location = (170, -170)
    _set_socket(bump, "Strength", bump_strength)
    _set_socket(bump, "Distance", 0.035)

    tree.links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    tree.links.new(texcoord.outputs["Generated"], weave_x.inputs["Vector"])
    tree.links.new(texcoord.outputs["Generated"], weave_y.inputs["Vector"])
    tree.links.new(weave_x.outputs["Color"], mix_weave.inputs[1])
    tree.links.new(weave_y.outputs["Color"], mix_weave.inputs[2])
    tree.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    tree.links.new(mix_weave.outputs["Color"], bump_mix.inputs[1])
    tree.links.new(noise.outputs["Fac"], bump_mix.inputs[2])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(bump_mix.outputs["Color"], bump.inputs["Height"])
    tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    _set_socket(bsdf, "Roughness", roughness)
    _set_socket(bsdf, "Sheen Weight", 0.22)
    return mat


def wood_material(name, base_color):
    """Procedural walnut with elongated grain and low-relief pores."""
    mat, tree, bsdf = _prepare_material(name)
    base_color = _clamp_color(base_color)

    texcoord = tree.nodes.new("ShaderNodeTexCoord")
    texcoord.location = (-1000, 0)

    mapping = tree.nodes.new("ShaderNodeMapping")
    mapping.location = (-820, 0)
    mapping.inputs["Scale"].default_value = (4.0, 15.0, 3.0)

    noise = tree.nodes.new("ShaderNodeTexNoise")
    noise.location = (-610, 90)
    _set_socket(noise, "Scale", 3.8)
    _set_socket(noise, "Detail", 6.5)
    _set_socket(noise, "Roughness", 0.66)

    grain = tree.nodes.new("ShaderNodeTexWave")
    grain.location = (-610, -160)
    grain.wave_type = "BANDS"
    grain.bands_direction = "Y"
    _set_socket(grain, "Scale", 8.5)
    _set_socket(grain, "Distortion", 7.0)
    _set_socket(grain, "Detail", 5.0)
    _set_socket(grain, "Detail Scale", 2.2)

    mix = tree.nodes.new("ShaderNodeMixRGB")
    mix.location = (-370, 10)
    mix.blend_type = "MULTIPLY"
    mix.inputs["Fac"].default_value = 0.58

    ramp = tree.nodes.new("ShaderNodeValToRGB")
    ramp.location = (-125, 70)
    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (*_color_mul(base_color, 0.38), 1.0)
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = (*_color_mul(base_color, 1.28), 1.0)

    bump = tree.nodes.new("ShaderNodeBump")
    bump.location = (170, -150)
    _set_socket(bump, "Strength", 0.16)
    _set_socket(bump, "Distance", 0.025)

    tree.links.new(texcoord.outputs["Generated"], mapping.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    tree.links.new(mapping.outputs["Vector"], grain.inputs["Vector"])
    tree.links.new(noise.outputs["Fac"], mix.inputs[1])
    tree.links.new(grain.outputs["Color"], mix.inputs[2])
    tree.links.new(mix.outputs["Color"], ramp.inputs["Fac"])
    tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    tree.links.new(mix.outputs["Color"], bump.inputs["Height"])
    tree.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    _set_socket(bsdf, "Roughness", 0.40)
    _set_socket(bsdf, "Coat Weight", 0.12)
    _set_socket(bsdf, "Coat Roughness", 0.28)
    return mat


def metal_material(name, color, *, metallic=0.88, roughness=0.26):
    mat, _tree, bsdf = _prepare_material(name)
    color = _clamp_color(color)
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    _set_socket(bsdf, "Metallic", metallic)
    _set_socket(bsdf, "Roughness", roughness)
    return mat


def simple_material(name, color, *, roughness=0.6):
    mat, _tree, bsdf = _prepare_material(name)
    color = _clamp_color(color)
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    _set_socket(bsdf, "Roughness", roughness)
    return mat


def add_cube(collection, name, location, dimensions, mat, bevel=0.03, segments=4):
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


def add_cylinder(
    collection,
    name,
    location,
    radius,
    depth,
    mat,
    *,
    rotation=(0.0, 0.0, 0.0),
    vertices=48,
    bevel=0.01,
):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    move_to_collection(obj, collection)

    if mat is not None:
        obj.data.materials.append(mat)

    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3

    return obj


def add_uv_sphere(collection, name, location, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=40,
        ring_count=24,
        location=location,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_rect_piping(collection, name, center, width, length, z, mat, thickness=0.006):
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

    for point, xyz in zip(
        spline.points,
        ((x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)),
    ):
        point.co = (*xyz, 1.0)

    spline.use_cyclic_u = True

    obj = bpy.data.objects.new(PREFIX + name, curve)
    collection.objects.link(obj)
    curve.materials.append(mat)
    return obj


def add_fabric_plane(
    collection,
    name,
    *,
    width,
    length,
    y_center,
    base_z,
    mat,
    cols=24,
    rows=34,
    side_drop=0.06,
    foot_drop=0.05,
    ripple=0.018,
):
    """Create a soft bedding surface with deterministic natural folds."""
    vertices = []
    faces = []

    y0 = y_center - length / 2
    y1 = y_center + length / 2

    for row in range(rows):
        fy = row / (rows - 1)
        y = y0 + (y1 - y0) * fy

        for col in range(cols):
            fx = col / (cols - 1)
            x = -width / 2 + width * fx

            z = base_z
            z += ripple * math.sin((fx * 5.5 + fy * 0.7) * math.pi)
            z += ripple * 0.55 * math.sin((fy * 8.2 - fx * 1.7) * math.pi)

            edge = abs(x) / (width / 2)
            z -= side_drop * max(0.0, edge - 0.78) ** 1.45

            foot_factor = max(0.0, 0.13 - fy) / 0.13
            z -= foot_drop * foot_factor

            vertices.append((x, y, z))

    for row in range(rows - 1):
        for col in range(cols - 1):
            a = row * cols + col
            b = a + 1
            c = a + cols + 1
            d = a + cols
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new(PREFIX + name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(PREFIX + name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)

    solid = obj.modifiers.new("Fabric thickness", "SOLIDIFY")
    solid.thickness = 0.018
    solid.offset = 0.0

    subdiv = obj.modifiers.new("Fabric smoothing", "SUBSURF")
    subdiv.subdivision_type = "CATMULL_CLARK"
    subdiv.levels = 1
    subdiv.render_levels = 1

    for polygon in obj.data.polygons:
        polygon.use_smooth = True

    return obj


def add_pillow(collection, name, location, dimensions, rotation_z, fabric, piping):
    pillow = add_cube(
        collection,
        name,
        location,
        dimensions,
        fabric,
        bevel=min(dimensions) * 0.32,
        segments=8,
    )
    pillow.rotation_euler[2] = math.radians(rotation_z)

    # Four tiny corner pulls create a more upholstered silhouette.
    x, y, z = location
    sx = dimensions[0] * 0.40
    sy = dimensions[1] * 0.37
    for index, (dx, dy) in enumerate(((-sx, -sy), (sx, -sy), (-sx, sy), (sx, sy))):
        add_uv_sphere(
            collection,
            f"{name}_CornerPull_{index}",
            (x + dx, y + dy, z + dimensions[2] * 0.02),
            (0.025, 0.018, 0.012),
            piping,
        )

    return pillow


def point_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def add_area_light(collection, name, location, energy, size, color, target):
    data = bpy.data.lights.new(PREFIX + name + "_Data", "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color

    light = bpy.data.objects.new(PREFIX + name, data)
    light.location = location
    collection.objects.link(light)
    point_at(light, target)
    return light


collection = ensure_collection()

# ---------------------------------------------------------------------------
# Premium palette and procedural PBR materials
# ---------------------------------------------------------------------------

walnut = wood_material(PREFIX + "Walnut", (0.30, 0.105, 0.035))
walnut_dark = wood_material(PREFIX + "Walnut_Dark", (0.16, 0.045, 0.018))
headboard_fabric = fabric_material(
    PREFIX + "Headboard_Taupe",
    (0.47, 0.38, 0.30),
    roughness=0.86,
    weave_scale=170.0,
    bump_strength=0.16,
)
ivory = fabric_material(
    PREFIX + "Ivory_Linen",
    (0.86, 0.81, 0.72),
    roughness=0.88,
    weave_scale=235.0,
    bump_strength=0.11,
)
ivory_light = fabric_material(
    PREFIX + "Warm_White_Linen",
    (0.96, 0.92, 0.84),
    roughness=0.86,
    weave_scale=240.0,
    bump_strength=0.09,
)
sage = fabric_material(
    PREFIX + "Sage_Duvet",
    (0.19, 0.32, 0.24),
    roughness=0.91,
    weave_scale=190.0,
    bump_strength=0.17,
)
terracotta = fabric_material(
    PREFIX + "Terracotta_Accent",
    (0.55, 0.18, 0.075),
    roughness=0.86,
    weave_scale=205.0,
    bump_strength=0.13,
)
oatmeal = fabric_material(
    PREFIX + "Oatmeal_Rug",
    (0.48, 0.37, 0.26),
    roughness=0.96,
    weave_scale=155.0,
    bump_strength=0.20,
)
mattress_mat = fabric_material(
    PREFIX + "Mattress_Knit",
    (0.82, 0.83, 0.79),
    roughness=0.82,
    weave_scale=285.0,
    bump_strength=0.10,
)
piping_mat = simple_material(PREFIX + "Piping", (0.17, 0.13, 0.10), roughness=0.55)
brass = metal_material(PREFIX + "Brushed_Brass", (0.44, 0.24, 0.065), metallic=0.90, roughness=0.27)
floor_mat = wood_material(PREFIX + "Floor_Wood", (0.24, 0.12, 0.06))


# ---------------------------------------------------------------------------
# Presentation floor and rug
# ---------------------------------------------------------------------------

add_cube(
    collection,
    "Presentation_Floor",
    (0.0, BED_Y - 0.10, -0.035),
    (3.4, 3.65, 0.07),
    floor_mat,
    bevel=0.01,
    segments=2,
)

add_cube(
    collection,
    "Rug",
    (0.0, BED_Y - 0.18, 0.015),
    (1.72, 2.52, 0.035),
    oatmeal,
    bevel=0.025,
    segments=4,
)


# ---------------------------------------------------------------------------
# Single-bed frame: 0.88 x 1.88 m mattress
# ---------------------------------------------------------------------------

add_cube(
    collection,
    "Frame_Base",
    (0.0, BED_Y, 0.30),
    (FRAME_W, FRAME_L, 0.18),
    walnut,
    bevel=0.035,
    segments=5,
)

add_cube(
    collection,
    "Left_Rail",
    (-FRAME_W / 2 + 0.035, BED_Y, 0.42),
    (0.07, FRAME_L - 0.06, 0.21),
    walnut_dark,
    bevel=0.018,
    segments=4,
)
add_cube(
    collection,
    "Right_Rail",
    (FRAME_W / 2 - 0.035, BED_Y, 0.42),
    (0.07, FRAME_L - 0.06, 0.21),
    walnut_dark,
    bevel=0.018,
    segments=4,
)
add_cube(
    collection,
    "Foot_Rail",
    (0.0, BED_Y - FRAME_L / 2 + 0.035, 0.42),
    (FRAME_W, 0.07, 0.22),
    walnut_dark,
    bevel=0.018,
    segments=4,
)

# Slats are visible at the foot/edges where bedding does not completely cover.
for index in range(8):
    offset = -0.78 + index * 0.225
    add_cube(
        collection,
        f"Slat_{index:02d}",
        (0.0, BED_Y + offset, 0.44),
        (0.82, 0.035, 0.035),
        walnut,
        bevel=0.006,
        segments=2,
    )

# Walnut legs with small brass shoes.
for xi, x in enumerate((-0.435, 0.435)):
    for yi, yoff in enumerate((-0.88, 0.88)):
        add_cylinder(
            collection,
            f"Leg_Wood_{xi}_{yi}",
            (x, BED_Y + yoff, 0.16),
            0.045,
            0.27,
            walnut_dark,
            vertices=36,
            bevel=0.008,
        )
        add_cylinder(
            collection,
            f"Leg_Brass_{xi}_{yi}",
            (x, BED_Y + yoff, 0.045),
            0.051,
            0.06,
            brass,
            vertices=36,
            bevel=0.006,
        )


# ---------------------------------------------------------------------------
# Mattress stack, fitted sheet and stitched edges
# ---------------------------------------------------------------------------

add_cube(
    collection,
    "Mattress",
    (0.0, BED_Y - 0.01, 0.59),
    (MATTRESS_W, MATTRESS_L, 0.28),
    mattress_mat,
    bevel=0.07,
    segments=7,
)

add_rect_piping(
    collection,
    "Mattress_Upper_Piping",
    (0.0, BED_Y - 0.01),
    MATTRESS_W - 0.035,
    MATTRESS_L - 0.035,
    0.725,
    piping_mat,
    thickness=0.005,
)
add_rect_piping(
    collection,
    "Mattress_Lower_Piping",
    (0.0, BED_Y - 0.01),
    MATTRESS_W - 0.025,
    MATTRESS_L - 0.025,
    0.465,
    piping_mat,
    thickness=0.004,
)

# Fitted sheet wraps the mattress and gives the bedding a distinct ivory layer.
add_cube(
    collection,
    "Fitted_Sheet",
    (0.0, BED_Y - 0.01, 0.745),
    (MATTRESS_W - 0.018, MATTRESS_L - 0.018, 0.055),
    ivory_light,
    bevel=0.026,
    segments=5,
)


# ---------------------------------------------------------------------------
# Upholstered headboard with vertical channels
# ---------------------------------------------------------------------------

head_y = BED_Y + FRAME_L / 2 + 0.055

add_cube(
    collection,
    "Headboard_Wood_Back",
    (0.0, head_y, 1.20),
    (1.13, 0.11, 1.62),
    walnut_dark,
    bevel=0.045,
    segments=5,
)
add_cube(
    collection,
    "Headboard_Upholstery_Base",
    (0.0, head_y - 0.075, 1.22),
    (1.01, 0.095, 1.43),
    headboard_fabric,
    bevel=0.055,
    segments=7,
)

# Five slim upholstered vertical channels.
panel_width = 0.176
for index in range(5):
    x = -0.352 + index * 0.176
    add_cube(
        collection,
        f"Headboard_Channel_{index}",
        (x, head_y - 0.132, 1.23),
        (panel_width - 0.014, 0.075, 1.28),
        headboard_fabric,
        bevel=0.045,
        segments=6,
    )

# Walnut cap and side posts make the headboard read as furniture, not a block.
add_cube(
    collection,
    "Headboard_Top_Cap",
    (0.0, head_y, 1.98),
    (1.16, 0.14, 0.095),
    walnut,
    bevel=0.026,
    segments=5,
)
for index, x in enumerate((-0.54, 0.54)):
    add_cube(
        collection,
        f"Headboard_Post_{index}",
        (x, head_y, 1.09),
        (0.075, 0.13, 1.72),
        walnut,
        bevel=0.022,
        segments=4,
    )


# ---------------------------------------------------------------------------
# Layered pillows and cushions
# ---------------------------------------------------------------------------

add_pillow(
    collection,
    "Sleeping_Pillow_Back",
    (-0.18, BED_Y + 0.64, 0.91),
    (0.47, 0.40, 0.16),
    -7.0,
    ivory,
    piping_mat,
)
add_pillow(
    collection,
    "Sleeping_Pillow_Front",
    (0.18, BED_Y + 0.60, 0.93),
    (0.47, 0.40, 0.16),
    8.0,
    ivory_light,
    piping_mat,
)

# Decorative square cushion.
accent = add_cube(
    collection,
    "Terracotta_Cushion",
    (0.0, BED_Y + 0.39, 0.96),
    (0.30, 0.21, 0.27),
    terracotta,
    bevel=0.075,
    segments=8,
)
accent.rotation_euler[2] = math.radians(-3.0)

# Slim lumbar cushion in sage for layered hotel-style styling.
lumbar = add_cube(
    collection,
    "Sage_Lumbar_Cushion",
    (0.0, BED_Y + 0.24, 0.89),
    (0.50, 0.17, 0.15),
    sage,
    bevel=0.052,
    segments=7,
)
lumbar.rotation_euler[2] = math.radians(2.0)


# ---------------------------------------------------------------------------
# Top sheet, duvet and foot throw
# ---------------------------------------------------------------------------

# Visible folded top sheet under the duvet.
add_cube(
    collection,
    "Top_Sheet_Fold",
    (0.0, BED_Y + 0.25, 0.81),
    (0.83, 0.22, 0.052),
    ivory_light,
    bevel=0.024,
    segments=5,
)
add_rect_piping(
    collection,
    "Top_Sheet_Stitch",
    (0.0, BED_Y + 0.25),
    0.80,
    0.19,
    0.838,
    ivory,
    thickness=0.003,
)

# Main sage duvet stops below the pillows, exposing the folded ivory sheet.
add_fabric_plane(
    collection,
    "Sage_Duvet",
    width=0.94,
    length=1.42,
    y_center=BED_Y - 0.18,
    base_z=0.825,
    mat=sage,
    cols=28,
    rows=40,
    side_drop=0.085,
    foot_drop=0.075,
    ripple=0.023,
)

# Foot throw adds a second textile layer and color contrast.
add_fabric_plane(
    collection,
    "Terracotta_Foot_Throw",
    width=0.99,
    length=0.42,
    y_center=BED_Y - 0.72,
    base_z=0.875,
    mat=terracotta,
    cols=24,
    rows=16,
    side_drop=0.055,
    foot_drop=0.035,
    ripple=0.017,
)

# Thin brass-toned decorative edge line on the throw.
add_cylinder(
    collection,
    "Foot_Throw_Edge",
    (0.0, BED_Y - 0.925, 0.875),
    0.005,
    0.91,
    brass,
    rotation=(0.0, math.radians(90.0), 0.0),
    vertices=24,
    bevel=0.0015,
)


# ---------------------------------------------------------------------------
# Camera, lighting and visible viewport presentation
# ---------------------------------------------------------------------------

camera_data = bpy.data.cameras.get(PREFIX + "CAMERA") or bpy.data.cameras.new(PREFIX + "CAMERA")
camera = bpy.data.objects.get(PREFIX + "CAMERA")

if camera is None:
    camera = bpy.data.objects.new(PREFIX + "CAMERA", camera_data)
    collection.objects.link(camera)
else:
    move_to_collection(camera, collection)

camera.location = (2.35, BED_Y - 2.65, 1.86)
camera.data.lens = 52
point_at(camera, (0.0, BED_Y + 0.03, 0.92))
bpy.context.scene.camera = camera

target = (0.0, BED_Y - 0.02, 0.88)
add_area_light(
    collection,
    "Key_Light",
    (1.75, BED_Y - 1.55, 2.55),
    850,
    2.5,
    (1.0, 0.82, 0.68),
    target,
)
add_area_light(
    collection,
    "Fill_Light",
    (-1.85, BED_Y - 0.45, 2.05),
    520,
    2.2,
    (0.72, 0.82, 1.0),
    target,
)
add_area_light(
    collection,
    "Headboard_Rim",
    (0.35, BED_Y + 1.80, 2.45),
    620,
    1.8,
    (1.0, 0.64, 0.42),
    (0.0, head_y, 1.30),
)

scene = bpy.context.scene
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100

# Neutral color-management choices keep the palette readable.
try:
    scene.view_settings.look = "AgX - Medium High Contrast"
except Exception:
    pass

window = bpy.context.window
screen = window.screen if window else None
if screen:
    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue

        space = area.spaces.active
        try:
            # Material Preview is fast, shows all node-based textures, and is
            # more deterministic than waiting for a full render in live mode.
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

print(
    "OrdaX premium single bed rebuilt: "
    f"{MATTRESS_W:.2f} x {MATTRESS_L:.2f} m, "
    "procedural walnut/fabric materials, layered bedding."
)
