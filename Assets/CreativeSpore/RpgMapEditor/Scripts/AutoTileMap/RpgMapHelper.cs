using UnityEngine;
using System.Collections;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// Helper static class with useful methods
    /// </summary>
    public class RpgMapHelper
    {

        /// <summary>
        /// Return the world position of the mouse. 
        /// Use together with GetTileIdxByPosition and GetAutoTileByPosition to get the tile under the mouse.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetMouseWorldPosition()
        {
            return AutoTileMap.Instance.ViewCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y));
        }

        /// <summary>
        /// Return the index of the tile containing the world position vPos
        /// </summary>
        /// <param name="vPos"></param>
        /// <returns></returns>
        public static int GetTileIdxByPosition(Vector3 vPos)
        {
            vPos -= AutoTileMap.Instance.transform.position;

            // transform to pixel coords
            vPos.y = -vPos.y;

            vPos *= AutoTileset.PixelToUnits;
            if (vPos.x >= 0 && vPos.y >= 0)
            {
                int tile_x = (int)vPos.x / AutoTileMap.Instance.Tileset.TileWidth;
                int tile_y = (int)vPos.y / AutoTileMap.Instance.Tileset.TileWidth;
                return tile_y * AutoTileMap.Instance.MapTileWidth + tile_x;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Return the AutoTile of the tile containing the world position vPos and located in the layer iLayer ( 0 - ground, 1 - ground overlay, 2 - overlay )
        /// </summary>
        /// <param name="vPos"></param>
        /// <param name="iLayer"></param>
        /// <returns></returns>
        public static AutoTileMap.AutoTile GetAutoTileByPosition( Vector3 vPos, int iLayer )
        {
            vPos -= AutoTileMap.Instance.transform.position;            
            vPos.y = -vPos.y;
            vPos *= AutoTileset.PixelToUnits; // transform to pixel coords
            int tileX = (int)vPos.x / AutoTileMap.Instance.Tileset.TileWidth;
            int tileY = (int)vPos.y / AutoTileMap.Instance.Tileset.TileWidth;
            if (vPos.x < 0) tileX -= 1;
            if (vPos.y < 0) tileY -= 1;
            return AutoTileMap.Instance.GetAutoTile(tileX, tileY, iLayer);
        }

        /// <summary>
        /// Set a tile using a world position, instead a map coordinate
        /// </summary>
        /// <param name="vPos">World position</param>
        /// <param name="tileId">Tile index</param>
        /// <param name="iLayer">0 for ground, 1 for ground overlay, 2 for overlay (see AutoTileMap.eLayerType)</param>
        public static void SetAutoTileByPosition( Vector3 vPos, int tileId, int iLayer )
        {
            vPos -= AutoTileMap.Instance.transform.position;
            vPos.y = -vPos.y;// transform to pixel coords
            vPos *= AutoTileset.PixelToUnits;
            if (vPos.x >= 0 && vPos.y >= 0)
            {
                int tile_x = (int)vPos.x / AutoTileMap.Instance.Tileset.TileWidth;
                int tile_y = (int)vPos.y / AutoTileMap.Instance.Tileset.TileWidth;
                AutoTileMap.Instance.SetAutoTile( tile_x, tile_y, tileId, iLayer);
            }
        }

        /// <summary>
        /// Return the center world position of a map tile rect
        /// </summary>
        /// <param name="tile_x">x map coordinate</param>
        /// <param name="tile_y">y map coordinate</param>
        /// <returns></returns>
        public static Vector3 GetTileCenterPosition( int tile_x, int tile_y )
        {
            Vector3 vPos = new Vector3((tile_x + 0.5f) * AutoTileMap.Instance.Tileset.TileWorldWidth, -(tile_y + 0.5f) * AutoTileMap.Instance.Tileset.TileWorldHeight);
            return vPos;
        }

        /// <summary>
        /// Draw a debug rect in the editor
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        public static void DebugDrawRect( Vector3 pos, Rect rect, Color color )
		{
			rect.position += new Vector2(pos.x, pos.y);
			Debug.DrawLine( new Vector3(rect.x, rect.y, pos.z ), new Vector3(rect.x+rect.width, rect.y, pos.z ), color );
            Debug.DrawLine(new Vector3(rect.x, rect.y, pos.z), new Vector3(rect.x, rect.y + rect.height, pos.z), color);
            Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y, pos.z), new Vector3(rect.x + rect.width, rect.y + rect.height, pos.z), color);
            Debug.DrawLine(new Vector3(rect.x, rect.y + rect.height, pos.z), new Vector3(rect.x + rect.width, rect.y + rect.height, pos.z), color);
		}

        /// <summary>
        /// Draw a debug rect in the editor
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="duration"></param>
        public static void DebugDrawRect(Vector3 pos, Rect rect, Color color, float duration)
        {
            rect.position += new Vector2(pos.x, pos.y);
            Debug.DrawLine(new Vector3(rect.x, rect.y, pos.z), new Vector3(rect.x + rect.width, rect.y, pos.z), color, duration);
            Debug.DrawLine(new Vector3(rect.x, rect.y, pos.z), new Vector3(rect.x, rect.y + rect.height, pos.z), color, duration);
            Debug.DrawLine(new Vector3(rect.x + rect.width, rect.y, pos.z), new Vector3(rect.x + rect.width, rect.y + rect.height, pos.z), color, duration);
            Debug.DrawLine(new Vector3(rect.x, rect.y + rect.height, pos.z), new Vector3(rect.x + rect.width, rect.y + rect.height, pos.z), color, duration);
        }

        
        /// <summary>
        /// Reveal an area using as center, the tile over worldPosition and a sight length in tiles (sightLength)
        /// </summary>
        /// <param name="worldPosition">The center tile</param>
        /// <param name="sightLength">The sight length in tiles</param>
        public static void RemoveFogOfWar(Vector3 worldPosition, int sightLength)
        {
            float sqrMaxDist = sightLength * sightLength * 4;
            int iFogLayer = AutoTileMap.Instance.MapLayers.FindIndex(x => x.LayerType == AutoTileMap.eLayerType.FogOfWar);
            if (iFogLayer >= 0)
            {
                AutoTileMap.AutoTile centerTile = RpgMapHelper.GetAutoTileByPosition(worldPosition, iFogLayer);
                for (int yf = -sightLength; yf <= sightLength; ++yf)
                {
                    for (int xf = -sightLength; xf <= sightLength; ++xf)
                    {
                        int tx = centerTile.TileX + xf;
                        int ty = centerTile.TileY + yf;
                        if (AutoTileMap.Instance.IsValidAutoTilePos(tx, ty)) // This extra check is needed with fog of war layer
                        {
                            AutoTileMap.AutoTile autoTile = AutoTileMap.Instance.GetAutoTile(tx, ty, iFogLayer);
                            byte[] aFogAlpha = System.BitConverter.GetBytes(autoTile.Id);
                            for (int i = 0; i < aFogAlpha.Length; ++i)
                            {
                                //NOTE: for the fog level, each tile is divided in 4 tileparts. 
                                // The index i of each tile part is:
                                // 0|2
                                // 1|3

                                int xf2 = 0;
                                if (xf < 0)
                                    xf2 = (i == 0 || i == 1) ? 2 * -xf : 2 * -xf - 1; //NOTE: 0, 1 are the left tile parts
                                else if (xf > 0)
                                    xf2 = (i == 2 || i == 3) ? 2 * xf : 2 * xf - 1;
                                else //if( xf == 0 )
                                    xf2 = 0;

                                int yf2 = 0;
                                if (yf < 0)
                                    yf2 = (i == 0 || i == 2) ? 2 * -yf : 2 * -yf - 1;
                                else if (yf > 0)
                                    yf2 = (i == 1 || i == 3) ? 2 * yf : 2 * yf - 1;
                                else //if( xf == 0 )
                                    yf2 = 0;

                                float sqrDist = xf2 * xf2 + yf2 * yf2;

                                //NOTE: sqrDist = [0..(sightAreaSize*2)^2]

                                float fAlphaFactor = Mathf.Clamp01(sqrDist / sqrMaxDist);
                                fAlphaFactor *= fAlphaFactor;
                                byte fogAlpha = (byte)(0xff * fAlphaFactor);
                                aFogAlpha[i] = (byte)Mathf.Min(aFogAlpha[i], fogAlpha);
                            }
                            
                            autoTile.Id = System.BitConverter.ToInt32(aFogAlpha, 0);
                            AutoTileMap.Instance.RefreshTile(autoTile);
                        }
                    }
                }

                AutoTileMap.Instance.RefreshMinimapTexture(centerTile.TileX - sightLength, centerTile.TileY - sightLength, 2 * sightLength, 2 * sightLength);
            }
        }

        /// <summary>
        /// Reveal an area using as center, the tile over worldPosition and a sight length in tiles (sightLength)
        /// Uses a coroutine to make a fade transition
        /// </summary>
        /// <param name="worldPosition">The center tile</param>
        /// <param name="sightLength">The sight length in tiles</param>
        public static void RemoveFogOfWarWithFade(Vector3 worldPosition, int sightLength)
        {
            float sqrMaxDist = sightLength * sightLength * 4;
            int iFogLayer = AutoTileMap.Instance.MapLayers.FindIndex(x => x.LayerType == AutoTileMap.eLayerType.FogOfWar);
            if (iFogLayer >= 0)
            {
                AutoTileMap.AutoTile centerTile = RpgMapHelper.GetAutoTileByPosition(worldPosition, iFogLayer);
                for (int yf = -sightLength; yf <= sightLength; ++yf)
                {
                    for (int xf = -sightLength; xf <= sightLength; ++xf)
                    {
                        int tx = centerTile.TileX + xf;
                        int ty = centerTile.TileY + yf;
                        if (AutoTileMap.Instance.IsValidAutoTilePos(tx, ty)) // This extra check is needed with fog of war layer
                        {
                            byte[] aFogAlpha = new byte[4];
                            for (int i = 0; i < aFogAlpha.Length; ++i)
                            {
                                //NOTE: for the fog level, each tile is divided in 4 tileparts. 
                                // The index i of each tile part is:
                                // 0|2
                                // 1|3

                                int xf2 = 0;
                                if (xf < 0)
                                    xf2 = (i == 0 || i == 1) ? 2 * -xf : 2 * -xf - 1; //NOTE: 0, 1 are the left tile parts
                                else if (xf > 0)
                                    xf2 = (i == 2 || i == 3) ? 2 * xf : 2 * xf - 1;
                                else //if( xf == 0 )
                                    xf2 = 0;

                                int yf2 = 0;
                                if (yf < 0)
                                    yf2 = (i == 0 || i == 2) ? 2 * -yf : 2 * -yf - 1;
                                else if (yf > 0)
                                    yf2 = (i == 1 || i == 3) ? 2 * yf : 2 * yf - 1;
                                else //if( xf == 0 )
                                    yf2 = 0;

                                float sqrDist = xf2 * xf2 + yf2 * yf2;

                                //NOTE: sqrDist = [0..(sightAreaSize*2)^2]

                                float fAlphaFactor = Mathf.Clamp01(sqrDist / sqrMaxDist);
                                fAlphaFactor *= fAlphaFactor;
                                byte fogAlpha = (byte)(0xff * fAlphaFactor);
                                aFogAlpha[i] = fogAlpha;
                            }
                            AutoTileMap.Instance.AddFogOfWarSetToQueue(tx + ty * AutoTileMap.Instance.MapTileWidth, aFogAlpha);
                        }
                    }
                }

                AutoTileMap.Instance.RefreshMinimapTexture(centerTile.TileX - sightLength, centerTile.TileY - sightLength, 2 * sightLength, 2 * sightLength);
            }
        }
    }
}
