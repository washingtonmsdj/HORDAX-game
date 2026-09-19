"""Create a non-destructive side-by-side component review stage.

The assembled asset is not moved. This script creates linked preview copies in
ORDAX_BED_COMPONENT_STAGE so frame, mattress, pillows and bedding can be
reviewed independently before final assembly.
"""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import bpy

from common import ASSET_ID, ensure_collection, objects_by_component, point_at, tag


STAGE_COLLECTION = "ORDAX_BED_COMPONENT_STAGE"
OFFSETS = {
    "frame": (-3.0, 0.0, 0.0),
    "mattress": (-1.0, 0.0, 0.0),
    "pillows": (1.0, 0.0, 0.0),
    "bedding": (3.0, 0.0, 0.0),
}


def build():
    stage = ensure_collection(STAGE_COLLECTION, clear=True)

    for component, offset in OFFSETS.items():
        for source in objects_by_component(component):
            if source.type not in {"MESH", "CURVE"}:
                continue
            copy = source.copy()
            copy.data = source.data
            copy.name = f"ORDAX_STAGE_{component}_{source.name}"
            copy.location = source.location
            copy.location.x += offset[0]
            copy.location.y += offset[1] + 13.0
            copy.location.z += offset[2]
            # Preview copies are deliberately outside the validated asset
            # namespace so they cannot duplicate roles/object IDs.
            for key in ("ordax_asset", "ordax_component", "ordax_role", "ordax_object_id", "ordax_standard_version"):
                if key in copy:
                    del copy[key]
            copy["ordax_stage_source"] = source.name
            copy["ordax_stage_component"] = component
            stage.objects.link(copy)

    cam_data = bpy.data.cameras.get("ORDAX_STAGE_CAMERA") or bpy.data.cameras.new("ORDAX_STAGE_CAMERA")
    cam = bpy.data.objects.get("ORDAX_STAGE_CAMERA")
    if cam is None:
        cam = bpy.data.objects.new("ORDAX_STAGE_CAMERA", cam_data)
        stage.objects.link(cam)
    elif cam.name not in stage.objects:
        for c in list(cam.users_collection):
            c.objects.unlink(cam)
        stage.objects.link(cam)

    cam.location = (7.5, 2.0, 4.8)
    cam.data.lens = 52
    point_at(cam, (0.0, 3.0, 0.85))
    tag(cam, "stage", "stage_camera")
    bpy.context.scene.camera = cam

    window = bpy.context.window
    screen = window.screen if window else None
    if screen:
        for area in screen.areas:
            if area.type != "VIEW_3D":
                continue
            region = next((r for r in area.regions if r.type == "WINDOW"), None)
            if region:
                try:
                    with bpy.context.temp_override(window=window, area=area, region=region):
                        bpy.ops.view3d.view_camera()
                except RuntimeError:
                    pass

    print("OrdaX component stage ready: frame | mattress | pillows | bedding")


if __name__ == "__main__":
    build()
