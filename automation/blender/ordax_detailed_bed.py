"""Compatibility entrypoint for the modular OrdaX bed generation pipeline.

Every Blender Live invocation reloads the pipeline modules from disk. Blender is
a persistent Python process, so normal imports would otherwise retain stale
module code after git.sync and make a new generation silently use old logic.
"""

from pathlib import Path
import importlib
import sys

PIPELINE = Path(__file__).resolve().parent / "bed_pipeline"
if str(PIPELINE) not in sys.path:
    sys.path.insert(0, str(PIPELINE))

# Reverse dependency order. Removing modules makes the next imports load the
# exact source currently on disk after git.sync.
for module_name in (
    "assemble",
    "validate",
    "bedding",
    "pillows",
    "mattress",
    "frame",
    "common",
):
    sys.modules.pop(module_name, None)

importlib.invalidate_caches()

import assemble

if __name__ == "__main__":
    assemble.build()
