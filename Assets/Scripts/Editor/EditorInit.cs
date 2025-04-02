using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class EditorInit
{
    const string firstSceneToLoad = "Assets/Scenes/TheImportantFirstSceneEverChangePath.unity";
    // Editor pref save name, no need to change
    const string activeEditorScene = "PreviousScenePath";
    const string isEditorInitialization = "EditorIntialization";

    // The scenes names that you want to do the editor initialization, only these scenes will work,
    // alternatively, you can do the initialization in all scenes by removing this list.
    static List<string> validScenes = new List<string>
    {
        "GameplayScene_1",
        "Level_2",
        "Test Scene"
    };
    // The scenes names that you want to load in addition to the first scene. Loaded in the list order.
    static List<string> extraScenesToLoad = new List<string>
    {
        "ExtraManager_ForGameplayOnly",
        "DeveloperScene_CheatScene_DebugScene_Scene"
    };

    static EditorInit()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // remove "IsValidScene" method if you want to do the initialization in all scenes.
        if (state == PlayModeStateChange.ExitingEditMode && IsValidScene(validScenes, out string sceneName))
        {
            EditorPrefs.SetString(activeEditorScene, sceneName);
            EditorPrefs.SetBool(isEditorInitialization, true);
            SetStartScene(firstSceneToLoad);
        }
        if (state == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(isEditorInitialization))
        {
            LoadExtraScenes();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorPrefs.SetBool(isEditorInitialization, false);
        }
    }
    static void SetStartScene(string scenePath)
    {
        SceneAsset firstSceneToLoad = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (firstSceneToLoad != null)
            EditorSceneManager.playModeStartScene = firstSceneToLoad;
        else
            Debug.Log("Could not find Scene " + scenePath);
    }
    static void LoadExtraScenes()
    {
        // extra scenes to load
        foreach (string scenePath in extraScenesToLoad)
        {
            SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
        }
        // the original scene loading
        var prevScene = EditorPrefs.GetString(activeEditorScene);
        SceneManager.LoadScene(prevScene, LoadSceneMode.Additive);
    }
    static bool IsValidScene(List<string> scenesToCheck, out string sceneName)
    {
        sceneName = SceneManager.GetActiveScene().name;
        return scenesToCheck.Contains(sceneName);
    }
}
