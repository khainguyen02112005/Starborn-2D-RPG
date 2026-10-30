# Cinematic signal revision — 2026-10-16

The old pink/cyan `DigitalCorruption.png` is retained as source art but no longer
used as the fullscreen signal. `CinematicDigitalGlitch` now copies the live
main-camera view and cinematic canvases to a temporary RenderTexture, processes
the complete picture including captions, and overlays animated monochrome signal
noise. It restores canvas render modes and the camera target when interrupted,
paused, resized or destroyed. A separate temporary capture camera keeps Main
Camera rendering to Display 1, avoiding Unity's "No cameras rendering" notice.
No new rendering package is installed.

Two styles share one time source: Television (soft live scene, subdued short elder
caption, light sync jitter) and SignalBreak (stronger tearing, missing scan bands,
no spoken captions). Neither uses repeated full-white strobe frames.

Technical references (no package/plugin imported):

- Unity Camera.targetTexture: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/camera/targettexture
- Keijiro's original analog-glitch implementation, reference for the distinct concepts of scanline jitter, horizontal shake and colour drift: https://github.com/keijiro/KinoGlitch/blob/master/Assets/Kino/Glitch/Shader/AnalogGlitch.shader

Pause fracture uses clipped Voronoi cells, 140 distributed seeds plus 32 impact
chips by default, further splitting selected cells into angular slivers (about
220 pieces), with the captured pause/errors image mapped continuously across
all fragments. The same UI shader has an isolated glass pass for untextured edge
glints, avoiding the old black texture sample on fragment edges.

The fracture capture uses an explicit URP SingleCameraRequest, not an unfinished
Editor backbuffer read. Reference:
https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/rendering/renderpipeline/submitrenderrequest
