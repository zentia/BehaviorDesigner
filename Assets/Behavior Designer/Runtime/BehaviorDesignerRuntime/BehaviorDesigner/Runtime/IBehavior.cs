namespace BehaviorDesigner.Runtime
{
    using System;
    using UnityEngine;

    public interface IBehavior
    {
        BehaviorSource GetBehaviorSource();
        int GetInstanceID();
        UnityEngine.Object GetObject();
        string GetOwnerName();
        SharedVariable GetVariable(string name);
        void SetBehaviorSource(BehaviorSource behaviorSource);
        void SetVariable(string name, SharedVariable item);
        void SetVariableValue(string name, object value);
    }
}

