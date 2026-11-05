using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SimpleMapMenu
{
    [MenuItem("Tools/Simple Map/Add Map Builder To Scene")]
    static void AddToScene()
    {
        if (Object.FindFirstObjectByType<SimpleMapBuilder>() != null)
        {
            Debug.Log("A SimpleMapBuilder is already in this scene.");
            return;
        }
        var go = new GameObject("SimpleMap");
        go.AddComponent<SimpleMapBuilder>();
        Undo.RegisterCreatedObjectUndo(go, "Add Simple Map");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
    }
}
