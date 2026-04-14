---
name: mrtk-prefabs
description: Reference for MRTK3 example prefabs in Assets/MRTK Prefabs. Use when instantiating, modifying, or wiring up MRTK3 demo prefabs for UI panels, eye tracking, object manipulation, text, whiteboard, spatial mesh, and scene helpers.
---

# MRTK3 Example Prefabs

## Location

`Assets/MRTK Prefabs/` — 52 prefab assets organized by feature category.

## Prefab Inventory

### Canvas UI (`CanvasExample/`)

| Prefab | Purpose |
|---|---|
| `UIPanelExample.prefab` | Full MRTK3 Canvas UI panel with buttons, sliders, toggles |
| `MenuExample.prefab` | Canvas menu layout |
| `TopNavigationExample.prefab` | Top navigation bar Canvas panel |
| `HeroButton.prefab` | Large hero-style Canvas button |

### Bounds Control (`BoundsControl/`)

| Prefab | Purpose |
|---|---|
| `CoffeeBoundsControl.prefab` | Coffee cup with `BoundsControl` handles |
| `CheeseBoundsControl.prefab` | Cheese wedge with `BoundsControl` handles |
| `ScaledBoundingBoxWithTraditionalHandles.prefab` | Scaled bounds control with traditional handle visuals |

### Object Manipulation (`ObjectManipulator/`)

| Prefab | Purpose |
|---|---|
| `CheeseObjectManipulator.prefab` | Cheese wedge with `ObjectManipulator` |
| `CoffeeCupObjectManipulator.prefab` | Coffee cup with `ObjectManipulator` |
| `EarthObjectManipulator.prefab` | Earth globe with `ObjectManipulator` |
| `PlatonicObjectManipulator.prefab` | Platonic solid with `ObjectManipulator` |

### Eye Tracking (`EyeTrackingExamples/`)

#### Navigation (`NavigationExample/`)

| Prefab | Purpose |
|---|---|
| `AutoEyeScroll.prefab` | Auto-scrolling content driven by eye gaze |
| `EyeScrollCanvas.prefab` | Canvas with eye-gaze scroll (`ScrollRectTransform`) |
| `EyeGazeRotation.prefab` | Object rotation following eye gaze (`OnLookAtRotateByEyeGaze`) |
| `EyeGazeRotationWithVoiceCommand.prefab` | Gaze rotation + voice command trigger (`TargetMoveToCamera`) |
| `PanAndZoomExample.prefab` | Texture pan/zoom driven by eye gaze (`PanZoomTexture`) |
| `Paragraph.prefab` | Text paragraph for eye-scroll demos |

#### Target Selection (`TargetSelection/`)

| Prefab | Purpose |
|---|---|
| `EyeTracking_BlueTarget.prefab` | Blue gaze-selectable target (`EyeTrackingTarget`) |
| `EyeTracking_GreenTarget.prefab` | Green gaze-selectable target |
| `EyeTracking_PurpleTarget.prefab` | Purple gaze-selectable target |
| `EyeTracking_YellowTarget.prefab` | Yellow gaze-selectable target |

#### Visualizer (`Visualizer/`)

| Prefab | Purpose |
|---|---|
| `TitleBar.prefab` | Title bar for visualizer UI |
| `LiveTrackingSample.prefab` | Live eye tracking heatmap sample (`DrawOnTexture` live mode) |
| `RecordingSample.prefab` | Recorded eye tracking heatmap sample (`DrawOnTexture` playback) |
| `GazeVisualizerControls.prefab` | Record/playback UI controls (`UserInputRecorderUIController`) |

#### Root

| Prefab | Purpose |
|---|---|
| `EyeCalibrationChecker.prefab` | Eye calibration status checker |
| `EyeTrackingCursor.prefab` | Eye gaze cursor (`FollowEyeGaze`) |

### Text Prefabs (`TextPrefabExample/`)

| Prefab | Purpose |
|---|---|
| `UITextSelawik.prefab` | Selawik regular (UI/Canvas) |
| `UITextSelawikBold.prefab` | Selawik bold (UI/Canvas) |
| `UITextSelawikLight.prefab` | Selawik light (UI/Canvas) |
| `UITextSelawikSemibold.prefab` | Selawik semibold (UI/Canvas) |
| `UITextSelawikSemilight.prefab` | Selawik semilight (UI/Canvas) |
| `3DTextSelawik.prefab` | Selawik regular (3D/world-space) |
| `3DTextSelawikBold.prefab` | Selawik bold (3D/world-space) |
| `3DTextSelawikLight.prefab` | Selawik light (3D/world-space) |
| `3DTextSelawikSemibold.prefab` | Selawik semibold (3D/world-space) |
| `3DTextSelawikSemilight.prefab` | Selawik semilight (3D/world-space) |

### Whiteboard (`Whiteboard/`)

| Prefab | Purpose |
|---|---|
| `WhiteboardExample.prefab` | Whiteboard surface with `Whiteboard` interactable |
| `pen.prefab` | Pen tool with `PenInteractor` (implements `IPokeInteractor`) |

### Spatial Mesh (`SpatialMesh/`)

| Prefab | Purpose |
|---|---|
| `SpatialMesh.prefab` | Spatial mesh observer visualization |

### Magic Window (`MagicWindow/`)

| Prefab | Purpose |
|---|---|
| `MagicWindowExample.prefab` | Portal/magic window effect |

### Virtualized Scroll (`VirtualizedScrollRectList/`)

| Prefab | Purpose |
|---|---|
| `VirtualizedListButton.prefab` | Button item for `VirtualizedScrollRectList` |

### Sample Scene Helpers (`SampleSceneHelper/`)

| Prefab | Purpose |
|---|---|
| `SampleSceneHandMenu.prefab` | Hand menu for scene navigation, ray/gaze toggles, profiler |
| `Placard.prefab` | Descriptive placard for placing near demo objects |
| `DescriptionPanel.prefab` | Description panel overlay |
| `ColorChangingCube.prefab` | Cube that changes color on interaction |
| `ColorChangingCubeWithDescription.prefab` | Same with attached description |
| `TestDummyWalls.prefab` | Wall geometry for spatial/physics testing |

### Experimental (`Experimental/CanvasExampleSimpleActionButton/`)

| Prefab | Purpose |
|---|---|
| `UIPanelExampleSimpleActionButton.prefab` | Simplified Canvas action button panel |
| `MenuExampleSimpleActionButton.prefab` | Simplified Canvas action button menu |
| `TopNavigationExampleSimpleActionButton.prefab` | Simplified top-nav with action buttons |

### Event System

| Prefab | Purpose |
|---|---|
| `EventSystem.prefab` | Pre-configured MRTK3 EventSystem |

## Prefab ↔ Script Pairings

| Prefab folder | Script(s) |
|---|---|
| `Whiteboard/` | `Whiteboard.cs`, `PenInteractor.cs` |
| `EyeTrackingExamples/Visualizer/` | `DrawOnTexture.cs`, `UserInputRecorder.cs`, `UserInputPlayback.cs`, `UserInputRecorderUIController.cs` |
| `EyeTrackingExamples/TargetSelection/` | `EyeTrackingTarget.cs`, `TargetGroupCreatorRadial.cs` |
| `EyeTrackingExamples/NavigationExample/` | `PanZoomTexture.cs`, `ScrollRectTransform.cs`, `OnLookAtRotateByEyeGaze.cs`, `TargetMoveToCamera.cs` |
| `SampleSceneHelper/` | `SampleSceneHandMenu.cs`, `ColorChanger.cs` |
| `ObjectManipulator/` | Uses MRTK3 built-in `ObjectManipulator` component |
| `BoundsControl/` | Uses MRTK3 built-in `BoundsControl` component |
| `VirtualizedScrollRectList/` | `VirtualizedScrollRectListTester.cs` |

## Conventions

- Prefab folder names match scene names (e.g. `CanvasExample/` ↔ `CanvasExample.unity`).
- Eye tracking prefabs expect `FuzzyGazeInteractor` in the scene's XR Rig.
- Text prefabs use Selawik font in five weights, available as both UI (Canvas) and 3D (world-space) variants.
- `SampleSceneHandMenu.prefab` is reused across most example scenes for consistent navigation.
- `EventSystem.prefab` should be in every scene that uses MRTK3 input.
