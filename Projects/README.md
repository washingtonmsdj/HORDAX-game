# Projects archive

This directory is the official HORDAX project archive.

The `mcp-blender` repository contains only MCP/agent/CI code. Project content,
work files, exports, references and project-specific history belong here.

## Layout

Each project gets an isolated directory:

```
Projects/
  <family>/
    <project-slug>/
      project.json
      README.md
      source/
      exports/
      references/
      checkpoints/
```

Large Blender and 3D assets are stored through Git LFS according to the repository
`.gitattributes`.

The local workstation path is intentionally not committed. The OrdaX Dev Agent keeps
the local path mapping in its local project registry.

## Backup policy

- Keep one canonical project folder per project.
- Save meaningful Blender checkpoints rather than every autosave.
- Preserve project-specific textures/materials/references.
- Do not store MCP implementation code here.
- Generated caches and temporary Blender/Unity files remain excluded.
