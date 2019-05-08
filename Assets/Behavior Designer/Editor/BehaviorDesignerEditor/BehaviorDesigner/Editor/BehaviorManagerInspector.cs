namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using UnityEditor;

    [CustomEditor(typeof(BehaviorManager))]
    public class BehaviorManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            BehaviorManager target = this.target as BehaviorManager;
            target.UpdateInterval = (UpdateIntervalType) EditorGUILayout.EnumPopup("Update Interval", target.UpdateInterval, new GUILayoutOption[0]);
            if (target.UpdateInterval == UpdateIntervalType.SpecifySeconds)
            {
                EditorGUI.indentLevel++;
                target.UpdateIntervalSeconds = EditorGUILayout.FloatField("Seconds", target.UpdateIntervalSeconds, new GUILayoutOption[0]);
                EditorGUI.indentLevel--;
            }
            target.ExecutionsPerTick = (BehaviorManager.ExecutionsPerTickType) EditorGUILayout.EnumPopup("Task Execution Type", target.ExecutionsPerTick, new GUILayoutOption[0]);
            if (target.ExecutionsPerTick == BehaviorManager.ExecutionsPerTickType.Count)
            {
                EditorGUI.indentLevel++;
                target.MaxTaskExecutionsPerTick = EditorGUILayout.IntField("Max Execution Count", target.MaxTaskExecutionsPerTick, new GUILayoutOption[0]);
                EditorGUI.indentLevel--;
            }
        }
    }
}

