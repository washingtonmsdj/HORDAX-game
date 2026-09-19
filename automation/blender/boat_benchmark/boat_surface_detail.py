"""Fourth fidelity pass: aged surface detail for the wooden boat benchmark.

Adds deterministic high-value visual cues without destructive sculpting:
- wood micro-grain/roughness bump on all boat wood materials;
- rust variation on metal rivets;
- sparse dark cracks/scratches following hull planks;
- moss/algae accents concentrated near lower outer planks;
- preserves the validated silhouette and construction.
"""

from __future__ import annotations

import json
import math
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[3]
ARTIFACT_ROOT = ROOT / "Artifacts" / "Blender" / "boat_benchmark"
REPORT_PATH = ARTIFACT_ROOT / "boat_surface_detail_report.json"
PREVIEW_PATH = ARTIFACT_ROOT / "boat_surface_detail_preview.png"
COLLECTION = "ORDAX_BOAT_SURFACE_DETAIL"


def _collection():
    col = bpy.data.collections.get(COLLECTION)
    if col is None:
        col = bpy.data.collections.new(COLLECTION)
        bpy.context.scene.collection.children.link(col)
    return col


def _cleanup():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("ORDAX_BOAT_SURFACE_"):
            bpy.data.objects.remove(obj, do_unlink=True)
    col = bpy.data.collections.get(COLLECTION)
    if col:
        for obj in list(col.objects):
            if obj.name in bpy.data.objects:
                bpy.data.objects.remove(obj, do_unlink=True)


def _wood_materials():
    mats = []
    for mat in bpy.data.materials:
        if mat.name.startswith("ORDAX_BOAT_MAT_AgedWood") or "Plank" in mat.name:
            mats.append(mat)
    return mats


def _upgrade_wood_material(mat):
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.bl_idname == "ShaderNodeBsdfPrincipled"), None)
    if bsdf is None:
        return False

    tex = nt.nodes.get("ORDAX_Wood_MicroCoord")
    if tex is None:
        tex = nt.nodes.new("ShaderNodeTexCoord")
        tex.name = "ORDAX_Wood_MicroCoord"

    mapping = nt.nodes.get("ORDAX_Wood_MicroMapping")
    if mapping is None:
        mapping = nt.nodes.new("ShaderNodeMapping")
        mapping.name = "ORDAX_Wood_MicroMapping"
    mapping.inputs["Scale"].default_value = (0.8, 13.0, 2.6)

    noise = nt.nodes.get("ORDAX_Wood_MicroNoise")
    if noise is None:
        noise = nt.nodes.new("ShaderNodeTexNoise")
        noise.name = "ORDAX_Wood_MicroNoise"
    noise.inputs["Scale"].default_value = 7.5
    noise.inputs["Detail"].default_value = 7.0
    noise.inputs["Roughness"].default_value = 0.82
    noise.inputs["Distortion"].default_value = 0.32

    bump = nt.nodes.get("ORDAX_Wood_MicroBump")
    if bump is None:
        bump = nt.nodes.new("ShaderNodeBump")
        bump.name = "ORDAX_Wood_MicroBump"
    bump.inputs["Strength"].default_value = 0.22
    bump.inputs["Distance"].default_value = 0.018

    for link in list(nt.links):
        if (
            link.to_node in {mapping, noise, bump}
            or (link.from_node == bump and link.to_node == bsdf)
        ):
            nt.links.remove(link)

    nt.links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    nt.links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    nt.links.new(noise.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = min(
        0.96,
        max(0.68, float(bsdf.inputs["Roughness"].default_value)),
    )
    return True


def _upgrade_metal():
    mat = bpy.data.materials.get("ORDAX_BOAT_MAT_Rivet")
    if mat is None:
        return False
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.bl_idname == "ShaderNodeBsdfPrincipled"), None)
    if bsdf is None:
        return False

    tex = nt.nodes.get("ORDAX_Rivet_Noise")
    if tex is None:
        tex = nt.nodes.new("ShaderNodeTexNoise")
        tex.name = "ORDAX_Rivet_Noise"
    tex.inputs["Scale"].default_value = 18.0
    tex.inputs["Detail"].default_value = 4.0
    tex.inputs["Roughness"].default_value = 0.75

    ramp = nt.nodes.get("ORDAX_Rivet_RustRamp")
    if ramp is None:
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.name = "ORDAX_Rivet_RustRamp"
    ramp.color_ramp.elements[0].color = (0.035, 0.025, 0.020, 1)
    ramp.color_ramp.elements[1].color = (0.26, 0.075, 0.018, 1)

    coord = nt.nodes.get("ORDAX_Rivet_Coord")
    if coord is None:
        coord = nt.nodes.new("ShaderNodeTexCoord")
        coord.name = "ORDAX_Rivet_Coord"

    for link in list(nt.links):
        if link.to_node in {tex, ramp}:
            nt.links.remove(link)

    nt.links.new(coord.outputs["Generated"], tex.inputs["Vector"])
    nt.links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Metallic"].default_value = 0.48
    bsdf.inputs["Roughness"].default_value = 0.66
    return True


def _simple_material(name, color, roughness=0.9):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.roughness = roughness
    return mat


def _polyline(col, name, points, depth, material, role):
    curve = bpy.data.curves.new(name + "_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = depth
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1.0)
    obj = bpy.data.objects.new(name, curve)
    col.objects.link(obj)
    curve.materials.append(material)
    obj["ordax_role"] = role
    return obj


def _outer_plank_candidates():
    return [
        o for o in bpy.data.objects
        if o.get("ordax_role") == "outer_plank" and o.type == "MESH"
    ]


def _make_cracks(col, dark):
    # Deterministic short longitudinal cracks on the exterior. They float only
    # a few millimetres above the plank so silhouette remains unchanged.
    created = 0
    samples = (
        (-1.12, -0.76, 0.31, 1),
        (-0.82, -0.39, 0.49, -1),
        (-0.52, -0.08, 0.67, 1),
        (-0.18, 0.28, 0.82, -1),
        (0.18, 0.61, 0.40, 1),
        (0.62, 1.02, 0.60, -1),
        (0.91, 1.27, 0.76, 1),
    )
    # Use nearest visible outer plank centroids as anchors.
    planks = _outer_plank_candidates()
    for idx, (x0, x1, frac, side) in enumerate(samples, start=1):
        candidates = [
            o for o in planks
            if (1 if o.get("ordax_side") == "starboard" else -1) == side
        ]
        if not candidates:
            continue
        band = max(1, min(6, int(round(frac * 6))))
        plank = next((o for o in candidates if int(o.get("ordax_plank_band", 0)) == band), candidates[0])
        # Approximate surface from object bounding box; small crack is a visual
        # cue rather than structural geometry.
        bb = [plank.matrix_world @ __import__("mathutils").Vector(c) for c in plank.bound_box]
        y = max(p.y for p in bb) if side > 0 else min(p.y for p in bb)
        z = sum(p.z for p in bb) / len(bb)
        pts = []
        for j in range(7):
            t = j / 6
            x = x0 + (x1 - x0) * t
            pts.append((
                x,
                y + side * 0.004,
                z + 0.007 * math.sin(j * 1.35 + idx),
            ))
        _polyline(
            col,
            f"ORDAX_BOAT_SURFACE_Crack_{idx:02d}",
            pts,
            0.0023,
            dark,
            "surface_crack",
        )
        created += 1
    return created


def _make_moss(col, moss):
    created = 0
    for side in (-1, 1):
        for idx, x in enumerate((-1.06, -0.58, -0.12, 0.34, 0.84), start=1):
            # Moss concentrated around lower/middle outer hull as in reference.
            y = side * (0.34 + 0.055 * math.cos(x * 1.7))
            z = -0.04 + 0.035 * math.sin(x * 2.2)
            length = 0.18 + 0.055 * ((idx + (1 if side > 0 else 0)) % 3)
            pts = []
            for j in range(8):
                t = j / 7
                pts.append((
                    x - length * 0.5 + length * t,
                    y + side * 0.004,
                    z + 0.010 * math.sin(t * math.tau * 1.5 + idx),
                ))
            _polyline(
                col,
                f"ORDAX_BOAT_SURFACE_Moss_{'S' if side > 0 else 'P'}_{idx:02d}",
                pts,
                0.006,
                moss,
                "moss",
            )
            created += 1
    return created


def _set_camera():
    scene = bpy.context.scene
    camera = bpy.data.objects.get("ORDAX_BOAT_Camera_3Q")
    if camera is None:
        raise RuntimeError("boat 3/4 camera is missing")
    camera.data.lens = 60
    camera.location = (3.75, -3.95, 2.05)
    direction = __import__("mathutils").Vector((0.04, 0.0, 0.09)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW_PATH)


def main():
    if not any(o.get("ordax_role") == "outer_plank" for o in bpy.data.objects):
        raise RuntimeError("boat planking pass is missing")
    if not any(o.get("ordax_role") == "rope" for o in bpy.data.objects):
        raise RuntimeError("boat posts/ropes pass is missing")

    _cleanup()
    col = _collection()

    wood_count = sum(_upgrade_wood_material(mat) for mat in _wood_materials())
    metal_ok = _upgrade_metal()

    dark = _simple_material("ORDAX_BOAT_MAT_Crack", (0.008, 0.004, 0.002), 0.98)
    moss = _simple_material("ORDAX_BOAT_MAT_Moss", (0.055, 0.075, 0.018), 0.96)
    crack_count = _make_cracks(col, dark)
    moss_count = _make_moss(col, moss)

    report = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "pass": "boat_surface_detail_pass",
        "ok": wood_count >= 5 and crack_count >= 6 and moss_count >= 8,
        "errors": [],
        "counts": {
            "wood_materials_upgraded": wood_count,
            "metal_upgraded": bool(metal_ok),
            "surface_cracks": crack_count,
            "moss_accents": moss_count,
        },
    }
    if not report["ok"]:
        report["errors"].append("surface detail minimum counts were not met")

    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    if not report["ok"]:
        raise RuntimeError("boat surface detail validation failed")

    _set_camera()
    bpy.ops.render.render(write_still=True)
    print("BOAT_SURFACE_DETAIL_REPORT", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
