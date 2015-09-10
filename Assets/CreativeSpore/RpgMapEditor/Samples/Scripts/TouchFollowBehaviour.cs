using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

namespace CreativeSpore
{

    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(MovingBehaviour))]
    [RequireComponent(typeof(PhysicCharBehaviour))]
    [RequireComponent(typeof(MapPathFindingBehaviour))]
    [RequireComponent(typeof(CharAnimationController))]
    public class TouchFollowBehaviour : MonoBehaviour
    {
        public float MinDistToReachTarget = 0.16f;

        MovingBehaviour m_moving;
        PhysicCharBehaviour m_phyChar;
        MapPathFindingBehaviour m_pathFindingBehaviour;
        CharAnimationController m_animCtrl;


        // Use this for initialization
        void Start()
        {
            m_moving = GetComponent<MovingBehaviour>();
            m_phyChar = GetComponent<PhysicCharBehaviour>();
            m_pathFindingBehaviour = GetComponent<MapPathFindingBehaviour>();
            m_animCtrl = GetComponent<CharAnimationController>();
        }

        void UpdateAnimDir()
        {
            float absVx = Mathf.Abs(m_moving.Veloc.x);
            float absVy = Mathf.Abs(m_moving.Veloc.y);
            m_animCtrl.IsAnimated = true;
            if (absVx > absVy)
            {
                if (m_moving.Veloc.x > 0)
                    m_animCtrl.CurrentDir = CharAnimationController.eDir.RIGHT;
                else if (m_moving.Veloc.x < 0)
                    m_animCtrl.CurrentDir = CharAnimationController.eDir.LEFT;
            }
            else if( absVy > 0f )
            {
                if (m_moving.Veloc.y > 0)
                    m_animCtrl.CurrentDir = CharAnimationController.eDir.UP;
                else if (m_moving.Veloc.y < 0)
                    m_animCtrl.CurrentDir = CharAnimationController.eDir.DOWN;
            }
            else
            {
                m_animCtrl.IsAnimated = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Get pressed world position
            bool mouseDown = Input.GetMouseButtonDown(0);
            bool touchUp = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
            if (mouseDown || touchUp)
            {
                Ray ray = Camera.main.ScreenPointToRay(mouseDown ? Input.mousePosition : new Vector3( Input.GetTouch(0).position.x, Input.GetTouch(0).position.y) );
                Plane hPlane = new Plane(Vector3.forward, Vector3.zero);
                float distance = 0;
                if (hPlane.Raycast(ray, out distance))
                {
                    // get the hit point:
                    m_pathFindingBehaviour.TargetPos = ray.GetPoint(distance);
                }
            }
            Vector3 vTarget = m_pathFindingBehaviour.TargetPos; vTarget.z = transform.position.z;

            // stop when target position has been reached
            Vector3 vDist = (vTarget - transform.position);
            //Debug.DrawLine(vTarget, transform.position); //TODO: the target is the touch position, not the target tile center. Fix this to go to target position once in the target tile
            m_pathFindingBehaviour.enabled = vDist.magnitude > MinDistToReachTarget;            
            if (!m_pathFindingBehaviour.enabled)
            {
                m_moving.Veloc = Vector3.zero;
            }

            //+++avoid obstacles
            Vector3 vTurnVel = Vector3.zero;
            if (0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.RIGHT))
            {
                vTurnVel.x = -m_moving.MaxSpeed;
            }
            else if (0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.LEFT))
            {
                vTurnVel.x = m_moving.MaxSpeed;
            }
            if (0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.DOWN))
            {
                vTurnVel.y = m_moving.MaxSpeed;
            }
            else if (0 != (m_phyChar.CollFlags & PhysicCharBehaviour.eCollFlags.UP))
            {
                vTurnVel.y = -m_moving.MaxSpeed;
            }
            if (vTurnVel != Vector3.zero)
            {
                m_moving.ApplyForce(vTurnVel - m_moving.Veloc);
            }
            //---

            UpdateAnimDir();
        }
    }
}