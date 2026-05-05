using UnityEngine;
using UnityEngine.UI;

public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject hint;

    private bool _turnActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleHints() => Refresh();

    public void SetTurnActive(bool active)
    {
        _turnActive = active;
        Refresh();
    }

    private void Refresh()
    {
        hint.SetActive(toggle.isOn && _turnActive);
    }
}
