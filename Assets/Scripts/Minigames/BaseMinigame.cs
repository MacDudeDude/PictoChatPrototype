
using FishNet.Managing.Scened;
using FishNet;
using UnityEngine;
using System;
using UnityEditor.U2D.Path.GUIFramework;
[CreateAssetMenu(fileName = "New Minigame", menuName = "Minigame")]
public class BaseMinigame : ScriptableObject, IMinigameScene
{
    [SerializeField]
    private string displayName;
    public string DisplayName { get; private set; }
    [SerializeField]
    private string description;
    public string Description { get; private set; }
    [SerializeField]
    private string sceneName;
    public string SceneName { get; private set; }
    [SerializeField]
    private int requiredPlayers;
    public int RequiredPlayers { get; private set; }

    private SceneLoadData sld => new SceneLoadData(SceneName);
    private SceneUnloadData sud => new SceneUnloadData(SceneName);

    public virtual void Initialize()
    {
        DisplayName = displayName;
        Description = description;
        SceneName = sceneName;
        RequiredPlayers = requiredPlayers;
        sld.ReplaceScenes = ReplaceOption.None;

    }

    public virtual void StartMinigame()
    {
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    public virtual void EndMinigame()
    {
        InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
    }

}
