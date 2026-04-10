using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class NetworkChat : NetworkBehaviour
{
    public static NetworkChat Instance { get; private set; }
    
    [Header("UI Elements")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private TMP_InputField messageInput;

    [Header("Voice Chat")]
    [SerializeField] private Color voiceActiveColor = Color.green;
    [SerializeField] private Color voiceInactiveColor = Color.gray;

    private bool isVoiceActive = false;
    private List<GameObject> messages = new List<GameObject>();
    private const int MaxMessages = 50;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            SetupUI();
        }
    }

    private void SetupUI()
    {
        messageInput.onSubmit.AddListener((text) => OnSendMessage());
    }

    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string sender, string message, NetworkConnectionToClient senderConn = null)
    {
        RpcReceiveMessage(sender, message);
    }

    [ClientRpc]
    private void RpcReceiveMessage(string sender, string message)
    {
        AddMessageToUI(sender, message);
    }

    public void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string playerName = PlayerPrefs.GetString("PlayerName", "Игрок");

        if (isServer)
        {
            RpcReceiveMessage(playerName, message);
        }
        else if (isClient)
        {
            CmdSendMessage(playerName, message);
        }
    }

    private void AddMessageToUI(string sender, string message)
    {
        if (messageContainer == null || messagePrefab == null)
            return;

        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        messages.Add(msgObj);

        TextMeshProUGUI[] texts = msgObj.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = sender;
            texts[1].text = message;
        }

        Canvas.ForceUpdateCanvases();

        if (messages.Count > MaxMessages)
        {
            Destroy(messages[0]);
            messages.RemoveAt(0);
        }
    }

    public void AddMessage(string sender, string message)
    {
        AddMessageToUI(sender, message);
    }

    public void AddSystemMessage(string message)
    {
        AddMessageToUI("Система", message);
    }

    public void ActivateChat(bool active)
    {
        messageInput.interactable = active;
    }

    private void OnSendMessage()
    {
        if (string.IsNullOrWhiteSpace(messageInput.text))
            return;

        SendChatMessage(messageInput.text);

        messageInput.text = "";
        messageInput.ActivateInputField();
    }

    private void ToggleVoiceChat()
    {
        isVoiceActive = !isVoiceActive;
        //VoiceChatManager.Instance.ToggleMic();
    }

    public void EnableDiscussionMode()
    {
        ActivateChat(true);
        AddSystemMessage("Началось обсуждение! Все могут писать в чат.");
    }
}
