"""Generate only the rigid bed frame and upholstered headboard."""

from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import bpy

from common import (
    OX, OY, COL_FRAME, add_collision, add_cube, add_tapered_leg,
    cleanup_orphans, dim, ensure_collection, fabric_material, mark_build,
    tag, wood_material,
)


def build():
    col = ensure_collection(COL_FRAME, clear=True)
    cleanup_orphans()

    frame_w = dim("frame_width")
    frame_l = dim("frame_length")
    walnut = wood_material("ORDAX_MAT_Walnut")
    walnut_dark = wood_material("ORDAX_MAT_Walnut_Dark", (0.075, 0.032, 0.015))
    taupe = fabric_material("ORDAX_MAT_Headboard_Taupe", (0.386, 0.301, 0.231), weave=180.0, bump=0.13)

    # Rails: real dimensions, stable anchors.
    left = add_cube(col, "Left_Rail", "frame", "left_rail",
                    (OX-frame_w/2+0.035, OY, 0.37), (0.07, frame_l-0.06, 0.23),
                    walnut, bevel=0.016)
    right = add_cube(col, "Right_Rail", "frame", "right_rail",
                     (OX+frame_w/2-0.035, OY, 0.37), (0.07, frame_l-0.06, 0.23),
                     walnut, bevel=0.016)
    foot = add_cube(col, "Foot_Rail", "frame", "foot_rail",
                    (OX, OY-frame_l/2+0.045, 0.39), (frame_w, 0.09, 0.26),
                    walnut, bevel=0.020)
    support = add_cube(col, "Mattress_Support", "frame", "support",
                       (OX, OY, 0.46), (0.91, 1.84, 0.07),
                       walnut_dark, bevel=0.010)

    for obj in (left, right, foot, support):
        add_collision(obj, thickness=0.004, friction=10.0)

    # Four tapered legs.
    for index, (x, y) in enumerate((
        (OX-0.46, OY-0.93), (OX+0.46, OY-0.93),
        (OX-0.46, OY+0.91), (OX+0.46, OY+0.91),
    )):
        add_tapered_leg(col, f"Leg_{index}", (x, y, 0.17), 0.34, walnut_dark)

    # Headboard posts and backer.
    head_y = OY + frame_l/2 - 0.015
    for index, x in enumerate((OX-0.515, OX+0.515)):
        post = add_cube(col, f"Headboard_Post_{index}", "frame", "headboard_post",
                        (x, head_y, 1.23), (0.075, 0.13, 1.72),
                        walnut, bevel=0.023, segments=5)
        add_collision(post, thickness=0.004, friction=8.0)
        add_cube(col, f"Headboard_Post_Cap_{index}", "frame", "headboard_cap",
                 (x, head_y, 2.105), (0.088, 0.145, 0.095),
                 walnut, bevel=0.030, segments=6)

    back = add_cube(col, "Headboard_Back", "frame", "headboard_back",
                    (OX, head_y+0.015, 1.39), (0.99, 0.095, 1.48),
                    walnut_dark, bevel=0.040, segments=5)
    add_collision(back, thickness=0.004, friction=10.0)

    channels = int(dim("headboard_channels"))
    usable_w = 0.92
    channel_w = usable_w / channels
    for index in range(channels):
        x = OX - usable_w/2 + channel_w/2 + index*channel_w
        panel = add_cube(col, f"Headboard_Channel_{index}", "frame", "headboard_channel",
                         (x, head_y-0.050, 1.40),
                         (channel_w-0.009, 0.105, 1.40),
                         taupe, bevel=0.048, segments=8)
        panel["ordax_channel_index"] = index
        add_collision(panel, thickness=0.004, friction=12.0)

    # Invisible safety floor used only as a cloth collider.
    safety = add_cube(col, "Safety_Floor", "frame", "safety_floor",
                      (OX, OY-0.05, -0.035), (3.4, 3.5, 0.06),
                      None, bevel=0.0)
    safety.hide_render = True
    safety.hide_set(True)
    add_collision(safety, thickness=0.003, friction=12.0)

    mark_build("frame")
    return col


if __name__ == "__main__":
    build()
    print("OrdaX bed pipeline: frame built.")
