using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Chat : MonoBehaviour
{
    private List<Message> chat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendMessage(Player player, string text)
    {
        chat.Add(new Message(player, text));
    }
}
