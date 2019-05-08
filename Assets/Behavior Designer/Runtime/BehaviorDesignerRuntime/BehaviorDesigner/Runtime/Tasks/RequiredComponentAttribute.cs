namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
    public class RequiredComponentAttribute : Attribute
    {
        public readonly Type mComponentType;

        public RequiredComponentAttribute(Type componentType)
        {
            this.mComponentType = componentType;
        }

        public Type ComponentType
        {
            get
            {
                return this.mComponentType;
            }
        }
    }
}

