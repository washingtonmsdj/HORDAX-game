"""Export validated generated components into independent .blend libraries."""

from __future__ import annotations

from datetime import datetime, timezone
import json
from pathlib import Path

import bpy


HERE = Path(__file__).resolve().parent
PROJECT_ROOT = HERE.parents[2]
PARTS_ROOT = PROJECT_ROOT / "Artifacts" / "Blender" / "bed_parts"
REPORTS_ROOT = PARTS_ROOT / "validation"


def ensure_output_dirs() -> None:
    PARTS_ROOT.mkdir(parents=True, exist_ok=True)
    REPORTS_ROOT.mkdir(parents=True, exist_ok=True)


def export_collection(collection, part_id: str, report: dict, *, metadata: dict | None = None) -> Path:
    """Write one component as an independent blend library.

    Blender expands indirectly referenced datablocks, so object meshes,
    materials and node trees referenced by this collection travel with it.
    """
    ensure_output_dirs()
    if not report.get("ok"):
        errors = report.get("errors") or []
        raise RuntimeError(
            f"Refusing to export invalid part: {part_id}; errors={json.dumps(errors, ensure_ascii=False)}"
        )

    filepath = PARTS_ROOT / f"{part_id}.blend"
    datablocks = {collection, *collection.objects}
    bpy.data.libraries.write(
        str(filepath),
        datablocks,
        path_remap="RELATIVE",
        fake_user=True,
        compress=True,
    )

    payload = {
        "part_id": part_id,
        "blend_file": str(filepath),
        "exported_at": datetime.now(timezone.utc).isoformat(),
        "object_names": sorted(obj.name for obj in collection.objects),
        "report": report,
        "metadata": metadata or {},
    }
    (REPORTS_ROOT / f"{part_id}.json").write_text(
        json.dumps(payload, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )
    return filepath


def clear_part_collections(prefix: str = "ORDAX_PART_") -> None:
    for collection in list(bpy.data.collections):
        if not collection.name.startswith(prefix):
            continue
        for obj in list(collection.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(collection)
