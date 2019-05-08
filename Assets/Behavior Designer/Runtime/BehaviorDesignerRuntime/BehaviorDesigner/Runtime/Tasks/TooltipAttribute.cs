namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple=false, Inherited=false)]
    public class TooltipAttribute : Attribute
    {
        public readonly string mTooltip;

        public TooltipAttribute(string tooltip)
        {
            this.mTooltip = tooltip;
        }

        public string Tooltip
        {
            get
            {
                return this.mTooltip;
            }
        }
    }
}

