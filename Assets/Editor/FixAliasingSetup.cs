// Auto-generated — safe to delete after use
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FixAliasingSetup
{
    [MenuItem("Tools/Fix Specular Aliasing")]
public static void Fix()
    {
        int fixes = 0;

        // 1. SMAA on all URP cameras
        var camDatas = GameObject.FindObjectsByType<UniversalAdditionalCameraData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cd in camDatas)
        {
            cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cd.antialiasingQuality = AntialiasingQuality.High;
            EditorUtility.SetDirty(cd);
            Debug.Log($"[FixAliasing] SMAA High on: {cd.gameObject.name}");
            fixes++;
        }

        // 2. Global Volume with ACES tonemapping
        var vol = GameObject.FindFirstObjectByType<Volume>();
        if (vol == null)
        {
            var go = new GameObject("Global Volume (AA Fix)");
            vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100;
            Debug.Log("[FixAliasing] Created Global Volume.");
        }
        if (vol.profile == null)
        {
            vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(vol.profile, "Assets/Scenes/BikeRepairDemo/AntiAliasingVolume.asset");
        }
        if (!vol.profile.TryGet<Tonemapping>(out _))
        {
            var tm = vol.profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);
            fixes++;
            Debug.Log("[FixAliasing] Added ACES Tonemapping.");
        }
        EditorUtility.SetDirty(vol);
        EditorUtility.SetDirty(vol.profile);

        // 3. Fix MRBike custom shader materials — raise _Roughness, cap _Ks
        string[] matPaths = {
            "Assets/Models/MRBike/Materials/Bike/ShinyMetal.mat",
            "Assets/Models/MRBike/Materials/Bike/Rim.mat",
            "Assets/Models/MRBike/Materials/Bike/Hub.mat",
            "Assets/Models/MRBike/Materials/Bike/RearHub.mat",
            "Assets/Models/MRBike/Materials/Bike/Rotor.mat",
            "Assets/Models/MRBike/Materials/Bike/GoldCassette.mat",
            "Assets/Models/MRBike/Materials/Bike/Chain.mat",
            "Assets/Models/MRBike/Materials/Bike/Fork.mat",
            "Assets/Models/MRBike/Materials/Bike/SeatpostKashima.mat",
            "Assets/Models/MRBike/Materials/Bike/Derailleur.mat",
            "Assets/Models/MRBike/Materials/Bike/SatinBlack.mat",
            "Assets/Models/MRBike/Materials/Bike/BlackPlastic.mat",
            "Assets/Models/MRBike/Materials/Bike/TopCaps.mat",
            "Assets/Models/MRBike/Materials/Bike/ShockBody.mat",
            "Assets/Models/MRBike/Materials/FrameMat.mat",
        };

        foreach (var path in matPaths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            bool changed = false;

            if (mat.HasProperty("_Roughness"))
            {
                float r = mat.GetFloat("_Roughness");
                if (r < 0.18f) { mat.SetFloat("_Roughness", 0.18f); Debug.Log($"[FixAliasing] {mat.name} _Roughness {r:F3}→0.18"); changed = true; }
            }
            if (mat.HasProperty("_OrthoRoughness"))
            {
                float r = mat.GetFloat("_OrthoRoughness");
                if (r < 0.18f) { mat.SetFloat("_OrthoRoughness", 0.18f); changed = true; }
            }
            if (mat.HasProperty("_Ks"))
            {
                float ks = mat.GetFloat("_Ks");
                if (ks > 0.6f) { mat.SetFloat("_Ks", 0.6f); Debug.Log($"[FixAliasing] {mat.name} _Ks {ks:F3}→0.60"); changed = true; }
            }

            if (changed) { EditorUtility.SetDirty(mat); fixes++; }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[FixAliasing] Done — {fixes} fix(es) applied.");
    }
}
