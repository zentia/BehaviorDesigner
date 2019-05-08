namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using UnityEngine;

    [Serializable]
    public class BehaviorSource : IVariableSource
    {
        public string behaviorDescription;
        private int behaviorID;
        public string behaviorName;
        private List<Task> mDetachedTasks;
        private Task mEntryTask;
        [NonSerialized]
        private bool mHasSerialized;
        [SerializeField]
        private IBehavior mOwner;
        private Task mRootTask;
        private Dictionary<string, int> mSharedVariableIndex;
        [SerializeField]
        private TaskSerializationData mTaskData;
        private List<SharedVariable> mVariables;

        public BehaviorSource()
        {
            this.behaviorName = "Behavior";
            this.behaviorDescription = string.Empty;
            this.behaviorID = -1;
        }

        public BehaviorSource(IBehavior owner)
        {
            this.behaviorName = "Behavior";
            this.behaviorDescription = string.Empty;
            this.behaviorID = -1;
            this.Initialize(owner);
        }

        public bool CheckForSerialization(bool force, BehaviorSource behaviorSource = null)
        {
            if (((behaviorSource == null) ? this.HasSerialized : behaviorSource.HasSerialized) && !force)
            {
                return false;
            }
            if (behaviorSource != null)
            {
                behaviorSource.HasSerialized = true;
            }
            else
            {
                this.HasSerialized = true;
            }
            if ((this.mTaskData != null) && !string.IsNullOrEmpty(this.mTaskData.JSONSerialization))
            {
                DeserializeJSON.Load(this.mTaskData, (behaviorSource != null) ? behaviorSource : this);
            }
            else
            {
                BinaryDeserialization.Load(this.mTaskData, (behaviorSource != null) ? behaviorSource : this);
            }
            return true;
        }

        public List<SharedVariable> GetAllVariables()
        {
            this.CheckForSerialization(false, null);
            return this.mVariables;
        }

        public SharedVariable GetVariable(string name)
        {
            if ((name != null) && (this.mVariables != null))
            {
                int num;
                if ((this.mSharedVariableIndex == null) || (this.mSharedVariableIndex.Count != this.mVariables.Count))
                {
                    this.UpdateVariablesIndex();
                }
                if (this.mSharedVariableIndex.TryGetValue(name, out num))
                {
                    return this.mVariables[num];
                }
            }
            return null;
        }

        public void Initialize(IBehavior owner)
        {
            this.mOwner = owner;
        }

        public void Load(out Task entryTask, out Task rootTask, out List<Task> detachedTasks)
        {
            entryTask = this.mEntryTask;
            rootTask = this.mRootTask;
            detachedTasks = this.mDetachedTasks;
        }

        public void Save(Task entryTask, Task rootTask, List<Task> detachedTasks)
        {
            this.mEntryTask = entryTask;
            this.mRootTask = rootTask;
            this.mDetachedTasks = detachedTasks;
        }

        public void SetAllVariables(List<SharedVariable> variables)
        {
            this.mVariables = variables;
            this.UpdateVariablesIndex();
        }

        public void SetVariable(string name, SharedVariable sharedVariable)
        {
            int num;
            if (this.mVariables == null)
            {
                this.mVariables = new List<SharedVariable>();
            }
            else if (this.mSharedVariableIndex == null)
            {
                this.UpdateVariablesIndex();
            }
            sharedVariable.Name = name;
            if ((this.mSharedVariableIndex != null) && this.mSharedVariableIndex.TryGetValue(name, out num))
            {
                SharedVariable variable = this.mVariables[num];
                if (!variable.GetType().Equals(typeof(SharedVariable)) && !variable.GetType().Equals(sharedVariable.GetType()))
                {
                    Debug.LogError(string.Format("Error: Unable to set SharedVariable {0} - the variable type {1} does not match the existing type {2}", name, variable.GetType(), sharedVariable.GetType()));
                }
                else
                {
                    variable.SetValue(sharedVariable.GetValue());
                }
            }
            else
            {
                this.mVariables.Add(sharedVariable);
                this.UpdateVariablesIndex();
            }
        }

        public override string ToString()
        {
            if ((this.mOwner != null) && (this.mOwner.GetObject() != null))
            {
                return string.Format("{0} - {1}", this.Owner.GetOwnerName(), this.behaviorName);
            }
            return this.behaviorName;
        }

        public void UpdateVariableName(SharedVariable sharedVariable, string name)
        {
            this.CheckForSerialization(false, null);
            sharedVariable.Name = name;
            this.UpdateVariablesIndex();
        }

        private void UpdateVariablesIndex()
        {
            if (this.mVariables == null)
            {
                if (this.mSharedVariableIndex != null)
                {
                    this.mSharedVariableIndex = null;
                }
            }
            else
            {
                if (this.mSharedVariableIndex == null)
                {
                    this.mSharedVariableIndex = new Dictionary<string, int>(this.mVariables.Count);
                }
                else
                {
                    this.mSharedVariableIndex.Clear();
                }
                for (int i = 0; i < this.mVariables.Count; i++)
                {
                    if (this.mVariables[i] != null)
                    {
                        this.mSharedVariableIndex.Add(this.mVariables[i].Name, i);
                    }
                }
            }
        }

        public int BehaviorID
        {
            get
            {
                return this.behaviorID;
            }
            set
            {
                this.behaviorID = value;
            }
        }

        public List<Task> DetachedTasks
        {
            get
            {
                return this.mDetachedTasks;
            }
            set
            {
                this.mDetachedTasks = value;
            }
        }

        public Task EntryTask
        {
            get
            {
                return this.mEntryTask;
            }
            set
            {
                this.mEntryTask = value;
            }
        }

        public bool HasSerialized
        {
            get
            {
                return this.mHasSerialized;
            }
            set
            {
                this.mHasSerialized = value;
            }
        }

        public IBehavior Owner
        {
            get
            {
                return this.mOwner;
            }
            set
            {
                this.mOwner = value;
            }
        }

        public Task RootTask
        {
            get
            {
                return this.mRootTask;
            }
            set
            {
                this.mRootTask = value;
            }
        }

        public TaskSerializationData TaskData
        {
            get
            {
                return this.mTaskData;
            }
            set
            {
                this.mTaskData = value;
            }
        }

        public List<SharedVariable> Variables
        {
            get
            {
                this.CheckForSerialization(false, null);
                return this.mVariables;
            }
            set
            {
                this.mVariables = value;
                this.UpdateVariablesIndex();
            }
        }
    }
}

