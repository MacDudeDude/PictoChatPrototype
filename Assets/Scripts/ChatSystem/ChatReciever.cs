using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatReciever : MonoBehaviour
{
    public Transform content;
    public GameObject chatMessagePrefab;
    public int maxPreviousMessages;
    private List<GameObject> messages = new List<GameObject>();

    public struct ChatBroadcast : IBroadcast
    {
        public string Username;
        public Color32[] textureColors;
    }

    public void SendChatMessage(Color32[] colors)
    {
        ChatBroadcast newMsh = new ChatBroadcast();
        newMsh.Username = "asd";
        newMsh.textureColors = colors;

        InstanceFinder.ClientManager.Broadcast(newMsh);
    }

    public void RecieveChatMessage(Color32[] colors, string username)
    {
        GameObject newChatMessage = Instantiate(chatMessagePrefab, content);
        messages.Add(newChatMessage);
        newChatMessage.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = username;

        if (messages.Count > maxPreviousMessages)
        {
            Destroy(messages[0]);
            messages.RemoveAt(0);
        }
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

        //Populate the username field in the received msg.
        //Let us assume GetClientUsername actually does something.
        msg.Username = conn.ClientId.ToString();

        //If you were to view the available Broadcast methods
        //you will find we are using the one with this signature...
        //NetworkObject nob, T message, bool requireAuthenticated = true, Channel channel = Channel.Reliable)
        //
        //This will send the message to all Observers on nob,
        //and require those observers to be authenticated with the server.
        InstanceFinder.ServerManager.Broadcast(nob, msg, true);
    }

    private void OnEnable()
    {
        //Begins listening for any ChatBroadcast from the server.
        //When one is received the OnChatBroadcast method will be
        //called with the broadcast data.
        InstanceFinder.ClientManager.RegisterBroadcast<ChatBroadcast>(OnChatBroadcast);

        if(InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.RegisterBroadcast<ChatBroadcast>(OnChatBroadcast);
        }
    }

    //When receiving on clients broadcast callbacks will only have
    //the message. In a future release they will also include the
    //channel they came in on.
    private void OnChatBroadcast(ChatBroadcast msg, Channel channel)
    {
        RecieveChatMessage(msg.textureColors, msg.Username);
    }

    private void OnDisable()
    {
        //Like with events it is VERY important to unregister broadcasts
        //When the object is being destroyed(in this case disabled), or when
        //you no longer wish to receive the broadcasts on that object.
        InstanceFinder.ClientManager.UnregisterBroadcast<ChatBroadcast>(OnChatBroadcast);

        if(InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.UnregisterBroadcast<ChatBroadcast>(OnChatBroadcast);
        }
    }
}
