"""Second fidelity pass for the OrdaX wooden-boat benchmark.

Run after boat_blockout.py. Replaces blockout plank-line cues with actual
overlapping clinker-style plank bands and upgrades the visible construction:
- six outer plank bands per side;
- four inner side plank bands per side;
- seven longitudinal floor boards;
- inner rails and center stems;
- denser rivet layout;
- rope material micro-bump;
- deterministic 3/4 preview and structural report.
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from boat_blockout import (  # noqa: E402
    STATIONS,
    _cross_point,
    _curve_object,
    _cube,
    _interp_station,
    _look_at,
)


DETAIL_COLLECTION = "ORDAX_BOAT_DETAIL"
ARTIFACT_ROOT = ROOT / "Artifacts" / "Blender" / "boat_benchmark"
REPORT_PATH = ARTIFACT_ROOT / "boat_planking_report.json"
PREVIEW_PATH = ARTIFACT_ROOT / "boat_planking_preview.png"


OUTER_BANDS = (
    (0.08, 0.285),
    (0.255, 0.455),
    (0.425, 0.620),
    (0.590, 0.765),
    (0.735, 0.885),
    (0.855, 0.985),
)

INNER_BANDS = (
    (0.18, 0.390),
    (0.360, 0.570),
    (0.540, 0.745),
    (0.715, 0.925),
)


def _collection(name: str):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    return col


def _remove_object(obj) -> None:
    if obj and obj.name in bpy.data.objects:
        bpy.data.objects.remove(obj, do_unlink=True)


def _cleanup_previous_detail() -> None:
    for obj in list(bpy.data.objects):
        if (
            obj.name.startswith("ORDAX_BOAT_DETAIL_")
            or obj.name.startswith("ORDAX_BOAT_PlankLine_")
            or obj.name.startswith("ORDAX_BOAT_FloorBoard_")
            or obj.name.startswith("ORDAX_BOAT_Rivet_")
        ):
            _remove_object(obj)

    col = bpy.data.collections.get(DETAIL_COLLECTION)
    if col is not None:
        for obj in list(col.objects):
            _remove_object(obj)


def _require_blockout() -> None:
    hull = bpy.data.objects.get("ORDAX_BOAT_Hull")
    if hull is None or hull.type != "MESH":
        raise RuntimeError("boat blockout is missing; run boat_blockout.py first")


def _wood_variant(index: int, inside: bool = False):
    base = bpy.data.materials.get("ORDAX_BOAT_MAT_AgedWood")
    if base is None:
        raise RuntimeError("aged wood material from blockout is missing")

    name = f"ORDAX_BOAT_MAT_{'Inner' if inside else 'Outer'}Plank_{index:02d}"
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = base.copy()
        mat.name = name

    if mat.use_nodes and mat.node_tree:
        factor_cycle = (0.78, 0.92, 1.02, 0.86, 1.08, 0.82)
        factor = factor_cycle[(index - 1) % len(factor_cycle)]
        if inside:
            factor *= 0.84

        for node in mat.node_tree.nodes:
            if node.bl_idname != "ShaderNodeValToRGB":
                continue
            for element in node.color_ramp.elements:
                r, g, b, a = element.color
                element.color = (
                    min(1.0, r * factor),
                    min(1.0, g * factor),
                    min(1.0, b * factor),
                    a,
                )
    return mat


def _band_mesh(
    col,
    *,
    name: str,
    side: int,
    lower: float,
    upper: float,
    band_index: int,
    inside: bool,
):
    verts = []
    faces = []

    # Higher clinker bands sit progressively farther out. Inner boards are
    # deliberately inset from the shell so ribs remain visible on top.
    if inside:
        lateral_offset = -side * (0.014 + band_index * 0.0015)
        vertical_offset = 0.006
    else:
        lateral_offset = side * (0.004 + band_index * 0.0030)
        vertical_offset = band_index * 0.0018

    for station_index, station in enumerate(STATIONS):
        for frac_index, frac in enumerate((lower, upper)):
            x, y, z = _cross_point(station, side * frac)

            phase = station_index * 1.73 + band_index * 0.81 + frac_index * 0.33
            # Controlled handmade irregularity: millimetres, not centimeters.
            y += lateral_offset + side * 0.0025 * math.sin(phase)
            z += vertical_offset + 0.0028 * math.sin(phase * 0.79)
            x += 0.0018 * math.sin(phase * 1.23)

            verts.append((x, y, z))

    for station_index in range(len(STATIONS) - 1):
        a = station_index * 2
        b = a + 1
        c = (station_index + 1) * 2 + 1
        d = (station_index + 1) * 2
        if side > 0:
            faces.append((a, b, c, d))
        else:
            faces.append((a, d, c, b))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(_wood_variant(band_index, inside=inside))
    obj["ordax_role"] = "inner_plank" if inside else "outer_plank"
    obj["ordax_plank_band"] = band_index
    obj["ordax_side"] = "starboard" if side > 0 else "port"

    solid = obj.modifiers.new("Plank thickness", "SOLIDIFY")
    solid.thickness = 0.012 if not inside else 0.009
    solid.offset = 0.20 if not inside else -0.15
    solid.use_even_offset = True

    bevel = obj.modifiers.new("Worn plank edges", "BEVEL")
    bevel.width = 0.004
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    return obj


def _make_planks(col) -> None:
    for side in (-1, 1):
        side_name = "Port" if side < 0 else "Starboard"
        for index, (lower, upper) in enumerate(OUTER_BANDS, start=1):
            _band_mesh(
                col,
                name=f"ORDAX_BOAT_DETAIL_OuterPlank_{side_name}_{index:02d}",
                side=side,
                lower=lower,
                upper=upper,
                band_index=index,
                inside=False,
            )

        for index, (lower, upper) in enumerate(INNER_BANDS, start=1):
            _band_mesh(
                col,
                name=f"ORDAX_BOAT_DETAIL_InnerPlank_{side_name}_{index:02d}",
                side=side,
                lower=lower,
                upper=upper,
                band_index=index,
                inside=True,
            )


def _make_floor(col, wood) -> None:
    # Seven narrow boards read much closer to the reference than the original
    # four blockout slabs. The floor is intentionally raised above the keel.
    y_positions = (-0.150, -0.100, -0.050, 0.0, 0.050, 0.100, 0.150)
    for index, y in enumerate(y_positions, start=1):
        board = _cube(
            col,
            f"ORDAX_BOAT_DETAIL_FloorBoard_{index:02d}",
            (0.0, y, -0.060 + 0.002 * math.sin(index * 1.7)),
            (2.24, 0.045, 0.030),
            wood,
            bevel=0.004,
        )
        board["ordax_role"] = "floor_board"
        board["ordax_board_index"] = index


def _make_inner_rails(col, wood) -> None:
    for side, label in ((-1, "Port"), (1, "Starboard")):
        points = []
        for station in STATIONS:
            x, y, z = _cross_point(station, side * 0.915)
            y -= side * 0.030
            z -= 0.018
            points.append((x, y, z))
        rail = _curve_object(
            col,
            f"ORDAX_BOAT_DETAIL_InnerRail_{label}",
            points,
            0.026,
            wood,
            bevel_resolution=0,
            resolution_u=8,
        )
        rail["ordax_role"] = "inner_rail"


def _make_stems(col, wood) -> None:
    for side, label in ((-1, "Stern"), (1, "Bow")):
        x0 = 1.0 if side > 0 else -1.0
        points = [
            (side * 1.525, 0.0, 0.78),
            (side * 1.565, 0.0, 0.61),
            (side * 1.600, 0.0, 0.43),
            (side * 1.605, 0.0, 0.25),
            (side * 1.570, 0.0, 0.10),
            (side * 1.485, 0.0, 0.00),
        ]
        stem = _curve_object(
            col,
            f"ORDAX_BOAT_DETAIL_{label}_Stem",
            points,
            0.034,
            wood,
            bevel_resolution=0,
            resolution_u=3,
        )
        stem["ordax_role"] = "stem"


def _make_rivets(col, metal) -> None:
    # Two staggered rivet rows on each side. This is still deliberately sparse
    # compared with final hero-detail density, but enough to match construction.
    x_values = (-1.30, -1.08, -0.86, -0.64, -0.42, -0.20, 0.02, 0.24, 0.46, 0.68, 0.90, 1.12, 1.32)
    for side in (-1, 1):
        for row_index, frac in enumerate((0.70, 0.885), start=1):
            for rivet_index, x in enumerate(x_values, start=1):
                # Offset alternate row by half a notional spacing.
                local_x = x + (0.055 if row_index == 1 else 0.0)
                if local_x <= STATIONS[0][0] + 0.10 or local_x >= STATIONS[-1][0] - 0.10:
                    continue
                station = _interp_station(local_x)
                px, py, pz = _cross_point(station, side * frac)
                py += side * 0.020
                pz += 0.004

                bpy.ops.mesh.primitive_uv_sphere_add(
                    segments=10,
                    ring_count=6,
                    radius=0.011,
                    location=(px, py, pz),
                )
                rivet = bpy.context.object
                rivet.name = (
                    f"ORDAX_BOAT_DETAIL_Rivet_"
                    f"{'P' if side < 0 else 'S'}_{row_index:02d}_{rivet_index:02d}"
                )
                for owner in list(rivet.users_collection):
                    owner.objects.unlink(rivet)
                col.objects.link(rivet)
                rivet.data.materials.append(metal)
                rivet["ordax_role"] = "rivet"


def _upgrade_rope_material() -> None:
    mat = bpy.data.materials.get("ORDAX_BOAT_MAT_Rope")
    if mat is None:
        return
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf is None:
        return

    # Idempotently add fine fiber variation.
    noise = nt.nodes.get("ORDAX_Rope_Fibers")
    if noise is None:
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.name = "ORDAX_Rope_Fibers"
    noise.inputs["Scale"].default_value = 55.0
    noise.inputs["Detail"].default_value = 3.0
    noise.inputs["Roughness"].default_value = 0.70

    bump = nt.nodes.get("ORDAX_Rope_Bump")
    if bump is None:
        bump = nt.nodes.new("ShaderNodeBump")
        bump.name = "ORDAX_Rope_Bump"
    bump.inputs["Strength"].default_value = 0.28
    bump.inputs["Distance"].default_value = 0.012

    generated = nt.nodes.get("Texture Coordinate")
    if generated is None:
        generated = nt.nodes.new("ShaderNodeTexCoord")

    # Avoid duplicate links by clearing only this tiny branch.
    for link in list(nt.links):
        if link.to_node in {noise, bump} or (
            link.from_node == bump and link.to_node == bsdf
        ):
            nt.links.remove(link)

    nt.links.new(generated.outputs["Generated"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])


def _set_three_quarter_camera() -> None:
    scene = bpy.context.scene
    camera = bpy.data.objects.get("ORDAX_BOAT_Camera_3Q")
    if camera is None:
        raise RuntimeError("3/4 benchmark camera is missing")
    camera.data.type = "PERSP"
    camera.data.lens = 58
    camera.location = (3.65, -3.85, 1.95)
    _look_at(camera, (0.06, 0.0, 0.04))
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_PATH)


def _validate() -> dict:
    objects = list(bpy.data.objects)
    counts = {
        "outer_plank": sum(o.get("ordax_role") == "outer_plank" for o in objects),
        "inner_plank": sum(o.get("ordax_role") == "inner_plank" for o in objects),
        "floor_board": sum(o.get("ordax_role") == "floor_board" for o in objects),
        "inner_rail": sum(o.get("ordax_role") == "inner_rail" for o in objects),
        "stem": sum(o.get("ordax_role") == "stem" for o in objects),
        "rivet": sum(o.get("ordax_role") == "rivet" for o in objects),
    }

    errors = []
    expected = {
        "outer_plank": 12,
        "inner_plank": 8,
        "floor_board": 7,
        "inner_rail": 2,
        "stem": 2,
    }
    for role, required in expected.items():
        if counts[role] != required:
            errors.append(f"{role}: expected {required}, got {counts[role]}")
    if counts["rivet"] < 45:
        errors.append(f"rivet: expected at least 45, got {counts['rivet']}")

    report = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "pass": "boat_planking_pass",
        "ok": not errors,
        "errors": errors,
        "counts": counts,
    }
    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    if errors:
        raise RuntimeError("boat planking validation failed: " + "; ".join(errors))
    return report


def main():
    _require_blockout()
    _cleanup_previous_detail()
    col = _collection(DETAIL_COLLECTION)

    wood = bpy.data.materials.get("ORDAX_BOAT_MAT_AgedWood")
    metal = bpy.data.materials.get("ORDAX_BOAT_MAT_Rivet")
    if wood is None or metal is None:
        raise RuntimeError("blockout materials are missing")

    _make_planks(col)
    _make_floor(col, wood)
    _make_inner_rails(col, wood)
    _make_stems(col, wood)
    _make_rivets(col, metal)
    _upgrade_rope_material()
    _set_three_quarter_camera()

    report = _validate()
    bpy.ops.render.render(write_still=True)
    print("BOAT_PLANKING_REPORT", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
