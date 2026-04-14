---
name: unity-video-canvas
description: CAUTION FILE — documents FAILED attempts at video playback on Canvas UI with MRTK Graphics Tools. Use when working with VideoPlayer on Canvas, RawImage/Image video display, round corners on video, or RoundedRectMask2D with video. READ THIS FIRST to avoid repeating known failures.
---

# Unity Video on Canvas UI — KNOWN FAILURES

> **CAUTION**: Every approach below was attempted and FAILED. Do NOT repeat them.
> If you need to implement video on Canvas, ASK THE USER for guidance first.
> Do NOT guess or try these approaches again.

## WRONG Approach 1: Code-based material swap in VideoPanelPlayer.cs

**What was tried**: Adding runtime code in `VideoPanelPlayer.Awake()` to find the `Graphics Tools/Standard Canvas` shader via `Shader.Find()`, create a new `Material`, and assign it to `rawImage.material`.

**Result**: FAILED. Video did not render.

> CAUTION: Do not write C# code to swap materials at runtime for video display.

## WRONG Approach 2: Create a Graphics Tools material asset and assign to RawImage via MCP

**What was tried**: Created `Assets/Materials/VideoSurfaceRoundedCanvas.mat` using `Graphics Tools/Standard Canvas` shader with `_ROUND_CORNERS` enabled. Assigned it to the `RawImage` on VideoSurface in the Instruct prefab via `Unity_RunCommand`.

**Result**: FAILED. Video completely stopped rendering. The Graphics Tools shader does not work with `RawImage`'s runtime texture assignment (`rawImage.texture = videoPlayer.texture`).

> CAUTION: Do NOT assign Graphics Tools/Standard Canvas material to a RawImage that displays video. The video texture will not show.

## WRONG Approach 3: Fixing material properties (_USE_WORLD_SCALE, radius, keywords)

**What was tried**: Disabled `_USE_WORLD_SCALE`, `_VERTEX_COLORS`, `_SPECULAR_HIGHLIGHTS`; removed `_UI_CLIP_RECT_ROUNDED` keyword; adjusted `_RoundCornerRadius` from 8 to 0.025.

**Result**: FAILED. Video still did not render regardless of material property tweaks.

> CAUTION: Tweaking Graphics Tools material properties does not fix the fundamental incompatibility with RawImage video texture.

## WRONG Approach 4: Adding extra GameObjects (VideoSurface child under Image Mask)

**What was tried**: Created a new `VideoSurface` child GameObject under `Image Mask` with `RawImage` + Graphics Tools material, then wired `VideoPanelPlayer.rawImage` to it.

**Result**: FAILED. Added unnecessary complexity. The user explicitly said: "you do not need so many GameObjects to make me confused."

> CAUTION: Do NOT add extra GameObjects to solve video display issues. Work with the existing hierarchy.

## WRONG Approach 5: Rewriting VideoPanelPlayer.cs from RawImage to Image + RenderTexture

**What was tried**: Replaced `RawImage rawImage` field with `Image videoImage` (resolved at runtime from `videoPlayerObject`). Changed from `VideoRenderMode.APIOnly` to `VideoRenderMode.RenderTexture`. Created RenderTexture dynamically in `OnPrepared`, set `videoPlayer.targetTexture = rt` and `image.material.mainTexture = rt`.

**Result**: FAILED. This was wrong — the code change was not the correct fix.

> CAUTION: Do NOT rewrite VideoPanelPlayer.cs field types or render mode without understanding the actual hierarchy and what the user expects.

## WRONG Approach 6: RoundedRectMask2D on parent VideoFrame

**What was tried**: Adding `RoundedRectMask2D` to VideoFrame as a parent mask to clip the child RawImage with rounded corners.

**Result**: FAILED. `RoundedRectMask2D` only works with Graphics Tools shaders. The default `UI/Default` shader on the RawImage ignores the `_UI_CLIP_RECT_ROUNDED` keyword, so no clipping happens. And assigning a Graphics Tools material breaks video rendering (see Approach 2).

> CAUTION: `RoundedRectMask2D` + video `RawImage` is a dead end from both directions.

## Summary of What Does NOT Work

| Approach | Why it fails |
|----------|-------------|
| Graphics Tools material on RawImage | Video texture not passed through shader |
| `_USE_WORLD_SCALE = 1` on Canvas | Clips everything on small Canvas elements |
| Adding child GameObjects for video | Unnecessary complexity, user rejected |
| Rewriting RawImage → Image + RenderTexture | Wrong fix direction |
| RoundedRectMask2D on RawImage parent | Requires Graphics Tools material which breaks video |
| Runtime material creation via code | Does not solve the rendering issue |

## What To Do Instead

**ASK THE USER.** The correct approach was not discovered in this session. Do not guess. The user has domain knowledge about how their Instruct panel and MRTK Graphics Tools setup should work together.

Before making any changes:
1. Read the current scene hierarchy via MCP (`Unity_RunCommand`)
2. Read `VideoPanelPlayer.cs` to understand current state
3. Ask the user what approach they want — do NOT assume
