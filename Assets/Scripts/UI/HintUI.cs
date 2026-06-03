using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject hint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleHints()
    {
        var gp = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>()?.GamePlayer;
        if (gp == null) return;
        hint.SetActive(toggle.isOn && gp.Role == Role.Active);
    }
}
