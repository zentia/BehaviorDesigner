namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(Behavior))]
    public class BehaviorInspector : Editor
    {
        private bool mShowOptions = true;
        private bool mShowVariables;
        private static int selectedVariableIndex = -1;
        private static string selectedVariableName;
        private static int selectedVariableTypeIndex;
        private static List<float> variablePosition;

        public static bool DrawInspectorGUI(Behavior behavior, SerializedObject serializedObject, bool fromInspector, ref bool externalModification, ref bool showOptions, ref bool showVariables)
        {
            bool flag3;
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(120f) };
            EditorGUILayout.LabelField("Behavior Name", options);
            behavior.GetBehaviorSource().behaviorName = EditorGUILayout.TextField(behavior.GetBehaviorSource().behaviorName, new GUILayoutOption[0]);
            if (fromInspector && GUILayout.Button("Open", new GUILayoutOption[0]))
            {
                BehaviorDesignerWindow.ShowWindow();
                BehaviorDesignerWindow.instance.LoadBehavior(behavior.GetBehaviorSource(), false, true);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Behavior Description", new GUILayoutOption[0]);
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Height(48f) };
            behavior.GetBehaviorSource().behaviorDescription = EditorGUILayout.TextArea(behavior.GetBehaviorSource().behaviorDescription, BehaviorDesignerUtility.TaskInspectorCommentGUIStyle, optionArray2);
            serializedObject.Update();
            GUI.enabled = (PrefabUtility.GetPrefabType(behavior) != PrefabType.PrefabInstance) || BehaviorDesignerPreferences.GetBool(BDPreferences.EditablePrefabInstances);
            SerializedProperty property = serializedObject.FindProperty("externalBehavior");
            ExternalBehavior objectReferenceValue = property.objectReferenceValue as ExternalBehavior;
            EditorGUILayout.PropertyField(property, true, new GUILayoutOption[0]);
            serializedObject.ApplyModifiedProperties();
            if ((!object.ReferenceEquals(behavior.ExternalBehavior, null) && !behavior.ExternalBehavior.Equals(objectReferenceValue)) || (!object.ReferenceEquals(objectReferenceValue, null) && !objectReferenceValue.Equals(behavior.ExternalBehavior)))
            {
                if (!object.ReferenceEquals(behavior.ExternalBehavior, null))
                {
                    behavior.ExternalBehavior.BehaviorSource.Owner = behavior.ExternalBehavior;
                    behavior.ExternalBehavior.BehaviorSource.CheckForSerialization(true, behavior.GetBehaviorSource());
                }
                else
                {
                    behavior.GetBehaviorSource().EntryTask = null;
                    behavior.GetBehaviorSource().RootTask = null;
                    behavior.GetBehaviorSource().DetachedTasks = null;
                    behavior.GetBehaviorSource().Variables = null;
                    behavior.GetBehaviorSource().CheckForSerialization(true, null);
                    behavior.GetBehaviorSource().Variables = null;
                    if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
                    {
                        BinarySerialization.Save(behavior.GetBehaviorSource());
                    }
                    else
                    {
                        SerializeJSON.Save(behavior.GetBehaviorSource());
                    }
                }
                externalModification = true;
            }
            GUI.enabled = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("group"), true, new GUILayoutOption[0]);
            if (fromInspector)
            {
                showVariables = flag3 = EditorGUILayout.Foldout(showVariables, "Variables");
                if (flag3)
                {
                    EditorGUI.indentLevel++;
                    List<SharedVariable> allVariables = behavior.GetAllVariables();
                    BehaviorSource behaviorSource = behavior.GetBehaviorSource();
                    bool flag = false;
                    if (!Application.isPlaying && (behavior.ExternalBehavior != null))
                    {
                        behaviorSource.CheckForSerialization(true, null);
                        flag = true;
                    }
                    bool flag2 = false;
                    if (VariableInspector.SyncVariables(behaviorSource, allVariables))
                    {
                        flag2 = true;
                    }
                    if ((allVariables != null) && (allVariables.Count > 0))
                    {
                        List<SharedVariable> variables = behaviorSource.GetAllVariables();
                        if (VariableInspector.DrawAllVariables(false, behaviorSource, ref variables, false, ref variablePosition, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex, false, true))
                        {
                            flag2 = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("There are no variables to display", new GUILayoutOption[0]);
                    }
                    if (flag)
                    {
                        ExternalBehavior externalBehavior = (behaviorSource.Owner as Behavior).ExternalBehavior;
                        externalBehavior.BehaviorSource.Owner = externalBehavior;
                        externalBehavior.BehaviorSource.CheckForSerialization(true, behaviorSource);
                    }
                    if (flag2)
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
                    EditorGUI.indentLevel--;
                }
            }
            if (fromInspector)
            {
                showOptions = flag3 = EditorGUILayout.Foldout(showOptions, "Options");
                if (!flag3)
                {
                    goto Label_0425;
                }
            }
            if (fromInspector)
            {
                EditorGUI.indentLevel++;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startWhenEnabled"), true, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseWhenDisabled"), true, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restartWhenComplete"), true, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("resetValuesOnRestart"), true, new GUILayoutOption[0]);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("logTaskChanges"), true, new GUILayoutOption[0]);
            if (fromInspector)
            {
                EditorGUI.indentLevel--;
            }
        Label_0425:
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            Behavior target = this.target as Behavior;
            if (target != null)
            {
                GizmoManager.UpdateGizmo(target);
            }
        }

        public override void OnInspectorGUI()
        {
            Behavior target = this.target as Behavior;
            if (target != null)
            {
                bool externalModification = false;
                if (DrawInspectorGUI(target, base.serializedObject, true, ref externalModification, ref this.mShowOptions, ref this.mShowVariables))
                {
                    EditorUtility.SetDirty(target);
                    if ((externalModification && (BehaviorDesignerWindow.instance != null)) && (target.GetBehaviorSource().BehaviorID == BehaviorDesignerWindow.instance.ActiveBehaviorID))
                    {
                        BehaviorDesignerWindow.instance.LoadBehavior(target.GetBehaviorSource(), false, false);
                    }
                }
            }
        }
    }
}

