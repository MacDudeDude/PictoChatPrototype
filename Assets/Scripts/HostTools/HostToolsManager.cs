using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Managing;
using System.Linq;

public class HostToolsManager : MonoBehaviour
{
    [System.Serializable]
    public class ToolEntry
    {
        public string toolName;  // For UI identification
        public DrawingToolBase tool;
    }

    [Header("Tool Settings")]
    [SerializeField] private List<ToolEntry> availableTools;
    private Dictionary<string, DrawingToolBase> toolDictionary;
    private DrawingToolBase currentTool;

    [Header("References")]
    [SerializeField] private IDrawingService drawingService;

    private static HostToolsManager _instance;
    public static HostToolsManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        _instance = this;
        InitializeTools();
    }

    private void InitializeTools()
    {
        toolDictionary = new Dictionary<string, DrawingToolBase>();
        foreach (var toolEntry in availableTools)
        {
            toolEntry.tool.Initialize(drawingService);
            toolDictionary[toolEntry.toolName] = toolEntry.tool;
        }

        currentTool = toolDictionary.Values.First();
    }

    private void Update()
    {
        currentTool?.OnToolUpdate();
    }

    private void SwitchTool(string toolName)
    {
        if (!toolDictionary.TryGetValue(toolName, out DrawingToolBase newTool))
            return;

        currentTool?.OnToolDeselected();
        currentTool = newTool;
        currentTool.OnToolSelected();
    }

    public void OnToolSelected(string toolName)
    {
        SwitchTool(toolName);
    }
}
