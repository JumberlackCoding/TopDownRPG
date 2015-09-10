using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// Map data containing all tiles and the size of the map
    /// </summary>
	[System.Serializable, XmlRoot("AutoTileMap")]    
	public class AutoTileMapSerializeData 
	{
        public const string k_version = "1.2.4";//TODO: change this after each update!

		[System.Serializable]
		public class MetadataChunk
		{
            public string version = k_version; 
			public bool compressedTileData = true;

            public bool IsVersionAboveOrEqual(string versionToCompare)
            {
                string[] curVersion = version.Split('.');
                string[] compareVersion = versionToCompare.Split('.');
                for( int i = 0; i < curVersion.Length && i < compareVersion.Length; ++i )
                {
                    if (System.Convert.ToInt32(curVersion[i]) < System.Convert.ToInt32(compareVersion[i]))
                        return false;
                }
                return compareVersion.Length >= curVersion.Length;
            }
		}

		[System.Serializable]
		public class TileLayer
		{
			public List<int> Tiles;
            public bool Visible = true;
            public string Name;
            public AutoTileMap.eLayerType LayerType;
            public string SortingLayer = "Default"; // sorting layer
            public int SortingOrder = 0; // sorting order
            public float Depth;
		}

		public MetadataChunk Metadata = new MetadataChunk(); 
		public int TileMapWidth;
		public int TileMapHeight;
        public List<TileLayer> TileData = new List<TileLayer>();

	 	public void CopyData (AutoTileMapSerializeData mapData)
		{
			Metadata = mapData.Metadata;
			TileMapWidth = mapData.TileMapWidth;
			TileMapHeight = mapData.TileMapHeight;
			TileData = mapData.TileData;
		}

        /// <summary>
        /// Save the map configuration
        /// </summary>
        /// <param name="_autoTileMap"></param>
        /// <returns></returns>
		public bool SaveData( AutoTileMap _autoTileMap, int width = -1, int height = -1 )
		{
            if (width < 0) width = TileMapWidth;
            if (height < 0) height = TileMapHeight;
            // avoid clear map data when auto tile map is not initialized
			if( !_autoTileMap.IsInitialized )
			{
				//Debug.LogError(" Error saving data. Autotilemap is not initialized! Map will not be saved. ");
				return false;
			}

            Metadata.version = k_version;
			
			TileData.Clear();
			for( int iLayer = 0; iLayer < _autoTileMap.GetLayerCount(); ++iLayer )
			{
                AutoTileMap.MapLayer mapLayer = _autoTileMap.MapLayers[iLayer];
                List<int> tileData = new List<int>(width * height);
				int iTileRepetition = 0;
				int savedTileId = 0;
                for (int tile_y = 0; tile_y < height; ++tile_y)
				{
                    for (int tile_x = 0; tile_x < width; ++tile_x)
					{
						int iType = _autoTileMap.GetAutoTile( tile_x, tile_y, iLayer ).Id;

                        //+++fix: FogOfWar tiles could be < -1, and this is not good for compress system, excepting ids >= -1
                        if (mapLayer.LayerType == AutoTileMap.eLayerType.FogOfWar)
                        {
                            iType = ((iType >> 1) & 0x7FFFFFFF); // remove the last bit of the last byte. Will be << 1 later when loading
                        }
                        //---

						if( iTileRepetition == 0 )
						{
							savedTileId = iType;
							iTileRepetition = 1;
						}
						else
						{
							// compression data. All tiles of the same type are store with number of repetitions ( negative number ) and type
							// ex: 5|5|5|5 --> |-4|5| (4 times 5) ex: -1|-1|-1 --> |-3|-1| ( 3 times -1 )
							if( iType == savedTileId ) ++iTileRepetition;
							else
							{
								if( iTileRepetition > 1 )
								{
									tileData.Add( -iTileRepetition ); // save number of repetition with negative sign
								}
                                if( savedTileId < -1 )
                                {
                                    Debug.LogError(" Wrong tile id found when compressing the tile layer " + mapLayer.Name);
                                    savedTileId = -1;
                                }
								tileData.Add( savedTileId );
								savedTileId = iType;
								iTileRepetition = 1;
							}
						}
					}
				}
				// save last tile type found
				if( iTileRepetition > 1 )
				{
					tileData.Add( -iTileRepetition );
				}
				tileData.Add( savedTileId );

				// 
                TileData.Add(new TileLayer() 
                { 
                    Tiles = tileData, 
                    Depth = mapLayer.Depth, 
                    LayerType = mapLayer.LayerType, 
                    SortingLayer = mapLayer.SortingLayer,
                    SortingOrder = mapLayer.SortingOrder,
                    Name = mapLayer.Name, 
                    Visible = mapLayer.Visible 
                });
			}
            TileMapWidth = width;
            TileMapHeight = height;
			return true;
		}

        /// <summary>
        /// Get this object serialized as an xml string
        /// </summary>
        /// <returns></returns>
		public string GetXmlString()
		{
			return UtilsSerialize.Serialize<AutoTileMapSerializeData>(this);
		}

        /// <summary>
        /// Save this object serialized in an xml file
        /// </summary>
        /// <param name="_filePath"></param>
		public void SaveToFile(string _filePath)
		{
			var serializer = new XmlSerializer(typeof(AutoTileMapSerializeData));
			var stream = new FileStream(_filePath, FileMode.Create);
			serializer.Serialize(stream, this);
			stream.Close();
		}

        /// <summary>
        /// Create map serialized data from xml file
        /// </summary>
        /// <param name="_filePath"></param>
        /// <returns></returns>
		public static AutoTileMapSerializeData LoadFromFile(string _filePath)
		{
			var serializer = new XmlSerializer(typeof(AutoTileMapSerializeData));
			var stream = new FileStream(_filePath, FileMode.Open);
			var obj = serializer.Deserialize(stream) as AutoTileMapSerializeData;
			stream.Close();
			return obj;
		}

        /// <summary>
        /// Create map serialized data from xml string
        /// </summary>
        /// <param name="_xml"></param>
        /// <returns></returns>
		public static AutoTileMapSerializeData LoadFromXmlString(string _xml)
		{
			return UtilsSerialize.Deserialize<AutoTileMapSerializeData>(_xml);
		}

        /// <summary>
        /// Load map serialized data into a map
        /// </summary>
        /// <param name="_autoTileMap"></param>
		public void LoadToMap( AutoTileMap _autoTileMap )
		{
			_autoTileMap.Initialize();
			_autoTileMap.ClearMap();
            
            if( !Metadata.IsVersionAboveOrEqual("1.2.4") )
            {
                _applyFixVersionBelow124(_autoTileMap);
            }

            int totalMapTiles = TileMapWidth * TileMapHeight;
			for( int iLayer = 0; iLayer < TileData.Count; ++iLayer )
			{
                TileLayer tileData = TileData[iLayer];
                _autoTileMap.MapLayers.Add(
                    new AutoTileMap.MapLayer() 
                    {
                        Name = tileData.Name,
                        Visible = tileData.Visible, 
                        LayerType = tileData.LayerType, 
                        SortingOrder = tileData.SortingOrder,
                        SortingLayer = tileData.SortingLayer,
                        Depth = tileData.Depth,
                        TileLayerIdx = iLayer,
                    });
                _autoTileMap.TileLayers.Add( new AutoTileMap.AutoTile[TileMapWidth * TileMapHeight] );
				int iTileRepetition = 1;
				int iTileIdx = 0;
                for (int i = 0; i < tileData.Tiles.Count; ++i )
                {
                    int iType = tileData.Tiles[i];
                    //see compression notes in CreateFromTilemap
                    if (iType < -1)
                    {
                        iTileRepetition = -iType;
                    }
                    else
                    {
                        //+++fix: FogOfWar tiles could be < -1, and this is not good for compress system, excepting ids >= -1
                        if (tileData.LayerType == AutoTileMap.eLayerType.FogOfWar)
                        {
                            iType = (iType << 1); //restore value so the lost bit was the less significant of last byte
                        }
                        //---
                        if (iTileRepetition > totalMapTiles)
                        {
                            Debug.LogError("Error uncompressing layer " + tileData.Name + ". The repetition of a tile was higher than map tiles " + iTileRepetition + " > " + totalMapTiles);
                            iTileRepetition = 0;
                        }
                        for (; iTileRepetition > 0; --iTileRepetition, ++iTileIdx)
                        {
                            int tile_x = iTileIdx % TileMapWidth;
                            int tile_y = iTileIdx / TileMapWidth;
                            if (iType >= 0 || tileData.LayerType == AutoTileMap.eLayerType.FogOfWar)
                            {
                                _autoTileMap.SetAutoTile(tile_x, tile_y, iType, iLayer, false);
                            }
                        }
                        iTileRepetition = 1;
                    }
                }
			}
            _autoTileMap.RefreshAllTiles();
            _autoTileMap.UpdateChunkLayersData();
			_autoTileMap.RefreshMinimapTexture();
		}

        private void _applyFixVersionBelow124(AutoTileMap _autoTileMap)
        {
            for (int iLayer = 0; iLayer < TileData.Count; ++iLayer)
            {
                TileLayer tileData = TileData[iLayer];
                tileData.Visible = true;
                switch (iLayer)
                {
                    case 0:
                        tileData.Name = "Ground";
                        tileData.LayerType = AutoTileMap.eLayerType.Ground;
                        tileData.Depth = 1f;
                        break;
                    case 1:
                        tileData.Name = "Ground Overlay";
                        tileData.LayerType = AutoTileMap.eLayerType.Ground;
                        tileData.Depth = 0.5f;
                        break;
                    case 2:
                        tileData.Name = "Overlay";
                        tileData.LayerType = AutoTileMap.eLayerType.Overlay;
                        tileData.Depth = -1f;
                        break;
                }
            }
        }
	}
}
