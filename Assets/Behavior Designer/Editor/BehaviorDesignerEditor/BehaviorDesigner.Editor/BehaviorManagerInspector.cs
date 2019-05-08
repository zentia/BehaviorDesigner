using BehaviorDesigner.Runtime;
using System;
using UnityEditor;
using UnityEngine;

namespace BehaviorDesigner.Editor
{
	[CustomEditor(typeof(BehaviorManager))]
	public class BehaviorManagerInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			BehaviorManager behaviorManager = this.target as BehaviorManager;
			behaviorManager.UpdateInterval = (UpdateIntervalType)EditorGUILayout.EnumPopup("Update Interval", behaviorManager.UpdateInterval, new GUILayoutOption[0]);
			if (behaviorManager.UpdateInterval == UpdateIntervalType.SpecifySeconds)
			{
				EditorGUI.indentLevel=(EditorGUI.indentLevel + 1);
				behaviorManager.UpdateIntervalSeconds = EditorGUILayout.FloatField("Seconds", behaviorManager.UpdateIntervalSeconds, new GUILayoutOption[0]);
				EditorGUI.indentLevel=(EditorGUI.indentLevel - 1);
			}
			behaviorManager.ExecutionsPerTick = (BehaviorManager.ExecutionsPerTickType)EditorGUILayout.EnumPopup("Task Execution Type", behaviorManager.ExecutionsPerTick, new GUILayoutOption[0]);
			if (behaviorManager.ExecutionsPerTick == BehaviorManager.ExecutionsPerTickType.Count)
			{
				EditorGUI.indentLevel=(EditorGUI.indentLevel + 1);
				behaviorManager.MaxTaskExecutionsPerTick = EditorGUILayout.IntField("Max Execution Count", behaviorManager.MaxTaskExecutionsPerTick, new GUILayoutOption[0]);
				EditorGUI.indentLevel=(EditorGUI.indentLevel - 1);
			}
		}
	}
}
