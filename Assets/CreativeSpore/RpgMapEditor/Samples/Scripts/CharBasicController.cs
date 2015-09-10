using UnityEngine;
using System.Collections;

namespace CreativeSpore
{
    [RequireComponent(typeof(CharAnimationController))]
    [RequireComponent(typeof(PhysicCharBehaviour))]
    public class CharBasicController : MonoBehaviour
    {
        public CharAnimationController AnimCtrl { get { return m_animCtrl; } }
        public PhysicCharBehaviour PhyCtrl { get { return m_phyChar; } }

        public bool IsVisible
        {
            get
            {
                return m_animCtrl.TargetSpriteRenderer.enabled;
            }

            set
            {
                SetVisible( value );
            }
        }

        protected CharAnimationController m_animCtrl;
        protected PhysicCharBehaviour m_phyChar;

        protected float m_timerBlockDir = 0f;

        protected virtual void Start()
        {
            m_animCtrl = GetComponent<CharAnimationController>();
            m_phyChar = GetComponent<PhysicCharBehaviour>();
        }

        protected virtual void Update()
        {
            float fAxisX = Input.GetAxis("Horizontal");
            float fAxisY = Input.GetAxis("Vertical");
            UpdateMovement(fAxisX, fAxisY);
        }


        protected void UpdateMovement( float fAxisX, float fAxisY )
        {
            m_timerBlockDir -= Time.deltaTime;
            m_phyChar.Dir = new Vector3(fAxisX, fAxisY, 0);

            if (m_phyChar.IsMoving)
            {
                m_animCtrl.IsAnimated = true;

                if (m_timerBlockDir <= 0f)
                {
                    if (Mathf.Abs(fAxisX) > Mathf.Abs(fAxisY))
                    {
                        if (fAxisX > 0)
                            m_animCtrl.CurrentDir = CharAnimationController.eDir.RIGHT;
                        else if (fAxisX < 0)
                            m_animCtrl.CurrentDir = CharAnimationController.eDir.LEFT;
                    }
                    else
                    {
                        if (fAxisY > 0)
                            m_animCtrl.CurrentDir = CharAnimationController.eDir.UP;
                        else if (fAxisY < 0)
                            m_animCtrl.CurrentDir = CharAnimationController.eDir.DOWN;
                    }
                }
            }
            else
            {
                m_animCtrl.IsAnimated = false;
            }
        }

        public virtual void SetVisible( bool value )
        {
            m_animCtrl.TargetSpriteRenderer.enabled = value;
        }
    }
}