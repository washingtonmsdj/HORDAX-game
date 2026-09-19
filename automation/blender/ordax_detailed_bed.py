"""Compatibility entrypoint for the modular OrdaX bed generation pipeline.

The implementation lives in automation/blender/bed_pipeline/.
This filename is kept so existing OrdaX Dev Agent jobs continue to work.
"""

from pathlib import Path
import sys

PIPELINE = Path(__file__).resolve().parent / "bed_pipeline"
if str(PIPELINE) not in sys.path:
    sys.path.insert(0, str(PIPELINE))

from assemble import build

if __name__ == "__main__":
    build()
