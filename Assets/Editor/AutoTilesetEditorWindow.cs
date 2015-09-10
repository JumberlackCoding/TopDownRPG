using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace CreativeSpore.RpgMapEditor
{
	public class AutoTilesetEditorWindow : EditorWindow 
	{
		AutoTileset m_autoTileset;
		int m_subTilesetIdx;
        string[] m_subTilesetNames;
		Texture2D m_tilesetTexture;
		Vector2 m_scrollPos;

        public enum eEditMode
        {
            TilesetAtlas,
            Collisions,
            ChangeTileset
        }
        eEditMode m_editMode = eEditMode.TilesetAtlas;

        public static void ShowDialog(AutoTileset _autoTileset, eEditMode mode = eEditMode.TilesetAtlas)
		{
			AutoTilesetEditorWindow window = (AutoTilesetEditorWindow)EditorWindow.GetWindow (typeof (AutoTilesetEditorWindow));
            window.m_editMode = mode;
			window.m_autoTileset = _autoTileset;
			window.wantsMouseMove = true;
		}

        private void _refreshSubTilesetNames()
        {
            m_subTilesetNames = new string[ m_autoTileset.SubTilesets.Count ];
            for( int i = 0; i < m_autoTileset.SubTilesets.Count; ++i )
            {
                m_subTilesetNames[i] = m_autoTileset.SubTilesets[i].Name;
            }
        }

        private eEditMode m_prevEditMode = eEditMode.TilesetAtlas;
        void OnGUI()
        {

            if (m_autoTileset == null || m_autoTileset.AtlasTexture == null)
            {
                Close();
                if (m_autoTileset != null)
                {
                    EditorGUIUtility.PingObject(m_autoTileset);
                    Selection.activeObject = m_autoTileset;
                }
            }
            else
            {
                string[] editModeNames = System.Enum.GetNames(typeof(eEditMode));
                m_editMode = (eEditMode)GUILayout.Toolbar((int)m_editMode, editModeNames);
                bool isFirstUpdate = m_editMode != m_prevEditMode;
                switch (m_editMode)
                {
                    case eEditMode.TilesetAtlas: OnGUI_TilesetAtlas(isFirstUpdate); break;
                    case eEditMode.Collisions: OnGUI_Collisions(isFirstUpdate); break;
                    case eEditMode.ChangeTileset: OnGUI_ChangeTileset(isFirstUpdate); break;
                }
                m_prevEditMode = m_editMode;
            }

            if (GUI.changed)
            {
                m_autoTileset.BuildSubTilesetsList();
                EditorUtility.SetDirty(m_autoTileset);
            }
        }

        private enum eAtlasEditMode
        {
            None,
            AddAutoTileset,
            AddNormalTileset,
            EditTileset,
            RemoveTileset,
        };
        private eAtlasEditMode m_atlasEditMode = eAtlasEditMode.None;
        void OnGUI_TilesetAtlas(bool isFirstUpdate)
        {
            GUILayout.Label("Tileset Atlas Configuration", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = m_atlasEditMode == eAtlasEditMode.AddAutoTileset? Color.gray : Color.white;
            if (GUILayout.Button("Add AutoTileset", GUILayout.Height(25))) m_atlasEditMode = eAtlasEditMode.AddAutoTileset;
            GUI.backgroundColor = m_atlasEditMode == eAtlasEditMode.AddNormalTileset ? Color.gray : Color.white;
            if (GUILayout.Button("Add NormalTileset", GUILayout.Height(25))) m_atlasEditMode = eAtlasEditMode.AddNormalTileset;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = m_atlasEditMode == eAtlasEditMode.EditTileset ? Color.gray : Color.white;
            if (GUILayout.Button("Edit Tileset", GUILayout.Height(25))) m_atlasEditMode = eAtlasEditMode.EditTileset;
            GUI.backgroundColor = m_atlasEditMode == eAtlasEditMode.RemoveTileset ? Color.gray : Color.white;
            if (GUILayout.Button("Remove Tileset", GUILayout.Height(25))) m_atlasEditMode = eAtlasEditMode.RemoveTileset;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            int cols = m_autoTileset.AtlasTexture.width / m_autoTileset.TilesetSlotSize;
            //int rows = m_autoTileset.AtlasTexture.height / k_TilesetSlotSize;

            Rect rAtlas = new Rect(0, 0, m_autoTileset.AtlasTexture.width, m_autoTileset.AtlasTexture.height);

            if (rAtlas.width - position.width > rAtlas.height - (position.height - 120f))
            {
                rAtlas.width = Mathf.Min(position.width, rAtlas.width);
                rAtlas.height *= rAtlas.width / m_autoTileset.AtlasTexture.width;
            }
            else
            {
                rAtlas.height = Mathf.Min((position.height - 120f), rAtlas.height);
                rAtlas.width *= rAtlas.height / m_autoTileset.AtlasTexture.height;
            }

            GUILayout.Box(m_autoTileset.AtlasTexture, GUILayout.Width(rAtlas.width), GUILayout.Height(rAtlas.height));

            Rect rGuiAtlas = GUILayoutUtility.GetLastRect();
            Vector2 vBtnOff = new Vector2(0.2f * rGuiAtlas.width / cols, 0.40f * rGuiAtlas.width / cols);
            Rect rButton = new Rect(0, 0, rGuiAtlas.width / cols -  2f * vBtnOff.x, rGuiAtlas.width / cols - 2f * vBtnOff.y);
            float scale = rAtlas.width / m_autoTileset.AtlasTexture.width;
            GUIStyle btnStyle = new GUIStyle("button");
            btnStyle.richText = true;
            btnStyle.fontSize = (int)(50*scale);
            btnStyle.fontStyle = FontStyle.Bold;            

            for (int i = 0; i < m_autoTileset.AtlasSlots.Count; ++i )
            {
                AtlasSlot atlasSlot = m_autoTileset.AtlasSlots[i];
                Rect rSlot = m_autoTileset.CalculateAtlasSlotRectByIdx(i);
                rButton.position = vBtnOff + rGuiAtlas.position + rSlot.position * scale;
                rButton.y = rGuiAtlas.y + rGuiAtlas.height - (rButton.y - rGuiAtlas.y) - rButton.height;

                if (atlasSlot.SubTilesets.Count == 0)
                {
                    GUI.color = new Color(0f, 0.7f, 1f, 0.8f);
                    Rect rSlotBox = rSlot;
                    rSlotBox.position *= scale;
                    rSlotBox.position += rGuiAtlas.position;
                    rSlotBox.width *= scale;
                    rSlotBox.height *= scale;
                    rSlotBox.y = rGuiAtlas.y + rGuiAtlas.height - (rSlotBox.y - rGuiAtlas.y) - rSlotBox.height;
                    GUI.Box(rSlotBox, "\n\nSlot " + (i + 1));
                }

                GUI.color = new Color(0f, 0.7f, 1f, 0.8f);
                
                //+++ Show name list
                if (m_atlasEditMode == eAtlasEditMode.None)
                {
                    if (atlasSlot.SubTilesets.Count == 1)
                    {
                        List<string> availableNames = m_autoTileset.CreateAvailableNameList();
                        availableNames.Insert(0, atlasSlot.SubTilesets[0].Name);
                        Rect rPopup = new Rect(rButton.x, rButton.y, 50, 20); rPopup.position -= vBtnOff;
                        int nameIdx = EditorGUI.Popup(rPopup, 0, availableNames.ToArray(), new GUIStyle("popup"));
                        atlasSlot.SubTilesets[0].Name = availableNames[nameIdx];
                    }
                    else
                    {
                        for (int j = 0; j < atlasSlot.SubTilesets.Count; ++j)
                        {
                            List<string> availableNames = m_autoTileset.CreateAvailableNameList();
                            availableNames.Insert(0, atlasSlot.SubTilesets[j].Name);
                            Rect rPopup = new Rect(rButton.x, rButton.y, 50, 20); rPopup.position -= vBtnOff;
                            if (j == 0)
                                rPopup.y += rSlot.height * scale / 2;
                            else if (j == 2)
                            {
                                rPopup.y += rSlot.height * scale / 2;
                                rPopup.x += rSlot.width * scale / 2;
                            }
                            else if (j == 3)
                            {
                                rPopup.x += rSlot.width * scale / 2;
                            }
                            int nameIdx = EditorGUI.Popup(rPopup, 0, availableNames.ToArray(), new GUIStyle("popup"));
                            atlasSlot.SubTilesets[j].Name = availableNames[nameIdx];
                        }
                    }
                }
                //---

                GUI.color = new Color(.5f, .5f, 0.5f, 0.7f);

                if (m_atlasEditMode == eAtlasEditMode.EditTileset && atlasSlot.SubTilesets.Count >= 1)
                {
                    if (GUI.Button(rButton, "<color=white>" + (atlasSlot.SubTilesets.Count > 1 ? "[+]" : "[A]") + "Edit</color>", btnStyle))
                    {
                        m_editAtlasSlot = atlasSlot;
                        m_editMode = eEditMode.ChangeTileset;
                        m_clearSlotOnCancel = false;
                        m_atlasEditMode = eAtlasEditMode.None;
                    }
                }
                else if (m_atlasEditMode == eAtlasEditMode.RemoveTileset && atlasSlot.SubTilesets.Count >= 1)
                {
                    if (GUI.Button(rButton, "<color=white>" + (atlasSlot.SubTilesets.Count > 1 ? "[+]" : "[A]") + "Remove</color>", btnStyle))
                    {
                        foreach( SubTilesetConf conf in atlasSlot.SubTilesets )
                        {
                            UtilsAutoTileMap.ClearAtlasArea(m_autoTileset.AtlasTexture, (int)conf.AtlasRec.x, (int)conf.AtlasRec.y, (int)conf.AtlasRec.width, (int)conf.AtlasRec.height);
                        }
                        atlasSlot.SubTilesets.Clear();
                        m_autoTileset.BuildSubTilesetsList();
                        EditorUtility.SetDirty(m_autoTileset);
                        if (m_autoTileset.SubTilesets.Count == 0)
                            m_atlasEditMode = eAtlasEditMode.None;
                    }
                }
                else if (m_atlasEditMode == eAtlasEditMode.AddAutoTileset && atlasSlot.SubTilesets.Count == 0)
                {
                    if (GUI.Button(rButton, "<color=white>Add AutoTileset</color>", btnStyle))
                    {
                        List<string> availableNames = m_autoTileset.CreateAvailableNameList();
                        atlasSlot.SubTilesets.Add(new SubTilesetConf() { Name = availableNames.Count > 0 ? availableNames[0] : "-", AtlasRec = rSlot, HasAutotiles = true });
                        m_autoTileset.BuildSubTilesetsList();
                        m_editAtlasSlot = atlasSlot;
                        m_editMode = eEditMode.ChangeTileset;
                        m_clearSlotOnCancel = true;
                        EditorUtility.SetDirty(m_autoTileset);
                    }
                }
                else if (m_atlasEditMode == eAtlasEditMode.AddNormalTileset && atlasSlot.SubTilesets.Count == 0)
                {
                    if (GUI.Button(rButton, "<color=white>Add Normal Tileset</color>", btnStyle))
                    {
                        List<string> availableNames = m_autoTileset.CreateAvailableNameList();
                        Rect rect = new Rect(rSlot.x, rSlot.y, rSlot.width / 2, rSlot.height / 2);
                        atlasSlot.SubTilesets.Add(new SubTilesetConf() { Name = availableNames.Count > 0 ? availableNames[0] : "-", AtlasRec = rect, HasAutotiles = false });
                        rect.y = rSlot.y + rect.height;
                        atlasSlot.SubTilesets.Add(new SubTilesetConf() { Name = availableNames.Count > 1 ? availableNames[1] : "-", AtlasRec = rect, HasAutotiles = false });
                        rect.x = rSlot.x + rect.width;
                        rect.y = rSlot.y;
                        atlasSlot.SubTilesets.Add(new SubTilesetConf() { Name = availableNames.Count > 2 ? availableNames[2] : "-", AtlasRec = rect, HasAutotiles = false });
                        rect.y = rSlot.y + rect.height;
                        atlasSlot.SubTilesets.Add(new SubTilesetConf() { Name = availableNames.Count > 3 ? availableNames[3] : "-", AtlasRec = rect, HasAutotiles = false });
                        m_autoTileset.BuildSubTilesetsList();
                        m_editAtlasSlot = atlasSlot;
                        m_editMode = eEditMode.ChangeTileset;
                        m_clearSlotOnCancel = true;
                        EditorUtility.SetDirty(m_autoTileset);
                    }
                }
            }
            GUI.color = Color.white;
            if ( m_atlasEditMode != eAtlasEditMode.None && GUILayout.Button("Cancel Action", GUILayout.Height(25))) m_atlasEditMode = eAtlasEditMode.None;
        }

        private string _GetSrcTextureName( int idx, bool hasAutotiles )
        {
            //+++ old values for 32x32 tiles, now depend on tile size
            int _512 = 16 * m_autoTileset.TileWidth;
            int _480 = 15 * m_autoTileset.TileWidth;
            int _384 = 12 * m_autoTileset.TileWidth;
            int _256 = 8 * m_autoTileset.TileWidth;
            //---

            switch(idx)
            {
                case 0: return hasAutotiles ? "--- Animated (A1) " + _512 + "x" + _384 + " ---" : "--- Objects (B) " + _512 + "x" + _512 + " ---";
                case 1: return hasAutotiles ? "--- Ground (A2) " + _512 + "x" + _384 + " ---" : "--- Objects (C) " + _512 + "x" + _512 + " ---";
                case 2: return hasAutotiles ? "--- Building (A3) " + _512 + "x" + _256 + " ---" : "--- Objects (D) " + _512 + "x" + _512 + " ---";
                case 3: return hasAutotiles ? "--- Wall (A4) " + _512 + "x" + _480 + " ---" : "--- Objects (E) " + _512 + "x" + _512 + " ---";
                case 4: return hasAutotiles ? "--- Normal (A5) " + _256 + "x" + _512 + " ---" : "<unknown>";
            }
            return "<unknown>";
        }

        private bool _validateTilesetTextures( AtlasSlot atlasSlot )
        {
            //+++ old values for 32x32 tiles, now depend on tile size
            int _512 = 16 * m_autoTileset.TileWidth;
            int _480 = 15 * m_autoTileset.TileWidth;
            int _384 = 12 * m_autoTileset.TileWidth;
            int _256 = 8 * m_autoTileset.TileWidth;
            //---

            List<Texture2D> invalidTextures = new List<Texture2D>();
            for (int i = 0; i < atlasSlot.SubTilesets.Count; ++i )
            {
                SubTilesetConf tilesetConf = atlasSlot.SubTilesets[i];
                if( tilesetConf.HasAutotiles )
                {
                    for( int j = 0; j < tilesetConf.SourceTexture.Length; ++j )
                    {
                        if ( tilesetConf.SourceTexture[j] != null &&
                            (
                            (j == 0) && (tilesetConf.SourceTexture[j].width != _512 || tilesetConf.SourceTexture[j].height != _384) ||
                            (j == 1) && (tilesetConf.SourceTexture[j].width != _512 || tilesetConf.SourceTexture[j].height != _384) ||
                            (j == 2) && (tilesetConf.SourceTexture[j].width != _512 || tilesetConf.SourceTexture[j].height != _256) ||
                            (j == 3) && (tilesetConf.SourceTexture[j].width != _512 || tilesetConf.SourceTexture[j].height != _480) ||
                            (j == 4) && (tilesetConf.SourceTexture[j].width != _256 || tilesetConf.SourceTexture[j].height != _512)
                            )
                           )
                        {
                            invalidTextures.Add(tilesetConf.SourceTexture[j]);
                            tilesetConf.SourceTexture[j] = null;
                        }
                    }
                }
                else
                {
                    if ( tilesetConf.SourceTexture[0] != null && (tilesetConf.SourceTexture[0].width != _512 || tilesetConf.SourceTexture[0].height != _512))
                    {
                        invalidTextures.Add(tilesetConf.SourceTexture[0]);
                        tilesetConf.SourceTexture[0] = null;
                    }
                }
            }
            if( invalidTextures.Count > 0 )
            {
                string wrongTextures = "";
                foreach (Texture2D text in invalidTextures) wrongTextures += "\n" + text.name;
                EditorUtility.DisplayDialog("Wrong texture size", "One or more textures have a wrong texture size:" + wrongTextures, "Ok");
            }
            return invalidTextures.Count == 0;
        }

        private AtlasSlot m_editAtlasSlot;
        private Vector2 m_changeTilesetScrollPos;
        private bool m_clearSlotOnCancel = false;
        void OnGUI_ChangeTileset(bool isFirstUpdate)
        {            
            GUILayout.Label("Sub Tileset Configuration", EditorStyles.boldLabel);
            GUILayout.Space(20);
            GUILayout.Label("Tile Size: "+m_autoTileset.TileWidth);
            m_changeTilesetScrollPos = GUILayout.BeginScrollView(m_changeTilesetScrollPos);

            if (m_editAtlasSlot == null || m_editAtlasSlot.SubTilesets.Count == 0)
            {
                EditorGUILayout.HelpBox("There is no tileset selected! Go to TilesetAtlas tab, press Edit Tileset button and select a tileset to be edited.", MessageType.Warning);
                if (GUILayout.Button("Cancel"))
                {
                    m_editMode = eEditMode.TilesetAtlas;
                    if( m_clearSlotOnCancel )
                    {
                        m_clearSlotOnCancel = false;
                        m_editAtlasSlot.SubTilesets.Clear();
                    }
                }
            }
            else
            {
                GUILayout.Space(20);
                if (GUILayout.Button("Accept", GUILayout.Height(25)))
                {
                    if (_validateTilesetTextures(m_editAtlasSlot))
                    {
                        foreach (SubTilesetConf tilesetConf in m_editAtlasSlot.SubTilesets)
                        {
                            UtilsAutoTileMap.CopySubTilesetInAtlas(m_autoTileset, tilesetConf);
                        }
                        m_autoTileset.AtlasTexture.Apply();
                        UtilsAutoTileMap.SaveTextureAsset(m_autoTileset.AtlasTexture);
                        EditorUtility.SetDirty(m_autoTileset);
                        m_editMode = eEditMode.TilesetAtlas;
                    }
                }
                GUILayout.Space(20);
                if (GUILayout.Button("Cancel", GUILayout.Height(25)))
                {
                    m_editMode = eEditMode.TilesetAtlas;
                    if (m_clearSlotOnCancel)
                    {
                        m_clearSlotOnCancel = false;
                        m_editAtlasSlot.SubTilesets.Clear();
                    }
                }
                GUILayout.Space(20);

                EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
                if (m_editAtlasSlot.SubTilesets.Count == 1)
                { // if it is an autotile tilesets, it's made of 5 textures
                    for (int i = 0; i < m_editAtlasSlot.SubTilesets[0].SourceTexture.Length; ++i)
                    {
                        EditorGUILayout.LabelField(_GetSrcTextureName(i, m_editAtlasSlot.SubTilesets[0].HasAutotiles));
                        m_editAtlasSlot.SubTilesets[0].SourceTexture[i] = EditorGUILayout.ObjectField(m_editAtlasSlot.SubTilesets[0].SourceTexture[i] == null ? "" : m_editAtlasSlot.SubTilesets[0].SourceTexture[i].name, m_editAtlasSlot.SubTilesets[0].SourceTexture[i], typeof(Texture2D), false) as Texture2D;
                    }
                }
                else
                { // if it is an object tileset, we have to modify this and the other 3 as object tilesets came in groups of 4
                    for (int i = 0; i < m_editAtlasSlot.SubTilesets.Count; ++i)
                    {
                        EditorGUILayout.LabelField(_GetSrcTextureName(i, m_editAtlasSlot.SubTilesets[i].HasAutotiles));
                        m_editAtlasSlot.SubTilesets[i].SourceTexture[0] = EditorGUILayout.ObjectField(m_editAtlasSlot.SubTilesets[i].SourceTexture[0] == null ? "" : m_editAtlasSlot.SubTilesets[i].SourceTexture[0].name, m_editAtlasSlot.SubTilesets[i].SourceTexture[0], typeof(Texture2D), false) as Texture2D;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        void OnGUI_Collisions(bool isFirstUpdate) 
		{
			GUILayout.Label ("Tileset Collision Configuration", EditorStyles.boldLabel);

			if( m_autoTileset.AtlasTexture == null )
			{
				Selection.activeObject = m_autoTileset;
				GUILayout.Label("You have to create a texture atlas first");
				return;
			}

			GUILayout.BeginHorizontal();

			if( GUILayout.Button( "Default Configuration") )
			{
				bool isOk = EditorUtility.DisplayDialog("Set Default Collision Data", "Are you sure?", "Yes");
				if( isOk )
				{
					SetDefaultConfig();
					EditorUtility.SetDirty( m_autoTileset );
				}
			}
            
			if( GUILayout.Button( "Clear") )
			{
				bool isOk = EditorUtility.DisplayDialog("Clear Collision Data", "Are you sure?", "Yes");
				if( isOk )
				{
                    System.Array.Clear(m_autoTileset.AutotileCollType, m_subTilesetIdx * AutoTileset.k_TilesPerSubTileset, AutoTileset.k_TilesPerSubTileset);
					EditorUtility.SetDirty( m_autoTileset );
				}
			}

			GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Press left or right mouse button over the tiles to change the collision type. You can do this also while editing a map in the editor or while playing.", MessageType.Info);

            // refresh data if needed
            if( isFirstUpdate )
            {
                m_autoTileset.GenerateAutoTileData(); // be sure to data is ok before editing in case of some modification was made                
                _refreshSubTilesetNames();
                m_subTilesetIdx = Mathf.Clamp(m_subTilesetIdx, 0, m_autoTileset.SubTilesets.Count);
            }
			m_subTilesetIdx = EditorGUILayout.Popup( "Tileset: ", m_subTilesetIdx, m_subTilesetNames );

			if( GUI.changed || m_tilesetTexture == null )
			{
				m_tilesetTexture = UtilsAutoTileMap.GenerateTilesetTexture( m_autoTileset, m_autoTileset.SubTilesets[ m_subTilesetIdx ] );
			}

			float fScrollBarWidth = 16f;
            float fScale = 32f/m_autoTileset.TileWidth; // scale texture to have the same size as when tile size was 32x32
            Rect rTileset = new Rect(0f, 0f, (float)m_tilesetTexture.width * fScale, (float)m_tilesetTexture.height * fScale);
            Rect rScrollView = new Rect(50, 140, rTileset.width + fScrollBarWidth, position.height - 150);
			if( m_tilesetTexture != null )
			{
                Rect rTile = new Rect(0, 0, 32, 32);
				// BeginScrollView
				m_scrollPos = GUI.BeginScrollView( rScrollView, m_scrollPos, rTileset);
				{
					GUI.DrawTexture( rTileset, m_tilesetTexture );

					for( int autoTileLocalIdx = 0; autoTileLocalIdx < 256; ++autoTileLocalIdx ) //autoTileLocalIdx: index of current tileset group
					{
                        rTile.x = rTileset.x + (autoTileLocalIdx % m_autoTileset.AutoTilesPerRow) * 32;
                        rTile.y = rTileset.y + (autoTileLocalIdx / m_autoTileset.AutoTilesPerRow) * 32;

						int autoTileIdx = autoTileLocalIdx + (int)m_subTilesetIdx * 256; // global autotile idx
						if (Event.current.type == EventType.MouseUp)
						{
							if( rTile.Contains( Event.current.mousePosition ) )
							{
								int collType = (int)m_autoTileset.AutotileCollType[ autoTileIdx ];
								if( Event.current.button == 0 )
								{
									collType += 1; // go next
								}
								else if( Event.current.button == 1 )
								{
									collType += (int)AutoTileMap.eTileCollisionType._SIZE - 1; // go back
								}
								collType%=(int)AutoTileMap.eTileCollisionType._SIZE;
								m_autoTileset.AutotileCollType[ autoTileIdx ] = (AutoTileMap.eTileCollisionType)(collType);
							}
							EditorUtility.SetDirty( m_autoTileset );
						}

						string sCollision = "";
						switch( m_autoTileset.AutotileCollType[autoTileIdx] )
						{
						case AutoTileMap.eTileCollisionType.BLOCK: sCollision = "■"; break;
						case AutoTileMap.eTileCollisionType.FENCE: sCollision = "#"; break;
						case AutoTileMap.eTileCollisionType.WALL: sCollision = "□"; break;
						case AutoTileMap.eTileCollisionType.OVERLAY: sCollision = "★"; break;
						}
						if( sCollision.Length > 0 )
						{
							GUI.color = new Color(1f, 1f, 1f, 1f);
							GUIStyle style = new GUIStyle();
							style.fontSize = 30;
							style.fontStyle = FontStyle.Bold;
							style.alignment = TextAnchor.MiddleCenter;
							style.normal.textColor = Color.white;
							GUI.Box( rTile, sCollision, style );
							GUI.color = Color.white;
						}

						//Show alpha tiles (debug)
						/*/
						if( m_autoTileset.IsAutoTileHasAlpha[autoTileIdx] )
						{
							GUIStyle style = new GUIStyle();
							style.fontSize = 30;
							style.fontStyle = FontStyle.Bold;
							style.alignment = TextAnchor.MiddleCenter;
							style.normal.textColor = Color.blue;
							GUI.Box( rTile, "A", style );
						}
						//*/
					}
				}
				GUI.EndScrollView();

				// Help Info
				{
					GUIStyle style = new GUIStyle();
					style.fontSize = 15;
					style.fontStyle = FontStyle.Bold;
					//NOTE: if you don't see the special characters properly, be sure this file is saved in UTF-8
					GUI.Label( new Rect( rScrollView.xMax + 30, rScrollView.y + 40, 100, 100), "■ - Block Collision", style);
					GUI.Label( new Rect( rScrollView.xMax + 30, rScrollView.y + 60, 100, 100), "□ - Wall Collision", style);
					GUI.Label( new Rect( rScrollView.xMax + 30, rScrollView.y + 80, 100, 100), "# - Fence Collision", style);
					GUI.Label( new Rect( rScrollView.xMax + 30, rScrollView.y + 100, 100, 100), "★ - Overlay", style);
				}

				Repaint();
			}
		}

		void SetDefaultConfig()
		{
            int baseIdx = m_subTilesetIdx * AutoTileset.k_TilesPerSubTileset;
			// Set animated collisions
			for( int i = 0; i < 16; ++i )
			{
                m_autoTileset.AutotileCollType[baseIdx+i] = AutoTileMap.eTileCollisionType.WALL;
			}

			// Set building collision
			for( int i = 48; i < 80; ++i )
			{
                m_autoTileset.AutotileCollType[baseIdx+i] = AutoTileMap.eTileCollisionType.BLOCK;
			}
			// Set wall collision
			for( int i = 80; i < 128; ++i )
			{
                m_autoTileset.AutotileCollType[baseIdx + i] = ((i / m_autoTileset.AutoTilesPerRow) % 2 == 0) ? AutoTileMap.eTileCollisionType.WALL : AutoTileMap.eTileCollisionType.BLOCK;
			}
            EditorUtility.SetDirty( m_autoTileset );
		}
	}
}