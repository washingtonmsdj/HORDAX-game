"""Faithful first-pass benchmark for the Wildwoods-style wooden boat reference.

This is a tooling benchmark for OrdaX Blender Live:
- controlled lofted hull from measured stations;
- coherent side/top/front silhouettes;
- structural landmarks (gunwales, ribs, thwarts, floor, posts);
- simple procedural aged-wood look;
- deterministic 3/4 camera for visual comparison.

The script intentionally stops before micro-detail sculpting.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import bpy
from mathutils import Vector


ASSET = "ORDAX_BOAT"
ROOT = Path(__file__).resolve().parents[3]
ARTIFACT_ROOT = ROOT / "Artifacts" / "Blender" / "boat_benchmark"
REPORT_PATH = ARTIFACT_ROOT / "boat_blockout_report.json"
PREVIEW_PATH = ARTIFACT_ROOT / "boat_blockout_preview.png"

# X = boat length, Y = beam, Z = vertical.
# Values were chosen from the supplied orthographic/3-quarter reference.
STATIONS = [
    (-1.60, 0.055, 0.64, -0.02),
    (-1.38, 0.155, 0.565, -0.18),
    (-1.05, 0.285, 0.505, -0.31),
    (-0.62, 0.405, 0.465, -0.39),
    ( 0.00, 0.475, 0.445, -0.43),
    ( 0.62, 0.405, 0.465, -0.39),
    ( 1.05, 0.285, 0.505, -0.31),
    ( 1.38, 0.155, 0.565, -0.18),
    ( 1.60, 0.055, 0.64, -0.02),
]

CROSS_SAMPLES = 17
HULL_THICKNESS = 0.032


def _remove_existing() -> None:
    for obj in list(bpy.data.objects):
        if obj.name.startswith(ASSET):
            bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        if col.name.startswith(ASSET):
            bpy.data.collections.remove(col)


def _collection(name: str):
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


def _aged_wood_material():
    mat = bpy.data.materials.get("ORDAX_BOAT_MAT_AgedWood")
    if mat is None:
        mat = bpy.data.materials.new("ORDAX_BOAT_MAT_AgedWood")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    bump = nt.nodes.new("ShaderNodeBump")
    mapping = nt.nodes.new("ShaderNodeMapping")
    tex = nt.nodes.new("ShaderNodeTexCoord")

    noise.inputs["Scale"].default_value = 4.2
    noise.inputs["Detail"].default_value = 5.0
    noise.inputs["Roughness"].default_value = 0.78
    noise.inputs["Distortion"].default_value = 0.22
    mapping.inputs["Scale"].default_value = (0.65, 5.5, 2.2)

    ramp.color_ramp.elements[0].position = 0.20
    ramp.color_ramp.elements[0].color = (0.018, 0.009, 0.0045, 1)
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = (0.28, 0.115, 0.045, 1)
    mid = ramp.color_ramp.elements.new(0.52)
    mid.color = (0.085, 0.030, 0.012, 1)

    bsdf.inputs["Roughness"].default_value = 0.78
    bsdf.inputs["Metallic"].default_value = 0.0
    bump.inputs["Strength"].default_value = 0.26
    bump.inputs["Distance"].default_value = 0.035

    nt.links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def _rope_material():
    mat = bpy.data.materials.get("ORDAX_BOAT_MAT_Rope")
    if mat is None:
        mat = bpy.data.materials.new("ORDAX_BOAT_MAT_Rope")
    mat.diffuse_color = (0.22, 0.14, 0.075, 1)
    mat.roughness = 0.93
    return mat


def _metal_material():
    mat = bpy.data.materials.get("ORDAX_BOAT_MAT_Rivet")
    if mat is None:
        mat = bpy.data.materials.new("ORDAX_BOAT_MAT_Rivet")
    mat.diffuse_color = (0.16, 0.14, 0.12, 1)
    mat.metallic = 0.68
    mat.roughness = 0.54
    return mat


def _cross_point(station, u: float) -> tuple[float, float, float]:
    x, half_width, sheer_z, keel_z = station
    # U-shaped section: narrow keel, flaring side wall toward the gunwale.
    # Exponent keeps the center deep while the sides rise more sharply.
    z = keel_z + (sheer_z - keel_z) * (abs(u) ** 1.62)
    y = half_width * u
    return (x, y, z)


def _make_hull(col, wood):
    verts = []
    faces = []
    for station in STATIONS:
        for j in range(CROSS_SAMPLES):
            u = -1.0 + 2.0 * j / (CROSS_SAMPLES - 1)
            verts.append(_cross_point(station, u))

    for i in range(len(STATIONS) - 1):
        for j in range(CROSS_SAMPLES - 1):
            a = i * CROSS_SAMPLES + j
            b = a + 1
            c = (i + 1) * CROSS_SAMPLES + j + 1
            d = (i + 1) * CROSS_SAMPLES + j
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new("ORDAX_BOAT_Hull_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    hull = bpy.data.objects.new("ORDAX_BOAT_Hull", mesh)
    col.objects.link(hull)
    hull.data.materials.append(wood)
    hull["ordax_asset"] = "wildwoods_boat_benchmark"
    hull["ordax_role"] = "hull"
    hull["ordax_object_id"] = "wildwoods_boat:hull:main"

    solid = hull.modifiers.new("Hull thickness", "SOLIDIFY")
    solid.thickness = HULL_THICKNESS
    solid.offset = -1.0
    solid.use_even_offset = True

    bevel = hull.modifiers.new("Worn hull edges", "BEVEL")
    bevel.width = 0.008
    bevel.segments = 2
    bevel.limit_method = "ANGLE"

    smooth = hull.modifiers.new("Hull smoothing", "WEIGHTED_NORMAL")
    return hull


def _curve_object(col, name, points, bevel_depth, material, *, cyclic=False):
    curve = bpy.data.curves.new(name + "_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 4
    curve.bevel_depth = bevel_depth
    curve.bevel_resolution = 3
    spline = curve.splines.new("NURBS")
    spline.points.add(len(points) - 1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1.0)
    spline.use_cyclic_u = cyclic
    if len(points) >= 3:
        spline.order_u = min(3, len(points))
        spline.use_endpoint_u = True
    obj = bpy.data.objects.new(name, curve)
    col.objects.link(obj)
    if material:
        curve.materials.append(material)
    return obj


def _interp_station(x: float):
    for a, b in zip(STATIONS[:-1], STATIONS[1:]):
        if a[0] <= x <= b[0]:
            t = (x - a[0]) / (b[0] - a[0])
            return tuple(a[k] * (1.0 - t) + b[k] * t for k in range(4))
    return STATIONS[0] if x < STATIONS[0][0] else STATIONS[-1]


def _make_gunwales(col, wood):
    for side, u in (("Port", -1.0), ("Starboard", 1.0)):
        pts = [_cross_point(s, u) for s in STATIONS]
        obj = _curve_object(
            col,
            f"ORDAX_BOAT_Gunwale_{side}",
            pts,
            0.038,
            wood,
        )
        obj["ordax_role"] = "gunwale"


def _make_plank_lines(col, wood):
    # Raised longitudinal cues approximate the clinker-planked silhouette.
    for side_name, sign in (("Port", -1.0), ("Starboard", 1.0)):
        for index, frac in enumerate((0.30, 0.48, 0.66, 0.83), start=1):
            pts = [_cross_point(s, sign * frac) for s in STATIONS]
            obj = _curve_object(
                col,
                f"ORDAX_BOAT_PlankLine_{side_name}_{index:02d}",
                pts,
                0.012,
                wood,
            )
            obj["ordax_role"] = "plank_seam"


def _cube(col, name, location, scale, material, bevel=0.012):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] / 2, scale[1] / 2, scale[2] / 2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Move from the temporary active scene collection into our collection.
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    col.objects.link(obj)

    if material:
        obj.data.materials.append(material)
    if bevel:
        mod = obj.modifiers.new("Worn edges", "BEVEL")
        mod.width = bevel
        mod.segments = 3
    return obj


def _make_structure(col, wood):
    # Cross ribs follow the hull section at seven interior stations.
    for idx, x in enumerate((-1.22, -0.88, -0.48, 0.0, 0.48, 0.88, 1.22), start=1):
        s = _interp_station(x)
        pts = []
        for j in range(13):
            u = -0.96 + 1.92 * j / 12
            p = _cross_point(s, u)
            pts.append((p[0], p[1], p[2] + 0.030))
        rib = _curve_object(
            col,
            f"ORDAX_BOAT_Rib_{idx:02d}",
            pts,
            0.024,
            wood,
        )
        rib["ordax_role"] = "rib"

    # Three broad thwarts are visually prominent in the supplied top/3-quarter views.
    for idx, x in enumerate((-0.66, 0.0, 0.66), start=1):
        s = _interp_station(x)
        half = s[1] * 0.84
        z = 0.305 if idx != 2 else 0.295
        beam = _cube(
            col,
            f"ORDAX_BOAT_Thwart_{idx:02d}",
            (x, 0, z),
            (0.12, max(0.20, half * 2.0), 0.065),
            wood,
            bevel=0.015,
        )
        beam["ordax_role"] = "thwart"

    # Floor boards run longitudinally through the deep center.
    for idx, y in enumerate((-0.17, -0.055, 0.055, 0.17), start=1):
        board = _cube(
            col,
            f"ORDAX_BOAT_FloorBoard_{idx:02d}",
            (0, y, -0.285),
            (2.18, 0.095, 0.035),
            wood,
            bevel=0.006,
        )
        board["ordax_role"] = "floor_board"


def _make_posts_and_rope_proxy(col, wood, rope):
    for label, x in (("Stern", -1.53), ("Bow", 1.53)):
        post = _cube(
            col,
            f"ORDAX_BOAT_{label}_Post",
            (x, 0, 0.68),
            (0.13, 0.13, 0.48),
            wood,
            bevel=0.02,
        )
        post["ordax_role"] = label.lower() + "_post"

        # Basic coils are enough for silhouette validation; high-detail rope follows later.
        for n in range(4):
            bpy.ops.mesh.primitive_torus_add(
                major_radius=0.105 + n * 0.006,
                minor_radius=0.018,
                major_segments=36,
                minor_segments=8,
                location=(x, 0, 0.66 + n * 0.038),
            )
            torus = bpy.context.object
            torus.name = f"ORDAX_BOAT_{label}_RopeCoil_{n+1:02d}"
            for owner in list(torus.users_collection):
                owner.objects.unlink(torus)
            col.objects.link(torus)
            torus.data.materials.append(rope)
            torus["ordax_role"] = "rope_proxy"

        direction = -1.0 if label == "Stern" else 1.0
        tail = _curve_object(
            col,
            f"ORDAX_BOAT_{label}_RopeTail",
            [
                (x, 0.05, 0.72),
                (x + 0.03 * direction, 0.07, 0.43),
                (x + 0.04 * direction, 0.09, 0.08),
            ],
            0.018,
            rope,
        )
        tail["ordax_role"] = "rope_proxy"


def _make_rivet_landmarks(col, metal):
    # Sparse rivets in blockout: enough to test detail scale without flooding geometry.
    for side_sign in (-1, 1):
        for x in (-1.2, -0.75, -0.3, 0.3, 0.75, 1.2):
            s = _interp_station(x)
            y = side_sign * s[1] * 1.015
            z = s[3] + (s[2] - s[3]) * (0.83 ** 1.62)
            bpy.ops.mesh.primitive_uv_sphere_add(
                segments=12,
                ring_count=6,
                radius=0.017,
                location=(x, y, z),
            )
            rivet = bpy.context.object
            rivet.name = f"ORDAX_BOAT_Rivet_{'L' if side_sign < 0 else 'R'}_{x:+.2f}"
            for owner in list(rivet.users_collection):
                owner.objects.unlink(rivet)
            col.objects.link(rivet)
            rivet.data.materials.append(metal)
            rivet["ordax_role"] = "rivet_proxy"


def _look_at(obj, target=(0, 0, 0.08)):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def _setup_camera_and_light(col):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_PATH)
    scene.render.film_transparent = False

    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.008, 0.008, 0.010, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.23

    bpy.ops.object.camera_add(location=(4.35, -4.55, 2.55))
    cam = bpy.context.object
    cam.name = "ORDAX_BOAT_Camera_3Q"
    cam.data.lens = 58
    _look_at(cam, (0.10, 0, 0.06))
    scene.camera = cam
    for owner in list(cam.users_collection):
        owner.objects.unlink(cam)
    col.objects.link(cam)

    for name, loc, energy, size in (
        ("Key", (1.6, -2.4, 4.2), 950, 4.0),
        ("Fill", (-3.0, -1.2, 2.1), 520, 3.5),
        ("Rim", (1.8, 3.2, 3.3), 760, 3.0),
    ):
        bpy.ops.object.light_add(type="AREA", location=loc)
        light = bpy.context.object
        light.name = f"ORDAX_BOAT_Light_{name}"
        light.data.energy = energy
        light.data.shape = "DISK"
        light.data.size = size
        _look_at(light, (0, 0, 0.05))
        for owner in list(light.users_collection):
            owner.objects.unlink(light)
        col.objects.link(light)

    # Dark neutral ground, close to the concept presentation.
    ground_mat = bpy.data.materials.get("ORDAX_BOAT_MAT_Ground") or bpy.data.materials.new("ORDAX_BOAT_MAT_Ground")
    ground_mat.diffuse_color = (0.012, 0.012, 0.014, 1)
    ground_mat.roughness = 0.72
    ground = _cube(col, "ORDAX_BOAT_Ground", (0, 0, -0.49), (6.8, 5.0, 0.05), ground_mat, bevel=0)
    ground["ordax_role"] = "presentation_ground"

    # Make Blender Live viewport capture use this deterministic camera.
    for window in bpy.context.window_manager.windows:
        screen = window.screen
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            space = area.spaces.active
            if getattr(space, "region_3d", None):
                space.region_3d.view_perspective = "CAMERA"


def _bounds(objects):
    coords = []
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        if obj.type not in {"MESH", "CURVE"}:
            continue
        if obj.get("ordax_role") in {"presentation_ground"}:
            continue
        evaluated = obj.evaluated_get(depsgraph)
        for corner in evaluated.bound_box:
            coords.append(evaluated.matrix_world @ Vector(corner))
    mins = [min(v[i] for v in coords) for i in range(3)]
    maxs = [max(v[i] for v in coords) for i in range(3)]
    dims = [maxs[i] - mins[i] for i in range(3)]
    return mins, maxs, dims


def _validate(col):
    model_objects = [
        obj for obj in col.objects
        if obj.name.startswith("ORDAX_BOAT_")
        and obj.type in {"MESH", "CURVE"}
        and obj.get("ordax_role") != "presentation_ground"
    ]
    mins, maxs, dims = _bounds(model_objects)

    errors = []
    # Allow rope/rivet/post landmarks to extend slightly beyond hull dimensions.
    if not (3.10 <= dims[0] <= 3.50):
        errors.append(f"overall length out of expected range: {dims[0]:.3f}m")
    if not (0.88 <= dims[1] <= 1.10):
        errors.append(f"overall beam out of expected range: {dims[1]:.3f}m")
    if not (0.85 <= dims[2] <= 1.35):
        errors.append(f"overall height out of expected range: {dims[2]:.3f}m")

    hull = bpy.data.objects.get("ORDAX_BOAT_Hull")
    if hull is None:
        errors.append("missing hull")
    if len([o for o in model_objects if o.get("ordax_role") == "rib"]) != 7:
        errors.append("expected 7 ribs")
    if len([o for o in model_objects if o.get("ordax_role") == "thwart"]) != 3:
        errors.append("expected 3 thwarts")

    report = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "pass": "boat_blockout_pass",
        "ok": not errors,
        "errors": errors,
        "bounds_min": [round(v, 5) for v in mins],
        "bounds_max": [round(v, 5) for v in maxs],
        "dimensions": [round(v, 5) for v in dims],
        "object_count": len(model_objects),
        "roles": {
            role: len([o for o in model_objects if o.get("ordax_role") == role])
            for role in sorted({str(o.get("ordax_role")) for o in model_objects if o.get("ordax_role")})
        },
        "target_reference": {
            "length_m": 3.20,
            "beam_m": 0.95,
            "character": "narrow aged wooden workboat; raised bow/stern; clinker-plank cues",
        },
    }
    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    if errors:
        raise RuntimeError("boat blockout validation failed: " + "; ".join(errors))
    return report


def main():
    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    _remove_existing()
    col = _collection("ORDAX_BOAT_BLOCKOUT")

    wood = _aged_wood_material()
    rope = _rope_material()
    metal = _metal_material()

    _make_hull(col, wood)
    _make_gunwales(col, wood)
    _make_plank_lines(col, wood)
    _make_structure(col, wood)
    _make_posts_and_rope_proxy(col, wood, rope)
    _make_rivet_landmarks(col, metal)
    _setup_camera_and_light(col)

    report = _validate(col)

    # Deterministic preview used in addition to the Blender Live viewport capture.
    bpy.context.scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)

    print("BOAT_BLOCKOUT_REPORT", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
