namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using UnityEditor;
    using UnityEngine;

    public static class FieldInspector
    {
        public static BehaviorSource behaviorSource;
        private static int currentKeyboardControl = -1;
        private static HashSet<int> drawnObjects = new HashSet<int>();
        private static bool editingArray = false;
        private static int editingFieldHash;
        private static Dictionary<int, bool> foldoutDictionary = new Dictionary<int, bool>();
        private static string[] layerNames;
        private static int[] maskValues;
        private static int savedArraySize = -1;

        private static object DrawArrayField(Task task, GUIContent guiContent, System.Reflection.FieldInfo fieldInfo, System.Type fieldType, object value)
        {
            System.Type elementType;
            IList list;
            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
            }
            else
            {
                System.Type baseType = fieldType;
                while (!baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }
                elementType = baseType.GetGenericArguments()[0];
            }
            if (value == null)
            {
                if (fieldType.IsGenericType || fieldType.IsArray)
                {
                    System.Type[] typeArguments = new System.Type[] { elementType };
                    list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArguments), true) as IList;
                }
                else
                {
                    list = Activator.CreateInstance(fieldType, true) as IList;
                }
                if (fieldType.IsArray)
                {
                    Array array = Array.CreateInstance(elementType, list.Count);
                    list.CopyTo(array, 0);
                    list = array;
                }
                GUI.changed = true;
            }
            else
            {
                list = (IList) value;
            }
            EditorGUILayout.BeginVertical(new GUILayoutOption[0]);
            if (DrawFoldout(list.GetHashCode(), guiContent))
            {
                EditorGUI.indentLevel++;
                bool flag = guiContent.text.GetHashCode() == editingFieldHash;
                int num = !flag ? list.Count : savedArraySize;
                int length = EditorGUILayout.IntField("Size", num, new GUILayoutOption[0]);
                if ((!flag || !editingArray) || ((GUIUtility.keyboardControl == currentKeyboardControl) && (Event.current.keyCode != KeyCode.Return)))
                {
                    if (length != num)
                    {
                        if (!editingArray)
                        {
                            currentKeyboardControl = GUIUtility.keyboardControl;
                            editingArray = true;
                            editingFieldHash = guiContent.text.GetHashCode();
                        }
                        savedArraySize = length;
                    }
                }
                else
                {
                    if (length != list.Count)
                    {
                        Array array2 = Array.CreateInstance(elementType, length);
                        int num3 = -1;
                        for (int j = 0; j < length; j++)
                        {
                            if (j < list.Count)
                            {
                                num3 = j;
                            }
                            if (num3 == -1)
                            {
                                break;
                            }
                            array2.SetValue(list[num3], j);
                        }
                        if (fieldType.IsArray)
                        {
                            list = array2;
                        }
                        else
                        {
                            if (fieldType.IsGenericType)
                            {
                                System.Type[] typeArray2 = new System.Type[] { elementType };
                                list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArray2), true) as IList;
                            }
                            else
                            {
                                list = Activator.CreateInstance(fieldType, true) as IList;
                            }
                            for (int k = 0; k < array2.Length; k++)
                            {
                                list.Add(array2.GetValue(k));
                            }
                        }
                    }
                    editingArray = false;
                    savedArraySize = -1;
                    editingFieldHash = -1;
                    GUI.changed = true;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    guiContent.text = "Element " + i;
                    list[i] = DrawField(task, guiContent, fieldInfo, elementType, list[i]);
                    GUILayout.Space(6f);
                    GUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            return list;
        }

        public static object DrawField(Task task, GUIContent guiContent, System.Reflection.FieldInfo field, object value)
        {
            ObjectDrawer objectDrawer = null;
            ObjectDrawerAttribute[] attributeArray = null;
            objectDrawer = ObjectDrawerUtility.GetObjectDrawer(task, field);
            if (objectDrawer != null)
            {
                if ((value == null) && !field.FieldType.IsAbstract)
                {
                    value = Activator.CreateInstance(field.FieldType, true);
                }
                objectDrawer.Value = value;
                objectDrawer.OnGUI(guiContent);
                if (objectDrawer.Value != value)
                {
                    value = objectDrawer.Value;
                    GUI.changed = true;
                }
                return value;
            }
            if (((attributeArray = field.GetCustomAttributes(typeof(ObjectDrawerAttribute), true) as ObjectDrawerAttribute[]).Length <= 0) || ((objectDrawer = ObjectDrawerUtility.GetObjectDrawer(task, attributeArray[0])) == null))
            {
                return DrawField(task, guiContent, field, field.FieldType, value);
            }
            if (value == null)
            {
                value = Activator.CreateInstance(field.FieldType, true);
            }
            objectDrawer.Value = value;
            objectDrawer.OnGUI(guiContent);
            if (objectDrawer.Value != value)
            {
                value = objectDrawer.Value;
                GUI.changed = true;
            }
            return value;
        }

        private static object DrawField(Task task, GUIContent guiContent, System.Reflection.FieldInfo fieldInfo, System.Type fieldType, object value)
        {
            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                return DrawArrayField(task, guiContent, fieldInfo, fieldType, value);
            }
            return DrawSingleField(task, guiContent, fieldInfo, fieldType, value);
        }

        public static object DrawFields(Task task, object obj)
        {
            return DrawFields(task, obj, null);
        }

        public static object DrawFields(Task task, object obj, GUIContent guiContent)
        {
            if (obj == null)
            {
                return null;
            }
            List<System.Type> baseClasses = GetBaseClasses(obj.GetType());
            BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (int i = baseClasses.Count - 1; i > -1; i--)
            {
                System.Reflection.FieldInfo[] fields = baseClasses[i].GetFields(bindingAttr);
                for (int j = 0; j < fields.Length; j++)
                {
                    if (((!BehaviorDesignerUtility.HasAttribute(fields[j], typeof(NonSerializedAttribute)) && !BehaviorDesignerUtility.HasAttribute(fields[j], typeof(HideInInspector))) && ((!fields[j].IsPrivate && !fields[j].IsFamily) || BehaviorDesignerUtility.HasAttribute(fields[j], typeof(SerializeField)))) && (!(obj is ParentTask) || !fields[j].Name.Equals("children")))
                    {
                        if (guiContent == null)
                        {
                            BehaviorDesigner.Runtime.Tasks.TooltipAttribute[] attributeArray = null;
                            string name = fields[j].Name;
                            if ((attributeArray = fields[j].GetCustomAttributes(typeof(BehaviorDesigner.Runtime.Tasks.TooltipAttribute), false) as BehaviorDesigner.Runtime.Tasks.TooltipAttribute[]).Length > 0)
                            {
                                guiContent = new GUIContent(BehaviorDesignerUtility.SplitCamelCase(name), attributeArray[0].Tooltip);
                            }
                            else
                            {
                                guiContent = new GUIContent(BehaviorDesignerUtility.SplitCamelCase(name));
                            }
                        }
                        EditorGUI.BeginChangeCheck();
                        object obj2 = DrawField(task, guiContent, fields[j], fields[j].GetValue(obj));
                        if (EditorGUI.EndChangeCheck())
                        {
                            fields[j].SetValue(obj, obj2);
                            GUI.changed = true;
                        }
                        guiContent = null;
                    }
                }
            }
            return obj;
        }

        public static bool DrawFoldout(int hash, GUIContent guiContent)
        {
            bool foldout = FoldOut(hash);
            bool flag2 = EditorGUILayout.Foldout(foldout, guiContent);
            if (flag2 != foldout)
            {
                SetFoldOut(hash, flag2);
            }
            return flag2;
        }

        private static LayerMask DrawLayerMask(GUIContent guiContent, LayerMask layerMask)
        {
            if (layerNames == null)
            {
                InitLayers();
            }
            int mask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                if ((layerMask.value & maskValues[i]) == maskValues[i])
                {
                    mask |= ((int) 1) << i;
                }
            }
            int num3 = EditorGUILayout.MaskField(guiContent, mask, layerNames, new GUILayoutOption[0]);
            if (num3 != mask)
            {
                mask = 0;
                for (int j = 0; j < layerNames.Length; j++)
                {
                    if ((num3 & (((int) 1) << j)) != 0)
                    {
                        mask |= maskValues[j];
                    }
                }
                layerMask.value = mask;
            }
            return layerMask;
        }

        public static SharedVariable DrawSharedVariable(Task task, GUIContent guiContent, System.Reflection.FieldInfo fieldInfo, System.Type fieldType, SharedVariable sharedVariable)
        {
            if (!fieldType.Equals(typeof(SharedVariable)) && (sharedVariable == null))
            {
                sharedVariable = Activator.CreateInstance(fieldType, true) as SharedVariable;
                if (TaskUtility.HasAttribute(fieldInfo, typeof(RequiredFieldAttribute)) || TaskUtility.HasAttribute(fieldInfo, typeof(SharedRequiredAttribute)))
                {
                    sharedVariable.IsShared = true;
                }
                GUI.changed = true;
            }
            if ((sharedVariable == null) || sharedVariable.IsShared)
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                string[] names = null;
                int globalStartIndex = -1;
                int selectedIndex = GetVariablesOfType((sharedVariable == null) ? null : sharedVariable.GetType().GetProperty("Value").PropertyType, (sharedVariable != null) && sharedVariable.IsGlobal, (sharedVariable == null) ? string.Empty : sharedVariable.Name, behaviorSource, out names, ref globalStartIndex, fieldType.Equals(typeof(SharedVariable)));
                Color backgroundColor = GUI.backgroundColor;
                if ((selectedIndex == 0) && !TaskUtility.HasAttribute(fieldInfo, typeof(SharedRequiredAttribute)))
                {
                    GUI.backgroundColor = Color.red;
                }
                int num3 = selectedIndex;
                selectedIndex = EditorGUILayout.Popup(guiContent.text, selectedIndex, names, BehaviorDesignerUtility.SharedVariableToolbarPopup, new GUILayoutOption[0]);
                GUI.backgroundColor = backgroundColor;
                if (selectedIndex != num3)
                {
                    if (selectedIndex == 0)
                    {
                        if (fieldType.Equals(typeof(SharedVariable)))
                        {
                            sharedVariable = null;
                        }
                        else
                        {
                            sharedVariable = Activator.CreateInstance(fieldType, true) as SharedVariable;
                            sharedVariable.IsShared = true;
                        }
                    }
                    else if ((globalStartIndex != -1) && (selectedIndex >= globalStartIndex))
                    {
                        sharedVariable = GlobalVariables.Instance.GetVariable(names[selectedIndex].Substring(8, names[selectedIndex].Length - 8));
                    }
                    else
                    {
                        sharedVariable = behaviorSource.GetVariable(names[selectedIndex]);
                    }
                    GUI.changed = true;
                }
                if ((!fieldType.Equals(typeof(SharedVariable)) && !TaskUtility.HasAttribute(fieldInfo, typeof(RequiredFieldAttribute))) && !TaskUtility.HasAttribute(fieldInfo, typeof(SharedRequiredAttribute)))
                {
                    sharedVariable = DrawSharedVariableToggleSharedButton(sharedVariable);
                    GUILayout.Space(-3f);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(3f);
                return sharedVariable;
            }
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            ObjectDrawer drawer = null;
            ObjectDrawerAttribute[] attributeArray = null;
            if (((fieldInfo != null) && ((attributeArray = fieldInfo.GetCustomAttributes(typeof(ObjectDrawerAttribute), true) as ObjectDrawerAttribute[]).Length > 0)) && ((drawer = ObjectDrawerUtility.GetObjectDrawer(task, attributeArray[0])) != null))
            {
                drawer.Value = sharedVariable;
                drawer.OnGUI(guiContent);
            }
            else
            {
                DrawFields(task, sharedVariable, guiContent);
            }
            if (!TaskUtility.HasAttribute(fieldInfo, typeof(RequiredFieldAttribute)) && !TaskUtility.HasAttribute(fieldInfo, typeof(SharedRequiredAttribute)))
            {
                sharedVariable = DrawSharedVariableToggleSharedButton(sharedVariable);
            }
            GUILayout.EndHorizontal();
            return sharedVariable;
        }

        internal static SharedVariable DrawSharedVariableToggleSharedButton(SharedVariable sharedVariable)
        {
            if (sharedVariable == null)
            {
                return null;
            }
            GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.Width(15f) };
            if (GUILayout.Button(!sharedVariable.IsShared ? BehaviorDesignerUtility.VariableButtonTexture : BehaviorDesignerUtility.VariableButtonSelectedTexture, BehaviorDesignerUtility.PlainButtonGUIStyle, options))
            {
                bool flag = !sharedVariable.IsShared;
                if (sharedVariable.GetType().Equals(typeof(SharedVariable)))
                {
                    sharedVariable = Activator.CreateInstance(FriendlySharedVariableName(sharedVariable.GetType().GetProperty("Value").PropertyType), true) as SharedVariable;
                }
                else
                {
                    sharedVariable = Activator.CreateInstance(sharedVariable.GetType(), true) as SharedVariable;
                }
                sharedVariable.IsShared = flag;
            }
            return sharedVariable;
        }

        private static object DrawSingleField(Task task, GUIContent guiContent, System.Reflection.FieldInfo fieldInfo, System.Type fieldType, object value)
        {
            if (fieldType.Equals(typeof(int)))
            {
                return EditorGUILayout.IntField(guiContent, (int) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(float)))
            {
                return EditorGUILayout.FloatField(guiContent, (float) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(double)))
            {
                return EditorGUILayout.FloatField(guiContent, Convert.ToSingle((double) value), new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(long)))
            {
                return (long) EditorGUILayout.IntField(guiContent, Convert.ToInt32((long) value), new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(bool)))
            {
                return EditorGUILayout.Toggle(guiContent, (bool) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(string)))
            {
                return EditorGUILayout.TextField(guiContent, (string) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(byte)))
            {
                return Convert.ToByte(EditorGUILayout.IntField(guiContent, Convert.ToInt32(value), new GUILayoutOption[0]));
            }
            if (fieldType.Equals(typeof(Vector2)))
            {
                return EditorGUILayout.Vector2Field(guiContent.text, (Vector2) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(Vector3)))
            {
                return EditorGUILayout.Vector3Field(guiContent.text, (Vector3) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(Vector4)))
            {
                return EditorGUILayout.Vector4Field(guiContent.text, (Vector4) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(Quaternion)))
            {
                Quaternion quaternion = (Quaternion) value;
                Vector4 zero = Vector4.zero;
                zero.Set(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
                zero = EditorGUILayout.Vector4Field(guiContent.text, zero, new GUILayoutOption[0]);
                quaternion.Set(zero.x, zero.y, zero.z, zero.w);
                return quaternion;
            }
            if (fieldType.Equals(typeof(Color)))
            {
                return EditorGUILayout.ColorField(guiContent, (Color) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(Rect)))
            {
                return EditorGUILayout.RectField(guiContent, (Rect) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(Matrix4x4)))
            {
                GUILayout.BeginVertical(new GUILayoutOption[0]);
                if (DrawFoldout(value.GetHashCode(), guiContent))
                {
                    EditorGUI.indentLevel++;
                    Matrix4x4 matrixx = (Matrix4x4) value;
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            EditorGUI.BeginChangeCheck();
                            matrixx[i, j] = EditorGUILayout.FloatField("E" + i.ToString() + j.ToString(), matrixx[i, j], new GUILayoutOption[0]);
                            if (EditorGUI.EndChangeCheck())
                            {
                                GUI.changed = true;
                            }
                        }
                    }
                    value = matrixx;
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
                return value;
            }
            if (fieldType.Equals(typeof(AnimationCurve)))
            {
                if (value == null)
                {
                    value = new AnimationCurve();
                }
                return EditorGUILayout.CurveField(guiContent, (AnimationCurve) value, new GUILayoutOption[0]);
            }
            if (fieldType.Equals(typeof(LayerMask)))
            {
                return DrawLayerMask(guiContent, (LayerMask) value);
            }
            if (typeof(SharedVariable).IsAssignableFrom(fieldType))
            {
                return DrawSharedVariable(task, guiContent, fieldInfo, fieldType, value as SharedVariable);
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                return EditorGUILayout.ObjectField(guiContent, (UnityEngine.Object) value, fieldType, true, new GUILayoutOption[0]);
            }
            if (fieldType.IsEnum)
            {
                return EditorGUILayout.EnumPopup(guiContent, (Enum) value, new GUILayoutOption[0]);
            }
            if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
            {
                int hashCode = guiContent.text.GetHashCode();
                if (drawnObjects.Contains(hashCode))
                {
                    return null;
                }
                drawnObjects.Add(hashCode);
                GUILayout.BeginVertical(new GUILayoutOption[0]);
                if (fieldType.IsAbstract)
                {
                    EditorGUILayout.LabelField(guiContent, new GUILayoutOption[0]);
                    GUILayout.EndVertical();
                    return null;
                }
                if (value == null)
                {
                    if (fieldType.IsGenericType && (fieldType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        fieldType = Nullable.GetUnderlyingType(fieldType);
                    }
                    value = Activator.CreateInstance(fieldType, true);
                }
                if (DrawFoldout(value.GetHashCode(), guiContent))
                {
                    EditorGUI.indentLevel++;
                    value = DrawFields(task, value);
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
                drawnObjects.Remove(hashCode);
                return value;
            }
            EditorGUILayout.LabelField("Unsupported Type: " + fieldType, new GUILayoutOption[0]);
            return null;
        }

        private static bool FoldOut(int hash)
        {
            if (foldoutDictionary.ContainsKey(hash))
            {
                return foldoutDictionary[hash];
            }
            foldoutDictionary.Add(hash, BehaviorDesignerPreferences.GetBool(BDPreferences.FoldoutFields));
            return true;
        }

        internal static System.Type FriendlySharedVariableName(System.Type type)
        {
            if (type.Equals(typeof(bool)))
            {
                return TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedBool");
            }
            if (type.Equals(typeof(int)))
            {
                return TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedInt");
            }
            if (type.Equals(typeof(float)))
            {
                return TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedFloat");
            }
            if (type.Equals(typeof(string)))
            {
                return TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString");
            }
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.Shared" + type.Name);
                if (typeWithinAssembly != null)
                {
                    return typeWithinAssembly;
                }
            }
            return type;
        }

        public static List<System.Type> GetBaseClasses(System.Type t)
        {
            List<System.Type> list = new List<System.Type>();
            while (((t != null) && !t.Equals(typeof(ParentTask))) && (!t.Equals(typeof(Task)) && !t.Equals(typeof(SharedVariable))))
            {
                list.Add(t);
                t = t.BaseType;
            }
            return list;
        }

        public static int GetVariablesOfType(System.Type valueType, bool isGlobal, string name, BehaviorSource behaviorSource, out string[] names, ref int globalStartIndex, bool getAll)
        {
            if (behaviorSource == null)
            {
                names = new string[0];
                return 0;
            }
            List<SharedVariable> variables = behaviorSource.Variables;
            int num = 0;
            List<string> list2 = new List<string> { "None" };
            if (variables != null)
            {
                for (int i = 0; i < variables.Count; i++)
                {
                    if (variables[i] != null)
                    {
                        System.Type propertyType = variables[i].GetType().GetProperty("Value").PropertyType;
                        if (((valueType == null) || getAll) || valueType.IsAssignableFrom(propertyType))
                        {
                            list2.Add(variables[i].Name);
                            if (!isGlobal && variables[i].Name.Equals(name))
                            {
                                num = list2.Count - 1;
                            }
                        }
                    }
                }
            }
            GlobalVariables instance = null;
            instance = GlobalVariables.Instance;
            if (instance != null)
            {
                globalStartIndex = list2.Count;
                variables = instance.Variables;
                if (variables != null)
                {
                    for (int j = 0; j < variables.Count; j++)
                    {
                        if (variables[j] != null)
                        {
                            System.Type type2 = variables[j].GetType().GetProperty("Value").PropertyType;
                            if (((valueType == null) || getAll) || type2.Equals(valueType))
                            {
                                list2.Add("Globals/" + variables[j].Name);
                                if (isGlobal && variables[j].Name.Equals(name))
                                {
                                    num = list2.Count - 1;
                                }
                            }
                        }
                    }
                }
            }
            names = list2.ToArray();
            return num;
        }

        public static void Init()
        {
            InitLayers();
        }

        private static void InitLayers()
        {
            List<string> list = new List<string>();
            List<int> list2 = new List<int>();
            for (int i = 0; i < 0x20; i++)
            {
                string str = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(str))
                {
                    list.Add(str);
                    list2.Add(((int) 1) << i);
                }
            }
            layerNames = list.ToArray();
            maskValues = list2.ToArray();
        }

        private static void SetFoldOut(int hash, bool value)
        {
            if (foldoutDictionary.ContainsKey(hash))
            {
                foldoutDictionary[hash] = value;
            }
            else
            {
                foldoutDictionary.Add(hash, value);
            }
        }
    }
}

