using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace CreativeSpore
{
	public class CharAnimationController : MonoBehaviour 
	{
		public enum eDir
		{
			DOWN,
			LEFT,
			RIGHT,
			UP
		}

        public enum eCharSetType
        {
            RPG_Maker_VX,
            RPG_Maker_XP
        }

        Vector3[] m_dirVect = new Vector3[]
        {
            new Vector3(0, -1), //DOWN
            new Vector3(-1, 0), //LEFT
            new Vector3(1, 0), //RIGHT
            new Vector3(0, 1), //UP
        };

		public Sprite SpriteCharSet;
		public SpriteRenderer TargetSpriteRenderer;
        public Vector2[] Pivot = null;
		public eDir CurrentDir
        {
            get { return m_currentDir; }
            set 
            {
                bool hasChanged = m_currentDir != value;
                m_currentDir = value;
                if (hasChanged)
                {
                    if (m_charsetType == eCharSetType.RPG_Maker_XP)
                        TargetSpriteRenderer.sprite = m_spriteXpIdleFrames[(int)m_currentDir];
                    else
                        TargetSpriteRenderer.sprite = m_spriteFrames[(int)((int)m_currentDir * AnimFrames)];
                }
            }
        }
        public Vector3 CurrentDirVect { get { return m_dirVect[(int)CurrentDir]; } }
        public bool IsPingPongAnim = true; // set true for ping pong animation
		public bool IsAnimated = true;
		public float AnimSpeed = 9f; // frames per second
        public int AnimFrames
        {
            get { return 3; }
        }
        public eCharSetType CharsetType
        {
            get { return m_charsetType; }
            set 
            {
                bool hasChanged = m_charsetType != value;
                m_charsetType = value;
                if (hasChanged) CreateSpriteFrames(); 
            }
        }

        [SerializeField]
        public int CurrentFrame
        {
            get
            {
                int curFrame = m_internalFrame;
                if (IsPingPongAnim && m_isPingPongReverse)
                {
                    curFrame = AnimFrames - 1 - curFrame;
                }
                return curFrame;
            }
        }

        [SerializeField]
		private List<Sprite> m_spriteFrames = new List<Sprite>();
        [SerializeField]
        private List<Sprite> m_spriteXpIdleFrames = new List<Sprite>();
        [SerializeField]
        private eCharSetType m_charsetType = eCharSetType.RPG_Maker_VX;
        [SerializeField]
        private eDir m_currentDir = eDir.DOWN;

        private int m_internalFrame; // frame counter used internally. CurFrame would be the real animation frame
        private float m_curFrameTime;               
        private bool m_isPingPongReverse = false;

		void Awake()
		{
            //if( IsDataBroken() ) //A weird bug happens when changing the spritesheet in the prefab. Sprites looks fine, the UVs looks broken in Down anima frame 2
                CreateSpriteFrames();
		}

		// Use this for initialization
		void Start () 
		{
            if (TargetSpriteRenderer == null)
            {
                TargetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
		}

        void Update()
        {
            UpdateAnim(Time.deltaTime);
        }

        public void UpdateAnim( float dt )
        {
            if (TargetSpriteRenderer == null || TargetSpriteRenderer.sprite == null)
                return;

            if (IsAnimated)
            {
                if (IsPingPongAnim && (m_internalFrame == 0 || m_internalFrame == (AnimFrames - 1)))
                    m_curFrameTime += dt * AnimSpeed * 2f; // avoid stay twice of the time in the first and last frame of the animation
                else
                    m_curFrameTime += dt * AnimSpeed;
                while (m_curFrameTime >= 1f)
                {             
                    m_curFrameTime -= 1f;
                    ++m_internalFrame; m_internalFrame %= AnimFrames;
                    if (m_internalFrame == 0)
                    {
                        m_isPingPongReverse = !m_isPingPongReverse;
                    }
                }
            }
            else
            {
                m_internalFrame = 0;
            }            

            TargetSpriteRenderer.sprite = GetCurrentSprite( CurrentDir );
        }

        public Sprite GetCurrentSprite( eDir dir )
        {
            if( !IsAnimated && m_charsetType == eCharSetType.RPG_Maker_XP )
                return m_spriteXpIdleFrames[(int)dir];
            else
                return m_spriteFrames[(int)((int)dir * AnimFrames + CurrentFrame)];
        }

        public IList<Sprite> GetSpriteFrames()
        {
            return m_spriteFrames;
        }

        public bool IsDataBroken()
        {
            return 
                (Pivot == null || Pivot.Length != 4 || 
                m_spriteFrames.Count != 4*AnimFrames || 
                (m_charsetType == eCharSetType.RPG_Maker_XP && m_spriteXpIdleFrames.Count != 4) ||
                m_spriteFrames.Contains(null) ||
                m_spriteXpIdleFrames.Contains(null)
            );
        }

		public bool CreateSpriteFrames()
		{
			if( SpriteCharSet != null )
			{
                if (Pivot == null || Pivot.Length < 4)
                {
                    Pivot = new Vector2[]
                    {
                        new Vector2( 0.5f, 0f ),
                        new Vector2( 0.5f, 0f ),
                        new Vector2( 0.5f, 0f ),
                        new Vector2( 0.5f, 0f )
                    };
                }
                else if( Pivot.Length != 4 )
                {
                    System.Array.Resize<Vector2>(ref Pivot, 4);
                }

                foreach (Sprite spr in m_spriteFrames) DestroyImmediate(spr);
                m_spriteFrames.Clear();
                m_spriteXpIdleFrames.Clear();

                int animFrames = ( CharsetType == eCharSetType.RPG_Maker_VX? AnimFrames : AnimFrames + 1); // XP characters have 4 frames per side, one is for idle
                int frameWidth = (int)SpriteCharSet.rect.width / animFrames;
                int frameHeight = (int)SpriteCharSet.rect.height / 4;
				int frameNb = 0;
				Rect rFrame = new Rect(0f, 0f, (float)frameWidth, (float)frameHeight);
                for (rFrame.y = SpriteCharSet.rect.yMax - frameHeight; rFrame.y >= SpriteCharSet.rect.y; rFrame.y -= frameHeight)
				{
                    for (rFrame.x = SpriteCharSet.rect.x; rFrame.x < SpriteCharSet.rect.xMax; rFrame.x += frameWidth, ++frameNb)
					{
                        try
                        {
                            Sprite sprFrame = Sprite.Create(SpriteCharSet.texture, rFrame, Pivot[frameNb / animFrames], 100f);
                            sprFrame.name = SpriteCharSet.name + "_" + frameNb;
                            if (CharsetType == eCharSetType.RPG_Maker_XP && (frameNb % animFrames == 0))
                                m_spriteXpIdleFrames.Add(sprFrame); // save apart the idle frame for each direction
                            else
                                m_spriteFrames.Add(sprFrame);
                        }
                        catch
                        { 
                            //NOTE: this happens when texture size is not multiple of AnimFrames. In this case, wrong frames are skipped
                            frameNb--;
                        }
					}
				}

                if (TargetSpriteRenderer != null)
				{
					TargetSpriteRenderer.sprite = GetCurrentSprite(CurrentDir);
				}
                return true;
			}
            return false;
		}
	}
}