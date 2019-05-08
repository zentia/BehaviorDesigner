namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class TaskIconAttribute : Attribute
    {
        public readonly string mIconPath;

        public TaskIconAttribute(string iconPath)
        {
            this.mIconPath = iconPath;
        }

        public string IconPath
        {
            get
            {
                return this.mIconPath;
            }
        }
    }
}

