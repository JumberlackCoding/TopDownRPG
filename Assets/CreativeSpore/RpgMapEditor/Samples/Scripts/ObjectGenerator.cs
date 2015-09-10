using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore
{
	public class ObjectGenerator : MonoBehaviour 
	{
		public GameObject Prefab;
		public float TimerBetweenCreation = 1f;
		public int MaxNumberOfEntities = -1; // < 0 for infinite
		public Vector3 Offset = default(Vector3);

		[SerializeField]
		private List<GameObject> m_listOfEntities = new List<GameObject>();

		void Start()
		{
			StartCoroutine(GenerateObj());
		}
		
		IEnumerator GenerateObj()
		{
			while(true)
			{
				m_listOfEntities.RemoveAll(item => item == null);
				if( MaxNumberOfEntities < 0 || m_listOfEntities.Count < MaxNumberOfEntities )
				{
					GameObject obj = Instantiate(Prefab, transform.position+Offset, Prefab.transform.rotation) as GameObject;
					m_listOfEntities.Add( obj );
				}
				yield return new WaitForSeconds(TimerBetweenCreation);
			}
		}
	}
}
