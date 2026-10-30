# Map repair — 18 October 2026

## Changes

- Level 02 (source map 11): replaced the offset courtyard tile layers with coordinated masonry, a visible intact two-unit staircase and an aligned approach. Explicit physical walls leave the stair passage open.
- Rebuilt object-level footprints instead of garden-wide colliders: large crates, stone corner blocks, lanterns, fences and the central circular monument. The monument has a twelve-point elliptical collider.
- Straightened the square's corner blocks and lanterns. Replaced the stray broken brick pile with a stone bench and removed floating ladder fragments.
- Generated and installed three transparent pixel buildings: guild hall, smithy and merchant shop. Their aspect ratios are preserved; Unity imports use Point filtering, no mipmaps and no texture compression. Moved the market stall away from the shop and removed the loose bag over the guild roof.
- Added continuous grass backing, trees, shrubs and rocks outside the village walls. `VillageNorthernOutskirts.prefab` is a reusable native Unity scenery asset.
- Level 03 (source map 12): rebuilt the environment with forest islands, connected earth trails, multiple combat clearings, a western branch, eastern ruins and one accessible northern arch. Removed the repeated altar/stair/wall enclosure. Gameplay enemies and identities remain; the boss remains exclusively in Level 04.

## Assets and reproducibility

- Images and scenery prefab: `Assets/Generated/MapRepair/`.
- Image tool: built-in image generation, not a Unity plugin or CLI/API fallback. The exact prompt set is in `Backups/MapRepair_20261018/ImagePrompts.md`.
- Native Editor builders and audits: `Backups/MapRepair_20261018/`. These run in memory and were not installed as gameplay scripts or new menus.
- Before-edit scenes, scripts and settings: `Backups/MapRepair_20261018/Original/`.
- Current-turn save/preferences backup: `Backups/MapRepair_20261018/PlayerPrefs.json`.

## Verification

The live Editor's frame counter was confirmed to advance before testing. `Verification.txt` contains 63 physical/gameplay checks plus 3 final landscape/static-prop checks, all passed. Checks use the actual player's Rigidbody2D and PlayerMovement movement method in Play Mode, not only navigation-grid queries.

Covered: both courtyard facades and the side parapet, stairs in both directions, large crates, stone block, circular monument from multiple sides, guild facade, north boundary; walking to the courtyard, guild, merchant and smithy; normal attacks and fire damage plus water/lightning casts for knight and mage; both forest branches and the northern gate; actual portal travel 02 → 03 → 04 → 03 → 02, safe arrivals and retained inventory.

The final scene audit found no missing components or dynamic decorative bodies. Level 02 keeps 6 enemies; Level 03 keeps 8 enemies and no boss; Level 04 keeps its original boss. All five real save strings and tested story preferences were restored and exactly matched the fresh backup. Runtime source-file hashes are unchanged; only `Level_02.unity` and `Level_03.unity` changed among the existing scenes. No standalone build or complete narrative-ending playthrough was performed in this repair.
