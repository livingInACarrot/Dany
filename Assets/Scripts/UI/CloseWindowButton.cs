using Unity.VisualScripting;
using UnityEngine;

public class CloseWindowButton : MonoBehaviour
{
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
    }
}
