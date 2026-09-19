from pathlib import Path
import importlib
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

# Blender Live is intentionally persistent. Purge only this bed-pipeline
# dependency set so a Git sync is reflected on the very next generation pass
# without restarting Blender or the OrdaX agent.
for module_name in ("cloth_part", "part_validation", "part_io", "common"):
    sys.modules.pop(module_name, None)
importlib.invalidate_caches()

from cloth_part import build


if __name__ == "__main__":
    print("PART_REPORT", build("duvet", export=True)[1])
