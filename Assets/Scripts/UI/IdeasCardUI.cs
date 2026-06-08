using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
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

    public void ShowForActiveRole(string cardKey, int wordIndex)
    {
        wordsPanel.SetActive(true);
        ToggleInteractable(false);
        FetchWords(cardKey, words =>
        {
            int count = Mathf.Min(wordButtons.Length, words.Length);
            for (int i = 0; i < count; i++)
            {
                wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = words[i];
                wordButtons[i].GetComponent<Image>().color = i == wordIndex ? currentWordColor : defaultWordColor;
            }
        });
    }

    public void ShowForOthers(string cardKey)
    {
        wordsPanel.SetActive(true);
        ToggleInteractable(false);
        FetchWords(cardKey, words =>
        {
            int count = Mathf.Min(wordButtons.Length, words.Length);
            for (int i = 0; i < count; i++)
            {
                wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = words[i];
                wordButtons[i].GetComponent<Image>().color = defaultWordColor;
            }
        });
    }

    private void FetchWords(string cardKey, Action<string[]> onReady)
    {
        var op = LocalizationSettings.StringDatabase.GetTableAsync("Word Cards Labels");
        if (op.IsDone)
        {
            onReady(ParseWords(op.Result?.GetEntry(cardKey)?.GetLocalizedString()));
            return;
        }
        op.Completed += handle => onReady(ParseWords(handle.Result?.GetEntry(cardKey)?.GetLocalizedString()));
    }

    private static string[] ParseWords(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        return raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
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
