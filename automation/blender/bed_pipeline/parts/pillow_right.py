from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

from pillow_part import build


if __name__ == "__main__":
    print("PART_REPORT", build("pillow_right", export=True)[1])
