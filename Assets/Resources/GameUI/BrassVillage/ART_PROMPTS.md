# Brass Village UI

Final game art lives in this folder. Generation mode: built-in image generation; no CLI/API fallback. These are original raster frames, imported Point-filtered and uncompressed with native Unity nine-slice sprite data. Vietnamese labels are real game text, never painted into the art. Existing item/coin/lock icons and the SPUM characters are retained. The inventory book and all its pages are excluded.

- `Panel.png`: opaque charcoal-olive/walnut/brass large frame. Used for menus, HUD cards, minimap, pause, choices, endings and game over.
- `Button.png`: matching walnut/brass action button.
- `Dialogue.png`: thin charcoal-olive dialogue/status strip.
- `UIFont.asset`: project-local copy of the existing Vietnamese-capable Carlito font, keeping a consistent typeface outside the book; not AI-generated.

## Final panel prompt (edit)

Reference: initial generated panel, palette/frame geometry only.

Edit target: the supplied rectangular pixel game UI panel. Keep the exact front-facing 3:2 proportions, thin brass double frame, walnut trim, square rivets, stepped corners and outline placement. Change only surface opacity/interior texture: the whole interior must now be a perfectly OPAQUE, almost uniform very dark charcoal-olive field #20231f. No cloudy white/grey areas, no light rim bleeding, no translucency, no cloudy texture or gradients anywhere inside the frame. Subtle darker pixel grain only at corners. Keep the pixel-art rim crisp. This will be nine-sliced, so interior must remain flat. Export this version fully opaque, even outside the frame; use flat dark charcoal-olive outside too. The game will crop the sprite to the frame, so do not add transparent background or any alpha cutout. No text or symbols, no new decorations, no perspective, no shadow or glow.

## Button prompt

Use case: stylized-concept. Reference image: style and palette ONLY. Create a NEW game button sprite to match that dark charcoal-olive, walnut and aged brass panel. ONE horizontal front-facing rectangle with 4:1 aspect ratio, centred filling about94% of canvas width; true transparent background everywhere outside it, NOT a brown background. Blank dark walnut inset face, thin straight aged brass double rim, simple stepped squared corners and tiny square rivets, crisp chunky 16-bit pixel art, low-noise flat centre for cream text drawn in Unity later. All decoration near the corners and edges for nine-slicing. No writing, no text, no label, no icon, no book, no shadow outside the button, no glow, no perspective, no rounded modern pill. A usable production raster UI sprite, no mockup, no multiple buttons. The surrounding canvas must have actual alpha transparency.

## Dialogue prompt

Use case: stylized-concept. Reference is palette/style only. Generate ONE NEW long dialogue frame sprite for the same medieval SPUM-style pixel RPG, perfectly straight front-facing horizontal rectangular inset plaque about6:1 aspect ratio. Fit across most canvas width, actual alpha transparency around the plaque. Dark nearly opaque charcoal-olive centre, thin walnut edge and very restrained aged brass double keyline, stepped square corners with small square brass pins. Nine-slice safe straight constant-thickness middle edges. Pixel art with deliberate chunky pixel steps, low-noise flat readable interior; 85% of plaque is empty space for external cream game subtitle text. No text, icons, curls, ribbons, banners, book pages, central ornaments, gradients, glow, perspective, scenery, multiple panels or mockup. This is an individual production UI texture. Keep the rim significantly thinner and simpler than reference panel, so small dialogue doesn't become all border. Transparent background outside plaque.

## Initial panel prompt (superseded by the opaque edit)

Use case: stylized-concept. Asset type: production game UI nine-slice panel sprite, not a mockup. Make ONE front-facing perfectly axis-aligned rectangular fantasy pixel-art panel for a small SPUM-style medieval top-down RPG. Landscape 3:2 silhouette nearly filling the canvas with a small transparent outer margin. Crisp deliberately chunky 16-bit pixel edges, no antialias, no painting blur. Restrained aged brass thin double-line frame with small square rivets and stepped squared corners; inner frame dark walnut, central field very dark muted charcoal-olive (#20231f), plain and low contrast so cream Vietnamese text can be drawn on it later. Center area at least 80% completely undecorated. Small consistent pixel wear at corners only, not noisy. Nine-slice safe: all middle edge segments straight and constant thickness, all decoration confined to corners, no central emblem, no text, no symbols, no shadows outside the sprite, no glow, no scenery, no book, no leather book binding. Fits olive grass, grey-beige stone, brown timber medieval village art. True transparent background outside the rectangle. The panel must be a usable shipped raster UI sprite, not multiple items or a screenshot.
