using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private string ideasCardsKeyName;
    [SerializeField] private int ideasCardsAmount;


    public static Sprite PictureCardBackSprite { get; private set; }
    public static Sprite IdeasCardBackSprite { get; private set; }
    public static Sprite PersonalityCardBackSprite { get; private set; }
    public static Sprite ChoiceCardBackSprite { get; private set; }
    public static Sprite DannyCardSprite { get; private set; }
    public static Sprite PersonalityCardSprite { get; private set; }
    public static List<Sprite> PictureCardsSprites { get; private set; }
    public static List<string> IdeasCardsKeys { get; private set; }

    private void Awake()
    {
        PictureCardBackSprite = pictureCardBackSprite;
        IdeasCardBackSprite = ideasCardBackSprite;
        PersonalityCardBackSprite = personalityCardBackSprite;
        ChoiceCardBackSprite = choiceCardBackSprite;
        DannyCardSprite = dannyCardSprite;
        PersonalityCardSprite = personalityCardSprite;
        PictureCardsSprites = pictureCardsSprites;

        IdeasCardsKeys = new List<string>();
        for (int i = 1; i <= ideasCardsAmount; i++)
        {
            IdeasCardsKeys.Add(ideasCardsKeyName + "." + i.ToString());
        }
    }
}
