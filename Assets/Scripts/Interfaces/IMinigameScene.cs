using System;

public interface IMinigameScene
{
    public string DisplayName { get; }
    public string Description { get; }
    public string SceneName { get; }
    public int RequiredPlayers { get; }
    public void StartMinigame();
    public void EndMinigame();
    public event Action OnMinigameOver;

}