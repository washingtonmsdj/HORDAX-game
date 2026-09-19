"""Prepare a non-destructive export copy of the validated boat.

Duplicates only boat geometry into ORDAX_BOAT_EXPORT, converts curves to mesh,
applies modifiers on the duplicates, selects the export set, and writes a
triangle/object report. Original modeling objects remain untouched.
"""

from __future__ import annotations

import json
from pathlib import Path

import bpy

ROOT = Path(__file__).resolve().parents[3]
ARTIFACT_ROOT = ROOT / "Artifacts" / "Blender" / "boat_benchmark"
REPORT_PATH = ARTIFACT_ROOT / "boat_export_prepare_report.json"
EXPORT_COLLECTION = "ORDAX_BOAT_EXPORT"


def _cleanup_export_collection():
    col = bpy.data.collections.get(EXPORT_COLLECTION)
    if col is not None:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
    else:
        col = bpy.data.collections.new(EXPORT_COLLECTION)
        bpy.context.scene.collection.children.link(col)
    return col


def _is_source_geometry(obj) -> bool:
    if not obj.name.startswith("ORDAX_BOAT_"):
        return False
    if obj.name.startswith("ORDAX_BOAT_EXPORT_"):
        return False
    if obj.type not in {"MESH", "CURVE"}:
        return False
    if obj.get("ordax_role") == "presentation_ground":
        return False
    return True


def _apply_modifiers(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    for modifier in list(obj.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        except Exception:
            # Export copy is best-effort for nonessential viewport modifiers;
            # validation below still requires resulting mesh geometry.
            pass


def _convert_curve(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    return bpy.context.object


def main():
    sources = [obj for obj in bpy.data.objects if _is_source_geometry(obj)]
    if not sources:
        raise RuntimeError("no boat source geometry found")

    export_col = _cleanup_export_collection()
    bpy.ops.object.select_all(action="DESELECT")

    exported = []
    for index, source in enumerate(sources, start=1):
        duplicate = source.copy()
        duplicate.data = source.data.copy()
        duplicate.animation_data_clear()
        duplicate.name = f"ORDAX_BOAT_EXPORT_{index:03d}_{source.name.removeprefix('ORDAX_BOAT_')}"
        export_col.objects.link(duplicate)

        bpy.context.view_layer.objects.active = duplicate
        duplicate.select_set(True)

        if duplicate.type == "CURVE":
            duplicate = _convert_curve(duplicate)
        if duplicate.type != "MESH":
            duplicate.select_set(False)
            continue

        _apply_modifiers(duplicate)
        duplicate["ordax_export_source"] = source.name
        duplicate["ordax_export_asset"] = "wildwoods_boat_benchmark"
        exported.append(duplicate)
        duplicate.select_set(False)

    if not exported:
        raise RuntimeError("boat export preparation produced no mesh objects")

    # Final deterministic selection consumed by blender.live_export.
    bpy.ops.object.select_all(action="DESELECT")
    triangles = 0
    vertices = 0
    for obj in exported:
        obj.select_set(True)
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        vertices += len(mesh.vertices)
    bpy.context.view_layer.objects.active = exported[0]

    report = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "pass": "boat_prepare_export",
        "ok": True,
        "source_geometry_count": len(sources),
        "export_mesh_count": len(exported),
        "vertices": vertices,
        "triangles": triangles,
        "selected_objects": [obj.name for obj in exported],
    }
    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("BOAT_EXPORT_PREP_REPORT", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
