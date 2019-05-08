namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using UnityEngine;

    public class DeserializeJSON : UnityEngine.Object
    {
        private static GlobalVariables globalVariables = null;
        private static Dictionary<int, Dictionary<string, object>> serializationCache = new Dictionary<int, Dictionary<string, object>>();
        private static Dictionary<TaskField, List<int>> taskIDs = null;

        private static NodeData DeserializeNodeData(Dictionary<string, object> dict, Task task)
        {
            object obj2;
            NodeData data = new NodeData();
            if (dict.TryGetValue("Offset", out obj2))
            {
                data.Offset = StringToVector2((string) obj2);
            }
            if (dict.TryGetValue("FriendlyName", out obj2))
            {
                task.FriendlyName = (string) obj2;
            }
            if (dict.TryGetValue("Comment", out obj2))
            {
                data.Comment = (string) obj2;
            }
            if (dict.TryGetValue("IsBreakpoint", out obj2))
            {
                data.IsBreakpoint = Convert.ToBoolean(obj2);
            }
            if (dict.TryGetValue("Collapsed", out obj2))
            {
                data.Collapsed = Convert.ToBoolean(obj2);
            }
            if (dict.TryGetValue("Disabled", out obj2))
            {
                data.Disabled = Convert.ToBoolean(obj2);
            }
            if (dict.TryGetValue("ColorIndex", out obj2))
            {
                data.ColorIndex = Convert.ToInt32(obj2);
            }
            if (dict.TryGetValue("WatchedFields", out obj2))
            {
                data.WatchedFieldNames = new List<string>();
                data.WatchedFields = new List<FieldInfo>();
                IList list = obj2 as IList;
                for (int i = 0; i < list.Count; i++)
                {
                    FieldInfo field = task.GetType().GetField((string) list[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (field != null)
                    {
                        data.WatchedFieldNames.Add(field.Name);
                        data.WatchedFields.Add(field);
                    }
                }
            }
            return data;
        }

        private static void DeserializeObject(Task task, object obj, Dictionary<string, object> dict, IVariableSource variableSource, List<UnityEngine.Object> unityObjects)
        {
            if (dict != null)
            {
                FieldInfo[] allFields = TaskUtility.GetAllFields(obj.GetType());
                for (int i = 0; i < allFields.Length; i++)
                {
                    object obj2;
                    if (dict.TryGetValue(allFields[i].FieldType + "," + allFields[i].Name, out obj2) || dict.TryGetValue(allFields[i].Name, out obj2))
                    {
                        if (typeof(IList).IsAssignableFrom(allFields[i].FieldType))
                        {
                            IList list = obj2 as IList;
                            if (list != null)
                            {
                                System.Type elementType;
                                if (allFields[i].FieldType.IsArray)
                                {
                                    elementType = allFields[i].FieldType.GetElementType();
                                }
                                else
                                {
                                    System.Type fieldType = allFields[i].FieldType;
                                    while (!fieldType.IsGenericType)
                                    {
                                        fieldType = fieldType.BaseType;
                                    }
                                    elementType = fieldType.GetGenericArguments()[0];
                                }
                                if (elementType.Equals(typeof(Task)) || elementType.IsSubclassOf(typeof(Task)))
                                {
                                    if (taskIDs != null)
                                    {
                                        List<int> list2 = new List<int>();
                                        for (int j = 0; j < list.Count; j++)
                                        {
                                            list2.Add(Convert.ToInt32(list[j]));
                                        }
                                        taskIDs.Add(new TaskField(task, allFields[i]), list2);
                                    }
                                }
                                else if (allFields[i].FieldType.IsArray)
                                {
                                    Array array = Array.CreateInstance(elementType, list.Count);
                                    for (int k = 0; k < list.Count; k++)
                                    {
                                        array.SetValue(ValueToObject(task, elementType, list[k], variableSource, unityObjects), k);
                                    }
                                    allFields[i].SetValue(obj, array);
                                }
                                else
                                {
                                    IList list3;
                                    if (allFields[i].FieldType.IsGenericType)
                                    {
                                        System.Type[] typeArguments = new System.Type[] { elementType };
                                        list3 = TaskUtility.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
                                    }
                                    else
                                    {
                                        list3 = TaskUtility.CreateInstance(allFields[i].FieldType) as IList;
                                    }
                                    for (int m = 0; m < list.Count; m++)
                                    {
                                        list3.Add(ValueToObject(task, elementType, list[m], variableSource, unityObjects));
                                    }
                                    allFields[i].SetValue(obj, list3);
                                }
                            }
                        }
                        else
                        {
                            System.Type type = allFields[i].FieldType;
                            if (type.Equals(typeof(Task)) || type.IsSubclassOf(typeof(Task)))
                            {
                                if (TaskUtility.HasAttribute(allFields[i], typeof(InspectTaskAttribute)))
                                {
                                    Dictionary<string, object> dictionary = obj2 as Dictionary<string, object>;
                                    System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(dictionary["ObjectType"] as string);
                                    if (typeWithinAssembly != null)
                                    {
                                        Task task2 = TaskUtility.CreateInstance(typeWithinAssembly) as Task;
                                        DeserializeObject(task2, task2, dictionary, variableSource, unityObjects);
                                        allFields[i].SetValue(task, task2);
                                    }
                                }
                                else if (taskIDs != null)
                                {
                                    List<int> list4 = new List<int> {
                                        Convert.ToInt32(obj2)
                                    };
                                    taskIDs.Add(new TaskField(task, allFields[i]), list4);
                                }
                            }
                            else
                            {
                                allFields[i].SetValue(obj, ValueToObject(task, type, obj2, variableSource, unityObjects));
                            }
                        }
                    }
                    else if (typeof(SharedVariable).IsAssignableFrom(allFields[i].FieldType))
                    {
                        System.Type type5 = TaskUtility.SharedVariableToConcreteType(allFields[i].FieldType);
                        if (type5 == null)
                        {
                            return;
                        }
                        if (dict.TryGetValue(type5 + "," + allFields[i].Name, out obj2))
                        {
                            SharedVariable variable = TaskUtility.CreateInstance(allFields[i].FieldType) as SharedVariable;
                            variable.SetValue(ValueToObject(task, type5, obj2, variableSource, unityObjects));
                            allFields[i].SetValue(obj, variable);
                        }
                        else
                        {
                            SharedVariable variable2 = TaskUtility.CreateInstance(allFields[i].FieldType) as SharedVariable;
                            allFields[i].SetValue(obj, variable2);
                        }
                    }
                }
            }
        }

        private static SharedVariable DeserializeSharedVariable(Dictionary<string, object> dict, IVariableSource variableSource, bool fromSource, List<UnityEngine.Object> unityObjects)
        {
            object obj2;
            if (dict == null)
            {
                return null;
            }
            SharedVariable variable = null;
            if ((!fromSource && (variableSource != null)) && dict.TryGetValue("Name", out obj2))
            {
                object obj3;
                dict.TryGetValue("IsGlobal", out obj3);
                if (!dict.TryGetValue("IsGlobal", out obj3) || !Convert.ToBoolean(obj3))
                {
                    variable = variableSource.GetVariable(obj2 as string);
                }
                else
                {
                    if (globalVariables == null)
                    {
                        globalVariables = GlobalVariables.Instance;
                    }
                    if (globalVariables != null)
                    {
                        variable = globalVariables.GetVariable(obj2 as string);
                    }
                }
            }
            System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(dict["Type"] as string);
            if (typeWithinAssembly == null)
            {
                return null;
            }
            bool flag = true;
            if ((variable == null) || !(flag = variable.GetType().Equals(typeWithinAssembly)))
            {
                object obj4;
                variable = TaskUtility.CreateInstance(typeWithinAssembly) as SharedVariable;
                variable.Name = dict["Name"] as string;
                if (dict.TryGetValue("IsShared", out obj4))
                {
                    variable.IsShared = Convert.ToBoolean(obj4);
                }
                if (dict.TryGetValue("IsGlobal", out obj4))
                {
                    variable.IsGlobal = Convert.ToBoolean(obj4);
                }
                if (dict.TryGetValue("NetworkSync", out obj4))
                {
                    variable.NetworkSync = Convert.ToBoolean(obj4);
                }
                if (!variable.IsGlobal && dict.TryGetValue("PropertyMapping", out obj4))
                {
                    variable.PropertyMapping = obj4 as string;
                    if (dict.TryGetValue("PropertyMappingOwner", out obj4))
                    {
                        variable.PropertyMappingOwner = IndexToUnityObject(Convert.ToInt32(obj4), unityObjects) as GameObject;
                    }
                    variable.InitializePropertyMapping(variableSource as BehaviorSource);
                }
                if (!flag)
                {
                    variable.IsShared = true;
                }
                DeserializeObject(null, variable, dict, variableSource, unityObjects);
            }
            return variable;
        }

        public static Task DeserializeTask(BehaviorSource behaviorSource, Dictionary<string, object> dict, ref Dictionary<int, Task> IDtoTask, List<UnityEngine.Object> unityObjects)
        {
            Task task = null;
            object obj2;
            try
            {
                System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(dict["ObjectType"] as string);
                if (typeWithinAssembly == null)
                {
                    if (dict.ContainsKey("Children"))
                    {
                        typeWithinAssembly = typeof(UnknownParentTask);
                    }
                    else
                    {
                        typeWithinAssembly = typeof(UnknownTask);
                    }
                }
                task = TaskUtility.CreateInstance(typeWithinAssembly) as Task;
            }
            catch (Exception)
            {
            }
            if (task == null)
            {
                Debug.Log("Error: task is null of type " + dict["ObjectType"]);
                return null;
            }
            task.Owner = behaviorSource.Owner.GetObject() as Behavior;
            task.ID = Convert.ToInt32(dict["ID"]);
            if (dict.TryGetValue("Name", out obj2))
            {
                task.FriendlyName = (string) obj2;
            }
            if (dict.TryGetValue("Instant", out obj2))
            {
                task.IsInstant = Convert.ToBoolean(obj2);
            }
            IDtoTask.Add(task.ID, task);
            task.NodeData = DeserializeNodeData(dict["NodeData"] as Dictionary<string, object>, task);
            if (task.GetType().Equals(typeof(UnknownTask)) || task.GetType().Equals(typeof(UnknownParentTask)))
            {
                if (!task.FriendlyName.Contains("Unknown "))
                {
                    task.FriendlyName = string.Format("Unknown {0}", task.FriendlyName);
                }
                if (!task.NodeData.Comment.Contains("Loaded from an unknown type. Was a task renamed or deleted?"))
                {
                    task.NodeData.Comment = string.Format("Loaded from an unknown type. Was a task renamed or deleted?{0}", !task.NodeData.Comment.Equals(string.Empty) ? string.Format("\0{0}", task.NodeData.Comment) : string.Empty);
                }
            }
            DeserializeObject(task, task, dict, behaviorSource, unityObjects);
            if ((task is ParentTask) && dict.TryGetValue("Children", out obj2))
            {
                ParentTask task2 = task as ParentTask;
                if (task2 == null)
                {
                    return task;
                }
                IEnumerator enumerator = (obj2 as IEnumerable).GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        Dictionary<string, object> current = (Dictionary<string, object>) enumerator.Current;
                        Task child = DeserializeTask(behaviorSource, current, ref IDtoTask, unityObjects);
                        int index = (task2.Children != null) ? task2.Children.Count : 0;
                        task2.AddChild(child, index);
                    }
                }
                finally
                {
                    IDisposable disposable = enumerator as IDisposable;
                    if (disposable == null)
                    {
                    }
                    disposable.Dispose();
                }
            }
            return task;
        }

        private static void DeserializeVariables(IVariableSource variableSource, Dictionary<string, object> dict, List<UnityEngine.Object> unityObjects)
        {
            object obj2;
            if (dict.TryGetValue("Variables", out obj2))
            {
                List<SharedVariable> variables = new List<SharedVariable>();
                IList list2 = obj2 as IList;
                for (int i = 0; i < list2.Count; i++)
                {
                    SharedVariable item = DeserializeSharedVariable(list2[i] as Dictionary<string, object>, variableSource, true, unityObjects);
                    variables.Add(item);
                }
                variableSource.SetAllVariables(variables);
            }
        }

        private static UnityEngine.Object IndexToUnityObject(int index, List<UnityEngine.Object> unityObjects)
        {
            if ((index >= 0) && (index < unityObjects.Count))
            {
                return unityObjects[index];
            }
            return null;
        }

        public static void Load(TaskSerializationData taskData, BehaviorSource behaviorSource)
        {
            Dictionary<string, object> dictionary;
            behaviorSource.EntryTask = null;
            behaviorSource.RootTask = null;
            behaviorSource.DetachedTasks = null;
            behaviorSource.Variables = null;
            if (!serializationCache.TryGetValue(taskData.JSONSerialization.GetHashCode(), out dictionary))
            {
                dictionary = MiniJSON.Deserialize(taskData.JSONSerialization) as Dictionary<string, object>;
                serializationCache.Add(taskData.JSONSerialization.GetHashCode(), dictionary);
            }
            if (dictionary == null)
            {
                Debug.Log("Failed to deserialize");
            }
            else
            {
                taskIDs = new Dictionary<TaskField, List<int>>();
                Dictionary<int, Task> iDtoTask = new Dictionary<int, Task>();
                DeserializeVariables(behaviorSource, dictionary, taskData.fieldSerializationData.unityObjects);
                if (dictionary.ContainsKey("EntryTask"))
                {
                    behaviorSource.EntryTask = DeserializeTask(behaviorSource, dictionary["EntryTask"] as Dictionary<string, object>, ref iDtoTask, taskData.fieldSerializationData.unityObjects);
                }
                if (dictionary.ContainsKey("RootTask"))
                {
                    behaviorSource.RootTask = DeserializeTask(behaviorSource, dictionary["RootTask"] as Dictionary<string, object>, ref iDtoTask, taskData.fieldSerializationData.unityObjects);
                }
                if (dictionary.ContainsKey("DetachedTasks"))
                {
                    List<Task> list = new List<Task>();
                    IEnumerator enumerator = (dictionary["DetachedTasks"] as IEnumerable).GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            Dictionary<string, object> current = (Dictionary<string, object>) enumerator.Current;
                            list.Add(DeserializeTask(behaviorSource, current, ref iDtoTask, taskData.fieldSerializationData.unityObjects));
                        }
                    }
                    finally
                    {
                        IDisposable disposable = enumerator as IDisposable;
                        if (disposable == null)
                        {
                        }
                        disposable.Dispose();
                    }
                    behaviorSource.DetachedTasks = list;
                }
                if ((taskIDs != null) && (taskIDs.Count > 0))
                {
                    foreach (TaskField field in taskIDs.Keys)
                    {
                        List<int> list2 = taskIDs[field];
                        System.Type fieldType = field.fieldInfo.FieldType;
                        if (field.fieldInfo.FieldType.IsArray)
                        {
                            int length = 0;
                            for (int i = 0; i < list2.Count; i++)
                            {
                                Task task = iDtoTask[list2[i]];
                                if (task.GetType().Equals(fieldType.GetElementType()) || task.GetType().IsSubclassOf(fieldType.GetElementType()))
                                {
                                    length++;
                                }
                            }
                            Array array = Array.CreateInstance(fieldType.GetElementType(), length);
                            int index = 0;
                            for (int j = 0; j < list2.Count; j++)
                            {
                                Task task2 = iDtoTask[list2[j]];
                                if (task2.GetType().Equals(fieldType.GetElementType()) || task2.GetType().IsSubclassOf(fieldType.GetElementType()))
                                {
                                    array.SetValue(task2, index);
                                    index++;
                                }
                            }
                            field.fieldInfo.SetValue(field.task, array);
                        }
                        else
                        {
                            Task task3 = iDtoTask[list2[0]];
                            if (task3.GetType().Equals(field.fieldInfo.FieldType) || task3.GetType().IsSubclassOf(field.fieldInfo.FieldType))
                            {
                                field.fieldInfo.SetValue(field.task, task3);
                            }
                        }
                    }
                    taskIDs = null;
                }
            }
        }

        public static void Load(string serialization, GlobalVariables globalVariables)
        {
            if (globalVariables != null)
            {
                Dictionary<string, object> dict = MiniJSON.Deserialize(serialization) as Dictionary<string, object>;
                if (dict == null)
                {
                    Debug.Log("Failed to deserialize");
                }
                else
                {
                    if (globalVariables.VariableData == null)
                    {
                        globalVariables.VariableData = new VariableSerializationData();
                    }
                    DeserializeVariables(globalVariables, dict, globalVariables.VariableData.fieldSerializationData.unityObjects);
                }
            }
        }

        private static Color StringToColor(string colorString)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = colorString.Substring(5, colorString.Length - 6).Split(separator);
            return new Color(float.Parse(strArray[0]), float.Parse(strArray[1]), float.Parse(strArray[2]), float.Parse(strArray[3]));
        }

        private static Matrix4x4 StringToMatrix4x4(string matrixString)
        {
            string[] strArray = matrixString.Split(null);
            return new Matrix4x4 { m00 = float.Parse(strArray[0]), m01 = float.Parse(strArray[1]), m02 = float.Parse(strArray[2]), m03 = float.Parse(strArray[3]), m10 = float.Parse(strArray[4]), m11 = float.Parse(strArray[5]), m12 = float.Parse(strArray[6]), m13 = float.Parse(strArray[7]), m20 = float.Parse(strArray[8]), m21 = float.Parse(strArray[9]), m22 = float.Parse(strArray[10]), m23 = float.Parse(strArray[11]), m30 = float.Parse(strArray[12]), m31 = float.Parse(strArray[13]), m32 = float.Parse(strArray[14]), m33 = float.Parse(strArray[15]) };
        }

        private static Quaternion StringToQuaternion(string quaternionString)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = quaternionString.Substring(1, quaternionString.Length - 2).Split(separator);
            return new Quaternion(float.Parse(strArray[0]), float.Parse(strArray[1]), float.Parse(strArray[2]), float.Parse(strArray[3]));
        }

        private static Rect StringToRect(string rectString)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = rectString.Substring(1, rectString.Length - 2).Split(separator);
            return new Rect(float.Parse(strArray[0].Substring(2, strArray[0].Length - 2)), float.Parse(strArray[1].Substring(3, strArray[1].Length - 3)), float.Parse(strArray[2].Substring(7, strArray[2].Length - 7)), float.Parse(strArray[3].Substring(8, strArray[3].Length - 8)));
        }

        private static Vector2 StringToVector2(string vector2String)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = vector2String.Substring(1, vector2String.Length - 2).Split(separator);
            return new Vector2(float.Parse(strArray[0]), float.Parse(strArray[1]));
        }

        private static Vector3 StringToVector3(string vector3String)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = vector3String.Substring(1, vector3String.Length - 2).Split(separator);
            return new Vector3(float.Parse(strArray[0]), float.Parse(strArray[1]), float.Parse(strArray[2]));
        }

        private static Vector4 StringToVector4(string vector4String)
        {
            char[] separator = new char[] { ',' };
            string[] strArray = vector4String.Substring(1, vector4String.Length - 2).Split(separator);
            return new Vector4(float.Parse(strArray[0]), float.Parse(strArray[1]), float.Parse(strArray[2]), float.Parse(strArray[3]));
        }

        private static AnimationCurve ValueToAnimationCurve(Dictionary<string, object> value)
        {
            object obj2;
            AnimationCurve curve = new AnimationCurve();
            if (value.TryGetValue("Keys", out obj2))
            {
                List<object> list = obj2 as List<object>;
                for (int i = 0; i < list.Count; i++)
                {
                    List<object> list2 = list[i] as List<object>;
                    Keyframe key = new Keyframe((float) Convert.ChangeType(list2[0], typeof(float)), (float) Convert.ChangeType(list2[1], typeof(float)), (float) Convert.ChangeType(list2[2], typeof(float)), (float) Convert.ChangeType(list2[3], typeof(float))) {
                        tangentMode = (int) Convert.ChangeType(list2[4], typeof(int))
                    };
                    curve.AddKey(key);
                }
            }
            if (value.TryGetValue("PreWrapMode", out obj2))
            {
                curve.preWrapMode = (WrapMode) ((int) Enum.Parse(typeof(WrapMode), (string) obj2));
            }
            if (value.TryGetValue("PostWrapMode", out obj2))
            {
                curve.postWrapMode = (WrapMode) ((int) Enum.Parse(typeof(WrapMode), (string) obj2));
            }
            return curve;
        }

        private static LayerMask ValueToLayerMask(int value)
        {
            return new LayerMask { value = value };
        }

        private static object ValueToObject(Task task, System.Type type, object obj, IVariableSource variableSource, List<UnityEngine.Object> unityObjects)
        {
            if (type.Equals(typeof(SharedVariable)) || type.IsSubclassOf(typeof(SharedVariable)))
            {
                SharedVariable variable = DeserializeSharedVariable(obj as Dictionary<string, object>, variableSource, false, unityObjects);
                if (variable == null)
                {
                    variable = TaskUtility.CreateInstance(type) as SharedVariable;
                }
                return variable;
            }
            if (type.Equals(typeof(UnityEngine.Object)) || type.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                return IndexToUnityObject(Convert.ToInt32(obj), unityObjects);
            }
            if (type.IsPrimitive || type.Equals(typeof(string)))
            {
                try
                {
                    return Convert.ChangeType(obj, type);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            if (type.IsSubclassOf(typeof(Enum)))
            {
                try
                {
                    return Enum.Parse(type, (string) obj);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            if (type.Equals(typeof(Vector2)))
            {
                return StringToVector2((string) obj);
            }
            if (type.Equals(typeof(Vector3)))
            {
                return StringToVector3((string) obj);
            }
            if (type.Equals(typeof(Vector4)))
            {
                return StringToVector4((string) obj);
            }
            if (type.Equals(typeof(Quaternion)))
            {
                return StringToQuaternion((string) obj);
            }
            if (type.Equals(typeof(Matrix4x4)))
            {
                return StringToMatrix4x4((string) obj);
            }
            if (type.Equals(typeof(Color)))
            {
                return StringToColor((string) obj);
            }
            if (type.Equals(typeof(Rect)))
            {
                return StringToRect((string) obj);
            }
            if (type.Equals(typeof(LayerMask)))
            {
                return ValueToLayerMask(Convert.ToInt32(obj));
            }
            if (type.Equals(typeof(AnimationCurve)))
            {
                return ValueToAnimationCurve((Dictionary<string, object>) obj);
            }
            object obj2 = TaskUtility.CreateInstance(type);
            DeserializeObject(task, obj2, obj as Dictionary<string, object>, variableSource, unityObjects);
            return obj2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TaskField
        {
            public Task task;
            public FieldInfo fieldInfo;
            public TaskField(Task t, FieldInfo f)
            {
                this.task = t;
                this.fieldInfo = f;
            }
        }
    }
}

