using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;

namespace CreativeSpore.RpgMapEditor
{
	[CustomEditor(typeof(AutoTileset))] 
	public class AutoTilesetEditor : Editor
	{
		AutoTileset MyAutoTileset{ get{ return (AutoTileset)target; } }

        int m_vSlots = 2;
        int m_hSlots = 2;
	
		public override void OnInspectorGUI() 
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();

			EditorGUILayout.HelpBox("Select a texture atlas directly or Generate a new atlas using separated textures", MessageType.Info);
            Texture2D prevTexture = MyAutoTileset.AtlasTexture;
			MyAutoTileset.AtlasTexture = EditorGUILayout.ObjectField ("Tileset Atlas", MyAutoTileset.AtlasTexture, typeof(Texture2D), false) as Texture2D;
            if (MyAutoTileset.AtlasTexture != null)
			{

                //NOTE: MyAutoTileset.SubTilesets.Count should be 0 when loading old Tileset below version 1.2.0 only
                if (prevTexture != MyAutoTileset.AtlasTexture || MyAutoTileset.SubTilesets.Count == 0) 
                {
                    MyAutoTileset.GenerateAutoTileData();
                    EditorUtility.SetDirty(MyAutoTileset);
                }

                EditorGUILayout.HelpBox("Add tilesets to any free slot", MessageType.Info);
				if( GUILayout.Button( "Edit Tileset...") )
				{
                    AutoTilesetEditorWindow.ShowDialog(MyAutoTileset, AutoTilesetEditorWindow.eEditMode.TilesetAtlas);
				}
			}
            else
            {
                EditorGUILayout.HelpBox("Set the number of slots for the atlas to be filled with normal or auto tilesets. If there are too many slots, texture atlas could be too big for some systems. Check the size of the texture while changing the values.", MessageType.Info);
                m_hSlots = EditorGUILayout.IntSlider("Horizontal Slots:", m_hSlots, 1, AutoTileset.k_MaxTextureSize / MyAutoTileset.TilesetSlotSize);
                m_vSlots = EditorGUILayout.IntSlider("Vertical Slots:", m_vSlots, 1, AutoTileset.k_MaxTextureSize / MyAutoTileset.TilesetSlotSize);
                m_hSlots = Mathf.Max(m_hSlots, 1);
                m_vSlots = Mathf.Max(m_vSlots, 1);
                EditorGUILayout.HelpBox("Set the size of the tile", MessageType.Info);
                MyAutoTileset.TileWidth = MyAutoTileset.TileHeight = EditorGUILayout.IntField("Tile Size In Pixels:", MyAutoTileset.TileWidth);
                EditorGUILayout.Space();
                if (GUILayout.Button("Generate Atlas of " + m_hSlots * MyAutoTileset.TilesetSlotSize + "x" + m_vSlots * MyAutoTileset.TilesetSlotSize))
                {
                    Texture2D atlasTexture = UtilsAutoTileMap.GenerateAtlas(MyAutoTileset, m_hSlots, m_vSlots);
                    if (atlasTexture)
                    {
                        string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MyAutoTileset));
                        string filePath = EditorUtility.SaveFilePanel("Save Atlas", path, MyAutoTileset.name + ".png", "png");
                        if (filePath.Length > 0)
                        {
                            byte[] bytes = atlasTexture.EncodeToPNG();
                            File.WriteAllBytes(filePath, bytes);

                            // make path relative to project
                            filePath = "Assets" + filePath.Remove(0, Application.dataPath.Length);

                            // Make sure LoadAssetAtPath and ImportTexture is going to work
                            AssetDatabase.Refresh();

                            UtilsAutoTileMap.ImportTexture(filePath);

                            // Link Atlas with asset to be able to save it in the prefab
                            MyAutoTileset.AtlasTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
                            UtilsAutoTileMap.ImportTexture(MyAutoTileset.AtlasTexture);
                        }
                        else
                        {
                            MyAutoTileset.AtlasTexture = null;
                        }
                    }
                    MyAutoTileset.GenerateAutoTileData();
                    EditorUtility.SetDirty(MyAutoTileset);
                }
            }

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties();
		}
	}
}