"""Integrated single-bed structure: frame + headboard, exactly four legs.

The two rear legs continue upward as the wooden headboard posts. There are no
extra headboard feet and no separate headboard asset.
"""

from pathlib import Path
import sys
import bpy

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import add_cube, ensure_collection, fabric_material, wood_material
from part_validation import validate_bed_structure
from part_io import export_collection


COLLECTION="ORDAX_PART_BED_STRUCTURE"
PART_ID="bed_structure"

FRAME_W=1.02
FRAME_L=2.02
TOTAL_H=1.56
CHANNELS=7

FRONT_LEG_H=0.42
REAR_POST_H=TOTAL_H
RAIL_Z=0.37


def _tapered_square_post(collection,name,role,location,height,mat,bottom=0.072,top=0.055):
    z0=-height/2
    z1=height/2
    verts=[
        (-bottom/2,-bottom/2,z0),(bottom/2,-bottom/2,z0),
        (bottom/2,bottom/2,z0),(-bottom/2,bottom/2,z0),
        (-top/2,-top/2,z1),(top/2,-top/2,z1),
        (top/2,top/2,z1),(-top/2,top/2,z1),
    ]
    faces=[(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
    mesh=bpy.data.meshes.new(name+"_Mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    obj.location=location
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    bev=obj.modifiers.new("Soft wood edges","BEVEL")
    bev.width=0.009
    bev.segments=4
    obj["ordax_asset"]="single_bed_reference"
    obj["ordax_component"]="bed_structure"
    obj["ordax_role"]=role
    obj["ordax_object_id"]=f"single_bed_reference:bed_structure:{role}:{name}"
    obj["ordax_standard_version"]=1
    obj["ordax_is_bed_leg"]=True
    return obj


def build(*,export=True):
    col=ensure_collection(COLLECTION,clear=True)
    walnut=wood_material("ORDAX_MAT_Walnut")
    dark=wood_material("ORDAX_MAT_Walnut_Dark",(0.075,0.032,0.015))
    taupe=fabric_material(
        "ORDAX_MAT_Headboard_Taupe",
        (0.386,0.301,0.231),
        roughness=0.92,weave=175.0,bump=0.12,
    )

    # FRONT: two short legs.
    for idx,x in enumerate((-0.46,0.46)):
        _tapered_square_post(
            col,f"ORDAX_PART_Bed_FrontLeg_{idx}","front_leg",
            (x,-0.93,FRONT_LEG_H/2),FRONT_LEG_H,dark,
            bottom=0.075,top=0.055,
        )

    # REAR: exactly two legs, each continues upward as the headboard post.
    for idx,x in enumerate((-0.515,0.515)):
        _tapered_square_post(
            col,f"ORDAX_PART_Bed_RearPostLeg_{idx}","rear_post_leg",
            (x,0.985,REAR_POST_H/2),REAR_POST_H,walnut,
            bottom=0.075,top=0.060,
        )

    # Main rigid bed frame.
    add_cube(col,"PART_Bed_Left_Rail","bed_structure","left_rail",
             (-0.48,0,RAIL_Z),(0.06,1.96,0.22),walnut,bevel=0.016)
    add_cube(col,"PART_Bed_Right_Rail","bed_structure","right_rail",
             (0.48,0,RAIL_Z),(0.06,1.96,0.22),walnut,bevel=0.016)
    add_cube(col,"PART_Bed_Foot_Rail","bed_structure","foot_rail",
             (0,-0.975,0.39),(1.02,0.07,0.26),walnut,bevel=0.020)
    add_cube(col,"PART_Bed_Mattress_Support","bed_structure","support",
             (0,0,0.425),(0.90,1.82,0.07),dark,bevel=0.010)

    # Reference proportions: headboard starts behind mattress, not at floor.
    panel_bottom=0.57
    panel_top=1.49
    panel_h=panel_top-panel_bottom
    panel_center=(panel_bottom+panel_top)/2

    add_cube(col,"PART_Bed_Headboard_Back","bed_structure","headboard_back",
             (0,0.995,panel_center),(0.94,0.070,panel_h),
             dark,bevel=0.025,segments=5)

    usable_w=0.89
    channel_w=usable_w/CHANNELS
    for index in range(CHANNELS):
        x=-usable_w/2+channel_w/2+index*channel_w
        panel=add_cube(
            col,f"PART_Bed_Headboard_Channel_{index}",
            "bed_structure","headboard_channel",
            (x,0.952,panel_center),
            (channel_w-0.010,0.082,panel_h-0.025),
            taupe,bevel=0.030,segments=7,
        )
        panel["ordax_channel_index"]=index
        panel["ordax_object_id"]=f"single_bed_reference:bed_structure:headboard_channel:{index}"

    report=validate_bed_structure(
        col,width=1.09,length=FRAME_L,height=TOTAL_H,channels=CHANNELS
    )
    if export:
        export_collection(col,PART_ID,report,metadata={
            "origin":"floor_center",
            "frame_width":FRAME_W,
            "frame_length":FRAME_L,
            "total_height":TOTAL_H,
            "bed_legs":4,
            "rear_legs_are_headboard_posts":True,
            "headboard_channels":CHANNELS,
            "mattress_reference":[0.88,1.88,0.30],
        })
    return col,report


if __name__=="__main__":
    _col,report=build(export=True)
    print("PART_REPORT",report)
