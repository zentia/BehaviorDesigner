namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class TaskCategoryAttribute : Attribute
    {
        public readonly string mCategory;

        public TaskCategoryAttribute(string category)
        {
            this.mCategory = category;
        }

        public string Category
        {
            get
            {
                return this.mCategory;
            }
        }
    }
}

