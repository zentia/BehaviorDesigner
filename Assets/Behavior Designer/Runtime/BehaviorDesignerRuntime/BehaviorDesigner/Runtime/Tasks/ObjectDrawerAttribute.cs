namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Field, Inherited=true, AllowMultiple=false)]
    public abstract class ObjectDrawerAttribute : Attribute
    {
        protected ObjectDrawerAttribute()
        {
        }
    }
}

