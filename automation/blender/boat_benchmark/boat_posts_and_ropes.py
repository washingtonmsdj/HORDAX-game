"""Third fidelity pass: bow/stern posts and real rope geometry.

Run after boat_blockout.py and boat_planking.py. Replaces the blockout torus
rope proxies and simple cube posts with tapered handmade posts, multi-turn rope
coils, diagonal securing loops and hanging rope tails.
"""

from __future__ import annotations

import json
import math
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from boat_blockout import _curve_object, _look_at  # noqa: E402


DETAIL_COLLECTION = "ORDAX_BOAT_ROPE_DETAIL"
ARTIFACT_ROOT = ROOT / "Artifacts" / "Blender" / "boat_benchmark"
REPORT_PATH = ARTIFACT_ROOT / "boat_posts_and_ropes_report.json"
PREVIEW_PATH = ARTIFACT_ROOT / "boat_posts_and_ropes_preview.png"


def _collection(name: str):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    return col


def _cleanup() -> None:
    prefixes = (
        "ORDAX_BOAT_Bow_RopeCoil_",
        "ORDAX_BOAT_Stern_RopeCoil_",
        "ORDAX_BOAT_Bow_RopeTail",
        "ORDAX_BOAT_Stern_RopeTail",
        "ORDAX_BOAT_Bow_Post",
        "ORDAX_BOAT_Stern_Post",
        "ORDAX_BOAT_DETAIL_Rope_",
        "ORDAX_BOAT_DETAIL_BowPost",
        "ORDAX_BOAT_DETAIL_SternPost",
    )
    for obj in list(bpy.data.objects):
        if any(obj.name.startswith(prefix) for prefix in prefixes):
            bpy.data.objects.remove(obj, do_unlink=True)

    col = bpy.data.collections.get(DETAIL_COLLECTION)
    if col:
        for obj in list(col.objects):
            if obj.name in bpy.data.objects:
                bpy.data.objects.remove(obj, do_unlink=True)


def _require_base() -> None:
    if bpy.data.objects.get("ORDAX_BOAT_Hull") is None:
        raise RuntimeError("boat blockout is missing")
    if not any(o.get("ordax_role") == "outer_plank" for o in bpy.data.objects):
        raise RuntimeError("boat planking pass is missing")


def _post_mesh(col, name: str, x: float, wood, role: str):
    # Four-corner tapered post. Top corners deliberately end at slightly
    # different heights to avoid the machine-perfect cube silhouette.
    sx = 0.072
    sy = 0.070
    bottom = 0.39
    top_heights = (0.91, 0.925, 0.905, 0.935)
    lower = [
        (x - sx, -sy, bottom),
        (x + sx, -sy, bottom),
        (x + sx,  sy, bottom),
        (x - sx,  sy, bottom),
    ]
    upper = [
        (x - sx * 0.88, -sy * 0.88, top_heights[0]),
        (x + sx * 0.84, -sy * 0.88, top_heights[1]),
        (x + sx * 0.90,  sy * 0.86, top_heights[2]),
        (x - sx * 0.86,  sy * 0.90, top_heights[3]),
    ]
    verts = lower + upper
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(wood)
    obj["ordax_role"] = role

    bevel = obj.modifiers.new("Worn post edges", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 3
    return obj


def _rope_curve(col, name, points, rope, depth=0.016, cyclic=False):
    obj = _curve_object(
        col,
        name,
        points,
        depth,
        rope,
        cyclic=cyclic,
        bevel_resolution=4,
        resolution_u=3,
    )
    obj["ordax_role"] = "rope"
    return obj


def _helix_points(x: float, *, turns: float, phase: float, z0: float, z1: float, radius: float):
    count = max(48, int(turns * 28))
    pts = []
    for i in range(count + 1):
        t = i / count
        angle = phase + turns * math.tau * t
        r = radius * (1.0 + 0.025 * math.sin(angle * 1.7))
        # The post is vertical (Z), so a true wrap must orbit in the X/Y plane.
        # The former implementation varied mostly Y and therefore crossed the
        # post instead of circling it.
        px = x + r * math.sin(angle)
        py = r * math.cos(angle)
        pz = (
            z0
            + (z1 - z0) * t
            + 0.004 * math.sin(angle * 1.35)
        )
        pts.append((px, py, pz))
    return pts


def _loop_points(x: float, phase: float, z_center: float, radius_y: float, radius_z: float):
    pts = []
    samples = 64
    radius_x = radius_y * 0.90
    for i in range(samples):
        a = phase + math.tau * i / samples
        # A securing loop also orbits the vertical post in X/Y, with a small
        # vertical wave to create the crossed hand-tied appearance.
        px = x + radius_x * math.sin(a)
        py = radius_y * math.cos(a)
        pz = (
            z_center
            + radius_z * 0.30 * math.sin(a + 0.55)
            + 0.008 * math.cos(2.0 * a)
        )
        pts.append((px, py, pz))
    return pts


def _tail_points(x: float, side_sign: int):
    pts = []
    samples = 18
    for i in range(samples):
        t = i / (samples - 1)
        px = x + side_sign * (0.015 + 0.025 * t) + 0.008 * math.sin(t * math.tau * 2.4)
        py = 0.105 + 0.025 * math.sin(t * math.tau * 1.6)
        # Gentle hanging curve with uneven end.
        pz = 0.68 - 0.73 * (t ** 1.18) + 0.012 * math.sin(t * math.tau * 3.1)
        pts.append((px, py, pz))
    return pts


def _make_rope_set(col, label: str, x: float, side_sign: int, rope):
    # Two overlapping windings create the thick, hand-wrapped bundle visible
    # in the reference without resorting to torus placeholders.
    coil_a = _rope_curve(
        col,
        f"ORDAX_BOAT_DETAIL_Rope_{label}_CoilA",
        _helix_points(x, turns=4.2, phase=0.0, z0=0.58, z1=0.73, radius=0.124),
        rope,
        depth=0.015,
    )
    coil_b = _rope_curve(
        col,
        f"ORDAX_BOAT_DETAIL_Rope_{label}_CoilB",
        _helix_points(x, turns=3.6, phase=math.pi, z0=0.61, z1=0.76, radius=0.132),
        rope,
        depth=0.014,
    )

    for idx, phase in enumerate((0.25, 1.35), start=1):
        _rope_curve(
            col,
            f"ORDAX_BOAT_DETAIL_Rope_{label}_Loop{idx:02d}",
            _loop_points(x, phase, 0.665 + 0.018 * idx, 0.125, 0.090),
            rope,
            depth=0.015,
            cyclic=True,
        )

    tail = _rope_curve(
        col,
        f"ORDAX_BOAT_DETAIL_Rope_{label}_Tail",
        _tail_points(x, side_sign),
        rope,
        depth=0.014,
    )

    for obj in (coil_a, coil_b, tail):
        obj["ordax_end"] = label.lower()


def _set_camera() -> None:
    scene = bpy.context.scene
    camera = bpy.data.objects.get("ORDAX_BOAT_Camera_3Q")
    if camera is None:
        raise RuntimeError("boat 3/4 camera is missing")
    camera.data.type = "PERSP"
    camera.data.lens = 60
    camera.location = (3.75, -3.95, 2.05)
    _look_at(camera, (0.04, 0.0, 0.09))
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
        "post": sum(o.get("ordax_role") in {"bow_post_detail", "stern_post_detail"} for o in objects),
        "rope": sum(o.get("ordax_role") == "rope" for o in objects),
    }
    errors = []
    if counts["post"] != 2:
        errors.append(f"post: expected 2, got {counts['post']}")
    if counts["rope"] < 10:
        errors.append(f"rope: expected at least 10, got {counts['rope']}")

    report = {
        "benchmark": "wildwoods_boat_faithful_modeling",
        "pass": "boat_posts_and_ropes_pass",
        "ok": not errors,
        "errors": errors,
        "counts": counts,
    }
    ARTIFACT_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
    if errors:
        raise RuntimeError("boat posts/ropes validation failed: " + "; ".join(errors))
    return report


def main():
    _require_base()
    _cleanup()
    col = _collection(DETAIL_COLLECTION)

    wood = bpy.data.materials.get("ORDAX_BOAT_MAT_AgedWood")
    rope = bpy.data.materials.get("ORDAX_BOAT_MAT_Rope")
    if wood is None or rope is None:
        raise RuntimeError("boat materials are missing")

    bow_x = 1.535
    stern_x = -1.535
    _post_mesh(col, "ORDAX_BOAT_DETAIL_BowPost", bow_x, wood, "bow_post_detail")
    _post_mesh(col, "ORDAX_BOAT_DETAIL_SternPost", stern_x, wood, "stern_post_detail")
    _make_rope_set(col, "Bow", bow_x, 1, rope)
    _make_rope_set(col, "Stern", stern_x, -1, rope)

    _set_camera()
    report = _validate()
    bpy.ops.render.render(write_still=True)
    print("BOAT_POSTS_ROPES_REPORT", json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
