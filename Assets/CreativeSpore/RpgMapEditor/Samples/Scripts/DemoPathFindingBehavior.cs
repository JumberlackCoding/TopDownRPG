using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

public class DemoPathFindingBehavior : MapPathFindingBehaviour 
{
    GUIContent[] comboBoxList;
    private ComboBox comboBoxControl;// = new ComboBox();
    private GUIStyle listStyle = new GUIStyle();

    new void Start()
    {
        base.Start();

        AutoTileMap.Instance.AutoTileMapGui.enabled = false;

        string[] toolBarNames = System.Enum.GetNames(typeof(MapPathFinding.eHeuristicType));
        comboBoxList = new GUIContent[toolBarNames.Length];
        for (int i = 0; i < toolBarNames.Length; ++i)
        {
            comboBoxList[i] = new GUIContent("Heuristic: "+toolBarNames[i]);
        }

        listStyle.normal.textColor = Color.white;
        listStyle.onHover.background =
        listStyle.hover.background = new Texture2D(2, 2);
        listStyle.padding.left =
        listStyle.padding.right =
        listStyle.padding.top =
        listStyle.padding.bottom = 4;

        comboBoxControl = new ComboBox(new Rect(600, 0, 150, 20), comboBoxList[0], comboBoxList, "button", "box", listStyle);
        comboBoxControl.SelectedItemIndex = (int)m_mapPathFinding.HeuristicType;
    }

    private void OnGUI()
    {
        if (AutoTileMap.Instance.AutoTileMapGui.enabled)
        {
            if( GUI.Button(new Rect(300, 0, 200, 20), "Enable Debug PathFinding"))
            {
                AutoTileMap.Instance.AutoTileMapGui.enabled = false;
            }
        }
        else
        {
            if (GUI.Button(new Rect(300, 0, 200, 20), "Enable Edit Map"))
            {
                AutoTileMap.Instance.AutoTileMapGui.enabled = true;
            }
            m_mapPathFinding.HeuristicType = (MapPathFinding.eHeuristicType)comboBoxControl.Show();
        }               
    }
}
