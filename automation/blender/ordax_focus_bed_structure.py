"""Open the isolated bed_structure source in a clean review scene and frame it."""

from pathlib import Path
import sys
import bpy
from mathutils import Vector

HERE=Path(__file__).resolve().parent
PIPELINE=HERE/"bed_pipeline"
PARTS=PIPELINE/"parts"
for p in (str(PIPELINE),str(PARTS)):
    if p not in sys.path:
        sys.path.insert(0,p)

import importlib
for name in ("bed_structure_part","part_validation","part_io","common"):
    sys.modules.pop(name,None)
importlib.invalidate_caches()
import bed_structure_part

SCENE_NAME="ORDAX_BED_STRUCTURE_FOCUS"
scene=bpy.data.scenes.get(SCENE_NAME) or bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene=scene

# Remove only this focus scene's previous children/objects.
for obj in list(scene.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
for child in list(scene.collection.children):
    scene.collection.children.unlink(child)

col,report=bed_structure_part.build(export=False)
# Focus mode must remain visible even when validation fails; the report is
# exposed on the scene so the invalid rule can be fixed without a blank Blender.


# If the generator linked the collection elsewhere, link it to this scene too.
if col.name not in scene.collection.children:
    scene.collection.children.link(col)

# Neutral floor.
bpy.ops.mesh.primitive_plane_add(size=5,location=(0,0,-0.005))
floor=bpy.context.object
floor.name="ORDAX_FOCUS_FLOOR"
mat=bpy.data.materials.get("ORDAX_FOCUS_FLOOR_MAT") or bpy.data.materials.new("ORDAX_FOCUS_FLOOR_MAT")
mat.diffuse_color=(0.18,0.18,0.18,1)
floor.data.materials.append(mat)

cam_data=bpy.data.cameras.get("ORDAX_FOCUS_CAMERA_DATA") or bpy.data.cameras.new("ORDAX_FOCUS_CAMERA_DATA")
cam=bpy.data.objects.get("ORDAX_FOCUS_CAMERA")
if cam is None:
    cam=bpy.data.objects.new("ORDAX_FOCUS_CAMERA",cam_data)
    scene.collection.objects.link(cam)
cam.location=(2.75,-3.45,2.15)
cam.data.lens=58
cam.rotation_euler=(Vector((0.0,0.10,0.78))-cam.location).to_track_quat("-Z","Y").to_euler()
scene.camera=cam

for idx,(loc,energy,size) in enumerate((((2.0,-2.0,3.5),900,3.0),((-2.0,-0.5,2.6),450,2.5))):
    data=bpy.data.lights.get(f"ORDAX_FOCUS_LIGHT_{idx}_DATA") or bpy.data.lights.new(f"ORDAX_FOCUS_LIGHT_{idx}_DATA","AREA")
    data.energy=energy
    data.size=size
    obj=bpy.data.objects.get(f"ORDAX_FOCUS_LIGHT_{idx}")
    if obj is None:
        obj=bpy.data.objects.new(f"ORDAX_FOCUS_LIGHT_{idx}",data)
        scene.collection.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector((0,0,0.75))-obj.location).to_track_quat("-Z","Y").to_euler()

scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=1280
scene.render.resolution_y=900
scene.render.resolution_percentage=100
scene["ordax_focus_part"]="bed_structure"
scene["ordax_focus_report"]=str(report)

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

print("Focused bed_structure ready:", report)
