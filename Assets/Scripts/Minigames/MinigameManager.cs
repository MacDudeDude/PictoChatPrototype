using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Manages the loading, unloading and state of minigames in the game.
/// </summary>
public class MinigameManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the MinigameManager.
    /// </summary>
    public static MinigameManager Instance { get; private set; }

    [Header("Minigames")]
    public List<BaseMinigame> Minigames;

    // Dictionary mapping scene names to minigame instances for quick lookup
    private Dictionary<string, BaseMinigame> minigameDictionary;
    private BaseMinigame currentMinigame;
    private bool isMinigameActive = false;

    // Events that fire when minigames start/end
    public event Action<BaseMinigame> OnMinigameStarted;
    public event Action OnMinigameEnded;

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Initialize();
    }

    /// <summary>
    /// Builds the dictionary of minigames for quick lookup by scene name.
    /// </summary>
    private void Initialize()
    {
        minigameDictionary = new Dictionary<string, BaseMinigame>();
        foreach (var minigame in Minigames)
        {
            minigameDictionary.Add(minigame.SceneName, minigame);
            Debug.Log($"[MinigameManager] Added minigame: {minigame.SceneName}");
        }
    }

    /// <summary>
    /// Changes to a new minigame if one isn't already active.
    /// </summary>
    /// <param name="minigameName">The scene name of the minigame to change to</param>
    public void ChangeMinigame(string minigameName)
    {
        if (minigameDictionary.TryGetValue(minigameName, out var minigame) && !isMinigameActive)
        {
            Debug.Log($"[MinigameManager] Changing minigame to: {minigameName}");
            currentMinigame = minigame;
            isMinigameActive = true;
            StartMinigame(currentMinigame);
        }
    }

    /// <summary>
    /// Starts the specified minigame and sets up event handlers.
    /// </summary>
    /// <param name="minigame">The minigame to start</param>
    private void StartMinigame(BaseMinigame minigame)
    {
        Debug.Log($"[MinigameManager] Starting minigame: {minigame.SceneName}");
        if (minigame.RequiredPlayers > SteamPlayerManager.Instance.GetPlayerCount())
        {
            Debug.LogError($"[MinigameManager] Not enough players to start minigame: {minigame.SceneName}");
            return;
        }
        minigame.StartMinigame();
        OnMinigameStarted?.Invoke(minigame);
        minigame.OnMinigameOver += OnMinigameOver;
    }

    /// <summary>
    /// Handler for when a minigame ends. Triggers the end event and cleanup.
    /// </summary>
    private void OnMinigameOver()
    {
        OnMinigameEnded?.Invoke();
        EndMinigame();
    }

    /// <summary>
    /// Ends the current minigame and cleans up event handlers.
    /// </summary>
    private void EndMinigame()
    {
        Debug.Log($"[MinigameManager] Ending minigame: {currentMinigame.SceneName}");
        isMinigameActive = false;
        currentMinigame.EndMinigame();
        currentMinigame.OnMinigameOver -= OnMinigameOver;
    }
}