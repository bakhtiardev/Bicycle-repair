using UnityEngine;
using UnityEditor;

public class SoftenMaterials {
    [MenuItem("Tools/Soften Materials")]
    public static void Run() {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("MRBike")) {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat.shader.name.Contains("MRBike/Lighting")) {
                    bool changed = false;

                    // Increase minimum roughness to avoid micro-specular hits
                    if (mat.HasProperty("_Roughness")) {
                        float r = mat.GetFloat("_Roughness");
                        if (r < 0.35f) {
                            mat.SetFloat("_Roughness", 0.35f);
                            changed = true;
                        }
                    }

                    // Decrease Glossiness
                    if (mat.HasProperty("_Ks")) {
                        float ks = mat.GetFloat("_Ks");
                        if (ks > 0.4f) {
                            mat.SetFloat("_Ks", 0.4f);
                            changed = true;
                        }
                    }

                    // Lower Normal Map Blend
                    if (mat.HasProperty("_NormalBlend")) {
                        float nb = mat.GetFloat("_NormalBlend");
                        if (nb > 0.5f) {
                            mat.SetFloat("_NormalBlend", 0.5f);
                            changed = true;
                        }
                    }

                    if (changed) {
                        EditorUtility.SetDirty(mat);
                        count++;
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Softened {count} materials to reduce VR aliasing.");
    }
}
