# Agent and editor context

Machine-readable project notes for assistants and tools:

- **Cursor (always):** `.cursor/rules/mrtkdev-project-context.mdc` — project-wide rendering/package context.
- **Cursor (file-specific):**
  - `.cursor/rules/drone-xr-emulator-input.mdc` — `LimitedDroneEmulator.cs` XR mapping, 0..1 throttle mapping, arming gate behavior.
  - `.cursor/rules/controller-scene-flight-volume.mdc` — `ControllerScene.unity` collider-only flight volume + separate glass visual convention.

Open the relevant rule(s) above and keep them updated when rendering, packages, or flight tooling changes.
