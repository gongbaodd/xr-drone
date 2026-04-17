---
name: unity-obj-mtl-texture-remap
description: >-
  Repairs Unity OBJ+MTL imports where diffuse textures from map_Kd are not wired
  into URP Lit (_BaseMap / _MainTex stay NULL), or where materials look wrong even
  when textures exist. Covers 3ds Max map_Kd lines with spaces in filenames,
  MTL vs on-disk underscore naming, and ModelImporter materialName/materialSearch
  so external material remaps match usemtl names. Use when imports look grey,
  MCP shows NULL _BaseMap, or one OBJ variant is wrong while a sibling (e.g.
  *-origo.obj) is correct.
---

# Unity OBJ/MTL → URP Lit texture remap

## When this applies

- **Symptom**: Building or prop from `.obj` + `.mtl` + sidecar `.jpg`/`.png` looks flat, grey, or uniformly wrong; albedo missing in Scene/Game view.
- **Symptom**: One mesh (e.g. `Building.obj`) is wrong while **`Building-origo.obj`** in the same folder looks correct — compare **ModelImporter** settings and remaps, not only materials.
- **Project context**: URP (e.g. Lit shader). OBJ importer often uses **ImportViaMaterialDescription** and still leaves **`_BaseMap` and `_MainTex` unset** even when `map_Kd` lines in the MTL point at existing files next to the asset.
- **Confirm**: Use **Unity MCP** `Unity.RunCommand` to load `AssetDatabase.LoadAllAssetsAtPath(objPath)` and log each `Material`: if `GetTexture("_BaseMap")` and `_MainTex` are both null for most slots, this workflow fits.

## Root cause (short)

Unity’s OBJ pipeline created **URP Lit** materials from the MTL but did **not** resolve `map_Kd` into the Lit base color map. Textures on disk are fine; the **material asset references** are wrong.

**Additional failure modes (common on Tallinn / 3ds Max exports):**

1. **`map_Kd` parsing**: If code takes only the **last whitespace-separated token** after `map_Kd`, lines like `map_Kd Vana-Viru tn 15 tex9.jpg` resolve to **`tex9.jpg`** instead of the full name — texture lookup fails or grabs the wrong file.
2. **MTL vs disk**: Exporters often put **spaces** in `map_Kd`; Unity/imported assets often use **underscores** in the real filename (`Vana-Viru_tn_15_tex9.jpg`). Resolve by trying the folder path + basename, then **basename with spaces → underscores**.
3. **ModelImporter naming vs remaps**: `externalObjects` remaps in `*.obj.meta` key materials by **name** (from MTL / `usemtl`). If **`materialName`** is **BasedOnTextureName** (serialized `0`), Unity names internal material slots from **texture filenames**, not from **`usemtl` / `newmtl` names** — **remap entries no longer match**, so the wrong materials apply even when `.mat` assets are correct. Align with a known-good import: **`materialName: BasedOnMaterialName` (typically serialized `1`)** and **`materialSearch: Everywhere` (typically serialized `2`)** when remaps use MTL material names (verify against a working `*-origo.obj` meta).

## Fix strategy

1. **Parse the MTL** on disk (under `Application.dataPath`, e.g. `Assets/...` mirrored as `.../MyFolder/model.mtl`):
   - For each `newmtl <name>`, remember current material name.
   - On `map_Kd`, record the **full diffuse filename** after `map_Kd` (trim). If the remainder does **not** start with `-` (Wavefront options), treat the **entire remainder** as the filename so **spaces** are preserved. Only fall back to “last token” when options like `-o u v w` precede the file.

2. **Ensure a sibling `Materials` folder** under the OBJ’s asset folder, e.g. `Assets/.../MyModel/Materials`.

3. **For each embedded `Material`** from `AssetDatabase.LoadAllAssetsAtPath(objPath)`:
   - `new Material(existingMat)` (copy).
   - If MTL has a mapping, load `Texture2D` from the asset folder + filename; assign **`SetTexture("_BaseMap", tex)`** and **`SetTexture("_MainTex", tex)`** for compatibility.
   - `AssetDatabase.CreateAsset(newMat, matFolder + "/" + mat.name + ".mat")`.

4. **Rewire the importer** (Editor-only, same session as MCP `Unity.RunCommand`):
   - `var importer = AssetImporter.GetAtPath(objPath) as ModelImporter;`
   - `importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;` (or keep existing if already correct).
   - `importer.materialLocation = ModelImporterMaterialLocation.External;`
   - **`importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;`**
   - **`importer.materialSearch = ModelImporterMaterialSearch.Everywhere;`** (or match your project’s known-good OBJ meta)
   - For each material slot name from the model: `importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), externalMat);`
   - `importer.SaveAndReimport();` then `AssetDatabase.SaveAssets();` / `Refresh` as needed.

5. **Repeat** for alternate meshes (e.g. `model-origo.obj`) that share the same MTL names; **reuse** the same external `.mat` files when material names match.

6. **Meta alignment**: If a sibling `*.obj` is correct, copy the **`materials:`** block’s **`materialName`** / **`materialSearch`** (and remaps if needed) from its `.meta` onto the broken asset, then reimport.

## Project helper (MRTKDev)

- **`Assets/Editor/TallinnObjMtlRewire.cs`**: scans `Assets/Tallinn` OBJs, parses MTL (including full `map_Kd` paths with spaces), resolves textures (including space → underscore), writes URP Lit externals, sets **BasedOnMaterialName** + **Everywhere**, and **`AddRemap`**. Menu: **Tallinn/Rewire All OBJ MTL Materials (URP Lit)**; single-asset menus: **192_Sauna_3** (`Sauna3_192ObjPath`), **Viru_16** (`Viru16ObjPath`), **Viru_22** (`Viru22ObjPath`), **Myyrivahe_myyr** (`MyyrivaheMyyrObjGuid` / `GetMyyrivaheMyyrObjPath()` — avoids hardcoding Unicode folder names), **Vana_Viru_10**, **Vana-viru_13**, **Vana-Viru_15**; arbitrary path: **`RewireObjAtPath("Assets/.../file.obj")`**. Requires **`com.unity.render-pipelines.universal`** (see **unity-openxr-urp-missing-universal** if `Universal Render Pipeline/Lit` is missing / CS0234).

## Materials without textures

If MTL has **no** `map_Kd` for a `newmtl` (e.g. pure `Kd` color), leave that external material **without** a base map — that is expected.

## Unity MCP constraints

- Use tool **`Unity.RunCommand`** with a single class named **`CommandScript`**, **`internal`**, implementing **`IRunCommand`** and `Execute(ExecutionResult result)`.
- Register edits with `result.RegisterObjectCreation` / `result.RegisterObjectModification` / `result.DestroyObject` per the MCP template when creating or destroying Unity objects (material assets created via `AssetDatabase.CreateAsset` are still assets — follow the host’s rules for `result` if required).
- Example: call `TallinnObjMtlRewire.RewireVanaViru15Only()` inside `Execute`, then `result.Log(...)`.

## Verification

After reimport, either:

- Re-run a small `Unity.RunCommand` that logs `MeshRenderer.sharedMaterials` on scene instances of the prefab, checking `_BaseMap` is non-null where MTL had `map_Kd`, or  
- Inspect one external `.mat` in the Editor: **Base Map** slot filled.
- Compare **`Vana-Viru_15.obj` vs `Vana-Viru_15-origo.obj`** (or any sibling): same **`usemtl`** order and same **remap GUIDs**; **`materialName` / `materialSearch`** on the broken asset should match the working one.

## Paths and naming

- Use **forward slashes** in `AssetDatabase` paths (`Assets/...`).
- MTL texture lines are often **bare filenames**; resolve against the **same folder** as the OBJ unless the MTL uses relative subpaths.
- **Never** reduce `map_Kd` to “last word only” unless you have parsed Wavefront option flags; multi-word filenames are valid.

## Related project notes

- This repo targets **Unity 6 + URP**; Lit expects `_BaseMap`, not only legacy `_MainTex`.
- For package/rendering context, see `.cursor/rules/mrtkdev-project-context.mdc`.
