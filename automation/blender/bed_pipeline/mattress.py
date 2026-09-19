"""Generate only the mattress and fitted sheet, with cloth collision."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from common import (
    OX, OY, COL_MATTRESS, add_collision, add_cube, cleanup_orphans, dim,
    ensure_collection, fabric_material, mark_build,
)


def build():
    col = ensure_collection(COL_MATTRESS, clear=True)
    cleanup_orphans()

    mw = dim("mattress_width")
    ml = dim("mattress_length")
    mh = dim("mattress_height")

    mattress_mat = fabric_material("ORDAX_MAT_Mattress", (0.730, 0.710, 0.660),
                                   roughness=0.86, weave=260.0, bump=0.08)
    ivory = fabric_material("ORDAX_MAT_Ivory_Bright", (0.807, 0.738, 0.680),
                            roughness=0.89, weave=240.0, bump=0.08)

    mattress = add_cube(
        col, "Mattress", "mattress", "mattress",
        (OX, OY-0.005, 0.61),
        (mw, ml, mh),
        mattress_mat,
        bevel=0.070, segments=7,
    )
    mattress["ordax_nominal_width"] = mw
    mattress["ordax_nominal_length"] = ml
    mattress["ordax_nominal_height"] = mh
    add_collision(mattress, thickness=0.005, friction=14.0)

    fitted = add_cube(
        col, "Fitted_Sheet", "mattress", "fitted_sheet",
        (OX, OY-0.005, 0.785),
        (mw-0.010, ml-0.016, 0.050),
        ivory,
        bevel=0.028, segments=6,
    )
    add_collision(fitted, thickness=0.004, friction=16.0)

    mark_build("mattress")
    return col


if __name__ == "__main__":
    build()
    print("OrdaX bed pipeline: mattress built.")
