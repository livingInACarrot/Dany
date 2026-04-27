using Mirror;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
    // Индекс в CardsStorage.PictureCardsSprites — синхронизируется к клиентам
    [SyncVar(hook = nameof(OnSpriteIndexChanged))]
    public int spriteIndex = -1;

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

    private Card card;
    private RectTransform rectTransform;

    private void Awake()
    {
        card = GetComponent<Card>();
        rectTransform = GetComponent<RectTransform>();
    }

    // Вызывается на сервере до NetworkServer.Spawn
    public void Initialize(int index, uint ownerId)
    {
        spriteIndex = index;
        ownerNetId  = ownerId;
        position    = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        rotation    = 0f;
        scale       = rectTransform != null ? rectTransform.localScale : Vector3.one;
        isFlipped   = false;
    }

    private void OnSpriteIndexChanged(int _, int newIndex)
    {
        if (newIndex < 0 || card == null) return;
        var sprites = CardsStorage.PictureCardsSprites;
        if (sprites != null && newIndex < sprites.Count)
            card.SetSprite(sprites[newIndex]);
    }

    public bool IsOwnedByLocalPlayer()
        => NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == ownerNetId;

    [Command(requiresAuthority = false)]
    public void CmdUpdateCard(Vector2 newPosition, float newRotation, Vector3 newScale, bool flipped)
    {
        position  = newPosition;
        rotation  = newRotation;
        scale     = newScale;
        isFlipped = flipped;
    }

    private void OnPositionChanged(Vector2 _, Vector2 newValue)
    {
        if (rectTransform != null) rectTransform.anchoredPosition = newValue;
    }

    private void OnRotationChanged(float _, float newValue)
        => transform.rotation = Quaternion.Euler(0, 0, newValue);

    private void OnScaleChanged(Vector3 _, Vector3 newValue)
    {
        if (rectTransform != null) rectTransform.localScale = newValue;
    }

    private void OnFlippedChanged(bool _, bool newValue)
        => card?.FlipCard(newValue);
}
