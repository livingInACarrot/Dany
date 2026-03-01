using Mirror;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSpriteIndexChanged))]
    public int spriteIndex;

    [SyncVar(hook = nameof(OnPositionChanged))]
    public Vector2 position;

    [SyncVar(hook = nameof(OnRotationChanged))]
    public float rotation;

    [SyncVar(hook = nameof(OnScaleChanged))]
    public Vector3 scale;

    [SyncVar(hook = nameof(OnFlippedChanged))]
    public bool isFlipped;

    [SyncVar]
    public uint ownerNetId;

    private Card cardComponent;
    private RectTransform rectTransform;

    private void Awake()
    {
        cardComponent = GetComponent<Card>();
        rectTransform = GetComponent<RectTransform>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (cardComponent != null)
        {
            position = rectTransform.anchoredPosition;
            rotation = rectTransform.rotation.eulerAngles.z;
            scale = rectTransform.localScale;
            isFlipped = false;
        }
    }

    public void Initialize(int index, uint owner)
    {
        spriteIndex = index;
        ownerNetId = owner;
    }

    private void OnSpriteIndexChanged(int oldIndex, int newIndex)
    {
        if (cardComponent != null && CardsStorage.PictureCardsSprites != null)
        {
            if (newIndex >= 0 && newIndex < CardsStorage.PictureCardsSprites.Count)
            {
                cardComponent.SetSprite(CardsStorage.PictureCardsSprites[newIndex]);
            }
        }
    }

    private void OnPositionChanged(Vector2 oldPos, Vector2 newPos)
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = newPos;
    }

    private void OnRotationChanged(float oldRot, float newRot)
    {
        if (rectTransform != null)
            rectTransform.rotation = Quaternion.Euler(0, 0, newRot);
    }

    private void OnScaleChanged(Vector3 oldScale, Vector3 newScale)
    {
        if (rectTransform != null)
            rectTransform.localScale = newScale;
    }

    private void OnFlippedChanged(bool oldFlip, bool newFlip)
    {
        cardComponent.FlipCard(newFlip);
    }

    [Command]
    public void CmdUpdateCard(Vector2 newPos, float newRot, Vector3 newScale, bool flipped)
    {
        position = newPos;
        rotation = newRot;
        scale = newScale;
        isFlipped = flipped;
    }
}