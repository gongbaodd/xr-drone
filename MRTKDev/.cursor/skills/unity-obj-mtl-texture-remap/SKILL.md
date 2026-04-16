---
name: unity-obj-mtl-texture-remap
description: >-
  Repairs Unity OBJ+MTL imports where diffuse textures from map_Kd are not wired
  into URP Lit (_BaseMap / _MainTex stay NULL). Use when imported Wavefront OBJ
  looks untextured or wrong, MTL lists map_Kd next to textures in the same
  folder, or when diagnosing materials via Unity MCP shows NULL albedo maps on
  Universal Render Pipeline/Lit.
---

# Unity OBJ/MTL → URP Lit texture remap

## When this applies

- **Symptom**: Building or prop from `.obj` + `.mtl` + sidecar `.jpg`/`.png` looks flat, grey, or uniformly wrong; albedo missing in Scene/Game view.
- **Project context**: URP (e.g. Lit shader). OBJ importer often uses **ImportViaMaterialDescription** and still leaves **`_BaseMap` and `_MainTex` unset** even when `map_Kd` lines in the MTL point at existing files next to the asset.
- **Confirm**: Use **Unity MCP** `Unity_RunCommand` to load `AssetDatabase.LoadAllAssetsAtPath(objPath)` and log each `Material`: if `GetTexture("_BaseMap")` and `_MainTex` are both null for most slots, this workflow fits.

## Root cause (short)

Unity’s OBJ pipeline created **URP Lit** materials from the MTL but did **not** resolve `map_Kd` into the Lit base color map. Textures on disk are fine; the **material asset references** are wrong.

## Fix strategy

1. **Parse the MTL** on disk (under `Application.dataPath`, e.g. `Assets/...` mirrored as `.../MyFolder/model.mtl`):
   - For each `newmtl <name>`, remember current material name.
   - On `map_Kd <filename>`, record `materialName → filename` (trim; handle tabs from 3ds Max export).

2. **Ensure a sibling `Materials` folder** under the OBJ’s asset folder, e.g. `Assets/.../MyModel/Materials`.

3. **For each embedded `Material`** from `AssetDatabase.LoadAllAssetsAtPath(objPath)`:
   - `new Material(existingMat)` (copy).
   - If MTL has a mapping, `AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + filename)` and assign **`SetTexture("_BaseMap", tex)`** and **`SetTexture("_MainTex", tex)`** for compatibility.
   - `AssetDatabase.CreateAsset(newMat, matFolder + "/" + mat.name + ".mat")`.

4. **Rewire the importer** (Editor-only, same session as MCP `Unity_RunCommand`):
   - `var importer = AssetImporter.GetAtPath(objPath) as ModelImporter;`
   - `importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;` (or keep existing if already correct).
   - `importer.materialLocation = ModelImporterMaterialLocation.External;`
   - For each embedded material: `importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), mat.name), externalMat);`
   - `importer.SaveAndReimport();` then `AssetDatabase.SaveAssets();` / `Refresh` as needed.

5. **Repeat** for alternate meshes (e.g. `model-origo.obj`) that share the same MTL names; **reuse** the same external `.mat` files when material names match.

## Materials without textures

If MTL has **no** `map_Kd` for a `newmtl` (e.g. pure `Kd` color), leave that external material **without** a base map — that is expected.

## Unity MCP constraints

- Use tool **`Unity_RunCommand`** with a single class named **`CommandScript`**, **`internal`**, implementing **`IRunCommand`** and `Execute(ExecutionResult result)`.
- Register edits with `result.RegisterObjectCreation` / `result.RegisterObjectModification` / `result.DestroyObject` per the MCP template when creating or destroying Unity objects (material assets created via `AssetDatabase.CreateAsset` are still assets — follow the host’s rules for `result` if required).

## Verification

After reimport, either:

- Re-run a small `Unity_RunCommand` that logs `MeshRenderer.sharedMaterials` on scene instances of the prefab, checking `_BaseMap` is non-null where MTL had `map_Kd`, or  
- Inspect one external `.mat` in the Editor: **Base Map** slot filled.

## Paths and naming

- Use **forward slashes** in `AssetDatabase` paths (`Assets/...`).
- MTL texture lines are often **bare filenames**; resolve against the **same folder** as the OBJ unless the MTL uses relative subpaths.

## Related project notes

- This repo targets **Unity 6 + URP**; Lit expects `_BaseMap`, not only legacy `_MainTex`.
- For package/rendering context, see `.cursor/rules/mrtkdev-project-context.mdc`.
