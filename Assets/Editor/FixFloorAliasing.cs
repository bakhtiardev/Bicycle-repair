// Auto-generated — safe to delete after use
using UnityEngine;
using UnityEditor;

public class FixFloorAliasing
{
    [MenuItem("Tools/Fix Floor Texture Aliasing")]
    public static void Fix()
    {
        // Find the Plane in the scene
        GameObject plane = GameObject.Find("Plane");
        if (plane == null) { Debug.LogError("[FixFloor] No GameObject named 'Plane' found!"); return; }

        var renderer = plane.GetComponent<MeshRenderer>();
        if (renderer == null) { Debug.LogError("[FixFloor] Plane has no MeshRenderer!"); return; }

        Material mat = renderer.sharedMaterial;
        if (mat == null) { Debug.LogError("[FixFloor] Plane has no material!"); return; }

        Debug.Log($"[FixFloor] Plane material: '{mat.name}', shader: '{mat.shader.name}'");

        // Log and fix all textures on the material
        int fixCount = 0;
        string[] texProps = mat.GetTexturePropertyNames();
        foreach (var prop in texProps)
        {
            Texture tex = mat.GetTexture(prop);
            if (tex == null) continue;

            string texPath = AssetDatabase.GetAssetPath(tex);
            Debug.Log($"[FixFloor] Texture '{prop}': {tex.name} at {texPath}");

            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            // Fix 1: Enable high anisotropic filtering (key fix for grazing angle moire)
            if (importer.anisoLevel < 8)
            {
                importer.anisoLevel = 8;
                changed = true;
                Debug.Log($"[FixFloor] {tex.name}: anisoLevel → 8");
            }

            // Fix 2: Ensure mipmaps are enabled
            if (!importer.mipmapEnabled)
            {
                importer.mipmapEnabled = true;
                changed = true;
                Debug.Log($"[FixFloor] {tex.name}: mipmaps enabled");
            }

            // Fix 3: Use best mipmap filter
            if (importer.mipmapFilter != TextureImporterMipFilter.KaiserFilter)
            {
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                changed = true;
                Debug.Log($"[FixFloor] {tex.name}: mipmap filter → Kaiser");
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                fixCount++;
            }
        }

        // Fix 4: Also check the tiling scale — if it's very high, reduce it
        if (mat.HasProperty("_BaseMap") || mat.HasProperty("_MainTex"))
        {
            string prop = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Vector2 tiling = mat.GetTextureScale(prop);
            Debug.Log($"[FixFloor] Material tiling: {tiling}");
            if (tiling.x > 20 || tiling.y > 20)
            {
                // Very high tiling causes moire — reduce but preserve ratio
                float scale = 10f / Mathf.Max(tiling.x, tiling.y);
                mat.SetTextureScale(prop, tiling * scale);
                EditorUtility.SetDirty(mat);
                Debug.Log($"[FixFloor] Reduced tiling from {tiling} to {mat.GetTextureScale(prop)}");
                fixCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixFloor] Done — {fixCount} fix(es) applied.");
    }
}
