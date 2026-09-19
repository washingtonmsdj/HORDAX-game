"""Machine-readable quality gate for the modular bed pipeline."""

from pathlib import Path
import json
import math
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import bpy

from common import OX, OY, ASSET_ID, dim, object_by_role, objects_by_component, tol, world_bounds


def _error(report, code, detail):
    report["errors"].append({"code": code, "detail": detail})


def _warning(report, code, detail):
    report["warnings"].append({"code": code, "detail": detail})


def _dimension_close(actual, expected):
    return abs(actual - expected) <= expected * tol("dimension_relative")


def validate(*, raise_on_error=True):
    report = {
        "standard": "ordax.blender.asset-generation",
        "asset": ASSET_ID,
        "ok": True,
        "errors": [],
        "warnings": [],
        "metrics": {},
    }

    required = [
        "left_rail", "right_rail", "foot_rail", "mattress", "fitted_sheet",
        "back_ivory_pillow", "sage_pillow", "terracotta_pillow", "sage_lumbar",
        "top_sheet", "duvet",
    ]
    for role in required:
        obj = object_by_role(role)
        if obj is None:
            _error(report, "missing_role", role)

    channels = [o for o in bpy.context.scene.objects
                if o.get("ordax_asset") == ASSET_ID and o.get("ordax_role") == "headboard_channel"]
    report["metrics"]["headboard_channels"] = len(channels)
    if len(channels) != int(dim("headboard_channels")):
        _error(report, "headboard_channel_count", f"expected {int(dim('headboard_channels'))}, got {len(channels)}")

    mattress = object_by_role("mattress")
    fitted = object_by_role("fitted_sheet")
    if mattress:
        report["metrics"]["mattress_dimensions"] = [round(v, 4) for v in mattress.dimensions]
        if not _dimension_close(mattress.dimensions.x, dim("mattress_width")):
            _error(report, "mattress_width", f"{mattress.dimensions.x:.4f}")
        if not _dimension_close(mattress.dimensions.y, dim("mattress_length")):
            _error(report, "mattress_length", f"{mattress.dimensions.y:.4f}")
        if not _dimension_close(mattress.dimensions.z, dim("mattress_height")):
            _error(report, "mattress_height", f"{mattress.dimensions.z:.4f}")

    # Every generated object is tagged and should not carry accidental scale.
    generated = [o for o in bpy.context.scene.objects if o.get("ordax_asset") == ASSET_ID]
    report["metrics"]["generated_objects"] = len(generated)
    for obj in generated:
        if any(abs(float(s) - 1.0) > 0.001 for s in obj.scale):
            # Rotation is allowed, unapplied scale is not.
            _warning(report, "unapplied_scale", obj.name)

    # Protected mattress zone: cloth may fall off the sides/foot, but vertices
    # above the mattress footprint must not penetrate the fitted-sheet surface.
    if fitted:
        fitted_top = fitted.location.z + fitted.dimensions.z/2
        half_w = dim("mattress_width")/2 - 0.025
        half_l = dim("mattress_length")/2 - 0.025
        penetration_limit = fitted_top - tol("penetration_m")
        for role in ("top_sheet", "duvet"):
            cloth = object_by_role(role)
            if cloth and cloth.type == "MESH":
                central = []
                floor_min = 10.0
                for vertex in cloth.data.vertices:
                    p = cloth.matrix_world @ vertex.co
                    floor_min = min(floor_min, p.z)
                    if abs(p.x-OX) <= half_w and abs(p.y-OY) <= half_l:
                        central.append(p.z)
                if central:
                    minimum = min(central)
                    report["metrics"][f"{role}_central_min_z"] = round(minimum, 5)
                    if minimum < penetration_limit:
                        _error(report, "cloth_mattress_penetration",
                               f"{role}: {minimum:.4f} < {penetration_limit:.4f}")
                report["metrics"][f"{role}_min_z"] = round(floor_min, 5)
                if floor_min < -tol("penetration_m"):
                    _error(report, "cloth_floor_penetration", f"{role}: {floor_min:.4f}")

    # Pillow contact gates: pillows can touch bedding but must not sink through
    # mattress or pass through the headboard plane.
    if fitted:
        fitted_top = fitted.location.z + fitted.dimensions.z/2
        head_channels = channels
        head_front_y = min((world_bounds(o)[0].y for o in head_channels), default=OY+0.90)
        for role in ("back_ivory_pillow", "sage_pillow", "terracotta_pillow", "sage_lumbar"):
            obj = object_by_role(role)
            if not obj:
                continue
            lo, hi = world_bounds(obj)
            if lo.z < fitted_top - 0.020:
                _error(report, "pillow_mattress_penetration", f"{role}: min_z={lo.z:.4f}")
            # Camera/front is negative Y; pillow's far/head side is max Y.
            if hi.y > head_front_y + 0.015:
                _error(report, "pillow_headboard_penetration",
                       f"{role}: max_y={hi.y:.4f}, headboard_front={head_front_y:.4f}")

    report["ok"] = not report["errors"]
    bpy.context.scene["ordax_validation_report"] = json.dumps(report, sort_keys=True)
    bpy.context.scene["ordax_validation_ok"] = report["ok"]

    if raise_on_error and not report["ok"]:
        raise RuntimeError("OrdaX asset validation failed: " + json.dumps(report["errors"], ensure_ascii=False))
    return report


if __name__ == "__main__":
    result = validate(raise_on_error=True)
    print(json.dumps(result, indent=2, ensure_ascii=False))
