"""Render deterministic validation views for the OrdaX boat benchmark.

Requires an already-generated boat scene (objects prefixed ORDAX_BOAT_).
Produces 3/4, side, top, front and rear PNGs plus a compact manifest.
"""

from __future__ import annotations

import json
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Artifacts" / "Blender" / "boat_benchmark" / "multiview"
MANIFEST = OUT / "boat_multiview.json"


VIEWS = {
    "three_quarter": ((4.35, -4.55, 2.55), (0.10, 0.0, 0.06), 58),
    "side": ((0.0, -5.2, 0.45), (0.0, 0.0, 0.05), 70),
    "top": ((0.0, 0.0, 6.0), (0.0, 0.0, 0.0), 70),
    "front": ((4.8, 0.0, 0.42), (0.0, 0.0, 0.05), 70),
    "rear": ((-4.8, 0.0, 0.42), (0.0, 0.0, 0.05), 70),
}


def _look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def _camera():
    cam = bpy.data.objects.get("ORDAX_BOAT_ValidationCamera")
    if cam is None:
        data = bpy.data.cameras.new("ORDAX_BOAT_ValidationCamera_Data")
        cam = bpy.data.objects.new("ORDAX_BOAT_ValidationCamera", data)
        bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    return cam


def _ensure_scene():
    hull = bpy.data.objects.get("ORDAX_BOAT_Hull")
    if hull is None:
        raise RuntimeError("ORDAX_BOAT_Hull not found; generate boat blockout first")
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    return scene


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    scene = _ensure_scene()
    cam = _camera()

    rendered = {}
    for name, (location, target, lens) in VIEWS.items():
        cam.location = location
        cam.data.lens = lens
        _look_at(cam, target)
        path = OUT / f"{name}.png"
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        if not path.is_file() or path.stat().st_size == 0:
            raise RuntimeError(f"render failed for view: {name}")
        rendered[name] = {
            "path": str(path),
            "size_bytes": path.stat().st_size,
            "camera_location": list(location),
            "lens_mm": lens,
        }

    manifest = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "views": rendered,
        "required_views": sorted(VIEWS),
        "ok": len(rendered) == len(VIEWS),
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print("BOAT_MULTIVIEW_REPORT", json.dumps(manifest, sort_keys=True))


if __name__ == "__main__":
    main()
