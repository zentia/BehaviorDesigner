namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Reflection;
    using UnityEngine;

    public class ObjectDrawer
    {
        protected ObjectDrawerAttribute attribute;
        protected System.Reflection.FieldInfo fieldInfo;
        protected BehaviorDesigner.Runtime.Tasks.Task task;
        protected object value;

        public virtual void OnGUI(GUIContent label)
        {
        }

        public ObjectDrawerAttribute Attribute
        {
            get
            {
                return this.attribute;
            }
            set
            {
                this.attribute = value;
            }
        }

        public System.Reflection.FieldInfo FieldInfo
        {
            get
            {
                return this.fieldInfo;
            }
            set
            {
                this.fieldInfo = value;
            }
        }

        public BehaviorDesigner.Runtime.Tasks.Task Task
        {
            get
            {
                return this.task;
            }
            set
            {
                this.task = value;
            }
        }

        public object Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
            }
        }
    }
}

