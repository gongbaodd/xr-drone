---
name: meta-quest-unity-docs
description: Provides a Meta Quest Unity documentation skill with a curated index and retrieval workflow. Use when the user asks for Meta Quest Unity docs, Horizon Unity SDK guidance, Interaction SDK help, MR/VR feature docs, performance optimization docs, or publishing guidance.
---

# Meta Quest Unity Docs

## Purpose

Use this skill to answer questions about Meta Horizon / Meta Quest Unity development by using:

- The master docs hub: `https://developers.meta.com/horizon/develop/unity/`
- The local docs index in `reference.md`

## Workflow

1. Clarify the user's intent (setup, SDK feature, tooling, optimization, publishing, or troubleshooting).
2. Start with `reference.md` to identify the most relevant official doc URLs.
3. Prefer current docs; avoid links explicitly marked as deprecated, legacy, retired, or outdated unless requested.
4. Fetch the target docs page(s) and summarize actionable guidance.
5. Return:
   - Direct official links
   - A concise step-by-step answer tailored to the current task
   - Warnings for version/deprecation pitfalls

## Priority Topics

- Getting started and environment setup
- Interaction SDK and input
- Mixed Reality Utility Kit and passthrough
- Performance optimization and profiling
- Build/test tooling and publishing

## Response Style

- Keep answers implementation-oriented and concise.
- Include exact Unity/Quest feature names from docs.
- If docs are ambiguous, provide the likely path and list assumptions.

## Additional Resource

- Full index and categorized links: `reference.md`
