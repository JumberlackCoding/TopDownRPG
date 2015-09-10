#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// Define a subtileset inside the AutoTileset. Each subtileset is named by a letter.
    /// </summary>
    [System.Serializable]
    public class SubTilesetConf
    {
        /// <summary>
        /// Rectangle in atlas texture of this subtileset
        /// </summary>
        public Rect AtlasRec;
        /// <summary>
        /// If this is an special tileset with animated tiles, wall, building, etc
        /// </summary>
        public bool HasAutotiles;
        /// <summary>
        /// A letter to name the subtileset
        /// </summary>
        public string Name; 
        /// <summary>
        /// A reference to the textures used to build the subtileset. Only autotileset have 5 textures, normal tilesets have 1.
        /// </summary>
        public Texture2D[] SourceTexture = new Texture2D[5];
        /// <summary>
        /// 
        /// </summary>
        public List<int> TilePartOffset; // offset of AutoTileRects. In case of autotiles it will be an offset per each type of tile: animated, building, etc. For object tileset, it will be one offset
    }

    /// <summary>
    /// Manage an slot in the Autotileset containing one autotileset or 4 normal tilesets.
    /// </summary>
    [System.Serializable]
    public class AtlasSlot
    {
        public List<SubTilesetConf> SubTilesets = new List<SubTilesetConf>();
    }

    /// <summary>
    /// Manage the autotileset containing all sub-tilesets named with a letter and used to draw map tiles.
    /// </summary>
	public class AutoTileset : ScriptableObject 
	{
        /// <summary>
        /// This is limited by Unity, don't set over this value unless limitation is removed in future versions
        /// </summary>
        public const int k_MaxTextureSize = 4096;
        /// <summary>
        /// The number of tiles per subtileset
        /// </summary>
        public const int k_TilesPerSubTileset = 256;
		public const float PixelToUnits = 100f;

        /// <summary>
        /// Size of the tile in pixels
        /// </summary>
		public int TileWidth = 32;
        /// <summary>
        /// Size of the tile in pixels
        /// </summary>
		public int TileHeight = 32;
        
        //TODO: precalculate this when TileSize is modified

        /// <summary>
        /// The size of an slot inside the atlas texture
        /// </summary>
        public int TilesetSlotSize { get{ return 32 * TileWidth; } }

        /// <summary>
        /// Half of the tile size in pixels
        /// </summary>
        public int TilePartWidth { get { return TileWidth >> 1; } }
        /// <summary>
        /// Half of the tile size in pixels
        /// </summary>
        public int TilePartHeight { get { return TileHeight >> 1; } }
        /// <summary>
        /// The size of a tile in world units
        /// </summary>
        public float TileWorldWidth { get { return TileWidth / PixelToUnits; } }
        /// <summary>
        /// The size of a tile in world units
        /// </summary>
        public float TileWorldHeight { get { return TileHeight / PixelToUnits; } }

        /// <summary>
        /// Tiles per row when drawing the subtilesets in the editor
        /// </summary>
		public int AutoTilesPerRow = 8;

        /// <summary>
        /// A list with all subtilesets. This list is updated when calling BuildSubTilesetsList
        /// </summary>
        public List<SubTilesetConf> SubTilesets = new List<SubTilesetConf>();

        /// <summary>
        /// A list with all slots. A slot contain 1 subtileset with autotiles or 4 subtileset with normal tiles
        /// </summary>
        public List<AtlasSlot> AtlasSlots;

        /// <summary>
        /// An array with the collision type for all tiles in the tileset
        /// </summary>
        public AutoTileMap.eTileCollisionType[] AutotileCollType;

        /// <summary>
        /// Check if a tile is animated
        /// </summary>
        /// <param name="autoTileIdx"></param>
        /// <returns></returns>
		public bool IsAutoTileAnimated( int autoTileIdx )
		{
            int subTilesetIdx = autoTileIdx / k_TilesPerSubTileset;
            if (SubTilesets[subTilesetIdx].HasAutotiles)
            {
                int tileIdx = autoTileIdx % k_TilesPerSubTileset;
                return (tileIdx >= 0 && tileIdx < 16 && (tileIdx % 2) == 0);
            }
            return false;
		}

        /// <summary>
        /// Check is a tile is an animated waterfall
        /// </summary>
        /// <param name="autoTileIdx"></param>
        /// <returns></returns>
        public bool IsAutoTileAnimatedWaterfall(int autoTileIdx)
        {
            int subTilesetIdx = autoTileIdx / k_TilesPerSubTileset;
            if (SubTilesets[subTilesetIdx].HasAutotiles)
            {
                int tileIdx = autoTileIdx % k_TilesPerSubTileset;
                return (tileIdx >= 0 && tileIdx < 16 && (tileIdx % 2) != 0 && tileIdx != 1 && tileIdx != 5);
            }
            return false;            
        }

        /// <summary>
        /// An array with alpha information of all tiles in the tileset
        /// </summary>
		public bool[] IsAutoTileHasAlpha;

        /// <summary>
        /// An array with rect source of the atlas with the thumbnail of each tile
        /// </summary>
		public List<Rect> ThumbnailRects;
        /// <summary>
        /// An array with rect source of the atlas for each tile
        /// </summary>
		public List<Rect> AutoTileRects;
        /// <summary>
        /// Map tileIdx with tilesetTileIdx. Used for animated tiles using some tiles as frames. Only one frame is taken into account.
        /// </summary>
		public int[] AutotileIdxMap;

        /// <summary>
        /// Material used to draw the tiles
        /// </summary>
		public Material AtlasMaterial{ get; private set; }

		[SerializeField]
		private Texture2D m_atlasTexture;
		public Texture2D AtlasTexture
		{  
			get{return m_atlasTexture;}
			set
			{
				if( value != null && value != m_atlasTexture )
				{
                    if (value.width % TilesetSlotSize == 0 && value.height % TilesetSlotSize == 0)
					{
						m_atlasTexture = value;
						UtilsAutoTileMap.ImportTexture( m_atlasTexture );
						GenerateAutoTileData();

						if( AtlasMaterial == null )
						{
							CreateAtlasMaterial();
						}
					}
					else
					{
						m_atlasTexture = null;
                        Debug.LogError(" TilesetsAtlasTexture.set: atlas texture has a wrong size " + value.width + "x" + value.height);
					}
				}
				else
				{
					m_atlasTexture = value;
				}
			}
		}

        /// <summary>
        /// Calculate the slot rect inside the atlas using index as reference
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public Rect CalculateAtlasSlotRectByIdx( int idx )
        {
            int w = AtlasTexture.width / TilesetSlotSize;
            //int h = AtlasTexture.height / k_TilesetSlotSize;
            int x = (idx % w) * TilesetSlotSize;
            int y = (idx / w) * TilesetSlotSize;
            return new Rect(x, y, TilesetSlotSize, TilesetSlotSize);
        }

        /// <summary>
        /// Build Altas Slot Data and update data according to atlas size
        /// </summary>
        public void BuildAtlasSlotData()
        {
            int w = AtlasTexture.width / TilesetSlotSize;
            int h = AtlasTexture.height / TilesetSlotSize;
            int size = (int)(w * h);
            if (size > 0)
            {
                if (AtlasSlots == null || AtlasSlots.Count == 0)
                {
                    AtlasSlots = new List<AtlasSlot>(size);
                    if( w == 2 && h == 2 && TileWidth == 32 && TileHeight == 32 ) //TODO: compatibility with older versions of asset 1.1.1 and below. To be removed after future versions
                    {
                        AtlasSlots.Add( new AtlasSlot() );
                        AtlasSlots[0].SubTilesets.Add(new SubTilesetConf() { AtlasRec = new Rect(0, 0, TilesetSlotSize, TilesetSlotSize), HasAutotiles = true, Name = "A" });
                        AtlasSlots.Add( new AtlasSlot() );
                        int halfSize = TilesetSlotSize / 2;
                        AtlasSlots[1].SubTilesets = new List<SubTilesetConf>(){  
                            new SubTilesetConf(){ AtlasRec = new Rect(TilesetSlotSize, 0, halfSize, halfSize), HasAutotiles = false, Name = "B"  },
                            new SubTilesetConf(){ AtlasRec = new Rect(TilesetSlotSize, halfSize, halfSize, halfSize), HasAutotiles = false, Name = "C"  },
                            new SubTilesetConf(){ AtlasRec = new Rect(TilesetSlotSize+halfSize, 0, halfSize, halfSize), HasAutotiles = false, Name = "D"  },
                            new SubTilesetConf(){ AtlasRec = new Rect(TilesetSlotSize+halfSize, halfSize, halfSize, halfSize), HasAutotiles = false, Name = "E"  },
                        };
                    }
                }

                if( AtlasSlots.Count > size )
                {
                    AtlasSlots.RemoveRange(size, AtlasSlots.Count - size);
                }
                else
                {
                    while( AtlasSlots.Count < size )
                    {
                        AtlasSlots.Add( new AtlasSlot() );
                    }
                }
                
            }
            else
            {
                AtlasSlots = null;
            }
        }

        /// <summary>
        /// Create the list of subtilesets using all subtilesets found int the slots
        /// </summary>
        public void BuildSubTilesetsList()
        {
            SubTilesets = new List<SubTilesetConf>();
            foreach( AtlasSlot slot in AtlasSlots )
            {
                foreach( SubTilesetConf conf in slot.SubTilesets )
                {
                    SubTilesets.Add( conf );
                }
            }
        }

        /// <summary>
        /// Create and return a list with all subtileset available names ( letters not used by other subtileset )
        /// </summary>
        /// <returns></returns>
        public List<string> CreateAvailableNameList()
        {
            List<string> retList = new List<string>();
            for (char c = 'A'; c <= 'Z'; ++c ) retList.Add( ""+c );
            BuildSubTilesetsList();
            foreach( SubTilesetConf conf in SubTilesets )
            {
                retList.Remove( conf.Name );
            }
            return retList;
        }

        /// <summary>
        /// Generate all needed data for the tileset
        /// </summary>
        public void GenerateAutoTileData( )
		{

			// force to generate atlas material if it is not instantiated
			if( AtlasMaterial == null )
			{
				//Debug.LogError( "GenerateAutoTileData error: missing AtlasMaterial" );
				//return;
				CreateAtlasMaterial();
			}

            BuildAtlasSlotData();
            BuildSubTilesetsList();

			_GenerateTilesetSprites();

            int tileNb = SubTilesets.Count * k_TilesPerSubTileset;

            if (AutotileCollType == null || tileNb != AutotileCollType.Length)
            {
                AutotileCollType = new AutoTileMap.eTileCollisionType[tileNb];
            }

			// get the mapped tileIdx ( for animated tile supporting. Animated tiles are considered as one, skipping the other 2 frames )
            // used only by sub tileset with autotiles
            AutotileIdxMap = new int[k_TilesPerSubTileset];
			int tileIdx = 0;
            for (int i = 0; i < k_TilesPerSubTileset; ++i)
			{
				AutotileIdxMap[i] = tileIdx;
				tileIdx += (i >= 0 && i < 16 && (i % 2) == 0)? 3 : 1;
			}

            IsAutoTileHasAlpha = new bool[tileNb];
            ThumbnailRects = new List<Rect>(tileNb);
            foreach( SubTilesetConf tilesetConf in SubTilesets )
            {               
                UtilsAutoTileMap.FillWithTilesetThumbnailSprites(ThumbnailRects, this, tilesetConf);
            }

			//+++ sometimes png texture loose isReadable value. Maybe a unity bug?
	#if UNITY_EDITOR
			string assetPath = AssetDatabase.GetAssetPath(AtlasTexture);
			TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter; 
			if( textureImporter != null && textureImporter.isReadable == false )
			{	// reimport texture
                Debug.LogWarning("TilesetsAtlasTexture " + assetPath + " isReadable is false. Will be re-imported to access pixels.");
				UtilsAutoTileMap.ImportTexture( AtlasTexture );
			}
	#endif
			//---
			Color32[] aAtlasColors = AtlasTexture.GetPixels32(); //NOTE: Color32 is faster than Color in this alpha check

			for( int i = 0; i < ThumbnailRects.Count; ++i )
			{
                int subTilesetIdx = i / k_TilesPerSubTileset;
                if (SubTilesets[subTilesetIdx].HasAutotiles && (i % k_TilesPerSubTileset) >= 48 && (i % k_TilesPerSubTileset) < 128)
				{
					// wall and building tiles has no alpha by default
					IsAutoTileHasAlpha[i] = false;
				}
				else
				{
					Rect sprTileRect = ThumbnailRects[i];
                    int pBaseIdx = (int)(sprTileRect.y * AtlasTexture.width + sprTileRect.x);
                    for (float py = 0; py < TileHeight && !IsAutoTileHasAlpha[i]; py++)
					{
                        for (float px = 0; px < TileWidth && !IsAutoTileHasAlpha[i]; px++)
						{
                            int pIdx = pBaseIdx + (int)(py * AtlasTexture.width + px);
							if( aAtlasColors[pIdx].a < 255 )
							{
								IsAutoTileHasAlpha[ i ] = true;
							}
						}
					}
				}
			}
	#if UNITY_EDITOR
			EditorUtility.SetDirty( this );
	#endif

		}

        /// <summary>
        /// Create a default material for the atlas
        /// </summary>
		private void CreateAtlasMaterial()
		{
			string matPath = "";
#if UNITY_EDITOR
			matPath = System.IO.Path.GetDirectoryName( AssetDatabase.GetAssetPath( m_atlasTexture ) );
			if( !string.IsNullOrEmpty( matPath ) )
			{
				matPath += "/"+AtlasTexture.name+" atlas material.mat";
				Material matAtlas = (Material)AssetDatabase.LoadAssetAtPath( matPath, typeof(Material));
				if( matAtlas == null )
				{
					matAtlas = new Material( Shader.Find("Unlit/Transparent") ); //NOTE: if this material changes, remember to change also the one inside #else #endif below
					AssetDatabase.CreateAsset(matAtlas, matPath );
				}
				AtlasMaterial = matAtlas;
				EditorUtility.SetDirty( AtlasMaterial );
				AssetDatabase.SaveAssets();
            }
#else
			AtlasMaterial = new Material( Shader.Find("Unlit/Transparent") );
#endif

            if ( AtlasMaterial != null )
			{
				AtlasMaterial.mainTexture = AtlasTexture;
			}
			else
			{
				m_atlasTexture = null;
				Debug.LogError( " TilesetsAtlasTexture.set: there was an error creating the material asset at "+matPath );
			}
		}


		private void __GenerateTileparts( int srcX, int srcY, int width, int height, int tileWidth, int tileHeight )
		{
			int iTilesetSprIdx = 0;
			Rect rec = new Rect( 0, 0, tileWidth, tileHeight );
			for( int y = height - tileHeight; y >= 0; y -= tileHeight )
			{
				for( int x = 0; x < width; x+=tileWidth, ++iTilesetSprIdx )
				{
					rec.x = srcX + x;
					rec.y = srcY + y;
					
					AutoTileRects.Add( rec );
				}
			}
		}
		
        /// <summary>
        /// Populate the list AutoTileRects with the rectangle source of each tile in the atlas
        /// </summary>
		private void _GenerateTilesetSprites()
		{
			AutoTileRects = new List<Rect>(4160);
			AutoTileRects.Clear();

            //+++ old values for 32x32 tiles, now depend on tile size
            int _768 = 24 * TileWidth;
            int _512 = 16 * TileWidth;
            int _480 = 15 * TileWidth;
            int _384 = 12 * TileWidth;
            int _256 = 8 * TileWidth;
            //---

            foreach(SubTilesetConf tilesetConf in SubTilesets)
            {
                tilesetConf.TilePartOffset = new List<int>();
                if (tilesetConf.HasAutotiles)
                {
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x, (int)tilesetConf.AtlasRec.y, _512, _384, TilePartWidth, TilePartHeight);		//Animated
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x, (int)tilesetConf.AtlasRec.y + _384, _512, _384, TilePartWidth, TilePartHeight);	//Ground
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x, (int)tilesetConf.AtlasRec.y + _768, _512, _256, TilePartWidth, TilePartHeight);	//Building
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x + _512, (int)tilesetConf.AtlasRec.y, _512, _480, TilePartWidth, TilePartHeight);	//Walls
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x + _512, (int)tilesetConf.AtlasRec.y + _512, _256, _512, TileWidth, TileHeight);		//Normal
                }
                else
                {
                    tilesetConf.TilePartOffset.Add(AutoTileRects.Count);
                    __GenerateTileparts((int)tilesetConf.AtlasRec.x, (int)tilesetConf.AtlasRec.y, _512, _512, TileWidth, TileHeight);		// Objects                     
                }
            }		
		}
		
	}
}