using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

namespace CreativeSpore.RpgMapEditor
{
	/// <summary>
	/// ScriptableObject containing map data
	/// </summary>
	public class AutoTileMapData : ScriptableObject 
	{
		public AutoTileMapSerializeData Data;
	}
}
