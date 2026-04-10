using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class VoiceButton : MonoBehaviour
{
    public static Key Bind;

    [SerializeField] private TMP_Text label;
    [SerializeField] private Key defaultKey = Key.X;

    private bool waitingForKey = false;
    private Keyboard keyboard;

    private void Start()
    {
        keyboard = Keyboard.current;
        Bind = defaultKey;
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
    private void UpdateLabel()
    {
        label.text = Bind.ToString();
    }
}