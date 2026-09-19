"""Build the complete bed through ordered, validated component stages."""

from pathlib import Path
import json
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import bpy

import frame
import mattress
import pillows
import bedding
import validate
from common import (
    OX, OY, COL_PRESENTATION, add_cube, cleanup_orphans, ensure_collection,
    point_at, simple_material, tag,
)


def _presentation():
    col = ensure_collection(COL_PRESENTATION, clear=True)

    floor_mat = simple_material("ORDAX_MAT_Studio_Floor", (0.738, 0.701, 0.658), 0.95)
    floor = add_cube(
        col, "Studio_Floor", "presentation", "studio_floor",
        (OX, OY-0.10, -0.045), (3.8, 4.0, 0.08),
        floor_mat, bevel=0.01, segments=2,
    )

    camera_data = bpy.data.cameras.get("ORDAX_BED_PIPELINE_CAMERA") or bpy.data.cameras.new("ORDAX_BED_PIPELINE_CAMERA")
    camera = bpy.data.objects.get("ORDAX_BED_PIPELINE_CAMERA")
    if camera is None:
        camera = bpy.data.objects.new("ORDAX_BED_PIPELINE_CAMERA", camera_data)
        col.objects.link(camera)
    else:
        for source in list(camera.users_collection):
            source.objects.unlink(camera)
        col.objects.link(camera)
    camera.location = (2.30, OY-2.72, 1.72)
    camera.data.lens = 56
    point_at(camera, (OX, OY+0.02, 0.98))
    tag(camera, "presentation", "camera")
    bpy.context.scene.camera = camera

    def area_light(name, location, energy, size, color, target):
        data = bpy.data.lights.new(name + "_Data", "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        data.color = color
        obj = bpy.data.objects.new(name, data)
        obj.location = location
        col.objects.link(obj)
        point_at(obj, target)
        tag(obj, "presentation", "light")
        return obj

    target = (OX, OY-0.04, 0.95)
    area_light("ORDAX_BED_Key", (1.85, OY-1.80, 2.75), 1050, 2.8, (1.0,0.88,0.78), target)
    area_light("ORDAX_BED_Fill", (-1.85, OY-0.35, 2.30), 620, 2.6, (0.82,0.86,1.0), target)
    area_light("ORDAX_BED_Rim", (0.0, OY+1.75, 2.65), 500, 2.2, (1.0,0.78,0.62), (OX, OY+1.0, 1.45))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 960
    scene.render.resolution_percentage = 100
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except Exception:
        pass
    if scene.world:
        scene.world.use_nodes = True
        background = scene.world.node_tree.nodes.get("Background")
        if background:
            background.inputs["Color"].default_value = (0.738,0.701,0.658,1.0)
            background.inputs["Strength"].default_value = 0.72

    window = bpy.context.window
    screen = window.screen if window else None
    if screen:
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            space = area.spaces.active
            try:
                space.shading.type = "RENDERED"
                space.shading.use_scene_world = True
                space.shading.use_scene_lights = True
                space.shading.show_shadows = True
            except Exception:
                pass
            region = next((r for r in area.regions if r.type == "WINDOW"), None)
            if region:
                try:
                    with bpy.context.temp_override(window=window, area=area, region=region):
                        bpy.ops.view3d.view_camera()
                except RuntimeError:
                    pass


def _purge_legacy_generated_bed():
    """Remove the pre-pipeline generated bed, never unrelated scene content."""
    legacy = bpy.data.collections.get("ORDAX_DETAILED_BED_TEST")
    if legacy is None:
        return 0
    removed = 0
    for obj in list(legacy.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
        removed += 1
    try:
        bpy.data.collections.remove(legacy)
    except Exception:
        pass
    return removed


def build():
    legacy_removed = _purge_legacy_generated_bed()
    bpy.context.scene["ordax_legacy_bed_objects_removed"] = legacy_removed

    # This order is the contract. Each stage can also be run alone.
    frame.build()
    mattress.build()
    pillows.build()
    bedding.build()
    _presentation()
    cleanup_orphans()
    report = validate.validate(raise_on_error=True)
    print("OrdaX modular bed pipeline validation:")
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return report


if __name__ == "__main__":
    build()
