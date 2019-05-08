namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    public class VariableInspector : ScriptableObject
    {
        private bool mFocusNameField;
        private static BehaviorSource mPropertyMappingBehaviorSource;
        private static GenericMenu mPropertyMappingMenu;
        private static SharedVariable mPropertyMappingVariable;
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
        [SerializeField]
        private float mVariableStartPosition = -1f;
        private int mVariableTypeIndex;
        private static string[] sharedVariableStrings;
        private static List<System.Type> sharedVariableTypes;
        private static Dictionary<string, int> sharedVariableTypesDict;

        private static int AddPropertyName(SharedVariable sharedVariable, GameObject gameObject, ref List<string> propertyNames, ref List<GameObject> propertyGameObjects, bool behaviorGameObject)
        {
            int count = -1;
            Component[] components = null;
            if (gameObject != null)
            {
                components = gameObject.GetComponents(typeof(Component));
                System.Type propertyType = sharedVariable.GetType().GetProperty("Value").PropertyType;
                for (int i = 0; i < components.Length; i++)
                {
                    PropertyInfo[] properties = components[i].GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    for (int j = 0; j < properties.Length; j++)
                    {
                        if (properties[j].PropertyType.Equals(propertyType) && !properties[j].IsSpecialName)
                        {
                            string item = components[i].GetType().FullName + "/" + properties[j].Name;
                            if (item.Equals(sharedVariable.PropertyMapping) && (object.Equals(sharedVariable.PropertyMappingOwner, gameObject) || (object.Equals(sharedVariable.PropertyMappingOwner, null) && behaviorGameObject)))
                            {
                                count = propertyNames.Count;
                            }
                            propertyNames.Add(item);
                            propertyGameObjects.Add(gameObject);
                        }
                    }
                }
            }
            return count;
        }

        private static bool AddVariable(IVariableSource variableSource, string variableName, int variableTypeIndex, bool fromGlobalVariablesWindow)
        {
            SharedVariable item = CreateVariable(variableTypeIndex, variableName, fromGlobalVariablesWindow);
            List<SharedVariable> list = (variableSource == null) ? null : variableSource.GetAllVariables();
            if (list == null)
            {
                list = new List<SharedVariable>();
            }
            list.Add(item);
            GUI.FocusControl("Add");
            if (fromGlobalVariablesWindow && (variableSource == null))
            {
                GlobalVariables asset = ScriptableObject.CreateInstance(typeof(GlobalVariables)) as GlobalVariables;
                string str = BehaviorDesignerUtility.GetEditorBaseDirectory(null).Substring(6, BehaviorDesignerUtility.GetEditorBaseDirectory(null).Length - 13);
                string str2 = str + "/Resources/BehaviorDesignerGlobalVariables.asset";
                if (!Directory.Exists(Application.dataPath + str + "/Resources"))
                {
                    Directory.CreateDirectory(Application.dataPath + str + "/Resources");
                }
                if (!File.Exists(Application.dataPath + str2))
                {
                    AssetDatabase.CreateAsset(asset, "Assets" + str2);
                    EditorUtility.DisplayDialog("Created Global Variables", "Behavior Designer Global Variables asset created:\n\nAssets" + str + "/Resources/BehaviorDesignerGlobalVariables.asset\n\nNote: Copy this file to transfer global variables between projects.", "OK");
                }
                variableSource = asset;
            }
            variableSource.SetAllVariables(list);
            return true;
        }

        private static bool CanNetworkSync(System.Type type)
        {
            if (((((type != typeof(bool)) && (type != typeof(Color))) && ((type != typeof(float)) && (type != typeof(GameObject)))) && (((type != typeof(int)) && (type != typeof(Quaternion))) && ((type != typeof(Rect)) && (type != typeof(string))))) && (((type != typeof(Transform)) && (type != typeof(Vector2))) && ((type != typeof(Vector3)) && (type != typeof(Vector4)))))
            {
                return false;
            }
            return true;
        }

        public bool ClearFocus(bool addVariable, BehaviorSource behaviorSource)
        {
            GUIUtility.keyboardControl = 0;
            bool flag = false;
            if ((addVariable && !string.IsNullOrEmpty(this.mVariableName)) && VariableNameValid(behaviorSource, this.mVariableName))
            {
                flag = AddVariable(behaviorSource, this.mVariableName, this.mVariableTypeIndex, false);
                this.mVariableName = string.Empty;
            }
            return flag;
        }

        private static SharedVariable CreateVariable(int index, string name, bool global)
        {
            SharedVariable variable = Activator.CreateInstance(sharedVariableTypes[index]) as SharedVariable;
            variable.Name = name;
            variable.IsShared = true;
            variable.IsGlobal = global;
            return variable;
        }

        public static bool DrawAllVariables(bool showFooter, IVariableSource variableSource, ref List<SharedVariable> variables, bool canSelect, ref List<float> variablePosition, ref int selectedVariableIndex, ref string selectedVariableName, ref int selectedVariableTypeIndex, bool drawRemoveButton, bool drawLastSeparator)
        {
            if (variables == null)
            {
                return false;
            }
            bool flag = false;
            if (canSelect && (variablePosition == null))
            {
                variablePosition = new List<float>();
            }
            for (int i = 0; i < variables.Count; i++)
            {
                SharedVariable sharedVariable = variables[i];
                if (sharedVariable == null)
                {
                    continue;
                }
                if (canSelect && (selectedVariableIndex == i))
                {
                    if (i == 0)
                    {
                        GUILayout.Space(2f);
                    }
                    bool deleted = false;
                    if (DrawSelectedVariable(variableSource, ref variables, sharedVariable, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex, ref deleted))
                    {
                        flag = true;
                    }
                    if (!deleted)
                    {
                        goto Label_01B1;
                    }
                    if (BehaviorDesignerWindow.instance != null)
                    {
                        BehaviorDesignerWindow.instance.RemoveSharedVariableReferences(sharedVariable);
                    }
                    variables.RemoveAt(i);
                    if (selectedVariableIndex == i)
                    {
                        selectedVariableIndex = -1;
                    }
                    else if (selectedVariableIndex > i)
                    {
                        selectedVariableIndex--;
                    }
                    flag = true;
                    break;
                }
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                if (DrawSharedVariable(variableSource, sharedVariable, false))
                {
                    flag = true;
                }
                if (drawRemoveButton)
                {
                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(19f) };
                    if (GUILayout.Button(BehaviorDesignerUtility.VariableDeleteButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, options) && EditorUtility.DisplayDialog("Delete Variable", "Are you sure you want to delete this variable?", "Yes", "No"))
                    {
                        if (BehaviorDesignerWindow.instance != null)
                        {
                            BehaviorDesignerWindow.instance.RemoveSharedVariableReferences(sharedVariable);
                        }
                        variables.RemoveAt(i);
                        if (canSelect)
                        {
                            if (selectedVariableIndex == i)
                            {
                                selectedVariableIndex = -1;
                            }
                            else if (selectedVariableIndex > i)
                            {
                                selectedVariableIndex--;
                            }
                        }
                        flag = true;
                        break;
                    }
                }
                GUILayout.Space(10f);
                GUILayout.EndHorizontal();
                if ((i != (variables.Count - 1)) || drawLastSeparator)
                {
                    BehaviorDesignerUtility.DrawContentSeperator(2, 7);
                }
            Label_01B1:
                GUILayout.Space(4f);
                if (canSelect && (Event.current.type == EventType.Repaint))
                {
                    if (variablePosition.Count <= i)
                    {
                        variablePosition.Add(GUILayoutUtility.GetLastRect().yMax);
                    }
                    else
                    {
                        variablePosition[i] = GUILayoutUtility.GetLastRect().yMax;
                    }
                }
            }
            if (canSelect && (variables.Count < variablePosition.Count))
            {
                for (int j = variablePosition.Count - 1; j >= variables.Count; j--)
                {
                    variablePosition.RemoveAt(j);
                }
            }
            if (showFooter && (variables.Count > 0))
            {
                GUI.enabled = true;
                GUILayout.Label("Select a variable to change its properties.", BehaviorDesignerUtility.LabelWrapGUIStyle, new GUILayoutOption[0]);
            }
            return flag;
        }

        private static bool DrawHeader(IVariableSource variableSource, bool fromGlobalVariablesWindow, ref float variableStartPosition, ref string variableName, ref bool focusNameField, ref int variableTypeIndex, ref int selectedVariableIndex, ref string selectedVariableName, ref int selectedVariableTypeIndex)
        {
            if (sharedVariableStrings == null)
            {
                FindAllSharedVariableTypes(true);
            }
            EditorGUIUtility.labelWidth = 150f;
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space(4f);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(70f) };
            EditorGUILayout.LabelField("Name", options);
            GUI.SetNextControlName("Name");
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(212f) };
            variableName = EditorGUILayout.TextField(variableName, optionArray2);
            if (focusNameField)
            {
                GUI.FocusControl("Name");
                focusNameField = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space(4f);
            GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(70f) };
            GUILayout.Label("Type", optionArray3);
            GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(163f) };
            variableTypeIndex = EditorGUILayout.Popup(variableTypeIndex, sharedVariableStrings, EditorStyles.toolbarPopup, optionArray4);
            GUILayout.Space(8f);
            bool flag = false;
            bool flag2 = VariableNameValid(variableSource, variableName);
            bool enabled = GUI.enabled;
            GUI.enabled = flag2 && enabled;
            GUI.SetNextControlName("Add");
            GUILayoutOption[] optionArray5 = new GUILayoutOption[] { GUILayout.Width(40f) };
            if (GUILayout.Button("Add", EditorStyles.toolbarButton, optionArray5) && flag2)
            {
                flag = AddVariable(variableSource, variableName, variableTypeIndex, fromGlobalVariablesWindow);
                if (flag)
                {
                    selectedVariableIndex = variableSource.GetAllVariables().Count - 1;
                    selectedVariableName = variableName;
                    selectedVariableTypeIndex = variableTypeIndex;
                    variableName = string.Empty;
                }
            }
            GUILayout.Space(6f);
            GUILayout.EndHorizontal();
            if (!fromGlobalVariablesWindow)
            {
                GUI.enabled = true;
                GUILayout.Space(3f);
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Space(5f);
                GUILayoutOption[] optionArray6 = new GUILayoutOption[] { GUILayout.Width(284f) };
                if (GUILayout.Button("Global Variables", EditorStyles.toolbarButton, optionArray6))
                {
                    GlobalVariablesWindow.ShowWindow();
                }
                GUILayout.EndHorizontal();
            }
            BehaviorDesignerUtility.DrawContentSeperator(2);
            GUILayout.Space(4f);
            if ((variableStartPosition == -1f) && (Event.current.type == EventType.Repaint))
            {
                variableStartPosition = GUILayoutUtility.GetLastRect().yMax;
            }
            GUI.enabled = enabled;
            return flag;
        }

        private static bool DrawSelectedVariable(IVariableSource variableSource, ref List<SharedVariable> variables, SharedVariable sharedVariable, ref int selectedVariableIndex, ref string selectedVariableName, ref int selectedVariableTypeIndex, ref bool deleted)
        {
            bool flag = false;
            GUILayout.BeginVertical(BehaviorDesignerUtility.SelectedBackgroundGUIStyle, new GUILayoutOption[0]);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(70f) };
            GUILayout.Label("Name", options);
            EditorGUI.BeginChangeCheck();
            GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(140f) };
            selectedVariableName = GUILayout.TextField(selectedVariableName, optionArray2);
            if (EditorGUI.EndChangeCheck())
            {
                if (VariableNameValid(variableSource, selectedVariableName))
                {
                    variableSource.UpdateVariableName(sharedVariable, selectedVariableName);
                }
                flag = true;
            }
            GUILayout.Space(10f);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && (selectedVariableIndex < (variables.Count - 1));
            GUILayoutOption[] optionArray3 = new GUILayoutOption[] { GUILayout.Width(19f) };
            if (GUILayout.Button(BehaviorDesignerUtility.DownArrowButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray3))
            {
                SharedVariable variable = variables[selectedVariableIndex + 1];
                variables[selectedVariableIndex + 1] = variables[selectedVariableIndex];
                variables[selectedVariableIndex] = variable;
                selectedVariableIndex++;
                flag = true;
            }
            GUI.enabled = enabled && ((selectedVariableIndex < (variables.Count - 1)) || (selectedVariableIndex != 0));
            GUILayoutOption[] optionArray4 = new GUILayoutOption[] { GUILayout.Width(1f), GUILayout.Height(18f) };
            GUILayout.Box(string.Empty, BehaviorDesignerUtility.ArrowSeparatorGUIStyle, optionArray4);
            GUI.enabled = enabled && (selectedVariableIndex != 0);
            GUILayoutOption[] optionArray5 = new GUILayoutOption[] { GUILayout.Width(20f) };
            if (GUILayout.Button(BehaviorDesignerUtility.UpArrowButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray5))
            {
                SharedVariable variable2 = variables[selectedVariableIndex - 1];
                variables[selectedVariableIndex - 1] = variables[selectedVariableIndex];
                variables[selectedVariableIndex] = variable2;
                selectedVariableIndex--;
                flag = true;
            }
            GUI.enabled = enabled;
            GUILayoutOption[] optionArray6 = new GUILayoutOption[] { GUILayout.Width(19f) };
            if (GUILayout.Button(BehaviorDesignerUtility.VariableDeleteButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, optionArray6) && EditorUtility.DisplayDialog("Delete Variable", "Are you sure you want to delete this variable?", "Yes", "No"))
            {
                deleted = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayoutOption[] optionArray7 = new GUILayoutOption[] { GUILayout.Width(70f) };
            GUILayout.Label("Type", optionArray7);
            EditorGUI.BeginChangeCheck();
            GUILayoutOption[] optionArray8 = new GUILayoutOption[] { GUILayout.Width(200f) };
            selectedVariableTypeIndex = EditorGUILayout.Popup(selectedVariableTypeIndex, sharedVariableStrings, EditorStyles.toolbarPopup, optionArray8);
            if (EditorGUI.EndChangeCheck() && (sharedVariableTypesDict[sharedVariable.GetType().Name] != selectedVariableTypeIndex))
            {
                if (BehaviorDesignerWindow.instance != null)
                {
                    BehaviorDesignerWindow.instance.RemoveSharedVariableReferences(sharedVariable);
                }
                sharedVariable = CreateVariable(selectedVariableTypeIndex, sharedVariable.Name, sharedVariable.IsGlobal);
                variables[selectedVariableIndex] = sharedVariable;
                flag = true;
            }
            GUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUI.enabled = CanNetworkSync(sharedVariable.GetType().GetProperty("Value").PropertyType);
            EditorGUI.BeginChangeCheck();
            sharedVariable.NetworkSync = EditorGUILayout.Toggle(new GUIContent("Network Sync", "Sync this variable over the network. Requires Unity 5.1 or greator. A NetworkIdentity must be attached to the behavior tree GameObject."), sharedVariable.NetworkSync, new GUILayoutOption[0]);
            if (EditorGUI.EndChangeCheck())
            {
                flag = true;
            }
            GUILayout.EndHorizontal();
            GUI.enabled = enabled;
            if (DrawSharedVariable(variableSource, sharedVariable, true))
            {
                flag = true;
            }
            BehaviorDesignerUtility.DrawContentSeperator(4, 7);
            GUILayout.EndVertical();
            GUILayout.Space(3f);
            return flag;
        }

        private static bool DrawSharedVariable(IVariableSource variableSource, SharedVariable sharedVariable, bool selected)
        {
            if ((sharedVariable == null) || (sharedVariable.GetType().GetProperty("Value") == null))
            {
                return false;
            }
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            bool flag = false;
            if (!string.IsNullOrEmpty(sharedVariable.PropertyMapping))
            {
                if (selected)
                {
                    GUILayout.Label("Property", new GUILayoutOption[0]);
                }
                else
                {
                    GUILayout.Label(sharedVariable.Name, new GUILayoutOption[0]);
                }
                char[] separator = new char[] { '.' };
                string[] strArray = sharedVariable.PropertyMapping.Split(separator);
                GUILayout.Label(strArray[strArray.Length - 1].Replace('/', '.'), new GUILayoutOption[0]);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                FieldInspector.DrawFields(null, sharedVariable, new GUIContent(sharedVariable.Name));
                flag = EditorGUI.EndChangeCheck();
            }
            if (!sharedVariable.IsGlobal)
            {
                GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(19f) };
                if (GUILayout.Button(BehaviorDesignerUtility.VariableMapButtonTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, options))
                {
                    ShowPropertyMappingMenu(variableSource as BehaviorSource, sharedVariable);
                }
            }
            GUILayout.EndHorizontal();
            return flag;
        }

        public bool DrawVariables(BehaviorSource behaviorSource, bool enabled)
        {
            return DrawVariables(behaviorSource, enabled, behaviorSource, ref this.mVariableName, ref this.mFocusNameField, ref this.mVariableTypeIndex, ref this.mScrollPosition, ref this.mVariablePosition, ref this.mVariableStartPosition, ref this.mSelectedVariableIndex, ref this.mSelectedVariableName, ref this.mSelectedVariableTypeIndex);
        }

        public static bool DrawVariables(IVariableSource variableSource, bool enabled, BehaviorSource behaviorSource, ref string variableName, ref bool focusNameField, ref int variableTypeIndex, ref Vector2 scrollPosition, ref List<float> variablePosition, ref float variableStartPosition, ref int selectedVariableIndex, ref string selectedVariableName, ref int selectedVariableTypeIndex)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[0]);
            GUI.enabled = enabled;
            bool flag = false;
            bool flag2 = false;
            List<SharedVariable> variables = (variableSource == null) ? null : variableSource.GetAllVariables();
            if ((!Application.isPlaying && (behaviorSource != null)) && (behaviorSource.Owner is Behavior))
            {
                Behavior owner = behaviorSource.Owner as Behavior;
                if (owner.ExternalBehavior != null)
                {
                    flag2 = true;
                    GUI.enabled = false;
                    BehaviorSource source = owner.GetBehaviorSource();
                    source.CheckForSerialization(true, null);
                    if (DrawHeader(source, false, ref variableStartPosition, ref variableName, ref focusNameField, ref variableTypeIndex, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex))
                    {
                        flag = true;
                    }
                    GUI.enabled = enabled;
                    if (SyncVariables(source, variables))
                    {
                        flag = true;
                    }
                    List<SharedVariable> allVariables = source.GetAllVariables();
                    if (DrawAllVariables(true, behaviorSource, ref allVariables, false, ref variablePosition, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex, false, true))
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
                        {
                            BinarySerialization.Save(source);
                        }
                        else
                        {
                            SerializeJSON.Save(source);
                        }
                    }
                    ExternalBehavior externalBehavior = (behaviorSource.Owner as Behavior).ExternalBehavior;
                    externalBehavior.BehaviorSource.Owner = externalBehavior;
                    externalBehavior.BehaviorSource.CheckForSerialization(true, behaviorSource);
                }
            }
            if (!flag2)
            {
                if (DrawHeader(variableSource, behaviorSource == null, ref variableStartPosition, ref variableName, ref focusNameField, ref variableTypeIndex, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex))
                {
                    flag = true;
                }
                variables = (variableSource == null) ? null : variableSource.GetAllVariables();
                if ((variables != null) && (variables.Count > 0))
                {
                    GUI.enabled = enabled && !flag2;
                    if (DrawAllVariables(true, variableSource, ref variables, true, ref variablePosition, ref selectedVariableIndex, ref selectedVariableName, ref selectedVariableTypeIndex, true, true))
                    {
                        flag = true;
                    }
                }
                if (flag && (variableSource != null))
                {
                    variableSource.SetAllVariables(variables);
                }
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();
            return flag;
        }

        public static List<System.Type> FindAllSharedVariableTypes(bool removeShared)
        {
            if (sharedVariableTypes == null)
            {
                sharedVariableTypes = new List<System.Type>();
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    System.Type[] types = assemblies[i].GetTypes();
                    for (int k = 0; k < types.Length; k++)
                    {
                        if (types[k].IsSubclassOf(typeof(SharedVariable)) && !types[k].IsAbstract)
                        {
                            sharedVariableTypes.Add(types[k]);
                        }
                    }
                }
                sharedVariableTypes.Sort(new AlphanumComparator<System.Type>());
                sharedVariableStrings = new string[sharedVariableTypes.Count];
                sharedVariableTypesDict = new Dictionary<string, int>();
                for (int j = 0; j < sharedVariableTypes.Count; j++)
                {
                    string name = sharedVariableTypes[j].Name;
                    sharedVariableTypesDict.Add(name, j);
                    if ((removeShared && (name.Length > 6)) && name.Substring(0, 6).Equals("Shared"))
                    {
                        name = name.Substring(6, name.Length - 6);
                    }
                    sharedVariableStrings[j] = name;
                }
            }
            return sharedVariableTypes;
        }

        public void FocusNameField()
        {
            this.mFocusNameField = true;
        }

        private static string GetFullPath(Transform transform)
        {
            if (transform.parent == null)
            {
                return transform.name;
            }
            return (GetFullPath(transform.parent) + "/" + transform.name);
        }

        public bool HasFocus()
        {
            return (GUIUtility.keyboardControl != 0);
        }

        public bool LeftMouseDown(IVariableSource variableSource, BehaviorSource behaviorSource, Vector2 mousePosition)
        {
            return LeftMouseDown(variableSource, behaviorSource, mousePosition, this.mVariablePosition, this.mVariableStartPosition, this.mScrollPosition, ref this.mSelectedVariableIndex, ref this.mSelectedVariableName, ref this.mSelectedVariableTypeIndex);
        }

        public static bool LeftMouseDown(IVariableSource variableSource, BehaviorSource behaviorSource, Vector2 mousePosition, List<float> variablePosition, float variableStartPosition, Vector2 scrollPosition, ref int selectedVariableIndex, ref string selectedVariableName, ref int selectedVariableTypeIndex)
        {
            if (((variablePosition != null) && (mousePosition.y > variableStartPosition)) && (variableSource != null))
            {
                List<SharedVariable> allVariables = null;
                if ((!Application.isPlaying && (behaviorSource != null)) && (behaviorSource.Owner is Behavior))
                {
                    Behavior owner = behaviorSource.Owner as Behavior;
                    if (owner.ExternalBehavior != null)
                    {
                        BehaviorSource source = owner.GetBehaviorSource();
                        source.CheckForSerialization(true, null);
                        allVariables = source.GetAllVariables();
                        ExternalBehavior externalBehavior = owner.ExternalBehavior;
                        externalBehavior.BehaviorSource.Owner = externalBehavior;
                        externalBehavior.BehaviorSource.CheckForSerialization(true, behaviorSource);
                    }
                    else
                    {
                        allVariables = variableSource.GetAllVariables();
                    }
                }
                else
                {
                    allVariables = variableSource.GetAllVariables();
                }
                if ((allVariables == null) || (allVariables.Count != variablePosition.Count))
                {
                    return false;
                }
                for (int i = 0; i < variablePosition.Count; i++)
                {
                    if (mousePosition.y < (variablePosition[i] - scrollPosition.y))
                    {
                        if (i == selectedVariableIndex)
                        {
                            return false;
                        }
                        selectedVariableIndex = i;
                        selectedVariableName = allVariables[i].Name;
                        selectedVariableTypeIndex = sharedVariableTypesDict[allVariables[i].GetType().Name];
                        return true;
                    }
                }
            }
            if (selectedVariableIndex != -1)
            {
                selectedVariableIndex = -1;
                return true;
            }
            return false;
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void PropertySelected(object selected)
        {
            bool flag = false;
            if ((!Application.isPlaying && (mPropertyMappingBehaviorSource.Owner.GetObject() is Behavior)) && ((mPropertyMappingBehaviorSource.Owner.GetObject() as Behavior).ExternalBehavior != null))
            {
                mPropertyMappingBehaviorSource.CheckForSerialization(true, null);
                mPropertyMappingVariable = mPropertyMappingBehaviorSource.GetVariable(mPropertyMappingVariable.Name);
                flag = true;
            }
            SelectedPropertyMapping mapping = selected as SelectedPropertyMapping;
            if (mapping.Property.Equals("None"))
            {
                mPropertyMappingVariable.PropertyMapping = string.Empty;
                mPropertyMappingVariable.PropertyMappingOwner = null;
            }
            else
            {
                mPropertyMappingVariable.PropertyMapping = mapping.Property;
                mPropertyMappingVariable.PropertyMappingOwner = mapping.GameObject;
            }
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
            {
                BinarySerialization.Save(mPropertyMappingBehaviorSource);
            }
            else
            {
                SerializeJSON.Save(mPropertyMappingBehaviorSource);
            }
            if (flag)
            {
                ExternalBehavior externalBehavior = (mPropertyMappingBehaviorSource.Owner as Behavior).ExternalBehavior;
                externalBehavior.BehaviorSource.Owner = externalBehavior;
                externalBehavior.BehaviorSource.CheckForSerialization(true, mPropertyMappingBehaviorSource);
            }
        }

        public void ResetSelectedVariableIndex()
        {
            this.mSelectedVariableIndex = -1;
            this.mVariableStartPosition = -1f;
            if (this.mVariablePosition != null)
            {
                this.mVariablePosition.Clear();
            }
        }

        private static void ShowPropertyMappingMenu(BehaviorSource behaviorSource, SharedVariable sharedVariable)
        {
            mPropertyMappingVariable = sharedVariable;
            mPropertyMappingBehaviorSource = behaviorSource;
            mPropertyMappingMenu = new GenericMenu();
            List<string> propertyNames = new List<string>();
            List<GameObject> propertyGameObjects = new List<GameObject> { "None", null };
            int num = 0;
            if (behaviorSource.Owner.GetObject() is Behavior)
            {
                GameObject gameObject = (behaviorSource.Owner.GetObject() as Behavior).gameObject;
                int num2 = AddPropertyName(sharedVariable, gameObject, ref propertyNames, ref propertyGameObjects, true);
                if (num2 != -1)
                {
                    num = num2;
                }
                GameObject[] objArray = UnityEngine.Object.FindObjectsOfType<GameObject>();
                for (int j = 0; j < objArray.Length; j++)
                {
                    if (!objArray[j].Equals(gameObject) && ((num2 = AddPropertyName(sharedVariable, objArray[j], ref propertyNames, ref propertyGameObjects, false)) != -1))
                    {
                        num = num2;
                    }
                }
            }
            for (int i = 0; i < propertyNames.Count; i++)
            {
                char[] separator = new char[] { '.' };
                string[] strArray = propertyNames[i].Split(separator);
                if (propertyGameObjects[i] != null)
                {
                    strArray[strArray.Length - 1] = GetFullPath(propertyGameObjects[i].transform) + "/" + strArray[strArray.Length - 1];
                }
                mPropertyMappingMenu.AddItem(new GUIContent(strArray[strArray.Length - 1]), i == num, new GenericMenu.MenuFunction2(VariableInspector.PropertySelected), new SelectedPropertyMapping(propertyNames[i], propertyGameObjects[i]));
            }
            mPropertyMappingMenu.ShowAsContext();
        }

        public static bool SyncVariables(BehaviorSource localBehaviorSource, List<SharedVariable> variables)
        {
            if (variables == null)
            {
                return false;
            }
            bool flag = false;
            List<SharedVariable> allVariables = localBehaviorSource.GetAllVariables();
            if (allVariables == null)
            {
                allVariables = new List<SharedVariable>();
                localBehaviorSource.SetAllVariables(allVariables);
                flag = true;
            }
            for (int i = 0; i < variables.Count; i++)
            {
                if ((allVariables.Count - 1) < i)
                {
                    SharedVariable item = Activator.CreateInstance(variables[i].GetType()) as SharedVariable;
                    item.Name = variables[i].Name;
                    item.IsShared = true;
                    item.SetValue(variables[i].GetValue());
                    allVariables.Add(item);
                    flag = true;
                }
                else if ((allVariables[i].Name != variables[i].Name) || (allVariables[i].GetType() != variables[i].GetType()))
                {
                    SharedVariable variable2 = Activator.CreateInstance(variables[i].GetType()) as SharedVariable;
                    variable2.Name = variables[i].Name;
                    variable2.IsShared = true;
                    variable2.SetValue(variables[i].GetValue());
                    allVariables[i] = variable2;
                    flag = true;
                }
            }
            for (int j = allVariables.Count - 1; j > (variables.Count - 1); j--)
            {
                allVariables.RemoveAt(j);
                flag = true;
            }
            return flag;
        }

        private static bool VariableNameValid(IVariableSource variableSource, string variableName)
        {
            return (!variableName.Equals(string.Empty) && ((variableSource == null) || (variableSource.GetVariable(variableName) == null)));
        }

        private class SelectedPropertyMapping
        {
            private UnityEngine.GameObject mGameObject;
            private string mProperty;

            public SelectedPropertyMapping(string property, UnityEngine.GameObject gameObject)
            {
                this.mProperty = property;
                this.mGameObject = gameObject;
            }

            public UnityEngine.GameObject GameObject
            {
                get
                {
                    return this.mGameObject;
                }
            }

            public string Property
            {
                get
                {
                    return this.mProperty;
                }
            }
        }
    }
}

