// Auto-generated — safe to delete after use
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FixShadowAliasing
{
    [MenuItem("Tools/Fix Shadow Aliasing")]
public static void Fix()
    {
        int fixes = 0;

        // 1. Directional light: lower bias to stop stripe/acne
        var lights = GameObject.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type != LightType.Directional) continue;
            light.shadowBias = 0.01f;
            light.shadowNormalBias = 0.4f;
            light.shadows = LightShadows.Soft;
            EditorUtility.SetDirty(light);
            Debug.Log($"[FixShadow] Light '{light.gameObject.name}': bias→0.01, normalBias→0.4, Soft");
            fixes++;
        }

        // 2. URP Assets: high-res shadowmap, tight distance, soft shadow quality
        string[] urpPaths = { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" };
        string[] rendererPaths = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

        foreach (var path in urpPaths)
        {
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (urp == null) continue;
            var so = new SerializedObject(urp);

            var shadowDist = so.FindProperty("m_ShadowDistance");
            if (shadowDist != null) shadowDist.floatValue = 15f;

            var depthBias = so.FindProperty("m_ShadowDepthBias");
            if (depthBias != null) depthBias.floatValue = 0.5f;

            var normalBias = so.FindProperty("m_ShadowNormalBias");
            if (normalBias != null) normalBias.floatValue = 0.4f;

            var shadowRes = so.FindProperty("m_MainLightShadowmapResolution");
            if (shadowRes != null) shadowRes.intValue = 4096;

            var cascades = so.FindProperty("m_ShadowCascadeCount");
            if (cascades != null && cascades.intValue < 4) cascades.intValue = 4;

            // Soft shadow quality: 0=Disabled, 1=Low, 2=Medium, 3=High
            var softShadow = so.FindProperty("m_SoftShadowQuality");
            if (softShadow != null) { softShadow.intValue = 3; Debug.Log($"[FixShadow] {urp.name}: softShadowQuality → High"); }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);
            fixes++;
        }

        // 3. Renderer: enable soft shadows
        foreach (var path in rendererPaths)
        {
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rd == null) continue;
            var so = new SerializedObject(rd);
            var ss = so.FindProperty("m_SoftShadowQuality");
            if (ss != null) { ss.intValue = 3; Debug.Log($"[FixShadow] Renderer '{rd.name}': softShadowQuality → High"); }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rd);
            fixes++;
        }

        // 4. Plane: make sure it receives shadows
        GameObject plane = GameObject.Find("Plane");
        if (plane != null)
        {
            var mr = plane.GetComponent<MeshRenderer>();
            if (mr != null) { mr.receiveShadows = true; EditorUtility.SetDirty(mr); }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[FixShadow] Done — {fixes} fix(es) applied.");
    }
}
