namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public class TaskReferences : MonoBehaviour
    {
        public static void CheckReferences(BehaviorSource behaviorSource)
        {
            if (behaviorSource.RootTask != null)
            {
                CheckReferences(behaviorSource, behaviorSource.RootTask);
            }
            if (behaviorSource.DetachedTasks != null)
            {
                for (int i = 0; i < behaviorSource.DetachedTasks.Count; i++)
                {
                    CheckReferences(behaviorSource, behaviorSource.DetachedTasks[i]);
                }
            }
        }

        public static void CheckReferences(Behavior behavior, List<Task> taskList)
        {
            for (int i = 0; i < taskList.Count; i++)
            {
                CheckReferences(behavior, taskList[i], taskList);
            }
        }

        private static void CheckReferences(BehaviorSource behaviorSource, Task task)
        {
            FieldInfo[] allFields = TaskUtility.GetAllFields(task.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if (!allFields[i].FieldType.IsArray && (allFields[i].FieldType.Equals(typeof(Task)) || allFields[i].FieldType.IsSubclassOf(typeof(Task))))
                {
                    Task referencedTask = allFields[i].GetValue(task) as Task;
                    if (referencedTask != null)
                    {
                        Task task3 = FindReferencedTask(behaviorSource, referencedTask);
                        if (task3 != null)
                        {
                            allFields[i].SetValue(task, task3);
                        }
                    }
                }
                else if (allFields[i].FieldType.IsArray && (allFields[i].FieldType.GetElementType().Equals(typeof(Task)) || allFields[i].FieldType.GetElementType().IsSubclassOf(typeof(Task))))
                {
                    Task[] taskArray = allFields[i].GetValue(task) as Task[];
                    if (taskArray != null)
                    {
                        System.Type[] typeArguments = new System.Type[] { allFields[i].FieldType.GetElementType() };
                        IList list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
                        for (int j = 0; j < taskArray.Length; j++)
                        {
                            Task task4 = FindReferencedTask(behaviorSource, taskArray[j]);
                            if (task4 != null)
                            {
                                list.Add(task4);
                            }
                        }
                        Array array = Array.CreateInstance(allFields[i].FieldType.GetElementType(), list.Count);
                        list.CopyTo(array, 0);
                        allFields[i].SetValue(task, array);
                    }
                }
            }
            if (task.GetType().IsSubclassOf(typeof(ParentTask)))
            {
                ParentTask task5 = task as ParentTask;
                if (task5.Children != null)
                {
                    for (int k = 0; k < task5.Children.Count; k++)
                    {
                        CheckReferences(behaviorSource, task5.Children[k]);
                    }
                }
            }
        }

        private static void CheckReferences(Behavior behavior, Task task, List<Task> taskList)
        {
            if (TaskUtility.CompareType(task.GetType(), "BehaviorDesigner.Runtime.Tasks.ConditionalEvaluator"))
            {
                object obj2 = task.GetType().GetField("conditionalTask").GetValue(task);
                if (obj2 != null)
                {
                    task = obj2 as Task;
                }
            }
            FieldInfo[] allFields = TaskUtility.GetAllFields(task.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if (!allFields[i].FieldType.IsArray && (allFields[i].FieldType.Equals(typeof(Task)) || allFields[i].FieldType.IsSubclassOf(typeof(Task))))
                {
                    Task referencedTask = allFields[i].GetValue(task) as Task;
                    if ((referencedTask != null) && !referencedTask.Owner.Equals(behavior))
                    {
                        Task task3 = FindReferencedTask(referencedTask, taskList);
                        if (task3 != null)
                        {
                            allFields[i].SetValue(task, task3);
                        }
                    }
                }
                else if (allFields[i].FieldType.IsArray && (allFields[i].FieldType.GetElementType().Equals(typeof(Task)) || allFields[i].FieldType.GetElementType().IsSubclassOf(typeof(Task))))
                {
                    Task[] taskArray = allFields[i].GetValue(task) as Task[];
                    if (taskArray != null)
                    {
                        System.Type[] typeArguments = new System.Type[] { allFields[i].FieldType.GetElementType() };
                        IList list = Activator.CreateInstance(typeof(List<>).MakeGenericType(typeArguments)) as IList;
                        for (int j = 0; j < taskArray.Length; j++)
                        {
                            Task task4 = FindReferencedTask(taskArray[j], taskList);
                            if (task4 != null)
                            {
                                list.Add(task4);
                            }
                        }
                        Array array = Array.CreateInstance(allFields[i].FieldType.GetElementType(), list.Count);
                        list.CopyTo(array, 0);
                        allFields[i].SetValue(task, array);
                    }
                }
            }
        }

        private static Task FindReferencedTask(BehaviorSource behaviorSource, Task referencedTask)
        {
            Task task;
            int iD = referencedTask.ID;
            if ((behaviorSource.RootTask != null) && ((task = FindReferencedTask(behaviorSource.RootTask, iD)) != null))
            {
                return task;
            }
            if (behaviorSource.DetachedTasks != null)
            {
                for (int i = 0; i < behaviorSource.DetachedTasks.Count; i++)
                {
                    task = FindReferencedTask(behaviorSource.DetachedTasks[i], iD);
                    if (task != null)
                    {
                        return task;
                    }
                }
            }
            return null;
        }

        private static Task FindReferencedTask(Task referencedTask, List<Task> taskList)
        {
            int referenceID = referencedTask.ReferenceID;
            for (int i = 0; i < taskList.Count; i++)
            {
                if (taskList[i].ReferenceID == referenceID)
                {
                    return taskList[i];
                }
            }
            return null;
        }

        private static Task FindReferencedTask(Task task, int referencedTaskID)
        {
            if (task.ID == referencedTaskID)
            {
                return task;
            }
            if (task.GetType().IsSubclassOf(typeof(ParentTask)))
            {
                ParentTask task2 = task as ParentTask;
                if (task2.Children != null)
                {
                    for (int i = 0; i < task2.Children.Count; i++)
                    {
                        Task task3 = FindReferencedTask(task2.Children[i], referencedTaskID);
                        if (task3 != null)
                        {
                            return task3;
                        }
                    }
                }
            }
            return null;
        }
    }
}

