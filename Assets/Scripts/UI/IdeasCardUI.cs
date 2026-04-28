using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class IdeasCardUI : MonoBehaviour
{
    public static IdeasCardUI Instance { get; private set; }

    [SerializeField] private GameObject wordsPanel;

    private Button[] wordButtons;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        wordButtons = wordsPanel.GetComponentsInChildren<Button>();
        wordsPanel.SetActive(true);
    }

    public void ShowForActiveRole(IdeasCard card, int wordIndex)
    {
        wordsPanel.SetActive(true);

        for (int i = 0; i < 5; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];
            if (i == wordIndex)
            {
                wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "(" + card.Words[i] + ")";
            }
        }
        ToggleInteractable(false);
    }

    public void ShowForOthers(IdeasCard card)
    {
        wordsPanel.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];
        }
        ToggleInteractable(false);
    }
    public void ShowGuessPanel(IdeasCard card)
    {
        ShowForOthers(card);
        NetworkChat.Instance.AddSystemMessage($"Решающая личность должна угадать слово!");
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
            //wordButtons[i].GetComponent<Image>().raycastTarget = active;
        }
    }
}