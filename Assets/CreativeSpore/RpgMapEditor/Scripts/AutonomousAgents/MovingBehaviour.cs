using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;
using System.Collections.Generic;
using CreativeSpore.PathFindingLib;

namespace CreativeSpore
{
    //ref: http://natureofcode.com/book/chapter-6-autonomous-agents/
	public class MovingBehaviour : MonoBehaviour {

		public Vector3 Veloc;
		public Vector3 Acc;
		public float Radious = 0.16f;
		public float MaxForce = 0.01f;
		public float MaxSpeed = 0.01f;

		// Use this for initialization
		void Start () 
		{
			
		}

		static void LimitVect( ref Vector3 vRef, float limit )
		{
			if (vRef.sqrMagnitude > limit * limit)
			{
				vRef = vRef.normalized * limit;
			}
		}
		
		// Update is called once per frame
		void Update () 
		{
			Veloc += Acc;
			Veloc.z = 0f;
			LimitVect( ref Veloc, MaxSpeed );
			transform.position += Veloc * Time.deltaTime;
			Acc = Vector3.zero;
		}

		public void ApplyForce( Vector3 vForce )
		{
			Acc += vForce;
		}

		public void Seek( Vector3 vTarget )
		{
			Vector3 vDir = vTarget - transform.position;
	//		float dist = vDir.magnitude;
			vDir.Normalize();
			vDir *= MaxSpeed;
			Vector3 vSteer = vDir - Veloc;
			LimitVect( ref vSteer, MaxForce );
			ApplyForce( vSteer );
		}

		public void Arrive( Vector3 vTarget )
		{
			Vector3 vDir = vTarget - transform.position;
			float dist = vDir.magnitude;
			vDir.Normalize();
			if (dist <= Radious)
			{
				vDir *= Mathf.Lerp(0f, 1f, dist/Radious);
			} 
			vDir *= MaxSpeed;
			Vector3 vSteer = vDir - Veloc;
			LimitVect( ref vSteer, MaxForce );
			ApplyForce( vSteer );
		}

        // This function implements Craig Reynolds' path following algorithm
        // http://www.red3d.com/cwr/steer/PathFollow.html
        /// <summary>
        /// Follow a path of positions
        /// </summary>
        /// <param name="pathList"></param>
        /// <param name="pathRadius"></param>
        public void Follow(List<Vector3> pathList, float pathRadius)
        {

            if( pathList.Count <= 1 )
            {
                return;
            }

            // Predict location 50 (arbitrary choice) frames ahead
            // This could be based on speed 
            Vector3 predict = Veloc;
            predict.Normalize();
            predict *= 0.3f;
            Vector3 predictLoc = transform.position + predict;
            Debug.DrawLine(transform.position, predictLoc, Color.blue);

            // Now we must find the normal to the path from the predicted location
            // We look at the normal for each line segment and pick out the closest one
            
            Vector3 target = Vector3.zero;
            float worldRecord = float.MaxValue;  // Start with a very high record distance that can easily be beaten

            // Loop through all points of the path
            for (int i = 0; i < pathList.Count - 1; i++)
            {

                // Look at a line segment
                Vector3 a = pathList[i];
                Vector3 b = pathList[i + 1];

                // Get the normal point to that line
                Vector3 normalPoint = GetNormalPoint(predictLoc, a, b);

                if (normalPoint.x < a.x || normalPoint.x > b.x || normalPoint.y < a.y || normalPoint.y > b.y)
                {
                    // This is something of a hacky solution, but if it's not within the line segment
                    // consider the normal to just be the end of the line segment (point b)
                    normalPoint = b;
                }

                // How far away are we from the path?
                float distance = Vector3.Distance(predictLoc, normalPoint);
                // Did we beat the record and find the closest line segment?
                if (distance < worldRecord)
                {
                    // If so the target we want to steer towards is the normal
                    worldRecord = distance;

                    // Look at the direction of the line segment so we can seek a little bit ahead of the normal
                    Vector3 dir = b - a;
                    dir.Normalize();
                    // This is an oversimplification
                    // Should be based on distance to path & velocity
                    dir *= 0.1f;
                    target = normalPoint;
                    target += dir;

                    //Debug.DrawLine(predictLoc, a, Color.green);
                    //Debug.DrawLine(predictLoc, b, Color.cyan);
                    Debug.DrawLine(predictLoc, normalPoint, Color.white);
                }
            }

            // Only if the distance is greater than the path's radius do we bother to steer
            if (worldRecord > pathRadius)
            {
                Seek(target);
                Debug.DrawLine(transform.position, target, Color.magenta);
            }
            else
            {
                Debug.DrawLine(transform.position, target, Color.grey);
            }
        }

        // A function to get the normal point from a point (p) to a line segment (a-b)
        // This function could be optimized to make fewer new Vector objects
        Vector3 GetNormalPoint(Vector3 p, Vector3 a, Vector3 b)
        {
            // Vector from a to p
            Vector3 ap = p - a;
            // Vector from a to b
            Vector3 ab = b - a;
            ab.Normalize(); // Normalize the line
            // Project vector "diff" onto line by using the dot product
            ab *= Vector3.Dot(ap, ab);
            Vector3 normalPoint = a + ab;
            return normalPoint;
        }
	}
}
