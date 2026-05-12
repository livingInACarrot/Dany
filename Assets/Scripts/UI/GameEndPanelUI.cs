using TMPro;
using UnityEngine;

public class GameEndPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI body;

    public void SetText(bool danyWon, int dany)
    {
        if (danyWon) 
        {
                title.text = Loc.Text("gameUI.end.danyWon");
        }
        else
        {
                title.text = Loc.Text("gameUI.end.personalitiesWon");
        }
        body.text = Loc.Nick(dany) + " " + Loc.Text("gameUI.end.wasDany");

    }
}
