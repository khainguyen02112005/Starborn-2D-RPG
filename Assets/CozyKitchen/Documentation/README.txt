Cozy Kitchen 2D Asset Pack
=========================

OVERVIEW
--------
The Cozy Kitchen 2D Asset Pack is a hand-crafted pixel art environment designed
for top-down or angled 2D games. It provides everything you need to quickly
build a warm, lived-in kitchen interior, ideal for RPGs, narrative games, or
cozy simulation-style games.

The pack includes:
- Modular kitchen sprites (furniture, props, decor)
- A fully assembled demo scene
- A pre-sliced and named spritesheet
- Optional utility scripts used in the demo

This pack is focused on environment art first, with scripts included only as
lightweight helpers.


FOLDER STRUCTURE
----------------
The asset is organized to be easy to explore and modify:

Assets/
  CozyKitchen/
    Demo/
      Animations/
      Audio/
      Materials/
      Rendering Pipeline/
      Scenes/
      Scripts/
      Tile Palettes/
    Sprites/
    Scenes/
    Settings/


KEY FOLDERS
-----------
Sprites/
Contains all kitchen sprites and the main spritesheet.
The spritesheet is already sliced and all sprites are named to make searching
and reuse easy.

Demo/Scenes/
Contains the sample scene demonstrating how the assets fit together.

Demo/Scripts/
Contains small optional scripts used only in the demo scene.


DEMO SCENE
----------
The included demo scene shows:
- A complete cozy kitchen layout
- Proper sprite layering and sorting
- Example URP 2D lighting
- Subtle ambient effects (lighting and rain)

Nothing in the demo scene is required for runtime use.
You are encouraged to duplicate the scene, remove anything you do not need,
and rebuild layouts using the individual sprites.


SCRIPTS (OPTIONAL)
------------------
This pack includes two very small utility scripts used only for visual polish
in the demo. These scripts are NOT required to use the art assets.

1) LightPulse2D
---------------
Used to gently pulse 2D lights such as candles, lanterns, and fireplaces.

How it works:
- Slowly oscillates a Light2D's intensity over time
- Fully inspector-driven
- Each light pulses slightly differently for a natural look

How to use:
1. Select a GameObject with a Light2D component
2. Add the LightPulse2D script
3. Adjust min/max intensity and pulse speed in the Inspector

You can safely remove this script if you do not want pulsing lights.


2) AnimatedObject
-----------------
Used for simple looping or interval-based animations with optional sound
(for example: fire flicker or ambient props).

How it works:
- Plays a secondary animation at a configurable time interval
- Optionally plays an audio clip when the animation triggers
- Defaults to an idle animation when inactive

How to use:
1. Add the script to a GameObject with an Animator
2. Assign animation names and timing in the Inspector
3. (Optional) Assign an AudioSource

This script is included for convenience and can be replaced by your own
animation logic if desired.


RENDERING NOTES
---------------
- The demo scene uses URP 2D Lighting
- Sprites are compatible with both Lit and Unlit materials
- Normal maps can be added, but are not required

If you are not using URP:
- The sprites will still work normally
- Lighting-related materials and settings can be ignored


CUSTOMIZATION TIPS
------------------
- All sprites are modular and can be rearranged freely
- Sorting Layers are used for clean top-down overlap
- Assets work well at 16x16, 32x32, or scaled resolutions
- Ideal for interiors such as cabins, inns, kitchens, and shops


LICENSE
-------
This asset pack may be used in both personal and commercial projects.
You may modify the assets to fit your project needs.
Redistribution of the assets themselves is not permitted.
