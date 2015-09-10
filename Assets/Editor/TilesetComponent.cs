using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// Draw and manage the tileset using the Unity GUI
    /// </summary>
	public class TilesetComponent
	{
        const int k_visualTileWidth = 32; // doesn't matter the tileset tile size, this size will be used to paint it in the inspector
        const int k_visualTileHeight = 32;

		public bool IsEditCollision;
		public int SelectedTileIdx { get{ return m_selectedTileIdx; } }
		
		Vector2 m_scrollPos;
		int m_subTilesetIdx;
        string[] m_subTilesetNames;
		Texture2D m_tilesetTexture;
		AutoTileMap m_autoTileMap;

		int m_selectedTileIdx = 0;

		public TilesetComponent( AutoTileMap _autoTileMap )
		{
			m_autoTileMap = _autoTileMap;
		}

		private int m_prevMouseTileX = -1;
		private int m_prevMouseTileY = -1;
		private int m_startDragTileX = -1;
		private int m_startDragTileY = -1;
		private int m_dragTileX = -1;
		private int m_dragTileY = -1;

		public void OnSceneGUI()
		{

			#region Undo / Redo
			if( Event.current.isKey && Event.current.shift && Event.current.type == EventType.KeyUp )
			{
				if( Event.current.keyCode == KeyCode.Z )
				{
					m_autoTileMap.BrushGizmo.UndoAction();
				}
				if( Event.current.keyCode == KeyCode.Y )
				{
					m_autoTileMap.BrushGizmo.RedoAction();
				}
			}
			#endregion

			Rect rSceneView = new Rect( 0, 0, Screen.width, Screen.height );
			if( rSceneView.Contains( Event.current.mousePosition ) )
			{
				UpdateMouseInputs();

				Ray ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
				Plane hPlane = new Plane(Vector3.forward, Vector3.zero);		
				float distance = 0; 
				if ( hPlane.Raycast(ray, out distance) )
				{
					// get the hit point:
					Vector3 vPos = ray.GetPoint(distance);
					m_autoTileMap.BrushGizmo.UpdateBrushGizmo( vPos );

					if( m_isMouseRight || m_isMouseLeft )
					{
						int tile_x = (int)(vPos.x / m_autoTileMap.Tileset.TileWorldWidth);
                        int tile_y = (int)(-vPos.y / m_autoTileMap.Tileset.TileWorldHeight);

						// for optimization, is true when mouse is over a diffent tile during the first update
						bool isMouseTileChanged = (tile_x != m_prevMouseTileX) || (tile_y != m_prevMouseTileY);
						
						//if ( m_autoTileMap.IsValidAutoTilePos(tile_x, tile_y)) // commented to allow drawing outside map, useful when brush has a lot of copied tiles
						{
                            int gndTileType = m_autoTileMap.GetAutoTile(tile_x, tile_y, m_autoTileMap.BrushGizmo.SelectedLayer).Id;
                            int gndOverlayTileType = m_autoTileMap.GetAutoTile(tile_x, tile_y, m_autoTileMap.BrushGizmo.SelectedLayer + 1).Id;
							
							// mouse right for tile selection
							if( m_isMouseRightDown || m_isMouseRight && isMouseTileChanged )
							{
								if( m_isMouseRightDown )
								{
									m_startDragTileX = tile_x;
									m_startDragTileY = tile_y;
								
									// Remove Brush
									m_autoTileMap.BrushGizmo.Clear();
									m_tilesetSelStart = m_tilesetSelEnd = -1;

									// copy tile
									if( Event.current.shift )
									{
										m_selectedTileIdx = -2; //NOTE: -2 means, ignore this tile when painting
									}
									else
									{
										m_selectedTileIdx = gndTileType >= 0? gndTileType : gndOverlayTileType;
									}
								}
								m_dragTileX = tile_x;
								m_dragTileY = tile_y;
								

							}
							// isMouseLeft
							else if( m_isMouseLeftDown || isMouseTileChanged ) // avoid Push the same action twice during mouse drag
							{
								AutoTileBrush.TileAction action = new AutoTileBrush.TileAction();
								if( m_autoTileMap.BrushGizmo.BrushAction != null )
								{
									//+++ case of multiple tiles painting
									action.CopyRelative( m_autoTileMap, m_autoTileMap.BrushGizmo.BrushAction, tile_x, tile_y );
									if(Event.current.shift && (m_autoTileMap.BrushGizmo.SelectedLayer + 1) < m_autoTileMap.GetLayerCount())
									{
                                        // old functionality: ground tiles become ground overlay, ground overlay are removed, overlay tiles remains
                                        // Tiles in SelectedLayer are moved to next layer
                                        action.BecomeOverlay(m_autoTileMap.BrushGizmo.SelectedLayer);
									}
								}
								else 
								{
									//+++ case of single tile painting
                                    // If smart brush is enabled, the tiles with collision type Overlay will be placed directly in the first overlay layer found over current SelectedLayer
                                    int overlayLayer = m_autoTileMap.FindFirstLayerIdx(AutoTileMap.eLayerType.Overlay, m_autoTileMap.BrushGizmo.SelectedLayer);
                                    if (m_autoTileMap.BrushGizmo.SmartBrushEnabled && overlayLayer >= 0 && m_selectedTileIdx >= 0 && m_autoTileMap.Tileset.AutotileCollType[m_selectedTileIdx] == AutoTileMap.eTileCollisionType.OVERLAY)
                                    {
                                        action.Push(m_autoTileMap, tile_x, tile_y, m_selectedTileIdx, overlayLayer);
                                    }
                                    else
                                    {
                                        if (Event.current.shift || m_autoTileMap.IsAutoTileHasAlpha(m_selectedTileIdx) && m_autoTileMap.BrushGizmo.SmartBrushEnabled)
                                        {
                                            // Put tiles with alpha in the layer over Selected Layer
                                            action.Push(m_autoTileMap, tile_x, tile_y, m_selectedTileIdx, m_autoTileMap.BrushGizmo.SelectedLayer + 1);
                                        }
                                        else
                                        {
                                            action.Push(m_autoTileMap, tile_x, tile_y, m_selectedTileIdx, m_autoTileMap.BrushGizmo.SelectedLayer);
                                        }
                                    }
								}
								
								m_autoTileMap.BrushGizmo.PerformAction( action );
								EditorUtility.SetDirty( m_autoTileMap );
							}
						}
						
						m_prevMouseTileX = tile_x;
						m_prevMouseTileY = tile_y;
					}
					else
					{
						// Copy selected tiles
						if( m_dragTileX != -1 && m_dragTileY != -1 )
						{
							m_autoTileMap.BrushGizmo.BrushAction = new AutoTileBrush.TileAction();
							int startTileX = Mathf.Min( m_startDragTileX, m_dragTileX );
							int startTileY = Mathf.Min( m_startDragTileY, m_dragTileY );
							int endTileX = Mathf.Max( m_startDragTileX, m_dragTileX );
							int endTileY = Mathf.Max( m_startDragTileY, m_dragTileY );
							
							for( int tile_x = startTileX; tile_x <= endTileX; ++tile_x  )
							{
								for( int tile_y = startTileY; tile_y <= endTileY; ++tile_y  )
								{								
									// Tile position is relative to last released position ( m_dragTile )
									if( Event.current.shift )
									{
										// Copy overlay only
                                        for (int i = m_autoTileMap.BrushGizmo.SelectedLayer + 1; i < m_autoTileMap.GetLayerCount(); ++i)
                                        {
                                            int tileType = m_autoTileMap.GetAutoTile(tile_x, tile_y, i).Id;
                                            if (
                                                (m_autoTileMap.MapLayers[i].LayerType != AutoTileMap.eLayerType.Ground) ||
                                                (tileType >= 0) // this allow paste overlay tiles without removing ground or ground overlay
                                            )
                                            {
                                                m_autoTileMap.BrushGizmo.BrushAction.Push(m_autoTileMap, tile_x - m_dragTileX, tile_y - m_dragTileY, tileType, i);
                                            }
                                        } 
									}
									else
									{
                                        for (int i = 0; i < m_autoTileMap.GetLayerCount(); ++i)
                                        {
                                            int tileType = m_autoTileMap.GetAutoTile(tile_x, tile_y, i).Id;
                                            m_autoTileMap.BrushGizmo.BrushAction.Push(m_autoTileMap, tile_x - m_dragTileX, tile_y - m_dragTileY, tileType, i);
                                        }    
									}
								}
							}
							
							m_autoTileMap.BrushGizmo.RefreshBrushGizmo( startTileX, startTileY, endTileX, endTileY, m_dragTileX, m_dragTileY, Event.current.shift );
							
							m_dragTileX = m_dragTileY = -1;
						}
					}
				}

				// Draw selection rect
				if( m_isMouseRight )
				{
                    float rX = m_autoTileMap.transform.position.x + Mathf.Min(m_startDragTileX, m_dragTileX) * m_autoTileMap.Tileset.TileWorldWidth;
                    float rY = m_autoTileMap.transform.position.y + Mathf.Min(m_startDragTileY, m_dragTileY) * m_autoTileMap.Tileset.TileWorldHeight;
                    float rWidth = (Mathf.Abs(m_dragTileX - m_startDragTileX) + 1) * m_autoTileMap.Tileset.TileWorldWidth;
                    float rHeight = (Mathf.Abs(m_dragTileY - m_startDragTileY) + 1) * m_autoTileMap.Tileset.TileWorldHeight;
					Rect rSelection = new Rect( rX, -rY, rWidth, -rHeight );
					UtilsGuiDrawing.DrawRectWithOutline( rSelection, new Color(0f, 1f, 0f, 0.2f), new Color(0f, 1f, 0f, 1f));
				}
			}
		}

        private void _refreshSubTilesetNames()
        {
            m_subTilesetNames = new string[m_autoTileMap.Tileset.SubTilesets.Count];
            for (int i = 0; i < m_autoTileMap.Tileset.SubTilesets.Count; ++i)
            {
                m_subTilesetNames[i] = m_autoTileMap.Tileset.SubTilesets[i].Name;
            }
        }

		public void OnInspectorGUI()
		{
            // refresh data if needed
            if (m_subTilesetNames == null || m_subTilesetNames.Length != m_autoTileMap.Tileset.SubTilesets.Count)
            {
                _refreshSubTilesetNames();
                m_subTilesetIdx = Mathf.Clamp(m_subTilesetIdx, 0, m_autoTileMap.Tileset.SubTilesets.Count);
            }
			m_subTilesetIdx = EditorGUILayout.Popup( "Tileset: ", m_subTilesetIdx, m_subTilesetNames );
			
			if( GUI.changed || m_tilesetTexture == null )
			{
				m_tilesetTexture = UtilsAutoTileMap.GenerateTilesetTexture( m_autoTileMap.Tileset, m_autoTileMap.Tileset.SubTilesets[ m_subTilesetIdx ] );
			}
			

			if( m_tilesetTexture != null )
			{
                Rect rTileset = new Rect();
                Rect rTile = new Rect(0, 0, k_visualTileWidth, k_visualTileHeight);

                if (IsEditCollision)
                {
                    //NOTE: if you don't see the special characters properly, be sure this file is saved in UTF-8
                    EditorGUILayout.HelpBox("■ - Block Collision\n□ - Wall Collision\n# - Fence Collision\n★ - Overlay", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Press Shift Key to change collisions by pressing left/right mouse button over the tile", MessageType.Info);
                }

				// BeginScrollView
                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.MinHeight(16f * k_visualTileHeight));
				{                    
                    GUIStyle tilesetStyle = new GUIStyle(GUI.skin.box);
                    tilesetStyle.normal.background = m_tilesetTexture;
                    tilesetStyle.border = tilesetStyle.margin = tilesetStyle.padding = new RectOffset(0, 0, 0, 0);
                    float fWidth = m_autoTileMap.Tileset.AutoTilesPerRow * k_visualTileWidth;
                    float fHeight = m_tilesetTexture.height * fWidth / m_tilesetTexture.width;
                    GUILayout.Box("", tilesetStyle, GUILayout.Width(fWidth), GUILayout.Height(fHeight));
                    rTileset = GUILayoutUtility.GetLastRect();
					
					if( IsEditCollision )
					{
						for( int autoTileLocalIdx = 0; autoTileLocalIdx < 256; ++autoTileLocalIdx ) //autoTileLocalIdx: index of current tileset group
						{
                            rTile.x = rTileset.x + (autoTileLocalIdx % m_autoTileMap.Tileset.AutoTilesPerRow) * k_visualTileWidth;
                            rTile.y = rTileset.y + (autoTileLocalIdx / m_autoTileMap.Tileset.AutoTilesPerRow) * k_visualTileHeight;
							
							int autoTileIdx = autoTileLocalIdx + (int)m_subTilesetIdx * 256; // global autotile idx
							if (Event.current.type == EventType.MouseUp)
							{
								if( rTile.Contains( Event.current.mousePosition ) )
								{
									int collType = (int)m_autoTileMap.Tileset.AutotileCollType[ autoTileIdx ];
									if( Event.current.button == 0 )
									{
										collType += 1; // go next
									}
									else if( Event.current.button == 1 )
									{
										collType += (int)AutoTileMap.eTileCollisionType._SIZE - 1; // go back
									}
									collType%=(int)AutoTileMap.eTileCollisionType._SIZE;
									m_autoTileMap.Tileset.AutotileCollType[ autoTileIdx ] = (AutoTileMap.eTileCollisionType)(collType);
								}
								EditorUtility.SetDirty( m_autoTileMap.Tileset );
							}

							
							string sCollision = "";
							switch( m_autoTileMap.Tileset.AutotileCollType[autoTileIdx] )
							{
							//NOTE: if you don't see the special characters properly, be sure this file is saved in UTF-8
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
							
							//debug Alpha tiles
							/*/
							if( m_autoTileMap.Tileset.IsAutoTileHasAlpha[autoTileIdx] )
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
					else
					{
                        UpdateTilesetOnInspector(rTileset);

                        Rect rSelected = new Rect(0, 0, k_visualTileWidth, k_visualTileHeight);
						int tileWithSelectMark = m_selectedTileIdx;
						tileWithSelectMark -= (int)m_subTilesetIdx * 256;
                        rSelected.position = rTileset.position + new Vector2((tileWithSelectMark % m_autoTileMap.Tileset.AutoTilesPerRow) * k_visualTileWidth, (tileWithSelectMark / m_autoTileMap.Tileset.AutoTilesPerRow) * k_visualTileHeight);
						UtilsGuiDrawing.DrawRectWithOutline( rSelected, new Color( 0f, 0f, 0f, 0.1f ), new Color( 1f, 1f, 1f, 1f ) );
					}
				}
                EditorGUILayout.EndScrollView();
			}
		}

		//+++ used for rectangle selection in tileset
		private int m_tilesetSelStart;
		private int m_tilesetSelEnd;
		//---

		bool m_isMouseLeft;
		bool m_isMouseRight;
		//bool m_isMouseMiddle;
		bool m_isMouseLeftDown;
		bool m_isMouseRightDown;
		//bool m_isMouseMiddleDown;

		void UpdateMouseInputs()
		{
			m_isMouseLeftDown = false;
			m_isMouseRightDown = false;
			//m_isMouseMiddleDown = false;

			if( Event.current.isMouse )
			{
				m_isMouseLeftDown = ( Event.current.type == EventType.MouseDown && Event.current.button == 0);
				m_isMouseRightDown = ( Event.current.type == EventType.MouseDown && Event.current.button == 1);
				//m_isMouseMiddleDown = ( Event.current.type == EventType.MouseDown && Event.current.button == 1);
				m_isMouseLeft = m_isMouseLeftDown || ( Event.current.type == EventType.MouseDrag && Event.current.button == 0);
				m_isMouseRight = m_isMouseRightDown || ( Event.current.type == EventType.MouseDrag && Event.current.button == 1);
				//m_isMouseMiddle = m_isMouseMiddleDown || ( Event.current.type == EventType.MouseDrag && Event.current.button == 2);
			}
		}

		void UpdateTilesetOnInspector( Rect rTileset )
		{
			if( rTileset.Contains( Event.current.mousePosition ) )
			{
				UpdateMouseInputs();
				Vector2 mouseLocalPos = Event.current.mousePosition - new Vector2( rTileset.x, rTileset.y);
                int tx = (int)(mouseLocalPos.x / k_visualTileWidth);
                int ty = (int)(mouseLocalPos.y / k_visualTileHeight);
                int autotileIdx = ty * m_autoTileMap.Tileset.AutoTilesPerRow + tx + ((int)m_subTilesetIdx * 256);

				if( m_isMouseLeftDown )
				{
					// select pressed tile
					m_selectedTileIdx = autotileIdx;

					// Remove Brush
					m_autoTileMap.BrushGizmo.Clear();
					m_tilesetSelStart = m_tilesetSelEnd = -1;

				}
				else if( m_isMouseRightDown )
				{
					m_tilesetSelStart = m_tilesetSelEnd = autotileIdx;
				}
				else if( m_isMouseRight )
				{
					m_tilesetSelEnd = autotileIdx;
				}
				else if( m_tilesetSelStart >= 0 && m_tilesetSelEnd >= 0 )
				{
					m_autoTileMap.BrushGizmo.RefreshBrushGizmoFromTileset( m_tilesetSelStart, m_tilesetSelEnd);
					m_tilesetSelStart = m_tilesetSelEnd = -1;
				}

				// Draw selection rect
				if( m_tilesetSelStart >= 0 && m_tilesetSelEnd >= 0 )
				{
					int tilesetIdxStart = m_tilesetSelStart - ((int)m_subTilesetIdx * 256); // make it relative to selected tileset
					int tilesetIdxEnd = m_tilesetSelEnd - ((int)m_subTilesetIdx * 256); // make it relative to selected tileset
					Rect selRect = new Rect( );
                    int TileStartX = tilesetIdxStart % m_autoTileMap.Tileset.AutoTilesPerRow;
                    int TileStartY = tilesetIdxStart / m_autoTileMap.Tileset.AutoTilesPerRow;
                    int TileEndX = tilesetIdxEnd % m_autoTileMap.Tileset.AutoTilesPerRow;
                    int TileEndY = tilesetIdxEnd / m_autoTileMap.Tileset.AutoTilesPerRow;
                    selRect.width = (Mathf.Abs(TileEndX - TileStartX) + 1) * k_visualTileWidth;
                    selRect.height = (Mathf.Abs(TileEndY - TileStartY) + 1) * k_visualTileHeight;
                    float scrX = Mathf.Min(TileStartX, TileEndX) * k_visualTileWidth;
                    float scrY = Mathf.Min(TileStartY, TileEndY) * k_visualTileHeight;
					selRect.position = new Vector2( scrX, scrY );
					selRect.position += rTileset.position;
					//selRect.y = Screen.height - selRect.y;
					UtilsGuiDrawing.DrawRectWithOutline( selRect, new Color(0f, 1f, 0f, 0.2f), new Color(0f, 1f, 0f, 1f));
				}
			}
		}
	}	
}