Retro Vision Pro v1.0 — README

THANK YOU
Thank you for your purchase.

ABOUT
Retro Vision Pro emulates screen bleeding , Film-accurate 80s/90s tape artifacts and distortions, interlacing, 
shake, glitches and other effects.

QUICK START (AFTER IMPORT)
You can enable the effects in two ways:

1. AUTOMATIC SETUP

* Menu: Tools → RetroVisionProSetupTool → Add
* The tool adds the required Render Features to your active URP Renderer asset.

2. MANUAL SETUP

* Open your active URP Renderer asset:
  Project Settings → Graphics → Scriptable Render Pipeline Settings → open your URP Renderer (e.g., ForwardRenderer).
* In the Renderer asset, click “Add Render Feature”.
* Add the Retro Vision Pro Render Features you plan to use.
* On your Camera, enable “Post Processing”.
* Create a Global Volume and add Retro Vision Pro effects, or use the provided volume presets.

SAMPLES AND PRESETS

* Example scene: ExampleScene/Example Scene
* Presets: ExampleScene/Presets
  Apply via the Inspector Preset menu or by dragging onto a Volume component.

PROJECT STRUCTURE
Required folders (do not remove):

* Resources — shaders used by effects.
* Scripts — render features, passes, volumes, editor tooling.

Optional (safe to delete if not needed in your project):

* ExampleScene — remove after testing.
* Presets — keep only the ones you use.

Note: removing Resources or Scripts will break the asset.

DOCUMENTATION
Latest documentation and usage tips:
https://bricedev.pro/documentation/Retro-Vision-Pro-Documentation/

RECOMMENDED RENDER FEATURE ORDER
Adjust if needed for your project, but this is a good starting point:

1. OLDTV_SIGNAL_DISTORTION_FX
2. LINE_NOISE_FX
3. TAPE_NOISE_FX
4. TAPE_DISTORTION_FX
5. VHS_JITTER_FX
6. VHS_STRETCH_FX
7. VHS_TWITCH_FX
8. FILMGRAIN_NOISE_FX
9. SIGNAL_NOISE_FX
10. ANALOG_NOISE_FX
11. CRTAPERTURE_FX
12. DOT_CRAWL_FX
13. NTSCCODEC_FX
14. RETROSCALE_FX
15. VCRGHOSTING_FX
16. VHS_TAPE_REWIND_FX
17. BLEED2PHASE_FX
18. BLEED3PHASE_FX
19. BLEEDOLD3PHASE_FX
20. VHSSCANLINES_FX
21. WARP_FX
22. FISHEYE_VIGNETTE_FX
23. AnalogFrameFeedbackFX


CONTENT DISCLAIMER — DEMO 3D MODELS

All 3D models included in this package are low-quality placeholders supplied solely to demonstrate the visual effects. They are not optimized for production use (low poly/detail, simple UVs/materials, minimal LODs).

You may use, modify, or replace these demo models at your discretion in your own projects. 
They are provided as-is, with no support or updates planned. 
These models are not representative of final art quality and are intended only to help you preview and test the effects.


UNINSTALL
Before deleting files, run:
Tools → RetroVisionProSetupTool → Remove
This cleans up added Render Features and references.

SUPPORT
Email: [debrice@bk.ru](mailto:debrice@bk.ru)

Please include:

* Unity version
* Target platform (Windows, Android, iOS, etc.)
* A short video or screenshot, if possible

Bug reports are welcome. Clear, minimal reproduction steps from a new scene result in the fastest fix.