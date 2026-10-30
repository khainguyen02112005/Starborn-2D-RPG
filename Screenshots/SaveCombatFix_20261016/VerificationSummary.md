# Save, SPUM and spell fixes — 16/10/2026

## Implemented

- Actor corruption copies the live SPUM renderer pieces, original sprites and colors. The previously generated elder/hero replacement images are no longer used for this effect.
- Save format 3 records position/facing, class, inventory/equipment/upgrades/durability, chest contents, health/mana/stamina, level/XP threshold/awakening, stat bonuses, learned skills, cast pattern/key bindings, quest/death/story flags, enemy/boss state, summoned minions and uncollected pickups across visited levels.
- Only explicit Save-slot selection overwrites a manual save. Quest, skill and portal callbacks no longer replace the user's chosen checkpoint. Restoration is guarded against partial-state saves and stale global story flags.
- Existing version-2 saves remain readable. Old interrupted ending autosaves resume at the elder's reward. Data that was never present in the old format cannot be reconstructed (for example its chest contents).
- XP is an animated purple crystal with bob/spin, attraction and collection. The placeholder circular coin is replaced with an animated pixel coin using the existing book UI asset. Both use saved Animation Clips and Animator Controllers.
- Boss spell objects have caster-owned lifetimes and are removed on death, deactivation or destruction. Water slow restores the player's normal speed. Death cannot start a new phase/attack loop.
- Tab, or the small button below the spell bar, cycles Single / 5-bolt Fan / 8-bolt Orbit. The orbit circles briefly before launching outward. Existing Q/F/R spell bindings remain. Spells must be learned; the volley charges mana once, with +1 for Fan or +2 for Orbit.
- Knight and mage cursor-fight AI use the same fan/orbit projectile implementation and learned-skill gates. Battle camera is fixed so hero movement cannot shift mouse world coordinates. End of battle clears remaining projectiles.

## Verification

- `Verification.txt`: full save menu / continue / cancel / cross-scene / inventory / quest / mid-boss tests; both classes' fan/orbit attacks, mana charging, cursor damage, and cleanup. All checks passed.
- `EdgeVerification.txt`: legacy-save reading and normal enemy collision/learned-skill gates passed. This original supplementary run was interrupted by an unresponsive Editor; it is not reported as a complete test run.
- `FinalVerification.txt`: after the user-approved Editor restart, phase-two HP/minions/slow/save roundtrip, projectile cleanup, native SPUM sprite identity, pickup Animators, and the actual self-destruction/reform sequence all passed.
- Final running Console: 0 errors, 0 runtime warnings. Compilation succeeded; the Editor still has pre-existing compile/package warnings.
- All five real save slots and story preferences were restored from the pre-test backup. The existing saved JSON is unchanged. Final scene left in Edit mode, clean.

## Recovery / maintenance

- Original scripts/assets and preference backup: `Backups/SaveCombatFix_20261016/`.
- Unity froze during an extra check. It was force-closed/reopened only after explicit user approval. The temporary scene recovery data was preserved in `Backups/SaveCombatFix_20261016/UnityRecoveryAfterHang/`, not deleted.
- Temporary verification ran from outside Assets; no Unity Tools menu, editor plugin or new package was installed.
