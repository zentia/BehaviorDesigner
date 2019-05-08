namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class GizmoManager
    {
        private static string currentScene = EditorApplication.currentScene;

        static GizmoManager()
        {
            EditorApplication.hierarchyWindowChanged = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.hierarchyWindowChanged, new EditorApplication.CallbackFunction(GizmoManager.HierarchyChange));
            if (!Application.isPlaying)
            {
                UpdateAllGizmos();
                EditorApplication.playmodeStateChanged = (EditorApplication.CallbackFunction) Delegate.Combine(EditorApplication.playmodeStateChanged, new EditorApplication.CallbackFunction(GizmoManager.UpdateAllGizmos));
            }
        }

        public static void HierarchyChange()
        {
            BehaviorManager instance = BehaviorManager.instance;
            if (Application.isPlaying)
            {
                if (instance != null)
                {
                    instance.onEnableBehavior = new BehaviorManager.BehaviorManagerHandler(GizmoManager.UpdateBehaviorManagerGizmos);
                }
            }
            else if (currentScene != EditorApplication.currentScene)
            {
                currentScene = EditorApplication.currentScene;
                UpdateAllGizmos();
            }
        }

        public static void UpdateAllGizmos()
        {
            Behavior[] behaviorArray = UnityEngine.Object.FindObjectsOfType<Behavior>();
            for (int i = 0; i < behaviorArray.Length; i++)
            {
                UpdateGizmo(behaviorArray[i]);
            }
        }

        private static void UpdateBehaviorManagerGizmos()
        {
            BehaviorManager instance = BehaviorManager.instance;
            if (instance != null)
            {
                for (int i = 0; i < instance.BehaviorTrees.Count; i++)
                {
                    UpdateGizmo(instance.BehaviorTrees[i].behavior);
                }
            }
        }

        public static void UpdateGizmo(Behavior behavior)
        {
            behavior.gizmoViewMode = (Behavior.GizmoViewMode) BehaviorDesignerPreferences.GetInt(BDPreferences.GizmosViewMode);
            behavior.showBehaviorDesignerGizmo = BehaviorDesignerPreferences.GetBool(BDPreferences.ShowSceneIcon);
        }
    }
}

