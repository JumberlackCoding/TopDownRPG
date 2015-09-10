using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.RpgMapEditor
{

    /// <summary>
    /// Manages the creation of all map tile chunks
    /// </summary>
	public class TileChunkPool : MonoBehaviour 
	{
        /// <summary>
        /// The witdth size of the generated tilechunks in tiles ( due max vertex limitation, this should be less than 62 )
        /// Increasing this value, map will load faster but drawing tiles will be slower
        /// </summary>
		public const int k_TileChunkWidth = 32;

        /// <summary>
        /// The height size of the generated tilechunks in tiles ( due max vertex limitation, this should be less than 62 )
        /// Increasing this value, map will load faster but drawing tiles will be slower
        /// </summary>
		public const int k_TileChunkHeight = 32;

		[System.Serializable]
		public class TileChunkLayer
		{
			public GameObject ObjNode;
			public TileChunk[] TileChunks;
            public int SortingOrder 
            { 
                get{ return _sortingOrder; }
                set
                {
                    _sortingOrder = value;
                    for (int i = 0; i < TileChunks.Length; ++i )
                    {
                        if (TileChunks[i] != null) TileChunks[i].SortingOrder = value;
                    }
                } 
            }

            public string SortingLayer
            {
                get { return _sortingLayer; }
                set
                {
                    _sortingLayer = value;
                    for (int i = 0; i < TileChunks.Length; ++i)
                    {
                        if (TileChunks[i] != null) TileChunks[i].SortingLayer = value;
                    }
                }
            }

            private int _sortingOrder = 0;
            private string _sortingLayer = "Default";
        }

		public List<TileChunkLayer> TileChunkLayers = new List<TileChunkLayer>();

		private List<TileChunk> m_tileChunkToBeUpdated = new List<TileChunk>();
		[SerializeField]
		private AutoTileMap m_autoTileMap;

		public void Initialize (AutoTileMap autoTileMap)
		{
			hideFlags = HideFlags.NotEditable;
			m_autoTileMap = autoTileMap;
			foreach( TileChunkLayer tileChunkLayer in TileChunkLayers )
			{
				if( tileChunkLayer.ObjNode != null )
				{
				#if UNITY_EDITOR
					DestroyImmediate(tileChunkLayer.ObjNode);
				#else
					Destroy(tileChunkLayer.ObjNode);
				#endif
				}
			}
			TileChunkLayers.Clear();
			m_tileChunkToBeUpdated.Clear();
		}

        /// <summary>
        /// Mark a tile to be updated during update
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="layer"></param>
		public void MarkUpdatedTile( int tileX, int tileY, int layer )
		{
			TileChunk tileChunk = _GetTileChunk( tileX, tileY, layer );
			if( !m_tileChunkToBeUpdated.Contains(tileChunk) )
			{
				m_tileChunkToBeUpdated.Add( tileChunk );
			}
		}

        /// <summary>
        /// Mark all tilechunks of a layer to be updated
        /// </summary>
        /// <param name="layer"></param>
        public void MarkLayerChunksForUpdate( int layer )
        {
            TileChunkLayer chunkLayer = _GetTileChunkLayer(layer);
            m_tileChunkToBeUpdated.AddRange(chunkLayer.TileChunks);
        }

        /// <summary>
        /// Update marked chunks
        /// </summary>
		public void UpdateChunks()
		{
			while( m_tileChunkToBeUpdated.Count > 0 )
			{
				m_tileChunkToBeUpdated[0].RefreshTileData();
				m_tileChunkToBeUpdated.RemoveAt(0);
			}
		}

		public void UpdateLayersData ( )
		{            
			for( int i = 0; i < TileChunkLayers.Count; ++i )
			{
                AutoTileMap.MapLayer mapLayer = m_autoTileMap.MapLayers[i];
                TileChunkLayer tileChunkLayer = TileChunkLayers[i];
                tileChunkLayer.ObjNode.SetActive(mapLayer.Visible);
                tileChunkLayer.ObjNode.transform.position = m_autoTileMap.transform.position + new Vector3(0f, 0f, mapLayer.Depth);
                tileChunkLayer.SortingLayer = mapLayer.SortingLayer;
                tileChunkLayer.SortingOrder = mapLayer.SortingOrder;               
			}
		}

        // TODO: When a layer is created with no tiles ( all set to -1 ) layer is never created and this is causing problems
        // but I should find a better way of doing this. TileChunkPool should be redesigned
        public void CreateLayers( int totalLayers )
        {
            _CreateTileChunkLayer(totalLayers - 1);
        }

		private TileChunk _GetTileChunk( int tileX, int tileY, int layer )
		{
			TileChunkLayer chunkLayer = _GetTileChunkLayer( layer );

			int rowTotalChunks = 1 + ((m_autoTileMap.MapTileWidth - 1) / k_TileChunkWidth);
			int chunkIdx = (tileY / k_TileChunkHeight) * rowTotalChunks + (tileX / k_TileChunkWidth);
			TileChunk tileChunk = chunkLayer.TileChunks[chunkIdx];
			if( tileChunk == null )
			{
				int startTileX = tileX - tileX % k_TileChunkWidth;
				int startTileY = tileY - tileY % k_TileChunkHeight;
				GameObject chunkObj = new GameObject();
                chunkObj.name = m_autoTileMap.MapLayers[layer].Name +"_" + startTileX + "_" + startTileY;
				chunkObj.transform.parent = chunkLayer.ObjNode.transform;
                chunkObj.layer = chunkLayer.ObjNode.layer;
				chunkObj.hideFlags = HideFlags.NotEditable;
				tileChunk = chunkObj.AddComponent<TileChunk>();
				chunkLayer.TileChunks[chunkIdx] = tileChunk;
				tileChunk.Configure( m_autoTileMap, layer, startTileX, startTileY, k_TileChunkWidth, k_TileChunkHeight );
			}
			return tileChunk;
		}

		private TileChunkLayer _GetTileChunkLayer( int layer )
		{
			return TileChunkLayers.Count > layer? TileChunkLayers[layer] : _CreateTileChunkLayer( layer );
		}

		private TileChunkLayer _CreateTileChunkLayer( int layer )
		{
			int rowTotalChunks = 1 + ((m_autoTileMap.MapTileWidth - 1) / k_TileChunkWidth);
			int colTotalChunks = 1 + ((m_autoTileMap.MapTileHeight - 1) / k_TileChunkHeight);
			int totalChunks = rowTotalChunks * colTotalChunks;
			TileChunkLayer chunkLayer = null;
			while( TileChunkLayers.Count <= layer )
			{
				chunkLayer = new TileChunkLayer();
				chunkLayer.TileChunks = new TileChunk[ totalChunks ];
				chunkLayer.ObjNode = new GameObject();
				chunkLayer.ObjNode.transform.parent = transform;
                chunkLayer.ObjNode.transform.position = m_autoTileMap.transform.position + new Vector3(0f, 0f, m_autoTileMap.MapLayers[TileChunkLayers.Count].Depth);
                chunkLayer.SortingOrder = m_autoTileMap.MapLayers[TileChunkLayers.Count].SortingOrder;
                chunkLayer.SortingLayer = m_autoTileMap.MapLayers[TileChunkLayers.Count].SortingLayer;
                chunkLayer.ObjNode.name = m_autoTileMap.MapLayers[TileChunkLayers.Count].Name + "_" + TileChunkLayers.Count;
				TileChunkLayers.Add( chunkLayer );
			}
			return chunkLayer;
		}
	}
}