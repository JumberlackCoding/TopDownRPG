using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

namespace CreativeSpore
{
	public class IsoSpriteController : MonoBehaviour {

		public SpriteRenderer m_spriteRender;

        private float m_OverlayLayerZ, m_GroundOverlayLayerZ;

		// Use this for initialization
		void Start () 
        {
			if (m_spriteRender == null)
			{
				m_spriteRender = GetComponent<SpriteRenderer>();
			}

            m_OverlayLayerZ = AutoTileMap.Instance.FindFirstLayer(AutoTileMap.eLayerType.Overlay).Depth;
            m_GroundOverlayLayerZ = AutoTileMap.Instance.FindLastLayer(AutoTileMap.eLayerType.Ground).Depth;
		}
		
		// Update is called once per frame
		void Update () 
		{
			Vector3 vPos = m_spriteRender.transform.position;
            float f0 = Mathf.Abs(m_OverlayLayerZ - m_GroundOverlayLayerZ);
            float f1 = Mathf.Abs(AutoTileMap.Instance.transform.position.y - transform.position.y) / (AutoTileMap.Instance.MapTileHeight * AutoTileMap.Instance.Tileset.TileWorldHeight);
            vPos.z = m_GroundOverlayLayerZ - f0 * f1;
			m_spriteRender.transform.position = vPos;
		}
	}
}