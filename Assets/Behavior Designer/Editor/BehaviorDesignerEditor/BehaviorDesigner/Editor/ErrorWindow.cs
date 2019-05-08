namespace BehaviorDesigner.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class ErrorWindow : EditorWindow
    {
        public static ErrorWindow instance;
        private List<BehaviorDesigner.Editor.ErrorDetails> mErrorDetails;
        private Vector2 mScrollPosition;

        public void OnFocus()
        {
            instance = this;
            if (BehaviorDesignerWindow.instance != null)
            {
                this.mErrorDetails = BehaviorDesignerWindow.instance.ErrorDetails;
            }
        }

        public void OnGUI()
        {
            this.mScrollPosition = EditorGUILayout.BeginScrollView(this.mScrollPosition, new GUILayoutOption[0]);
            if ((this.mErrorDetails != null) && (this.mErrorDetails.Count > 0))
            {
                for (int i = 0; i < this.mErrorDetails.Count; i++)
                {
                    BehaviorDesigner.Editor.ErrorDetails details = this.mErrorDetails[i];
                    if (((details != null) && (details.NodeDesigner != null)) && (details.NodeDesigner.Task != null))
                    {
                        string label = string.Empty;
                        switch (details.Type)
                        {
                            case BehaviorDesigner.Editor.ErrorDetails.ErrorType.RequiredField:
                            {
                                object[] args = new object[] { details.TaskFriendlyName, details.TaskType, details.NodeDesigner.Task.ID, BehaviorDesignerUtility.SplitCamelCase(details.FieldName) };
                                label = string.Format("The task {0} ({1}, index {2}) requires a value for the field {3}.", args);
                                break;
                            }
                            case BehaviorDesigner.Editor.ErrorDetails.ErrorType.SharedVariable:
                            {
                                object[] objArray2 = new object[] { details.TaskFriendlyName, details.TaskType, details.NodeDesigner.Task.ID, BehaviorDesignerUtility.SplitCamelCase(details.FieldName) };
                                label = string.Format("The task {0} ({1}, index {2}) has a Shared Variable field ({3}) that is marked as shared but is not referencing a Shared Variable.", objArray2);
                                break;
                            }
                            case BehaviorDesigner.Editor.ErrorDetails.ErrorType.MissingChildren:
                                label = string.Format("The {0} task ({1}, index {2}) is a parent task which does not have any children", details.TaskFriendlyName, details.TaskType, details.NodeDesigner.Task.ID);
                                break;

                            case BehaviorDesigner.Editor.ErrorDetails.ErrorType.UnknownTask:
                                label = string.Format("The task at index {0} is unknown. Has a task been renamed or deleted?", details.NodeDesigner.Task.ID);
                                break;
                        }
                        GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Height(30f), GUILayout.Width((float) (Screen.width - 7)) };
                        EditorGUILayout.LabelField(label, ((i % 2) != 0) ? BehaviorDesignerUtility.ErrorListDarkBackground : BehaviorDesignerUtility.ErrorListLightBackground, options);
                    }
                }
            }
            else if (!BehaviorDesignerPreferences.GetBool(BDPreferences.ErrorChecking))
            {
                EditorGUILayout.LabelField("Enable realtime error checking from the preferences to view the errors.", BehaviorDesignerUtility.ErrorListLightBackground, new GUILayoutOption[0]);
            }
            else
            {
                EditorGUILayout.LabelField("The behavior tree has no errors.", BehaviorDesignerUtility.ErrorListLightBackground, new GUILayoutOption[0]);
            }
            EditorGUILayout.EndScrollView();
        }

        [UnityEditor.MenuItem("Tools/Behavior Designer/Error List", false, 2)]
        public static void ShowWindow()
        {
            ErrorWindow window = EditorWindow.GetWindow<ErrorWindow>(false, "Error List");
            window.minSize = new Vector2(400f, 200f);
            window.wantsMouseMove = true;
        }

        public List<BehaviorDesigner.Editor.ErrorDetails> ErrorDetails
        {
            set
            {
                this.mErrorDetails = value;
            }
        }
    }
}

