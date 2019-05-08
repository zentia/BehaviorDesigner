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

    public class SerializeJSON : UnityEngine.Object
    {
        private static FieldSerializationData fieldSerializationData;
        private static TaskSerializationData taskSerializationData;
        private static VariableSerializationData variableSerializationData;

        public static void Save(BehaviorSource behaviorSource)
        {
            behaviorSource.CheckForSerialization(false, null);
            taskSerializationData = new TaskSerializationData();
            fieldSerializationData = taskSerializationData.fieldSerializationData;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            if (behaviorSource.EntryTask != null)
            {
                dictionary.Add("EntryTask", SerializeTask(behaviorSource.EntryTask, true, ref fieldSerializationData.unityObjects));
            }
            if (behaviorSource.RootTask != null)
            {
                dictionary.Add("RootTask", SerializeTask(behaviorSource.RootTask, true, ref fieldSerializationData.unityObjects));
            }
            if ((behaviorSource.DetachedTasks != null) && (behaviorSource.DetachedTasks.Count > 0))
            {
                Dictionary<string, object>[] dictionaryArray = new Dictionary<string, object>[behaviorSource.DetachedTasks.Count];
                for (int i = 0; i < behaviorSource.DetachedTasks.Count; i++)
                {
                    dictionaryArray[i] = SerializeTask(behaviorSource.DetachedTasks[i], true, ref fieldSerializationData.unityObjects);
                }
                dictionary.Add("DetachedTasks", dictionaryArray);
            }
            if ((behaviorSource.Variables != null) && (behaviorSource.Variables.Count > 0))
            {
                dictionary.Add("Variables", SerializeVariables(behaviorSource.Variables, ref fieldSerializationData.unityObjects));
            }
            taskSerializationData.JSONSerialization = MiniJSON.Serialize(dictionary);
            behaviorSource.TaskData = taskSerializationData;
            if (behaviorSource.Owner != null)
            {
                EditorUtility.SetDirty(behaviorSource.Owner.GetObject());
            }
        }

        public static void Save(GlobalVariables variables)
        {
            if (variables != null)
            {
                variableSerializationData = new VariableSerializationData();
                fieldSerializationData = variableSerializationData.fieldSerializationData;
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add("Variables", SerializeVariables(variables.Variables, ref fieldSerializationData.unityObjects));
                variableSerializationData.JSONSerialization = MiniJSON.Serialize(dictionary);
                variables.VariableData = variableSerializationData;
                EditorUtility.SetDirty(variables);
            }
        }

        private static void SerializeFields(object obj, ref Dictionary<string, object> dict, ref List<UnityEngine.Object> unityObjects)
        {
            System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(obj.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if ((!BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(NonSerializedAttribute)) && ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(SerializeField)))) && ((!(obj is ParentTask) || !allFields[i].Name.Equals("children")) && (allFields[i].GetValue(obj) != null)))
                {
                    if (typeof(IList).IsAssignableFrom(allFields[i].FieldType))
                    {
                        IList list = allFields[i].GetValue(obj) as IList;
                        if (list != null)
                        {
                            List<object> list2 = new List<object>();
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (((list[j] == null) || object.ReferenceEquals(list[j], null)) || list[j].Equals(null))
                                {
                                    list2.Add(-1);
                                }
                                else
                                {
                                    System.Type type = list[j].GetType();
                                    if (list[j] is Task)
                                    {
                                        Task task = list[j] as Task;
                                        list2.Add(task.ID);
                                    }
                                    else if (list[j] is SharedVariable)
                                    {
                                        list2.Add(SerializeVariable(list[j] as SharedVariable, ref unityObjects));
                                    }
                                    else if (list[j] is UnityEngine.Object)
                                    {
                                        UnityEngine.Object objA = list[j] as UnityEngine.Object;
                                        if (!object.ReferenceEquals(objA, null) && (objA != null))
                                        {
                                            list2.Add(unityObjects.Count);
                                            unityObjects.Add(objA);
                                        }
                                    }
                                    else if (type.Equals(typeof(LayerMask)))
                                    {
                                        LayerMask mask = (LayerMask) list[j];
                                        list2.Add(mask.value);
                                    }
                                    else if (((type.IsPrimitive || type.IsEnum) || (type.Equals(typeof(string)) || type.Equals(typeof(Vector2)))) || (((type.Equals(typeof(Vector3)) || type.Equals(typeof(Vector4))) || (type.Equals(typeof(Quaternion)) || type.Equals(typeof(Matrix4x4)))) || (type.Equals(typeof(Color)) || type.Equals(typeof(Rect)))))
                                    {
                                        list2.Add(list[j]);
                                    }
                                    else
                                    {
                                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                                        SerializeFields(list[j], ref dictionary, ref unityObjects);
                                        list2.Add(dictionary);
                                    }
                                }
                            }
                            if (list2 != null)
                            {
                                dict.Add(allFields[i].FieldType + "," + allFields[i].Name, list2);
                            }
                        }
                    }
                    else if (allFields[i].FieldType.Equals(typeof(Task)) || allFields[i].FieldType.IsSubclassOf(typeof(Task)))
                    {
                        Task task2 = allFields[i].GetValue(obj) as Task;
                        if (task2 != null)
                        {
                            if (BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(InspectTaskAttribute)))
                            {
                                Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
                                dictionary2.Add("ObjectType", task2.GetType());
                                SerializeFields(task2, ref dictionary2, ref unityObjects);
                                dict.Add(allFields[i].Name, dictionary2);
                            }
                            else
                            {
                                dict.Add(allFields[i].FieldType + "," + allFields[i].Name, task2.ID);
                            }
                        }
                    }
                    else if (allFields[i].FieldType.Equals(typeof(SharedVariable)) || allFields[i].FieldType.IsSubclassOf(typeof(SharedVariable)))
                    {
                        dict.Add(allFields[i].FieldType + "," + allFields[i].Name, SerializeVariable(allFields[i].GetValue(obj) as SharedVariable, ref unityObjects));
                    }
                    else if (allFields[i].FieldType.Equals(typeof(UnityEngine.Object)) || allFields[i].FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        UnityEngine.Object obj3 = allFields[i].GetValue(obj) as UnityEngine.Object;
                        if (!object.ReferenceEquals(obj3, null) && (obj3 != null))
                        {
                            dict.Add(allFields[i].FieldType + "," + allFields[i].Name, unityObjects.Count);
                            unityObjects.Add(obj3);
                        }
                    }
                    else if (allFields[i].FieldType.Equals(typeof(LayerMask)))
                    {
                        LayerMask mask2 = (LayerMask) allFields[i].GetValue(obj);
                        dict.Add(allFields[i].FieldType + "," + allFields[i].Name, mask2.value);
                    }
                    else if (((allFields[i].FieldType.IsPrimitive || allFields[i].FieldType.IsEnum) || (allFields[i].FieldType.Equals(typeof(string)) || allFields[i].FieldType.Equals(typeof(Vector2)))) || (((allFields[i].FieldType.Equals(typeof(Vector3)) || allFields[i].FieldType.Equals(typeof(Vector4))) || (allFields[i].FieldType.Equals(typeof(Quaternion)) || allFields[i].FieldType.Equals(typeof(Matrix4x4)))) || (allFields[i].FieldType.Equals(typeof(Color)) || allFields[i].FieldType.Equals(typeof(Rect)))))
                    {
                        dict.Add(allFields[i].FieldType + "," + allFields[i].Name, allFields[i].GetValue(obj));
                    }
                    else if (allFields[i].FieldType.Equals(typeof(AnimationCurve)))
                    {
                        AnimationCurve curve = allFields[i].GetValue(obj) as AnimationCurve;
                        Dictionary<string, object> dictionary3 = new Dictionary<string, object>();
                        if (curve.keys != null)
                        {
                            Keyframe[] keys = curve.keys;
                            List<List<object>> list3 = new List<List<object>>();
                            for (int k = 0; k < keys.Length; k++)
                            {
                                List<object> list4;
                                list4 = new List<object> {
                                    keys[k].time,
                                    keys[k].value,
                                    keys[k].inTangent,
                                    keys[k].outTangent,
                                    keys[k].tangentMode,
                                    list4
                                };
                            }
                            dictionary3.Add("Keys", list3);
                        }
                        dictionary3.Add("PreWrapMode", curve.preWrapMode);
                        dictionary3.Add("PostWrapMode", curve.postWrapMode);
                        dict.Add(allFields[i].FieldType + "," + allFields[i].Name, dictionary3);
                    }
                    else
                    {
                        Dictionary<string, object> dictionary4 = new Dictionary<string, object>();
                        SerializeFields(allFields[i].GetValue(obj), ref dictionary4, ref unityObjects);
                        dict.Add(allFields[i].FieldType + "," + allFields[i].Name, dictionary4);
                    }
                }
            }
        }

        private static Dictionary<string, object> SerializeNodeData(NodeData nodeData)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("Offset", nodeData.Offset);
            if (nodeData.Comment.Length > 0)
            {
                dictionary.Add("Comment", nodeData.Comment);
            }
            if (nodeData.IsBreakpoint)
            {
                dictionary.Add("IsBreakpoint", nodeData.IsBreakpoint);
            }
            if (nodeData.Collapsed)
            {
                dictionary.Add("Collapsed", nodeData.Collapsed);
            }
            if (nodeData.Disabled)
            {
                dictionary.Add("Disabled", nodeData.Disabled);
            }
            if (nodeData.ColorIndex != 0)
            {
                dictionary.Add("ColorIndex", nodeData.ColorIndex);
            }
            if ((nodeData.WatchedFieldNames != null) && (nodeData.WatchedFieldNames.Count > 0))
            {
                dictionary.Add("WatchedFields", nodeData.WatchedFieldNames);
            }
            return dictionary;
        }

        public static Dictionary<string, object> SerializeTask(Task task, bool serializeChildren, ref List<UnityEngine.Object> unityObjects)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("ObjectType", task.GetType());
            dict.Add("NodeData", SerializeNodeData(task.NodeData));
            dict.Add("ID", task.ID);
            dict.Add("Name", task.FriendlyName);
            dict.Add("Instant", task.IsInstant);
            SerializeFields(task, ref dict, ref unityObjects);
            if (serializeChildren && (task is ParentTask))
            {
                ParentTask task2 = task as ParentTask;
                if ((task2.Children == null) || (task2.Children.Count <= 0))
                {
                    return dict;
                }
                Dictionary<string, object>[] dictionaryArray = new Dictionary<string, object>[task2.Children.Count];
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    dictionaryArray[i] = SerializeTask(task2.Children[i], serializeChildren, ref unityObjects);
                }
                dict.Add("Children", dictionaryArray);
            }
            return dict;
        }

        private static Dictionary<string, object> SerializeVariable(SharedVariable sharedVariable, ref List<UnityEngine.Object> unityObjects)
        {
            if (sharedVariable == null)
            {
                return null;
            }
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("Type", sharedVariable.GetType());
            dict.Add("Name", sharedVariable.Name);
            if (sharedVariable.IsShared)
            {
                dict.Add("IsShared", sharedVariable.IsShared);
            }
            if (sharedVariable.IsGlobal)
            {
                dict.Add("IsGlobal", sharedVariable.IsGlobal);
            }
            if (sharedVariable.NetworkSync)
            {
                dict.Add("NetworkSync", sharedVariable.NetworkSync);
            }
            if (!string.IsNullOrEmpty(sharedVariable.PropertyMapping))
            {
                dict.Add("PropertyMapping", sharedVariable.PropertyMapping);
                if (!object.Equals(sharedVariable.PropertyMappingOwner, null))
                {
                    dict.Add("PropertyMappingOwner", unityObjects.Count);
                    unityObjects.Add(sharedVariable.PropertyMappingOwner);
                }
            }
            SerializeFields(sharedVariable, ref dict, ref unityObjects);
            return dict;
        }

        private static Dictionary<string, object>[] SerializeVariables(List<SharedVariable> variables, ref List<UnityEngine.Object> unityObjects)
        {
            Dictionary<string, object>[] dictionaryArray = new Dictionary<string, object>[variables.Count];
            for (int i = 0; i < variables.Count; i++)
            {
                dictionaryArray[i] = SerializeVariable(variables[i], ref unityObjects);
            }
            return dictionaryArray;
        }
    }
}

