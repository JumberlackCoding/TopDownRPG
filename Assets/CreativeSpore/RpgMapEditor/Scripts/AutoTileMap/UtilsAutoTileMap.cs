using UnityEngine;
using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CreativeSpore.RpgMapEditor
{
	public class UtilsAutoTileMap 
	{
        /// <summary>
        /// Generate a tileset texture for a given tilesetConf
        /// </summary>
        /// <param name="autoTileset"></param>
        /// <param name="tilesetConf"></param>
        /// <returns></returns>
		public static Texture2D GenerateTilesetTexture( AutoTileset autoTileset, SubTilesetConf tilesetConf )
		{
            //+++ old values for 32x32 tiles, now depend on tile size
            int _1024 = 32 * autoTileset.TileWidth;
            int _256 = 8 * autoTileset.TileWidth;
            //---
			List<Rect> sprList = new List<Rect>();
            FillWithTilesetThumbnailSprites(sprList, autoTileset, tilesetConf);
			Texture2D tilesetTexture = new Texture2D( _256, _1024, TextureFormat.ARGB32, false );
            tilesetTexture.filterMode = FilterMode.Point;

			int sprIdx = 0;
            Rect dstRect = new Rect(0, tilesetTexture.height - autoTileset.TileHeight, autoTileset.TileWidth, autoTileset.TileHeight);
            for (; dstRect.y >= 0; dstRect.y -= autoTileset.TileHeight)
			{
                for (dstRect.x = 0; dstRect.x < tilesetTexture.width && sprIdx < sprList.Count; dstRect.x += autoTileset.TileWidth, ++sprIdx)
				{
					Rect srcRect = sprList[sprIdx];
                    Color[] autotileColors = autoTileset.AtlasTexture.GetPixels(Mathf.RoundToInt(srcRect.x), Mathf.RoundToInt(srcRect.y), autoTileset.TileWidth, autoTileset.TileHeight);
                    tilesetTexture.SetPixels(Mathf.RoundToInt(dstRect.x), Mathf.RoundToInt(dstRect.y), autoTileset.TileWidth, autoTileset.TileHeight, autotileColors);
				}
			}
			tilesetTexture.Apply();

			return tilesetTexture;
		}

        /// <summary>
        /// Fill a list of rects with all rect sources for the thumbnails of the tiles. For normal tiles is the same tile, but autotiles are special.
        /// </summary>
        /// <param name="_outList"></param>
        /// <param name="autoTileset"></param>
        /// <param name="tilesetConf"></param>
		public static void FillWithTilesetThumbnailSprites( List<Rect> _outList, AutoTileset autoTileset, SubTilesetConf tilesetConf)
		{
            //+++ old values for 32x32 tiles, now depend on tile size
            int _1024 = 32 * autoTileset.TileWidth;
            int _768 = 24 * autoTileset.TileWidth;
            int _640 = 20 * autoTileset.TileWidth;
            int _512 = 16 * autoTileset.TileWidth;
            int _384 = 12 * autoTileset.TileWidth;
            int _256 = 8 * autoTileset.TileWidth;
            //---

            Rect sprRect = new Rect(0, 0, autoTileset.TileWidth, autoTileset.TileHeight);
            int AtlasPosX = (int)tilesetConf.AtlasRec.x;
            int AtlasPosY = (int)tilesetConf.AtlasRec.y;
            if (tilesetConf.HasAutotiles)
			{
				// animated
                for (sprRect.y = _384 - autoTileset.TileHeight; sprRect.y >= 0; sprRect.y -= 3 * autoTileset.TileHeight)
				{
					int tx;
                    for (tx = 0, sprRect.x = 0; sprRect.x < _512; sprRect.x += 2 * autoTileset.TileWidth, ++tx)
					{
						if( tx % 4 == 0 || tx % 4 == 3 )
						{
                            Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                            _outList.Add(r);
						}
					}
				}

				// ground
                for (sprRect.y = _768 - autoTileset.TileHeight; sprRect.y >= _384; sprRect.y -= 3 * autoTileset.TileHeight)
				{
                    for (sprRect.x = 0; sprRect.x < _512; sprRect.x += 2 * autoTileset.TileWidth)
					{
                        Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                        _outList.Add(r);
					}
				}

				// building
                for (sprRect.y = _512 + 3 * autoTileset.TileHeight; sprRect.y >= _512; sprRect.y -= autoTileset.TileHeight)
				{
                    for (sprRect.x = _768; sprRect.x < _768 + 8 * autoTileset.TileWidth; sprRect.x += autoTileset.TileWidth)
					{
                        Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                        _outList.Add(r);
					}
				}

				// walls
                sprRect.y = (15 - 1) * autoTileset.TileHeight;
                for (sprRect.x = _512; sprRect.x < _1024; sprRect.x += 2 * autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
                sprRect.y = _640 + 2 * autoTileset.TileHeight;
                for (sprRect.x = _768; sprRect.x < _768 + 8 * autoTileset.TileWidth; sprRect.x += autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
                sprRect.y = (10 - 1) * autoTileset.TileHeight;
                for (sprRect.x = _512; sprRect.x < _1024; sprRect.x += 2 * autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
                sprRect.y = _640 + autoTileset.TileHeight;
                for (sprRect.x = _768; sprRect.x < _768 + 8 * autoTileset.TileWidth; sprRect.x += autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
                sprRect.y = (5 - 1) * autoTileset.TileHeight;
                for (sprRect.x = _512; sprRect.x < _1024; sprRect.x += 2 * autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
				sprRect.y = _640;
                for (sprRect.x = _768; sprRect.x < _768 + 8 * autoTileset.TileWidth; sprRect.x += autoTileset.TileWidth)
				{
                    Rect r = sprRect; r.position += tilesetConf.AtlasRec.position;
                    _outList.Add(r);
				}
				//--- walls

				//Normal
                _FillSpritesFromRect(_outList, autoTileset, _512 + AtlasPosX, _512 + AtlasPosY, _256, _512);
			}
            else
            { // split the tileset to create a column of 8 tiles per row
                _FillSpritesFromRect(_outList, autoTileset, AtlasPosX, AtlasPosY, _256, _512);
                _FillSpritesFromRect(_outList, autoTileset, AtlasPosX+_256, AtlasPosY, _256, _512);
            }
		}

		private static void _FillSpritesFromRect( List<Rect> _outList, AutoTileset autoTileset, int x, int y, int width, int height )
		{

            Rect srcRect = new Rect(0, 0, autoTileset.TileWidth, autoTileset.TileHeight);
            for (srcRect.y = height - autoTileset.TileHeight; srcRect.y >= 0; srcRect.y -= autoTileset.TileHeight)
			{
                for (srcRect.x = 0; srcRect.x < width; srcRect.x += autoTileset.TileWidth)
				{
					Rect sprRect = srcRect;
					sprRect.x += x;
					sprRect.y += y;
                    _outList.Add(sprRect);
				}
			}
		}

        /// <summary>
        /// Generate a tileset atlas
        /// </summary>
        /// <param name="autoTileset"></param>
        /// <param name="hSlots"></param>
        /// <param name="vSlots"></param>
        /// <returns></returns>
		public static Texture2D GenerateAtlas( AutoTileset autoTileset, int hSlots, int vSlots )
		{
            int w = hSlots * autoTileset.TilesetSlotSize;
            int h = vSlots * autoTileset.TilesetSlotSize;
			Texture2D atlasTexture = new Texture2D(w, h);
			Color32[] atlasColors = Enumerable.Repeat<Color32>( new Color32(0, 0, 0, 0) , w*h).ToArray();
			atlasTexture.SetPixels32(atlasColors);
			atlasTexture.Apply();

			return atlasTexture;
		}

        /// <summary>
        /// Copy a subtileset source textures in the atlas
        /// </summary>
        /// <param name="autoTileset"></param>
        /// <param name="tilesetConf"></param>
        public static void CopySubTilesetInAtlas( AutoTileset autoTileset, SubTilesetConf tilesetConf )
        {
            //+++ old values for 32x32 tiles, now depend on tile size
            int _768 = 24 * autoTileset.TileWidth;
            int _640 = 20 * autoTileset.TileWidth;
            int _512 = 16 * autoTileset.TileWidth;
            int _480 = 15 * autoTileset.TileWidth;
            int _384 = 12 * autoTileset.TileWidth;
            int _256 = 8 * autoTileset.TileWidth;
            //---

            for (int i = 0; i < tilesetConf.SourceTexture.Length; ++i )
            {
                ImportTexture(tilesetConf.SourceTexture[i]);
            }

            if (tilesetConf.HasAutotiles)
            {
                int xf = (int)tilesetConf.AtlasRec.x;
                int yf = (int)tilesetConf.AtlasRec.y;
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[0], xf, yf, _512, _384); // animated
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[1], xf, yf+_384, _512, _384); // ground
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[2], xf, yf + _768, _512, _256); // building
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[3], xf+_512, yf, _512, _480); // wall
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[4], xf+_512, yf + _512, _256, _512); // normal

                _CopyBuildingThumbnails(autoTileset, tilesetConf.SourceTexture[2], xf + _768, yf + _512);
                _CopyWallThumbnails(autoTileset, tilesetConf.SourceTexture[3], xf+_768, yf+_640);
            }
            else
            {
                _CopyTilesetInAtlas(autoTileset.AtlasTexture, tilesetConf.SourceTexture[0], tilesetConf.AtlasRec); // object
            }            
        }

        /// <summary>
        /// Clear an area of the atlas texture
        /// </summary>
        /// <param name="atlasTexture"></param>
        /// <param name="dstX"></param>
        /// <param name="dstY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void ClearAtlasArea(Texture2D atlasTexture, int dstX, int dstY, int width, int height)
        {
            Color[] atlasColors = Enumerable.Repeat<Color>(new Color(0f, 0f, 0f, 0f), width * height).ToArray();
            atlasTexture.SetPixels(dstX, dstY, width, height, atlasColors);
            atlasTexture.Apply();
        }

        /// <summary>
        /// Import the texture making sure the texture import settings are properly set
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
		public static bool ImportTexture( Texture2D texture )
		{
	#if UNITY_EDITOR
			if( texture != null )
			{
				return ImportTexture( AssetDatabase.GetAssetPath(texture) );
			}
	#endif
			return false;
		}

        /// <summary>
        /// Save a texture asset
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static bool SaveTextureAsset( Texture2D texture )
        {
#if UNITY_EDITOR
            if (texture != null)
            {
                string filePath = AssetDatabase.GetAssetPath(texture);
                if (filePath.Length > 0)
                {
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(filePath, bytes);

                    // Make sure LoadAssetAtPath and ImportTexture is going to work
                    AssetDatabase.Refresh();

                    UtilsAutoTileMap.ImportTexture(filePath);
                }
                else
                {
                    return false;
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Import the texture making sure the texture import settings are properly set
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
		public static bool ImportTexture( string path )
		{
	#if UNITY_EDITOR
			if( path.Length > 0 )
			{
				TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter; 
				if( textureImporter )
				{
                    textureImporter.alphaIsTransparency = true; // default
                    textureImporter.anisoLevel = 1; // default
                    textureImporter.borderMipmap = false; // default
                    textureImporter.mipmapEnabled = false; // default
                    textureImporter.compressionQuality = 100;
					textureImporter.isReadable = true;
					textureImporter.spritePixelsPerUnit = AutoTileset.PixelToUnits;                    
					textureImporter.spriteImportMode = SpriteImportMode.None;
					textureImporter.wrapMode = TextureWrapMode.Clamp;
					textureImporter.filterMode = FilterMode.Point;
					textureImporter.textureFormat = TextureImporterFormat.ARGB32;
                    textureImporter.textureType = TextureImporterType.Advanced;
					textureImporter.maxTextureSize = AutoTileset.k_MaxTextureSize;                    
					AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); 
				}
				return true;
			}
	#endif
			return false;
		}
		
		private static void _CopyBuildingThumbnails( AutoTileset autoTileset, Texture2D tilesetTex, int dstX, int dstY )
		{
			if( tilesetTex != null )
			{
                Rect srcRect = new Rect(0, 0, autoTileset.TilePartWidth, autoTileset.TilePartWidth);
                Rect dstRect = new Rect(0, 0, autoTileset.TileWidth, autoTileset.TileHeight);
                for (dstRect.y = dstY, srcRect.y = 0; dstRect.y < (dstY + 4 * autoTileset.TileHeight); dstRect.y += autoTileset.TileHeight, srcRect.y += 2 * autoTileset.TileHeight)
				{
                    for (dstRect.x = dstX, srcRect.x = 0; dstRect.x < dstX + autoTileset.AutoTilesPerRow * autoTileset.TileWidth; dstRect.x += autoTileset.TileWidth, srcRect.x += 2 * autoTileset.TileWidth)
					{
						Color[] thumbnailPartColors;
						thumbnailPartColors = tilesetTex.GetPixels( Mathf.RoundToInt(srcRect.x), Mathf.RoundToInt(srcRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x), Mathf.RoundToInt(dstRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x) + 3 * autoTileset.TilePartWidth, Mathf.RoundToInt(srcRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x) + autoTileset.TilePartWidth, Mathf.RoundToInt(dstRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x), Mathf.RoundToInt(srcRect.y) + 3 * autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x), Mathf.RoundToInt(dstRect.y) + autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x) + 3 * autoTileset.TilePartWidth, Mathf.RoundToInt(srcRect.y) + 3 * autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x) + autoTileset.TilePartWidth, Mathf.RoundToInt(dstRect.y) + autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);
						
					}
				}
			}
		}

        private static void _CopyWallThumbnails(AutoTileset autoTileset, Texture2D tilesetTex, int dstX, int dstY)
		{
			if( tilesetTex != null )
			{
                Rect srcRect = new Rect(0, 3 * autoTileset.TileHeight, autoTileset.TilePartWidth, autoTileset.TilePartWidth);
                Rect dstRect = new Rect(0, 0, autoTileset.TileWidth, autoTileset.TileHeight);
                for (dstRect.y = dstY, srcRect.y = 0; dstRect.y < (dstY + 3 * autoTileset.TileHeight); dstRect.y += autoTileset.TileHeight, srcRect.y += 5 * autoTileset.TileHeight)
				{
                    for (dstRect.x = dstX, srcRect.x = 0; dstRect.x < dstX + autoTileset.AutoTilesPerRow * autoTileset.TileWidth; dstRect.x += autoTileset.TileWidth, srcRect.x += 2 * autoTileset.TileWidth)
					{
						Color[] thumbnailPartColors;
						thumbnailPartColors = tilesetTex.GetPixels( Mathf.RoundToInt(srcRect.x), Mathf.RoundToInt(srcRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x), Mathf.RoundToInt(dstRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x) + 3 * autoTileset.TilePartWidth, Mathf.RoundToInt(srcRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x) + autoTileset.TilePartWidth, Mathf.RoundToInt(dstRect.y), Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x), Mathf.RoundToInt(srcRect.y) + 3 * autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x), Mathf.RoundToInt(dstRect.y) + autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);

                        thumbnailPartColors = tilesetTex.GetPixels(Mathf.RoundToInt(srcRect.x) + 3 * autoTileset.TilePartWidth, Mathf.RoundToInt(srcRect.y) + 3 * autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height));
                        autoTileset.AtlasTexture.SetPixels(Mathf.RoundToInt(dstRect.x) + autoTileset.TilePartWidth, Mathf.RoundToInt(dstRect.y) + autoTileset.TilePartHeight, Mathf.RoundToInt(srcRect.width), Mathf.RoundToInt(srcRect.height), thumbnailPartColors);
						
					}
				}
			}
		}
		
        private static void _CopyTilesetInAtlas( Texture2D atlasTexture, Texture2D tilesetTex, Rect rect )
        {
            _CopyTilesetInAtlas(atlasTexture, tilesetTex, (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }

		private static void _CopyTilesetInAtlas( Texture2D atlasTexture, Texture2D tilesetTex, int dstX, int dstY, int width, int height )
		{
			Color[] atlasColors;
			if( tilesetTex == null )
			{
				atlasColors = Enumerable.Repeat<Color>( new Color(0f, 0f, 0f, 0f) , width*height).ToArray();
			}
			else
			{
				atlasColors = tilesetTex.GetPixels();
			}
			
			atlasTexture.SetPixels( dstX, dstY, width, height, atlasColors);
		}
	}
}
