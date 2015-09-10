using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// This class manages the tile brush used to paint tiles in the editor or in game:
    /// - Drawing tiles
    /// - Copying tiles
    /// - Redo/Undo drawing actions
    /// </summary>
	public class AutoTileBrush : MonoBehaviour 
	{
        /// <summary>
        /// The AutoTileMap owner of this brush
        /// </summary>
		public AutoTileMap MyAutoTileMap;

        /// <summary>
        /// Tile action with copied tiles to be pasted over the map
        /// </summary>
		public TileAction BrushAction;

        /// <summary>
        /// Position of the brush over the auto tile map in tile coordinates
        /// </summary>
		public Vector2 BrushTilePos;
		public bool HasChangedTilePos;

        /// <summary>
        /// If true, minimap will be updated by each drawing action. This could be slow for big maps.
        /// </summary>
        public bool IsRefreshMinimapEnabled = false;

        /// <summary>
        /// Selected layer where the brush will draw the tiles and will take as reference for special actions when holding action key
        /// </summary>
        public int SelectedLayer = 0;

        /// <summary>
        /// When this is true, the brush will have some special functionalities to make the map edition easier
        /// </summary>
        public bool SmartBrushEnabled = true;

		#region Historic Ctrl-Z Ctrl-Y
		[System.Serializable]
		public class TileAction
		{
			public class TileData
			{
				public int Tile_x;
				public int Tile_y;
				public int Tile_type;
				public int Tile_layer;
				public int Tile_type_prev;
			}
			
			List<TileData> aTileData = new List<TileData>();
			
			public void Push( AutoTileMap _autoTileMap, int tile_x, int tile_y, int tileId, int tile_layer )
			{
				if( 
                    tileId <= -2 || tile_layer >= _autoTileMap.MapLayers.Count ||  !_autoTileMap.MapLayers[tile_layer].Visible ||
                    _autoTileMap.MapLayers[tile_layer].LayerType == AutoTileMap.eLayerType.FogOfWar ||
                    _autoTileMap.MapLayers[tile_layer].LayerType == AutoTileMap.eLayerType.Objects
                )
				{
					; // do nothing, skip tile
				}
				else
				{
                    TileData tileData = new TileData()
					{  
						Tile_x = tile_x,
						Tile_y = tile_y,
						Tile_type = tileId,
						Tile_layer = tile_layer,
					};
					aTileData.Add( tileData );
				}
			}
			
			public void DoAction( AutoTileMap _autoTileMap )
			{
				int tileMinX = _autoTileMap.MapTileWidth-1;
				int tileMinY = _autoTileMap.MapTileHeight-1;
				int tileMaxX = 0;
				int tileMaxY = 0;

				for( int i = 0; i < aTileData.Count; ++i )
				{
					TileData tileData = aTileData[i];
					// save prev tile type for undo action
					tileData.Tile_type_prev = _autoTileMap.GetAutoTile( tileData.Tile_x, tileData.Tile_y, tileData.Tile_layer ).Id;
					_autoTileMap.SetAutoTile( tileData.Tile_x, tileData.Tile_y, tileData.Tile_type, tileData.Tile_layer );

					tileMinX = Mathf.Min( tileMinX, tileData.Tile_x );
					tileMinY = Mathf.Min( tileMinY, tileData.Tile_y );
					tileMaxX = Mathf.Max( tileMaxX, tileData.Tile_x );
					tileMaxY = Mathf.Max( tileMaxY, tileData.Tile_y );
				}

                if (_autoTileMap.BrushGizmo.IsRefreshMinimapEnabled)
                {
                    _autoTileMap.RefreshMinimapTexture(tileMinX, tileMinY, (tileMaxX - tileMinX) + 1, (tileMaxY - tileMinY) + 1);
                }
				_autoTileMap.UpdateChunks();
			}
			
			public void UndoAction( AutoTileMap _autoTileMap )
			{
				int tileMinX = _autoTileMap.MapTileWidth-1;
				int tileMinY = _autoTileMap.MapTileHeight-1;
				int tileMaxX = 0;
				int tileMaxY = 0;
				
				for( int i = 0; i < aTileData.Count; ++i )
				{
					TileData tileData = aTileData[i];
					_autoTileMap.SetAutoTile( tileData.Tile_x, tileData.Tile_y, tileData.Tile_type_prev, tileData.Tile_layer );

					tileMinX = Mathf.Min( tileMinX, tileData.Tile_x );
					tileMinY = Mathf.Min( tileMinY, tileData.Tile_y );
					tileMaxX = Mathf.Max( tileMaxX, tileData.Tile_x );
					tileMaxY = Mathf.Max( tileMaxY, tileData.Tile_y );
				}
                if ( _autoTileMap.BrushGizmo.IsRefreshMinimapEnabled )
                {
                    _autoTileMap.RefreshMinimapTexture(tileMinX, tileMinY, (tileMaxX - tileMinX) + 1, (tileMaxY - tileMinY) + 1);
                }
				_autoTileMap.UpdateChunks();
			}
			
			public void CopyRelative( AutoTileMap _autoTileMap, TileAction _action, int tile_x, int tile_y )
			{
				foreach( TileData tileData in _action.aTileData )
				{
					Push( _autoTileMap, tileData.Tile_x + tile_x, tileData.Tile_y + tile_y, tileData.Tile_type, tileData.Tile_layer );
				}
			}
			
			/// <summary>
            /// Tile layer will be moved a layer forward ( top direction ) but only when layer has ground type and there is a ground layer over it.
            /// This is not checking max layer count, so be careful with layer value
			/// </summary>
			/// <param name="layer"></param> 
			public void BecomeOverlay( int layer )
			{                
				for( int idx = 0; idx < aTileData.Count; ++idx )
				{
					TileData tileData = aTileData[idx];					
                    if (tileData.Tile_layer == layer)
					{
                        tileData.Tile_layer = layer + 1;
					}
                    if (tileData.Tile_layer == (layer + 1) && tileData.Tile_type == -1)
					{
						aTileData.RemoveAt(idx);
						--idx;
					}
				}
			}
			
		}
		
		private int m_actionIdx = -1;
		private List<TileAction>  m_actionsHistoric = new List<TileAction>();
		
		public void PerformAction( TileAction _action )
		{
			if( m_actionIdx < (m_actionsHistoric.Count - 1) && m_actionsHistoric.Count > 0 )
			{
				m_actionsHistoric.RemoveRange( m_actionIdx+1, m_actionsHistoric.Count - m_actionIdx - 1 );
			}
			
			m_actionsHistoric.Add( _action ); ++m_actionIdx;
			_action.DoAction( MyAutoTileMap );
		}
		
		public void UndoAction()
		{
			// this could happen in editor mode. Should be fixed by serializing TileAction class, but just in case...
			if( m_actionIdx >= m_actionsHistoric.Count )
			{
				Debug.LogWarning(" AutoTileBrush.UndoAction: m_actionIdx >= m_actionsHistoric.Count will be set to m_actionsHistoric.Count - 1 ");
				m_actionIdx = m_actionsHistoric.Count - 1;
			}

			if( m_actionIdx >= 0 )
			{
				m_actionsHistoric[ m_actionIdx ].UndoAction( MyAutoTileMap );
				--m_actionIdx;
			}
		}
		
		public void RedoAction()
		{
			if( m_actionIdx < m_actionsHistoric.Count - 1 )
			{
				++m_actionIdx;
				m_actionsHistoric[ m_actionIdx ].DoAction( MyAutoTileMap );
			}
		}
		#endregion

        /// <summary>
        /// Update Brush by calling this method with mouse position
        /// </summary>
        /// <param name="mousePos"></param>
		public void UpdateBrushGizmo( Vector3 mousePos )
		{
			Vector3 vTemp = mousePos;
            vTemp.x -= mousePos.x % MyAutoTileMap.Tileset.TileWorldWidth;
            vTemp.y -= mousePos.y % MyAutoTileMap.Tileset.TileWorldHeight;
			vTemp.z += 1f;
			transform.position = vTemp;

            int tile_x = (int)(0.5 + transform.position.x / MyAutoTileMap.Tileset.TileWorldWidth);
            int tile_y = (int)(0.5 + -transform.position.y / MyAutoTileMap.Tileset.TileWorldHeight);

			Vector2 vPrevTilePos = BrushTilePos;
			BrushTilePos = new Vector2( tile_x, tile_y );
			HasChangedTilePos = (vPrevTilePos != BrushTilePos);
		}

        /// <summary>
        /// Clear Brush tile selection
        /// </summary>
		public void Clear()
		{            
			RefreshBrushGizmo( -1, -1, -1, -1, -1, -1, false );
			BrushAction = null;
		}

        /// <summary>
        /// Copy a section of the map and use it as drawing template
        /// </summary>
        /// <param name="tile_start_x"></param>
        /// <param name="tile_start_y"></param>
        /// <param name="tile_end_x"></param>
        /// <param name="tile_end_y"></param>
        /// <param name="_dragEndTileX"></param>
        /// <param name="_dragEndTileY"></param>
        /// <param name="isCtrlKeyHold"></param>
		public void RefreshBrushGizmo( int tile_start_x, int tile_start_y, int tile_end_x, int tile_end_y, int _dragEndTileX, int _dragEndTileY, bool isCtrlKeyHold )
		{
            Vector2 pivot = new Vector2(0f, 1f);
			SpriteRenderer[] aSprites = GetComponentsInChildren<SpriteRenderer>();
			
			int sprIdx = 0;
			for( int tile_x = tile_start_x; tile_x <= tile_end_x; ++tile_x)
			{
				for( int tile_y = tile_start_y; tile_y <= tile_end_y; ++tile_y)
				{
					for( int tile_layer = 0; tile_layer < MyAutoTileMap.GetLayerCount(); ++tile_layer )
					{                        
                        if(
                            (isCtrlKeyHold && tile_layer == SelectedLayer) || //copy all layers over the SelectedLayer
                            !MyAutoTileMap.MapLayers[tile_layer].Visible // skip invisible layers
                        )
						{
							continue;
						}
						
						AutoTileMap.AutoTile autoTile = MyAutoTileMap.GetAutoTile( tile_x, tile_y, tile_layer );
						if( autoTile != null && autoTile.TilePartsIdx != null && autoTile.Id >= 0 )
						{
							for( int partIdx = 0; partIdx < autoTile.TilePartsLength; ++partIdx, ++sprIdx )
							{
								SpriteRenderer spriteRender = sprIdx < aSprites.Length? aSprites[sprIdx] : null;
								if( spriteRender == null )
								{
									GameObject spriteObj = new GameObject();
									spriteObj.transform.parent = transform;
									spriteRender = spriteObj.AddComponent<SpriteRenderer>();
								}
								spriteRender.transform.gameObject.name = "BrushGizmoPart"+sprIdx;
                                spriteRender.sprite = Sprite.Create(MyAutoTileMap.Tileset.AtlasTexture, MyAutoTileMap.Tileset.AutoTileRects[autoTile.TilePartsIdx[partIdx]], pivot, AutoTileset.PixelToUnits);
								spriteRender.sortingOrder = 50; //TODO: +50 temporal? see for a const number later
								spriteRender.color = new Color32( 192, 192, 192, 192);

								// get last tile as relative position
								int tilePart_x = (tile_x - _dragEndTileX)*2 + partIdx%2;
								int tilePart_y = (tile_y - _dragEndTileY)*2 + partIdx/2;

                                float xFactor = MyAutoTileMap.Tileset.TilePartWidth / AutoTileset.PixelToUnits;
                                float yFactor = MyAutoTileMap.Tileset.TilePartHeight / AutoTileset.PixelToUnits;
								spriteRender.transform.localPosition = new Vector3( tilePart_x * xFactor, -tilePart_y * yFactor, spriteRender.transform.position.z );
							}
						}
					}
				}
			}
			// clean unused sprite objects
			while(sprIdx < aSprites.Length)
			{
				if( Application.isEditor )
				{
					DestroyImmediate( aSprites[sprIdx].transform.gameObject );
				}
				else
				{
					Destroy( aSprites[sprIdx].transform.gameObject );
				}
				++sprIdx;
			}
		}
		
        /// <summary>
        /// Copy a section of the tileset and use it as drawing template
        /// </summary>
        /// <param name="tilesetSelStart"></param>
        /// <param name="tilesetSelEnd"></param>
		public void RefreshBrushGizmoFromTileset( int tilesetSelStart, int tilesetSelEnd )
		{
			SpriteRenderer[] aSprites = GetComponentsInChildren<SpriteRenderer>();
			
			BrushAction = new TileAction();
            Vector2 pivot = new Vector2(0f,1f);

            int selTileW = (Mathf.Abs(tilesetSelStart - tilesetSelEnd) % MyAutoTileMap.Tileset.AutoTilesPerRow + 1);
            int selTileH = (Mathf.Abs(tilesetSelStart - tilesetSelEnd) / MyAutoTileMap.Tileset.AutoTilesPerRow + 1);
			int tileIdx = Mathf.Min( tilesetSelStart, tilesetSelEnd );
			int sprIdx = 0;
			Vector3 vSprPos = new Vector3( 0f, 0f, 0f );
            for (int j = 0; j < selTileH; ++j, tileIdx += (MyAutoTileMap.Tileset.AutoTilesPerRow - selTileW), vSprPos.y -= MyAutoTileMap.Tileset.TileWorldHeight)
			{
				vSprPos.x = 0;
                for (int i = 0; i < selTileW; ++i, ++tileIdx, ++sprIdx, vSprPos.x += MyAutoTileMap.Tileset.TileWorldWidth)
				{
					SpriteRenderer spriteRender = sprIdx < aSprites.Length? aSprites[sprIdx] : null;
					if( spriteRender == null )
					{
						GameObject spriteObj = new GameObject();
						spriteObj.transform.parent = transform;
						spriteRender = spriteObj.AddComponent<SpriteRenderer>();
					}
					spriteRender.transform.gameObject.name = "BrushGizmoPart"+sprIdx;
                    spriteRender.sprite = Sprite.Create(MyAutoTileMap.Tileset.AtlasTexture, MyAutoTileMap.Tileset.ThumbnailRects[tileIdx], pivot, AutoTileset.PixelToUnits);
					spriteRender.sortingOrder = 50; //TODO: +50 temporal? see for a const number later
					spriteRender.color = new Color32( 255, 255, 255, 160);
					
					spriteRender.transform.localPosition = vSprPos;

                    // If smart brush is enabled, the tiles with collision type Overlay will be placed directly in the first overlay layer found over current SelectedLayer
                    if (SmartBrushEnabled)
                    {
                        int overlayLayer = MyAutoTileMap.FindFirstLayerIdx(AutoTileMap.eLayerType.Overlay, MyAutoTileMap.BrushGizmo.SelectedLayer);
                        if (overlayLayer >= 0 && tileIdx >= 0 && MyAutoTileMap.Tileset.AutotileCollType[tileIdx] == AutoTileMap.eTileCollisionType.OVERLAY)
                        {
                            BrushAction.Push(MyAutoTileMap, i, j, tileIdx, overlayLayer);
                        }
                        else
                        {
                            // If SmartBrushEnabled and tile has alpha, it will be draw in the layer over the SelectedLayer
                            BrushAction.Push(MyAutoTileMap, i, j, tileIdx, SmartBrushEnabled && MyAutoTileMap.IsAutoTileHasAlpha(tileIdx) ? SelectedLayer + 1 : SelectedLayer);
                        }
                    }
                    else
                    {
                        BrushAction.Push(MyAutoTileMap, i, j, tileIdx, SelectedLayer);
                    }
				}
			}
			
			// clean unused sprite objects
			while(sprIdx < aSprites.Length)
			{
				if( Application.isEditor )
				{
					DestroyImmediate( aSprites[sprIdx].transform.gameObject );
				}
				else
				{
					Destroy( aSprites[sprIdx].transform.gameObject );
				}
				++sprIdx;
			}
		}
	}
}
