using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class BinaryDeserialization
{
    private static GlobalVariables globalVariables;
    private static Dictionary<ObjectFieldMap, List<int>> taskIDs;

    private static AnimationCurve BytesToAnimationCurve(byte[] bytes, int dataPosition)
    {
        AnimationCurve curve = new AnimationCurve();
        int num = BitConverter.ToInt32(bytes, dataPosition);
        for (int i = 0; i < num; i++)
        {
            Keyframe key = new Keyframe {
                time = BitConverter.ToSingle(bytes, dataPosition + 4),
                value = BitConverter.ToSingle(bytes, dataPosition + 8),
                inTangent = BitConverter.ToSingle(bytes, dataPosition + 12),
                outTangent = BitConverter.ToSingle(bytes, dataPosition + 0x10),
                tangentMode = BitConverter.ToInt32(bytes, dataPosition + 20)
            };
            curve.AddKey(key);
            dataPosition += 20;
        }
        curve.preWrapMode = (WrapMode) BitConverter.ToInt32(bytes, dataPosition + 4);
        curve.postWrapMode = (WrapMode) BitConverter.ToInt32(bytes, dataPosition + 8);
        return curve;
    }

    private static bool BytesToBool(byte[] bytes, int dataPosition)
    {
        return BitConverter.ToBoolean(bytes, dataPosition);
    }

    private static byte BytesToByte(byte[] bytes, int dataPosition)
    {
        return bytes[dataPosition];
    }

    private static Color BytesToColor(byte[] bytes, int dataPosition)
    {
        Color black = Color.black;
        black.r = BitConverter.ToSingle(bytes, dataPosition);
        black.g = BitConverter.ToSingle(bytes, dataPosition + 4);
        black.b = BitConverter.ToSingle(bytes, dataPosition + 8);
        black.a = BitConverter.ToSingle(bytes, dataPosition + 12);
        return black;
    }

    private static double BytesToDouble(byte[] bytes, int dataPosition)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, dataPosition, 8);
        }
        return BitConverter.ToDouble(bytes, dataPosition);
    }

    private static float BytesToFloat(byte[] bytes, int dataPosition)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, dataPosition, 4);
        }
        return BitConverter.ToSingle(bytes, dataPosition);
    }

    private static int BytesToInt(byte[] bytes, int dataPosition)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, dataPosition, 4);
        }
        return BitConverter.ToInt32(bytes, dataPosition);
    }

    private static LayerMask BytesToLayerMask(byte[] bytes, int dataPosition)
    {
        return new LayerMask { value = BytesToInt(bytes, dataPosition) };
    }

    private static long BytesToLong(byte[] bytes, int dataPosition)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, dataPosition, 8);
        }
        return BitConverter.ToInt64(bytes, dataPosition);
    }

    private static Matrix4x4 BytesToMatrix4x4(byte[] bytes, int dataPosition)
    {
        Matrix4x4 identity = Matrix4x4.identity;
        identity.m00 = BitConverter.ToSingle(bytes, dataPosition);
        identity.m01 = BitConverter.ToSingle(bytes, dataPosition + 4);
        identity.m02 = BitConverter.ToSingle(bytes, dataPosition + 8);
        identity.m03 = BitConverter.ToSingle(bytes, dataPosition + 12);
        identity.m10 = BitConverter.ToSingle(bytes, dataPosition + 0x10);
        identity.m11 = BitConverter.ToSingle(bytes, dataPosition + 20);
        identity.m12 = BitConverter.ToSingle(bytes, dataPosition + 0x18);
        identity.m13 = BitConverter.ToSingle(bytes, dataPosition + 0x1c);
        identity.m20 = BitConverter.ToSingle(bytes, dataPosition + 0x20);
        identity.m21 = BitConverter.ToSingle(bytes, dataPosition + 0x24);
        identity.m22 = BitConverter.ToSingle(bytes, dataPosition + 40);
        identity.m23 = BitConverter.ToSingle(bytes, dataPosition + 0x2c);
        identity.m30 = BitConverter.ToSingle(bytes, dataPosition + 0x30);
        identity.m31 = BitConverter.ToSingle(bytes, dataPosition + 0x34);
        identity.m32 = BitConverter.ToSingle(bytes, dataPosition + 0x38);
        identity.m33 = BitConverter.ToSingle(bytes, dataPosition + 60);
        return identity;
    }

    private static Quaternion BytesToQuaternion(byte[] bytes, int dataPosition)
    {
        Quaternion identity = Quaternion.identity;
        identity.x = BitConverter.ToSingle(bytes, dataPosition);
        identity.y = BitConverter.ToSingle(bytes, dataPosition + 4);
        identity.z = BitConverter.ToSingle(bytes, dataPosition + 8);
        identity.w = BitConverter.ToSingle(bytes, dataPosition + 12);
        return identity;
    }

    private static Rect BytesToRect(byte[] bytes, int dataPosition)
    {
        return new Rect { x = BitConverter.ToSingle(bytes, dataPosition), y = BitConverter.ToSingle(bytes, dataPosition + 4), width = BitConverter.ToSingle(bytes, dataPosition + 8), height = BitConverter.ToSingle(bytes, dataPosition + 12) };
    }

    private static SharedVariable BytesToSharedVariable(FieldSerializationData fieldSerializationData, Dictionary<string, int> fieldIndexMap, byte[] bytes, int dataPosition, IVariableSource variableSource, bool fromField, string namePrefix)
    {
        SharedVariable variable = null;
        string str = (string) LoadField(fieldSerializationData, fieldIndexMap, typeof(string), namePrefix + "Type", null, null, null);
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }
        string name = (string) LoadField(fieldSerializationData, fieldIndexMap, typeof(string), namePrefix + "Name", null, null, null);
        bool flag = Convert.ToBoolean(LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), namePrefix + "IsShared", null, null, null));
        bool flag2 = Convert.ToBoolean(LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), namePrefix + "IsGlobal", null, null, null));
        if (flag && fromField)
        {
            if (!flag2)
            {
                variable = variableSource.GetVariable(name);
            }
            else
            {
                if (globalVariables == null)
                {
                    globalVariables = GlobalVariables.Instance;
                }
                if (globalVariables != null)
                {
                    variable = globalVariables.GetVariable(name);
                }
            }
        }
        System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(str);
        if (typeWithinAssembly == null)
        {
            return null;
        }
        bool flag3 = true;
        if ((variable == null) || !(flag3 = variable.GetType().Equals(typeWithinAssembly)))
        {
            variable = TaskUtility.CreateInstance(typeWithinAssembly) as SharedVariable;
            variable.Name = name;
            variable.IsShared = flag;
            variable.IsGlobal = flag2;
            variable.NetworkSync = Convert.ToBoolean(LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), namePrefix + "NetworkSync", null, null, null));
            if (!flag2)
            {
                variable.PropertyMapping = (string) LoadField(fieldSerializationData, fieldIndexMap, typeof(string), namePrefix + "PropertyMapping", null, null, null);
                variable.PropertyMappingOwner = (GameObject) LoadField(fieldSerializationData, fieldIndexMap, typeof(GameObject), namePrefix + "PropertyMappingOwner", null, null, null);
                variable.InitializePropertyMapping(variableSource as BehaviorSource);
            }
            if (!flag3)
            {
                variable.IsShared = true;
            }
            LoadFields(fieldSerializationData, fieldIndexMap, variable, namePrefix, variableSource);
        }
        return variable;
    }

    private static string BytesToString(byte[] bytes, int dataPosition, int dataSize)
    {
        if (dataSize == 0)
        {
            return string.Empty;
        }
        return Encoding.UTF8.GetString(bytes, dataPosition, dataSize);
    }

    private static uint BytesToUInt(byte[] bytes, int dataPosition)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes, dataPosition, 4);
        }
        return BitConverter.ToUInt32(bytes, dataPosition);
    }

    private static Vector2 BytesToVector2(byte[] bytes, int dataPosition)
    {
        Vector2 zero = Vector2.zero;
        zero.x = BitConverter.ToSingle(bytes, dataPosition);
        zero.y = BitConverter.ToSingle(bytes, dataPosition + 4);
        return zero;
    }

    private static Vector3 BytesToVector3(byte[] bytes, int dataPosition)
    {
        Vector3 zero = Vector3.zero;
        zero.x = BitConverter.ToSingle(bytes, dataPosition);
        zero.y = BitConverter.ToSingle(bytes, dataPosition + 4);
        zero.z = BitConverter.ToSingle(bytes, dataPosition + 8);
        return zero;
    }

    private static Vector4 BytesToVector4(byte[] bytes, int dataPosition)
    {
        Vector4 zero = Vector4.zero;
        zero.x = BitConverter.ToSingle(bytes, dataPosition);
        zero.y = BitConverter.ToSingle(bytes, dataPosition + 4);
        zero.z = BitConverter.ToSingle(bytes, dataPosition + 8);
        zero.w = BitConverter.ToSingle(bytes, dataPosition + 12);
        return zero;
    }

    private static int GetFieldSize(FieldSerializationData fieldSerializationData, int fieldIndex)
    {
        return ((((fieldIndex + 1) >= fieldSerializationData.dataPosition.Count) ? fieldSerializationData.byteData.Count : fieldSerializationData.dataPosition[fieldIndex + 1]) - fieldSerializationData.dataPosition[fieldIndex]);
    }

    private static UnityEngine.Object IndexToUnityObject(int index, FieldSerializationData activeFieldSerializationData)
    {
        if ((index >= 0) && (index < activeFieldSerializationData.unityObjects.Count))
        {
            return activeFieldSerializationData.unityObjects[index];
        }
        return null;
    }

    public static void Load(BehaviorSource behaviorSource)
    {
        Load(behaviorSource.TaskData, behaviorSource);
    }

    public static void Load(GlobalVariables globalVariables)
    {
        if (globalVariables != null)
        {
            FieldSerializationData data;
            globalVariables.Variables = null;
            if (((globalVariables.VariableData != null) && ((data = globalVariables.VariableData.fieldSerializationData).byteData != null)) && (data.byteData.Count != 0))
            {
                VariableSerializationData variableData = globalVariables.VariableData;
                data.byteDataArray = data.byteData.ToArray();
                if (variableData.variableStartIndex != null)
                {
                    List<SharedVariable> list = new List<SharedVariable>();
                    Dictionary<string, int> fieldIndexMap = ObjectPool.Get<Dictionary<string, int>>();
                    for (int i = 0; i < variableData.variableStartIndex.Count; i++)
                    {
                        int count;
                        int num2 = variableData.variableStartIndex[i];
                        if ((i + 1) < variableData.variableStartIndex.Count)
                        {
                            count = variableData.variableStartIndex[i + 1];
                        }
                        else
                        {
                            count = data.startIndex.Count;
                        }
                        fieldIndexMap.Clear();
                        for (int j = num2; j < count; j++)
                        {
                            fieldIndexMap.Add(data.typeName[j], data.startIndex[j]);
                        }
                        SharedVariable item = BytesToSharedVariable(data, fieldIndexMap, data.byteDataArray, variableData.variableStartIndex[i], globalVariables, false, string.Empty);
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    }
                    ObjectPool.Return<Dictionary<string, int>>(fieldIndexMap);
                    globalVariables.Variables = list;
                }
            }
        }
    }

    public static void Load(TaskSerializationData taskData, BehaviorSource behaviorSource)
    {
        FieldSerializationData data2;
        behaviorSource.EntryTask = null;
        behaviorSource.RootTask = null;
        behaviorSource.DetachedTasks = null;
        behaviorSource.Variables = null;
        TaskSerializationData taskSerializationData = taskData;
        if (((taskSerializationData != null) && ((data2 = taskSerializationData.fieldSerializationData).byteData != null)) && (data2.byteData.Count != 0))
        {
            data2.byteDataArray = data2.byteData.ToArray();
            taskIDs = null;
            if (taskSerializationData.variableStartIndex != null)
            {
                List<SharedVariable> list = new List<SharedVariable>();
                Dictionary<string, int> fieldIndexMap = ObjectPool.Get<Dictionary<string, int>>();
                for (int i = 0; i < taskSerializationData.variableStartIndex.Count; i++)
                {
                    int count;
                    int num2 = taskSerializationData.variableStartIndex[i];
                    if ((i + 1) < taskSerializationData.variableStartIndex.Count)
                    {
                        count = taskSerializationData.variableStartIndex[i + 1];
                    }
                    else if ((taskSerializationData.startIndex != null) && (taskSerializationData.startIndex.Count > 0))
                    {
                        count = taskSerializationData.startIndex[0];
                    }
                    else
                    {
                        count = data2.startIndex.Count;
                    }
                    fieldIndexMap.Clear();
                    for (int j = num2; j < count; j++)
                    {
                        fieldIndexMap.Add(data2.typeName[j], data2.startIndex[j]);
                    }
                    SharedVariable item = BytesToSharedVariable(data2, fieldIndexMap, data2.byteDataArray, taskSerializationData.variableStartIndex[i], behaviorSource, false, string.Empty);
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }
                ObjectPool.Return<Dictionary<string, int>>(fieldIndexMap);
                behaviorSource.Variables = list;
            }
            List<Task> taskList = new List<Task>();
            if (taskSerializationData.types != null)
            {
                for (int k = 0; k < taskSerializationData.types.Count; k++)
                {
                    LoadTask(taskSerializationData, data2, ref taskList, ref behaviorSource);
                }
            }
            if (taskSerializationData.parentIndex.Count != taskList.Count)
            {
                Debug.LogError("Deserialization Error: parent index count does not match task list count");
            }
            else
            {
                for (int m = 0; m < taskSerializationData.parentIndex.Count; m++)
                {
                    if (taskSerializationData.parentIndex[m] == -1)
                    {
                        if (behaviorSource.EntryTask == null)
                        {
                            behaviorSource.EntryTask = taskList[m];
                        }
                        else
                        {
                            if (behaviorSource.DetachedTasks == null)
                            {
                                behaviorSource.DetachedTasks = new List<Task>();
                            }
                            behaviorSource.DetachedTasks.Add(taskList[m]);
                        }
                    }
                    else if (taskSerializationData.parentIndex[m] == 0)
                    {
                        behaviorSource.RootTask = taskList[m];
                    }
                    else if (taskSerializationData.parentIndex[m] != -1)
                    {
                        ParentTask task = taskList[taskSerializationData.parentIndex[m]] as ParentTask;
                        if (task != null)
                        {
                            int index = (task.Children != null) ? task.Children.Count : 0;
                            task.AddChild(taskList[m], index);
                        }
                    }
                }
                if (taskIDs != null)
                {
                    foreach (ObjectFieldMap map in taskIDs.Keys)
                    {
                        List<int> list3 = taskIDs[map];
                        System.Type fieldType = map.fieldInfo.FieldType;
                        if (typeof(IList).IsAssignableFrom(fieldType))
                        {
                            if (fieldType.IsArray)
                            {
                                Array array = Array.CreateInstance(fieldType.GetElementType(), list3.Count);
                                for (int n = 0; n < array.Length; n++)
                                {
                                    array.SetValue(taskList[list3[n]], n);
                                }
                                map.fieldInfo.SetValue(map.obj, array);
                            }
                            else
                            {
                                System.Type type3 = fieldType.GetGenericArguments()[0];
                                System.Type[] typeArguments = new System.Type[] { type3 };
                                IList list4 = TaskUtility.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
                                for (int num9 = 0; num9 < list3.Count; num9++)
                                {
                                    list4.Add(taskList[list3[num9]]);
                                }
                                map.fieldInfo.SetValue(map.obj, list4);
                            }
                        }
                        else
                        {
                            map.fieldInfo.SetValue(map.obj, taskList[list3[0]]);
                        }
                    }
                }
            }
        }
    }

    private static object LoadField(FieldSerializationData fieldSerializationData, Dictionary<string, int> fieldIndexMap, System.Type fieldType, string fieldName, IVariableSource variableSource, object obj = null, FieldInfo fieldInfo = null)
    {
        int num;
        string key = fieldType.Name + fieldName;
        if (!fieldIndexMap.TryGetValue(key, out num))
        {
            if (typeof(SharedVariable).IsAssignableFrom(fieldType))
            {
                System.Type type = TaskUtility.SharedVariableToConcreteType(fieldType);
                if (type == null)
                {
                    return null;
                }
                key = type.Name + fieldName;
                if (fieldIndexMap.ContainsKey(key))
                {
                    SharedVariable variable = TaskUtility.CreateInstance(fieldType) as SharedVariable;
                    variable.SetValue(LoadField(fieldSerializationData, fieldIndexMap, type, fieldName, variableSource, null, null));
                    return variable;
                }
            }
            if (typeof(SharedVariable).IsAssignableFrom(fieldType))
            {
                return TaskUtility.CreateInstance(fieldType);
            }
            return null;
        }
        object obj2 = null;
        if (typeof(IList).IsAssignableFrom(fieldType))
        {
            IList list;
            int length = BytesToInt(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
            if (fieldType.IsArray)
            {
                System.Type elementType = fieldType.GetElementType();
                if (elementType == null)
                {
                    return null;
                }
                Array array = Array.CreateInstance(elementType, length);
                for (int j = 0; j < length; j++)
                {
                    object objA = LoadField(fieldSerializationData, fieldIndexMap, elementType, key + j, variableSource, obj, fieldInfo);
                    array.SetValue((!object.ReferenceEquals(objA, null) && !objA.Equals(null)) ? objA : null, j);
                }
                return array;
            }
            System.Type baseType = fieldType;
            while (!baseType.IsGenericType)
            {
                baseType = baseType.BaseType;
            }
            System.Type type4 = baseType.GetGenericArguments()[0];
            if (fieldType.IsGenericType)
            {
                System.Type[] typeArguments = new System.Type[] { type4 };
                list = TaskUtility.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
            }
            else
            {
                list = TaskUtility.CreateInstance(fieldType) as IList;
            }
            for (int i = 0; i < length; i++)
            {
                object obj4 = LoadField(fieldSerializationData, fieldIndexMap, type4, key + i, variableSource, obj, fieldInfo);
                list.Add((!object.ReferenceEquals(obj4, null) && !obj4.Equals(null)) ? obj4 : null);
            }
            return list;
        }
        if (typeof(Task).IsAssignableFrom(fieldType))
        {
            if ((fieldInfo != null) && TaskUtility.HasAttribute(fieldInfo, typeof(InspectTaskAttribute)))
            {
                string str2 = BytesToString(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num], GetFieldSize(fieldSerializationData, num));
                if (!string.IsNullOrEmpty(str2))
                {
                    System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(str2);
                    if (typeWithinAssembly == null)
                    {
                        return obj2;
                    }
                    obj2 = TaskUtility.CreateInstance(typeWithinAssembly);
                    LoadFields(fieldSerializationData, fieldIndexMap, obj2, key, variableSource);
                }
                return obj2;
            }
            if (taskIDs == null)
            {
                taskIDs = new Dictionary<ObjectFieldMap, List<int>>(new ObjectFieldMapComparer());
            }
            int item = BytesToInt(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
            ObjectFieldMap map = new ObjectFieldMap(obj, fieldInfo);
            if (taskIDs.ContainsKey(map))
            {
                taskIDs[map].Add(item);
                return obj2;
            }
            List<int> list2 = new List<int> {
                item
            };
            taskIDs.Add(map, list2);
            return obj2;
        }
        if (typeof(SharedVariable).IsAssignableFrom(fieldType))
        {
            return BytesToSharedVariable(fieldSerializationData, fieldIndexMap, fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num], variableSource, true, key);
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
        {
            return IndexToUnityObject(BytesToInt(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]), fieldSerializationData);
        }
        if (fieldType.Equals(typeof(int)) || fieldType.IsEnum)
        {
            return BytesToInt(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(uint)))
        {
            return BytesToUInt(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(float)))
        {
            return BytesToFloat(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(double)))
        {
            return BytesToDouble(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(long)))
        {
            return BytesToLong(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(bool)))
        {
            return BytesToBool(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(string)))
        {
            return BytesToString(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num], GetFieldSize(fieldSerializationData, num));
        }
        if (fieldType.Equals(typeof(byte)))
        {
            return BytesToByte(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Vector2)))
        {
            return BytesToVector2(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Vector3)))
        {
            return BytesToVector3(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Vector4)))
        {
            return BytesToVector4(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Quaternion)))
        {
            return BytesToQuaternion(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Color)))
        {
            return BytesToColor(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Rect)))
        {
            return BytesToRect(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(Matrix4x4)))
        {
            return BytesToMatrix4x4(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(AnimationCurve)))
        {
            return BytesToAnimationCurve(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.Equals(typeof(LayerMask)))
        {
            return BytesToLayerMask(fieldSerializationData.byteDataArray, fieldSerializationData.dataPosition[num]);
        }
        if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
        {
            obj2 = TaskUtility.CreateInstance(fieldType);
            LoadFields(fieldSerializationData, fieldIndexMap, obj2, key, variableSource);
        }
        return obj2;
    }

    private static void LoadFields(FieldSerializationData fieldSerializationData, Dictionary<string, int> fieldIndexMap, object obj, string namePrefix, IVariableSource variableSource)
    {
        FieldInfo[] allFields = TaskUtility.GetAllFields(obj.GetType());
        for (int i = 0; i < allFields.Length; i++)
        {
            if ((!TaskUtility.HasAttribute(allFields[i], typeof(NonSerializedAttribute)) && ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || TaskUtility.HasAttribute(allFields[i], typeof(SerializeField)))) && (!(obj is ParentTask) || !allFields[i].Name.Equals("children")))
            {
                object objA = LoadField(fieldSerializationData, fieldIndexMap, allFields[i].FieldType, namePrefix + allFields[i].Name, variableSource, obj, allFields[i]);
                if (((objA != null) && !object.ReferenceEquals(objA, null)) && !objA.Equals(null))
                {
                    allFields[i].SetValue(obj, objA);
                }
            }
        }
    }

    private static void LoadNodeData(FieldSerializationData fieldSerializationData, Dictionary<string, int> fieldIndexMap, Task task)
    {
        NodeData data = new NodeData {
            Offset = (Vector2) LoadField(fieldSerializationData, fieldIndexMap, typeof(Vector2), "NodeDataOffset", null, null, null),
            Comment = (string) LoadField(fieldSerializationData, fieldIndexMap, typeof(string), "NodeDataComment", null, null, null),
            IsBreakpoint = (bool) LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), "NodeDataIsBreakpoint", null, null, null),
            Disabled = (bool) LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), "NodeDataDisabled", null, null, null),
            Collapsed = (bool) LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), "NodeDataCollapsed", null, null, null)
        };
        object obj2 = LoadField(fieldSerializationData, fieldIndexMap, typeof(int), "NodeDataColorIndex", null, null, null);
        if (obj2 != null)
        {
            data.ColorIndex = (int) obj2;
        }
        obj2 = LoadField(fieldSerializationData, fieldIndexMap, typeof(List<string>), "NodeDataWatchedFields", null, null, null);
        if (obj2 != null)
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
        task.NodeData = data;
    }

    private static void LoadTask(TaskSerializationData taskSerializationData, FieldSerializationData fieldSerializationData, ref List<Task> taskList, ref BehaviorSource behaviorSource)
    {
        int num4;
        int count = taskList.Count;
        Task item = null;
        System.Type typeWithinAssembly = TaskUtility.GetTypeWithinAssembly(taskSerializationData.types[count]);
        if (typeWithinAssembly == null)
        {
            bool flag = false;
            for (int j = 0; j < taskSerializationData.parentIndex.Count; j++)
            {
                if (count == taskSerializationData.parentIndex[j])
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                typeWithinAssembly = typeof(UnknownParentTask);
            }
            else
            {
                typeWithinAssembly = typeof(UnknownTask);
            }
        }
        item = TaskUtility.CreateInstance(typeWithinAssembly) as Task;
        item.Owner = behaviorSource.Owner.GetObject() as Behavior;
        taskList.Add(item);
        int num3 = taskSerializationData.startIndex[count];
        if ((count + 1) < taskSerializationData.startIndex.Count)
        {
            num4 = taskSerializationData.startIndex[count + 1];
        }
        else
        {
            num4 = fieldSerializationData.startIndex.Count;
        }
        Dictionary<string, int> fieldIndexMap = ObjectPool.Get<Dictionary<string, int>>();
        fieldIndexMap.Clear();
        for (int i = num3; i < num4; i++)
        {
            fieldIndexMap.Add(fieldSerializationData.typeName[i], fieldSerializationData.startIndex[i]);
        }
        item.ID = (int) LoadField(fieldSerializationData, fieldIndexMap, typeof(int), "ID", null, null, null);
        item.FriendlyName = (string) LoadField(fieldSerializationData, fieldIndexMap, typeof(string), "FriendlyName", null, null, null);
        item.IsInstant = (bool) LoadField(fieldSerializationData, fieldIndexMap, typeof(bool), "IsInstant", null, null, null);
        LoadNodeData(fieldSerializationData, fieldIndexMap, taskList[count]);
        if (item.GetType().Equals(typeof(UnknownTask)) || item.GetType().Equals(typeof(UnknownParentTask)))
        {
            if (!item.FriendlyName.Contains("Unknown "))
            {
                item.FriendlyName = string.Format("Unknown {0}", item.FriendlyName);
            }
            if (!item.NodeData.Comment.Contains("Loaded from an unknown type. Was a task renamed or deleted?"))
            {
                item.NodeData.Comment = string.Format("Loaded from an unknown type. Was a task renamed or deleted?{0}", !item.NodeData.Comment.Equals(string.Empty) ? string.Format("\0{0}", item.NodeData.Comment) : string.Empty);
            }
        }
        LoadFields(fieldSerializationData, fieldIndexMap, taskList[count], string.Empty, behaviorSource);
        ObjectPool.Return<Dictionary<string, int>>(fieldIndexMap);
    }

    private class ObjectFieldMap
    {
        public FieldInfo fieldInfo;
        public object obj;

        public ObjectFieldMap(object o, FieldInfo f)
        {
            this.obj = o;
            this.fieldInfo = f;
        }
    }

    private class ObjectFieldMapComparer : IEqualityComparer<BinaryDeserialization.ObjectFieldMap>
    {
        public bool Equals(BinaryDeserialization.ObjectFieldMap a, BinaryDeserialization.ObjectFieldMap b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return false;
            }
            if (object.ReferenceEquals(b, null))
            {
                return false;
            }
            return (a.obj.Equals(b.obj) && a.fieldInfo.Equals(b.fieldInfo));
        }

        public int GetHashCode(BinaryDeserialization.ObjectFieldMap a)
        {
            return ((a == null) ? 0 : (a.obj.ToString().GetHashCode() + a.fieldInfo.ToString().GetHashCode()));
        }
    }
}

