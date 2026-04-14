---
name: mrtk-scenes
description: Reference for MRTK3 example scenes in Assets/MRTKScenes. Use when navigating, loading, or extending MRTK3 demo scenes covering hand interaction, eye tracking, UI canvas, solvers, speech, and experimental features.
---

# MRTK3 Example Scenes

## Location

`Assets/MRTKScenes/` — 51 Unity scenes demonstrating MRTK3 features.

## Scene Inventory

### Hand Interaction & Manipulation

| Scene | Demonstrates |
|---|---|
| `HandInteractionExamples.unity` | Hand tracking basics, near/far interaction |
| `HandMenuExamples.unity` | Hand-attached menus via `SampleSceneHandMenu` |
| `BoundsControlExamples.unity` | `BoundsControl` handles on 3D objects |
| `BoundsControlRuntimeExample.unity` | Runtime-spawned `BoundsControl` + `ObjectManipulator` |
| `TapToPlaceExample.unity` | Tap-to-place surface alignment |
| `LegacyConstraintsExample.unity` | `ConstraintManager` with manipulation |
| `DisableInteractorsExample.unity` | `InteractorBehaviorControls` toggling modalities |

### UI — Canvas / UGUI

| Scene | Demonstrates |
|---|---|
| `CanvasExample.unity` | MRTK3 Canvas-based UI panels |
| `CanvasUITearsheet.unity` | Full UI component tearsheet (Canvas) |
| `VanillaUGUIExample.unity` | Standard Unity UGUI with MRTK3 |
| `NonCanvasUITearSheet.unity` | Non-Canvas UI component tearsheet |
| `NonCanvasUIBackplateExample.unity` | Non-Canvas backplate styling |
| `NonCanvasObjectBarExample.unity` | Non-Canvas object bar controls |
| `NonCanvasDialogExample.unity` | Non-Canvas dialog popups |

### UI — Navigation & Menus

| Scene | Demonstrates |
|---|---|
| `TopNavigationExample.unity` | Top navigation bar pattern |
| `NearMenuExamples.unity` | Near-interaction menus |
| `InteractableButtonExamples.unity` | Button variants and press feedback |
| `FontIconExample.unity` | MRTK3 font icon rendering |
| `InputFieldExamples.unity` | Text input fields |
| `TextPrefabExamples.unity` | Selawik text prefab variants |
| `ToggleCollectionExample.unity` | Toggle groups (`ToggleCollection`) |

### Dialogs

| Scene | Demonstrates |
|---|---|
| `DialogExample.unity` | Code-driven + inspector-driven dialogs via `DialogPool` |

### Solvers

| Scene | Demonstrates |
|---|---|
| `SolverExamples.unity` | Follow, RadialView, Orbital, SurfaceMagnetism |
| `DirectionalIndicatorExample.unity` | Directional indicator solver |

### Speech / Dictation / TTS

| Scene | Demonstrates |
|---|---|
| `SpeechInputExamples.unity` | `SpeechKeywordRecognitionHandler` keyword events |
| `DictationExample.unity` | `DictationHandler` start/stop dictation |
| `TextToSpeechExamples.unity` | `TextToSpeechHandler` with subsystem voices |
| `SeeItSayItExample.unity` | See-it-say-it voice label pattern |

### Eye Tracking (subfolder `EyeTracking/`)

| Scene | Demonstrates |
|---|---|
| `EyeGazeExample.unity` | Basic eye gaze cursor (`FollowEyeGaze`) |
| `EyeTracking/EyeTrackingBasicSetupExample.unity` | Eye calibration + `EyeCalibrationWarning` |
| `EyeTracking/EyeTrackingTargetSelectionExample.unity` | Gaze target selection (`EyeTrackingTarget`, `TargetGroupCreatorRadial`) |
| `EyeTracking/EyeTrackingTargetPositioningExample.unity` | Eye-gaze object positioning (`MoveObjectByEyeGaze`, `ObjectGoalZone`) |
| `EyeTracking/EyeTrackingExampleNavigationExample.unity` | Gaze scroll/pan/zoom (`PanZoomTexture`, `ScrollRectTransform`) |
| `EyeTracking/EyeTrackingVisualizerExample.unity` | Gaze recording/playback/heatmap (`UserInputRecorder`, `DrawOnTexture`) |

### Drawing / Whiteboard

| Scene | Demonstrates |
|---|---|
| `SlateDrawingExample.unity` | `Whiteboard` + `PenInteractor` drawing |

### Spatial

| Scene | Demonstrates |
|---|---|
| `SpatialMappingExample.unity` | Spatial mesh visualization |
| `MagicWindowExample.unity` | Magic window portal effect |

### Rendering

| Scene | Demonstrates |
|---|---|
| `OutlineExamples.unity` | Outline shader/effect |
| `ClippingExamples.unity` | Clipping primitives |
| `ClippingInstancedExamples.unity` | Clipping on instanced geometry |
| `DwellExample.unity` | Dwell interaction timing |

### Audio (subfolder `Audio/`)

| Scene | Demonstrates |
|---|---|
| `Audio/AudioLoFiExample.unity` | Lo-fi audio effect |
| `Audio/AudioOcclusionExample.unity` | Audio occlusion |

### Diagnostics / Performance

| Scene | Demonstrates |
|---|---|
| `DiagnosticsDemo.unity` | `SimpleProfiler` diagnostics overlay |
| `PerformanceEvaluation.unity` | `PerfSceneManager` FPS grid stress test |

### Empty / Starter

| Scene | Demonstrates |
|---|---|
| `EmptyScene/SampleEmptyMRTKScene.unity` | Minimal MRTK3 scene setup (good starting template) |

### Experimental (subfolder `Experimental/`)

| Scene | Demonstrates |
|---|---|
| `Experimental/CanvasExampleSimpleActionButton.unity` | Simple action button Canvas variant |
| `Experimental/NonNativeKeyboard.unity` | Non-native on-screen keyboard |
| `Experimental/ScrollingExample.unity` | Scrolling collection UI |
| `Experimental/SpatialMouseSample.unity` | Spatial mouse interaction |
| `Experimental/VirtualizedScrollRectList.unity` | `VirtualizedScrollRectList` performance scrolling |

## Scene ↔ Script ↔ Prefab Cross-References

- Hand menu scenes use `SampleSceneHandMenu.cs` + `SampleSceneHandMenu.prefab`.
- Dialog scenes use `DialogExample.cs` / `InspectorDrivenDialog.cs`.
- Whiteboard scene uses `Whiteboard.cs` + `PenInteractor.cs` + `pen.prefab` + `WhiteboardExample.prefab`.
- Eye tracking scenes use scripts under `MRTKScripts/EyeTracking/` + prefabs under `MRTK Prefabs/EyeTrackingExamples/`.
- `SampleEmptyMRTKScene.unity` is the recommended base for new scenes.

## Conventions

- Scene names end with `Example` or `Examples` (plural when multiple demos in one scene).
- Each scene typically pairs with a same-named prefab folder and script(s).
- Experimental scenes may use APIs from `MixedReality.Toolkit.UX.Experimental`.
