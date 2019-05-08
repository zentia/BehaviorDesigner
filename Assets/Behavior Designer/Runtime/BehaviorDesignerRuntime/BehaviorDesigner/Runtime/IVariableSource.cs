namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections.Generic;

    public interface IVariableSource
    {
        List<SharedVariable> GetAllVariables();
        SharedVariable GetVariable(string name);
        void SetAllVariables(List<SharedVariable> variables);
        void SetVariable(string name, SharedVariable sharedVariable);
        void UpdateVariableName(SharedVariable sharedVariable, string name);
    }
}

