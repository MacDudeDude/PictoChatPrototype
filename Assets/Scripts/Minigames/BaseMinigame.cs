
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet;
using UnityEngine;
using System;
public class BaseMinigame : MonoBehaviour, IMinigameScene
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string SceneName { get; set; }
    public int RequiredPlayers { get; set; }

    private SceneLoadData sld => new SceneLoadData(SceneName);
    private SceneUnloadData sud => new SceneUnloadData(SceneName);
    public virtual void StartMinigame()
    {
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    public virtual void EndMinigame()
    {
        InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
    }

    public event Action OnMinigameOver;
}
