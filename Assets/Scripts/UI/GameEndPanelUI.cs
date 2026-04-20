using TMPro;
using UnityEngine;

public class GameEndPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;

    public void SetText(bool danyWon)
    {
        switch (danyWon) 
        { 
            case true:
                title.text = Loc.Text("gameUI.end.danyWon");
                break;
            case false:
                title.text = Loc.Text("gameUI.end.personalitiesWon");
                break;
        }
    }
}
