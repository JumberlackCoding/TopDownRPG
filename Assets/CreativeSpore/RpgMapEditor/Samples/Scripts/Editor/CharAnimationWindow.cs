using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore
{
    public class CharAnimationWindow : EditorWindow
    {

        enum eCharAnimEditMode
        {
            EditPivot,
            Row,
            Column,
            MoveInCircles,
        }

        CharAnimationController m_animCtrl;
        GameObject m_prevSelectedGameObject = null;
        bool m_showGrid = false;
        Color m_gridColor0 = Color.grey;
        Color m_gridColor1 = new Color32(180, 180, 180, 255);
        float m_gridSize = 10;
        Texture2D s_gridTex;
        eCharAnimEditMode m_viewMode = eCharAnimEditMode.MoveInCircles;
        float m_walkSpeed = 1f;
        float m_walkAnimTimer = 0f;
        float m_prevDt = Time.realtimeSinceStartup;
        float m_zoom = 1f;
        Vector2 m_vTrans = Vector2.zero;
        Rect m_rGridView;
        Vector2 m_mousePos;

        public static void Init( CharAnimationController animCtrl )
        {
            // Get existing open window or if none, make a new one:
            CharAnimationWindow window = (CharAnimationWindow)EditorWindow.GetWindow(typeof(CharAnimationWindow));
            if (animCtrl.CreateSpriteFrames())
            {
                window.m_gridSize = animCtrl.GetSpriteFrames()[0].rect.width / 4;
            }
            window.m_animCtrl = animCtrl;
            window.Show();
        }

        void OnGUI()
        {
            // Change m_animCtrl if another object is selected with this component attached
            if (m_prevSelectedGameObject != Selection.activeGameObject)
            {
                CharAnimationController animCtrl = Selection.activeGameObject != null? Selection.activeGameObject.GetComponent<CharAnimationController>() : null;
                if( animCtrl != null )
                {
                    m_animCtrl = animCtrl;
                    if (animCtrl.CreateSpriteFrames())
                    {
                        m_gridSize = animCtrl.GetSpriteFrames()[0].rect.width / 4;
                    }
                }
            }
            m_prevSelectedGameObject = Selection.activeGameObject;

            if (m_animCtrl == null)
            {
                Close();
                return;
            }

            if (m_animCtrl.IsDataBroken()) m_animCtrl.CreateSpriteFrames();

            if (s_gridTex == null)
            {
                s_gridTex = new Texture2D(2, 2);
                s_gridTex.wrapMode = TextureWrapMode.Repeat;
                s_gridTex.filterMode = FilterMode.Point;
                s_gridTex.SetPixels32(new Color32[] { m_gridColor0, m_gridColor1, m_gridColor1, m_gridColor0 });
                s_gridTex.Apply();
            }

            Repaint();

            if( Event.current.type == EventType.Repaint)
            {
                float timeDt = Time.realtimeSinceStartup - m_prevDt;
                m_prevDt = Time.realtimeSinceStartup;
    
                if (!Application.isPlaying)
                    m_animCtrl.UpdateAnim(timeDt);

                m_walkAnimTimer -= timeDt * m_walkSpeed;
                while (m_walkAnimTimer <= 0f) m_walkAnimTimer += 1f;
                m_walkAnimTimer %= 1;
            }
            else if( Event.current.type == EventType.scrollWheel && m_rGridView.Contains(m_mousePos) )
            {
                float prevZoom = m_zoom;
                if (Event.current.delta.y > 0) // back
                {
                    if (m_zoom > 1f)
                        m_zoom = Mathf.Max(m_zoom - 1, 1);
                    else
                        m_zoom = Mathf.Max(m_zoom / 2f, 0.05f);
                }
                else if (Event.current.delta.y < 0) // forward
                {
                    if (m_zoom >= 1f)
                        m_zoom = Mathf.Min(m_zoom + 1, 10);
                    else
                        m_zoom *= 2f;
                }
                m_zoom = Mathf.Clamp(m_zoom, 1f, 5f);

                if (prevZoom != m_zoom)
                {
                    Vector2 vZoomCenter = (m_mousePos - m_rGridView.position);
                    m_vTrans += (prevZoom - m_zoom) * vZoomCenter / (prevZoom * m_zoom);
                }
            }
            else if (Event.current.type == EventType.MouseDrag && m_rGridView.Contains(m_mousePos))
            {
                m_vTrans += Event.current.delta / m_zoom;
            }

            m_mousePos = Event.current.mousePosition;

            DrawGUI();

            if( GUI.changed )
            {
                EditorUtility.SetDirty(m_animCtrl);
            }
        }        

        void DrawGUI()
        {
            GUILayout.Label("Character Animation", EditorStyles.boldLabel);

            //*** TODO:
            // - Zoom with mouse wheel or enum 2x, 4x, 8x
            // - Anim frames
            // - Set Pivot or sprite offset
            //

            EditorGUI.BeginChangeCheck();
            m_animCtrl.SpriteCharSet = (Sprite)EditorGUILayout.ObjectField("CharacterSet", m_animCtrl.SpriteCharSet, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                m_animCtrl.CreateSpriteFrames();
            }

            if (m_animCtrl.SpriteCharSet == null)
            {
                return;
            }

            GUIStyle styleFoldout = new GUIStyle(EditorStyles.foldout);
            styleFoldout.fontStyle = FontStyle.Bold;

            m_animCtrl.CharsetType = (CharAnimationController.eCharSetType)EditorGUILayout.EnumPopup("Charset Type", m_animCtrl.CharsetType);
            m_animCtrl.AnimSpeed = EditorGUILayout.FloatField("Anim Speed", m_animCtrl.AnimSpeed);
            m_animCtrl.IsPingPongAnim = EditorGUILayout.ToggleLeft("Ping-Pong Anim", m_animCtrl.IsPingPongAnim);

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            EditorGUI.BeginChangeCheck();
            m_viewMode = (eCharAnimEditMode)EditorGUILayout.EnumPopup("View Mode", m_viewMode);
            if(EditorGUI.EndChangeCheck())
            {
                m_vTrans = Vector2.zero;
                m_zoom = 1f;
            }

            EditorGUI.indentLevel += 1;
            if (m_viewMode == eCharAnimEditMode.EditPivot)
            {
                m_animCtrl.CurrentDir = (CharAnimationController.eDir)EditorGUILayout.EnumPopup("View Direction", m_animCtrl.CurrentDir);
                EditorGUI.BeginChangeCheck();
                m_animCtrl.Pivot[(int)m_animCtrl.CurrentDir] = EditorGUILayout.Vector2Field("Sprite Pivot (" + m_animCtrl.CurrentDir + ")", m_animCtrl.Pivot[(int)m_animCtrl.CurrentDir]);
                if (EditorGUI.EndChangeCheck())
                {
                    m_animCtrl.CreateSpriteFrames();
                }
            }
            else if (m_viewMode == eCharAnimEditMode.MoveInCircles)
            {
                m_walkSpeed = EditorGUILayout.FloatField("Walk Speed", m_walkSpeed);
            }
            EditorGUI.indentLevel -= 1;

            GUILayout.Label("View Zoom:" + m_zoom);
            m_showGrid = EditorGUILayout.Foldout(m_showGrid, "Grid Settings:", styleFoldout);
            if (m_showGrid)
            {
                m_gridColor0 = EditorGUILayout.ColorField("Grid Color0", m_gridColor0);
                m_gridColor1 = EditorGUILayout.ColorField("Grid Color1", m_gridColor1);
                m_gridSize = EditorGUILayout.FloatField("Grid Size", m_gridSize);
            }

            // +++ Draw Animation Grid +++ ///

            Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.BeginGroup(r, s_gridTex);

            if( Event.current.type == EventType.Repaint)
            {
                m_rGridView = r;
            }
            r.position = Vector2.zero;

            // Draw grid
            Rect rGrid = r;
            rGrid.position *= m_zoom;
            rGrid.width *= m_zoom;
            rGrid.height *= m_zoom;
            GUI.DrawTextureWithTexCoords(rGrid, s_gridTex, new Rect(0, 0, r.width / (m_gridSize * s_gridTex.width), r.height / (m_gridSize * s_gridTex.height)));

            float fWalkDistX = 2 * m_animCtrl.GetSpriteFrames()[0].rect.width;
            float fWalkDistY = 2 * m_animCtrl.GetSpriteFrames()[0].rect.height;
            float fWalkProg = (1 - m_walkAnimTimer);
            foreach (CharAnimationController.eDir dir in System.Enum.GetValues(typeof(CharAnimationController.eDir)))
            {
                Sprite sprFrame = m_animCtrl.GetCurrentSprite(dir);
                Rect r2 = new Rect();
                if (m_viewMode == eCharAnimEditMode.EditPivot)
                {
                    if (m_animCtrl.CurrentDir != dir) continue;
                    r2 = new Rect(r.x, r.y, sprFrame.rect.width, sprFrame.rect.height);
                }
                else if (m_viewMode == eCharAnimEditMode.Row)
                {
                    r2 = new Rect(r.x + (int)dir * sprFrame.rect.width * 1.2f, r.y, sprFrame.rect.width, sprFrame.rect.height);
                }
                else if (m_viewMode == eCharAnimEditMode.Column)
                {
                    r2 = new Rect(r.x, r.y + (int)dir * sprFrame.rect.height * 1.2f, sprFrame.rect.width, sprFrame.rect.height);
                }
                else if (m_viewMode == eCharAnimEditMode.MoveInCircles)
                {
                    switch (dir)
                    {
                        case CharAnimationController.eDir.DOWN: r2 = new Rect(r.x, r.y + fWalkProg * fWalkDistY, sprFrame.rect.width, sprFrame.rect.height); break;
                        case CharAnimationController.eDir.LEFT: r2 = new Rect(r.x + (1 - fWalkProg) * fWalkDistX, r.y, sprFrame.rect.width, sprFrame.rect.height); break;
                        case CharAnimationController.eDir.RIGHT: r2 = new Rect(r.x + fWalkProg * fWalkDistX, r.y + fWalkDistY, sprFrame.rect.width, sprFrame.rect.height); break;
                        case CharAnimationController.eDir.UP: r2 = new Rect(r.x + fWalkDistX, r.y + (1f - fWalkProg) * fWalkDistY, sprFrame.rect.width, sprFrame.rect.height); break;
                    }
                }

                DrawSprite(r2, sprFrame);

                // draw pivot
                if (m_viewMode == eCharAnimEditMode.EditPivot)
                {
                    Vector2 pivot = m_animCtrl.Pivot[(int)m_animCtrl.CurrentDir];
                    Vector2 vPivotPos = r2.position + new Vector2(pivot.x * r2.width, r2.height - pivot.y * r2.height);
                    DrawLine(new Vector2(vPivotPos.x, r2.yMin), new Vector2(vPivotPos.x, r2.yMax), Color.red);
                    DrawLine(new Vector2(r2.xMin, vPivotPos.y), new Vector2(r2.xMax, vPivotPos.y), Color.green);
                }
            }
            GUI.matrix = Matrix4x4.identity;

            GUI.EndGroup();
            EditorGUILayout.HelpBox("Use mouse scroll wheel to change the View Zoom level\nDrag mouse over grid to drag sprites", MessageType.Info);
        }

        void DrawLine( Vector2 l0, Vector2 l1, Color c )
        {
            l0 += m_vTrans;
            l0 *= m_zoom;
            l1 += m_vTrans;
            l1 *= m_zoom;
            Handles.color = c;
            Handles.DrawPolyLine(l0, l1);
        }

        void DrawSprite(Rect r, Sprite sprite)
        {
            Rect texCoords = new Rect(
                //NOTE: sprite.texture.texelSize is multiplied to allow textures with mipmaps
                sprite.rect.x * sprite.texture.texelSize.x,
                sprite.rect.y * sprite.texture.texelSize.y,
                sprite.rect.width * sprite.texture.texelSize.x,
                sprite.rect.height * sprite.texture.texelSize.y
                );

            r.position += m_vTrans;
            r.position *= m_zoom;
            r.width *= m_zoom;
            r.height *= m_zoom;
            GUI.DrawTextureWithTexCoords(r, sprite.texture, texCoords);
        }       
    }
}
