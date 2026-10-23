# Fever Dream: Team Workflow

Unity **6000.3.19f1**, 3D, Universal Render Pipeline. Team of 5.

## One-time setup (every team member)

```bash
git lfs install
git clone https://github.com/akeremkar-sketch/fever-dream.git
cd fever-dream
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "\"C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/Tools/UnityYAMLMerge.exe\" merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

Adjust the `UnityYAMLMerge.exe` path if Unity is installed elsewhere.

In Unity: **Edit > Project Settings > Editor** must stay on `Asset Serialization: Force Text`.

## Branches

- `main`: always opens and runs. No direct pushes; merge through pull requests.
- Work branches: `feature/<short-name>`, `fix/<short-name>`, `art/<short-name>`.
- Keep branches short-lived and rebase or merge `main` into yours at least daily.

## Scenes and prefabs

- Only **one person edits a given scene at a time**. Say so in the team chat before opening it.
- Prefer prefabs and additive scenes over one big scene so several people can work in parallel.
- Never commit a scene that has unrelated lighting or layout changes you did not intend.

## Commits

- Short imperative subject, e.g. `Add player stamina bar`.
- Always commit a `.meta` file together with its asset. Never delete a `.meta` by hand.
- Large binaries (textures, models, audio, video) go through Git LFS automatically via `.gitattributes`. Do not rename or move them outside Unity.

## Before opening a pull request

1. The project opens with no console errors.
2. The main scene enters Play mode.
3. `git status` shows no stray `Library/`, `Temp/` or build output.
