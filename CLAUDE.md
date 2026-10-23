# Fever Dream: Project Rules for Claude

Unity 6000.3.19f1, 3D, Universal Render Pipeline. Team of 5. Repo: `akeremkar-sketch/fever-dream` (private).

## Golden rule: the project must always be tidy and professional

Several designers and developers work in this project. Anyone opening the editor must be able to find what they
are looking for and understand the structure at a glance. This outranks speed. Never leave a mess for later.

### Project window (`Assets/`)
- One agreed folder structure, documented in `Docs/PROJECT_STRUCTURE.md`. That file is the single source of truth.
  Read it before creating, moving or renaming anything, and update it in the same change when the structure changes.
- Assets live in the folder of their type and feature. No loose files at the root of `Assets/`, no `New Folder`,
  no `Untitled`, no duplicate or `_copy`/`_old` leftovers.
- Consistent naming for every asset type (prefixes or suffixes as defined in `Docs/PROJECT_STRUCTURE.md`).
  PascalCase for assets and scripts, no spaces, no Turkish characters in file names.
- Always move and rename assets through Unity (or the Unity MCP asset tools) so `.meta` files and references
  stay intact. Never move asset files with the OS or plain `git mv` while Unity references them.

### Hierarchy (scenes)
- Every scene is organised with clear empty parent objects (for example `--- Environment ---`, `Gameplay`,
  `Lighting`, `UI`, `Managers`). Separator style and exact groups are defined in `Docs/PROJECT_STRUCTURE.md`.
- Meaningful object names. No `GameObject (1)`, `Cube (3)` or default names left behind.
- Reusable things are prefabs. Prefer additive and split scenes so several people can work in parallel.
- Transforms of grouping objects are reset (position 0, rotation 0, scale 1).

### Code
- One namespace per feature or system, matching the folder structure. One top-level class per file, file name
  equals class name.
- Short, readable scripts with clear responsibilities. Comment only the non-obvious why.
- After every script change, check the Unity console for compile errors before moving on.

## How to work

1. **Understand before building.** Read the GDD (`Docs/`) fully and ask about unclear points before implementing.
2. **Ask before restructuring.** Show the user any folder scheme, naming convention or large reorganisation
   first and wait for approval. Restructuring later is expensive for the whole team.
3. **Ask before irreversible actions:** deleting or moving many assets, overwriting scenes, changing project
   settings that affect everyone.
4. **Small, clean commits.** One concern per commit. Commit a `.meta` file together with its asset. Never commit
   `Library/`, `Temp/`, `Logs/`, `UserSettings/` or the machine-specific `.mcp.json`.
5. **Keep it consistent.** If an existing convention is in the way, follow it or propose a change, do not quietly
   invent a second style.
6. **Verify.** After changes, check the console, the hierarchy and the scene state through the Unity tools.

## Unity tooling notes

- The Unity editor must be open for the `unityMCP` tools to work. They load only when a session starts in this folder.
- `.mcp.json` contains machine-specific paths and is excluded from git locally (`.git/info/exclude`).
- Prototype scope for now: basic core mechanics only, no polish, but the structure must already be production-clean.
