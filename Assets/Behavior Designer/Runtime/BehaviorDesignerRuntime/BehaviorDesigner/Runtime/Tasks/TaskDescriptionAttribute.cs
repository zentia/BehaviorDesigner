namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class TaskDescriptionAttribute : Attribute
    {
        public readonly string mDescription;

        public TaskDescriptionAttribute(string description)
        {
            this.mDescription = description;
        }

        public string Description
        {
            get
            {
                return this.mDescription;
            }
        }
    }
}

