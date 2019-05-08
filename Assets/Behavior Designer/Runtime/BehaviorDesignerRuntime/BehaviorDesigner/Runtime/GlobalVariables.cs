namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class GlobalVariables : ScriptableObject, IVariableSource
    {
        private static GlobalVariables instance;
        private Dictionary<string, int> mSharedVariableIndex;
        [SerializeField]
        private VariableSerializationData mVariableData;
        [SerializeField]
        private List<SharedVariable> mVariables;

        public void CheckForSerialization(bool force)
        {
            if ((force || (this.mVariables == null)) || ((this.mVariables.Count > 0) && (this.mVariables[0] == null)))
            {
                if ((this.VariableData != null) && !string.IsNullOrEmpty(this.VariableData.JSONSerialization))
                {
                    DeserializeJSON.Load(this.VariableData.JSONSerialization, this);
                }
                else
                {
                    BinaryDeserialization.Load(this);
                }
            }
        }

        public List<SharedVariable> GetAllVariables()
        {
            this.CheckForSerialization(false);
            return this.mVariables;
        }

        public SharedVariable GetVariable(string name)
        {
            if (name != null)
            {
                this.CheckForSerialization(false);
                if (this.mVariables != null)
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
            }
            return null;
        }

        public void SetAllVariables(List<SharedVariable> variables)
        {
            this.mVariables = variables;
        }

        public void SetVariable(string name, SharedVariable sharedVariable)
        {
            int num;
            this.CheckForSerialization(false);
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

        public void SetVariableValue(string name, object value)
        {
            SharedVariable variable = this.GetVariable(name);
            if (variable != null)
            {
                variable.SetValue(value);
                variable.ValueChanged();
            }
        }

        public void UpdateVariableName(SharedVariable sharedVariable, string name)
        {
            this.CheckForSerialization(false);
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

        public static GlobalVariables Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load("BehaviorDesignerGlobalVariables", typeof(GlobalVariables)) as GlobalVariables;
                    if (instance != null)
                    {
                        instance.CheckForSerialization(false);
                    }
                }
                return instance;
            }
        }

        public VariableSerializationData VariableData
        {
            get
            {
                return this.mVariableData;
            }
            set
            {
                this.mVariableData = value;
            }
        }

        public List<SharedVariable> Variables
        {
            get
            {
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

