using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace CreativeSpore.RpgMapEditor
{
	[ExecuteInEditMode]
	public class AutoTileMapEditorBehaviour : MonoBehaviour 
	{
		AutoTileMap MyAutoTileMap;

	#if UNITY_EDITOR

		void Awake()
		{
			//hideFlags = HideFlags.HideInInspector;
			MyAutoTileMap = GetComponent<AutoTileMap>();

			OnRenderObject();
		}
		
		void OnRenderObject() 
		{
			if( MyAutoTileMap == null )
			{
				MyAutoTileMap = GetComponent<AutoTileMap>();
			}

			if( MyAutoTileMap != null )
			{
				if( !MyAutoTileMap.IsInitialized )
				{
					if( MyAutoTileMap.CanBeInitialized() )
					{
						MyAutoTileMap.LoadMap();
					}
				}
			}
		}
	#endif
	}
}