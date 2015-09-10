using UnityEngine;
using System.Collections;
using CreativeSpore.RpgMapEditor;
using System.Collections.Generic;
using CreativeSpore.PathFindingLib;

namespace CreativeSpore.RpgMapEditor
{
    [RequireComponent(typeof(MovingBehaviour))]
    public class MapPathFindingBehaviour : MonoBehaviour
    {
        public Vector2 TargetPos;

        public delegate void OnComputedPathDelegate(MapPathFindingBehaviour source);
        public OnComputedPathDelegate OnComputedPath;

        public LinkedList<IPathNode> Path { get; private set; }
        public float MinDistToMoveNextTarget = 0.02f;

        MovingBehaviour m_movingBehavior;

        // Use this for initialization
        protected void Start()
        {
            m_movingBehavior = GetComponent<MovingBehaviour>();
            Path = new LinkedList<IPathNode>();
        }

        protected MapPathFinding m_mapPathFinding = new MapPathFinding();
        int m_startTileIdx;
        int m_endTileIdx;
        bool m_isUpdatePath = false;
        bool m_isComputing = false;
        LinkedListNode<IPathNode> m_curNode = null;

        //float now;

        float GetDistToTarget( AutoTileMap.AutoTile targetTile )
        {
            Vector3 target = RpgMapHelper.GetTileCenterPosition(targetTile.TileX, targetTile.TileY);
            target.z = transform.position.z;
            return (transform.position - target).magnitude;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_curNode != null)
            {                
                MapTileNode curTileNode = m_curNode.Value as MapTileNode;
                AutoTileMap.AutoTile autoTile = RpgMapHelper.GetAutoTileByPosition(transform.position, 0);
                if (
                    (autoTile.TileX != curTileNode.TileX || autoTile.TileY != curTileNode.TileY) ||
                    GetDistToTarget(autoTile) > MinDistToMoveNextTarget // wait until min dist is reached
                )
                {
                    Vector3 vSeek = curTileNode.Position; vSeek.z = transform.position.z; // put at this object level
                    if (m_movingBehavior)
                    {
                        if (m_curNode.Next != null)
                            m_movingBehavior.Seek(vSeek);
                        else
                            m_movingBehavior.Arrive(vSeek);
                    }
                }
                else
                {
                    m_curNode = m_curNode.Next;
                }
            }

            //if (TargetPos != null) //TODO: TargetPos can't be null
            {
                int prevTileidx = m_startTileIdx;
                m_startTileIdx = RpgMapHelper.GetTileIdxByPosition(TargetPos);
                m_isUpdatePath |= prevTileidx != m_startTileIdx;
                if (m_isUpdatePath || !m_isComputing)
                {
                    m_isUpdatePath = false;
                    m_endTileIdx = RpgMapHelper.GetTileIdxByPosition(transform.position);
                    //now = Time.realtimeSinceStartup;
                    StopCoroutine("ComputePath");
                    StartCoroutine("ComputePath");
                }
            }

            //+++ Debug
            for (LinkedListNode<IPathNode> it = Path.First; it != null; it = it.Next)
            {
                MapTileNode mapTileNode0 = it.Value as MapTileNode;
                if (it.Next != null)
                {
                    MapTileNode mapTileNode1 = it.Next.Value as MapTileNode;
                    Vector3 v0 = mapTileNode0.Position;
                    Vector3 v1 = mapTileNode1.Position;
                    v0.z = transform.position.z;
                    v1.z = transform.position.z;
                    Debug.DrawLine(v0, v1, Color.red);
                }
            }
            //---
        }

        IEnumerator ComputePath()
        {
            m_isComputing = true;
            IEnumerator coroutine = m_mapPathFinding.GetRouteFromToAsync(m_startTileIdx, m_endTileIdx);
            while (coroutine.MoveNext()) yield return null;
            //Debug.Log("GetRouteFromToAsync execution time(ms): " + (Time.realtimeSinceStartup - now) * 1000);
            PathFinding.FindingParams findingParams = (PathFinding.FindingParams)coroutine.Current;
            Path = findingParams.computedPath;
            //+++find closest node and take next one if possible
            m_curNode = Path.First;
            if( m_curNode != null )
            {
                Vector3 vPos = transform.position; vPos.z = ((MapTileNode)m_curNode.Value).Position.z; // use same z level                
                while (m_curNode != null && m_curNode.Next != null)
                {                    
                    MapTileNode n0 = m_curNode.Value as MapTileNode;
                    MapTileNode n1 = m_curNode.Next.Value as MapTileNode;
                    float distSqr = (vPos - n0.Position).sqrMagnitude;
                    float distSqr2 = (vPos - n1.Position).sqrMagnitude;
                    if (distSqr2 < distSqr) 
                        m_curNode = m_curNode.Next;
                    else 
                        break;
                }
                // take next one, avoid moving backward in the path
                if (m_curNode.Next != null)
                    m_curNode = m_curNode.Next;
            }
            //---
            m_isComputing = false;
            if (OnComputedPath != null)
            {
                OnComputedPath(this);
            }
            yield return null;
        }
    }
}