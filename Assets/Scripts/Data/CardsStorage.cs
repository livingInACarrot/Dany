using System.Collections.Generic;
using UnityEngine;

public class CardsStorage : MonoBehaviour
{
    [SerializeField] private Sprite pictureCardBackSprite;
    [SerializeField] private Sprite ideasCardBackSprite;
    [SerializeField] private Sprite personalityCardBackSprite;
    [SerializeField] private Sprite choiceCardBackSprite;

    [SerializeField] private Sprite danyCardSprite;
    [SerializeField] private Sprite personalityCardSprite;

    [SerializeField] private List<Sprite> pictureCardsSprites;

    [SerializeField] private string ideasCardsKeyName;
    [SerializeField] private int ideasCardsAmount;


    public static Sprite PictureCardBackSprite { get; private set; }
    public static Sprite IdeasCardBackSprite { get; private set; }
    public static Sprite PersonalityCardBackSprite { get; private set; }
    public static Sprite ChoiceCardBackSprite { get; private set; }
    public static Sprite DanyCardSprite { get; private set; }
    public static Sprite PersonalityCardSprite { get; private set; }
    public static List<Sprite> PictureCardsSprites { get; private set; }
    public static List<IdeasCard> IdeasCards { get; private set; }

    private void Awake()
    {
        PictureCardBackSprite = pictureCardBackSprite;
        IdeasCardBackSprite = ideasCardBackSprite;
        PersonalityCardBackSprite = personalityCardBackSprite;
        ChoiceCardBackSprite = choiceCardBackSprite;
        DanyCardSprite = danyCardSprite;
        PersonalityCardSprite = personalityCardSprite;
        PictureCardsSprites = pictureCardsSprites;
        IdeasCards = GenerateIdeasCards();
    }

    private List<IdeasCard> GenerateIdeasCards()
    {
        List<IdeasCard> result = new();
        for (int i = 1; i <= ideasCardsAmount; ++i)
        {
            result.Add(new IdeasCard(ideasCardsKeyName + "." + i.ToString()));
        }
        return result;
    }
}
