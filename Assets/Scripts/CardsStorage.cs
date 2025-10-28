using UnityEngine;
using System.Collections.Generic;

public class CardsStorage : MonoBehaviour
{
    [Header("Cards back")]
    [SerializeField] private Sprite pictureCardBackSprite;
    [SerializeField] private Sprite ideasCardBackSprite;
    [SerializeField] private Sprite personalityCardBackSprite;
    [SerializeField] private Sprite choiceCardBackSprite;

    [Header("Different cards")]
    [SerializeField] private Sprite dannyCardSprite;
    [SerializeField] private Sprite personalityCardSprite;

    [Header("Pictures cards")]
    [SerializeField] private List<Sprite> pictureCardsSprites;

    [Header("Ideas cards")]
    [SerializeField] private List<string> ideasCardsTexts;


    public static Sprite PictureCardBackSprite { get; private set; }
    public static Sprite IdeasCardBackSprite { get; private set; }
    public static Sprite PersonalityCardBackSprite { get; private set; }
    public static Sprite ChoiceCardBackSprite { get; private set; }
    public static Sprite DannyCardSprite { get; private set; }
    public static Sprite PersonalityCardSprite { get; private set; }
    public static List<Sprite> PictureCardsSprites { get; private set; }
    public static List<string> IdeasCardsTexts { get; private set; }

    private void Awake()
    {
        PictureCardBackSprite = pictureCardBackSprite;
        IdeasCardBackSprite = ideasCardBackSprite;
        PersonalityCardBackSprite = personalityCardBackSprite;
        ChoiceCardBackSprite = choiceCardBackSprite;
        DannyCardSprite = dannyCardSprite;
        PersonalityCardSprite = personalityCardSprite;
        PictureCardsSprites = pictureCardsSprites;
        IdeasCardsTexts = ideasCardsTexts;
    }
}
