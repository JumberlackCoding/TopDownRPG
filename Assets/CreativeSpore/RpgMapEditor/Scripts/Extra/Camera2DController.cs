using UnityEngine;
using System.Collections;

namespace CreativeSpore
{
	[RequireComponent(typeof(Camera))]
	public class Camera2DController : MonoBehaviour {


		public Camera Camera{ get; private set; }

		public float Zoom = 1f;
		public float PixelToUnits = 100f;

		// Use this for initialization
		void Start () 
		{
			Camera = GetComponent<Camera>();
		}
		
		Vector3 m_vCamRealPos;
		void LateUpdate () 
		{
			//Note: ViewCamera.orthographicSize is not a real zoom based on pixels. This is the formula to calculate the real zoom.
			Camera.orthographicSize =  (Screen.height)/(2f*Zoom*PixelToUnits);
			Vector3 vOri = Camera.ScreenPointToRay( Vector3.zero ).origin;

			m_vCamRealPos = Camera.transform.position;
			Vector3 vPos = Camera.transform.position;
			float mod = (1f/(Zoom*PixelToUnits));
			vPos.x -= vOri.x % mod;
			vPos.y -= vOri.y % mod;
			Camera.transform.position = vPos;
		}

		void OnPostRender()
		{
			Camera.transform.position = m_vCamRealPos;
		}
	}
}
