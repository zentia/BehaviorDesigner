namespace BehaviorDesigner.Runtime
{
    using System;
    using UnityEngine;

    public abstract class SharedVariable
    {
        [SerializeField]
        private bool mIsGlobal;
        [SerializeField]
        private bool mIsShared;
        [SerializeField]
        private string mName;
        [SerializeField]
        private bool mNetworkSync;
        [SerializeField]
        private string mPropertyMapping;
        [SerializeField]
        private GameObject mPropertyMappingOwner;

        protected SharedVariable()
        {
        }

        public abstract object GetValue();
        public virtual void InitializePropertyMapping(BehaviorSource behaviorSource)
        {
        }

        public abstract void SetValue(object value);
        public void ValueChanged()
        {
        }

        public bool IsGlobal
        {
            get
            {
                return this.mIsGlobal;
            }
            set
            {
                this.mIsGlobal = value;
            }
        }

        public bool IsNone
        {
            get
            {
                return (this.mIsShared && string.IsNullOrEmpty(this.mName));
            }
        }

        public bool IsShared
        {
            get
            {
                return this.mIsShared;
            }
            set
            {
                this.mIsShared = value;
            }
        }

        public string Name
        {
            get
            {
                return this.mName;
            }
            set
            {
                this.mName = value;
            }
        }

        public bool NetworkSync
        {
            get
            {
                return this.mNetworkSync;
            }
            set
            {
                this.mNetworkSync = value;
            }
        }

        public string PropertyMapping
        {
            get
            {
                return this.mPropertyMapping;
            }
            set
            {
                this.mPropertyMapping = value;
            }
        }

        public GameObject PropertyMappingOwner
        {
            get
            {
                return this.mPropertyMappingOwner;
            }
            set
            {
                this.mPropertyMappingOwner = value;
            }
        }
    }
}

