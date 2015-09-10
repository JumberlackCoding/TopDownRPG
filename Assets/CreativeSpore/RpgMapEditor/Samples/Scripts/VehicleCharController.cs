using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;

namespace CreativeSpore
{
    [RequireComponent(typeof(PhysicCharBehaviour))]
    public class VehicleCharController : CharBasicController
    {
        public enum eVehicleType
        {
            Boat,
            Aircraft
        }
        public eVehicleType VehicleType;

        public GameObject HelpText;
        public GameObject ShadowSprite;
        public float MaxAltitude = 1.8f;
        public int MaxAltitudeSightLength = 10; // sight view length through fog of war at max altitude
        public float LandingTime = 1f; // used for taking off too
        PlayerController m_playerCtrl;
        PhysicCharBehaviour phyChar;

        private bool m_isLanding = false;
        private Vector3 m_vLandingPos;
        private float m_altitude = 0f;

        protected override void Start()
        {
            base.Start();
            m_playerCtrl = GameObject.FindObjectOfType<PlayerController>();
            phyChar = GetComponent<PhysicCharBehaviour>();
        }

        // used calculation of landing position according to direction
        float[] _landingMult = new float[]
        {
            1f, //DOWN
            1.2f, //LEFT
            1.2f, //RIGHT
            0.7f// UP
        };

        private int m_lastTileIdx = -1;
        private int m_lastFogSightLength = 0;

        protected override void Update()
        {
            if (m_playerCtrl == null)
                return;

            Color textColor = HelpText.GetComponent<Renderer>().material.color;
            textColor.a = Mathf.Clamp(0.2f + Mathf.Abs(Mathf.Sin(0.05f * Time.frameCount)), 0f, 1f);
            HelpText.GetComponent<Renderer>().material.color = textColor;

            if (m_isLanding)
            {
                if (VehicleType == eVehicleType.Aircraft)
                {
                    m_altitude = Mathf.Lerp(MaxAltitude, 0f, LandingTime * (1 - m_altitude / MaxAltitude) + Time.deltaTime);
                    //m_altitude -= m_altitude / 20;                    
                    
                    if (m_altitude <= 0.01)
                    {
                        m_altitude = 0;
                        m_playerCtrl.SetVisible(true);
                    }
                }
            }
            else
            {
                // Empty vehicle logic
                if (m_playerCtrl.Vehicle == null)
                {
                    Vector3 vPos = transform.position;
                    bool isPlayerCloseEnough = false;
                    if (VehicleType == eVehicleType.Boat)
                    {
                        m_animCtrl.IsAnimated = true; //Force to always animate
                        Rect rVert = new Rect(vPos.x + phyChar.CollRect.x, vPos.y + phyChar.CollRect.y - phyChar.CollRect.height, phyChar.CollRect.width, 3 * phyChar.CollRect.height);
                        Rect rHor = new Rect(vPos.x + phyChar.CollRect.x - phyChar.CollRect.width, vPos.y + phyChar.CollRect.y, 3 * phyChar.CollRect.width, phyChar.CollRect.height);
                        RpgMapHelper.DebugDrawRect(Vector3.zero, rVert, Color.blue);
                        RpgMapHelper.DebugDrawRect(Vector3.zero, rHor, Color.green);
                        isPlayerCloseEnough = rVert.Contains(m_playerCtrl.transform.position) || rHor.Contains(m_playerCtrl.transform.position);
                    }
                    else //if (VehicleType == eVehicleType.Aircraft)
                    {
                        m_animCtrl.IsAnimated = false; //Force to don't animate
                        Rect rect = new Rect(vPos.x + phyChar.CollRect.x, vPos.y + phyChar.CollRect.y - phyChar.CollRect.height / 3, phyChar.CollRect.width, phyChar.CollRect.height);
                        RpgMapHelper.DebugDrawRect(Vector3.zero, rect, Color.blue);
                        isPlayerCloseEnough = rect.Contains(m_playerCtrl.transform.position);
                    }
                   
                    HelpText.GetComponent<Renderer>().enabled = isPlayerCloseEnough;
                    if (isPlayerCloseEnough)
                    {
                        if (Input.GetKeyDown(KeyCode.Return))
                        {
                            m_playerCtrl.Vehicle = this;
                            HelpText.GetComponent<Renderer>().enabled = false;
                            m_playerCtrl.SetVisible(VehicleType == eVehicleType.Boat);
                            if (VehicleType == eVehicleType.Aircraft)
                            {
                                m_phyChar.IsCollEnabled = false;                                
                            }
                        }
                    }
                }
                // Occupied vehicle logic
                else if (m_playerCtrl.Vehicle == this)
                {
                    base.Update();
                    m_animCtrl.IsAnimated = true; //Force to always animate

                    if (VehicleType == eVehicleType.Aircraft)
                    {
                        m_altitude = Mathf.Lerp(0f, MaxAltitude, LandingTime*(m_altitude / MaxAltitude) + Time.deltaTime);
                        int sightLength = (int)(MaxAltitudeSightLength*(m_altitude / MaxAltitude));

                        int tileIdx = RpgMapHelper.GetTileIdxByPosition(transform.position);

                        if (tileIdx != m_lastTileIdx || m_lastFogSightLength != sightLength)
                        {
                            RpgMapHelper.RemoveFogOfWarWithFade(transform.position, sightLength);
                        }

                        m_lastFogSightLength = sightLength;
                        m_lastTileIdx = tileIdx;

                    }

                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        int dir = VehicleType == eVehicleType.Boat ? (int)m_playerCtrl.AnimCtrl.CurrentDir : (int)CharAnimationController.eDir.DOWN;
                        Vector3 vDir = VehicleType == eVehicleType.Boat ? m_playerCtrl.AnimCtrl.CurrentDirVect : new Vector3(0, -0.5f);
                        float fMult = _landingMult[dir];
                        Vector3 vLandingPos = transform.position + fMult * vDir * AutoTileMap.Instance.Tileset.TileWorldWidth;
                        vLandingPos.z = m_playerCtrl.transform.position.z;
                        bool isColliding = m_playerCtrl.PhyCtrl.IsColliding(vLandingPos);
                        RpgMapHelper.DebugDrawRect(vLandingPos, m_playerCtrl.PhyCtrl.CollRect, isColliding ? Color.red : Color.blue, 0.5f);

                        if (VehicleType == eVehicleType.Aircraft)
                        {
                            isColliding |= PhyCtrl.IsColliding(transform.position);
                            RpgMapHelper.DebugDrawRect(transform.position, PhyCtrl.CollRect, isColliding ? Color.red : Color.blue, 0.5f);
                        }

                        isColliding |= RpgMapHelper.GetAutoTileByPosition( vLandingPos, 0 ).IsWaterTile();

                        if (!isColliding)
                        {
                            m_vLandingPos = vLandingPos;
                            m_isLanding = true;
                            PhyCtrl.Dir = Vector3.zero;
                        }
                    }
                }
            }

            Vector3 vAnimSprPos = AnimCtrl.TargetSpriteRenderer.transform.localPosition;
            vAnimSprPos.y = m_altitude;
            AnimCtrl.TargetSpriteRenderer.transform.localPosition = vAnimSprPos;   
            if( ShadowSprite != null )
            {
                float scale = (MaxAltitude - m_altitude / 3);
                ShadowSprite.transform.localScale = new Vector3(scale, scale, scale);
            }

            //+++fix overlay layer problem while landing
            if (VehicleType == eVehicleType.Aircraft)
            {
                Vector3 vCheckPos = m_animCtrl.TargetSpriteRenderer.transform.position;
                //Debug.DrawRay( vCheckPos, Vector3.right, Color.red );
                int bottomTile = RpgMapHelper.GetAutoTileByPosition(vCheckPos, 2).Id;
                vCheckPos.y += AutoTileMap.Instance.Tileset.TileWorldHeight;
                //Debug.DrawRay(vCheckPos, Vector3.right, Color.green);
                int topTile = RpgMapHelper.GetAutoTileByPosition(vCheckPos, 2).Id;
                if (
                    (bottomTile < 0 && topTile >= 0 && m_altitude > 0.30f) // bottom tile is not overlay and top tile is overlay
                    || (m_altitude > 0.02f && bottomTile < 0) // taking off set Z over overlay layer only after bottom tile is not overlay
                    ) 
                {
                    GetComponent<IsoSpriteController>().enabled = false;
                    Vector3 vSprPos = m_animCtrl.TargetSpriteRenderer.transform.position;
                    vSprPos.z = AutoTileMap.Instance.FindLastLayer( AutoTileMap.eLayerType.Overlay ).Depth - 0.1f;
                    m_animCtrl.TargetSpriteRenderer.transform.position = vSprPos;
                }
                else if( m_altitude <= 0.8 )
                { // found an overlay tile? enable IsoSpriteController to adjust Z under overlay layer
                    GetComponent<IsoSpriteController>().enabled = true;
                }
            }
            //---
        }

        void FixedUpdate()
        {
            if (m_isLanding && m_altitude == 0)
            {
                Vector3 vDiff = m_vLandingPos - m_playerCtrl.transform.position;
                m_playerCtrl.transform.position += vDiff / 4;
                if (vDiff.sqrMagnitude <= 0.02)
                {
                    m_isLanding = false;
                    m_playerCtrl.Vehicle = null;
                    HelpText.GetComponent<Renderer>().enabled = true;
                    m_playerCtrl.transform.position = m_vLandingPos;
                    
                    if (VehicleType == eVehicleType.Aircraft)
                    {
                        GetComponent<IsoSpriteController>().enabled = true;
                    }
                    m_phyChar.IsCollEnabled = true;
                }
            }
        }

        void LateUpdate()
        {
            // controls player while on vehicle
            if ( m_playerCtrl != null && m_playerCtrl.Vehicle == this && !m_isLanding)
            {
                m_playerCtrl.AnimCtrl.IsAnimated = false;
                Vector3 vPos = m_playerCtrl.transform.position;
                vPos += (transform.position - m_playerCtrl.transform.position) / 2;
                vPos.z = m_playerCtrl.transform.position.z;
                m_playerCtrl.transform.position = vPos;
            }
        }
    }
}