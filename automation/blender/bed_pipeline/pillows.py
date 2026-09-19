"""Generate the four reference pillows as independent soft meshes."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from common import (
    OX, OY, COL_PILLOWS, add_soft_pillow, cleanup_orphans,
    ensure_collection, fabric_material, mark_build,
)


def build():
    col = ensure_collection(COL_PILLOWS, clear=True)
    cleanup_orphans()

    ivory = fabric_material("ORDAX_MAT_Ivory_Bright", (0.807, 0.738, 0.680),
                            roughness=0.89, weave=240.0, bump=0.08)
    sage = fabric_material("ORDAX_MAT_Sage_Olive", (0.098, 0.087, 0.051),
                           roughness=0.93, weave=195.0, bump=0.15)
    sage_dark = fabric_material("ORDAX_MAT_Sage_Dark", (0.078, 0.069, 0.042),
                                roughness=0.92, weave=205.0, bump=0.13)
    terra = fabric_material("ORDAX_MAT_Terracotta", (0.305, 0.120, 0.061),
                            roughness=0.90, weave=205.0, bump=0.12)

    add_soft_pillow(col, "Back_Ivory_Pillow", "back_ivory_pillow",
                    (OX, OY+0.66, 1.16), (0.76, 0.38, 0.34),
                    ivory, rotation_x=-10.0)
    add_soft_pillow(col, "Sage_Pillow", "sage_pillow",
                    (OX-0.15, OY+0.49, 1.08), (0.50, 0.30, 0.29),
                    sage_dark, rotation_z=-5.0, rotation_x=-5.0)
    add_soft_pillow(col, "Terracotta_Accent", "terracotta_pillow",
                    (OX+0.06, OY+0.39, 1.09), (0.50, 0.27, 0.27),
                    terra, rotation_z=3.0, rotation_x=-3.0)
    add_soft_pillow(col, "Sage_Lumbar", "sage_lumbar",
                    (OX+0.09, OY+0.25, 1.00), (0.51, 0.20, 0.18),
                    sage, rotation_z=1.0)

    mark_build("pillows")
    return col


if __name__ == "__main__":
    build()
    print("OrdaX bed pipeline: pillows built.")
