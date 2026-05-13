using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdeasCardUI : MonoBehaviour
{
    public static IdeasCardUI Instance { get; private set; }

    [SerializeField] private GameObject wordsPanel;
    [SerializeField] private Color currentWordColor = new(0.9f, 1f, 0.9f);
    [SerializeField] private Color defaultWordColor = new(1f, 1f, 1f);
    [SerializeField] private Color wrongWordColor = new(1f, 0.8f, 0.8f);
    [SerializeField] private Color correctWordColor = new(0.8f, 1f, 0.8f);

    private Button[] wordButtons;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        wordButtons = wordsPanel.GetComponentsInChildren<Button>();
        HideCard();
    }

    public void ShowForActiveRole(IdeasCard card, int wordIndex)
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];
            wordButtons[i].GetComponent<Image>().color = defaultWordColor;

            if (i == wordIndex) wordButtons[i].GetComponent<Image>().color = currentWordColor;
        }
        ToggleInteractable(false);
        wordsPanel.SetActive(true);
    }

    public void ShowForOthers(IdeasCard card)
    {
        for (int i = 0; i < 5; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];
            wordButtons[i].GetComponent<Image>().color = defaultWordColor;
        }
        ToggleInteractable(false);
        wordsPanel.SetActive(true);
    }

    public void ShowGuessPanel(IdeasCard card)
    {
        ToggleInteractable(true);
    }

    public void OnWordButtonClicked(int wordIndex)
    {
        NetworkPlayer np = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (np == null) return;
        if (NetworkClient.spawned.TryGetValue(np.GamePlayerNetId, out NetworkIdentity id))
            id.GetComponent<GamePlayer>()?.CmdWordGuessed(wordIndex);
    }

    public void HideCard()
    {
        wordsPanel.SetActive(false);
    }

    public void ToggleInteractable(bool active)
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            wordButtons[i].interactable = active;
        }
    }

    public void HighLightGuess(int correct, int guess)
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            wordButtons[i].GetComponent<Image>().color = defaultWordColor;

            if (i == correct) wordButtons[i].GetComponent<Image>().color = correctWordColor;
            else if (i == guess) wordButtons[i].GetComponent<Image>().color = wrongWordColor;
        }
        ToggleInteractable(false);
        wordsPanel.SetActive(true);
    }
}
