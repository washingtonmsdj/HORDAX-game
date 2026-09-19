"""Generate top-sheet/duvet parts against a temporary mattress collision proxy."""

from pathlib import Path
import sys
import bpy

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import (
    add_cloth_grid,add_collision,add_cube,ensure_collection,fabric_material,
    simulate_cloth,
)
from part_validation import validate_cloth_part
from part_io import export_collection


PROXY_COLLECTION="ORDAX_PART_CLOTH_PROXY"
MW,ML,MH=0.88,1.88,0.30

SPECS={
    "top_sheet":{
        "collection":"ORDAX_PART_TOP_SHEET",
        "role":"top_sheet",
        "name":"PART_Top_Sheet",
        "width":1.24,"length":2.00,"z":0.37,
        "cols":30,"rows":44,"frames":38,
        "thickness":0.004,
        "color":(0.807,0.738,0.680),
        "roughness":0.91,"weave":235.0,"bump":0.08,
    },
    "duvet":{
        "collection":"ORDAX_PART_DUVET",
        "role":"duvet",
        "name":"PART_Duvet",
        "width":1.52,"length":2.08,"z":0.42,
        "cols":36,"rows":52,"frames":46,
        "thickness":0.005,
        "color":(0.098,0.087,0.051),
        "roughness":0.94,"weave":190.0,"bump":0.15,
    }
}


def _proxy():
    col=ensure_collection(PROXY_COLLECTION,clear=True)
    proxy_mat=None
    proxy=add_cube(
        col,"PART_Cloth_Proxy_Mattress","proxy","proxy_mattress",
        (0,0,MH/2),(MW,ML,MH),proxy_mat,bevel=0.06,segments=6
    )
    proxy["ordax_proxy_only"]=True
    proxy.hide_render=True
    add_collision(proxy,thickness=0.005,friction=16.0)

    # A low safety plane prevents runaway cloth but is well below mattress top.
    floor=add_cube(
        col,"PART_Cloth_Proxy_Floor","proxy","proxy_floor",
        (0,0,-0.30),(3.0,3.2,0.05),None,bevel=0.0
    )
    floor["ordax_proxy_only"]=True
    floor.hide_render=True
    add_collision(floor,thickness=0.004,friction=12.0)
    return col,proxy


def build(part_id: str,*,export=True):
    spec=SPECS[part_id]
    _proxy_col,proxy=_proxy()
    col=ensure_collection(spec["collection"],clear=True)

    mat=fabric_material(
        "ORDAX_MAT_"+part_id,spec["color"],
        roughness=spec["roughness"],weave=spec["weave"],bump=spec["bump"]
    )
    cloth=add_cloth_grid(
        col,spec["name"],spec["role"],
        width=spec["width"],length=spec["length"],center_y=-0.03,
        z=spec["z"],mat=mat,cols=spec["cols"],rows=spec["rows"],pin_head=True
    )
    cloth["ordax_component"]=part_id
    cloth["ordax_object_id"]=f"single_bed_reference:{part_id}:main"
    cloth["ordax_proxy_dimensions"]=[MW,ML,MH]
    simulate_cloth(
        cloth,end_frame=spec["frames"],
        thickness=spec["thickness"],self_collision=True
    )

    report=validate_cloth_part(col,part_id,spec["role"],proxy)
    if export:
        export_collection(col,part_id,report,metadata={
            "origin":"mattress_bottom_center",
            "mattress_proxy_dimensions":[MW,ML,MH],
            "assembly_policy":"FORBID_INTERSECTION",
            "simulation_frames":spec["frames"],
        })

    # Proxies stay hidden and are never exported.
    for obj in _proxy_col.objects:
        obj.hide_set(True)
    return col,report


if __name__=="__main__":
    raise RuntimeError("Use top_sheet_part.py or duvet_part.py")
