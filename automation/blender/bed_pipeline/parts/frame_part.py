"""Standalone single-bed frame part. No mattress, headboard or bedding."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
PIPELINE = HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0, str(PIPELINE))

from common import add_cube, add_tapered_leg, ensure_collection, wood_material, tag
from part_validation import validate_frame
from part_io import export_collection


COLLECTION = "ORDAX_PART_FRAME"
PART_ID = "frame"
FRAME_W = 1.02
FRAME_L = 2.02


def build(*, export=True):
    col = ensure_collection(COLLECTION, clear=True)
    walnut = wood_material("ORDAX_MAT_Walnut")
    dark = wood_material("ORDAX_MAT_Walnut_Dark", (0.075, 0.032, 0.015))

    # Bed floor = Z 0. Legs support rails without any mattress/header content.
    for index, (x, y) in enumerate((
        (-0.46,-0.93),(0.46,-0.93),(-0.46,0.93),(0.46,0.93)
    )):
        leg = add_tapered_leg(col, f"PART_Frame_Leg_{index}", (x,y,0.17), 0.34, dark)
        leg["ordax_component"] = "frame"
        leg["ordax_role"] = "leg"
        leg["ordax_object_id"] = f"single_bed_reference:frame:leg:{index}"

    left = add_cube(col,"PART_Frame_Left_Rail","frame","left_rail",
                    (-0.48,0,0.37),(0.06,1.96,0.23),walnut,bevel=0.016)
    right = add_cube(col,"PART_Frame_Right_Rail","frame","right_rail",
                     (0.48,0,0.37),(0.06,1.96,0.23),walnut,bevel=0.016)
    foot = add_cube(col,"PART_Frame_Foot_Rail","frame","foot_rail",
                    (0,-0.975,0.39),(1.02,0.07,0.26),walnut,bevel=0.020)
    support = add_cube(col,"PART_Frame_Support","frame","support",
                       (0,0,0.425),(0.90,1.82,0.07),dark,bevel=0.010)

    for obj in (left,right,foot,support):
        obj["ordax_part_local_space"] = True

    report = validate_frame(col,width=FRAME_W,length=FRAME_L)
    if export:
        export_collection(col, PART_ID, report, metadata={
            "origin":"floor_center",
            "frame_width":FRAME_W,
            "frame_length":FRAME_L,
            "assembly_anchor":"frame_origin",
        })
    return col, report


if __name__ == "__main__":
    _col, report = build(export=True)
    print("PART_REPORT", report)
