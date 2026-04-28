using UnityEngine;
using UnityEngine.UI;

public class HintUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject hint;
    public void ToggleHints()
    {
        hint.SetActive(toggle.isOn);
    }
}
