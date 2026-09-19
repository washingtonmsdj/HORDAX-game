"""Standalone mattress only. Bedding is intentionally excluded."""

from pathlib import Path
import sys

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import add_cube, ensure_collection, fabric_material
from part_validation import validate_mattress
from part_io import export_collection


COLLECTION="ORDAX_PART_MATTRESS"
PART_ID="mattress"
WIDTH=0.88
LENGTH=1.88
HEIGHT=0.30


def build(*,export=True):
    col=ensure_collection(COLLECTION,clear=True)
    mat=fabric_material(
        "ORDAX_MAT_Mattress",
        (0.730,0.710,0.660),
        roughness=0.86,weave=260.0,bump=0.08
    )
    obj=add_cube(
        col,"PART_Mattress","mattress","mattress",
        (0,0,HEIGHT/2),(WIDTH,LENGTH,HEIGHT),
        mat,bevel=0.070,segments=7
    )
    obj["ordax_nominal_width"]=WIDTH
    obj["ordax_nominal_length"]=LENGTH
    obj["ordax_nominal_height"]=HEIGHT
    obj["ordax_anchor_bottom_z"]=0.0
    obj["ordax_anchor_top_z"]=HEIGHT

    report=validate_mattress(col,width=WIDTH,length=LENGTH,height=HEIGHT)
    if export:
        export_collection(col,PART_ID,report,metadata={
            "origin":"bottom_center",
            "dimensions":[WIDTH,LENGTH,HEIGHT],
            "assembly_anchor":"mattress_bottom_center",
        })
    return col,report


if __name__=="__main__":
    _col,report=build(export=True)
    print("PART_REPORT",report)
