
using NotReaper;
using NotReaper.Models;
using NotReaper.Timing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Tools.CustomSnapMenu {
public class SnapPresetScrollItem : MonoBehaviour
{
    public TextMeshProUGUI snap;
    private int snapValue = -1;
    public Button deleteButton;


    public void SetInfoFromData(string snaptext)
    {      
        snap.text = snaptext;

        int.TryParse(snaptext.Substring(2),out snapValue);
    }

    public void DeleteItem()
    {
        SnapPresetScrollWindow panel = GameObject.FindObjectOfType<SnapPresetScrollWindow>();
        panel.snapMenu.RemoveSnap(snapValue);
        panel.UpdateSnapList();
    }
}
}