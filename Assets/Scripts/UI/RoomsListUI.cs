using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает список открытых (не приватных) комнат из GameRoom.All.
/// Список обновляется автоматически через событие GameRoom.OnRoomListChanged.
/// </summary>
public class RoomsListUI : MonoBehaviour
{
    public static RoomsListUI Instance { get; private set; }

    [SerializeField] private GameObject roomEntryPrefab;
    [SerializeField] private Transform roomsContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        RefreshRoomList();
    }

    public void RefreshRoomList()
    {
        foreach (Transform child in roomsContainer)
            Destroy(child.gameObject);

        foreach (GameRoom room in GameRoom.All)
        {
            // Показываем только незакрытые и незапущенные комнаты
            if (room.IsPrivate || room.IsInProgress) continue;
            CreateRoomEntry(room);
        }
    }

    private void CreateRoomEntry(GameRoom room)
    {
        GameObject entry = Instantiate(roomEntryPrefab, roomsContainer);

        TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 1) texts[0].text = room.RoomCode;
        if (texts.Length >= 2) texts[1].text = $"{room.PlayerCount}/{room.MaxPlayers}";

        Button joinBtn = entry.GetComponentInChildren<Button>();
        if (joinBtn != null)
        {
            string code = room.RoomCode;
            joinBtn.onClick.AddListener(() =>
            {
                // Скрываем панель; переход в лобби произойдёт через TargetJoinedRoom → ShowLobby
                LobbyManager.Instance.ShowMainMenu();
                LobbyManager.Instance.JoinRoom(code);
            });
        }
    }
}
