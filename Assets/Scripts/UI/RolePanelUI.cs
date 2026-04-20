using TMPro;
using UnityEngine;

public class RolePanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI task;

    public void SetTexts(bool isDany)
    {
        switch (isDany) 
        { 
            case true:
                title.text = Loc.Text("gameUI.danyTitle");
                task.text = Loc.Text("gameUI.danyTask");
                break;
            case false:
                title.text = Loc.Text("gameUI.personalityTitle");
                task.text = Loc.Text("gameUI.personalityTask");
                break;
        }
    }
}
