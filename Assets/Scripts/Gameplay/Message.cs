using UnityEngine;

public class Message
{
    public Player Player;
    public string Text;

    public Message(Player player, string text)
    {
        Player = player;
        Text = text;
    }
}
