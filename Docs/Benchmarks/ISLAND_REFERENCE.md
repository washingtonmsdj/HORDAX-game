# Island Reference Benchmark

This benchmark tests whether the OrdaX MCP/Dev Agent can reproduce a reference-driven Unity environment workflow without depending on Codex.

## Input intent

Recreate a near-top-down tropical reference as an explorable 3D Unity environment, matching terrain composition, water, vegetation, lighting and camera.

The external example requested **Pure Nature 2: Islands**. The current HORDAX workstation inventory does not contain that asset pack, so the first run is deliberately harder: OrdaX generates the environment procedurally with Unity-native geometry and materials.

A second run can use the asset pack later if it is legitimately imported into the project.

## Fairness

The benchmark does not claim that a procedural run and an asset-pack run are identical conditions. Results are labeled:

- `procedural-baseline`
- `pure-nature-2-islands`

Both must satisfy the same scene/composition/validation gates.

## Required output

- two separate crescent-like islands;
- central turquoise channel;
- light beach/sand zones;
- dense vegetation;
- coastal rocks;
- shallow-water halo plus deep water;
- comparable aerial camera;
- explorable WASD/QE camera;
- saved Unity scene;
- deterministic preview PNG;
- JSON benchmark report;
- successful compile and physics audit.

## MCP workflow

1. `unity.project_profile`
2. `unity.capabilities`
3. `unity.asset_inventory`
4. generate isolated benchmark scene
5. compile
6. render preview
7. `unity.scene_summary` when Editor is open
8. `unity.physics_audit` when Editor is open
9. compare visual output to the reference
10. iterate without changing the benchmark definition

This makes the benchmark reusable as OrdaX evolves.
