namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class SkipErrorCheckAttribute : Attribute
    {
    }
}

