using UnityEngine;
using System.Collections;

namespace CreativeSpore
{
	public class GameFactory : MonoBehaviour
	{
		public static BulletController CreateBullet( GameObject caller, GameObject bulletPrefab, Vector3 vPos, Vector3 vDir, float speed, float dmgQty = 0.5f )
		{
			GameObject bullet = Instantiate(bulletPrefab, vPos, bulletPrefab.transform.rotation) as GameObject;
			
			// set friendly tag, to avoid collision with this layers
			bullet.layer = caller.layer;
			
			BulletController bulletCtrl = bullet.GetComponent<BulletController>();		
			bulletCtrl.Dir = vDir;
			bulletCtrl.Speed = speed;
            bulletCtrl.DamageQty = dmgQty;
			
			return bulletCtrl;
		}

		public static AnimationController CreateAnimation( GameObject prefab, Vector3 vPos )
		{
			GameObject obj = Instantiate(prefab, vPos, prefab.transform.rotation) as GameObject;		
			AnimationController ctrl = obj.GetComponent<AnimationController>();			
			return ctrl;
		}
	}
}