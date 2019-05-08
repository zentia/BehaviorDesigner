namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple=false, Inherited=false)]
    public class RequiredFieldAttribute : Attribute
    {
    }
}

