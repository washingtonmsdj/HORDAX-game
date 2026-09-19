"""Validation primitives used before an individual part can be exported."""

from __future__ import annotations

import json
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def world_bounds(obj):
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners))),
        Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners))),
    )


def _base(part_id: str) -> dict:
    return {
        "part_id": part_id,
        "ok": True,
        "errors": [],
        "warnings": [],
        "metrics": {},
    }


def validate_collection(collection, part_id: str, *, required_roles=(), expected_bounds=None, tolerance=0.025) -> dict:
    report = _base(part_id)
    objects = list(collection.objects)
    report["metrics"]["object_count"] = len(objects)

    roles = [str(obj.get("ordax_role") or "") for obj in objects]
    ids = [str(obj.get("ordax_object_id") or "") for obj in objects]

    for role in required_roles:
        if role not in roles:
            report["errors"].append({"code":"missing_role","detail":role})

    if any(not value for value in ids):
        report["errors"].append({"code":"missing_object_id","detail":"Every exported object requires ordax_object_id"})
    duplicates = sorted({value for value in ids if value and ids.count(value) > 1})
    if duplicates:
        report["errors"].append({"code":"duplicate_object_id","detail":duplicates})

    for obj in objects:
        if any(abs(float(s)-1.0) > 0.001 for s in obj.scale):
            report["errors"].append({"code":"unapplied_scale","detail":obj.name})

    if expected_bounds and objects:
        lows, highs = zip(*(world_bounds(obj) for obj in objects))
        lo = Vector((
            min(v.x for v in lows), min(v.y for v in lows), min(v.z for v in lows)
        ))
        hi = Vector((
            max(v.x for v in highs), max(v.y for v in highs), max(v.z for v in highs)
        ))
        actual = (hi.x-lo.x, hi.y-lo.y, hi.z-lo.z)
        report["metrics"]["bounds"] = [round(v,5) for v in actual]
        names=("x","y","z")
        for idx, expected in enumerate(expected_bounds):
            if expected is None:
                continue
            if abs(actual[idx]-expected) > tolerance:
                report["errors"].append({
                    "code":f"bounds_{names[idx]}",
                    "detail":f"expected {expected:.4f}, got {actual[idx]:.4f}",
                })

    report["ok"] = not report["errors"]
    return report


def validate_headboard(collection, *, width: float, height: float, channels: int) -> dict:
    report = validate_collection(
        collection,
        "headboard",
        required_roles=("headboard_post","headboard_back","headboard_channel"),
        expected_bounds=(width, None, height),
        tolerance=0.035,
    )
    actual_channels = len([o for o in collection.objects if o.get("ordax_role") == "headboard_channel"])
    report["metrics"]["channels"] = actual_channels
    if actual_channels != channels:
        report["errors"].append({"code":"channel_count","detail":f"expected {channels}, got {actual_channels}"})
    report["ok"] = not report["errors"]
    return report


def validate_mattress(collection, *, width: float, length: float, height: float) -> dict:
    report = validate_collection(
        collection,
        "mattress",
        required_roles=("mattress",),
        expected_bounds=(width, length, height),
        tolerance=0.012,
    )
    report["ok"] = not report["errors"]
    return report


def validate_frame(collection, *, width: float, length: float) -> dict:
    report = validate_collection(
        collection,
        "frame",
        required_roles=("left_rail","right_rail","foot_rail","support","leg"),
    )
    report["metrics"]["nominal_width"] = width
    report["metrics"]["nominal_length"] = length
    report["ok"] = not report["errors"]
    return report


def validate_pillow(collection, part_id: str, *, expected_dimensions) -> dict:
    role = {
        "pillow_back": "back_ivory_pillow",
        "pillow_sage": "sage_pillow",
        "pillow_terracotta": "terracotta_pillow",
        "pillow_lumbar": "sage_lumbar",
    }[part_id]
    report = validate_collection(
        collection,
        part_id,
        required_roles=(role,),
        expected_bounds=expected_dimensions,
        tolerance=0.045,
    )
    report["metrics"]["contact_envelope"] = list(expected_dimensions)
    report["ok"] = not report["errors"]
    return report


def mesh_overlap_pairs(obj_a, obj_b) -> int:
    if obj_a.type != "MESH" or obj_b.type != "MESH":
        return 0
    depsgraph = obj_a.evaluated_get.__self__.id_data if False else None
    import bpy
    dg = bpy.context.evaluated_depsgraph_get()
    tree_a = BVHTree.FromObject(obj_a, dg, deform=True, cage=False, epsilon=0.0)
    tree_b = BVHTree.FromObject(obj_b, dg, deform=True, cage=False, epsilon=0.0)
    if tree_a is None or tree_b is None:
        return 0
    return len(tree_a.overlap(tree_b))


def validate_cloth_part(collection, part_id: str, cloth_role: str, proxy) -> dict:
    report = validate_collection(collection, part_id, required_roles=(cloth_role,))
    cloth = next((o for o in collection.objects if o.get("ordax_role") == cloth_role), None)
    if cloth is not None and proxy is not None:
        overlaps = mesh_overlap_pairs(cloth, proxy)
        report["metrics"]["proxy_triangle_overlaps"] = overlaps
        if overlaps:
            report["errors"].append({
                "code":"cloth_proxy_intersection",
                "detail":f"{overlaps} evaluated triangle overlaps",
            })
        lo, hi = world_bounds(cloth)
        report["metrics"]["bounds"] = [
            round(hi.x-lo.x,5),round(hi.y-lo.y,5),round(hi.z-lo.z,5)
        ]
        report["metrics"]["min_z"] = round(lo.z,5)
    report["ok"] = not report["errors"]
    return report
