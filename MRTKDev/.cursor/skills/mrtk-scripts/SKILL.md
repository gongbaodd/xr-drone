---
name: mrtk-scripts
description: Reference for MRTK3 example scripts in Assets/MRTKScripts. Use when editing, extending, or debugging MRTK3 sample MonoBehaviours covering eye tracking, UI, speech/dictation, solvers, interaction, whiteboard drawing, and audio.
---

# MRTK3 Example Scripts

## Location

`Assets/MRTKScripts/` — 53 C# scripts (MRTK3 sample/demo code).

## Package Dependencies

Scripts reference these MRTK3 and Unity packages:

| Assembly / Namespace | Used For |
|---|---|
| `MixedReality.Toolkit.Input` | `MRTKBaseInteractable`, `StatefulInteractable`, `FuzzyGazeInteractor`, `MRTKRayInteractor`, `GazePinchInteractor`, `IPokeInteractor`, `InteractionModeManager`, `PlayspaceUtilities` |
| `MixedReality.Toolkit.UX` | `ToggleCollection`, `DialogPool`, `IDialog`, `KeyboardPreview`, `SliderEventData`, `BoundsControl`, `ObjectManipulator`, `UGUIInputAdapterDraggable` |
| `MixedReality.Toolkit.Subsystems` | `XRSubsystemHelpers`, `TextToSpeechSubsystem`, `KeywordRecognitionSubsystem`, `DictationSubsystem`, `HandsSubsystem` |
| `MixedReality.Toolkit.SpatialManipulation` | `Solver`, `SolverHandler`, `Follow`, `RadialView`, `Orbital`, `SurfaceMagnetism` |
| `MixedReality.Toolkit.Audio` | `AudioBandPassEffect`, `AudioBandPassFilter` |
| `MixedReality.Toolkit.Diagnostics` | `SimpleProfiler` |
| Unity XR Interaction Toolkit | `XRBaseInteractor`, `SelectEnterEventArgs` |
| Unity Input System | `ActionBasedController`, `InputActionProperty` |

## Script Categories

### UI / Layout / Visual Feedback

| Script | Extends | Purpose |
|---|---|---|
| `VirtualizedScrollRectListTester.cs` | MonoBehaviour | Drives `VirtualizedScrollRectList` with sine scroll or page controls; fills TMP labels via `OnVisible` |
| `ToggleCollectionObjectActivate.cs` | MonoBehaviour | Shows one `GameObject` per toggle index from `ToggleCollection` |
| `ToggleCollectionColorChange.cs` | MonoBehaviour | Swaps renderer material by `ToggleCollection` index |
| `ScrollRectCurve.cs` | MonoBehaviour | Curves `ScrollRect` content children (parabola + tilt) on scroll |
| `GridSqueezer.cs` | MonoBehaviour | Resizes `GridLayoutGroup` cell size to fit fixed columns |
| `AdjustLabelPosition.cs` | MonoBehaviour | Offsets Y position based on control lossy scale |
| `ObjectSpinner.cs` | MonoBehaviour | Continuous spin; slider-driven rotation/scale via `SliderEventData` |
| `MeshChanger.cs` | MonoBehaviour | Cycles `MeshFilter.sharedMesh` for press feedback |
| `ColorChanger.cs` | MonoBehaviour | Cycles materials; `RandomColor()` |

### Dialogs

| Script | Extends | Purpose |
|---|---|---|
| `InspectorDrivenDialog.cs` | MonoBehaviour | Inspector-configured `IDialog` via `DialogPool.Get().Show()` |
| `DialogExample.cs` | MonoBehaviour | Code-spawned dialogs; `ShowAsync` with `DialogDismissedEventArgs` |

### Speech / Dictation / TTS

| Script | Extends | Purpose |
|---|---|---|
| `TextToSpeechHandler.cs` | MonoBehaviour | `Speak()` via `TextToSpeechSubsystem` + `MRTKProfile` voice config |
| `SpeechKeywordRecognitionHandler.cs` | MonoBehaviour | Registers keywords on `KeywordRecognitionSubsystem`; fires per-keyword + global events |
| `DictationHandler.cs` | MonoBehaviour | Start/stop dictation; pauses keyword subsystem while dictating |
| `EyeTracking/KeywordRecognitionHandler.cs` | MonoBehaviour | Same keyword pattern using `IKeywordRecognitionSubsystem` |

### Keyboard

| Script | Extends | Purpose |
|---|---|---|
| `SystemKeyboardExample.cs` | MonoBehaviour | UWP: `WindowsMRKeyboard` + `KeyboardPreview`; mobile: `TouchScreenKeyboard` |

### Solvers

| Script | Extends | Purpose |
|---|---|---|
| `SolverExampleManager.cs` | MonoBehaviour | Switches Follow/RadialView/Orbital/SurfaceMagnetism; configures `SolverHandler` tracked object, interactors, and hand joint rotation |

### Interaction / Interactors

| Script | Extends | Purpose |
|---|---|---|
| `Whiteboard.cs` | MRTKBaseInteractable | Paint texture from select attach points; line splats; `ClearDrawing` |
| `PenInteractor.cs` | XRBaseInteractor + IPokeInteractor | Trigger-based hover → valid targets; simple `PokePath` |
| `InteractorBehaviorControls.cs` | MonoBehaviour | Enable/disable interactor groups via `InteractionModeManager` presets |
| `SampleSceneHandMenu.cs` | MonoBehaviour | Toggles hand rays / gaze pinch; prev/next scene; profiler toggle |

### Bounds Control / Manipulation

| Script | Extends | Purpose |
|---|---|---|
| `BoundsControlRuntimeExample.cs` | MonoBehaviour | Coroutine: spawns cubes with `BoundsControl` + `ObjectManipulator`; cycles config options |

### Audio

| Script | Extends | Purpose |
|---|---|---|
| `BandPassFilterSelection.cs` | MonoBehaviour | Ensures `AudioBandPassEffect` on emitter; `SetFilter(index)` selects preset |

### Performance / Placement Utilities

| Script | Extends | Purpose |
|---|---|---|
| `PerfSceneManager.cs` | MonoBehaviour | Spawns grid of instances until FPS drops below threshold; reports count |
| `TetheredPlacement.cs` | MonoBehaviour | Resets transform + rigidbody when object drifts beyond `distanceThreshold` |

### Eye Calibration

| Script | Extends | Purpose |
|---|---|---|
| `EyeCalibrationWarning.cs` | MonoBehaviour | Subscribes to `EyeCalibrationChecker` status; shows green/red TMP messages |

### Eye Tracking — Utilities

| Script | Type | Purpose |
|---|---|---|
| `EyeTrackingUtilities.cs` | static class | Visual-angle ↔ meters conversion; delayed scene load; material color/transparency |
| `ChangeRenderMode.cs` | static class | Standard-material blend mode switching (Opaque/Cutout/Fade/Transparent) |
| `DisableOnStart.cs` | MonoBehaviour | `Awake`: `SetActive(false)` |
| `ColorTap.cs` | MonoBehaviour | Gaze hover/pinch + XRI select color tinting for visual feedback |
| `FollowEyeGaze.cs` | MonoBehaviour | Cursor positioned along `gazeController` forward; colors via `IGazeInteractor` validity |

### Eye Tracking — Logging / Playback / Heatmap

| Script | Extends | Purpose |
|---|---|---|
| `LogStructure.cs` | MonoBehaviour (abstract) | `GetHeaderColumns` / `GetData` for CSV rows |
| `LogStructureEyeGaze.cs` | LogStructure | Logs gaze origin/direction/hit via `FuzzyGazeInteractor.PreciseHitResult` |
| `FileInputLogger.cs` | IDisposable | Writes CSV to `persistentDataPath` via `StreamWriter` |
| `UserInputRecorder.cs` | MonoBehaviour | Per-frame: merges user/session/time + `LogStructure.GetData()` into CSV |
| `UserInputRecorderUIController.cs` | MonoBehaviour | Record/playback UI button visibility controller |
| `UserInputRecorderFeedback.cs` | MonoBehaviour | Brief TMP status strings for record/replay lifecycle |
| `UserInputPlayback.cs` | MonoBehaviour | Loads CSV; parses gaze columns; raycasts to drive `DrawOnTexture` heatmap |
| `DrawOnTexture.cs` | MRTKBaseInteractable | Heatmap texture on renderer; live gaze or playback `DrawAtThisHitPos` |
| `AsyncHelpers.cs` | static class | UWP-only sync-over-async helper |

### Eye Tracking — Target Selection / Positioning

| Script | Extends | Purpose |
|---|---|---|
| `TargetGroupCreatorRadial.cs` | MonoBehaviour | Instantiates targets in rings at visual-angle sizes |
| `FaceUser.cs` | MonoBehaviour | Rotates object toward camera; lerps back on disable |
| `EyeTrackingTarget.cs` | MonoBehaviour | Hover spin coroutine; select plays audio/VFX and destroys |
| `TransportToRespawnLocation.cs` | MonoBehaviour | `OnTriggerEnter`: teleports to respawn point |
| `ObjectGoalZone.cs` | MonoBehaviour | Trigger-tracks target names; success color/audio |
| `MoveObjectByEyeGaze.cs` | StatefulInteractable | Grab + hand move + gaze placement; uses `HandsSubsystem` for pinch detection |

### Eye Tracking — Scroll / Pan / Zoom

| Script | Extends | Purpose |
|---|---|---|
| `PanZoomBase.cs` | StatefulInteractable (abstract) | Gaze cursor UV; auto-pan; hand zoom via `HandsSubsystem` + `FuzzyGazeInteractor` |
| `PanZoomBaseTexture.cs` | PanZoomBase | UV scale/offset on material; pan speed curve; zoom with pivot |
| `PanZoomTexture.cs` | PanZoomBaseTexture | Wires serialized fields into base via `ProcessInteractable` |
| `PanZoomBaseRectTransform.cs` | PanZoomBase | Pan/zoom by `RectTransform` anchored position + scale |
| `ScrollRectTransform.cs` | PanZoomBaseRectTransform | Eye-gaze-driven scrolling of nested UI |
| `OnLookAtRotateByEyeGaze.cs` | StatefulInteractable | Rotates object so looked-at region faces user |
| `TargetMoveToCamera.cs` | OnLookAtRotateByEyeGaze | Moves object to camera-front or home; keyword-triggered |

## Conventions

- All scripts use `MixedReality.Toolkit.Examples` or `MixedReality.Toolkit.Examples.Demos` namespace.
- Eye-tracking CSV column order matters: `UserInputPlayback` expects the exact column indices produced by `UserInputRecorder` + `LogStructureEyeGaze`.
- `InteractorBehaviorControls.interactionManager` is serialized but unused; all logic routes through `InteractionModeManager`.
- Whiteboard/Pen pair: `PenInteractor` implements `IPokeInteractor` for `Whiteboard` (an `MRTKBaseInteractable`).
