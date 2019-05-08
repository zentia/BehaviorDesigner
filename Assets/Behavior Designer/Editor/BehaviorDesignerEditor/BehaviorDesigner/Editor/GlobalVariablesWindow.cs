namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class GlobalVariablesWindow : EditorWindow
    {
        public static GlobalVariablesWindow instance;
        private bool mFocusNameField;
        private Vector2 mScrollPosition = Vector2.zero;
        [SerializeField]
        private int mSelectedVariableIndex = -1;
        [SerializeField]
        private string mSelectedVariableName;
        [SerializeField]
        private int mSelectedVariableTypeIndex;
        private string mVariableName = string.Empty;
        [SerializeField]
        private List<float> mVariablePosition;
        private GlobalVariables mVariableSource;
        [SerializeField]
        private float mVariableStartPosition = -1f;
        private int mVariableTypeIndex;

        public void OnFocus()
        {
            instance = this;
            this.mVariableSource = GlobalVariables.Instance;
            if (this.mVariableSource != null)
            {
                this.mVariableSource.CheckForSerialization(!Application.isPlaying);
            }
            FieldInspector.Init();
        }

        public void OnGUI()
        {
            if (this.mVariableSource == null)
            {
                this.mVariableSource = GlobalVariables.Instance;
            }
            if (VariableInspector.DrawVariables(this.mVariableSource, true, null, ref this.mVariableName, ref this.mFocusNameField, ref this.mVariableTypeIndex, ref this.mScrollPosition, ref this.mVariablePosition, ref this.mVariableStartPosition, ref this.mSelectedVariableIndex, ref this.mSelectedVariableName, ref this.mSelectedVariableTypeIndex))
            {
                this.SerializeVariables();
            }
            if ((Event.current.type == EventType.MouseDown) && VariableInspector.LeftMouseDown(this.mVariableSource, null, Event.current.mousePosition, this.mVariablePosition, this.mVariableStartPosition, this.mScrollPosition, ref this.mSelectedVariableIndex, ref this.mSelectedVariableName, ref this.mSelectedVariableTypeIndex))
            {
                Event.current.Use();
                base.Repaint();
            }
        }

        private void SerializeVariables()
        {
            if (this.mVariableSource == null)
            {
                this.mVariableSource = GlobalVariables.Instance;
            }
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
            {
                BinarySerialization.Save(this.mVariableSource);
            }
            else
            {
                SerializeJSON.Save(this.mVariableSource);
            }
        }

        [UnityEditor.MenuItem("Tools/Behavior Designer/Global Variables", false, 1)]
        public static void ShowWindow()
        {
            GlobalVariablesWindow window = EditorWindow.GetWindow<GlobalVariablesWindow>(false, "Global Variables");
            window.minSize = new Vector2(300f, 410f);
            window.maxSize = new Vector2(300f, float.MaxValue);
            window.wantsMouseMove = true;
        }
    }
}

