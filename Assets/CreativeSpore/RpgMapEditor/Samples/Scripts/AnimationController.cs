using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace CreativeSpore
{
	public class AnimationController : MonoBehaviour 
	{

		public enum eAnimType
		{
			LOOP,
			ONESHOOT
		}
		
		public List<Sprite> SpriteFrames = new List<Sprite>();
		
		public eAnimType AnimType = eAnimType.LOOP;
		public bool IsAnimated = true;
		public float AnimSpeed = 0.2f;
		public bool IsDestroyedOnAnimEnd = false;
		public float CurrentFrame
		{
			get { return m_currAnimFrame; }
		}
		
		private float m_currAnimFrame = 0f;
		
		
		private SpriteRenderer m_spriteRenderer;
		
		// Use this for initialization
		void Start () 
		{
			m_spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}
		
		// Update is called once per frame
		void Update() 
		{
			if (IsAnimated)
			{
				m_currAnimFrame += AnimSpeed * Time.deltaTime;

				if( AnimType == eAnimType.LOOP )
				{
					while (m_currAnimFrame >= SpriteFrames.Count)
						m_currAnimFrame -= SpriteFrames.Count;
				}
				else if(m_currAnimFrame >= SpriteFrames.Count) 
				{
					if( IsDestroyedOnAnimEnd )
					{
						Destroy(transform.gameObject);
						return;
					}
					else
					{
						m_currAnimFrame = 0f;
						IsAnimated = false;
					}
				}
			}
			else
			{
				m_currAnimFrame = 0.9f;
			}
			
			m_spriteRenderer.sprite = SpriteFrames[(int)m_currAnimFrame];
		}
	}
}
