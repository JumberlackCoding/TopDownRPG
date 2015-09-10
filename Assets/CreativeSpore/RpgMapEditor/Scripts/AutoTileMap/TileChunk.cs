using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.RpgMapEditor
{
    /// <summary>
    /// Manage a chunk of tiles in the 3D world
    /// </summary>
	public class TileChunk : MonoBehaviour 
	{
        private static Material s_fogOfWarMaterial;
		private Vector3[] m_vertices;
		private Vector2[] m_uv;
		private int[] m_triangles;
        private Color32[] m_colors;

		public AutoTileMap MyAutoTileMap;

        /// <summary>
        /// Width size of chunk in tiles ( change default value in TileChunkPool.k_TileChunkWidth )
        /// </summary>
        public int TileWidth = 8;
        /// <summary>
        /// Height size of chunk int tiles ( change default value in TileChunkPool.k_TileChunkHeight )
        /// </summary>
        public int TileHeight = 4;

        /// <summary>
        /// Map layer for this tilechunk
        /// </summary>
		public int MapLayerIdx = 0;

        /// <summary>
        /// Position of top left tile of thins chunk in the map
        /// </summary>
		public int StartTileX = 0;

        /// <summary>
        /// Position of top left tile of thins chunk in the map
        /// </summary>
		public int StartTileY = 0;

        /// <summary>
        /// Sorting Order
        /// </summary>
        public int SortingOrder
        {
            get{ return m_meshFilter.GetComponent<Renderer>().sortingOrder; }
            set{ m_meshFilter.GetComponent<Renderer>().sortingOrder = value; }
        }

        public string SortingLayer
        {
            get { return m_meshFilter.GetComponent<Renderer>().sortingLayerName; }
            set { m_meshFilter.GetComponent<Renderer>().sortingLayerName = value; }
        }

		private MeshFilter m_meshFilter;

		struct AnimTileData
		{
			public int VertexIdx;
			public float U0,U1;
            public float V0, V1; // used for waterfalls
            public int SubTileRow; // used for waterfalls
		}
		private List<AnimTileData> m_animatedTiles = new List<AnimTileData>();
        private List<AnimTileData> m_animatedWaterfallTiles = new List<AnimTileData>();

		void OnWillRenderObject()
		{
			if( MyAutoTileMap.TileAnimFrameHasChanged )
			{
				m_uv = m_meshFilter.mesh.uv;

                // Animated tiles
                float fTextTileWidth = (float)MyAutoTileMap.Tileset.TileWidth / MyAutoTileMap.Tileset.AtlasTexture.width;
				float offset = fTextTileWidth * MyAutoTileMap.TileAnim3Frame * 2;
				foreach( AnimTileData animTileData in m_animatedTiles )
				{
					m_uv[ animTileData.VertexIdx + 0 ].x = animTileData.U0 + offset;
					m_uv[ animTileData.VertexIdx + 1 ].x = animTileData.U0 + offset;
					m_uv[ animTileData.VertexIdx + 2 ].x = animTileData.U1 + offset;
					m_uv[ animTileData.VertexIdx + 3 ].x = animTileData.U1 + offset;
				}
                // waterfall tiles
                float fTextTilePartHeight = (float)MyAutoTileMap.Tileset.TilePartHeight / MyAutoTileMap.Tileset.AtlasTexture.height;
                foreach (AnimTileData animTileData in m_animatedWaterfallTiles)
                {
                    int tilePartOff = (animTileData.SubTileRow + 4-MyAutoTileMap.TileAnim4Frame) % 4 - animTileData.SubTileRow;
                    offset = -fTextTilePartHeight * tilePartOff;
                    m_uv[animTileData.VertexIdx + 0].y = animTileData.V0 + offset;
                    m_uv[animTileData.VertexIdx + 1].y = animTileData.V1 + offset;
                    m_uv[animTileData.VertexIdx + 2].y = animTileData.V1 + offset;
                    m_uv[animTileData.VertexIdx + 3].y = animTileData.V0 + offset;
                }
				m_meshFilter.mesh.uv = m_uv;
			}
		}

        void OnDestroy()
        {
            //avoid memory leak
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                DestroyImmediate(meshFilter.sharedMesh);
            }
        }

        /// <summary>
        /// Configure this chunk
        /// </summary>
        /// <param name="autoTileMap"></param>
        /// <param name="layer"></param>
        /// <param name="startTileX"></param>
        /// <param name="startTileY"></param>
        /// <param name="tileChunkWidth"></param>
        /// <param name="tileChunkHeight"></param>
		public void Configure (AutoTileMap autoTileMap, int layer, int startTileX, int startTileY, int tileChunkWidth, int tileChunkHeight)
		{
			MyAutoTileMap = autoTileMap;
			TileWidth = tileChunkWidth;
			TileHeight = tileChunkHeight;
			MapLayerIdx = layer;
			StartTileX = startTileX;
			StartTileY = startTileY;

			transform.gameObject.name = "TileChunk"+startTileX+""+startTileY;
			Vector3 vPosition = new Vector3();
            vPosition.x = startTileX * MyAutoTileMap.Tileset.TileWorldWidth;
            vPosition.y = -startTileY * MyAutoTileMap.Tileset.TileWorldHeight;
			transform.localPosition = vPosition;

			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
			if( meshRenderer == null )
			{
				meshRenderer = transform.gameObject.AddComponent<MeshRenderer>();
			}

            if (autoTileMap.MapLayers[layer].LayerType == AutoTileMap.eLayerType.FogOfWar)
            {
                if( s_fogOfWarMaterial == null)
                {
                    s_fogOfWarMaterial = new Material(Shader.Find("Sprites/Default"));
                    Texture2D fogOfWarTexture = new Texture2D(1, 1);
                    fogOfWarTexture.SetPixel(0, 0, Color.gray);
                    fogOfWarTexture.Apply();
                    s_fogOfWarMaterial.SetTexture(0, fogOfWarTexture);
                }
                meshRenderer.sharedMaterial = s_fogOfWarMaterial;
            }
            else
            {
                meshRenderer.sharedMaterial = MyAutoTileMap.Tileset.AtlasMaterial;
            }

#if UNITY_5_0 || UNITY_5_1
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
#else
            meshRenderer.castShadows = false;
#endif
			meshRenderer.receiveShadows = false;

            m_meshFilter = GetComponent<MeshFilter>();
            if (m_meshFilter == null)
            {
                m_meshFilter = transform.gameObject.AddComponent<MeshFilter>();
            }
		}

        /// <summary>
        /// Regenerate the mesh for this tile chunk
        /// </summary>
		public void RefreshTileData()
		{
            if (m_meshFilter.sharedMesh == null)
            {
                m_meshFilter.sharedMesh = new Mesh();
            }
            Mesh mesh = m_meshFilter.sharedMesh;
            mesh.Clear();

            if (MyAutoTileMap.MapLayers[MapLayerIdx].LayerType == AutoTileMap.eLayerType.FogOfWar)
            {
                FillFogOfWarData();
            }
            else
            {
                FillData();
            }

            mesh.vertices = m_vertices;
            mesh.colors32 = m_colors;
            mesh.uv = m_uv;
            mesh.triangles = m_triangles;

            mesh.RecalculateNormals(); // allow using lights
		}

		void FillFogOfWarData()
		{
			m_animatedTiles.Clear();
            m_animatedWaterfallTiles.Clear();
			m_vertices = new Vector3[ TileWidth * TileHeight * 4 * 4 ]; // 4 subtiles x 4 vertex per tile
            m_colors = new Color32[TileWidth * TileHeight * 4 * 4];
			m_uv = new Vector2[ m_vertices.Length ];
			m_triangles = new int[ TileWidth*TileHeight*4*2*3 ]; // 4 subtiles x 2 triangles per tile x 3 vertex per triangle

			int vertexIdx = 0;
			int triangleIdx = 0;
            //TODO: optimize updating only updated tiles inside the chunk
            for (int tileX = 0; tileX < TileWidth; ++tileX)
            {
                for (int tileY = 0; tileY < TileHeight; ++tileY)
                {
                    AutoTileMap.AutoTile autoTile = MyAutoTileMap.GetAutoTile( StartTileX + tileX, StartTileY + tileY, MapLayerIdx);
                    byte[] fogColors = System.BitConverter.GetBytes(autoTile.Id);
                    {
                        int subTileXBase = tileX << 1; // <<1 == *2
                        int subTileYBase = tileY << 1; // <<1 == *2
                        int fogColorIdx = 0;
                        for (int xf = 0; xf < 2; ++xf)
                        {
                            for (int yf = 0; yf < 2; ++yf, ++fogColorIdx)
                            {
                                int subTileX = subTileXBase + xf;
                                int subTileY = subTileYBase + yf;

                                float px0 = subTileX * (MyAutoTileMap.Tileset.TileWorldWidth / 2);
                                float py0 = -subTileY * (MyAutoTileMap.Tileset.TileWorldHeight / 2);
                                float px1 = (subTileX + 1) * (MyAutoTileMap.Tileset.TileWorldWidth / 2);
                                float py1 = -(subTileY + 1) * (MyAutoTileMap.Tileset.TileWorldHeight / 2);

                                m_vertices[vertexIdx + 0] = new Vector3(px0, py0, 0);
                                m_vertices[vertexIdx + 1] = new Vector3(px0, py1, 0);
                                m_vertices[vertexIdx + 2] = new Vector3(px1, py1, 0);
                                m_vertices[vertexIdx + 3] = new Vector3(px1, py0, 0);

                                m_colors[vertexIdx + 0] = new Color32(0, 0, 0, fogColors[fogColorIdx]);
                                m_colors[vertexIdx + 1] = new Color32(0, 0, 0, fogColors[fogColorIdx]);
                                m_colors[vertexIdx + 2] = new Color32(0, 0, 0, fogColors[fogColorIdx]);
                                m_colors[vertexIdx + 3] = new Color32(0, 0, 0, fogColors[fogColorIdx]);                                

                                m_triangles[triangleIdx + 0] = vertexIdx + 2;
                                m_triangles[triangleIdx + 1] = vertexIdx + 1;
                                m_triangles[triangleIdx + 2] = vertexIdx + 0;
                                m_triangles[triangleIdx + 3] = vertexIdx + 0;
                                m_triangles[triangleIdx + 4] = vertexIdx + 3;
                                m_triangles[triangleIdx + 5] = vertexIdx + 2;

                                float u0 = 0;
                                float u1 = 0;
                                float v0 = 1;
                                float v1 = 1;
                                m_uv[vertexIdx + 0] = new Vector3(u0, v0, 0);
                                m_uv[vertexIdx + 1] = new Vector3(u0, v1, 0);
                                m_uv[vertexIdx + 2] = new Vector3(u1, v1, 0);
                                m_uv[vertexIdx + 3] = new Vector3(u1, v0, 0);

                                // increment vectex and triangle idx
                                vertexIdx += 4;
                                triangleIdx += 6;
                            }
                        }
                    }
                }
            }
			
			// resize arrays
			System.Array.Resize( ref m_vertices, vertexIdx );
            System.Array.Resize(ref m_colors, vertexIdx);
			System.Array.Resize( ref m_uv, vertexIdx );
			System.Array.Resize( ref m_triangles, triangleIdx );
		}

        void FillData()
        {
            m_animatedTiles.Clear();
            m_animatedWaterfallTiles.Clear();
            m_vertices = new Vector3[TileWidth * TileHeight * 4 * 4]; // 4 subtiles x 4 vertex per tile
            m_colors = new Color32[TileWidth * TileHeight * 4 * 4];
            m_uv = new Vector2[m_vertices.Length];
            m_triangles = new int[TileWidth * TileHeight * 4 * 2 * 3]; // 4 subtiles x 2 triangles per tile x 3 vertex per triangle

            int vertexIdx = 0;
            int triangleIdx = 0;
            //TODO: optimize updating only updated tiles inside the chunk
            for (int tileX = 0; tileX < TileWidth; ++tileX)
            {
                for (int tileY = 0; tileY < TileHeight; ++tileY)
                {
                    AutoTileMap.AutoTile autoTile = MyAutoTileMap.GetAutoTile(StartTileX + tileX, StartTileY + tileY, MapLayerIdx);
                    if (autoTile.Id >= 0)
                    {
                        int subTileXBase = tileX << 1; // <<1 == *2
                        int subTileYBase = tileY << 1; // <<1 == *2
                        for (int xf = 0; xf < 2; ++xf)
                        {
                            for (int yf = 0; yf < 2; ++yf)
                            {
                                int subTileX = subTileXBase + xf;
                                int subTileY = subTileYBase + yf;

                                float px0 = subTileX * (MyAutoTileMap.Tileset.TileWorldWidth / 2);
                                float py0 = -subTileY * (MyAutoTileMap.Tileset.TileWorldHeight / 2);
                                float px1 = (subTileX + 1) * (MyAutoTileMap.Tileset.TileWorldWidth / 2);
                                float py1 = -(subTileY + 1) * (MyAutoTileMap.Tileset.TileWorldHeight / 2);

                                m_vertices[vertexIdx + 0] = new Vector3(px0, py0, 0);
                                m_vertices[vertexIdx + 1] = new Vector3(px0, py1, 0);
                                m_vertices[vertexIdx + 2] = new Vector3(px1, py1, 0);
                                m_vertices[vertexIdx + 3] = new Vector3(px1, py0, 0);

                                m_colors[vertexIdx + 0] = new Color32(255, 255, 255, 255);
                                m_colors[vertexIdx + 1] = new Color32(255, 255, 255, 255);
                                m_colors[vertexIdx + 2] = new Color32(255, 255, 255, 255);
                                m_colors[vertexIdx + 3] = new Color32(255, 255, 255, 255);

                                m_triangles[triangleIdx + 0] = vertexIdx + 2;
                                m_triangles[triangleIdx + 1] = vertexIdx + 1;
                                m_triangles[triangleIdx + 2] = vertexIdx + 0;
                                m_triangles[triangleIdx + 3] = vertexIdx + 0;
                                m_triangles[triangleIdx + 4] = vertexIdx + 3;
                                m_triangles[triangleIdx + 5] = vertexIdx + 2;

                                float u0, u1, v0, v1;
                                if (autoTile.Type == AutoTileMap.eTileType.OBJECTS || autoTile.Type == AutoTileMap.eTileType.NORMAL)
                                {
                                    int spriteIdx = autoTile.TilePartsIdx[0];
                                    Rect sprTileRect = MyAutoTileMap.Tileset.AutoTileRects[spriteIdx];
                                    u0 = (((subTileX % 2) * sprTileRect.width / 2) + sprTileRect.x) / MyAutoTileMap.Tileset.AtlasTexture.width;
                                    u1 = (((subTileX % 2) * sprTileRect.width / 2) + sprTileRect.x + sprTileRect.width / 2) / MyAutoTileMap.Tileset.AtlasTexture.width;
                                    v0 = (((1 - subTileY % 2) * sprTileRect.height / 2) + sprTileRect.y + sprTileRect.height / 2) / MyAutoTileMap.Tileset.AtlasTexture.height;
                                    v1 = (((1 - subTileY % 2) * sprTileRect.height / 2) + sprTileRect.y) / MyAutoTileMap.Tileset.AtlasTexture.height;
                                }
                                else
                                {
                                    int tilePartIdx = (subTileY % 2) * 2 + (subTileX % 2);
                                    int spriteIdx = autoTile.TilePartsIdx[tilePartIdx];
                                    Rect sprTileRect = MyAutoTileMap.Tileset.AutoTileRects[spriteIdx];
                                    u0 = sprTileRect.x / MyAutoTileMap.Tileset.AtlasTexture.width;
                                    u1 = (sprTileRect.x + sprTileRect.width) / MyAutoTileMap.Tileset.AtlasTexture.width;
                                    v0 = (sprTileRect.y + sprTileRect.height) / MyAutoTileMap.Tileset.AtlasTexture.height;
                                    v1 = sprTileRect.y / MyAutoTileMap.Tileset.AtlasTexture.height;

                                    if (MyAutoTileMap.Tileset.IsAutoTileAnimated(autoTile.Id))
                                    {
                                        m_animatedTiles.Add(new AnimTileData() { VertexIdx = vertexIdx, U0 = u0, U1 = u1 });
                                    }
                                    else if (MyAutoTileMap.Tileset.IsAutoTileAnimatedWaterfall(autoTile.Id))
                                    {
                                        int subTileRow = (spriteIdx / 32);//32 = number of subtiles in a row;
                                        subTileRow %= 6; // make it relative to this autotile
                                        subTileRow -= 2; // remove top tiles
                                        m_animatedWaterfallTiles.Add(new AnimTileData() { VertexIdx = vertexIdx, V0 = v0, V1 = v1, SubTileRow = subTileRow });
                                    }
                                }
                                m_uv[vertexIdx + 0] = new Vector3(u0, v0, 0);
                                m_uv[vertexIdx + 1] = new Vector3(u0, v1, 0);
                                m_uv[vertexIdx + 2] = new Vector3(u1, v1, 0);
                                m_uv[vertexIdx + 3] = new Vector3(u1, v0, 0);

                                // increment vectex and triangle idx
                                vertexIdx += 4;
                                triangleIdx += 6;
                            }
                        }
                    }
                }
            }

            // resize arrays
            System.Array.Resize(ref m_vertices, vertexIdx);
            System.Array.Resize(ref m_colors, vertexIdx);
            System.Array.Resize(ref m_uv, vertexIdx);
            System.Array.Resize(ref m_triangles, triangleIdx);
        }
	}
}