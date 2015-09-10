using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore
{
    [CustomEditor(typeof(PhysicCharBehaviour))]
    public class PhysicCharBehaviourEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            PhysicCharBehaviour myTarget = (PhysicCharBehaviour)target;

            float fHandleSize = 0.04f * HandleUtility.GetHandleSize(myTarget.transform.position);

            Vector3[] verts = new Vector3[]
            {   //NOTE: GUI Y axis has opposite direction, so y sign is changed
                new Vector3( myTarget.CollRect.xMin, myTarget.CollRect.yMin, 0 ) + myTarget.transform.position,
                new Vector3( myTarget.CollRect.xMax, myTarget.CollRect.yMin, 0 ) + myTarget.transform.position,
                new Vector3( myTarget.CollRect.xMax, myTarget.CollRect.yMax, 0 ) + myTarget.transform.position,
                new Vector3( myTarget.CollRect.xMin, myTarget.CollRect.yMax, 0 ) + myTarget.transform.position,
            };
            Handles.DrawSolidRectangleWithOutline(verts, new Color(0, 0, 0, 0), new Color(0f, 0.4f, 1f));            

            Vector3[] handles = new Vector3[ verts.Length ];
            for (int i = 0; i < verts.Length; ++i)
            {
                handles[i] = (verts[i] + verts[(i + 1) % verts.Length]) / 2;
                Handles.color = Color.cyan;
                handles[i] = Handles.FreeMoveHandle(handles[i], Quaternion.identity, fHandleSize, Vector3.zero, Handles.DotCap);
                //handles[i].y = -handles[i].y; // restore y sign
                handles[i] -= myTarget.transform.position;
            }

            myTarget.CollRect.yMin = handles[0].y;
            myTarget.CollRect.xMax = handles[1].x;
            myTarget.CollRect.yMax = handles[2].y;
            myTarget.CollRect.xMin = handles[3].x;

            Vector3 vCenter = new Vector3(myTarget.CollRect.center.x, myTarget.CollRect.center.y);
            vCenter = Handles.FreeMoveHandle(vCenter + myTarget.transform.position, Quaternion.identity, fHandleSize, Vector3.zero, Handles.DotCap);
            vCenter -= myTarget.transform.position;
            myTarget.CollRect.center = new Vector2( vCenter.x, vCenter.y );

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }
    }
}