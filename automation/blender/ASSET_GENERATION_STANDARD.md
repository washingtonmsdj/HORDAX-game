# OrdaX Blender Asset Generation Standard v1

This is the default workflow for future generated 3D assets. A generated asset is not complete merely because it looks plausible.

## Mandatory pipeline

1. **Reference contract** — record real-world dimensions, coordinate system, camera/reference assumptions and required visible parts.
2. **Component generation** — build major parts in separate collections/modules. Do not start with one monolithic scene script.
3. **Rigid assembly** — use explicit anchors, dimensions and tolerances. Apply transforms before validation.
4. **Soft goods / deformables** — use an appropriate deformable workflow (Cloth where useful) with collision geometry; do not fake cloth by allowing penetration.
5. **Validation gate** — verify required objects, scale, dimensions, contact/clearance, floor penetration and component-specific rules. Critical failures must stop the pipeline.
6. **Materials** — only after geometry passes structural validation.
7. **Reference comparison** — compare the real Blender scene to the exact source reference from a controlled camera.
8. **Artifact save** — save to an ignored artifact path only after the validation gate passes.

## Scene contract

- Units: meters.
- Up axis: +Z.
- Generated objects must have stable names and the custom properties:
  - `ordax_asset`
  - `ordax_component`
  - `ordax_role`
  - `ordax_object_id`
  - `ordax_standard_version`
- One collection per component.
- No unrelated scene objects may be deleted.
- Object scale should be applied before final validation.
- Components must not interpenetrate unless the manifest explicitly marks the pair as an allowed contact.
- Contact surfaces use a small positive collision/clearance tolerance instead of exact coplanar overlap.

## Geometry quality

- Start from measured proportions, never from camera appearance alone.
- Use bevels proportional to the physical object.
- Prefer low-complexity controllable base geometry; add subdivision only where curvature requires it.
- Generate soft objects from their own mesh. Do not model a duvet, sheet, pillow or clothing item as a hard box for final output.
- Cloth meshes must have enough subdivisions to bend without becoming needlessly expensive.

## Validation

A pipeline must have a machine-readable validator. At minimum:

- required components exist exactly once;
- known real dimensions are within tolerance;
- scales are approximately `(1, 1, 1)`;
- rigid pieces obey their assembly bounds;
- soft goods do not pass below the mattress/floor in protected zones;
- pillows do not pass through the mattress or headboard;
- the camera transform is stored and repeatable.

Validation results are stored on the Blender scene as `ordax_validation_report`.

## Iteration

Use three passes:

- **structure** — frame, anchors, proportions;
- **contact** — mattress, pillows, cloth, collisions;
- **appearance** — materials, lighting, final camera.

A failed earlier pass is fixed before later passes continue.

The bed pipeline in `automation/blender/bed_pipeline/` is the reference implementation of this standard.

## Persistent-session correctness

Blender Live is a persistent Python process. Every generation entrypoint must invalidate import caches and reload its pipeline modules from the synchronized source tree. A generation must never silently execute stale module code after a Git update.

## OrdaX perception and contact gates

Future asset generators must use the OrdaX Blender live perception layer as part of
the normal generation loop. Visual plausibility alone is not an acceptance gate.

Before assembly:

1. Run a rich scene snapshot and record world-space bounds, dimensions, parent
   relationships, collections, modifiers, constraints, materials and mesh counts.
2. Inspect any suspicious component individually by stable object name.
3. Run BVH contact audits for every pair declared `forbid_intersection` in the
   asset manifest/contact rules.
4. Fix structural/contact failures before materials or final lighting.
5. Capture a viewport image only after structural validation passes.
6. Save the final artifact only after both machine validation and visual
   comparison pass.

The live MCP actions are:

- `blender.live_scene_snapshot`
- `blender.live_object_inspect`
- `blender.live_contact_audit`
- `blender.live_capture`

The generator manifest remains the source of truth for which pairs may touch,
which pairs must remain separated, and what dimensional tolerances apply.

### Object addressability

Every meaningful generated object must remain independently addressable. Use
stable `ordax_object_id` values rather than relying on Blender's automatically
suffixed names. Assemblies are built from validated components; they are not a
reason to lose component identity.

### No-intersection rule

AABB checks are only broad-phase diagnostics. Final critical contact checks use
evaluated mesh BVHs, because two rotated or soft objects can have overlapping
bounding boxes without their surfaces intersecting, or can visually intersect
while a simple transform-only inspection misses it.

For deformables, collision simulation remains the primary generation mechanism;
BVH auditing is the acceptance check after evaluation/bake.

