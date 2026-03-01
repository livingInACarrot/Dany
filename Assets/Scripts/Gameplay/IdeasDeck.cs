using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class IdeasCard
{
    public string[] Words = new string[5];

    public int GetRandomWord()
    {
        return UnityEngine.Random.Range(0, Words.Length);
    }
}

public class IdeasDeck : MonoBehaviour
{
    public static IdeasDeck Instance { get; private set; }

    private Queue<IdeasCard> ideasCards = new();

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        InitializeDeck();
    }

    private void InitializeDeck()
    {
        List<IdeasCard> cards = new();

        foreach (string cardKey in CardsStorage.IdeasCardsKeys)
        {
            var table = LocalizationSettings.StringDatabase.GetTable("Word Cards Labels");
            if (table == null)
                return;
            string cardText = table.GetEntry(cardKey).GetLocalizedString();

            string[] words = cardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            IdeasCard card = new();
            for (int i = 0; i < words.Length; ++i)
            {
                card.Words[i] = words[i][3..];
            }
            cards.Add(card);
        }

        Shuffle(cards);

        ideasCards.Clear();
        foreach (var card in cards)
        {
            ideasCards.Enqueue(card);
        }
    }

    public IdeasCard DrawCard()
    {
        if (ideasCards.Count == 0)
        {
            Debug.LogError("Закончились карты идей!");
            return null;
        }

        return ideasCards.Dequeue();
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}