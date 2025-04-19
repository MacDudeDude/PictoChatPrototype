using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using TMPro;
using System;
public class ChatReciever : MonoBehaviour
{
    public Transform content;
    public GameObject chatMessagePrefab;
    public int maxPreviousMessages;
    private List<GameObject> messages = new List<GameObject>();

    private int width;
    private int height;
    private float ppu;

    public event Action<Color32[], string, bool, NetworkConnection> OnChatMessageReceived;

    public static ChatReciever Instance { get; private set; }

    public void Init(float ppu, int height, int width)
    {
        this.ppu = 1f / ppu;
        this.width = width;
        this.height = height;
    }

    public struct ChatBroadcast : IBroadcast
    {
        public string Username;
        public Color32[] textureColors;
        public string TextMessage;
        public bool popupAbovePlayer;
        public NetworkConnection connection;
    }

    public void SendChatMessage(Color32[] colors, string textMessage, bool playerPopup)
    {
        ChatBroadcast newMsh = new ChatBroadcast();
        newMsh.Username = SteamClient.Name;
        newMsh.textureColors = colors;
        newMsh.TextMessage = textMessage;
        newMsh.popupAbovePlayer = playerPopup;
        newMsh.connection = InstanceFinder.ClientManager.Connection;
        InstanceFinder.ClientManager.Broadcast(newMsh, Channel.Reliable);
    }

    public void RecieveChatMessage(Color32[] colors, string username, string textMessage, bool playerPopup, NetworkConnection connection)
    {
        GameObject newChatMessage = Instantiate(chatMessagePrefab, content);
        messages.Add(newChatMessage);
        TMP_Text[] messageText = newChatMessage.GetComponentsInChildren<TMP_Text>();
        messageText[0].text = username + ":";
        if (!string.IsNullOrEmpty(textMessage))
        {
            messageText[1].text = textMessage;
        }


        Texture2D message = CreateTextureFromMessage(colors);
        newChatMessage.GetComponentInChildren<UnityEngine.UI.RawImage>().texture = message;
        OnChatMessageReceived?.Invoke(colors, textMessage, playerPopup, connection);
        if (messages.Count > maxPreviousMessages)
        {
            Destroy(messages[0]);
            messages.RemoveAt(0);
        }
    }

    private Texture2D CreateTextureFromMessage(Color32[] colors)
    {
        Texture2D newTex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        newTex.filterMode = FilterMode.Point;

        newTex.SetPixels32(colors);
        newTex.Apply();

        return newTex;
    }

    public void OnChatBroadcast(NetworkConnection conn, ChatBroadcast msg, Channel channel)
    {
        //For the sake of simplicity we are using observers
        //on conn's first object.
        NetworkObject nob = conn.FirstObject;

        //The FirstObject can be null if the client
        //does not have any objects spawned.
        if (nob == null)
            return;


        //If you were to view the available Broadcast methods
        //you will find we are using the one with this signature...
        //NetworkObject nob, T message, bool requireAuthenticated = true, Channel channel = Channel.Reliable)
        //
        //This will send the message to all Observers on nob,
        //and require those observers to be authenticated with the server.
        InstanceFinder.ServerManager.Broadcast(nob, msg, true);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }
    private void OnEnable()
    {
        //Begins listening for any ChatBroadcast from the server.
        //When one is received the OnChatBroadcast method will be
        //called with the broadcast data.
        InstanceFinder.ClientManager.RegisterBroadcast<ChatBroadcast>(OnChatBroadcast);

        if (InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.RegisterBroadcast<ChatBroadcast>(OnChatBroadcast);
        }
    }

    //When receiving on clients broadcast callbacks will only have
    //the message. In a future release they will also include the
    //channel they came in on.
    private void OnChatBroadcast(ChatBroadcast msg, Channel channel)
    {
        RecieveChatMessage(msg.textureColors, msg.Username, msg.TextMessage, msg.popupAbovePlayer, msg.connection);
    }

    private void OnDisable()
    {
        //Like with events it is VERY important to unregister broadcasts
        //When the object is being destroyed(in this case disabled), or when
        //you no longer wish to receive the broadcasts on that object.
        InstanceFinder.ClientManager.UnregisterBroadcast<ChatBroadcast>(OnChatBroadcast);

        if (InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.UnregisterBroadcast<ChatBroadcast>(OnChatBroadcast);
        }
    }
}
