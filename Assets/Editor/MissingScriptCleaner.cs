// Auto-generated utility — safe to delete after use
using UnityEngine;
using UnityEditor;

public class MissingScriptCleaner
{
    [MenuItem("Tools/Remove All Missing Scripts In Scene")]
    public static void RemoveMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int removed = 0;
        int objsFixed = 0;

        foreach (GameObject go in allObjects)
        {
            // Only process scene objects, not assets
            if (go.scene.name == null || go.scene.name == "") continue;

            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                removed += count;
                objsFixed++;
                Debug.Log($"[MissingScriptCleaner] Removed {count} missing script(s) from: {go.name} (path: {GetPath(go)})");
            }
        }

        if (removed == 0)
            Debug.Log("[MissingScriptCleaner] No missing scripts found.");
        else
        {
            Debug.Log($"[MissingScriptCleaner] Done. Removed {removed} missing script(s) from {objsFixed} GameObject(s).");
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
