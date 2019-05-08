namespace BehaviorDesigner.Runtime
{
    using System;
    using UnityEngine;

    [Serializable]
    public abstract class ExternalBehavior : ScriptableObject, IBehavior
    {
        [SerializeField]
        private BehaviorDesigner.Runtime.BehaviorSource mBehaviorSource;

        protected ExternalBehavior()
        {
        }

        int IBehavior.GetInstanceID()
        {
            return base.GetInstanceID();
        }

        public BehaviorDesigner.Runtime.BehaviorSource GetBehaviorSource()
        {
            return this.mBehaviorSource;
        }

        public UnityEngine.Object GetObject()
        {
            return this;
        }

        public string GetOwnerName()
        {
            return base.name;
        }

        public SharedVariable GetVariable(string name)
        {
            this.mBehaviorSource.CheckForSerialization(false, null);
            return this.mBehaviorSource.GetVariable(name);
        }

        public void SetBehaviorSource(BehaviorDesigner.Runtime.BehaviorSource behaviorSource)
        {
            this.mBehaviorSource = behaviorSource;
        }

        public void SetVariable(string name, SharedVariable item)
        {
            this.mBehaviorSource.CheckForSerialization(false, null);
            this.mBehaviorSource.SetVariable(name, item);
        }

        public void SetVariableValue(string name, object value)
        {
            SharedVariable variable = this.GetVariable(name);
            if (variable != null)
            {
                variable.SetValue(value);
                variable.ValueChanged();
            }
        }

        public BehaviorDesigner.Runtime.BehaviorSource BehaviorSource
        {
            get
            {
                return this.mBehaviorSource;
            }
            set
            {
                this.mBehaviorSource = value;
            }
        }
    }
}

