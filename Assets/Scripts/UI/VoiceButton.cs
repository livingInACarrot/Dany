using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceButton : MonoBehaviour
{
    public static Key Bind = Key.X;

    [SerializeField] private TMP_Text label;

    private bool waitingForKey = false;
    private Keyboard keyboard;
    private RectTransform panel;

    private void Start()
    {
        keyboard = Keyboard.current;
        panel = GetComponentInParent<RectTransform>().GetComponentInParent<RectTransform>();
        UpdateLabel();
    }
    void Update()
    {
        if (!waitingForKey) return;
        if (keyboard == null) return;

        if (keyboard.anyKey.wasPressedThisFrame)
        {
            foreach (var key in keyboard.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    Bind = key.keyCode;
                    waitingForKey = false;
                    UpdateLabel();
                    Debug.Log($"Bound to: {Bind}");
                    break;
                }
            }
        }
    }
    public void OnClick()
    {
        waitingForKey = true;
        label.text = "";
    }

    public void OnClose()
    {
        waitingForKey = false;
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        label.text = Bind.ToString();
    }
}