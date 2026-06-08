using System;
using System.Collections.Generic;
using UnityEngine;

public class IdeasCard
{
    public string Key { get; }

    public IdeasCard(string key) { Key = key; }

    public string[] GetWords() => ParseKey(Key);

    public int GetRandomWord() => UnityEngine.Random.Range(0, 5);

    private string[] ParseKey(string k) 
        => Loc.Text(k, "Word Cards Labels").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
}

public class IdeasDeck : MonoBehaviour
{
    public static IdeasDeck Instance { get; private set; }

    private Queue<IdeasCard> deck = new();

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
        List<IdeasCard> cards = CardsStorage.IdeasCards;
        deck.Clear();
        Shuffle(cards);
        foreach (var card in cards)
        {
            deck.Enqueue(card);
        }
    }

    public void Reset() => InitializeDeck();

    public IdeasCard DrawCard()
    {
        if (deck.Count == 0) return null;
        return deck.Dequeue();
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