---
name: xri-starter-assets
description: Reference for XR Interaction Toolkit 3.2.1 Starter Assets sample. Use when working with XRI input actions, XR Origin rig prefab, interactor prefabs, locomotion providers, controller animation, teleport, affordances, gaze input, object spawning, or platform permissions.
---

# XRI 3.2.1 Starter Assets

## Location

`Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/` — official Unity sample imported from the XRI package.

## Top-Level Structure

| Folder / File | Contents |
|---|---|
| `Scripts/` | 14 runtime C# scripts |
| `Editor/Scripts/` | 1 build-validation editor script |
| `DemoSceneAssets/Scripts/` | 2 demo-only scripts |
| `Prefabs/` | XR Origin rig, interactors, controllers, teleport reticles, affordances |
| `DemoSceneAssets/Prefabs/` | Demo interactables, teleport environment, UI samples |
| `Presets/` | 9 component presets for locomotion, turn, grab-move, UI input module |
| `XRI Default Input Actions.inputactions` | Master input action asset |
| `Shaders/` | BiRP_Fresnel, Interactable, Unlit_Fresnel, Unlit_ShaderGraph, UI-NoZTest |
| `TunnelingVignette/` | Comfort vignette shader + prefab + material |
| `Materials/` | Controller, teleport, interactable, UI materials + `MaterialPipelineHandler` SO |
| `AffordanceThemes/` | Highlight, poke sphere color/scale theme assets |
| `Filters/` | `AnyGazedAtTeleportAnchorFilter.asset` |
| `Animations/` | `ArrowBounce.anim` + `Climb Teleport Arrow.controller` |
| `Textures/` | `DefaultMaterial_AO.png` |
| `StarterAssets.asmdef` | Assembly definition |

## Input Action Maps

`XRI Default Input Actions.inputactions` defines these maps:

| Action Map | Key Actions |
|---|---|
| **XRI Head** | Position, Rotation, Is Tracked, Tracking State, Eye Gaze Position/Rotation/Is Tracked/Tracking State |
| **XRI Left** | Position, Rotation, Is Tracked, Tracking State, Haptic Device, Aim Position/Rotation, Meta Aim Flags, Pinch Position, Poke Position/Rotation, Grip Position/Rotation, Thumbstick |
| **XRI Left Interaction** | Select, Select Value, Activate, Activate Value, UI Press/Value, UI Scroll, Translate/Rotate/Scale Manipulation, Scale Toggle, Scale Over Time |
| **XRI Left Locomotion** | Teleport Mode, Teleport Mode Cancel, Turn, Snap Turn, Move, Grab Move |
| **XRI Right** | Mirror of XRI Left |
| **XRI Right Interaction** | Mirror of XRI Left Interaction |
| **XRI Right Locomotion** | Mirror of XRI Left Locomotion + Jump |
| **XRI UI** | Navigate, Submit, Cancel, Point, Click, ScrollWheel, MiddleClick, RightClick |
| **Touchscreen Gestures** | Tap Start Position, Drag Start/Current/Delta, Pinch Start/Gap/Delta, Twist Start/Delta Rotation, Screen Touch Count, Spawn Object |

## Presets (9 component presets)

| Preset | Configures |
|---|---|
| `XRI Default Continuous Move.preset` | `ContinuousMoveProvider` |
| `XRI Default Dynamic Move.preset` | `DynamicMoveProvider` |
| `XRI Default Continuous Turn.preset` | `ContinuousTurnProvider` |
| `XRI Default Snap Turn.preset` | `SnapTurnProvider` |
| `XRI Default Left/Right Grab Move.preset` | `GrabMoveProvider` per hand |
| `XRI Default Left/Right Controller InputActionManager.preset` | `ControllerInputActionManager` per hand |
| `XRI Default XR UI Input Module.preset` | `XRUIInputModule` |

## Prefabs

### Core Rig (`Prefabs/`)

| Prefab | Purpose |
|---|---|
| `XR Origin (XR Rig).prefab` | Complete XR Origin with camera, controllers, interaction groups — main rig prefab |
| `Permissions Manager.prefab` | Android runtime permission requests by platform |

### Interactors (`Prefabs/Interactors/`)

| Prefab | Purpose |
|---|---|
| `Left_NearFarInteractor.prefab` | Left hand near-far combined interactor |
| `Right_NearFarInteractor.prefab` | Right hand near-far combined interactor |
| `Ray Interactor.prefab` | Standard ray interactor |
| `Direct Interactor.prefab` | Near/direct grab interactor |
| `Poke Interactor.prefab` | Poke/touch interactor |
| `Gaze Interactor.prefab` | Eye/head gaze interactor |
| `Teleport Interactor.prefab` | Teleportation ray interactor |

### Controllers (`Prefabs/Controllers/`)

| Prefab | Purpose |
|---|---|
| `XR Controller Left.prefab` | Left controller model with `ControllerAnimator` |
| `XR Controller Right.prefab` | Right controller model with `ControllerAnimator` |

### Teleport (`Prefabs/Teleport/`)

| Prefab | Purpose |
|---|---|
| `Directional Teleport Reticle.prefab` | Reticle showing teleport direction |
| `Blocking Teleport Reticle.prefab` | Reticle for blocked teleport areas |
| `Climb Teleport Arrow.prefab` | Animated arrow for climb-teleport destination |

### Affordances (`Prefabs/Affordances/`)

| Prefab | Purpose |
|---|---|
| `PokePointerAffordance.prefab` | Poke pointer visual feedback |
| `HighlightInteractionAffordance.prefab` | Highlight glow on interactable hover/select |

### Demo Prefabs (`DemoSceneAssets/Prefabs/`)

| Prefab | Purpose |
|---|---|
| `Interactables Sample.prefab` | Collection of grabbable demo objects |
| `Interactables/Cube|Cylinder|Torus|Pot|Blaser|Confetti|Push Button.prefab` | Individual demo interactables |
| `Poke Interactions Sample.prefab` | Poke button/slider demo |
| `UI Sample.prefab` | World-space UI panel |
| `UI/*.prefab` | TextButton, Text Toggle, Icon Button/Toggle, Dropdown, Slider, Scroll, Modal |
| `Teleportation Environment.prefab` | Floor/walls with teleport areas |
| `Teleport/Teleport Area|Anchor|Snap Teleport Anchor.prefab` | Individual teleport targets |
| `Gaze Interactables.prefab` | Gaze-selectable demo objects |
| `InteractionAffordance.prefab` | Affordance demo object |
| `Climb Sample.prefab` | Climbable surface demo |

## Scripts

### Locomotion

| Script | Extends | Purpose |
|---|---|---|
| `DynamicMoveProvider.cs` | ContinuousMoveProvider | Blends head vs. left/right controller forward direction for movement based on thumbstick magnitude |
| `ClimbTeleportDestinationIndicator.cs` | MonoBehaviour | Spawns pointer arrow at multi-anchor teleport destination during climb hover |

### Controller

| Script | Extends | Purpose |
|---|---|---|
| `ControllerAnimator.cs` | MonoBehaviour | Maps thumbstick/trigger/grip input values to controller submesh rotation/position |
| `ControllerInputActionManager.cs` | MonoBehaviour | Mediates ray vs. teleport interactor activation; enables/disables locomotion actions based on smooth motion/turn mode, near-far region, and UI scroll state |

### Interaction / Affordance

| Script | Extends | Purpose |
|---|---|---|
| `XRPokeFollowAffordance.cs` | MonoBehaviour | Tweens child transform toward poke point from `IPokeStateDataProvider`; deprecated affordance API |
| `ToggleColorToggler.cs` | MonoBehaviour | Sets `Toggle` normal color to on/off color on value change |
| `TeleportVolumeAnchorAffordanceStateLink.cs` | MonoBehaviour | Switches affordance state provider between volume and anchor on destination change; deprecated |
| `RotationAxisLockGrabTransformer.cs` | XRBaseGrabTransformer | Locks grab rotation to permitted axes bitmask |

### Spawning

| Script | Extends | Purpose |
|---|---|---|
| `ObjectSpawner.cs` | MonoBehaviour | Spawns prefab at point+normal, orients toward camera, optional viewport gate and VFX |
| `DestroySelf.cs` | MonoBehaviour | Self-destructs after `lifetime` seconds |

### Input / Gaze

| Script | Extends | Purpose |
|---|---|---|
| `GazeInputManager.cs` | MonoBehaviour | Enables GameObject when eye-tracking device detected; optional fallback mode |

### Platform

| Script | Extends | Purpose |
|---|---|---|
| `PlatformUnderstanding.cs` | static class | Classifies runtime as Meta / AndroidXR / Other OpenXR / Other via `OpenXRRuntime.name` |
| `PermissionsManager.cs` | MonoBehaviour | Requests Android permissions grouped by platform with granted/denied events |

### Materials (Runtime + Editor)

| Script | Extends | Purpose |
|---|---|---|
| `MaterialPipelineHandler.cs` | ScriptableObject + Editor types | Auto-swaps material shaders between BiRP and SRP based on `GraphicsSettings.currentRenderPipeline` |

### Demo-Only

| Script | Extends | Purpose |
|---|---|---|
| `MultiAnchorTeleportReticle.cs` | MonoBehaviour + IXRInteractableCustomReticle | Custom reticle for `TeleportationMultiAnchorVolume` with timer fill and destination indicators |
| `IncrementUIText.cs` | MonoBehaviour | Increments counter text on button press |

### Editor-Only

| Script | Extends | Purpose |
|---|---|---|
| `StarterAssetsSampleProjectValidation.cs` | static | Build validation rules: interaction layer 31 = "Teleport", Shader Graph installed, Input System version check |

## Deprecation Notes

- `XRPokeFollowAffordance` uses deprecated `Vector3TweenableVariable` from XRI affordance system.
- `TeleportVolumeAnchorAffordanceStateLink` is marked `[Obsolete]`.
- These still function in 3.2.1 but may be removed in future XRI versions.

## Conventions

- The `XR Origin (XR Rig).prefab` is the canonical XRI rig — extend it rather than building from scratch.
- Input actions are split by device (Left/Right), intent (Interaction/Locomotion), and modality (Head/UI/Touchscreen).
- `ControllerInputActionManager` is the key script for toggling between ray, teleport, and near-far modes at runtime.
- Presets apply default tuning to locomotion/turn providers — apply them via Inspector or `Preset.ApplyTo()`.
- `MaterialPipelineHandler` auto-runs in editor to keep materials compatible with the active render pipeline.
