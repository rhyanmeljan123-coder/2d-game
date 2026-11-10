using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes sure SampleScene always contains the SimpleMap object, so the map shows up in the
/// Scene view in Edit mode (even if the scene was open while the project was updated).
/// </summary>
[InitializeOnLoad]
public static class EnsureMapInScene
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static EnsureMapInScene()
    {
        EditorSceneManager.sceneOpened += (scene, mode) => Ensure(scene);
        EditorApplication.delayCall += () => Ensure(SceneManager.GetActiveScene());
    }

    static void Ensure(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<SimpleMapBuilder>(true) != null) return;

        var go = new GameObject("SimpleMap");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<SimpleMapBuilder>();
        EditorSceneManager.MarkSceneDirty(scene);

        // Bring the map into view
        var view = SceneView.lastActiveSceneView;
        if (view != null) view.LookAt(new Vector3(20f, 12f, 0f), Quaternion.identity, 14f, true);
    }
}
