using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static UnityEngine.Rendering.GPUSort;

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
        IdeasCards = ParseIdeasKeys();
    }

    private List<IdeasCard> ParseIdeasKeys()
    {
        List<IdeasCard> result = new();
        var table = LocalizationSettings.StringDatabase.GetTable("Word Cards Labels");
        for (int i = 1; i <= ideasCardsAmount; ++i)
        {
            string key = ideasCardsKeyName + "." + i.ToString();
            string cardText = table.GetEntry(key).GetLocalizedString();
            string[] words = cardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            IdeasCard card = new();
            for (int j = 0; j < words.Length; ++j)
            {
                card.Words[j] = words[j][3..];
            }
            result.Add(card);
        }
        return result;
    }
}
