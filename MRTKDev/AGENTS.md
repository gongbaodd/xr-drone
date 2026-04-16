# Agent and editor context

Machine-readable project notes for assistants and tools:

- **Cursor (always):** `.cursor/rules/mrtkdev-project-context.mdc` — project-wide rendering/package context.
- **Cursor (file-specific):**
  - `.cursor/rules/drone-xr-emulator-input.mdc` — `LimitedDroneEmulator.cs` XR mapping, 0..1 throttle mapping, arming gate behavior.
  - `.cursor/rules/controller-scene-flight-volume.mdc` — `ControllerScene.unity` collider-only flight volume + separate glass visual convention.

Open the relevant rule(s) above and keep them updated when rendering, packages, or flight tooling changes.

## OBJ / MTL texture mapping (URP)

For Wavefront **OBJ + MTL** imports where **`map_Kd` textures exist on disk but URP Lit materials show NULL `_BaseMap`**, follow `.cursor/skills/unity-obj-mtl-texture-remap/SKILL.md` (parse MTL, write external materials under `Materials/`, `ModelImporter.AddRemap` + external location, reimport). Prefer **Unity MCP** `Unity_RunCommand` for scripted fix and verification.

## XR Joystick HUD conventions

For HUD work, follow `.cursor/skills/xr-joystick-hud/SKILL.md`.

Key constraints:
- Keep `XRJoystickHud` asset-driven (no runtime creation of quad/material/render texture/panel settings).
- Use scene child `XR Joystick HUD Quad` with material asset + `RenderTexture` asset.
- Ensure `UIDocument.panelSettings.targetTexture` matches the quad material `mainTexture`.
- Keep HUD background transparent via `PanelSettings.colorClearValue = (0,0,0,0)`.
- Keep quad front facing camera with `Quaternion.LookRotation(-toCamera.normalized, camera.up)`.

## MRTK3 example scripts

For script reference, follow `.cursor/skills/mrtk-scripts/SKILL.md`.

53 C# scripts in `Assets/MRTKScripts/` covering:
- **Eye tracking** — gaze cursor, target selection/positioning, scroll/pan/zoom, recording/playback/heatmap (subfolder `EyeTracking/`)
- **UI / layout** — `ToggleCollection` demos, `ScrollRectCurve`, `GridSqueezer`, mesh/color changers
- **Dialogs** — `DialogPool`-based inspector-driven and code-driven dialogs
- **Speech / dictation / TTS** — keyword recognition, dictation start/stop, text-to-speech via subsystems
- **Solvers** — Follow, RadialView, Orbital, SurfaceMagnetism via `SolverHandler`
- **Interaction** — `Whiteboard` + `PenInteractor`, `InteractorBehaviorControls`, `SampleSceneHandMenu`
- **Bounds control** — runtime `BoundsControl` + `ObjectManipulator` spawning
- **Audio** — `AudioBandPassEffect` filter selection

## MRTK3 example scenes

For scene reference, follow `.cursor/skills/mrtk-scenes/SKILL.md`.

51 Unity scenes in `Assets/MRTKScenes/` organized by feature:
- Hand interaction, menus, bounds control, tap-to-place
- Canvas and non-Canvas UI tearsheets, navigation, dialogs, toggles, input fields
- Speech input, dictation, TTS, see-it-say-it
- Eye tracking (subfolder): basic setup, target selection, positioning, navigation, visualizer
- Solvers, spatial mapping, magic window, whiteboard drawing
- Audio (subfolder): lo-fi, occlusion
- Diagnostics, performance evaluation
- Experimental (subfolder): virtualized scroll, spatial mouse, non-native keyboard
- `EmptyScene/SampleEmptyMRTKScene.unity` — recommended base for new scenes

## MRTK3 example prefabs

For prefab reference, follow `.cursor/skills/mrtk-prefabs/SKILL.md`.

52 prefab assets in `Assets/MRTK Prefabs/` organized by feature:
- **Canvas UI** — `UIPanelExample`, `MenuExample`, `TopNavigationExample`, `HeroButton`
- **Bounds control** — Coffee/Cheese with `BoundsControl` handles
- **Object manipulation** — Cheese/Coffee/Earth/Platonic with `ObjectManipulator`
- **Eye tracking** — target selection colors, navigation (scroll/pan/zoom/rotation), visualizer (live/recording heatmap), cursor, calibration
- **Text** — Selawik font in 5 weights × 2 modes (UI Canvas + 3D world-space)
- **Whiteboard** — `WhiteboardExample` + `pen` (paired with `Whiteboard.cs` / `PenInteractor.cs`)
- **Spatial mesh** — `SpatialMesh` observer visualization
- **Scene helpers** — `SampleSceneHandMenu`, `Placard`, `DescriptionPanel`, `ColorChangingCube`, `TestDummyWalls`
- **Event system** — Pre-configured MRTK3 `EventSystem`

## XRI 3.2.1 Starter Assets

For XRI starter asset reference, follow `.cursor/skills/xri-starter-assets/SKILL.md`.

Official Unity XR Interaction Toolkit 3.2.1 sample in `Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/`:
- **XR Origin rig** — `XR Origin (XR Rig).prefab` is the canonical XRI rig prefab
- **Input actions** — `XRI Default Input Actions.inputactions` with maps: Head, Left/Right device, Left/Right Interaction, Left/Right Locomotion, UI, Touchscreen Gestures
- **Interactor prefabs** — NearFar (L/R), Ray, Direct, Poke, Gaze, Teleport
- **Controller prefabs** — Left/Right with `ControllerAnimator` mapping stick/trigger/grip to mesh transforms
- **Locomotion** — `DynamicMoveProvider` (head/hand-relative blend), presets for continuous/snap turn, continuous move, grab move
- **Teleport** — Directional/Blocking reticles, climb teleport arrow, `ClimbTeleportDestinationIndicator`
- **Controller input management** — `ControllerInputActionManager` mediates ray vs. teleport vs. near-far activation
- **Affordances** — Poke pointer, highlight interaction (deprecated affordance APIs still functional)
- **Platform** — `PlatformUnderstanding` (Meta/AndroidXR/OpenXR classification), `PermissionsManager` (Android per-platform permissions)
- **Materials** — `MaterialPipelineHandler` auto-swaps shaders between BiRP and SRP
- **Shaders** — BiRP_Fresnel, Interactable, Unlit_Fresnel, UI-NoZTest, TunnelingVignette
- **Demo prefabs** — Interactables (Cube, Torus, Pot, etc.), Poke interactions, UI panels, Teleportation environment
