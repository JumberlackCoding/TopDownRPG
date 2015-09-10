using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

namespace CreativeSpore
{
	public class PlayerController : CharBasicController {

		public GameObject BulletPrefab;
		public float TimerBlockDirSet = 0.6f;
		public Camera2DController Camera2D;
		public float BulletAngDispersion = 15f;
        public SpriteRenderer ShadowSprite;
        public SpriteRenderer WeaponSprite;
        public int FogSightLength = 5;

        /// <summary>
        /// If player is driving a vehicle, this will be that vehicle
        /// </summary>
        public VehicleCharController Vehicle;        

		private FollowObjectBehaviour m_camera2DFollowBehaviour;


        public override void SetVisible(bool value)
        {
            base.SetVisible(value);
            ShadowSprite.enabled = value;
            WeaponSprite.enabled = value;
        }

		// Use this for initialization
		protected override void Start () 
		{
            base.Start();
			if( Camera2D == null )
			{
				Camera2D = GameObject.FindObjectOfType<Camera2DController>();
			}
			
			m_camera2DFollowBehaviour = Camera2D.transform.GetComponent<FollowObjectBehaviour>();
			m_camera2DFollowBehaviour.Target = transform;
		}

		void CreateBullet( Vector3 vPos, Vector3 vDir )
		{
			GameFactory.CreateBullet( gameObject, BulletPrefab, vPos, vDir, 4f );
		}

		void DoInputs()
		{			
			Vector3 vBulletDir = Vector3.zero;
			Vector3 vBulletPos = Vector3.zero;
			if( Input.GetKeyDown( "j" ) ) //down
			{
				vBulletPos = new Vector3( -0.08f, -0.02f, 0f );
				vBulletPos += transform.position;
				vBulletDir = Vector3.down;
				m_animCtrl.CurrentDir = CharAnimationController.eDir.DOWN;
				m_timerBlockDir = TimerBlockDirSet;
			}
			else if( Input.GetKeyDown( "h" ) ) // left
			{
				vBulletPos = new Vector3( -0.10f, 0.10f, 0f );
				vBulletPos += transform.position;
				vBulletDir = -Vector3.right;
				m_animCtrl.CurrentDir = CharAnimationController.eDir.LEFT;
				m_timerBlockDir = TimerBlockDirSet;
			}
			else if( Input.GetKeyDown( "k" ) ) // right
			{
				vBulletPos = new Vector3( 0.10f, 0.10f, 0f );
				vBulletPos += transform.position;
				vBulletDir = Vector3.right;
				m_animCtrl.CurrentDir = CharAnimationController.eDir.RIGHT;
				m_timerBlockDir = TimerBlockDirSet;
			}
			else if( Input.GetKeyDown( "u" ) ) // up
			{
				vBulletPos = new Vector3( 0.08f, 0.32f, 0f );
				vBulletPos += transform.position;
				vBulletDir = -Vector3.down;
				m_animCtrl.CurrentDir = CharAnimationController.eDir.UP;
				m_timerBlockDir = TimerBlockDirSet;
			}

			if( vBulletDir != Vector3.zero )
			{
				float fRand = Random.Range(-1f, 1f);
				fRand = Mathf.Pow(fRand, 5f);
				vBulletDir = Quaternion.AngleAxis(BulletAngDispersion*fRand, Vector3.forward) * vBulletDir;
				CreateBullet( vBulletPos, vBulletDir);
			}
		}

        private int m_lastTileIdx = -1;
        private int m_lastFogSightLength = 0;

        protected override void Update()
		{
            base.Update();
            m_phyChar.enabled = (Vehicle == null);
            if (Vehicle != null)
            {
                m_animCtrl.IsAnimated = false;
            }
            else
            {
                DoInputs();

                bool isMoving = (m_phyChar.Dir.sqrMagnitude >= 0.01);
                if (isMoving)
                {
                    //m_phyChar.Dir.Normalize();
                    m_camera2DFollowBehaviour.Target = transform;
                }
                else
                {
                    m_phyChar.Dir = Vector3.zero;
                }
            }

            int tileIdx = RpgMapHelper.GetTileIdxByPosition(transform.position);

            if (tileIdx != m_lastTileIdx || m_lastFogSightLength != FogSightLength)
            {
                RpgMapHelper.RemoveFogOfWarWithFade(transform.position, FogSightLength);
            }

            m_lastFogSightLength = FogSightLength;
            m_lastTileIdx = tileIdx;
		}             
	}
}