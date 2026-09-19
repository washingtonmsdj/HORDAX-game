"""Generate isolated top-sheet/duvet parts against bed-faithful collision proxies."""

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
from part_validation import mesh_overlap_pairs, validate_cloth_part
from part_io import export_collection


PROXY_COLLECTION="ORDAX_PART_CLOTH_PROXY"

# All isolated cloth coordinates are mattress-local: Z=0 is the mattress
# bottom. These values mirror bed_structure_part.py without importing/building
# that asset, so the cloth remains an independent export.
MW,ML,MH=0.88,1.88,0.30
FRAME_SUPPORT_TOP_Z=0.46
FRAME_RAIL_Z=0.37-FRAME_SUPPORT_TOP_Z
FRAME_FOOT_Z=0.39-FRAME_SUPPORT_TOP_Z

SPECS={
    "top_sheet":{
        "collection":"ORDAX_PART_TOP_SHEET",
        "role":"top_sheet",
        "name":"PART_Top_Sheet",
        "width":1.24,"length":2.02,"center_y":-0.02,"z":0.37,
        "cols":30,"rows":44,"frames":38,
        "thickness":0.004,"solidify":0.003,
        "color":(0.807,0.738,0.680),
        "roughness":0.91,"weave":235.0,"bump":0.08,
    },
    "duvet":{
        "collection":"ORDAX_PART_DUVET",
        "role":"duvet",
        "name":"PART_Duvet",
        "width":1.52,"length":2.14,"center_y":-0.10,"z":0.42,
        "cols":36,"rows":52,"frames":46,
        "thickness":0.005,"solidify":0.012,
        "color":(0.098,0.087,0.051),
        "roughness":0.94,"weave":190.0,"bump":0.15,
    }
}


def _add_proxy_cube(col,name,role,location,dimensions,*,bevel=0.0,friction=14.0):
    obj=add_cube(
        col,name,"proxy",role,location,dimensions,None,
        bevel=bevel,segments=6 if bevel else 4,
    )
    obj["ordax_proxy_only"]=True
    obj.hide_render=True
    add_collision(obj,thickness=0.004,friction=friction)
    return obj


def _proxy():
    """Create only the rigid surfaces that can influence bedding drape.

    The previous isolated test used a mattress plus a very low safety floor.
    That made the fabric physically valid against the mattress while ignoring
    the rails and footboard it would meet in the real bed. These proxies use
    the same dimensions/placement as bed_structure_part.py, translated into
    mattress-local coordinates. They are hidden and never exported.
    """
    col=ensure_collection(PROXY_COLLECTION,clear=True)

    mattress=_add_proxy_cube(
        col,"PART_Cloth_Proxy_Mattress","proxy_mattress",
        (0,0,MH/2),(MW,ML,MH),bevel=0.06,friction=16.0,
    )

    # Structure immediately outside/below the mattress. The real mattress sits
    # on a support whose top is world Z=.46, therefore rails are shifted by
    # -0.46 here because isolated cloth uses the mattress bottom as Z=0.
    left=_add_proxy_cube(
        col,"PART_Cloth_Proxy_Left_Rail","proxy_left_rail",
        (-0.48,0,FRAME_RAIL_Z),(0.06,1.96,0.22),bevel=0.016,
    )
    right=_add_proxy_cube(
        col,"PART_Cloth_Proxy_Right_Rail","proxy_right_rail",
        (0.48,0,FRAME_RAIL_Z),(0.06,1.96,0.22),bevel=0.016,
    )
    foot=_add_proxy_cube(
        col,"PART_Cloth_Proxy_Foot_Rail","proxy_foot_rail",
        (0,-0.975,FRAME_FOOT_Z),(1.02,0.07,0.26),bevel=0.020,
    )

    # Last-resort runaway guard only. It is intentionally below the frame and
    # does not define the desired drape.
    floor=_add_proxy_cube(
        col,"PART_Cloth_Proxy_Floor","proxy_floor",
        (0,0,-0.32),(3.0,3.2,0.05),friction=12.0,
    )

    return col,{
        "mattress":mattress,
        "left_rail":left,
        "right_rail":right,
        "foot_rail":foot,
        "floor":floor,
    }


def _validate_structure_clearance(report,cloth,proxies):
    overlaps={}
    for role,obj in proxies.items():
        if role=="floor":
            continue
        count=mesh_overlap_pairs(cloth,obj)
        overlaps[role]=count
        if count:
            report["errors"].append({
                "code":"cloth_structure_intersection",
                "detail":f"{role}: {count} evaluated triangle overlaps",
            })
    report["metrics"]["structure_proxy_triangle_overlaps"]=overlaps
    report["metrics"]["structure_proxy_overlap_total"]=sum(overlaps.values())
    report["ok"]=not report["errors"]


def _set_visual_thickness(cloth,thickness):
    modifier=next(
        (item for item in cloth.modifiers
         if item.type=="SOLIDIFY" and item.name=="Fabric thickness"),
        None,
    )
    if modifier is None:
        raise RuntimeError("Cloth simulation did not create Fabric thickness modifier")
    modifier.thickness=float(thickness)
    modifier.offset=0.0


def build(part_id: str,*,export=True):
    if part_id not in SPECS:
        raise ValueError(f"Unsupported cloth part: {part_id}")

    spec=SPECS[part_id]
    _proxy_col,proxies=_proxy()
    col=ensure_collection(spec["collection"],clear=True)

    mat=fabric_material(
        "ORDAX_MAT_"+part_id,spec["color"],
        roughness=spec["roughness"],weave=spec["weave"],bump=spec["bump"]
    )
    cloth=add_cloth_grid(
        col,spec["name"],spec["role"],
        width=spec["width"],length=spec["length"],center_y=spec["center_y"],
        z=spec["z"],mat=mat,cols=spec["cols"],rows=spec["rows"],pin_head=True
    )
    cloth["ordax_component"]=part_id
    cloth["ordax_object_id"]=f"single_bed_reference:{part_id}:main"
    cloth["ordax_proxy_dimensions"]=[MW,ML,MH]
    cloth["ordax_visual_thickness_m"]=spec["solidify"]

    simulate_cloth(
        cloth,end_frame=spec["frames"],
        thickness=spec["thickness"],self_collision=True
    )
    _set_visual_thickness(cloth,spec["solidify"])

    report=validate_cloth_part(
        col,part_id,spec["role"],proxies["mattress"]
    )
    _validate_structure_clearance(report,cloth,proxies)

    if export:
        export_collection(col,part_id,report,metadata={
            "origin":"mattress_bottom_center",
            "mattress_proxy_dimensions":[MW,ML,MH],
            "collision_proxy_roles":[
                "mattress","left_rail","right_rail","foot_rail"
            ],
            "assembly_policy":"FORBID_INTERSECTION",
            "simulation_frames":spec["frames"],
            "visual_thickness_m":spec["solidify"],
        })

    # Proxies stay hidden and are never exported.
    for obj in _proxy_col.objects:
        obj.hide_set(True)
    return col,report


if __name__=="__main__":
    raise RuntimeError("Use top_sheet_part.py or duvet_part.py")
