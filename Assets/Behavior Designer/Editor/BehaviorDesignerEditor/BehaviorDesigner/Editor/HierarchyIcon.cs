namespace BehaviorDesigner.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class HierarchyIcon : ScriptableObject
    {
        private static Texture2D icon = (AssetDatabase.LoadAssetAtPath("Assets/Gizmos/Behavior Designer Hier Icon.png", typeof(Texture2D)) as Texture2D);

        static HierarchyIcon()
        {
            if (icon != null)
            {
                EditorApplication.hierarchyWindowItemOnGUI = (EditorApplication.HierarchyWindowItemCallback) Delegate.Combine(EditorApplication.hierarchyWindowItemOnGUI, new EditorApplication.HierarchyWindowItemCallback(HierarchyIcon.HierarchyWindowItemOnGUI));
            }
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.ShowHierarchyIcon))
            {
                GameObject obj2 = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if ((obj2 != null) && (obj2.GetComponent<Behavior>() != null))
                {
                    Rect rect;
                    rect = new Rect(selectionRect) {
                        x = rect.width + (selectionRect.x - 16f),
                        width = 16f,
                        height = 16f
                    };
                    GUI.DrawTexture(rect, icon);
                }
            }
        }
    }
}

