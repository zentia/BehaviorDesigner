namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class TaskInspector : ScriptableObject
    {
        private Task activeReferenceTask;
        private System.Reflection.FieldInfo activeReferenceTaskFieldInfo;
        private BehaviorDesignerWindow behaviorDesignerWindow;
        private Task mActiveMenuSelectionTask;
        private Vector2 mScrollPosition = Vector2.zero;

        private void AddColorMenuItem(ref GenericMenu menu, Task task, string color, int index)
        {
            menu.AddItem(new GUIContent(color), task.NodeData.ColorIndex == index, new GenericMenu.MenuFunction2(this.SetTaskColor), new TaskColor(task, index));
        }

        private bool CanDrawReflectedField(object task, System.Reflection.FieldInfo field)
        {
            if (((!field.Name.Contains("parameter") && !field.Name.Contains("storeResult")) && (!field.Name.Contains("fieldValue") && !field.Name.Contains("propertyValue"))) && !field.Name.Contains("compareValue"))
            {
                return true;
            }
            if (this.IsInvokeMethodTask(task.GetType()))
            {
                if (field.Name.Contains("parameter"))
                {
                    return (task.GetType().GetField(field.Name).GetValue(task) != null);
                }
                MethodInfo invokeMethodInfo = null;
                invokeMethodInfo = this.GetInvokeMethodInfo(task);
                if (invokeMethodInfo == null)
                {
                    return false;
                }
                if (field.Name.Equals("storeResult"))
                {
                    return !invokeMethodInfo.ReturnType.Equals(typeof(void));
                }
                return true;
            }
            if (this.IsFieldReflectionTask(task.GetType()))
            {
                SharedVariable variable = task.GetType().GetField("fieldName").GetValue(task) as SharedVariable;
                return ((variable != null) && !string.IsNullOrEmpty((string) variable.GetValue()));
            }
            SharedVariable variable2 = task.GetType().GetField("propertyName").GetValue(task) as SharedVariable;
            return ((variable2 != null) && !string.IsNullOrEmpty((string) variable2.GetValue()));
        }

        public void ClearFocus()
        {
            GUIUtility.keyboardControl = 0;
        }

        private void ClearInvokeVariablesTask()
        {
            for (int i = 0; i < 4; i++)
            {
                this.mActiveMenuSelectionTask.GetType().GetField("parameter" + (i + 1)).SetValue(this.mActiveMenuSelectionTask, null);
            }
            this.mActiveMenuSelectionTask.GetType().GetField("storeResult").SetValue(this.mActiveMenuSelectionTask, null);
        }

        private void ComponentSelectionCallback(object obj)
        {
            if (this.mActiveMenuSelectionTask != null)
            {
                System.Reflection.FieldInfo field = this.mActiveMenuSelectionTask.GetType().GetField("componentName");
                SharedVariable variable = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")) as SharedVariable;
                if (obj == null)
                {
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                    variable = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")) as SharedVariable;
                    System.Reflection.FieldInfo info2 = null;
                    if (this.IsInvokeMethodTask(this.mActiveMenuSelectionTask.GetType()))
                    {
                        info2 = this.mActiveMenuSelectionTask.GetType().GetField("methodName");
                        this.ClearInvokeVariablesTask();
                    }
                    else if (this.IsFieldReflectionTask(this.mActiveMenuSelectionTask.GetType()))
                    {
                        info2 = this.mActiveMenuSelectionTask.GetType().GetField("fieldName");
                    }
                    else
                    {
                        info2 = this.mActiveMenuSelectionTask.GetType().GetField("propertyName");
                    }
                    info2.SetValue(this.mActiveMenuSelectionTask, variable);
                }
                else
                {
                    string str = (string) obj;
                    SharedVariable variable2 = field.GetValue(this.mActiveMenuSelectionTask) as SharedVariable;
                    if (!str.Equals((string) variable2.GetValue()))
                    {
                        System.Reflection.FieldInfo info3 = null;
                        System.Reflection.FieldInfo info4 = null;
                        if (this.IsInvokeMethodTask(this.mActiveMenuSelectionTask.GetType()))
                        {
                            info3 = this.mActiveMenuSelectionTask.GetType().GetField("methodName");
                            for (int i = 0; i < 4; i++)
                            {
                                this.mActiveMenuSelectionTask.GetType().GetField("parameter" + (i + 1)).SetValue(this.mActiveMenuSelectionTask, null);
                            }
                            info4 = this.mActiveMenuSelectionTask.GetType().GetField("storeResult");
                        }
                        else if (this.IsFieldReflectionTask(this.mActiveMenuSelectionTask.GetType()))
                        {
                            info3 = this.mActiveMenuSelectionTask.GetType().GetField("fieldName");
                            info4 = this.mActiveMenuSelectionTask.GetType().GetField("fieldValue");
                            if (info4 == null)
                            {
                                info4 = this.mActiveMenuSelectionTask.GetType().GetField("compareValue");
                            }
                        }
                        else
                        {
                            info3 = this.mActiveMenuSelectionTask.GetType().GetField("propertyName");
                            info4 = this.mActiveMenuSelectionTask.GetType().GetField("propertyValue");
                            if (info4 == null)
                            {
                                info4 = this.mActiveMenuSelectionTask.GetType().GetField("compareValue");
                            }
                        }
                        info3.SetValue(this.mActiveMenuSelectionTask, variable);
                        info4.SetValue(this.mActiveMenuSelectionTask, null);
                    }
                    variable = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")) as SharedVariable;
                    variable.SetValue(str);
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                }
            }
            BehaviorDesignerWindow.instance.SaveBehavior();
        }

        private void DrawObjectFields(BehaviorSource behaviorSource, TaskList taskList, Task task, object obj, bool enabled, bool drawWatch)
        {
            if (obj != null)
            {
                List<System.Type> baseClasses = FieldInspector.GetBaseClasses(obj.GetType());
                BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                bool isReflectionTask = this.IsReflectionTask(obj.GetType());
                for (int i = baseClasses.Count - 1; i > -1; i--)
                {
                    System.Reflection.FieldInfo[] fields = baseClasses[i].GetFields(bindingAttr);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        if (((!BehaviorDesignerUtility.HasAttribute(fields[j], typeof(NonSerializedAttribute)) && !BehaviorDesignerUtility.HasAttribute(fields[j], typeof(HideInInspector))) && ((!fields[j].IsPrivate && !fields[j].IsFamily) || BehaviorDesignerUtility.HasAttribute(fields[j], typeof(SerializeField)))) && ((!(obj is ParentTask) || !fields[j].Name.Equals("children")) && ((!isReflectionTask || (!fields[j].FieldType.Equals(typeof(SharedVariable)) && !fields[j].FieldType.IsSubclassOf(typeof(SharedVariable)))) || this.CanDrawReflectedField(obj, fields[j]))))
                        {
                            GUIContent guiContent = null;
                            BehaviorDesigner.Runtime.Tasks.TooltipAttribute[] attributeArray = null;
                            string name = fields[j].Name;
                            if (isReflectionTask && (fields[j].FieldType.Equals(typeof(SharedVariable)) || fields[j].FieldType.IsSubclassOf(typeof(SharedVariable))))
                            {
                                name = this.InvokeParameterName(obj, fields[j]);
                            }
                            if ((attributeArray = fields[j].GetCustomAttributes(typeof(BehaviorDesigner.Runtime.Tasks.TooltipAttribute), false) as BehaviorDesigner.Runtime.Tasks.TooltipAttribute[]).Length > 0)
                            {
                                guiContent = new GUIContent(BehaviorDesignerUtility.SplitCamelCase(name), attributeArray[0].Tooltip);
                            }
                            else
                            {
                                guiContent = new GUIContent(BehaviorDesignerUtility.SplitCamelCase(name));
                            }
                            object obj2 = fields[j].GetValue(obj);
                            System.Type fieldType = fields[j].FieldType;
                            if (typeof(Task).IsAssignableFrom(fieldType) || (typeof(IList).IsAssignableFrom(fieldType) && (typeof(Task).IsAssignableFrom(fieldType.GetElementType()) || (fieldType.IsGenericType && typeof(Task).IsAssignableFrom(fieldType.GetGenericArguments()[0])))))
                            {
                                EditorGUI.BeginChangeCheck();
                                this.DrawTaskValue(behaviorSource, taskList, fields[j], guiContent, task, obj2 as Task, enabled);
                                if (TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) && !ErrorCheck.IsRequiredFieldValid(fieldType, obj2))
                                {
                                    GUILayout.Space(-3f);
                                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(20f) };
                                    GUILayout.Box(BehaviorDesignerUtility.ErrorIconTexture, BehaviorDesignerUtility.PlainTextureGUIStyle, options);
                                }
                                if (EditorGUI.EndChangeCheck())
                                {
                                    GUI.changed = true;
                                }
                            }
                            else if (fieldType.Equals(typeof(SharedVariable)) || fieldType.IsSubclassOf(typeof(SharedVariable)))
                            {
                                SharedVariable variable = fields[j].GetValue(task) as SharedVariable;
                                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                                EditorGUI.BeginChangeCheck();
                                if (drawWatch)
                                {
                                    this.DrawWatchedButton(task, fields[j]);
                                }
                                SharedVariable variable2 = this.DrawSharedVariableValue(behaviorSource, fields[j], guiContent, task, obj2 as SharedVariable, isReflectionTask, enabled, drawWatch);
                                if (!TaskUtility.HasAttribute(fields[j], typeof(SharedRequiredAttribute)) && ((TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) && !ErrorCheck.IsRequiredFieldValid(fieldType, obj2)) || (((variable != null) && variable.IsShared) && string.IsNullOrEmpty(variable.Name))))
                                {
                                    GUILayout.Space(-3f);
                                    GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(20f) };
                                    GUILayout.Box(BehaviorDesignerUtility.ErrorIconTexture, BehaviorDesignerUtility.PlainTextureGUIStyle, optionArray2);
                                }
                                GUILayout.EndHorizontal();
                                GUILayout.Space(4f);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    fields[j].SetValue(obj, variable2);
                                    GUI.changed = true;
                                }
                            }
                            else
                            {
                                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                                EditorGUI.BeginChangeCheck();
                                if (drawWatch)
                                {
                                    this.DrawWatchedButton(task, fields[j]);
                                }
                                object obj3 = FieldInspector.DrawField(task, guiContent, fields[j], obj2);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    fields[j].SetValue(obj, obj3);
                                    GUI.changed = true;
                                }
                                if (TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) && !ErrorCheck.IsRequiredFieldValid(fieldType, obj2))
                                {
                                    GUILayout.Space(-3f);
                                    GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(20f) };
                                    GUILayout.Box(BehaviorDesignerUtility.ErrorIconTexture, BehaviorDesignerUtility.PlainTextureGUIStyle, optionArray3);
                                }
                                GUILayout.EndHorizontal();
                                GUILayout.Space(4f);
                            }
                        }
                    }
                }
            }
        }

        private void DrawReflectionField(Task task, GUIContent guiContent, bool drawComponentField, System.Reflection.FieldInfo field)
        {
            SharedVariable variable = task.GetType().GetField("targetGameObject").GetValue(task) as SharedVariable;
            if (drawComponentField)
            {
                GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(146f) };
                GUILayout.Label(guiContent, options);
                SharedVariable variable2 = field.GetValue(task) as SharedVariable;
                string text = string.Empty;
                if (string.IsNullOrEmpty((string) variable2.GetValue()))
                {
                    text = "Select";
                }
                else
                {
                    string str2 = (string) variable2.GetValue();
                    char[] separator = new char[] { '.' };
                    string[] strArray = str2.Split(separator);
                    text = strArray[strArray.Length - 1];
                }
                GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(92f) };
                if (GUILayout.Button(text, EditorStyles.toolbarPopup, optionArray2))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty((string) variable2.GetValue()), new GenericMenu.MenuFunction2(this.ComponentSelectionCallback), null);
                    GameObject gameObject = null;
                    if ((variable == null) || (((GameObject) variable.GetValue()) == null))
                    {
                        if (task.Owner != null)
                        {
                            gameObject = task.Owner.gameObject;
                        }
                    }
                    else
                    {
                        gameObject = (GameObject) variable.GetValue();
                    }
                    if (gameObject != null)
                    {
                        Component[] components = gameObject.GetComponents<Component>();
                        for (int i = 0; i < components.Length; i++)
                        {
                            menu.AddItem(new GUIContent(components[i].GetType().Name), components[i].GetType().FullName.Equals((string) variable2.GetValue()), new GenericMenu.MenuFunction2(this.ComponentSelectionCallback), components[i].GetType().FullName);
                        }
                        menu.ShowAsContext();
                        this.mActiveMenuSelectionTask = task;
                    }
                }
            }
            else
            {
                GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(146f) };
                GUILayout.Label(guiContent, optionArray3);
                SharedVariable variable3 = task.GetType().GetField("componentName").GetValue(task) as SharedVariable;
                SharedVariable variable4 = field.GetValue(task) as SharedVariable;
                string str3 = string.Empty;
                if (string.IsNullOrEmpty((string) variable3.GetValue()))
                {
                    str3 = "Component Required";
                }
                else if (string.IsNullOrEmpty((string) variable4.GetValue()))
                {
                    str3 = "Select";
                }
                else
                {
                    str3 = (string) variable4.GetValue();
                }
                GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(92f) };
                if (GUILayout.Button(str3, EditorStyles.toolbarPopup, optionArray4) && !string.IsNullOrEmpty((string) variable3.GetValue()))
                {
                    GenericMenu menu2 = new GenericMenu();
                    menu2.AddItem(new GUIContent("None"), string.IsNullOrEmpty((string) variable4.GetValue()), new GenericMenu.MenuFunction2(this.SecondaryReflectionSelectionCallback), null);
                    GameObject obj3 = null;
                    if ((variable == null) || (((GameObject) variable.GetValue()) == null))
                    {
                        if (task.Owner != null)
                        {
                            obj3 = task.Owner.gameObject;
                        }
                    }
                    else
                    {
                        obj3 = (GameObject) variable.GetValue();
                    }
                    if (obj3 != null)
                    {
                        Component component = obj3.GetComponent(TaskUtility.GetTypeWithinAssembly((string) variable3.GetValue()));
                        List<System.Type> sharedVariableTypes = VariableInspector.FindAllSharedVariableTypes(false);
                        if (this.IsInvokeMethodTask(task.GetType()))
                        {
                            MethodInfo[] methods = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                            for (int j = 0; j < methods.Length; j++)
                            {
                                if ((methods[j].IsSpecialName || methods[j].IsGenericMethod) || (methods[j].GetParameters().Length > 4))
                                {
                                    continue;
                                }
                                System.Reflection.ParameterInfo[] parameters = methods[j].GetParameters();
                                bool flag = true;
                                for (int k = 0; k < parameters.Length; k++)
                                {
                                    if (!this.SharedVariableTypeExists(sharedVariableTypes, parameters[k].ParameterType))
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                                if (flag && (methods[j].ReturnType.Equals(typeof(void)) || this.SharedVariableTypeExists(sharedVariableTypes, methods[j].ReturnType)))
                                {
                                    menu2.AddItem(new GUIContent(methods[j].Name), methods[j].Name.Equals((string) variable4.GetValue()), new GenericMenu.MenuFunction2(this.SecondaryReflectionSelectionCallback), methods[j]);
                                }
                            }
                        }
                        else if (this.IsFieldReflectionTask(task.GetType()))
                        {
                            System.Reflection.FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                            for (int m = 0; m < fields.Length; m++)
                            {
                                if (!fields[m].IsSpecialName && this.SharedVariableTypeExists(sharedVariableTypes, fields[m].FieldType))
                                {
                                    menu2.AddItem(new GUIContent(fields[m].Name), fields[m].Name.Equals((string) variable4.GetValue()), new GenericMenu.MenuFunction2(this.SecondaryReflectionSelectionCallback), fields[m]);
                                }
                            }
                        }
                        else
                        {
                            PropertyInfo[] properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            for (int n = 0; n < properties.Length; n++)
                            {
                                if (!properties[n].IsSpecialName && this.SharedVariableTypeExists(sharedVariableTypes, properties[n].PropertyType))
                                {
                                    menu2.AddItem(new GUIContent(properties[n].Name), properties[n].Name.Equals((string) variable4.GetValue()), new GenericMenu.MenuFunction2(this.SecondaryReflectionSelectionCallback), properties[n]);
                                }
                            }
                        }
                        menu2.ShowAsContext();
                        this.mActiveMenuSelectionTask = task;
                    }
                }
            }
            GUILayout.Space(8f);
        }

        private SharedVariable DrawSharedVariableValue(BehaviorSource behaviorSource, System.Reflection.FieldInfo field, GUIContent guiContent, Task task, SharedVariable sharedVariable, bool isReflectionTask, bool enabled, bool drawWatch)
        {
            if (isReflectionTask)
            {
                if (!field.FieldType.Equals(typeof(SharedVariable)) && (sharedVariable == null))
                {
                    sharedVariable = Activator.CreateInstance(field.FieldType) as SharedVariable;
                    if (TaskUtility.HasAttribute(field, typeof(RequiredFieldAttribute)) || TaskUtility.HasAttribute(field, typeof(SharedRequiredAttribute)))
                    {
                        sharedVariable.IsShared = true;
                    }
                    GUI.changed = true;
                }
                if (sharedVariable.IsShared)
                {
                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(126f) };
                    GUILayout.Label(guiContent, options);
                    string[] names = null;
                    int globalStartIndex = -1;
                    int selectedIndex = FieldInspector.GetVariablesOfType(sharedVariable.GetType().GetProperty("Value").PropertyType, sharedVariable.IsGlobal, sharedVariable.Name, behaviorSource, out names, ref globalStartIndex, false);
                    Color backgroundColor = GUI.backgroundColor;
                    if ((selectedIndex == 0) && !TaskUtility.HasAttribute(field, typeof(SharedRequiredAttribute)))
                    {
                        GUI.backgroundColor = Color.red;
                    }
                    int num3 = selectedIndex;
                    selectedIndex = EditorGUILayout.Popup(selectedIndex, names, EditorStyles.toolbarPopup, new GUILayoutOption[0]);
                    GUI.backgroundColor = backgroundColor;
                    if (selectedIndex != num3)
                    {
                        if (selectedIndex == 0)
                        {
                            if (field.FieldType.Equals(typeof(SharedVariable)))
                            {
                                sharedVariable = Activator.CreateInstance(FieldInspector.FriendlySharedVariableName(sharedVariable.GetType().GetProperty("Value").PropertyType)) as SharedVariable;
                            }
                            else
                            {
                                sharedVariable = Activator.CreateInstance(field.FieldType) as SharedVariable;
                            }
                            sharedVariable.IsShared = true;
                        }
                        else if ((globalStartIndex != -1) && (selectedIndex >= globalStartIndex))
                        {
                            sharedVariable = GlobalVariables.Instance.GetVariable(names[selectedIndex].Substring(8, names[selectedIndex].Length - 8));
                        }
                        else
                        {
                            sharedVariable = behaviorSource.GetVariable(names[selectedIndex]);
                        }
                    }
                    GUILayout.Space(8f);
                }
                else
                {
                    bool drawComponentField = false;
                    if (((drawComponentField = field.Name.Equals("componentName")) || field.Name.Equals("methodName")) || (field.Name.Equals("fieldName") || field.Name.Equals("propertyName")))
                    {
                        this.DrawReflectionField(task, guiContent, drawComponentField, field);
                    }
                    else
                    {
                        FieldInspector.DrawFields(task, sharedVariable, guiContent);
                    }
                }
                if (!TaskUtility.HasAttribute(field, typeof(RequiredFieldAttribute)) && !TaskUtility.HasAttribute(field, typeof(SharedRequiredAttribute)))
                {
                    sharedVariable = FieldInspector.DrawSharedVariableToggleSharedButton(sharedVariable);
                }
                else if (!sharedVariable.IsShared)
                {
                    sharedVariable.IsShared = true;
                }
            }
            else
            {
                sharedVariable = FieldInspector.DrawSharedVariable(null, guiContent, field, field.FieldType, sharedVariable);
            }
            GUILayout.Space(8f);
            return sharedVariable;
        }

        private bool DrawTaskFields(BehaviorSource behaviorSource, TaskList taskList, Task task, bool enabled)
        {
            if (task == null)
            {
                return false;
            }
            EditorGUI.BeginChangeCheck();
            FieldInspector.behaviorSource = behaviorSource;
            this.DrawObjectFields(behaviorSource, taskList, task, task, enabled, true);
            return EditorGUI.EndChangeCheck();
        }

        public bool DrawTaskInspector(BehaviorSource behaviorSource, TaskList taskList, Task task, bool enabled)
        {
            if ((task == null) || (task.NodeData.NodeDesigner as NodeDesigner).IsEntryDisplay)
            {
                return false;
            }
            this.mScrollPosition = GUILayout.BeginScrollView(this.mScrollPosition, new GUILayoutOption[0]);
            GUI.enabled = enabled;
            if (this.behaviorDesignerWindow == null)
            {
                this.behaviorDesignerWindow = BehaviorDesignerWindow.instance;
            }
            EditorGUIUtility.labelWidth = 150f;
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(90f) };
            EditorGUILayout.LabelField("Name", options);
            task.FriendlyName = EditorGUILayout.TextField(task.FriendlyName, new GUILayoutOption[0]);
            if (GUILayout.Button(BehaviorDesignerUtility.DocTexture, BehaviorDesignerUtility.TransparentButtonGUIStyle, new GUILayoutOption[0]))
            {
                this.OpenHelpURL(task);
            }
            if (GUILayout.Button(BehaviorDesignerUtility.ColorSelectorTexture(task.NodeData.ColorIndex), BehaviorDesignerUtility.TransparentButtonOffsetGUIStyle, new GUILayoutOption[0]))
            {
                GenericMenu menu = new GenericMenu();
                this.AddColorMenuItem(ref menu, task, "Default", 0);
                this.AddColorMenuItem(ref menu, task, "Red", 1);
                this.AddColorMenuItem(ref menu, task, "Pink", 2);
                this.AddColorMenuItem(ref menu, task, "Brown", 3);
                this.AddColorMenuItem(ref menu, task, "Orange", 4);
                this.AddColorMenuItem(ref menu, task, "Turquoise", 5);
                this.AddColorMenuItem(ref menu, task, "Cyan", 6);
                this.AddColorMenuItem(ref menu, task, "Blue", 7);
                this.AddColorMenuItem(ref menu, task, "Purple", 8);
                menu.ShowAsContext();
            }
            if (GUILayout.Button(BehaviorDesignerUtility.GearTexture, BehaviorDesignerUtility.TransparentButtonGUIStyle, new GUILayoutOption[0]))
            {
                GenericMenu menu2 = new GenericMenu();
                menu2.AddItem(new GUIContent("Edit Script"), false, new GenericMenu.MenuFunction2(TaskInspector.OpenInFileEditor), task);
                menu2.AddItem(new GUIContent("Locate Script"), false, new GenericMenu.MenuFunction2(TaskInspector.SelectInProject), task);
                menu2.AddItem(new GUIContent("Reset"), false, new GenericMenu.MenuFunction2(this.ResetTask), task);
                menu2.ShowAsContext();
            }
            GUILayout.EndHorizontal();
            string str = BehaviorDesignerUtility.SplitCamelCase(task.GetType().Name.ToString());
            if (!task.FriendlyName.Equals(str))
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(90f) };
                EditorGUILayout.LabelField("Type", optionArray2);
                EditorGUILayout.LabelField(str, new GUILayoutOption[0]);
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(90f) };
            EditorGUILayout.LabelField("Instant", optionArray3);
            task.IsInstant = EditorGUILayout.Toggle(task.IsInstant, new GUILayoutOption[0]);
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Comment", new GUILayoutOption[0]);
            GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Height(48f) };
            task.NodeData.Comment = EditorGUILayout.TextArea(task.NodeData.Comment, BehaviorDesignerUtility.TaskInspectorCommentGUIStyle, optionArray4);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.changed = true;
            }
            BehaviorDesignerUtility.DrawContentSeperator(2);
            GUILayout.Space(6f);
            if (this.DrawTaskFields(behaviorSource, taskList, task, enabled))
            {
                BehaviorUndo.RegisterUndo("Inspector", behaviorSource.Owner.GetObject());
                GUI.changed = true;
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();
            return GUI.changed;
        }

        private void DrawTaskValue(BehaviorSource behaviorSource, TaskList taskList, System.Reflection.FieldInfo field, GUIContent guiContent, Task parentTask, Task task, bool enabled)
        {
            if (BehaviorDesignerUtility.HasAttribute(field, typeof(InspectTaskAttribute)))
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(144f) };
                GUILayout.Label(guiContent, options);
                GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(134f) };
                if (GUILayout.Button((task == null) ? "Select" : BehaviorDesignerUtility.SplitCamelCase(task.GetType().Name.ToString()), EditorStyles.toolbarPopup, optionArray2))
                {
                    GenericMenu genericMenu = new GenericMenu();
                    genericMenu.AddItem(new GUIContent("None"), task == null, new GenericMenu.MenuFunction2(this.InspectedTaskCallback), null);
                    taskList.AddConditionalTasksToMenu(ref genericMenu, (task == null) ? null : task.GetType(), string.Empty, new GenericMenu.MenuFunction2(this.InspectedTaskCallback));
                    genericMenu.ShowAsContext();
                    this.mActiveMenuSelectionTask = parentTask;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(2f);
                this.DrawObjectFields(behaviorSource, taskList, task, task, enabled, false);
            }
            else
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                this.DrawWatchedButton(parentTask, field);
                GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(165f) };
                GUILayout.Label(guiContent, BehaviorDesignerUtility.TaskInspectorGUIStyle, optionArray3);
                bool flag = this.behaviorDesignerWindow.IsReferencingField(field);
                Color backgroundColor = GUI.backgroundColor;
                if (flag)
                {
                    GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                }
                GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(80f) };
                if (GUILayout.Button(!flag ? "Select" : "Done", EditorStyles.miniButtonMid, optionArray4))
                {
                    if (this.behaviorDesignerWindow.IsReferencingTasks() && !flag)
                    {
                        this.behaviorDesignerWindow.ToggleReferenceTasks();
                    }
                    this.behaviorDesignerWindow.ToggleReferenceTasks(parentTask, field);
                }
                GUI.backgroundColor = backgroundColor;
                EditorGUILayout.EndHorizontal();
                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    IList list = field.GetValue(parentTask) as IList;
                    if ((list == null) || (list.Count == 0))
                    {
                        GUILayout.Label("No Tasks Referenced", BehaviorDesignerUtility.TaskInspectorGUIStyle, new GUILayoutOption[0]);
                    }
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
                            GUILayoutOption[] optionArray5 = new GUILayoutOption[] { GUILayout.Width(232f) };
                            GUILayout.Label((list[i] as Task).NodeData.NodeDesigner.ToString(), BehaviorDesignerUtility.TaskInspectorGUIStyle, optionArray5);
                            GUILayoutOption[] optionArray6 = new GUILayoutOption[] { GUILayout.Width(14f) };
                            if (GUILayout.Button(BehaviorDesignerUtility.DeleteButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray6))
                            {
                                this.ReferenceTasks(parentTask, ((list[i] as Task).NodeData.NodeDesigner as NodeDesigner).Task, field);
                                GUI.changed = true;
                            }
                            GUILayout.Space(3f);
                            GUILayoutOption[] optionArray7 = new GUILayoutOption[] { GUILayout.Width(14f) };
                            if (GUILayout.Button(BehaviorDesignerUtility.IdentifyButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray7))
                            {
                                this.behaviorDesignerWindow.IdentifyNode((list[i] as Task).NodeData.NodeDesigner as NodeDesigner);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    Task task2 = field.GetValue(parentTask) as Task;
                    GUILayoutOption[] optionArray8 = new GUILayoutOption[] { GUILayout.Width(232f) };
                    GUILayout.Label((task2 == null) ? "No Tasks Referenced" : task2.NodeData.NodeDesigner.ToString(), BehaviorDesignerUtility.TaskInspectorGUIStyle, optionArray8);
                    if (task2 != null)
                    {
                        GUILayoutOption[] optionArray9 = new GUILayoutOption[] { GUILayout.Width(14f) };
                        if (GUILayout.Button(BehaviorDesignerUtility.DeleteButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray9))
                        {
                            this.ReferenceTasks(task, (task2.NodeData.NodeDesigner as NodeDesigner).Task, field);
                            GUI.changed = true;
                        }
                        GUILayout.Space(3f);
                        GUILayoutOption[] optionArray10 = new GUILayoutOption[] { GUILayout.Width(14f) };
                        if (GUILayout.Button(BehaviorDesignerUtility.IdentifyButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray10))
                        {
                            this.behaviorDesignerWindow.IdentifyNode(task2.NodeData.NodeDesigner as NodeDesigner);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private bool DrawWatchedButton(Task task, System.Reflection.FieldInfo field)
        {
            GUILayout.Space(3f);
            bool flag = task.NodeData.ContainsWatchedField(field);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(15f) };
            if (!GUILayout.Button(!flag ? BehaviorDesignerUtility.VariableWatchButtonTexture : BehaviorDesignerUtility.VariableWatchButtonSelectedTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, options))
            {
                return false;
            }
            if (flag)
            {
                task.NodeData.RemoveWatchedField(field);
            }
            else
            {
                task.NodeData.AddWatchedField(field);
            }
            return true;
        }

        private MethodInfo GetInvokeMethodInfo(object task)
        {
            SharedVariable variable = task.GetType().GetField("targetGameObject").GetValue(task) as SharedVariable;
            GameObject gameObject = null;
            if ((variable == null) || (((GameObject) variable.GetValue()) == null))
            {
                if ((task as Task).Owner != null)
                {
                    gameObject = (task as Task).Owner.gameObject;
                }
            }
            else
            {
                gameObject = (GameObject) variable.GetValue();
            }
            if (gameObject == null)
            {
                return null;
            }
            SharedVariable variable2 = task.GetType().GetField("componentName").GetValue(task) as SharedVariable;
            if ((variable2 == null) || string.IsNullOrEmpty((string) variable2.GetValue()))
            {
                return null;
            }
            SharedVariable variable3 = task.GetType().GetField("methodName").GetValue(task) as SharedVariable;
            if ((variable3 == null) || string.IsNullOrEmpty((string) variable3.GetValue()))
            {
                return null;
            }
            List<System.Type> list = new List<System.Type>();
            SharedVariable variable4 = null;
            for (int i = 0; i < 4; i++)
            {
                variable4 = task.GetType().GetField("parameter" + (i + 1)).GetValue(task) as SharedVariable;
                if (variable4 == null)
                {
                    break;
                }
                list.Add(variable4.GetType().GetProperty("Value").PropertyType);
            }
            return gameObject.GetComponent(TaskUtility.GetTypeWithinAssembly((string) variable2.GetValue())).GetType().GetMethod((string) variable3.GetValue(), list.ToArray());
        }

        public static List<Task> GetReferencedTasks(Task task)
        {
            List<Task> list = new List<Task>();
            System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(task.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(SerializeField)))
                {
                    if (typeof(IList).IsAssignableFrom(allFields[i].FieldType) && (typeof(Task).IsAssignableFrom(allFields[i].FieldType.GetElementType()) || (allFields[i].FieldType.IsGenericType && typeof(Task).IsAssignableFrom(allFields[i].FieldType.GetGenericArguments()[0]))))
                    {
                        Task[] taskArray = allFields[i].GetValue(task) as Task[];
                        if (taskArray != null)
                        {
                            for (int j = 0; j < taskArray.Length; j++)
                            {
                                list.Add(taskArray[j]);
                            }
                        }
                    }
                    else if (allFields[i].FieldType.IsSubclassOf(typeof(Task)) && (allFields[i].GetValue(task) != null))
                    {
                        list.Add(allFields[i].GetValue(task) as Task);
                    }
                }
            }
            return ((list.Count <= 0) ? null : list);
        }

        public bool HasFocus()
        {
            return (GUIUtility.keyboardControl != 0);
        }

        private void InspectedTaskCallback(object obj)
        {
            if (this.mActiveMenuSelectionTask != null)
            {
                System.Reflection.FieldInfo field = this.mActiveMenuSelectionTask.GetType().GetField("conditionalTask");
                if (obj == null)
                {
                    field.SetValue(this.mActiveMenuSelectionTask, null);
                }
                else
                {
                    System.Type type = (System.Type) obj;
                    Task task = Activator.CreateInstance(type, true) as Task;
                    field.SetValue(this.mActiveMenuSelectionTask, task);
                    System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(type);
                    for (int i = 0; i < allFields.Length; i++)
                    {
                        if (((allFields[i].FieldType.IsSubclassOf(typeof(SharedVariable)) && !BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(HideInInspector))) && !BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(NonSerializedAttribute))) && ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(SerializeField))))
                        {
                            SharedVariable variable = Activator.CreateInstance(allFields[i].FieldType) as SharedVariable;
                            variable.IsShared = false;
                            allFields[i].SetValue(task, variable);
                        }
                    }
                }
            }
            BehaviorDesignerWindow.instance.SaveBehavior();
        }

        private string InvokeParameterName(object task, System.Reflection.FieldInfo field)
        {
            if (field.Name.Contains("parameter"))
            {
                MethodInfo invokeMethodInfo = null;
                invokeMethodInfo = this.GetInvokeMethodInfo(task);
                if (invokeMethodInfo == null)
                {
                    return field.Name;
                }
                System.Reflection.ParameterInfo[] parameters = invokeMethodInfo.GetParameters();
                int index = int.Parse(field.Name.Substring(9)) - 1;
                if (index < parameters.Length)
                {
                    return parameters[index].Name;
                }
            }
            return field.Name;
        }

        public bool IsActiveTaskArray()
        {
            return this.activeReferenceTaskFieldInfo.FieldType.IsArray;
        }

        public bool IsActiveTaskNull()
        {
            return (this.activeReferenceTaskFieldInfo.GetValue(this.activeReferenceTask) == null);
        }

        public static bool IsFieldLinked(System.Reflection.FieldInfo field)
        {
            return BehaviorDesignerUtility.HasAttribute(field, typeof(LinkedTaskAttribute));
        }

        private bool IsFieldReflectionTask(System.Type type)
        {
            return ((TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.GetFieldValue") || TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.SetFieldValue")) || TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.CompareFieldValue"));
        }

        private bool IsInvokeMethodTask(System.Type type)
        {
            return TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.InvokeMethod");
        }

        private bool IsPropertyReflectionTask(System.Type type)
        {
            return ((TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.GetPropertyValue") || TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.SetPropertyValue")) || TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.ComparePropertyValue"));
        }

        private bool IsReflectionGetterTask(System.Type type)
        {
            return (TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.GetFieldValue") || TaskUtility.CompareType(type, "BehaviorDesigner.Runtime.Tasks.GetPropertyValue"));
        }

        private bool IsReflectionTask(System.Type type)
        {
            return ((this.IsInvokeMethodTask(type) || this.IsFieldReflectionTask(type)) || this.IsPropertyReflectionTask(type));
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OpenHelpURL(Task task)
        {
            BehaviorDesigner.Runtime.Tasks.HelpURLAttribute[] attributeArray = null;
            if ((attributeArray = task.GetType().GetCustomAttributes(typeof(BehaviorDesigner.Runtime.Tasks.HelpURLAttribute), false) as BehaviorDesigner.Runtime.Tasks.HelpURLAttribute[]).Length > 0)
            {
                Application.OpenURL(attributeArray[0].URL);
            }
        }

        public static void OpenInFileEditor(object task)
        {
            MonoScript[] scriptArray = (MonoScript[]) Resources.FindObjectsOfTypeAll(typeof(MonoScript));
            for (int i = 0; i < scriptArray.Length; i++)
            {
                if (((scriptArray[i] != null) && (scriptArray[i].GetClass() != null)) && scriptArray[i].GetClass().Equals(task.GetType()))
                {
                    AssetDatabase.OpenAsset(scriptArray[i]);
                    break;
                }
            }
        }

        private void PerformFullSync(Task task)
        {
            List<Task> referencedTasks = GetReferencedTasks(task);
            if (referencedTasks != null)
            {
                System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(task.GetType());
                for (int i = 0; i < allFields.Length; i++)
                {
                    if (!IsFieldLinked(allFields[i]))
                    {
                        for (int j = 0; j < referencedTasks.Count; j++)
                        {
                            System.Reflection.FieldInfo field = referencedTasks[j].GetType().GetField(allFields[i].Name);
                            if (field != null)
                            {
                                field.SetValue(referencedTasks[j], allFields[i].GetValue(task));
                            }
                        }
                    }
                }
            }
        }

        public bool ReferenceTasks(Task referenceTask)
        {
            return this.ReferenceTasks(this.activeReferenceTask, referenceTask, this.activeReferenceTaskFieldInfo);
        }

        private bool ReferenceTasks(Task sourceTask, Task referenceTask, System.Reflection.FieldInfo sourceFieldInfo)
        {
            bool fullSync = false;
            bool doReference = false;
            if (!ReferenceTasks(sourceTask, referenceTask, sourceFieldInfo, ref fullSync, ref doReference, true, false))
            {
                return false;
            }
            (referenceTask.NodeData.NodeDesigner as NodeDesigner).ShowReferenceIcon = doReference;
            if (fullSync)
            {
                this.PerformFullSync(this.activeReferenceTask);
            }
            return true;
        }

        public static bool ReferenceTasks(Task sourceTask, Task referenceTask, System.Reflection.FieldInfo sourceFieldInfo, ref bool fullSync, ref bool doReference, bool synchronize, bool unreferenceAll)
        {
            if ((((referenceTask == null) || referenceTask.Equals(sourceTask)) || (!typeof(IList).IsAssignableFrom(sourceFieldInfo.FieldType) && !referenceTask.GetType().IsAssignableFrom(sourceFieldInfo.FieldType))) || (typeof(IList).IsAssignableFrom(sourceFieldInfo.FieldType) && ((sourceFieldInfo.FieldType.IsGenericType && !referenceTask.GetType().IsAssignableFrom(sourceFieldInfo.FieldType.GetGenericArguments()[0])) || (!sourceFieldInfo.FieldType.IsGenericType && !referenceTask.GetType().IsAssignableFrom(sourceFieldInfo.FieldType.GetElementType())))))
            {
                return false;
            }
            if (synchronize && !IsFieldLinked(sourceFieldInfo))
            {
                synchronize = false;
            }
            if (unreferenceAll)
            {
                sourceFieldInfo.SetValue(sourceTask, null);
                (sourceTask.NodeData.NodeDesigner as NodeDesigner).ShowReferenceIcon = false;
            }
            else
            {
                doReference = true;
                bool flag = false;
                if (typeof(IList).IsAssignableFrom(sourceFieldInfo.FieldType))
                {
                    System.Type elementType;
                    Task[] taskArray = sourceFieldInfo.GetValue(sourceTask) as Task[];
                    if (sourceFieldInfo.FieldType.IsArray)
                    {
                        elementType = sourceFieldInfo.FieldType.GetElementType();
                    }
                    else
                    {
                        System.Type fieldType = sourceFieldInfo.FieldType;
                        while (!fieldType.IsGenericType)
                        {
                            fieldType = fieldType.BaseType;
                        }
                        elementType = fieldType.GetGenericArguments()[0];
                    }
                    System.Type[] typeArguments = new System.Type[] { elementType };
                    IList list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
                    if (taskArray != null)
                    {
                        for (int i = 0; i < taskArray.Length; i++)
                        {
                            if (referenceTask.Equals(taskArray[i]))
                            {
                                doReference = false;
                            }
                            else
                            {
                                list.Add(taskArray[i]);
                            }
                        }
                    }
                    if (synchronize)
                    {
                        if ((taskArray != null) && (taskArray.Length > 0))
                        {
                            for (int j = 0; j < taskArray.Length; j++)
                            {
                                ReferenceTasks(taskArray[j], referenceTask, taskArray[j].GetType().GetField(sourceFieldInfo.Name), ref flag, ref doReference, false, false);
                                if (doReference)
                                {
                                    ReferenceTasks(referenceTask, taskArray[j], referenceTask.GetType().GetField(sourceFieldInfo.Name), ref flag, ref doReference, false, false);
                                }
                            }
                        }
                        else if (doReference)
                        {
                            taskArray = referenceTask.GetType().GetField(sourceFieldInfo.Name).GetValue(referenceTask) as Task[];
                            if (taskArray != null)
                            {
                                for (int k = 0; k < taskArray.Length; k++)
                                {
                                    list.Add(taskArray[k]);
                                    (taskArray[k].NodeData.NodeDesigner as NodeDesigner).ShowReferenceIcon = true;
                                    ReferenceTasks(taskArray[k], sourceTask, taskArray[k].GetType().GetField(sourceFieldInfo.Name), ref doReference, ref flag, false, false);
                                }
                                doReference = true;
                            }
                        }
                        ReferenceTasks(referenceTask, sourceTask, referenceTask.GetType().GetField(sourceFieldInfo.Name), ref flag, ref doReference, false, !doReference);
                    }
                    if (doReference)
                    {
                        list.Add(referenceTask);
                    }
                    if (sourceFieldInfo.FieldType.IsArray)
                    {
                        Array array = Array.CreateInstance(sourceFieldInfo.FieldType.GetElementType(), list.Count);
                        list.CopyTo(array, 0);
                        sourceFieldInfo.SetValue(sourceTask, array);
                    }
                    else
                    {
                        sourceFieldInfo.SetValue(sourceTask, list);
                    }
                }
                else
                {
                    Task task = sourceFieldInfo.GetValue(sourceTask) as Task;
                    doReference = !referenceTask.Equals(task);
                    if (IsFieldLinked(sourceFieldInfo) && (task != null))
                    {
                        ReferenceTasks(task, sourceTask, task.GetType().GetField(sourceFieldInfo.Name), ref flag, ref doReference, false, true);
                    }
                    if (synchronize)
                    {
                        ReferenceTasks(referenceTask, sourceTask, referenceTask.GetType().GetField(sourceFieldInfo.Name), ref flag, ref doReference, false, !doReference);
                    }
                    sourceFieldInfo.SetValue(sourceTask, !doReference ? null : referenceTask);
                }
                if (synchronize)
                {
                    (referenceTask.NodeData.NodeDesigner as NodeDesigner).ShowReferenceIcon = doReference;
                }
                fullSync = doReference && synchronize;
            }
            return true;
        }

        private void ResetTask(object task)
        {
            (task as Task).OnReset();
            List<System.Type> baseClasses = FieldInspector.GetBaseClasses(task.GetType());
            BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (int i = baseClasses.Count - 1; i > -1; i--)
            {
                System.Reflection.FieldInfo[] fields = baseClasses[i].GetFields(bindingAttr);
                for (int j = 0; j < fields.Length; j++)
                {
                    if (typeof(SharedVariable).IsAssignableFrom(fields[j].FieldType))
                    {
                        SharedVariable variable = fields[j].GetValue(task) as SharedVariable;
                        if ((TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) && (variable != null)) && !variable.IsShared)
                        {
                            variable.IsShared = true;
                        }
                    }
                }
            }
        }

        private void SecondaryReflectionSelectionCallback(object obj)
        {
            if (this.mActiveMenuSelectionTask != null)
            {
                SharedVariable variable = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")) as SharedVariable;
                System.Reflection.FieldInfo field = null;
                if (this.IsInvokeMethodTask(this.mActiveMenuSelectionTask.GetType()))
                {
                    this.ClearInvokeVariablesTask();
                    field = this.mActiveMenuSelectionTask.GetType().GetField("methodName");
                }
                else if (this.IsFieldReflectionTask(this.mActiveMenuSelectionTask.GetType()))
                {
                    field = this.mActiveMenuSelectionTask.GetType().GetField("fieldName");
                }
                else
                {
                    field = this.mActiveMenuSelectionTask.GetType().GetField("propertyName");
                }
                if (obj == null)
                {
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                }
                else if (this.IsInvokeMethodTask(this.mActiveMenuSelectionTask.GetType()))
                {
                    MethodInfo info2 = (MethodInfo) obj;
                    variable.SetValue(info2.Name);
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                    System.Reflection.ParameterInfo[] parameters = info2.GetParameters();
                    for (int i = 0; i < 4; i++)
                    {
                        System.Reflection.FieldInfo info3 = this.mActiveMenuSelectionTask.GetType().GetField("parameter" + (i + 1));
                        if (i < parameters.Length)
                        {
                            variable = Activator.CreateInstance(FieldInspector.FriendlySharedVariableName(parameters[i].ParameterType)) as SharedVariable;
                            info3.SetValue(this.mActiveMenuSelectionTask, variable);
                        }
                        else
                        {
                            info3.SetValue(this.mActiveMenuSelectionTask, null);
                        }
                    }
                    if (!info2.ReturnType.Equals(typeof(void)))
                    {
                        System.Reflection.FieldInfo info4 = this.mActiveMenuSelectionTask.GetType().GetField("storeResult");
                        variable = Activator.CreateInstance(FieldInspector.FriendlySharedVariableName(info2.ReturnType)) as SharedVariable;
                        variable.IsShared = true;
                        info4.SetValue(this.mActiveMenuSelectionTask, variable);
                    }
                }
                else if (this.IsFieldReflectionTask(this.mActiveMenuSelectionTask.GetType()))
                {
                    System.Reflection.FieldInfo info5 = (System.Reflection.FieldInfo) obj;
                    variable.SetValue(info5.Name);
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                    System.Reflection.FieldInfo info6 = this.mActiveMenuSelectionTask.GetType().GetField("fieldValue");
                    if (info6 == null)
                    {
                        info6 = this.mActiveMenuSelectionTask.GetType().GetField("compareValue");
                    }
                    variable = Activator.CreateInstance(FieldInspector.FriendlySharedVariableName(info5.FieldType)) as SharedVariable;
                    variable.IsShared = this.IsReflectionGetterTask(this.mActiveMenuSelectionTask.GetType());
                    info6.SetValue(this.mActiveMenuSelectionTask, variable);
                }
                else
                {
                    PropertyInfo info7 = (PropertyInfo) obj;
                    variable.SetValue(info7.Name);
                    field.SetValue(this.mActiveMenuSelectionTask, variable);
                    System.Reflection.FieldInfo info8 = this.mActiveMenuSelectionTask.GetType().GetField("propertyValue");
                    if (info8 == null)
                    {
                        info8 = this.mActiveMenuSelectionTask.GetType().GetField("compareValue");
                    }
                    variable = Activator.CreateInstance(FieldInspector.FriendlySharedVariableName(info7.PropertyType)) as SharedVariable;
                    variable.IsShared = this.IsReflectionGetterTask(this.mActiveMenuSelectionTask.GetType());
                    info8.SetValue(this.mActiveMenuSelectionTask, variable);
                }
            }
            BehaviorDesignerWindow.instance.SaveBehavior();
        }

        public static void SelectInProject(object task)
        {
            MonoScript[] scriptArray = (MonoScript[]) Resources.FindObjectsOfTypeAll(typeof(MonoScript));
            for (int i = 0; i < scriptArray.Length; i++)
            {
                if (((scriptArray[i] != null) && (scriptArray[i].GetClass() != null)) && scriptArray[i].GetClass().Equals(task.GetType()))
                {
                    Selection.activeObject = scriptArray[i];
                    break;
                }
            }
        }

        public void SetActiveReferencedTasks(Task referenceTask, System.Reflection.FieldInfo fieldInfo)
        {
            this.activeReferenceTask = referenceTask;
            this.activeReferenceTaskFieldInfo = fieldInfo;
        }

        private void SetTaskColor(object value)
        {
            TaskColor color = value as TaskColor;
            if (color.task.NodeData.ColorIndex != color.colorIndex)
            {
                color.task.NodeData.ColorIndex = color.colorIndex;
                BehaviorDesignerWindow.instance.SaveBehavior();
            }
        }

        private bool SharedVariableTypeExists(List<System.Type> sharedVariableTypes, System.Type type)
        {
            if (!type.IsEnum)
            {
                for (int i = 0; i < sharedVariableTypes.Count; i++)
                {
                    if (FieldInspector.FriendlySharedVariableName(type).Equals(sharedVariableTypes[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Task ActiveReferenceTask
        {
            get
            {
                return this.activeReferenceTask;
            }
        }

        public System.Reflection.FieldInfo ActiveReferenceTaskFieldInfo
        {
            get
            {
                return this.activeReferenceTaskFieldInfo;
            }
        }

        private class TaskColor
        {
            public int colorIndex;
            public Task task;

            public TaskColor(Task task, int colorIndex)
            {
                this.task = task;
                this.colorIndex = colorIndex;
            }
        }
    }
}

