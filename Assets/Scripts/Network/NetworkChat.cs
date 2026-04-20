using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
        SetupUI();
        if (isLocalPlayer)
        {
            Debug.Log("local player");
            SetupUI();
        }
        else
        {
            Debug.Log("not local player");
        }
    }

    private void SetupUI()
    {
        messageInput.onSubmit.AddListener(OnSendMessage);
    }

    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string message, NetworkConnectionToClient senderConn = null)
    {
        var sender = senderConn.identity.GetComponent<NetworkPlayer>().Number.ToString();
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

        CmdSendMessage(message);
    }

    private void AddMessageToUI(string sender, string message)
    {
        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        messages.Add(msgObj);

        TextMeshProUGUI text = msgObj.GetComponentInChildren<TextMeshProUGUI>();
        text.text = sender + ": " + message;

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

    private void OnSendMessage(string input)
    {
        //if (string.IsNullOrWhiteSpace(messageInput.text))
        //    return;
        if (string.IsNullOrWhiteSpace(input))
            return;

        SendChatMessage(input);

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
