namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(VariableSynchronizer))]
    public class VariableSynchronizerInspector : Editor
    {
        private System.Type playMakerSynchronizationType;
        [SerializeField]
        private bool setVariable;
        [SerializeField]
        private Synchronizer sharedVariableSynchronizer = new Synchronizer();
        private System.Type sharedVariableValueType;
        [SerializeField]
        private string sharedVariableValueTypeName;
        [SerializeField]
        private VariableSynchronizer.SynchronizationType synchronizationType;
        [SerializeField]
        private Synchronizer targetSynchronizer;
        private Action<Synchronizer, System.Type> thirdPartySynchronizer;
        private System.Type uFrameSynchronizationType;

        private void DrawAnimatorSynchronizer(Synchronizer synchronizer)
        {
            DrawComponentSelector(synchronizer, typeof(Animator), ComponentListType.Instant);
            synchronizer.targetName = EditorGUILayout.TextField("Parameter Name", synchronizer.targetName, new GUILayoutOption[0]);
        }

        public static void DrawComponentSelector(Synchronizer synchronizer, System.Type componentType, ComponentListType listType)
        {
            int count;
            List<string> list;
            Component[] components;
            bool flag = false;
            EditorGUI.BeginChangeCheck();
            synchronizer.gameObject = EditorGUILayout.ObjectField("GameObject", synchronizer.gameObject, typeof(GameObject), true, new GUILayoutOption[0]) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                flag = true;
            }
            if (synchronizer.gameObject == null)
            {
                GUI.enabled = false;
            }
            switch (listType)
            {
                case ComponentListType.Instant:
                    if (flag)
                    {
                        if (synchronizer.gameObject == null)
                        {
                            synchronizer.component = null;
                            return;
                        }
                        synchronizer.component = synchronizer.gameObject.GetComponent(componentType);
                    }
                    return;

                case ComponentListType.Popup:
                    count = 0;
                    list = new List<string>();
                    components = null;
                    list.Add("None");
                    if (synchronizer.gameObject != null)
                    {
                        components = synchronizer.gameObject.GetComponents(componentType);
                        for (int i = 0; i < components.Length; i++)
                        {
                            if (components[i].Equals(synchronizer.component))
                            {
                                count = list.Count;
                            }
                            string str = BehaviorDesignerUtility.SplitCamelCase(components[i].GetType().Name);
                            int num3 = 0;
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (list[i].Equals(str))
                                {
                                    num3++;
                                }
                            }
                            if (num3 > 0)
                            {
                                str = str + " " + num3;
                            }
                            list.Add(str);
                        }
                    }
                    break;

                case ComponentListType.BehaviorDesignerGroup:
                    if (synchronizer.gameObject != null)
                    {
                        Behavior[] behaviors = synchronizer.gameObject.GetComponents<Behavior>();
                        if ((behaviors != null) && (behaviors.Length > 1))
                        {
                            synchronizer.componentGroup = EditorGUILayout.IntField("Behavior Tree Group", synchronizer.componentGroup, new GUILayoutOption[0]);
                        }
                        synchronizer.component = GetBehaviorWithGroup(behaviors, synchronizer.componentGroup);
                    }
                    return;

                default:
                    return;
            }
            EditorGUI.BeginChangeCheck();
            count = EditorGUILayout.Popup("Component", count, list.ToArray(), new GUILayoutOption[0]);
            if (EditorGUI.EndChangeCheck())
            {
                if (count != 0)
                {
                    synchronizer.component = components[count - 1];
                }
                else
                {
                    synchronizer.component = null;
                }
            }
        }

        private void DrawPlayMakerSynchronizer(Synchronizer synchronizer, System.Type valueType)
        {
            if (this.playMakerSynchronizationType == null)
            {
                this.playMakerSynchronizationType = System.Type.GetType("BehaviorDesigner.Editor.VariableSynchronizerInspector_PlayMaker, Assembly-CSharp-Editor");
                if (this.playMakerSynchronizationType == null)
                {
                    EditorGUILayout.LabelField("Unable to find PlayMaker inspector task.", new GUILayoutOption[0]);
                    return;
                }
            }
            if (this.thirdPartySynchronizer == null)
            {
                MethodInfo method = this.playMakerSynchronizationType.GetMethod("DrawPlayMakerSynchronizer");
                if (method != null)
                {
                    this.thirdPartySynchronizer = (Action<Synchronizer, System.Type>) Delegate.CreateDelegate(typeof(Action<Synchronizer, System.Type>), method);
                }
            }
            this.thirdPartySynchronizer(synchronizer, valueType);
        }

        private void DrawPropertySynchronizer(Synchronizer synchronizer, System.Type valueType)
        {
            DrawComponentSelector(synchronizer, typeof(Component), ComponentListType.Popup);
            int selectedIndex = 0;
            List<string> list = new List<string>();
            PropertyInfo[] properties = null;
            list.Add("None");
            if (synchronizer.component != null)
            {
                properties = synchronizer.component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].PropertyType.Equals(valueType) && !properties[i].IsSpecialName)
                    {
                        if (properties[i].Name.Equals(synchronizer.targetName))
                        {
                            selectedIndex = list.Count;
                        }
                        list.Add(properties[i].Name);
                    }
                }
            }
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Property", selectedIndex, list.ToArray(), new GUILayoutOption[0]);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex != 0)
                {
                    synchronizer.targetName = list[selectedIndex];
                }
                else
                {
                    synchronizer.targetName = string.Empty;
                }
            }
        }

        private bool DrawSharedVariableSynchronizer(Synchronizer synchronizer, System.Type valueType)
        {
            DrawComponentSelector(synchronizer, typeof(Behavior), ComponentListType.BehaviorDesignerGroup);
            int selectedIndex = 0;
            int globalStartIndex = -1;
            string[] names = null;
            if (synchronizer.component != null)
            {
                selectedIndex = FieldInspector.GetVariablesOfType(valueType, synchronizer.global, synchronizer.targetName, (synchronizer.component as Behavior).GetBehaviorSource(), out names, ref globalStartIndex, valueType == null);
            }
            else
            {
                names = new string[] { "None" };
            }
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("Shared Variable", selectedIndex, names, new GUILayoutOption[0]);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex != 0)
                {
                    if ((globalStartIndex != -1) && (selectedIndex >= globalStartIndex))
                    {
                        synchronizer.targetName = names[selectedIndex].Substring(8, names[selectedIndex].Length - 8);
                        synchronizer.global = true;
                    }
                    else
                    {
                        synchronizer.targetName = names[selectedIndex];
                        synchronizer.global = false;
                    }
                    if (valueType == null)
                    {
                        SharedVariable variable;
                        if (synchronizer.global)
                        {
                            variable = GlobalVariables.Instance.GetVariable(synchronizer.targetName);
                        }
                        else
                        {
                            variable = (synchronizer.component as Behavior).GetVariable(names[selectedIndex]);
                        }
                        this.sharedVariableValueTypeName = variable.GetType().GetProperty("Value").PropertyType.FullName;
                        this.sharedVariableValueType = null;
                    }
                }
                else
                {
                    synchronizer.targetName = null;
                }
            }
            if (string.IsNullOrEmpty(synchronizer.targetName))
            {
                GUI.enabled = false;
            }
            return GUI.enabled;
        }

        private void DrawSynchronizedVariables(VariableSynchronizer variableSynchronizer)
        {
            GUI.enabled = true;
            if ((variableSynchronizer.SynchronizedVariables != null) && (variableSynchronizer.SynchronizedVariables.Count != 0))
            {
                Rect lastRect = GUILayoutUtility.GetLastRect();
                lastRect.x = -5f;
                lastRect.y += lastRect.height + 1f;
                lastRect.height = 2f;
                lastRect.width += 20f;
                GUI.DrawTexture(lastRect, BehaviorDesignerUtility.LoadTexture("ContentSeparator.png", true, this));
                GUILayout.Space(6f);
                for (int i = 0; i < variableSynchronizer.SynchronizedVariables.Count; i++)
                {
                    VariableSynchronizer.SynchronizedVariable variable = variableSynchronizer.SynchronizedVariables[i];
                    if (variable.global)
                    {
                        if (GlobalVariables.Instance.GetVariable(variable.variableName) != null)
                        {
                            goto Label_00FB;
                        }
                        variableSynchronizer.SynchronizedVariables.RemoveAt(i);
                        break;
                    }
                    if (variable.behavior.GetVariable(variable.variableName) == null)
                    {
                        variableSynchronizer.SynchronizedVariables.RemoveAt(i);
                        break;
                    }
                Label_00FB:
                    EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MaxWidth(120f) };
                    EditorGUILayout.LabelField(variable.variableName, options);
                    GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(22f) };
                    if (GUILayout.Button(BehaviorDesignerUtility.LoadTexture(!variable.setVariable ? "RightArrowButton.png" : "LeftArrowButton.png", true, this), BehaviorDesignerUtility.ButtonGUIStyle, optionArray2) && !Application.isPlaying)
                    {
                        variable.setVariable = !variable.setVariable;
                    }
                    GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.MinWidth(120f) };
                    EditorGUILayout.LabelField(string.Format("{0} ({1})", variable.targetName, variable.synchronizationType.ToString()), optionArray3);
                    GUILayout.FlexibleSpace();
                    GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(22f) };
                    if (GUILayout.Button(BehaviorDesignerUtility.LoadTexture("DeleteButton.png", true, this), BehaviorDesignerUtility.ButtonGUIStyle, optionArray4))
                    {
                        variableSynchronizer.SynchronizedVariables.RemoveAt(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.Space(2f);
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2f);
                }
                GUILayout.Space(4f);
            }
        }

        private void DrawuFrameSynchronizer(Synchronizer synchronizer, System.Type valueType)
        {
            if (this.uFrameSynchronizationType == null)
            {
                this.uFrameSynchronizationType = System.Type.GetType("BehaviorDesigner.Editor.VariableSynchronizerInspector_uFrame, Assembly-CSharp-Editor");
                if (this.uFrameSynchronizationType == null)
                {
                    EditorGUILayout.LabelField("Unable to find uFrame inspector task.", new GUILayoutOption[0]);
                    return;
                }
            }
            if (this.thirdPartySynchronizer == null)
            {
                MethodInfo method = this.uFrameSynchronizationType.GetMethod("DrawSynchronizer");
                if (method != null)
                {
                    this.thirdPartySynchronizer = (Action<Synchronizer, System.Type>) Delegate.CreateDelegate(typeof(Action<Synchronizer, System.Type>), method);
                }
            }
            this.thirdPartySynchronizer(synchronizer, valueType);
        }

        private static Behavior GetBehaviorWithGroup(Behavior[] behaviors, int group)
        {
            if ((behaviors == null) || (behaviors.Length == 0))
            {
                return null;
            }
            if (behaviors.Length != 1)
            {
                for (int i = 0; i < behaviors.Length; i++)
                {
                    if (behaviors[i].Group == group)
                    {
                        return behaviors[i];
                    }
                }
            }
            return behaviors[0];
        }

        public override void OnInspectorGUI()
        {
            VariableSynchronizer target = this.target as VariableSynchronizer;
            if (target != null)
            {
                GUILayout.Space(5f);
                target.UpdateInterval = (UpdateIntervalType) EditorGUILayout.EnumPopup("Update Interval", target.UpdateInterval, new GUILayoutOption[0]);
                if (target.UpdateInterval == UpdateIntervalType.SpecifySeconds)
                {
                    target.UpdateIntervalSeconds = EditorGUILayout.FloatField("Seconds", target.UpdateIntervalSeconds, new GUILayoutOption[0]);
                }
                GUILayout.Space(5f);
                GUI.enabled = !Application.isPlaying;
                this.DrawSharedVariableSynchronizer(this.sharedVariableSynchronizer, null);
                if (string.IsNullOrEmpty(this.sharedVariableSynchronizer.targetName))
                {
                    this.DrawSynchronizedVariables(target);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MaxWidth(146f) };
                    EditorGUILayout.LabelField("Direction", options);
                    GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(22f) };
                    if (GUILayout.Button(BehaviorDesignerUtility.LoadTexture(!this.setVariable ? "RightArrowButton.png" : "LeftArrowButton.png", true, this), BehaviorDesignerUtility.ButtonGUIStyle, optionArray2))
                    {
                        this.setVariable = !this.setVariable;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    this.synchronizationType = (VariableSynchronizer.SynchronizationType) EditorGUILayout.EnumPopup("Type", this.synchronizationType, new GUILayoutOption[0]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        this.targetSynchronizer = new Synchronizer();
                    }
                    if (this.targetSynchronizer == null)
                    {
                        this.targetSynchronizer = new Synchronizer();
                    }
                    if ((this.sharedVariableValueType == null) && !string.IsNullOrEmpty(this.sharedVariableValueTypeName))
                    {
                        this.sharedVariableValueType = TaskUtility.GetTypeWithinAssembly(this.sharedVariableValueTypeName);
                    }
                    switch (this.synchronizationType)
                    {
                        case VariableSynchronizer.SynchronizationType.BehaviorDesigner:
                            this.DrawSharedVariableSynchronizer(this.targetSynchronizer, this.sharedVariableValueType);
                            break;

                        case VariableSynchronizer.SynchronizationType.Property:
                            this.DrawPropertySynchronizer(this.targetSynchronizer, this.sharedVariableValueType);
                            break;

                        case VariableSynchronizer.SynchronizationType.Animator:
                            this.DrawAnimatorSynchronizer(this.targetSynchronizer);
                            break;

                        case VariableSynchronizer.SynchronizationType.PlayMaker:
                            this.DrawPlayMakerSynchronizer(this.targetSynchronizer, this.sharedVariableValueType);
                            break;

                        case VariableSynchronizer.SynchronizationType.uFrame:
                            this.DrawuFrameSynchronizer(this.targetSynchronizer, this.sharedVariableValueType);
                            break;
                    }
                    if (string.IsNullOrEmpty(this.targetSynchronizer.targetName))
                    {
                        GUI.enabled = false;
                    }
                    if (GUILayout.Button("Add", new GUILayoutOption[0]))
                    {
                        VariableSynchronizer.SynchronizedVariable item = new VariableSynchronizer.SynchronizedVariable(this.synchronizationType, this.setVariable, this.sharedVariableSynchronizer.component as Behavior, this.sharedVariableSynchronizer.targetName, this.sharedVariableSynchronizer.global, this.targetSynchronizer.component, this.targetSynchronizer.targetName, this.targetSynchronizer.global);
                        target.SynchronizedVariables.Add(item);
                        EditorUtility.SetDirty(target);
                        this.sharedVariableSynchronizer = new Synchronizer();
                        this.targetSynchronizer = new Synchronizer();
                    }
                    GUI.enabled = true;
                    this.DrawSynchronizedVariables(target);
                }
            }
        }

        public enum ComponentListType
        {
            Instant,
            Popup,
            BehaviorDesignerGroup,
            None
        }

        [Serializable]
        public class Synchronizer
        {
            public Component component;
            public int componentGroup;
            public string componentName;
            public GameObject gameObject;
            public bool global;
            public string targetName;
        }
    }
}

