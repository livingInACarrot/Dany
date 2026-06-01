using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class NetworkCard : NetworkBehaviour
{
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

    [SyncVar(hook = nameof(OnIsInHandChanged))]
    public bool inHand;

    [SyncVar]
    public uint ownerNetId;

    private Card card;
    private RectTransform rectTransform;
    private bool _initialized;

    private void Awake()
    {
        card = GetComponent<Card>();
        rectTransform = GetComponent<RectTransform>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        bool isLocalOwner = NetworkClient.localPlayer != null
                            && NetworkClient.localPlayer.netId == ownerNetId;
        if (!isLocalOwner)
        {
            GetComponent<Image>().raycastTarget = false;
            PlayingCardsTable.Instance?.StageCard(card);
        }
        _initialized = true;
    }

    public void Initialize(int index, uint ownerId)
    {
        spriteIndex = index;
        ownerNetId = ownerId;
        position = rectTransform.anchoredPosition;
        rotation = 0f;
        scale = rectTransform.localScale;
        isFlipped = false;
        inHand = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdPlaceOnTable(Vector2 pos, float rot, Vector3 sc, bool flipped, NetworkConnectionToClient sender = null)
    {
        if (sender?.identity?.netId != ownerNetId) return;
        position = pos;
        rotation = rot;
        scale = sc;
        isFlipped = flipped;
        inHand = false;
    }

    [Command(requiresAuthority = false)]
    public void CmdReturnFromTable(NetworkConnectionToClient sender = null)
    {
        if (sender?.identity?.netId != ownerNetId) return;
        inHand = true;
        isFlipped = false;
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateCard(Vector2 newPosition, float newRotation, Vector3 newScale, bool flipped, NetworkConnectionToClient sender = null)
    {
        if (sender?.identity?.netId != ownerNetId) return;
        position = newPosition;
        rotation = newRotation;
        scale = newScale;
        isFlipped = flipped;
    }

    [Command(requiresAuthority = false)]
    public void CmdToggleCardInteraction(bool interactable, NetworkConnectionToClient sender = null)
    {
        if (sender?.identity?.netId != ownerNetId) return;
        Image img = GetComponent<Image>();
        img.raycastTarget = interactable;
    }

    public bool IsOwnedByLocalPlayer() => NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == ownerNetId;

    private void OnSpriteIndexChanged(int _, int newIndex)
    {
        if (newIndex < 0 || card == null) return;
        var sprites = CardsStorage.PictureCardsSprites;
        if (sprites != null && newIndex < sprites.Count)
            card.SetSprite(sprites[newIndex]);
    }

    private void OnPositionChanged(Vector2 _, Vector2 newValue) => rectTransform.anchoredPosition = newValue;

    private void OnRotationChanged(float _, float newValue) => transform.rotation = Quaternion.Euler(0, 0, newValue);

    private void OnScaleChanged(Vector3 _, Vector3 newValue) => rectTransform.localScale = newValue;

    private void OnFlippedChanged(bool _, bool newValue)
    {
        if (isOwned) return;
        if (!card.gameObject.activeInHierarchy)
        {
            card.FlipCard(newValue);
            return;
        }
        card.TriggerFlipAnimation(newValue);
    }

    private void OnIsInHandChanged(bool _, bool nowInHand)
    {
        if (!_initialized) return;
        if (isOwned) return;
        if (nowInHand)
            PlayingCardsTable.Instance?.StageCard(card);
        else
        {
            card.StopFlipAnimation();
            PlayingCardsTable.Instance?.ShowOnTable(card);
            rectTransform.anchoredPosition = position;
            transform.rotation = Quaternion.Euler(0, 0, rotation);
            rectTransform.localScale = scale;
            card.FlipCard(isFlipped);
        }
    }
}
