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

    private struct ChatEntry
    {
        public bool isSystem;
        public int senderNum;
        public string text;
    }

    private readonly List<ChatEntry> _entries = new();
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

    private void OnEnable() => LocalizationManager.OnLocaleReady += RebuildMessages;
    private void OnDisable() => LocalizationManager.OnLocaleReady -= RebuildMessages;

    private void OnSendMessage(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np != null)
            np.CmdSendChatMessage(input, np.Number);
        messageInput.text = string.Empty;
        messageInput.ActivateInputField();
    }

    public void AddMessage(int senderNum, string message)
        => AddEntry(new ChatEntry { isSystem = false, senderNum = senderNum, text = message });

    public void AddSystemMessage(string message)
        => AddEntry(new ChatEntry { isSystem = true, senderNum = -1, text = message });

    public void ActivateChat(bool active) => messageInput.interactable = active;

    private void AddEntry(ChatEntry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > MaxMessages)
            _entries.RemoveAt(0);

        SpawnMessage(entry);

        if (_messages.Count > MaxMessages)
        {
            Destroy(_messages[0]);
            _messages.RemoveAt(0);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void SpawnMessage(ChatEntry entry)
    {
        string sender = entry.isSystem ? Loc.Text("chat.system") : Loc.Nick(entry.senderNum);
        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        msgObj.GetComponentInChildren<TextMeshProUGUI>().text = $"{sender}: {entry.text}";
        _messages.Add(msgObj);
    }

    private void RebuildMessages()
    {
        foreach (var msg in _messages)
            if (msg != null) Destroy(msg);
        _messages.Clear();

        foreach (var entry in _entries)
            SpawnMessage(entry);

        Canvas.ForceUpdateCanvases();
    }
}
