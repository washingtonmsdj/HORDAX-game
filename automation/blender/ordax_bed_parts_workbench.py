"""Build and export independent bed parts, then show them side-by-side.

No final bed assembly is created here.
"""

from pathlib import Path
import importlib
import sys

import bpy

HERE=Path(__file__).resolve().parent
PIPELINE=HERE/"bed_pipeline"
PARTS=PIPELINE/"parts"
for p in (str(PIPELINE),str(PARTS)):
    if p not in sys.path:
        sys.path.insert(0,p)

for name in (
    "bed_structure_part","mattress_part","pillow_part",
    "fitted_sheet_part","cloth_part",
    "part_validation","part_io","common"
):
    sys.modules.pop(name,None)
importlib.invalidate_caches()

import bed_structure_part
import mattress_part
import pillow_part
import fitted_sheet_part
import cloth_part

REVIEW_SCENE="ORDAX_BED_PARTS_REVIEW"
STAGE_COLLECTION="ORDAX_BED_PARTS_STAGE"

PART_OFFSETS={
    "bed_structure":(-2.8,1.15,0.0),
    "mattress":(-0.75,1.15,0.0),
    "pillow_back":(0.55,1.15,0.72),
    "pillow_left":(1.40,1.15,0.58),
    "pillow_right":(2.15,1.15,0.56),
    "pillow_accent":(2.90,1.15,0.56),
    "pillow_lumbar":(3.70,1.15,0.48),
    "fitted_sheet":(-1.80,-1.45,0.0),
    "top_sheet":(0.15,-1.45,0.0),
    "duvet":(2.25,-1.45,0.0),
}


def _remove_obsolete_source_collections():
    for name in ("ORDAX_PART_FRAME","ORDAX_PART_HEADBOARD","ORDAX_PART_PILLOW_SAGE","ORDAX_PART_PILLOW_TERRACOTTA"):
        col=bpy.data.collections.get(name)
        if col is None:
            continue
        for obj in list(col.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(col)


def build_parts():
    _remove_obsolete_source_collections()
    reports={}
    collections={}

    collections["bed_structure"],reports["bed_structure"]=bed_structure_part.build(export=True)
    collections["mattress"],reports["mattress"]=mattress_part.build(export=True)

    for pid in ("pillow_back","pillow_left","pillow_right","pillow_accent","pillow_lumbar"):
        collections[pid],reports[pid]=pillow_part.build(pid,export=True)

    collections["fitted_sheet"],reports["fitted_sheet"]=fitted_sheet_part.build(export=True)
    collections["top_sheet"],reports["top_sheet"]=cloth_part.build("top_sheet",export=True)
    collections["duvet"],reports["duvet"]=cloth_part.build("duvet",export=True)

    return collections,reports


def _clear_scene(scene):
    for obj in list(scene.objects):
        bpy.data.objects.remove(obj,do_unlink=True)
    for child in list(scene.collection.children):
        scene.collection.children.unlink(child)


def _label(collection,text,location):
    curve=bpy.data.curves.new("ORDAX_STAGE_LABEL_"+text,"FONT")
    curve.body=text.upper().replace("_"," ")
    curve.align_x="CENTER"
    curve.size=0.16
    curve.extrude=0.003
    obj=bpy.data.objects.new("ORDAX_STAGE_LABEL_"+text,curve)
    obj.location=location
    obj.rotation_euler=(1.57079632679,0,0)
    collection.objects.link(obj)


def build_review_scene(collections):
    scene=bpy.data.scenes.get(REVIEW_SCENE) or bpy.data.scenes.new(REVIEW_SCENE)
    _clear_scene(scene)

    stage=bpy.data.collections.new(STAGE_COLLECTION)
    scene.collection.children.link(stage)

    # Collection instances preserve the canonical local coordinates of the source
    # asset while letting us inspect all parts in one review scene.
    for part_id,source in collections.items():
        inst=bpy.data.objects.new("ORDAX_STAGE_"+part_id,None)
        inst.instance_type="COLLECTION"
        inst.instance_collection=source
        inst.location=PART_OFFSETS[part_id]
        stage.objects.link(inst)
        _label(stage,part_id,(PART_OFFSETS[part_id][0],-1.30,0.02))

    # Neutral ground.
    bpy.context.window.scene=scene
    bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,-0.01))
    floor=bpy.context.object
    floor.name="ORDAX_STAGE_FLOOR"
    # Move the operator-created floor into stage.
    for src in list(floor.users_collection):
        src.objects.unlink(floor)
    stage.objects.link(floor)

    mat=bpy.data.materials.get("ORDAX_STAGE_FLOOR_MAT") or bpy.data.materials.new("ORDAX_STAGE_FLOOR_MAT")
    mat.diffuse_color=(0.19,0.19,0.19,1)
    floor.data.materials.append(mat)

    # Camera: front 3/4, wide enough to compare proportions.
    cam_data=bpy.data.cameras.get("ORDAX_STAGE_CAMERA") or bpy.data.cameras.new("ORDAX_STAGE_CAMERA")
    cam=bpy.data.objects.get("ORDAX_STAGE_CAMERA")
    if cam is None:
        cam=bpy.data.objects.new("ORDAX_STAGE_CAMERA",cam_data)
    stage.objects.link(cam)
    cam.location=(8.6,-10.8,5.4)
    cam.data.lens=58
    from mathutils import Vector
    cam.rotation_euler=(Vector((0.2,-0.25,0.75))-cam.location).to_track_quat("-Z","Y").to_euler()
    scene.camera=cam

    # Simple review lighting.
    for idx,(loc,energy,size) in enumerate((((2,-4,6),1200,5.0),((-4,-1,3),650,4.0))):
        data=bpy.data.lights.new(f"ORDAX_STAGE_LIGHT_{idx}_DATA","AREA")
        data.energy=energy
        data.size=size
        light=bpy.data.objects.new(f"ORDAX_STAGE_LIGHT_{idx}",data)
        light.location=loc
        light.rotation_euler=(Vector((0,0,0.7))-light.location).to_track_quat("-Z","Y").to_euler()
        stage.objects.link(light)

    scene.render.engine="BLENDER_EEVEE"
    scene.render.resolution_x=1600
    scene.render.resolution_y=1000
    scene.render.resolution_percentage=100

    window=bpy.context.window
    screen=window.screen if window else None
    if screen:
        for area in screen.areas:
            if area.type!="VIEW_3D":
                continue
            space=area.spaces.active
            try:
                space.shading.type="RENDERED"
                space.shading.use_scene_lights=True
                space.shading.use_scene_world=False
            except Exception:
                pass
            region=next((r for r in area.regions if r.type=="WINDOW"),None)
            if region:
                try:
                    with bpy.context.temp_override(window=window,area=area,region=region):
                        bpy.ops.view3d.view_camera()
                except RuntimeError:
                    pass

    return scene


# Remove obsolete pre-correction artifacts before producing the new review.
from part_io import PARTS_ROOT, REPORTS_ROOT
for stale in (
    "frame.blend","headboard.blend","pillow_sage.blend","pillow_terracotta.blend",
):
    path=PARTS_ROOT/stale
    if path.is_file():
        path.unlink()
    report=REPORTS_ROOT/(Path(stale).stem+".json")
    if report.is_file():
        report.unlink()

collections,reports=build_parts()
build_review_scene(collections)

bad={k:v for k,v in reports.items() if not v.get("ok")}
if bad:
    raise RuntimeError("Part workbench validation failed: "+str(bad))

expected=[f"{part_id}.blend" for part_id in reports]
missing=[name for name in expected if not (PARTS_ROOT/name).is_file()]
if missing:
    raise RuntimeError("Missing exported part artifacts: "+str(missing))

bpy.context.scene["ordax_parts_review_reports"]=str(reports)
bpy.context.scene["ordax_part_artifacts_verified"]=True
bpy.context.scene["ordax_part_artifacts"]=str(sorted(expected))
bpy.context.scene["ordax_part_reports_root"]=str(REPORTS_ROOT)
print("OrdaX parts exported and review scene ready:", sorted(reports))
