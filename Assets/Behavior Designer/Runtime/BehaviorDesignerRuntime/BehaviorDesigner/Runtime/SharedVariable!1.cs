namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public abstract class SharedVariable<T> : SharedVariable
    {
        private Func<T> mGetter;
        private Action<T> mSetter;
        [SerializeField]
        protected T mValue;

        protected SharedVariable()
        {
        }

        public override object GetValue()
        {
            return this.Value;
        }

        public override void InitializePropertyMapping(BehaviorSource behaviorSource)
        {
            if ((Application.isPlaying && (behaviorSource.Owner.GetObject() is Behavior)) && !string.IsNullOrEmpty(base.PropertyMapping))
            {
                GameObject propertyMappingOwner;
                char[] separator = new char[] { '/' };
                string[] strArray = base.PropertyMapping.Split(separator);
                if (!object.Equals(base.PropertyMappingOwner, null))
                {
                    propertyMappingOwner = base.PropertyMappingOwner;
                }
                else
                {
                    propertyMappingOwner = (behaviorSource.Owner.GetObject() as Behavior).gameObject;
                }
                Component firstArgument = propertyMappingOwner.GetComponent(TaskUtility.GetTypeWithinAssembly(strArray[0]));
                PropertyInfo property = firstArgument.GetType().GetProperty(strArray[1]);
                if (property != null)
                {
                    MethodInfo getMethod = property.GetGetMethod();
                    if (getMethod != null)
                    {
                        this.mGetter = (Func<T>) Delegate.CreateDelegate(typeof(Func<T>), firstArgument, getMethod);
                    }
                    getMethod = property.GetSetMethod();
                    if (getMethod != null)
                    {
                        this.mSetter = (Action<T>) Delegate.CreateDelegate(typeof(Action<T>), firstArgument, getMethod);
                    }
                }
            }
        }

        public override void SetValue(object value)
        {
            if (this.mSetter != null)
            {
                this.mSetter((T) value);
            }
            else
            {
                this.mValue = (T) value;
            }
        }

        public override string ToString()
        {
            return ((this.Value != null) ? this.Value.ToString() : "(null)");
        }

        public T Value
        {
            get
            {
                return ((this.mGetter == null) ? this.mValue : this.mGetter());
            }
            set
            {
                bool flag = !object.Equals(this.Value, value);
                if (this.mSetter != null)
                {
                    this.mSetter(value);
                }
                else
                {
                    this.mValue = value;
                }
                if (flag)
                {
                    base.ValueChanged();
                }
            }
        }
    }
}

