"""Reference-faithful thin soft pillow asset factory.

These are not beveled boxes. Each pillow is a sewn-bag mesh with a thin edge,
a gently filled center and compressed corners/perimeter.
"""

from pathlib import Path
import math
import sys
import bpy

HERE=Path(__file__).resolve().parent
PIPELINE=HERE.parent
if str(PIPELINE) not in sys.path:
    sys.path.insert(0,str(PIPELINE))

from common import ensure_collection, fabric_material
from part_validation import validate_pillow
from part_io import export_collection


SPECS={
    "pillow_back":{
        "collection":"ORDAX_PART_PILLOW_BACK",
        "name":"PART_Pillow_Back",
        "role":"back_ivory_pillow",
        "dimensions":(0.72,0.105,0.39),
        "color":(0.807,0.738,0.680),
        "roughness":0.91,"weave":240.0,"bump":0.07,
        "softness":1.00,
    },
    "pillow_left":{
        "collection":"ORDAX_PART_PILLOW_LEFT",
        "name":"PART_Pillow_Left",
        "role":"pillow_left",
        "dimensions":(0.42,0.095,0.32),
        "color":(0.105,0.095,0.060),
        "roughness":0.93,"weave":205.0,"bump":0.11,
        "softness":0.92,
    },
    "pillow_right":{
        "collection":"ORDAX_PART_PILLOW_RIGHT",
        "name":"PART_Pillow_Right",
        "role":"pillow_right",
        "dimensions":(0.40,0.090,0.30),
        "color":(0.105,0.095,0.060),
        "roughness":0.93,"weave":205.0,"bump":0.11,
        "softness":0.92,
    },
    "pillow_accent":{
        "collection":"ORDAX_PART_PILLOW_ACCENT",
        "name":"PART_Pillow_Accent",
        "role":"pillow_accent",
        "dimensions":(0.47,0.095,0.30),
        "color":(0.305,0.120,0.061),
        "roughness":0.91,"weave":205.0,"bump":0.10,
        "softness":0.95,
    },
    "pillow_lumbar":{
        "collection":"ORDAX_PART_PILLOW_LUMBAR",
        "name":"PART_Pillow_Lumbar",
        "role":"pillow_lumbar",
        "dimensions":(0.50,0.075,0.18),
        "color":(0.105,0.095,0.060),
        "roughness":0.93,"weave":200.0,"bump":0.11,
        "softness":0.88,
    },
}


def _soft_pillow_mesh(name,width,depth,height,softness):
    nx,nz=30,22
    verts=[]
    faces=[]

    # Front/back are grids. Edge thickness collapses strongly near the seam;
    # center stays softly filled. Rounded corners are created by shrinking
    # coordinates in the corner zones.
    for side in (-1.0,1.0):
        for iz in range(nz):
            v=-1.0+2.0*iz/(nz-1)
            av=abs(v)
            for ix in range(nx):
                u=-1.0+2.0*ix/(nx-1)
                au=abs(u)

                corner=max(0.0,(au+av-1.42)/0.58)
                corner=min(1.0,corner)
                corner_shrink=1.0-0.13*corner*corner

                x=(width/2)*u*corner_shrink
                z=(height/2)*v*corner_shrink

                edge=max(au,av)
                fill=max(0.0,1.0-edge**2.35)
                half_d=depth*(0.14+0.36*(fill**0.52)*softness)

                # Gentle center compression prevents balloon-like rigidity.
                dimple=0.07*depth*math.exp(-((u/0.44)**2+(v/0.48)**2))
                # Small deterministic wrinkles near the sewn edge.
                wrinkle=(0.010*depth*math.sin(u*math.pi*5.0)*math.sin(v*math.pi*3.0)
                         * (edge**2.0))
                y=side*(half_d-dimple+wrinkle)
                verts.append((x,y,z))

    layer=nx*nz
    for s in range(2):
        base=s*layer
        for iz in range(nz-1):
            for ix in range(nx-1):
                a=base+iz*nx+ix
                b=a+1
                c=a+nx+1
                d=a+nx
                faces.append((a,d,c,b) if s==0 else (a,b,c,d))

    perimeter=[]
    perimeter.extend(range(nx))
    perimeter.extend(iz*nx+(nx-1) for iz in range(1,nz))
    perimeter.extend((nz-1)*nx+ix for ix in range(nx-2,-1,-1))
    perimeter.extend(iz*nx for iz in range(nz-2,0,-1))
    for i,a in enumerate(perimeter):
        b=perimeter[(i+1)%len(perimeter)]
        faces.append((a,b,b+layer,a+layer))

    mesh=bpy.data.meshes.new(name+"_Mesh")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    return mesh


def build(part_id: str,*,export=True):
    spec=SPECS[part_id]
    col=ensure_collection(spec["collection"],clear=True)
    mat=fabric_material(
        "ORDAX_MAT_"+part_id,spec["color"],
        roughness=spec["roughness"],weave=spec["weave"],bump=spec["bump"],
    )
    w,d,h=spec["dimensions"]
    mesh=_soft_pillow_mesh(spec["name"],w,d,h,spec["softness"])
    obj=bpy.data.objects.new(spec["name"],mesh)
    col.objects.link(obj)
    obj.data.materials.append(mat)
    obj["ordax_asset"]="single_bed_reference"
    obj["ordax_component"]=part_id
    obj["ordax_role"]=spec["role"]
    obj["ordax_object_id"]=f"single_bed_reference:{part_id}:main"
    obj["ordax_standard_version"]=1
    obj["ordax_contact_width"]=w
    obj["ordax_contact_depth"]=d
    obj["ordax_contact_height"]=h
    obj["ordax_soft_body_visual"]=True
    obj["ordax_no_intersection"]=True

    sub=obj.modifiers.new("Soft textile surface","SUBSURF")
    sub.levels=2
    sub.render_levels=2
    for poly in mesh.polygons:
        poly.use_smooth=True

    report=validate_pillow(col,part_id,expected_dimensions=spec["dimensions"])
    if export:
        export_collection(col,part_id,report,metadata={
            "origin":"geometric_center",
            "dimensions":list(spec["dimensions"]),
            "mattress_reference_width":0.88,
            "shape":"thin_sewn_soft_bag",
            "assembly_policy":"FORBID_INTERSECTION",
        })
    return col,report
