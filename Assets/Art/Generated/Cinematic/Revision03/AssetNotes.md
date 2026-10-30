# Revision 03 — actor-bound pixel corruption

`ActorCorruptionAtlas.png` was generated with the built-in ImageGen tool, using
the existing knight/mage sheets for identity and the user's small pixel effect
as an effect reference. No Unity AI plugin or external API script was installed.

32 transparent sprites: 8 columns, 4 rows. Rows: knight, mage, blond NPC,
empty-centre pixel fragments. Actor rows have Flicker, Disintegrate and Reform
AnimationClips/Animator states; the fragment row has Scatter. Prefabs live in
`Prefabs/` and are referenced by the existing CinematicGeneratedLibrary resource.
Effects are world-space, actor-size bounded, and pause with the story.

## Final generation prompt

Use case: identity-preserve / stylized-concept. Asset type: transparent 2D pixel RPG animation sprite atlas for Unity, exactly 8 columns x 4 rows, equal cells, no labels, no grid lines, no text. Input 1 is the identity/style reference sheet of the red-haired knight; input 2 is the reference for knight and purple-haired mage; input 3 is ONLY reference for sparse purple digital disintegration, not its white background. Generate ONE cohesive production atlas. Retain the same tiny chibi SPUM-like proportions, black pixel outlines, skin and clothing palettes, flat side/front three-quarter game view, no painted shading, no bloom, no blurry glow. Every cell must fit its subject with at least 16% transparent margins, same feet baseline, no overlapping cells. Row 1: eight successive red-haired knight glitch frames, knight with small silver sword and round wood shield, troubled expression, slight stagger, progressively sparse violet/black pixel fragments breaking off only his silhouette, ending in scattered pixels. Row 2: eight successive purple double-bun-haired female mage glitch frames, burgundy outfit and wooden purple-orb staff, same progression. Row 3: eight successive blond village messenger/elder glitch frames, short blond hair, green tunic and brown boots, same progression. Row 4: eight successive EMPTY-CENTER rings of small violet/charcoal pixel fragments which expand then disperse, no character, no opaque center and no solid white rectangle. Animation left to right, frames 1-3 mostly intact/readable, 4-6 disintegrating, 7-8 mostly fragments. Restrained dark violet accents, NOT a huge saturated magenta explosion. Genuinely transparent background including all cell margins and empty ring centers. No background scenery, no text, no watermarks, no artificial checkerboard.
