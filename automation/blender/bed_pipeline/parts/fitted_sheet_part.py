"""Standalone fitted-sheet shell built around mattress dimensions, without mattress export."""

from pathlib import Path
import sys
import bpy

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import ensure_collection, fabric_material, tag
from part_validation import validate_collection
from part_io import export_collection


COLLECTION="ORDAX_PART_FITTED_SHEET"
PART_ID="fitted_sheet"
MW,ML,MH=0.88,1.88,0.30
CLEAR=0.006
SKIRT=0.115


def build(*,export=True):
    col=ensure_collection(COLLECTION,clear=True)
    mat=fabric_material("ORDAX_MAT_Fitted_Sheet",(0.807,0.738,0.680),
                        roughness=0.91,weave=250.0,bump=0.07)

    x0,x1=-(MW/2+CLEAR),(MW/2+CLEAR)
    y0,y1=-(ML/2+CLEAR),(ML/2+CLEAR)
    zt=MH+CLEAR
    zb=MH-SKIRT

    # Open-bottom shell: top + four outside skirts. It wraps around the proxy
    # without occupying the mattress volume.
    verts=[
        (x0,y0,zt),(x1,y0,zt),(x1,y1,zt),(x0,y1,zt),
        (x0,y0,zb),(x1,y0,zb),(x1,y1,zb),(x0,y1,zb),
    ]
    faces=[
        (0,1,2,3),
        (0,4,5,1),
        (1,5,6,2),
        (2,6,7,3),
        (3,7,4,0),
    ]
    mesh=bpy.data.meshes.new("ORDAX_PART_Fitted_Sheet_Mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new("ORDAX_PART_Fitted_Sheet",mesh)
    col.objects.link(obj)
    obj.data.materials.append(mat)
    tag(obj,"fitted_sheet","fitted_sheet")
    obj["ordax_object_id"]="single_bed_reference:fitted_sheet:main"
    obj["ordax_clearance_m"]=CLEAR

    bevel=obj.modifiers.new("Soft fitted corners","BEVEL")
    bevel.width=0.014
    bevel.segments=4
    solid=obj.modifiers.new("Fabric thickness","SOLIDIFY")
    solid.thickness=0.003
    solid.offset=1.0

    report=validate_collection(
        col,PART_ID,required_roles=("fitted_sheet",),
        expected_bounds=(MW+2*CLEAR,ML+2*CLEAR,None),tolerance=0.018
    )
    if export:
        export_collection(col,PART_ID,report,metadata={
            "origin":"mattress_bottom_center",
            "mattress_proxy_dimensions":[MW,ML,MH],
            "clearance_m":CLEAR,
            "assembly_policy":"TOUCH_ONLY",
        })
    return col,report


if __name__=="__main__":
    print("PART_REPORT",build(export=True)[1])
