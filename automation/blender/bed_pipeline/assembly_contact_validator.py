"""Universal pairwise contact validator for future bed master assembly.

Default policy is FORBID_INTERSECTION. Structural overlaps must be explicitly
whitelisted in contact_rules.json.
"""

from __future__ import annotations

from datetime import datetime, timezone
import fnmatch
import json
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


HERE=Path(__file__).resolve().parent
RULES=json.loads((HERE/"contact_rules.json").read_text(encoding="utf-8"))
PROJECT_ROOT=HERE.parents[2]
REPORT_PATH=PROJECT_ROOT/"Artifacts"/"Blender"/"validation"/"assembly_contacts_latest.json"


def key(obj):
    return f"{obj.get('ordax_component') or ''}:{obj.get('ordax_role') or ''}"


def _matches(pattern, value):
    return fnmatch.fnmatchcase(value, pattern)


def policy_for(a,b):
    ka,kb=key(a),key(b)
    for left,right,policy in RULES.get("allowed_pairs",[])+RULES.get("assembly_pairs",[]):
        if (_matches(left,ka) and _matches(right,kb)) or (_matches(left,kb) and _matches(right,ka)):
            return policy
    return RULES["default_policy"]


def bvh_overlap_count(a,b):
    if a.type!="MESH" or b.type!="MESH":
        return 0
    dg=bpy.context.evaluated_depsgraph_get()
    ta=BVHTree.FromObject(a,dg,deform=True,cage=False,epsilon=0.0)
    tb=BVHTree.FromObject(b,dg,deform=True,cage=False,epsilon=0.0)
    if ta is None or tb is None:
        return 0
    return len(ta.overlap(tb))


def bounds(obj):
    pts=[obj.matrix_world@Vector(corner) for corner in obj.bound_box]
    lo=Vector((min(p.x for p in pts),min(p.y for p in pts),min(p.z for p in pts)))
    hi=Vector((max(p.x for p in pts),max(p.y for p in pts),max(p.z for p in pts)))
    return lo,hi


def aabb_gap(a,b):
    alo,ahi=bounds(a); blo,bhi=bounds(b)
    dx=max(blo.x-ahi.x,alo.x-bhi.x,0.0)
    dy=max(blo.y-ahi.y,alo.y-bhi.y,0.0)
    dz=max(blo.z-ahi.z,alo.z-bhi.z,0.0)
    return (dx*dx+dy*dy+dz*dz)**0.5


def validate(objects=None,*,raise_on_error=True):
    objs=list(objects) if objects is not None else [
        obj for obj in bpy.context.scene.objects
        if obj.get("ordax_asset")=="single_bed_reference" and obj.type=="MESH"
    ]
    report={
        "standard":"ordax.blender.asset-generation",
        "rule":"default FORBID_INTERSECTION",
        "checked_objects":len(objs),
        "checked_pairs":0,
        "errors":[],
        "warnings":[],
        "pair_metrics":[],
        "validated_at":datetime.now(timezone.utc).isoformat(),
    }

    for i,a in enumerate(objs):
        for b in objs[i+1:]:
            report["checked_pairs"]+=1
            policy=policy_for(a,b)
            if policy=="ALLOW_STRUCTURAL_JOIN":
                continue

            overlaps=bvh_overlap_count(a,b)
            metric={
                "a":str(a.get("ordax_object_id") or a.name),
                "b":str(b.get("ordax_object_id") or b.name),
                "policy":policy,
                "triangle_overlaps":overlaps,
            }

            if overlaps:
                report["errors"].append({
                    "code":"mesh_intersection",
                    **metric,
                })
            elif policy=="TOUCH_ONLY":
                gap=aabb_gap(a,b)
                metric["aabb_gap_m"]=round(gap,6)
                spec=RULES["policies"]["TOUCH_ONLY"]
                if gap>float(spec.get("max_clearance_m",0.012)):
                    report["warnings"].append({
                        "code":"touch_pair_too_far",
                        **metric,
                    })
            report["pair_metrics"].append(metric)

    # Floor rule applies universally in final assembly.
    for obj in objs:
        lo,_hi=bounds(obj)
        if lo.z < -0.008:
            report["errors"].append({
                "code":"floor_penetration",
                "object":str(obj.get("ordax_object_id") or obj.name),
                "min_z":round(lo.z,6),
            })

    report["ok"]=not report["errors"]
    REPORT_PATH.parent.mkdir(parents=True,exist_ok=True)
    REPORT_PATH.write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding="utf-8")
    bpy.context.scene["ordax_assembly_contact_report"]=json.dumps(report,sort_keys=True)
    bpy.context.scene["ordax_assembly_contact_ok"]=report["ok"]

    if raise_on_error and not report["ok"]:
        raise RuntimeError("Assembly contact validation failed: "+json.dumps(report["errors"][:20],ensure_ascii=False))
    return report


if __name__=="__main__":
    print(json.dumps(validate(raise_on_error=True),indent=2,ensure_ascii=False))
