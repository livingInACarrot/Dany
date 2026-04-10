using Mirror;
using UnityEngine;

public class NetworkCard : NetworkBehaviour
{
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

    public void Initialize(Sprite sprite, uint ownerId)
    {
        card.SetSprite(sprite);
        ownerNetId = ownerId;
        position = rectTransform.anchoredPosition;
        rotation = 0f;
        scale = rectTransform.localScale;
        isFlipped = false;
    }

    public bool IsOwnedByLocalPlayer()
    {
        return NetworkClient.localPlayer.netId == ownerNetId;
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateCard(Vector2 newPosition, float newRotation, Vector3 newScale, bool flipped)
    {
        position = newPosition;
        rotation = newRotation;
        scale = newScale;
        isFlipped = flipped;
    }

    private void OnPositionChanged(Vector2 oldValue, Vector2 newValue)
    {
        rectTransform.anchoredPosition = newValue;
    }

    private void OnRotationChanged(float oldValue, float newValue)
    {
        transform.rotation = Quaternion.Euler(0, 0, newValue);
    }

    private void OnScaleChanged(Vector3 oldValue, Vector3 newValue)
    {
        rectTransform.localScale = newValue;
    }

    private void OnFlippedChanged(bool oldValue, bool newValue)
    {
        card.FlipCard(newValue);
    }
}