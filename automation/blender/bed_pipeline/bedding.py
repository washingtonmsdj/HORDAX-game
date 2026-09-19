"""Generate top sheet and duvet using Blender Cloth + real colliders."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import bpy

from common import (
    OX, OY, COL_BEDDING, add_cloth_grid, add_collision, cleanup_orphans,
    ensure_collection, fabric_material, mark_build, object_by_role,
    simulate_cloth,
)


def _require(role: str):
    obj = object_by_role(role)
    if obj is None:
        raise RuntimeError(f"bed pipeline dependency missing: {role}")
    return obj


def build():
    # Cloth must be generated only after rigid frame + mattress exist.
    mattress = _require("mattress")
    fitted = _require("fitted_sheet")
    _require("left_rail")
    _require("right_rail")
    _require("foot_rail")

    col = ensure_collection(COL_BEDDING, clear=True)
    cleanup_orphans()

    # Refresh collision physics on contact surfaces.
    for role in ("mattress", "fitted_sheet", "left_rail", "right_rail", "foot_rail", "headboard_channel"):
        if role == "headboard_channel":
            for obj in [o for o in bpy.context.scene.objects if o.get("ordax_role") == role]:
                add_collision(obj, thickness=0.004, friction=14.0)
        else:
            obj = object_by_role(role)
            if obj:
                add_collision(obj, thickness=0.004, friction=14.0)

    ivory = fabric_material("ORDAX_MAT_Ivory_Bright", (0.807, 0.738, 0.680),
                            roughness=0.89, weave=240.0, bump=0.08)
    sage = fabric_material("ORDAX_MAT_Sage_Olive", (0.098, 0.087, 0.051),
                           roughness=0.93, weave=195.0, bump=0.15)

    fitted_top = fitted.location.z + fitted.dimensions.z / 2

    # The sheet is intentionally wider than the mattress, so its edges can fall.
    top_sheet = add_cloth_grid(
        col, "Ivory_Top_Sheet", "top_sheet",
        width=1.26, length=2.02, center_y=OY-0.02,
        z=fitted_top + 0.055, mat=ivory,
        cols=30, rows=44, pin_head=True,
    )
    top_sheet["ordax_reference_layer"] = 1
    simulate_cloth(top_sheet, end_frame=34, thickness=0.004, self_collision=True)
    add_collision(top_sheet, thickness=0.004, friction=12.0)

    # The duvet uses real single-bed duvet width, not mattress width. That gives
    # it enough material to fall down the sides instead of stretching through
    # the mattress.
    duvet = add_cloth_grid(
        col, "Sage_Duvet", "duvet",
        width=1.55, length=2.14, center_y=OY-0.10,
        z=fitted_top + 0.115, mat=sage,
        cols=36, rows=50, pin_head=True,
    )
    duvet["ordax_reference_layer"] = 2
    simulate_cloth(duvet, end_frame=42, thickness=0.005, self_collision=True)

    # The simulation output itself is the final geometry. No manual drape shape
    # is imposed afterward; future references can change dimensions/physics.
    mark_build("bedding")
    return col


if __name__ == "__main__":
    build()
    print("OrdaX bed pipeline: collision-aware bedding built.")
