namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(ExternalBehavior))]
    public class ExternalBehaviorInspector : Editor
    {
        private bool mShowVariables;
        private static int selectedVariableIndex = -1;
        private static string selectedVariableName;
        private static int selectedVariableTypeIndex;
        private static List<float> variablePosition;

        public static bool DrawInspectorGUI(BehaviorSource behaviorSource, bool fromInspector, ref bool showVariables)
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(120f) };
            EditorGUILayout.LabelField("Behavior Name", options);
            behaviorSource.behaviorName = EditorGUILayout.TextField(behaviorSource.behaviorName, new GUILayoutOption[0]);
            if (fromInspector && GUILayout.Button("Open", new GUILayoutOption[0]))
            {
                BehaviorDesignerWindow.ShowWindow();
                BehaviorDesignerWindow.instance.LoadBehavior(behaviorSource, false, true);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Behavior Description", new GUILayoutOption[0]);
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Height(48f) };
            behaviorSource.behaviorDescription = EditorGUILayout.TextArea(behaviorSource.behaviorDescription, optionArray2);
            if (fromInspector && (showVariables = EditorGUILayout.Foldout(showVariables, "Variables")))
            {
                List<SharedVariable> allVariables = behaviorSource.GetAllVariables();
                if ((allVariables != null) && VariableInspector.DrawAllVariables(false, behaviorSource, ref allVariables, false, ref variablePosition, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex, true, false))
                {
                    if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
                    {
                        BinarySerialization.Save(behaviorSource);
                    }
                    else
                    {
                        SerializeJSON.Save(behaviorSource);
                    }
                }
            }
            return EditorGUI.EndChangeCheck();
        }

        public override void OnInspectorGUI()
        {
            ExternalBehavior target = this.target as ExternalBehavior;
            if ((target != null) && DrawInspectorGUI(target.BehaviorSource, true, ref this.mShowVariables))
            {
                EditorUtility.SetDirty(target);
            }
        }

        public void Reset()
        {
            ExternalBehavior target = this.target as ExternalBehavior;
            if ((target != null) && (target.BehaviorSource.Owner == null))
            {
                target.BehaviorSource.Owner = target;
            }
        }
    }
}

