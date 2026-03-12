// Auto-generated — safe to delete after use
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FixShadowArtifacts
{
    [MenuItem("Tools/Fix Shadow Artifacts")]
    public static void Fix()
    {
        // ── 1. Fix Directional Light shadow settings ──
        var lights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.shadowBias = 0.05f;
                light.shadowNormalBias = 0.4f;
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
                light.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(light);
                Debug.Log($"[FixShadows] Directional light '{light.name}': bias=0.05, normalBias=0.4, Soft shadows, VeryHigh res");
            }
        }

        // ── 2. Fix URP Asset shadow settings ──
        string[] urpAssets = {
            "Assets/Settings/PC_RPAsset.asset",
            "Assets/Settings/Mobile_RPAsset.asset"
        };

        foreach (var assetPath in urpAssets)
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null) { Debug.LogWarning($"[FixShadows] Could not load: {assetPath}"); continue; }

            // Use reflection to set private fields Unity doesn't expose directly
            var type = typeof(UniversalRenderPipelineAsset);

            // Shadow distance — keep tight to maximize shadow map texel density
            SetField(type, urp, "m_ShadowDistance", 20f);

            // Cascade count — 2 for mobile, 4 for PC  
            bool isMobile = assetPath.Contains("Mobile");
            SetField(type, urp, "m_ShadowCascadeCount", isMobile ? 2 : 4);

            // Depth bias and normal bias
            SetField(type, urp, "m_ShadowDepthBias", 1.0f);
            SetField(type, urp, "m_ShadowNormalBias", 1.0f);

            // Main light shadow resolution: bump to 4096
            SetField(type, urp, "m_MainLightShadowmapResolution", 4096);

            EditorUtility.SetDirty(urp);
            Debug.Log($"[FixShadows] Updated URP asset: {assetPath}");
        }

        // ── 3. Ensure the Plane mesh has correct shadow casting ──
        var planeRenderers = GameObject.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in planeRenderers)
        {
            if (r.gameObject.name.ToLower().Contains("plane"))
            {
                r.receiveShadows = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                EditorUtility.SetDirty(r);
                Debug.Log($"[FixShadows] Plane '{r.gameObject.name}': receiveShadows=true, castShadows=Off");
            }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[FixShadows] Done.");
    }

    static void SetField(System.Type type, object obj, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, System.Convert.ChangeType(value, field.FieldType));
            Debug.Log($"[FixShadows] Set {fieldName} = {value}");
        }
        else
        {
            Debug.LogWarning($"[FixShadows] Field not found: {fieldName}");
        }
    }
}
