"""Standalone upholstered headboard, calibrated lower than the old 2.10 m version."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
PIPELINE = HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0, str(PIPELINE))

from common import add_cube, ensure_collection, fabric_material, wood_material
from part_validation import validate_headboard
from part_io import export_collection


COLLECTION = "ORDAX_PART_HEADBOARD"
PART_ID = "headboard"
WIDTH = 1.08
HEIGHT = 1.72
DEPTH = 0.14
CHANNELS = 8


def build(*, export=True):
    col = ensure_collection(COLLECTION, clear=True)
    walnut = wood_material("ORDAX_MAT_Walnut")
    walnut_dark = wood_material("ORDAX_MAT_Walnut_Dark", (0.075,0.032,0.015))
    taupe = fabric_material(
        "ORDAX_MAT_Headboard_Taupe",
        (0.386,0.301,0.231),
        roughness=0.90,
        weave=180.0,
        bump=0.13,
    )

    # Full part is 1.72 m tall from floor. The upholstery intentionally begins
    # above the lower frame/mattress zone and ends below the wood post tops.
    for index, x in enumerate((-0.515,0.515)):
        post = add_cube(
            col,f"PART_Headboard_Post_{index}","headboard","headboard_post",
            (x,0,HEIGHT/2),(0.05,DEPTH,HEIGHT),walnut,bevel=0.018,segments=5
        )
        post["ordax_object_id"] = f"single_bed_reference:headboard:post:{index}"

    panel_bottom = 0.43
    panel_top = 1.64
    panel_h = panel_top-panel_bottom
    panel_center = (panel_top+panel_bottom)/2

    back = add_cube(
        col,"PART_Headboard_Back","headboard","headboard_back",
        (0,0.018,panel_center),(0.96,0.085,panel_h),
        walnut_dark,bevel=0.032,segments=5
    )

    usable_w=0.90
    channel_w=usable_w/CHANNELS
    for index in range(CHANNELS):
        x=-usable_w/2+channel_w/2+index*channel_w
        panel=add_cube(
            col,f"PART_Headboard_Channel_{index}","headboard","headboard_channel",
            (x,-0.040,panel_center),
            (channel_w-0.009,0.095,panel_h-0.035),
            taupe,bevel=0.038,segments=7
        )
        panel["ordax_channel_index"]=index
        panel["ordax_object_id"]=f"single_bed_reference:headboard:channel:{index}"

    report=validate_headboard(col,width=WIDTH,height=HEIGHT,channels=CHANNELS)
    if export:
        export_collection(col,PART_ID,report,metadata={
            "origin":"floor_center",
            "width":WIDTH,
            "height":HEIGHT,
            "depth":DEPTH,
            "channels":CHANNELS,
            "reference_change":"lowered_from_2.10m_to_1.72m",
            "assembly_anchor":"headboard_floor_center",
        })
    return col,report


if __name__=="__main__":
    _col,report=build(export=True)
    print("PART_REPORT",report)
