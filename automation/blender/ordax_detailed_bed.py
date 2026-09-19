"""Detailed idempotent bed generation test for OrdaX Blender Live.

Touches only the ORDAX_DETAILED_BED_TEST collection. Safe to run repeatedly
without deleting HORDAX preview/game objects.
"""
from __future__ import annotations

import math

import bpy
from mathutils import Vector


COLLECTION_NAME = "ORDAX_DETAILED_BED_TEST"
PREFIX = "ORDAX_BED_"
BED_Y = -10.0


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


def material(name, color, *, metallic=0.0, roughness=0.5):
    full_name = PREFIX + name
    mat = bpy.data.materials.get(full_name) or bpy.data.materials.new(full_name)
    mat.diffuse_color = (*color, 1.0)
    mat.metallic = metallic
    mat.roughness = roughness
    return mat


def add_cube(collection, name, location, dimensions, mat, bevel=0.06, segments=3):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    if mat:
        obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Edge softness", "BEVEL")
        mod.width = bevel
        mod.segments = segments
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
    bevel=0.02,
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
    if mat:
        obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Edge softness", "BEVEL")
        mod.width = bevel
        mod.segments = 3
    return obj


def add_sphere(collection, name, location, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=48,
        ring_count=24,
        location=location,
    )
    obj = bpy.context.object
    obj.name = PREFIX + name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    if mat:
        obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def add_rect_piping(collection, name, center, width, length, z, mat, thickness=0.025):
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
    coords = ((x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z))
    for point, xyz in zip(spline.points, coords):
        point.co = (*xyz, 1.0)
    spline.use_cyclic_u = True
    obj = bpy.data.objects.new(PREFIX + name, curve)
    collection.objects.link(obj)
    curve.materials.append(mat)
    return obj


def add_blanket(collection, mat):
    cols = 18
    rows = 22
    width = 3.18
    y0 = BED_Y - 2.08
    y1 = BED_Y + 0.95
    vertices = []
    faces = []

    for row in range(rows):
        fy = row / (rows - 1)
        y = y0 + (y1 - y0) * fy
        for col in range(cols):
            fx = col / (cols - 1)
            x = -width / 2 + width * fx

            # Layered soft folds: long ripples plus smaller fabric variation.
            z = 1.47
            z += 0.055 * math.sin((fx * 5.0 + fy * 0.8) * math.pi)
            z += 0.025 * math.sin((fy * 7.0 - fx * 1.5) * math.pi)

            # Slight natural sag toward sides and foot.
            side = abs(x) / (width / 2)
            z -= 0.13 * max(0.0, side - 0.78) ** 1.4
            z -= 0.10 * max(0.0, 0.18 - fy) / 0.18

            vertices.append((x, y, z))

    for row in range(rows - 1):
        for col in range(cols - 1):
            a = row * cols + col
            b = a + 1
            c = a + cols + 1
            d = a + cols
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new(PREFIX + "BlanketMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(PREFIX + "Quilted_Blanket", mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)

    solid = obj.modifiers.new("Fabric thickness", "SOLIDIFY")
    solid.thickness = 0.045
    solid.offset = 0.0

    bevel = obj.modifiers.new("Soft blanket edges", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2

    subdiv = obj.modifiers.new("Soft fabric surface", "SUBSURF")
    subdiv.subdivision_type = "CATMULL_CLARK"
    subdiv.levels = 1
    subdiv.render_levels = 1

    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def add_pillow(collection, name, x, y, z, angle, fabric, piping):
    pillow = add_cube(
        collection,
        name,
        (x, y, z),
        (1.32, 0.64, 0.30),
        fabric,
        bevel=0.20,
        segments=6,
    )
    pillow.rotation_euler[2] = math.radians(angle)

    # Central depression gives a more cushioned silhouette.
    dimple = add_sphere(
        collection,
        name + "_Dimple",
        (x, y - 0.015, z + 0.14),
        (0.16, 0.09, 0.025),
        piping,
    )
    dimple.rotation_euler[2] = math.radians(angle)
    return pillow


def point_camera(camera, point):
    direction = Vector(point) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


collection = ensure_collection()

wood = material("Walnut", (0.16, 0.075, 0.035), roughness=0.34)
wood_dark = material("Walnut_Dark", (0.07, 0.025, 0.012), roughness=0.30)
linen = material("Warm_Linen", (0.78, 0.72, 0.62), roughness=0.72)
linen_light = material("Ivory_Linen", (0.93, 0.90, 0.82), roughness=0.76)
mattress = material("Mattress", (0.91, 0.90, 0.86), roughness=0.62)
piping = material("Piping", (0.35, 0.31, 0.27), roughness=0.55)
blanket_mat = material("Blanket_Blue", (0.16, 0.27, 0.36), roughness=0.82)
metal = material("Brushed_Brass", (0.46, 0.29, 0.09), metallic=0.72, roughness=0.30)
button_mat = material("Tuft_Button", (0.27, 0.22, 0.17), metallic=0.08, roughness=0.48)
rug_mat = material("Rug", (0.30, 0.22, 0.18), roughness=0.92)

# ---- context rug / presentation base ----
add_cube(
    collection,
    "Rug",
    (0.0, BED_Y - 0.12, 0.045),
    (4.65, 6.05, 0.09),
    rug_mat,
    bevel=0.08,
    segments=4,
)

# ---- structural bed frame ----
add_cube(collection, "Frame_Base", (0, BED_Y, 0.58), (3.58, 4.64, 0.34), wood, 0.10, 4)
add_cube(collection, "Left_Side_Rail", (-1.73, BED_Y, 0.80), (0.14, 4.54, 0.44), wood_dark, 0.045, 3)
add_cube(collection, "Right_Side_Rail", (1.73, BED_Y, 0.80), (0.14, 4.54, 0.44), wood_dark, 0.045, 3)
add_cube(collection, "Foot_Rail", (0, BED_Y - 2.22, 0.81), (3.60, 0.18, 0.48), wood_dark, 0.05, 3)

# Visible support slats.
for i, y in enumerate((-1.60, -1.05, -0.50, 0.05, 0.60, 1.15)):
    add_cube(
        collection,
        f"Slat_{i:02d}",
        (0, BED_Y + y, 0.83),
        (3.20, 0.09, 0.10),
        wood,
        0.02,
        2,
    )

# Four tapered-looking legs: brass foot + walnut column.
for ix, x in enumerate((-1.58, 1.58)):
    for iy, yoff in enumerate((-2.05, 2.02)):
        add_cylinder(
            collection,
            f"Leg_Brass_{ix}_{iy}",
            (x, BED_Y + yoff, 0.18),
            0.12,
            0.22,
            metal,
            vertices=40,
            bevel=0.015,
        )
        add_cylinder(
            collection,
            f"Leg_Wood_{ix}_{iy}",
            (x, BED_Y + yoff, 0.39),
            0.105,
            0.36,
            wood_dark,
            vertices=40,
            bevel=0.018,
        )

# ---- mattress system ----
add_cube(
    collection,
    "Mattress_Lower",
    (0, BED_Y - 0.01, 1.05),
    (3.36, 4.34, 0.44),
    mattress,
    0.16,
    6,
)
add_cube(
    collection,
    "Mattress_Topper",
    (0, BED_Y - 0.04, 1.31),
    (3.30, 4.26, 0.20),
    linen_light,
    0.10,
    5,
)
add_rect_piping(collection, "Mattress_Lower_Piping", (0, BED_Y - 0.01), 3.28, 4.26, 1.25, piping, 0.023)
add_rect_piping(collection, "Mattress_Top_Piping", (0, BED_Y - 0.04), 3.20, 4.16, 1.42, piping, 0.020)

# Subtle mattress side stitching bands.
for z in (0.97, 1.12):
    add_rect_piping(collection, f"Mattress_Stitch_{int(z*100)}", (0, BED_Y - 0.01), 3.33, 4.31, z, piping, 0.010)

# ---- upholstered headboard with wood frame ----
head_y = BED_Y + 2.34
add_cube(collection, "Headboard_Back", (0, head_y, 2.18), (3.86, 0.28, 2.65), wood_dark, 0.10, 4)
add_cube(collection, "Headboard_Inset", (0, head_y - 0.18, 2.20), (3.48, 0.22, 2.23), linen, 0.12, 5)

# Wood posts and cap.
add_cube(collection, "Headboard_Left_Post", (-1.86, head_y, 1.92), (0.18, 0.40, 3.00), wood, 0.05, 3)
add_cube(collection, "Headboard_Right_Post", (1.86, head_y, 1.92), (0.18, 0.40, 3.00), wood, 0.05, 3)
add_cube(collection, "Headboard_Top_Cap", (0, head_y, 3.53), (3.90, 0.42, 0.16), wood, 0.05, 3)

# Padded tuft panels.
cols = 4
rows = 3
panel_w = 0.78
panel_h = 0.62
for row in range(rows):
    z = 1.54 + row * 0.66
    for col in range(cols):
        x = -1.20 + col * 0.80
        add_cube(
            collection,
            f"Headboard_Panel_{row}_{col}",
            (x, head_y - 0.34, z),
            (panel_w, 0.16, panel_h),
            linen,
            0.10,
            5,
        )

# Tuft buttons at panel intersections.
for row in range(2):
    z = 1.87 + row * 0.66
    for col in range(3):
        x = -0.80 + col * 0.80
        button = add_sphere(
            collection,
            f"Tuft_Button_{row}_{col}",
            (x, head_y - 0.445, z),
            (0.070, 0.035, 0.070),
            button_mat,
        )
        button.rotation_euler[0] = math.radians(90)

# ---- pillows and layered bedding ----
add_pillow(collection, "Pillow_Back_Left", -0.82, BED_Y + 1.48, 1.68, -7, linen, piping)
add_pillow(collection, "Pillow_Back_Right", 0.82, BED_Y + 1.48, 1.68, 7, linen, piping)
add_pillow(collection, "Pillow_Front_Left", -0.62, BED_Y + 1.02, 1.70, 5, linen_light, piping)
add_pillow(collection, "Pillow_Front_Right", 0.62, BED_Y + 1.02, 1.70, -5, linen_light, piping)

# Decorative lumbar cushion.
add_cube(
    collection,
    "Lumbar_Cushion",
    (0, BED_Y + 0.76, 1.76),
    (1.38, 0.38, 0.34),
    blanket_mat,
    bevel=0.16,
    segments=6,
)
add_rect_piping(collection, "Lumbar_Piping", (0, BED_Y + 0.76), 1.28, 0.30, 1.90, metal, 0.014)

# Folded top sheet under the quilt.
add_cube(
    collection,
    "Top_Sheet_Fold",
    (0, BED_Y + 0.35, 1.47),
    (3.22, 0.48, 0.10),
    linen_light,
    bevel=0.07,
    segments=5,
)
for x in (-1.45, -0.90, -0.30, 0.30, 0.90, 1.45):
    add_cylinder(
        collection,
        f"Sheet_Fold_Ridge_{x:+.2f}",
        (x, BED_Y + 0.12, 1.525),
        0.018,
        0.40,
        piping,
        rotation=(math.radians(90), 0, 0),
        vertices=20,
        bevel=0.006,
    )

add_blanket(collection, blanket_mat)

# Blanket edge piping at foot for a crafted finish.
add_cylinder(
    collection,
    "Blanket_Foot_Edge",
    (0, BED_Y - 2.07, 1.39),
    0.025,
    3.12,
    metal,
    rotation=(0, math.radians(90), 0),
    vertices=32,
    bevel=0.006,
)

# ---- dedicated camera and lighting ----
camera_data = bpy.data.cameras.get(PREFIX + "CAMERA") or bpy.data.cameras.new(PREFIX + "CAMERA")
camera = bpy.data.objects.get(PREFIX + "CAMERA")
if camera is None:
    camera = bpy.data.objects.new(PREFIX + "CAMERA", camera_data)
    collection.objects.link(camera)
elif camera not in collection.objects[:]:
    move_to_collection(camera, collection)

camera.location = (6.85, BED_Y - 7.30, 5.20)
camera.data.lens = 54
point_camera(camera, (0, BED_Y - 0.15, 1.48))
bpy.context.scene.camera = camera

# Soft three-point lighting, all scoped to this test collection.
for name, location, energy, size, color in (
    ("Key_Light", (4.0, BED_Y - 3.0, 6.7), 1050, 4.2, (1.0, 0.82, 0.68)),
    ("Fill_Light", (-4.8, BED_Y - 1.0, 4.6), 700, 3.8, (0.68, 0.80, 1.0)),
    ("Headboard_Light", (0.0, BED_Y + 3.0, 5.5), 850, 3.0, (1.0, 0.72, 0.48)),
):
    light_data = bpy.data.lights.new(PREFIX + name + "_Data", "AREA")
    light_data.energy = energy
    light_data.shape = "DISK"
    light_data.size = size
    light_data.color = color
    light = bpy.data.objects.new(PREFIX + name, light_data)
    light.location = location
    collection.objects.link(light)
    point_camera(light, (0, BED_Y, 1.2))

# Presentation settings.
scene = bpy.context.scene
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
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
            space.shading.cavity_type = "WORLD"
        except Exception:
            pass
        region = next((r for r in area.regions if r.type == "WINDOW"), None)
        if region:
            try:
                with bpy.context.temp_override(window=window, area=area, region=region):
                    bpy.ops.view3d.view_camera()
            except RuntimeError:
                pass

# Select the mattress as a clear focus object.
bpy.ops.object.select_all(action="DESELECT")
focus = bpy.data.objects.get(PREFIX + "Mattress_Topper")
if focus:
    focus.select_set(True)
    bpy.context.view_layer.objects.active = focus

print("OrdaX detailed bed generated in isolated collection.")
