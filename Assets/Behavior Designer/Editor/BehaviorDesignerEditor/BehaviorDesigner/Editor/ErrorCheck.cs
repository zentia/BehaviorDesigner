namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    public static class ErrorCheck
    {
        private static void AddError(ref List<ErrorDetails> errorDetails, ErrorDetails.ErrorType type, Task task, string fieldName)
        {
            if (errorDetails == null)
            {
                errorDetails = new List<ErrorDetails>();
            }
            errorDetails.Add(new ErrorDetails(type, task, fieldName));
        }

        public static List<ErrorDetails> CheckForErrors(BehaviorSource behaviorSource)
        {
            if ((behaviorSource == null) || (behaviorSource.EntryTask == null))
            {
                return null;
            }
            List<ErrorDetails> errorDetails = null;
            CheckTaskForErrors(behaviorSource.EntryTask, ref errorDetails);
            if (behaviorSource.RootTask == null)
            {
                AddError(ref errorDetails, ErrorDetails.ErrorType.MissingChildren, behaviorSource.EntryTask, null);
            }
            if (behaviorSource.RootTask != null)
            {
                CheckTaskForErrors(behaviorSource.RootTask, ref errorDetails);
            }
            return errorDetails;
        }

        private static void CheckTaskForErrors(Task task, ref List<ErrorDetails> errorDetails)
        {
            if (!task.NodeData.Disabled)
            {
                if ((task is UnknownTask) || (task is UnknownParentTask))
                {
                    AddError(ref errorDetails, ErrorDetails.ErrorType.UnknownTask, task, null);
                }
                if (task.GetType().GetCustomAttributes(typeof(SkipErrorCheckAttribute), false).Length == 0)
                {
                    foreach (FieldInfo info in TaskUtility.GetAllFields(task.GetType()))
                    {
                        object obj2 = info.GetValue(task);
                        if (TaskUtility.HasAttribute(info, typeof(RequiredFieldAttribute)) && !IsRequiredFieldValid(info.FieldType, obj2))
                        {
                            AddError(ref errorDetails, ErrorDetails.ErrorType.RequiredField, task, info.Name);
                        }
                        if (info.FieldType.Equals(typeof(SharedVariable)) || info.FieldType.IsSubclassOf(typeof(SharedVariable)))
                        {
                            SharedVariable variable = obj2 as SharedVariable;
                            if (((variable != null) && variable.IsShared) && (string.IsNullOrEmpty(variable.Name) && !TaskUtility.HasAttribute(info, typeof(SharedRequiredAttribute))))
                            {
                                AddError(ref errorDetails, ErrorDetails.ErrorType.SharedVariable, task, info.Name);
                            }
                        }
                    }
                }
                if (((task is ParentTask) && (task.NodeData.NodeDesigner != null)) && !(task.NodeData.NodeDesigner as NodeDesigner).IsEntryDisplay)
                {
                    ParentTask task2 = task as ParentTask;
                    if ((task2.Children == null) || (task2.Children.Count == 0))
                    {
                        AddError(ref errorDetails, ErrorDetails.ErrorType.MissingChildren, task, null);
                    }
                    else
                    {
                        for (int i = 0; i < task2.Children.Count; i++)
                        {
                            CheckTaskForErrors(task2.Children[i], ref errorDetails);
                        }
                    }
                }
            }
        }

        public static bool IsRequiredFieldValid(Type fieldType, object value)
        {
            if ((value == null) || value.Equals(null))
            {
                return false;
            }
            if (typeof(IList).IsAssignableFrom(fieldType))
            {
                IList list = value as IList;
                if (list.Count == 0)
                {
                    return false;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if ((list[i] == null) || list[i].Equals(null))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}

