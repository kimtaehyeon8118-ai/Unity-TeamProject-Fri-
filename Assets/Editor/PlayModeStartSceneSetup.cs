#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeStartSceneSetup
{
    private const string StartScenePath = "Assets/Scenes/DayScene.unity";

    static PlayModeStartSceneSetup()
    {
        SetPlayModeStartScene();
    }

    [MenuItem("Tools/Set Play Start Scene to Day")]
    public static void SetPlayModeStartScene()
    {
        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        if (startScene == null)
        {
            Debug.LogWarning("Play mode start scene not found: " + StartScenePath);
            return;
        }

        EditorSceneManager.playModeStartScene = startScene;
    }
}
#endif
