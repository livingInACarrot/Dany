using System.Collections.Generic;
using UnityEngine;

public class PicturesDeck : MonoBehaviour
{
    public static PicturesDeck Instance { get; private set; }

    private Queue<Sprite> deck = new();

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
        List<Sprite> cards = CardsStorage.PictureCardsSprites;
        deck.Clear();
        Shuffle(cards);
        foreach (var card in cards)
        {
            deck.Enqueue(card);
        }
    }

    public Sprite DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.LogError("Закончились карты воспоминаний!");
            return null;
        }

        return deck.Dequeue();
    }

    public bool EnoughCardsToDraw()
    {
        return deck.Count >= 7;
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
