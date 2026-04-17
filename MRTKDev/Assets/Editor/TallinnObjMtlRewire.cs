using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Full pass: Assets/Tallinn Wavefront OBJ+MTL → external URP Lit materials with map_Kd on _BaseMap/_MainTex,
/// ModelImporter external remaps. Handles stem matching (e.g. P2_wall1.jpg ↔ material name) and single-material fallbacks.
/// </summary>
public static class TallinnObjMtlRewire
{
    const string TallinnRoot = "Assets/Tallinn";
    const string MenuPath = "Tallinn/Rewire All OBJ MTL Materials (URP Lit)";
    /// <summary>Single-asset path for MCP <c>Unity.RunCommand</c> (see <see cref="RewireVanaViru15Only"/>).</summary>
    public const string VanaViru15ObjPath = "Assets/Tallinn/Vana-Viru 15/Vana-Viru_15.obj";

    [MenuItem(MenuPath, false, 1000)]
    public static void RewireAll()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            EditorUtility.DisplayDialog("Tallinn OBJ rewire", "URP Lit shader not found. Is URP installed?", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("", new[] { TallinnRoot });
        var objPaths = new List<string>();
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                objPaths.Add(p);
        }

        objPaths.Sort(StringComparer.Ordinal);
        int total = objPaths.Count;
        int ok = 0, skipped = 0, errors = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < objPaths.Count; i++)
            {
                var objPath = objPaths[i];
                if (EditorUtility.DisplayCancelableProgressBar("Tallinn OBJ rewire", objPath, (float)i / total))
                    break;

                try
                {
                    if (ProcessOneObj(objPath, lit))
                        ok++;
                    else
                        skipped++;
                }
                catch (Exception ex)
                {
                    errors++;
                    Debug.LogError($"[TallinnObjMtlRewire] {objPath}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[TallinnObjMtlRewire] Done. Processed OK: {ok}, skipped: {skipped}, errors: {errors}, total .obj: {total}.");
    }

    /// <summary>
    /// Rewires <see cref="VanaViru15ObjPath"/> only (MTL map_Kd → URP Lit external materials + importer remaps).
    /// Call from Unity MCP tool <c>Unity.RunCommand</c> using the golden <c>CommandScript</c> template, e.g.:
    /// <code>
    /// using UnityEngine;
    /// using UnityEditor;
    ///
    /// internal class CommandScript : IRunCommand
    /// {
    ///     public void Execute(ExecutionResult result)
    ///     {
    ///         TallinnObjMtlRewire.RewireVanaViru15Only();
    ///         result.Log("Rewired Vana-Viru_15.obj materials from MTL.");
    ///     }
    /// }
    /// </code>
    /// </summary>
    [MenuItem("Tallinn/Rewire Vana-Viru_15.obj (MTL)", false, 999)]
    public static void RewireVanaViru15Only()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            EditorUtility.DisplayDialog("Tallinn OBJ rewire", "URP Lit shader not found. Is URP installed?", "OK");
            return;
        }

        AssetDatabase.StartAssetEditing();
        try
        {
            if (!ProcessOneObj(VanaViru15ObjPath, lit))
                Debug.LogWarning($"[TallinnObjMtlRewire] RewireVanaViru15Only: skipped or failed for {VanaViru15ObjPath}");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[TallinnObjMtlRewire] RewireVanaViru15Only finished for {VanaViru15ObjPath}.");
    }

    static bool ProcessOneObj(string objAssetPath, Shader litShader)
    {
        var folder = Path.GetDirectoryName(objAssetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(folder))
            return false;

        var fullObj = ToFullPath(objAssetPath);
        if (!File.Exists(fullObj))
            return false;

        if (!TryParseObjHeader(fullObj, out var mtllibFile, out var usemtlOrder))
            return false;

        if (string.IsNullOrEmpty(mtllibFile))
            mtllibFile = Path.GetFileNameWithoutExtension(objAssetPath) + ".mtl";

        var mtlAssetPath = folder + "/" + mtllibFile.Replace('\\', '/');
        var fullMtl = ToFullPath(mtlAssetPath);
        if (!File.Exists(fullMtl))
        {
            Debug.LogWarning($"[TallinnObjMtlRewire] Missing MTL for {objAssetPath}: {mtlAssetPath}");
            return false;
        }

        ParseMtlFile(fullMtl, out var nameToMapKd, out var nameToKd, out var firstMapKdInMtlOrder, out var firstKdInMtlOrder);
        if (nameToMapKd.Count == 0 && nameToKd.Count == 0)
            return false;

        var usemtlOrderUnique = new List<string>();
        var seenUsemtl = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in usemtlOrder)
        {
            if (seenUsemtl.Add(u))
                usemtlOrderUnique.Add(u);
        }

        string firstUsemtl = usemtlOrder.Count > 0 ? usemtlOrder[0] : null;

        var materialsFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            var parent = Path.GetDirectoryName(materialsFolder)?.Replace('\\', '/');
            var leaf = Path.GetFileName(materialsFolder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        var importer = AssetImporter.GetAtPath(objAssetPath) as ModelImporter;
        if (importer == null)
            return false;

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        // Must match working imports (e.g. Vana-Viru_15-origo.obj): name slots from MTL/usemtl, then apply remaps.
        // BasedOnTextureName + RecursiveUp breaks externalObject material remaps when map_Kd filenames differ from material names.
        importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        importer.materialSearch = ModelImporterMaterialSearch.Everywhere;

        var subMaterials = AssetDatabase.LoadAllAssetsAtPath(objAssetPath).OfType<Material>().ToArray();
        IEnumerable<string> modelMaterialNames;
        if (subMaterials.Length > 0)
            modelMaterialNames = subMaterials.Select(m => m.name);
        else if (usemtlOrderUnique.Count > 0)
            modelMaterialNames = usemtlOrderUnique;
        else
            return false;

        int uniqueCount = subMaterials.Length > 0 ? subMaterials.Length : usemtlOrderUnique.Count;

        foreach (var matName in modelMaterialNames)
        {
            if (string.IsNullOrEmpty(matName))
                continue;

            var mapFile = ResolveMapKd(matName, nameToMapKd, firstUsemtl, uniqueCount, firstMapKdInMtlOrder);
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(mapFile))
                tex = LoadTextureCaseInsensitive(folder, mapFile);

            var kd = ResolveKd(matName, nameToKd, firstUsemtl, uniqueCount, firstKdInMtlOrder);
            var extPath = materialsFolder + "/" + SanitizeFileName(matName) + ".mat";

            var mat = AssetDatabase.LoadAssetAtPath<Material>(extPath);
            if (mat == null)
            {
                mat = new Material(litShader) { name = matName };
                AssetDatabase.CreateAsset(mat, extPath);
            }
            else
            {
                mat.name = matName;
                mat.shader = litShader;
            }

            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", null);
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", null);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", kd);
            }

            EditorUtility.SetDirty(mat);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), matName), mat);
        }

        importer.SaveAndReimport();
        return true;
    }

    static string ResolveMapKd(
        string matName,
        Dictionary<string, string> nameToMapKd,
        string firstUsemtl,
        int uniqueMaterialCount,
        string firstMapKdInMtlOrder)
    {
        if (nameToMapKd.TryGetValue(matName, out var fn))
            return fn;

        foreach (var kv in nameToMapKd)
        {
            if (string.Equals(kv.Key, matName, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        foreach (var kv in nameToMapKd)
        {
            var stem = Path.GetFileNameWithoutExtension(kv.Value);
            if (string.Equals(stem, matName, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        if (uniqueMaterialCount == 1 && !string.IsNullOrEmpty(firstUsemtl))
        {
            if (nameToMapKd.TryGetValue(firstUsemtl, out fn))
                return fn;
            foreach (var kv in nameToMapKd)
            {
                if (string.Equals(kv.Key, firstUsemtl, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
        }

        if (uniqueMaterialCount == 1 && !string.IsNullOrEmpty(firstMapKdInMtlOrder))
            return firstMapKdInMtlOrder;

        return null;
    }

    static Color ResolveKd(
        string matName,
        Dictionary<string, Color> nameToKd,
        string firstUsemtl,
        int uniqueMaterialCount,
        Color? firstKdInMtlOrder)
    {
        var grey = new Color(0.75f, 0.75f, 0.75f, 1f);
        if (nameToKd.TryGetValue(matName, out var c))
            return c;
        foreach (var kv in nameToKd)
        {
            if (string.Equals(kv.Key, matName, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        if (uniqueMaterialCount == 1 && !string.IsNullOrEmpty(firstUsemtl) && nameToKd.TryGetValue(firstUsemtl, out c))
            return c;

        if (uniqueMaterialCount == 1 && firstKdInMtlOrder.HasValue)
            return firstKdInMtlOrder.Value;

        return grey;
    }

    static bool TryParseObjHeader(string fullObjPath, out string mtllibFile, out List<string> usemtlOrder)
    {
        mtllibFile = null;
        usemtlOrder = new List<string>();

        foreach (var raw in File.ReadLines(fullObjPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line.StartsWith("mtllib", StringComparison.OrdinalIgnoreCase))
            {
                var rest = line.Substring(6).Trim();
                if (!string.IsNullOrEmpty(rest))
                    mtllibFile = rest;
                continue;
            }

            if (line.StartsWith("usemtl", StringComparison.OrdinalIgnoreCase))
            {
                var name = line.Substring(6).Trim();
                if (string.IsNullOrEmpty(name))
                    continue;
                usemtlOrder.Add(name);
            }
        }

        return true;
    }

    static void ParseMtlFile(
        string fullMtlPath,
        out Dictionary<string, string> nameToMapKd,
        out Dictionary<string, Color> nameToKd,
        out string firstMapKdInMtlOrder,
        out Color? firstKdInMtlOrder)
    {
        nameToMapKd = new Dictionary<string, string>(StringComparer.Ordinal);
        nameToKd = new Dictionary<string, Color>(StringComparer.Ordinal);
        firstMapKdInMtlOrder = null;
        firstKdInMtlOrder = null;

        string current = null;
        foreach (var raw in File.ReadLines(fullMtlPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (line.StartsWith("newmtl", StringComparison.OrdinalIgnoreCase))
            {
                current = line.Substring(6).Trim();
                continue;
            }

            if (string.IsNullOrEmpty(current))
                continue;

            if (line.StartsWith("map_Kd", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("map_kd", StringComparison.OrdinalIgnoreCase))
            {
                var file = ExtractMapKdFilename(line);
                if (!string.IsNullOrEmpty(file))
                {
                    nameToMapKd[current] = file;
                    if (firstMapKdInMtlOrder == null)
                        firstMapKdInMtlOrder = file;
                }

                continue;
            }

            if (line.StartsWith("Kd", StringComparison.OrdinalIgnoreCase))
            {
                var nums = Regex.Matches(line, @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?");
                if (nums.Count >= 3 &&
                    float.TryParse(nums[0].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var r) &&
                    float.TryParse(nums[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var g) &&
                    float.TryParse(nums[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var b))
                {
                    var col = new Color(r, g, b, 1f);
                    nameToKd[current] = col;
                    if (firstKdInMtlOrder == null)
                        firstKdInMtlOrder = col;
                }
            }
        }
    }

    static string ExtractMapKdFilename(string line)
    {
        var idx = line.IndexOf("map_Kd", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        var rest = line.Substring(idx + 6).Trim();
        if (rest.Length == 0)
            return null;

        // 3ds Max / many exporters: filename may contain spaces ("Vana-Viru tn 15 tex9.jpg").
        // Do NOT take only the last token — that becomes "tex9.jpg" and fails to resolve on disk.
        if (!rest.StartsWith("-", StringComparison.Ordinal))
            return rest.Replace('\\', '/');

        // Wavefront optional flags (-o u v w, -s u v w, -clamp on, …) then filename (often last token).
        var parts = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        return parts[parts.Length - 1].Replace('\\', '/');
    }

    static Texture2D LoadTextureCaseInsensitive(string folderAssetPath, string fileName)
    {
        var rel = folderAssetPath + "/" + fileName.TrimStart('/');
        if (File.Exists(ToFullPath(rel)))
            return AssetDatabase.LoadAssetAtPath<Texture2D>(rel);

        var baseName = Path.GetFileName(fileName);
        var fullFolder = ToFullPath(folderAssetPath);
        if (!Directory.Exists(fullFolder))
            return null;

        foreach (var path in Directory.GetFiles(fullFolder))
        {
            if (string.Equals(Path.GetFileName(path), baseName, StringComparison.OrdinalIgnoreCase))
            {
                var assetPath = folderAssetPath + "/" + Path.GetFileName(path);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
        }

        // 3ds Max MTL: map_Kd "Vana-Viru tn 15 tex9.jpg" → on disk "Vana-Viru_tn_15_tex9.jpg"
        var altBase = baseName.Replace(' ', '_');
        if (!string.Equals(altBase, baseName, StringComparison.Ordinal))
        {
            var altRel = folderAssetPath + "/" + altBase;
            if (File.Exists(ToFullPath(altRel)))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(altRel);

            foreach (var path in Directory.GetFiles(fullFolder))
            {
                if (string.Equals(Path.GetFileName(path), altBase, StringComparison.OrdinalIgnoreCase))
                {
                    var assetPath = folderAssetPath + "/" + Path.GetFileName(path);
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
            }
        }

        return null;
    }

    static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "material" : name.Trim();
    }

    static string ToFullPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;
        var rel = assetPath.StartsWith("Assets/", StringComparison.Ordinal) ? assetPath.Substring(7) : assetPath;
        return Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
    }
}
