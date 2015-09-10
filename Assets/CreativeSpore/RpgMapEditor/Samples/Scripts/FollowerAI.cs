using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

namespace CreativeSpore
{
    [RequireComponent(typeof(MovingBehaviour))]
    [RequireComponent(typeof(PhysicCharBehaviour))]
    [RequireComponent(typeof(MapPathFindingBehaviour))]
    [RequireComponent(typeof(CharAnimationController))]
	public class FollowerAI : MonoBehaviour {

		GameObject m_player;
		MovingBehaviour m_moving;
		PhysicCharBehaviour m_phyChar;
        MapPathFindingBehaviour m_pathFindingBehaviour;
        CharAnimationController m_animCtrl;

        public bool LockAnimDir = false;
        public float MinDistToReachTarget = 0.32f;
		public float AngRandRadious = 0.16f;
		public float AngRandOff = 15f;
		float m_fAngOff;

		// Use this for initialization
		void Start () 
		{
			m_fAngOff = 360f * Random.value;
			m_player = GameObject.FindGameObjectWithTag("Player");
			m_moving = GetComponent<MovingBehaviour>();
			m_phyChar = GetComponent<PhysicCharBehaviour>();
            m_pathFindingBehaviour = GetComponent<MapPathFindingBehaviour>();
            m_animCtrl = GetComponent<CharAnimationController>();
		}

        void UpdateAnimDir()
        {
            if (m_moving.Veloc.magnitude >= (m_moving.MaxSpeed / 2f))
            {
                if (Mathf.Abs(m_moving.Veloc.x) > Mathf.Abs(m_moving.Veloc.y))
                {
                    if (m_moving.Veloc.x > 0)
                        m_animCtrl.CurrentDir = CharAnimationController.eDir.RIGHT;
                    else if (m_moving.Veloc.x < 0)
                        m_animCtrl.CurrentDir = CharAnimationController.eDir.LEFT;
                }
                else
                {
                    if (m_moving.Veloc.y > 0)
                        m_animCtrl.CurrentDir = CharAnimationController.eDir.UP;
                    else if (m_moving.Veloc.y < 0)
                        m_animCtrl.CurrentDir = CharAnimationController.eDir.DOWN;
                }
            }
        }

		// Update is called once per frame
		void Update() 
		{
            m_pathFindingBehaviour.TargetPos = m_player.transform.position;
            Vector3 vTarget = m_player.transform.position; vTarget.z = transform.position.z;

            // stop folling the path when closed enough to target
            m_pathFindingBehaviour.enabled = (vTarget - transform.position).magnitude > MinDistToReachTarget;
            if (!m_pathFindingBehaviour.enabled)
            {
                m_fAngOff += Random.Range(-AngRandOff, AngRandOff);
                Vector3 vOffset = Quaternion.AngleAxis(m_fAngOff, Vector3.forward) * (AngRandRadious * Vector3.right);
                vTarget += vOffset;
                Debug.DrawLine(transform.position, m_player.transform.position, Color.blue);
                Debug.DrawRay(m_player.transform.position, vOffset, Color.blue);

                m_moving.Arrive(vTarget);
            }

            //+++avoid obstacles
            Vector3 vTurnVel = Vector3.zero;
			if ( 0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.RIGHT))
			{
                vTurnVel.x = -m_moving.MaxSpeed;
			}
			else if ( 0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.LEFT))
			{
                vTurnVel.x = m_moving.MaxSpeed;
			}
			if ( 0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.DOWN))
			{
                vTurnVel.y = m_moving.MaxSpeed;
			}
			else if ( 0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.UP))
			{
                vTurnVel.y = -m_moving.MaxSpeed;
			}
            if( vTurnVel != Vector3.zero )
            {
                m_moving.ApplyForce(vTurnVel - m_moving.Veloc);
            }
            //---

            //fix to avoid flickering of the creature when collides with wall
            if (Time.frameCount % 16 == 0)
            //---            
            {
                if (!LockAnimDir) UpdateAnimDir();
            }
		}
	}
}