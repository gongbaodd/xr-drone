---
name: xr-joystick-hud
description: Configure and maintain XRJoystickHud with scene-owned UI assets in Unity. Use when working on XR HUD quad setup, RenderTexture/PanelSettings wiring, camera-facing behavior, or simplifying XRJoystickHud null-check-heavy code.
---

# XR Joystick HUD

## Goal

Keep `XRJoystickHud` minimal and asset-driven:
- No runtime creation of quad/material/render texture/panel settings.
- Scene object + asset configuration handled in Unity editor (prefer MCP).
- Script only reads configuration and updates stick dots + camera-facing rotation.

## Required Scene/Asset Setup

1. `XRJoystickHud` GameObject has a child named `XR Joystick HUD Quad`.
2. Quad has a `Renderer` with a material asset (not default temp material).
3. Material `mainTexture` is a `RenderTexture` asset.
4. `UIDocument.panelSettings.targetTexture` points to the same `RenderTexture`.
5. `PanelSettings.colorClearValue` is transparent (`0,0,0,0`).

## Script Rules

- Keep null checks compact via readiness flags (for example `isUiReady`, `isHudReady`).
- Avoid verbose warning spam; fail quietly unless action is needed.
- Do not allocate/destroy rendering assets in `XRJoystickHud`.
- Ensure quad front faces camera:
  - Compute `toCamera = camera.position - quad.position`
  - Use `Quaternion.LookRotation(-toCamera.normalized, camera.up)` for Unity quad front side.

## MCP Workflow (Unity_RunCommand)

When setup is missing, use editor automation to:
1. Find all `XRJoystickHud` in open scenes.
2. Ensure child quad exists.
3. Ensure material asset exists (create once under `Assets/Settings/UI/` if missing).
4. Ensure a `RenderTexture` asset exists and is assigned to material `mainTexture`.
5. Assign `PanelSettings.targetTexture` to that same render texture.
6. Set `PanelSettings.colorClearValue` transparent.
7. Save assets.

## Refactor Checklist

- [ ] Remove runtime object/asset creation code.
- [ ] Remove unnecessary serialized config fields.
- [ ] Keep component responsibilities narrow: bind UI, update dots, rotate quad to camera.
- [ ] Validate with lint and Unity console errors after edits.
