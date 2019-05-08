namespace BehaviorDesigner.Runtime.Tasks
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public class HelpURLAttribute : Attribute
    {
        private readonly string mURL;

        public HelpURLAttribute(string url)
        {
            this.mURL = url;
        }

        public string URL
        {
            get
            {
                return this.mURL;
            }
        }
    }
}

