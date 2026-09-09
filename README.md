# ProjCorruly

Unity C# scripts and assets for a multiplayer game prototype. The source covers player movement, inventory and equipment, projectile effects, enemies, dungeon generation and terrain editing.

## Read the code

- `Scripts/Inventory`: item definitions, slots, equipment and UI.
- `Scripts/Player/Projectiles`: composable projectile effects.
- `Editor/DunGen`: dungeon layouts and geometry generation.
- `Scripts/Terrain`: terrain graph nodes and processing.

This is a collection of project assets, not a complete standalone Unity project. It uses Mirror networking, ProBuilder and CSG support. The repository does not include a Unity package manifest or project settings, so the original package versions and scene setup are needed to run it.

Some earlier and later implementations coexist. In particular, the two global `Projectile` classes need to be reconciled before compiling all scripts together. Treat this repository as prototype code rather than a ready-to-build game.
