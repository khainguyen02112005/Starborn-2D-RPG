# Potion, damage and royal delivery fixes — 2026-10-16

Implemented in the existing runtime Scripts folder, with native Unity assets saved through the running Editor. No new plugin, Editor menu or installed tool was added.

## Changes

- Bag left-click uses one potion even while the merchant is open. Right-click explicitly sells one bag item while the shop is visible. Duplicate callbacks in the same frame cannot double-use or double-sell.
- Health/mana potions consume only after actual recovery, clamp to maximum and retain the bottle when already full. The book footer reports recovery or full resources.
- Restored readable floating damage numbers for enemies, boss, player and cursor combat. Critical hits remain gold; player damage is red. Battle numbers use unscaled time and clear after their lifetime.
- Fast elemental bolts sweep their travel path to avoid skipping enemies between frames.
- Royal messenger stays clear of the hero, transfers the original scroll artwork with an Animation Clip/Animator, then steps back. Timeline captions after delivery were moved 1.2 seconds later without changing their content. Early cancellation clears delayed staging.
- Knockback/freeze no longer re-enable enemy AI that a cinematic or other system disabled.

## Play Mode verification

See `Verification.txt` for the final passing run. Both knight and mage were exercised through their actual inventory and merchant Button callbacks, basic attacks and five-projectile spells. Also checked full mana/health, full bag, right-click selling, fast projectile collision, royal delivery, control restoration, boss/player damage numbers, battle-number lifetime and early Timeline cancellation.

Screenshots: `01KnightDamage.png`, `02MageDamage.png`, `03KnightLetterHandoff.png`, `04MageLetterHandoff.png`.

Final compilation succeeded and the Unity Console reported zero current errors. All backed-up preference keys and five save-slot strings were compared after restoration: no differences. Editor left stopped on the clean Main menu scene.

## Editor diagnostic limitation

Synchronous scene loads during the temporary combat verification runner froze the Editor on both DX12 and DX11. The final verification uses asynchronous scene loads and completed. This does not establish a graphics-driver cause or claim that every possible Editor hang is fixed. DX11 was used only for this Editor launch, with user approval; build graphics settings were not changed.

Pre-change runtime sources, the original Timeline and PlayerPrefs backup are under `Backups/PotionHandoffDamage_20261016`. Validation C# is kept outside Assets and is not imported as an Editor tool.
