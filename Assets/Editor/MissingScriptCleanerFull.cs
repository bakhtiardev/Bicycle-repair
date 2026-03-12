// Auto-generated utility — safe to delete after use
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MissingScriptCleanerFull
{
    [MenuItem("Tools/Remove Missing Scripts (Scene + Prefabs)")]
    public static void RemoveAll()
    {
        int total = 0;

        // --- 1. Scene objects ---
        total += CleanSceneObjects();

        // --- 2. All prefabs in Assets ---
        total += CleanAllPrefabs();

        Debug.Log($"[MissingScriptCleanerFull] TOTAL removed: {total} missing script(s).");
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    static int CleanSceneObjects()
    {
        int removed = 0;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.name == null || go.scene.name == "") continue;
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                removed += count;
                Debug.Log($"[Scene] Removed {count} from '{go.name}'");
            }
        }
        return removed;
    }

    static int CleanAllPrefabs()
    {
        int removed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // Check all children recursively
            bool dirty = false;
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    removed += count;
                    dirty = true;
                    Debug.Log($"[Prefab] Removed {count} from '{t.gameObject.name}' in prefab: {path}");
                }
            }

            if (dirty)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }
        return removed;
    }
}
