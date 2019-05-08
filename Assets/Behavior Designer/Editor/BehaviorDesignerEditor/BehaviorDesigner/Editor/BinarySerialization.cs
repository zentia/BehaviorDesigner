namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    public class BinarySerialization
    {
        private static int fieldIndex;
        private static FieldSerializationData fieldSerializationData;
        private static TaskSerializationData taskSerializationData;

        private static void AddByteData(System.Type fieldType, ICollection<byte> bytes)
        {
            fieldSerializationData.dataPosition.Add(fieldSerializationData.byteData.Count);
            if (bytes != null)
            {
                fieldSerializationData.byteData.AddRange(bytes);
            }
            fieldIndex++;
        }

        private static ICollection<byte> AnimationCurveToBytes(AnimationCurve animationCurve)
        {
            List<byte> list = new List<byte>();
            Keyframe[] keys = animationCurve.keys;
            if (keys != null)
            {
                list.AddRange(BitConverter.GetBytes(keys.Length));
                for (int i = 0; i < keys.Length; i++)
                {
                    list.AddRange(BitConverter.GetBytes(keys[i].time));
                    list.AddRange(BitConverter.GetBytes(keys[i].value));
                    list.AddRange(BitConverter.GetBytes(keys[i].inTangent));
                    list.AddRange(BitConverter.GetBytes(keys[i].outTangent));
                    list.AddRange(BitConverter.GetBytes(keys[i].tangentMode));
                }
            }
            else
            {
                list.AddRange(BitConverter.GetBytes(0));
            }
            list.AddRange(BitConverter.GetBytes((int) animationCurve.preWrapMode));
            list.AddRange(BitConverter.GetBytes((int) animationCurve.postWrapMode));
            return list;
        }

        private static byte[] BoolToBytes(bool value)
        {
            return BitConverter.GetBytes(value);
        }

        private static byte[] ByteToBytes(byte value)
        {
            return new byte[] { value };
        }

        private static ICollection<byte> ColorToBytes(Color color)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(color.r));
            list.AddRange(BitConverter.GetBytes(color.g));
            list.AddRange(BitConverter.GetBytes(color.b));
            list.AddRange(BitConverter.GetBytes(color.a));
            return list;
        }

        private static byte[] DoubleToBytes(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static byte[] FloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static byte[] Int16ToBytes(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static byte[] IntToBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static byte[] LongToBytes(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static ICollection<byte> Matrix4x4ToBytes(Matrix4x4 matrix4x4)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(matrix4x4.m00));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m01));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m02));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m03));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m10));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m11));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m12));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m13));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m20));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m21));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m22));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m23));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m30));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m31));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m32));
            list.AddRange(BitConverter.GetBytes(matrix4x4.m33));
            return list;
        }

        private static ICollection<byte> QuaternionToBytes(Quaternion quaternion)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(quaternion.x));
            list.AddRange(BitConverter.GetBytes(quaternion.y));
            list.AddRange(BitConverter.GetBytes(quaternion.z));
            list.AddRange(BitConverter.GetBytes(quaternion.w));
            return list;
        }

        private static ICollection<byte> RectToBytes(Rect rect)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(rect.x));
            list.AddRange(BitConverter.GetBytes(rect.y));
            list.AddRange(BitConverter.GetBytes(rect.width));
            list.AddRange(BitConverter.GetBytes(rect.height));
            return list;
        }

        public static void Save(BehaviorSource behaviorSource)
        {
            fieldIndex = 0;
            taskSerializationData = new TaskSerializationData();
            fieldSerializationData = taskSerializationData.fieldSerializationData;
            if (behaviorSource.Variables != null)
            {
                for (int i = 0; i < behaviorSource.Variables.Count; i++)
                {
                    taskSerializationData.variableStartIndex.Add(fieldSerializationData.startIndex.Count);
                    SaveSharedVariable(behaviorSource.Variables[i], string.Empty);
                }
            }
            if (!object.ReferenceEquals(behaviorSource.EntryTask, null))
            {
                SaveTask(behaviorSource.EntryTask, -1);
            }
            if (!object.ReferenceEquals(behaviorSource.RootTask, null))
            {
                SaveTask(behaviorSource.RootTask, 0);
            }
            if (behaviorSource.DetachedTasks != null)
            {
                for (int j = 0; j < behaviorSource.DetachedTasks.Count; j++)
                {
                    SaveTask(behaviorSource.DetachedTasks[j], -1);
                }
            }
            behaviorSource.TaskData = taskSerializationData;
            if (behaviorSource.Owner != null)
            {
                EditorUtility.SetDirty(behaviorSource.Owner.GetObject());
            }
        }

        public static void Save(GlobalVariables globalVariables)
        {
            if (globalVariables != null)
            {
                fieldIndex = 0;
                globalVariables.VariableData = new VariableSerializationData();
                if ((globalVariables.Variables != null) && (globalVariables.Variables.Count != 0))
                {
                    fieldSerializationData = globalVariables.VariableData.fieldSerializationData;
                    for (int i = 0; i < globalVariables.Variables.Count; i++)
                    {
                        globalVariables.VariableData.variableStartIndex.Add(fieldSerializationData.startIndex.Count);
                        SaveSharedVariable(globalVariables.Variables[i], string.Empty);
                    }
                    EditorUtility.SetDirty(globalVariables);
                }
            }
        }

        private static void SaveField(System.Type fieldType, string fieldName, object value, System.Reflection.FieldInfo fieldInfo = null)
        {
            string item = fieldType.Name + fieldName;
            fieldSerializationData.typeName.Add(item);
            fieldSerializationData.startIndex.Add(fieldIndex);
            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                System.Type elementType;
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
                IList list = value as IList;
                if (list == null)
                {
                    AddByteData(typeof(int), IntToBytes(0));
                }
                else
                {
                    AddByteData(typeof(int), IntToBytes(list.Count));
                    if (list.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (object.ReferenceEquals(list[i], null))
                            {
                                AddByteData(elementType, IntToBytes(-1));
                            }
                            else
                            {
                                SaveField(elementType, item + i, list[i], fieldInfo);
                            }
                        }
                    }
                }
            }
            else if (typeof(Task).IsAssignableFrom(fieldType))
            {
                if ((fieldInfo != null) && BehaviorDesignerUtility.HasAttribute(fieldInfo, typeof(InspectTaskAttribute)))
                {
                    AddByteData(fieldType, StringToBytes(value.GetType().ToString()));
                    SaveFields(value, item);
                }
                else
                {
                    AddByteData(fieldType, IntToBytes((value as Task).ID));
                }
            }
            else if (typeof(SharedVariable).IsAssignableFrom(fieldType))
            {
                SaveSharedVariable(value as SharedVariable, item);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                AddByteData(fieldType, IntToBytes(fieldSerializationData.unityObjects.Count));
                fieldSerializationData.unityObjects.Add(value as UnityEngine.Object);
            }
            else if (fieldType.Equals(typeof(int)) || fieldType.IsEnum)
            {
                AddByteData(fieldType, IntToBytes((int) value));
            }
            else if (fieldType.Equals(typeof(short)))
            {
                AddByteData(fieldType, Int16ToBytes((short) value));
            }
            else if (fieldType.Equals(typeof(uint)))
            {
                AddByteData(fieldType, UIntToBytes((uint) value));
            }
            else if (fieldType.Equals(typeof(float)))
            {
                AddByteData(fieldType, FloatToBytes((float) value));
            }
            else if (fieldType.Equals(typeof(double)))
            {
                AddByteData(fieldType, DoubleToBytes((double) value));
            }
            else if (fieldType.Equals(typeof(long)))
            {
                AddByteData(fieldType, LongToBytes((long) value));
            }
            else if (fieldType.Equals(typeof(bool)))
            {
                AddByteData(fieldType, BoolToBytes((bool) value));
            }
            else if (fieldType.Equals(typeof(string)))
            {
                AddByteData(fieldType, StringToBytes((string) value));
            }
            else if (fieldType.Equals(typeof(byte)))
            {
                AddByteData(fieldType, ByteToBytes((byte) value));
            }
            else if (fieldType.Equals(typeof(Vector2)))
            {
                AddByteData(fieldType, Vector2ToBytes((Vector2) value));
            }
            else if (fieldType.Equals(typeof(Vector3)))
            {
                AddByteData(fieldType, Vector3ToBytes((Vector3) value));
            }
            else if (fieldType.Equals(typeof(Vector4)))
            {
                AddByteData(fieldType, Vector4ToBytes((Vector4) value));
            }
            else if (fieldType.Equals(typeof(Quaternion)))
            {
                AddByteData(fieldType, QuaternionToBytes((Quaternion) value));
            }
            else if (fieldType.Equals(typeof(Color)))
            {
                AddByteData(fieldType, ColorToBytes((Color) value));
            }
            else if (fieldType.Equals(typeof(Rect)))
            {
                AddByteData(fieldType, RectToBytes((Rect) value));
            }
            else if (fieldType.Equals(typeof(Matrix4x4)))
            {
                AddByteData(fieldType, Matrix4x4ToBytes((Matrix4x4) value));
            }
            else if (fieldType.Equals(typeof(LayerMask)))
            {
                LayerMask mask = (LayerMask) value;
                AddByteData(fieldType, IntToBytes(mask.value));
            }
            else if (fieldType.Equals(typeof(AnimationCurve)))
            {
                AddByteData(fieldType, AnimationCurveToBytes((AnimationCurve) value));
            }
            else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
            {
                if (object.ReferenceEquals(value, null))
                {
                    value = Activator.CreateInstance(fieldType, true);
                }
                SaveFields(value, item);
            }
            else
            {
                Debug.LogError("Missing Serialization for " + fieldType);
            }
        }

        private static void SaveFields(object obj, string namePrefix)
        {
            System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(obj.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if ((!BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(NonSerializedAttribute)) && ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(SerializeField)))) && (!(obj is ParentTask) || !allFields[i].Name.Equals("children")))
                {
                    object objA = allFields[i].GetValue(obj);
                    if (!object.ReferenceEquals(objA, null))
                    {
                        SaveField(allFields[i].FieldType, namePrefix + allFields[i].Name, objA, allFields[i]);
                    }
                }
            }
        }

        private static void SaveNodeData(NodeData nodeData)
        {
            SaveField(typeof(Vector2), "NodeDataOffset", nodeData.Offset, null);
            SaveField(typeof(string), "NodeDataComment", nodeData.Comment, null);
            SaveField(typeof(bool), "NodeDataIsBreakpoint", nodeData.IsBreakpoint, null);
            SaveField(typeof(bool), "NodeDataDisabled", nodeData.Disabled, null);
            SaveField(typeof(bool), "NodeDataCollapsed", nodeData.Collapsed, null);
            SaveField(typeof(int), "NodeDataColorIndex", nodeData.ColorIndex, null);
            SaveField(typeof(List<string>), "NodeDataWatchedFields", nodeData.WatchedFieldNames, null);
        }

        private static void SaveSharedVariable(SharedVariable sharedVariable, string namePrefix)
        {
            if (sharedVariable != null)
            {
                SaveField(typeof(string), namePrefix + "Type", sharedVariable.GetType().ToString(), null);
                SaveField(typeof(string), namePrefix + "Name", sharedVariable.Name, null);
                if (sharedVariable.IsShared)
                {
                    SaveField(typeof(bool), namePrefix + "IsShared", sharedVariable.IsShared, null);
                }
                if (sharedVariable.IsGlobal)
                {
                    SaveField(typeof(bool), namePrefix + "IsGlobal", sharedVariable.IsGlobal, null);
                }
                if (sharedVariable.NetworkSync)
                {
                    SaveField(typeof(bool), namePrefix + "NetworkSync", sharedVariable.NetworkSync, null);
                }
                if (!string.IsNullOrEmpty(sharedVariable.PropertyMapping))
                {
                    SaveField(typeof(string), namePrefix + "PropertyMapping", sharedVariable.PropertyMapping, null);
                    if (!object.Equals(sharedVariable.PropertyMappingOwner, null))
                    {
                        SaveField(typeof(GameObject), namePrefix + "PropertyMappingOwner", sharedVariable.PropertyMappingOwner, null);
                    }
                }
                SaveFields(sharedVariable, namePrefix);
            }
        }

        private static void SaveTask(Task task, int parentTaskIndex)
        {
            taskSerializationData.types.Add(task.GetType().ToString());
            taskSerializationData.parentIndex.Add(parentTaskIndex);
            taskSerializationData.startIndex.Add(fieldSerializationData.startIndex.Count);
            SaveField(typeof(int), "ID", task.ID, null);
            SaveField(typeof(string), "FriendlyName", task.FriendlyName, null);
            SaveField(typeof(bool), "IsInstant", task.IsInstant, null);
            SaveNodeData(task.NodeData);
            SaveFields(task, string.Empty);
            if (task is ParentTask)
            {
                ParentTask task2 = task as ParentTask;
                if ((task2.Children != null) && (task2.Children.Count > 0))
                {
                    for (int i = 0; i < task2.Children.Count; i++)
                    {
                        SaveTask(task2.Children[i], task2.ID);
                    }
                }
            }
        }

        private static byte[] StringToBytes(string str)
        {
            if (str == null)
            {
                str = string.Empty;
            }
            return Encoding.UTF8.GetBytes(str);
        }

        private static byte[] UIntToBytes(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static ICollection<byte> Vector2ToBytes(Vector2 vector2)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(vector2.x));
            list.AddRange(BitConverter.GetBytes(vector2.y));
            return list;
        }

        private static ICollection<byte> Vector3ToBytes(Vector3 vector3)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(vector3.x));
            list.AddRange(BitConverter.GetBytes(vector3.y));
            list.AddRange(BitConverter.GetBytes(vector3.z));
            return list;
        }

        private static ICollection<byte> Vector4ToBytes(Vector4 vector4)
        {
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(vector4.x));
            list.AddRange(BitConverter.GetBytes(vector4.y));
            list.AddRange(BitConverter.GetBytes(vector4.z));
            list.AddRange(BitConverter.GetBytes(vector4.w));
            return list;
        }
    }
}

