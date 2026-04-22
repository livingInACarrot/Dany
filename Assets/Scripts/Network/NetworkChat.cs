using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NetworkChat : MonoBehaviour
{
    public static NetworkChat Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private TMP_InputField messageInput;

    private readonly List<GameObject> _messages = new();
    private const int MaxMessages = 50;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        messageInput.onSubmit.AddListener(OnSendMessage);
    }

    private void OnSendMessage(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np != null)
            np.CmdSendChatMessage(input, np.Number);

        messageInput.text = string.Empty;
        messageInput.ActivateInputField();
    }

    public void AddMessage(string sender, string message) => AddMessageToUI(sender, message);

    public void AddSystemMessage(string message) => AddMessageToUI("Система", message);

    public void EnableDiscussionMode()
    {
        messageInput.interactable = true;
        AddSystemMessage("Началось обсуждение! Все могут писать в чат.");
    }

    public void ActivateChat(bool active) => messageInput.interactable = active;

    private void AddMessageToUI(string sender, string message)
    {
        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        _messages.Add(msgObj);

        TextMeshProUGUI text = msgObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null) text.text = $"{sender}: {message}";

        Canvas.ForceUpdateCanvases();

        if (_messages.Count > MaxMessages)
        {
            Destroy(_messages[0]);
            _messages.RemoveAt(0);
        }
    }
}
