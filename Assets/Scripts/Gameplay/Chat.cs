using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Сетевой компонент чата. Обрабатывает отправку, получение и отображение сообщений.
/// Все сообщения проходят через сервер для синхронизации между всеми игроками.
/// Этот компонент заменяет отдельный ChatUI и NetworkChat.
/// </summary>
public class NetworkChat : NetworkBehaviour
{
    public static NetworkChat Instance { get; private set; }
    
    [Header("UI Elements")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button voiceButton;
    [SerializeField] private Button sendButton;

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
            DontDestroyOnLoad(gameObject);
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
        if (voiceButton != null)
            voiceButton.onClick.AddListener(ToggleVoiceChat);
        
        if (messageInput != null)
        {
            messageInput.onSubmit.AddListener((text) => OnSendMessage());
            messageInput.onValueChanged.AddListener(_ => UpdateSendButton());
        }
        
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendMessageButtonClick);
            sendButton.interactable = false;
        }
    }

    private void UpdateSendButton()
    {
        if (sendButton != null && messageInput != null)
        {
            sendButton.interactable = !string.IsNullOrWhiteSpace(messageInput.text);
        }
    }

    /// <summary>
    /// Вызывается клиентом для отправки сообщения на сервер
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdSendMessage(string sender, string message, NetworkConnectionToClient senderConn = null)
    {
        // Сервер получает сообщение и рассылает всем клиентам
        RpcReceiveMessage(sender, message);
    }

    /// <summary>
    /// Сервер рассылает сообщение всем клиентам для отображения
    /// </summary>
    [ClientRpc]
    private void RpcReceiveMessage(string sender, string message)
    {
        AddMessageToUI(sender, message);
    }

    /// <summary>
    /// Публичный метод для отправки сообщения из UI
    /// </summary>
    public void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string playerName = PlayerPrefs.GetString("PlayerName", "Игрок");

        if (isServer)
        {
            // Если мы сервер, сразу рассылаем
            RpcReceiveMessage(playerName, message);
        }
        else if (isClient)
        {
            // Если клиент, отправляем команду на сервер
            CmdSendMessage(playerName, message);
        }
    }

    /// <summary>
    /// Отправка системного сообщения от сервера
    /// </summary>
    [ClientRpc]
    public void RpcSendSystemMessage(string message)
    {
        AddMessageToUI("Система", message);
    }

    /// <summary>
    /// Добавление сообщения в UI (вызывается только на локальном клиенте)
    /// </summary>
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
        if (messageInput != null)
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

    private void OnSendMessageButtonClick()
    {
        OnSendMessage();
    }

    private void ToggleVoiceChat()
    {
        isVoiceActive = !isVoiceActive;

        ColorBlock colors = voiceButton.colors;
        if (isVoiceActive)
        {
            colors.normalColor = voiceActiveColor;
        }
        else
        {
            colors.normalColor = voiceInactiveColor;
        }
        voiceButton.colors = colors;
    }

    public void EnableDiscussionMode()
    {
        ActivateChat(true);
        AddSystemMessage("Началось обсуждение! Все могут писать в чат.");
    }
}
