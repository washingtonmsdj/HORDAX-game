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
    "frame_part","headboard_part","mattress_part","pillow_part",
    "part_validation","part_io","common"
):
    sys.modules.pop(name,None)
importlib.invalidate_caches()

import frame_part
import headboard_part
import mattress_part
import pillow_part

REVIEW_SCENE="ORDAX_BED_PARTS_REVIEW"
STAGE_COLLECTION="ORDAX_BED_PARTS_STAGE"

PART_OFFSETS={
    "frame":(-3.3,0.0,0.0),
    "headboard":(-1.6,0.0,0.0),
    "mattress":(0.0,0.0,0.0),
    "pillow_back":(1.35,0.0,0.55),
    "pillow_sage":(2.15,0.0,0.48),
    "pillow_terracotta":(2.90,0.0,0.45),
    "pillow_lumbar":(3.65,0.0,0.38),
}


def build_parts():
    reports={}
    collections={}

    collections["frame"],reports["frame"]=frame_part.build(export=True)
    collections["headboard"],reports["headboard"]=headboard_part.build(export=True)
    collections["mattress"],reports["mattress"]=mattress_part.build(export=True)

    for pid in ("pillow_back","pillow_sage","pillow_terracotta","pillow_lumbar"):
        collections[pid],reports[pid]=pillow_part.build(pid,export=True)

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
    cam.location=(7.8,-9.2,4.4)
    cam.data.lens=58
    from mathutils import Vector
    cam.rotation_euler=(Vector((0.2,0,0.8))-cam.location).to_track_quat("-Z","Y").to_euler()
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
    scene.render.resolution_y=800
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


collections,reports=build_parts()
build_review_scene(collections)

bad={k:v for k,v in reports.items() if not v.get("ok")}
if bad:
    raise RuntimeError("Part workbench validation failed: "+str(bad))

bpy.context.scene["ordax_parts_review_reports"]=str(reports)
print("OrdaX parts exported and review scene ready:", sorted(reports))
