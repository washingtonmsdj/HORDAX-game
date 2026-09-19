"""Current OrdaX bed workflow: isolated parts review only.

The final bed is intentionally NOT assembled until every part is approved.
"""

from pathlib import Path
import runpy

SCRIPT=Path(__file__).resolve().parent/"ordax_bed_parts_workbench.py"
runpy.run_path(str(SCRIPT),run_name="__main__")
