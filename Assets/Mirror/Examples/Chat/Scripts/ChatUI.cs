using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.Chat
{
    public class ChatUI : NetworkBehaviour
    {
        [SerializeField] Text chatHistory;
        [SerializeField] Scrollbar scrollbar;
        [SerializeField] InputField chatMessage;
        [SerializeField] Button sendButton;

        internal static string localPlayerName;
        internal static readonly Dictionary<NetworkConnectionToClient, string> connNames = new Dictionary<NetworkConnectionToClient, string>();

        public override void OnStartServer()
        {
            connNames.Clear();
        }

        public override void OnStartClient()
        {
            chatHistory.text = "";
        }

        [Command(requiresAuthority = false)]
        void CmdSend(string message, NetworkConnectionToClient sender = null)
        {
            if (!connNames.ContainsKey(sender))
                connNames.Add(sender, sender.identity.GetComponent<Player>().playerName);

            if (!string.IsNullOrWhiteSpace(message))
                RpcReceive(connNames[sender], message.Trim());
        }

        [ClientRpc]
        void RpcReceive(string playerName, string message)
        {
            string prettyMessage = playerName == localPlayerName ?
                $"<color=red>{playerName}:</color> {message}" :
                $"<color=blue>{playerName}:</color> {message}";
            AppendMessage(prettyMessage);
        }

        void AppendMessage(string message)
        {
            StartCoroutine(AppendAndScroll(message));
        }

        IEnumerator AppendAndScroll(string message)
        {
            chatHistory.text += message + "\n";

            yield return null;
            yield return null;

            scrollbar.value = 0;
        }

        public void ExitButtonOnClick()
        {
            NetworkManager.singleton.StopHost();
        }

        public void ToggleButton(string input)
        {
            sendButton.interactable = !string.IsNullOrWhiteSpace(input);
        }

        public void OnEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetButtonDown("Submit"))
                SendMessage();
        }

        public void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(chatMessage.text))
            {
                CmdSend(chatMessage.text.Trim());
                chatMessage.text = string.Empty;
                chatMessage.ActivateInputField();
            }
        }
    }
}
