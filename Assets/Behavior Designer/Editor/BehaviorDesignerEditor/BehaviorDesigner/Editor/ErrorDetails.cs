namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using UnityEngine;

    [Serializable]
    public class ErrorDetails
    {
        [SerializeField]
        private string fieldName;
        [SerializeField]
        private BehaviorDesigner.Editor.NodeDesigner nodeDesigner;
        [SerializeField]
        private string taskFriendlyName;
        [SerializeField]
        private string taskType;
        [SerializeField]
        private ErrorType type;

        public ErrorDetails(ErrorType type, Task task, string fieldName)
        {
            this.type = type;
            this.nodeDesigner = task.NodeData.NodeDesigner as BehaviorDesigner.Editor.NodeDesigner;
            this.taskFriendlyName = task.FriendlyName;
            this.taskType = task.GetType().ToString();
            this.fieldName = fieldName;
        }

        public string FieldName
        {
            get
            {
                return this.fieldName;
            }
        }

        public BehaviorDesigner.Editor.NodeDesigner NodeDesigner
        {
            get
            {
                return this.nodeDesigner;
            }
        }

        public string TaskFriendlyName
        {
            get
            {
                return this.taskFriendlyName;
            }
        }

        public string TaskType
        {
            get
            {
                return this.taskType;
            }
        }

        public ErrorType Type
        {
            get
            {
                return this.type;
            }
        }

        public enum ErrorType
        {
            RequiredField,
            SharedVariable,
            MissingChildren,
            UnknownTask
        }
    }
}

