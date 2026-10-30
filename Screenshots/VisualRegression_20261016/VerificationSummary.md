# Visual regression — 2026-10-16

Verified in the live Unity Editor with advancing frames:

- Knight and mage: correct loading avatar, right-facing run, 88px logical UI icon positioned at the progress front. Final track fill ratio matched progress (0.4215 and 0.4250); draggable handle removed.
- Both Level03 intros moved right with X scale -1 and the correct active avatar. Screenshots confirm the knight's sword and mage's staff face the travel direction.
- Boss health remained 375/500 over 24 frames alternating near/far player positions and simulated pointer-down/drag events. Fill width 469.5 within its 626px area: exactly 75%.
- Happy→Bad, white-void ending, defeat/reset prompt and the replayed-opening loop ending all reached their expected final panels. The corruption branch reached its choice panel and returned to the main menu.
- Main Camera stayed enabled, on Display 1 and without a target texture during the monitored story frames: zero invalid-display frames.
- TV pause froze the signal clock, finished glass fracture, resumed and cleaned up its auxiliary camera (zero remaining copies).
- World-space actor disintegration/reformation is visible in screenshots 08–11; no giant fullscreen reconstruction sprite remains.

The raw `WhiteVoid hero restored=False` log inspected the intentionally hidden legacy renderer on PlayerRoot, not the selected KnightAvatar. Screenshot `11_AfterReform.png` verifies the selected knight is visible again.

Final compile completed successfully with zero errors. The final recompile shows 18 deprecated-API warnings; the completed runtime checks had zero runtime warnings/errors. Historical console entries from earlier iterations remain in the Pipeline log and are not current failures.

Temporary verification/build scripts were removed from Temp. Original changed sources/scenes were backed up under `Backups/CinematicLoadingBoss_20261016`.
