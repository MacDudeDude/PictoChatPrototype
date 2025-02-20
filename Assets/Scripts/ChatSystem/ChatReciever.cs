using FishNet.Broadcast;
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
        public Color32[] textureColors;
    }

    public void SendChatMessage(Color32[] colors)
    {

    }

    public void RecieveChatMessage()
    {
        GameObject newChatMessage = Instantiate(chatMessagePrefab, content);
        messages.Add(newChatMessage);

        if (messages.Count > maxPreviousMessages)
        {
            Destroy(messages[0]);
            messages.RemoveAt(0);
        }
    }
}
