namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class TaskNameAttribute : Attribute
    {
        public readonly string mName;

        public TaskNameAttribute(string name)
        {
            this.mName = name;
        }

        public string Name
        {
            get
            {
                return this.mName;
            }
        }
    }
}

