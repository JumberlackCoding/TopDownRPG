using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore
{
    [CustomEditor(typeof(CharAnimationController))]
    public class CharAnimationControllerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            CharAnimationController targetComp = (CharAnimationController)target;
            if (GUILayout.Button("Open Editor..."))
            {
                CharAnimationWindow.Init(targetComp);
            }

            if (targetComp.IsDataBroken()) targetComp.CreateSpriteFrames();

            EditorGUI.BeginChangeCheck();
            targetComp.SpriteCharSet = (Sprite)EditorGUILayout.ObjectField("SpriteCharSet", targetComp.SpriteCharSet, typeof(Sprite), false);
            targetComp.CharsetType = (CharAnimationController.eCharSetType)EditorGUILayout.EnumPopup("Charset Type", targetComp.CharsetType);
            if( EditorGUI.EndChangeCheck() )
            {
                targetComp.CreateSpriteFrames();
            }

            targetComp.TargetSpriteRenderer = (SpriteRenderer)EditorGUILayout.ObjectField("Target Sprite Render", targetComp.TargetSpriteRenderer, typeof(SpriteRenderer), true);
            targetComp.AnimSpeed = EditorGUILayout.FloatField("Anim Speed", targetComp.AnimSpeed);
            targetComp.IsPingPongAnim = EditorGUILayout.ToggleLeft("Ping-Pong Anim", targetComp.IsPingPongAnim);
            targetComp.CurrentDir = (CharAnimationController.eDir)EditorGUILayout.EnumPopup("Facing Dir", targetComp.CurrentDir);

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetComp);
            }
        }
    }
}
