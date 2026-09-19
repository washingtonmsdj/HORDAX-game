"""Standalone pillow asset factory. Every pillow is exported independently."""

from pathlib import Path
import sys

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import add_soft_pillow, ensure_collection, fabric_material
from part_validation import validate_pillow
from part_io import export_collection


SPECS={
    "pillow_back":{
        "collection":"ORDAX_PART_PILLOW_BACK",
        "name":"PART_Pillow_Back",
        "role":"back_ivory_pillow",
        "dimensions":(0.76,0.38,0.34),
        "color":(0.807,0.738,0.680),
        "roughness":0.89,"weave":240.0,"bump":0.08,
    },
    "pillow_sage":{
        "collection":"ORDAX_PART_PILLOW_SAGE",
        "name":"PART_Pillow_Sage",
        "role":"sage_pillow",
        "dimensions":(0.50,0.30,0.29),
        "color":(0.078,0.069,0.042),
        "roughness":0.92,"weave":205.0,"bump":0.13,
    },
    "pillow_terracotta":{
        "collection":"ORDAX_PART_PILLOW_TERRACOTTA",
        "name":"PART_Pillow_Terracotta",
        "role":"terracotta_pillow",
        "dimensions":(0.50,0.27,0.27),
        "color":(0.305,0.120,0.061),
        "roughness":0.90,"weave":205.0,"bump":0.12,
    },
    "pillow_lumbar":{
        "collection":"ORDAX_PART_PILLOW_LUMBAR",
        "name":"PART_Pillow_Lumbar",
        "role":"sage_lumbar",
        "dimensions":(0.51,0.20,0.18),
        "color":(0.098,0.087,0.051),
        "roughness":0.93,"weave":195.0,"bump":0.15,
    },
}


def build(part_id: str, *, export=True):
    spec=SPECS[part_id]
    col=ensure_collection(spec["collection"],clear=True)
    mat=fabric_material(
        "ORDAX_MAT_"+part_id,
        spec["color"],
        roughness=spec["roughness"],
        weave=spec["weave"],
        bump=spec["bump"],
    )
    obj=add_soft_pillow(
        col,spec["name"],spec["role"],
        (0,0,0),spec["dimensions"],mat,
        rotation_z=0.0,rotation_x=0.0,
    )
    obj["ordax_component"]=part_id
    obj["ordax_object_id"]=f"single_bed_reference:{part_id}:main"
    obj["ordax_contact_width"]=spec["dimensions"][0]
    obj["ordax_contact_depth"]=spec["dimensions"][1]
    obj["ordax_contact_height"]=spec["dimensions"][2]

    report=validate_pillow(col,part_id,expected_dimensions=spec["dimensions"])
    if export:
        export_collection(col,part_id,report,metadata={
            "origin":"geometric_center",
            "dimensions":list(spec["dimensions"]),
            "assembly_policy":"FORBID_INTERSECTION",
            "assembly_anchor":"pillow_center",
        })
    return col,report
