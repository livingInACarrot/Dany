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
    [SerializeField] private Color keyWordColor_norm = Color.lightGreen;
    [SerializeField] private Color keyWordColor_hight = Color.mediumSpringGreen;
    [SerializeField] private Color keyWordColor_disab = Color.mediumSpringGreen;

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
        ColorBlock colors = new();

        // Активная личность видит все слова, и выделено то, которое надо показать
        for (int i = 0; i < 5; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];

            // Делаем все кнопки белыми
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.lightGray;
            colors.disabledColor = Color.gray;
            wordButtons[i].colors = colors;

            // Подсвечиваем правильное слово
            if (i == wordIndex)
            {
                colors.normalColor = keyWordColor_norm;
                colors.highlightedColor = keyWordColor_hight;
                colors.disabledColor = keyWordColor_disab;
                wordButtons[i].colors = colors;
                wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "(" + card.Words[i] + ")";
            }
        }
        SetButtonsActive(false);
    }

    public void ShowForOthers(IdeasCard card)
    {
        wordsPanel.SetActive(true);
        ColorBlock colors = new();

        // Все слова белые
        for (int i = 0; i < 5; i++)
        {
            wordButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = card.Words[i];

            colors.normalColor = Color.white;
            colors.highlightedColor = Color.lightGray;
            colors.disabledColor = Color.gray;
            wordButtons[i].colors = colors;
        }
        SetButtonsActive(false);
    }
    public void ShowGuessPanel(IdeasCard card)
    {
        ShowForOthers(card);

        NetworkChat.Instance.AddSystemMessage($"Решающая личность ({GameManager.Instance.Players.FirstOrDefault(p => p.Role == Role.Decisive)}) должна угадать слово!");
    }

    public void HideCard()
    {
        wordsPanel.SetActive(false);
    }

    public void SetButtonsActive(bool active)
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            wordButtons[i].interactable = active;
        }
    }
}