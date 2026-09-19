# OrdaX Unity automation

HORDAX uses two complementary layers.

## 1. OrdaX Dev Agent — execution authority

The Dev Agent remains responsible for deterministic operations on the user's Unity installation:

- `unity.compile`
- `unity.validate`
- `unity.run_method`
- `unity.capture`
- project/toolchain status and Git synchronization

This is the bridge between remote orchestration and the real local Unity Editor/project.

## 2. Unity official agent plugin — Unity knowledge layer

Unity's official `Unity-Technologies/unity-agent-plugin` provides first-party Unity skills to Codex. It should be treated as a knowledge/workflow layer, not as a replacement for OrdaX execution.

Use it for Unity-specific implementation guidance such as:

- UI Toolkit
- 2D / sprites / tilemaps
- Render Graph and rendering workflows
- localization
- multiplayer and Unity Gaming Services
- In-App Purchasing
- LevelPlay / monetization
- other skills shipped by Unity

### Codex installation

This is installed into Codex, not into Unity Package Manager or the Asset Store:

```
codex plugin marketplace add Unity-Technologies/unity-agent-plugin
codex plugin add unity@unity-agent-plugin
```

Verify with:

```
codex plugin list
```

and start a new Codex session; `/unity:` should expose Unity skills.

## OrdaX policy

- Unity plugin skills may propose or author changes.
- OrdaX Dev Agent performs compile/validate/capture against the real project.
- A successful code edit is not considered complete until the local Unity validation step passes.
- Generated Blender assets still pass the OrdaX asset pipeline before Unity import.
- Do not duplicate Unity execution logic in the plugin integration if the Dev Agent already provides a typed action for it.
