using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;

namespace CreativeSpore.RpgMapEditor
{
	[CustomEditor(typeof(AutoTileMap))]
	public class AutoTileMapEditor : Editor
	{
		AutoTileMap MyAutoTileMap { get { return (AutoTileMap)target; } }
		static bool s_isEditModeOn = false;
		TilesetComponent m_tilesetComponent;

		int m_mapWidth;
		int m_mapHeight;
		bool m_showCollisions = false;

        // Thanks to http://va.lent.in/unity-make-your-lists-functional-with-reorderablelist/
        ReorderableList m_layerList;

        string[] m_sortingLayerNames;


		void OnEnable()
		{
            m_sortingLayerNames = GetSortingLayerNames();
			m_tilesetComponent = new TilesetComponent( MyAutoTileMap );
			if( MyAutoTileMap.MapData != null )
			{
				m_mapWidth = MyAutoTileMap.MapData.Data.TileMapWidth;
				m_mapHeight = MyAutoTileMap.MapData.Data.TileMapHeight;
			}
			if( MyAutoTileMap.BrushGizmo != null )
			{
				MyAutoTileMap.BrushGizmo.gameObject.SetActive(false);
			}

            m_layerList = new ReorderableList(serializedObject, serializedObject.FindProperty("MapLayers"), true, true, true, true);
            m_layerList.drawElementCallback += _LayerList_DrawElementCallback;
            m_layerList.drawHeaderCallback = (Rect rect) =>
            {  
                EditorGUI.LabelField(rect, "Map Layers");
            };
            m_layerList.onChangedCallback = (ReorderableList list) =>
            {
                //NOTE: When reordering elements in ReorderableList, elements are not moved, but data is swapped between them.
                // So if you keep addres of element 0 ex: data = list[0], after reordering element 0 with 1, data will contain the elemnt1 data.
                // Keeping a reference to MapLayer in TileChunks is useless
                serializedObject.ApplyModifiedProperties(); // apply adding and removing changes
                MyAutoTileMap.SaveMap();
                MyAutoTileMap.LoadMap();
            };
            m_layerList.onAddCallback = (ReorderableList list) =>
            {
                list.index = MyAutoTileMap.MapLayers.Count; // select added layer
                MyAutoTileMap.AddMapLayer();
            };
		}

        // Get the sorting layer names
        public string[] GetSortingLayerNames()
        {
            System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        private void _LayerList_DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            var element = m_layerList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2;
            float elemWidth = 20; float elemX = rect.x;
            EditorGUI.PropertyField(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("Visible"), GUIContent.none);
            elemX += elemWidth; elemWidth = Mathf.Clamp(Screen.width - 500, 50, 240); //NOTE: Screen.width - n, avoid left element to overlap right padding elements when resizing inspector width
            EditorGUI.PropertyField(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("Name"), GUIContent.none);
            elemX += elemWidth; elemWidth = 80;
            EditorGUI.PropertyField(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("LayerType"), GUIContent.none);
            EditorGUIUtility.labelWidth = 75f;
            elemX += elemWidth; elemWidth = 155;
            int sortingLayerIdx = System.Array.IndexOf(m_sortingLayerNames, element.FindPropertyRelative("SortingLayer").stringValue);
            if (sortingLayerIdx < 0)
            {
                Debug.LogError(" Sorting Layer " + element.FindPropertyRelative("SortingLayer").stringValue + " not found! Default layer will be taken instead.");
                sortingLayerIdx = 0;
            }
            sortingLayerIdx = EditorGUI.Popup(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight),
                "SortingLayer:", sortingLayerIdx, m_sortingLayerNames);
            element.FindPropertyRelative("SortingLayer").stringValue = m_sortingLayerNames[sortingLayerIdx];
            elemX += elemWidth; elemWidth = 90;
            EditorGUIUtility.labelWidth = 45f;
            EditorGUI.PropertyField(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("SortingOrder"), new GUIContent() { text = "Order:" });
            elemX += elemWidth; elemWidth = 90;
            EditorGUIUtility.labelWidth = 45f;
            EditorGUI.PropertyField(
                new Rect(elemX, rect.y, elemWidth, EditorGUIUtility.singleLineHeight), 
                element.FindPropertyRelative("Depth"), new GUIContent() { text = "Depth:" });

            EditorGUIUtility.labelWidth = savedLabelWidth;
        }

		void OnDisable()
		{
			if( MyAutoTileMap != null )
			{
				if( MyAutoTileMap.BrushGizmo != null )
				{
					MyAutoTileMap.BrushGizmo.gameObject.SetActive(false);
				}

				if( s_isEditModeOn )
				{
					Debug.LogWarning(" You forgot to commit map changes! Map will be saved automatically for you! ");
					MyAutoTileMap.SaveMap();
				}
			}
		}

		public void OnSceneGUI()
		{
			if( !MyAutoTileMap.IsInitialized )
			{
				return;
			}

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            Handles.Label(HandleUtility.GUIPointToWorldRay(Vector3.zero).origin, " Brush Pos: " + MyAutoTileMap.BrushGizmo.BrushTilePos, style);
            Handles.Label(HandleUtility.GUIPointToWorldRay(new Vector3(0f, 16f)).origin, " Select Tile Idx: " + m_tilesetComponent.SelectedTileIdx, style);

            Rect rAutoTileMap = new Rect(MyAutoTileMap.transform.position.x, MyAutoTileMap.transform.position.y, MyAutoTileMap.MapTileWidth * MyAutoTileMap.Tileset.TileWorldWidth, -MyAutoTileMap.MapTileHeight * MyAutoTileMap.Tileset.TileWorldHeight);
			UtilsGuiDrawing.DrawRectWithOutline( rAutoTileMap, new Color(0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, 1f));
			if( m_showCollisions )
			{
				DrawCollisions();
			}

			if (s_isEditModeOn)
			{
				int controlID = GUIUtility.GetControlID(FocusType.Passive);
				HandleUtility.AddDefaultControl(controlID);
				EventType currentEventType = Event.current.GetTypeForControl(controlID);
				bool skip = false;
				int saveControl = GUIUtility.hotControl;

                if (currentEventType == EventType.Layout) { skip = true; }
                else if (currentEventType == EventType.ScrollWheel) { skip = true; }

                if (!skip)
                {
                    EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), MouseCursor.Arrow);
                    GUIUtility.hotControl = controlID;

                    m_tilesetComponent.OnSceneGUI();

                    if (currentEventType == EventType.MouseDrag && Event.current.button < 2) // 2 is for central mouse button
                    {
                        // avoid dragging the map
                        Event.current.Use();
                    }
                }

				GUIUtility.hotControl = saveControl;

				if (GUI.changed) 
				{
					EditorUtility.SetDirty(target);
				}
			}
		}

        enum eTabType
        {
            Settings,
            Paint,
            Data
        }
        private static eTabType s_tabType = eTabType.Settings;

		public override void OnInspectorGUI()
		{
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update();

			EditorGUILayout.BeginHorizontal();
			if( MyAutoTileMap.Tileset != null && MyAutoTileMap.Tileset.AtlasTexture == null )
			{
				MyAutoTileMap.Tileset.AtlasTexture = EditorGUILayout.ObjectField ("Tileset Atlas", MyAutoTileMap.Tileset.AtlasTexture, typeof(Texture2D), false) as Texture2D;
				if (GUILayout.Button("Edit Tileset..."))
				{
					AutoTilesetEditorWindow.ShowDialog( MyAutoTileMap.Tileset );
				}
			}
			else
			{
				MyAutoTileMap.Tileset = (AutoTileset)EditorGUILayout.ObjectField("Tileset", MyAutoTileMap.Tileset, typeof(AutoTileset), false);
			}

			if( MyAutoTileMap.Tileset == null )
			{
				if( GUILayout.Button("Create...") )
				{
					MyAutoTileMap.Tileset = RpgMapMakerEditor.CreateTileset();
				}
			}
			EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
			MyAutoTileMap.MapData = (AutoTileMapData)EditorGUILayout.ObjectField("Map Data", MyAutoTileMap.MapData, typeof(AutoTileMapData), false);
			if( MyAutoTileMap.MapData == null && GUILayout.Button("Create...") )
			{
				MyAutoTileMap.MapData = RpgMapMakerEditor.CreateAutoTileMapData();
			}
			EditorGUILayout.EndHorizontal();

			if( MyAutoTileMap.Tileset != null && MyAutoTileMap.MapData != null && MyAutoTileMap.IsInitialized )
			{
                string[] toolBarButtonNames = System.Enum.GetNames(typeof(eTabType));

                s_tabType = (eTabType)GUILayout.Toolbar((int)s_tabType, toolBarButtonNames);
                switch (s_tabType)
                {
                    case eTabType.Settings: DrawSettingsTab(); break;
                    case eTabType.Paint: DrawPaintTab(); break;
                    case eTabType.Data: DrawDataTab(); break;
                }								
			}
            else if (!MyAutoTileMap.IsInitialized)
            {
                MyAutoTileMap.LoadMap();
            }
            else
            {
                EditorGUILayout.HelpBox("You need to select or create a Tileset and a Map Data asset", MessageType.Info);
            }

			if( GUI.changed )
			{
				EditorUtility.SetDirty( MyAutoTileMap );
				if( MyAutoTileMap.Tileset != null )
					EditorUtility.SetDirty( MyAutoTileMap.Tileset );
				if( MyAutoTileMap.MapData != null )
					EditorUtility.SetDirty( MyAutoTileMap.MapData );
			}

			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties();
		}	

        void DrawSettingsTab()
        {
            MyAutoTileMap.ViewCamera = (Camera)EditorGUILayout.ObjectField("View Camera", MyAutoTileMap.ViewCamera, typeof(Camera), true);
            m_mapWidth = EditorGUILayout.IntField("Map Width", m_mapWidth);
            m_mapHeight = EditorGUILayout.IntField("Map Height", m_mapHeight);
            if (m_mapWidth != MyAutoTileMap.MapData.Data.TileMapWidth || m_mapHeight != MyAutoTileMap.MapData.Data.TileMapHeight)
            {
                //TODO: refactor resize dimensions
                if (GUILayout.Button("Apply New Dimensions"))
                {
                    MyAutoTileMap.SaveMap(m_mapWidth, m_mapHeight);
                    MyAutoTileMap.LoadMap();
                }
            }

            MyAutoTileMap.AnimatedTileSpeed = EditorGUILayout.FloatField("Animated Tile Speed", MyAutoTileMap.AnimatedTileSpeed);

            MyAutoTileMap.AutoTileMapGui.enabled = EditorGUILayout.Toggle("Show Map Editor On Play", MyAutoTileMap.AutoTileMapGui.enabled);
            if (Application.isPlaying)
            {
                MyAutoTileMap.IsCollisionEnabled = EditorGUILayout.Toggle("Collision Enabled", MyAutoTileMap.IsCollisionEnabled);
            }
            else
            {
                Renderer minimapRenderer = MyAutoTileMap.EditorMinimapRender.GetComponent<Renderer>();
                bool prevEnabled = minimapRenderer.enabled;
                minimapRenderer.enabled = EditorGUILayout.Toggle("Show Minimap", minimapRenderer.enabled);                
                if (!prevEnabled && minimapRenderer.enabled) MyAutoTileMap.RefreshMinimapTexture();
                MyAutoTileMap.BrushGizmo.IsRefreshMinimapEnabled = minimapRenderer.enabled;
            }
            m_showCollisions = EditorGUILayout.Toggle("Show Collisions", m_showCollisions);
            MyAutoTileMap.ShowGrid = EditorGUILayout.Toggle("Show Grid", MyAutoTileMap.ShowGrid);
            MyAutoTileMap.SaveChangesAfterPlaying = EditorGUILayout.Toggle("Save Changes After Playing", MyAutoTileMap.SaveChangesAfterPlaying);

            //DrawDefaultInspector();
        }

        void DrawDataTab()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (GUILayout.Button("Reload Map"))
            {
                MyAutoTileMap.LoadMap();
            }

            if (GUILayout.Button("Import Map..."))
            {
                if (MyAutoTileMap.ShowLoadDialog())
                {
                    EditorUtility.SetDirty(MyAutoTileMap);
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Export Map..."))
            {
                MyAutoTileMap.ShowSaveDialog();
            }

            if (GUILayout.Button("Clear Map..."))
            {
                bool isOk = EditorUtility.DisplayDialog("Clear Map...", "Are you sure?", "Yes");
                if (isOk)
                {
                    MyAutoTileMap.ClearMap();
                    MyAutoTileMap.SaveMap();
                }
            }

            if (GUILayout.Button("Generate Map..."))
            {
                bool isOk = EditorUtility.DisplayDialog("Generate Map...", "Are you sure?", "Yes");
                if (isOk)
                {
                    MyAutoTileMap.ClearMap();

                    // set the right layer index if default layers are changed
                    int gndLayer = 0;
                    int gndOverlay = 1;                    

                    float fDiv = 25f;
                    float xf = Random.value * 100;
                    float yf = Random.value * 100;
                    for (int i = 0; i < MyAutoTileMap.MapTileWidth; i++)
                    {
                        for (int j = 0; j < MyAutoTileMap.MapTileHeight; j++)
                        {
                            float fRand = Random.value;
                            float noise = Mathf.PerlinNoise((i + xf) / fDiv, (j + yf) / fDiv);
                            //Debug.Log( "noise: "+noise+"; i: "+i+"; j: "+j );
                            if (noise < 0.3) //water
                            {
                                MyAutoTileMap.SetAutoTile(i, j, 0, gndLayer, false);
                            }
                            else if (noise < 0.4) // water plants
                            {
                                MyAutoTileMap.SetAutoTile(i, j, 0, gndLayer, false);
                                if (fRand < noise / 3)
                                    MyAutoTileMap.SetAutoTile(i, j, 5, gndOverlay, false);
                            }
                            else if (noise < 0.5 && fRand < (1 - noise / 2)) // dark grass
                            {
                                MyAutoTileMap.SetAutoTile(i, j, 32, gndLayer, false);
                            }
                            else if (noise < 0.6 && fRand < (1 - 1.2 * noise)) // flowers
                            {
                                //MyAutoTileMap.AddAutoTile( i, j, 24, (int)AutoTileMap.eTileLayer.GROUND);
                                MyAutoTileMap.SetAutoTile(i, j, 144, gndLayer, false);
                                MyAutoTileMap.SetAutoTile(i, j, 288 + Random.Range(0, 5), gndOverlay, false);
                            }
                            else if (noise < 0.7) // grass
                            {
                                MyAutoTileMap.SetAutoTile(i, j, 144, gndLayer, false);
                            }
                            else // mountains
                            {
                                MyAutoTileMap.SetAutoTile(i, j, 33, gndLayer, false);
                            }
                        }
                    }
                    //float now, now2;
                    //now = Time.realtimeSinceStartup;

                    //now2 = Time.realtimeSinceStartup;
                    MyAutoTileMap.RefreshAllTiles();
                    //Debug.Log("RefreshAllTiles execution time(ms): " + (Time.realtimeSinceStartup - now2) * 1000);

                    //now2 = Time.realtimeSinceStartup;
                    MyAutoTileMap.SaveMap();
                    //Debug.Log("SaveMap execution time(ms): " + (Time.realtimeSinceStartup - now2) * 1000);

                    MyAutoTileMap.RefreshMinimapTexture();

                    //now2 = Time.realtimeSinceStartup;
                    MyAutoTileMap.UpdateChunks();
                    //Debug.Log("UpdateChunks execution time(ms): " + (Time.realtimeSinceStartup - now2) * 1000);

                    //Debug.Log("Total execution time(ms): " + (Time.realtimeSinceStartup - now) * 1000);

                }
            }
        }

        void DrawPaintTab()
        {
            if (MyAutoTileMap.BrushGizmo != null)
            {
                MyAutoTileMap.BrushGizmo.gameObject.SetActive(s_isEditModeOn);
            }
            if (s_isEditModeOn)
            {
                if (GUILayout.Button("Commit"))
                {
                    s_isEditModeOn = false;                 
                    MyAutoTileMap.SaveMap();
                    EditorUtility.SetDirty(MyAutoTileMap);
                    EditorUtility.SetDirty(MyAutoTileMap.Tileset);
                    EditorUtility.SetDirty(MyAutoTileMap.MapData);
                    AssetDatabase.SaveAssets();
                    Repaint();
                }

                // avoid value -1 (  no row selected ) if there is at least an element
                if( m_layerList.count > 0 )
                {
                    m_layerList.index = Mathf.Max(m_layerList.index, 0);
                    EditorGUILayout.HelpBox("Selected Layer: " + MyAutoTileMap.MapLayers[m_layerList.index].Name, MessageType.None);
                    m_layerList.DoLayoutList();
                    MyAutoTileMap.BrushGizmo.SelectedLayer = m_layerList.index;
                    MyAutoTileMap.UpdateChunkLayersData(); //update layer data changed by DoLayoutList
                }
                else
                {
                    m_layerList.DoLayoutList();
                }

                if (GUILayout.Button("Clear Selected Layer: " + MyAutoTileMap.MapLayers[m_layerList.index].Name))
                {
                    if (EditorUtility.DisplayDialog("Clear Layer " + MyAutoTileMap.MapLayers[m_layerList.index].Name, "Are your sure? This action cannot be undone!", "Clear Layer"))
                    {
                        MyAutoTileMap.ClearLayer(MyAutoTileMap.MapLayers[m_layerList.index]);
                        MyAutoTileMap.MarkLayerChunksForUpdate(MyAutoTileMap.MapLayers[m_layerList.index]);
                        MyAutoTileMap.UpdateChunks();
                    }
                }

                GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });                
                MyAutoTileMap.BrushGizmo.SmartBrushEnabled = EditorGUILayout.ToggleLeft("Smart Brush", MyAutoTileMap.BrushGizmo.SmartBrushEnabled, EditorStyles.boldLabel);
                if (MyAutoTileMap.BrushGizmo.SmartBrushEnabled)
                {
                    GUILayout.Box("Tiles with overlay collision ★ will be placed in the first overlay layer found over selected layer");
                    GUILayout.Box("Tiles with alpha will be placed in the layer over the selected layer");
                }
                else
                {
                    GUILayout.Box("Tiles will be placed in the selected layer");
                }

                m_tilesetComponent.OnInspectorGUI();
                m_tilesetComponent.IsEditCollision = Event.current.shift;

                Repaint();

            }
            else
            {                
                GUILayout.BeginVertical();
                if (GUILayout.Button("Edit"))
                {
                    s_isEditModeOn = true;
                    Repaint();
                }
                EditorGUILayout.HelpBox("Press Edit to start painting and remember to commit your changes to be sure they are saved into asset map data.", MessageType.Info);
                GUILayout.EndVertical();
            }
        }

		void DrawCollisions()
		{
            float fCollW = MyAutoTileMap.Tileset.TileWorldWidth / 4;
            float fCollH = MyAutoTileMap.Tileset.TileWorldHeight / 4;
			Rect rColl = new Rect(0, 0, fCollW, -fCollH);
			Color cColl = new Color( 1f, 0f, 0f, 0.1f );
			Vector3 vTopLeft = HandleUtility.GUIPointToWorldRay(Vector3.zero).origin;
			Vector3 vBottomRight = HandleUtility.GUIPointToWorldRay( new Vector3( Screen.width, Screen.height ) ).origin;
			vTopLeft.y = -vTopLeft.y;
			vBottomRight.y = -vBottomRight.y;
			vTopLeft.x -= (vTopLeft.x % fCollW) + fCollW/2;
			vTopLeft.y -= (vTopLeft.y % fCollH) + fCollH/2;
			vBottomRight.x -= (vBottomRight.x % fCollW) - fCollW/2;
			vBottomRight.y -= (vBottomRight.y % fCollH) - fCollH/2;
            for (float y = vTopLeft.y; y <= vBottomRight.y; y += MyAutoTileMap.Tileset.TileWorldHeight / 4)
			{
                for (float x = vTopLeft.x; x <= vBottomRight.x; x += MyAutoTileMap.Tileset.TileWorldWidth / 4)
				{
					AutoTileMap.eTileCollisionType collType = MyAutoTileMap.GetAutotileCollisionAtPosition( new Vector3( x, -y ) );
					if( collType != AutoTileMap.eTileCollisionType.PASSABLE )
					{
						rColl.position = new Vector2(x-fCollW/2, -(y-fCollH/2));
						UtilsGuiDrawing.DrawRectWithOutline( rColl, cColl, cColl );
					}
				}
			}
		}
	}
}