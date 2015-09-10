using UnityEngine;
using System.Collections;

namespace CreativeSpore
{
	public class DamageData 
	{

		public enum eDmgType
		{
			NORMAL, // general damage
			SHIELD // applied to shield
		}

		public eDmgType Type = eDmgType.NORMAL;
		public float 	Quantity	= 0.0f;
		public Vector2	Force		= Vector2.zero;

		public void ApplyDamage( GameObject _obj )
		{
			DamageBehaviour damageBehaviour = _obj.GetComponent<DamageBehaviour>();
			if (damageBehaviour)
			{
				damageBehaviour.ApplyDamage(this);
			}
		}

		public static void ApplyDamage( GameObject _obj, float _quantity, Vector2 _force = default(Vector2), eDmgType _type = eDmgType.NORMAL )
		{
			DamageData damageData = new DamageData(){ Quantity = _quantity, Force = _force, Type = _type };
			damageData.ApplyDamage( _obj );
		}
	}
}
