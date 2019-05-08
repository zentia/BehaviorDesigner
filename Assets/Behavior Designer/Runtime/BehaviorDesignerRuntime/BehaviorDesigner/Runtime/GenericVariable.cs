namespace BehaviorDesigner.Runtime
{
    using System;
    using UnityEngine;

    [Serializable]
    public class GenericVariable
    {
        [SerializeField]
        public string type = "SharedString";
        [SerializeField]
        public SharedVariable value = (Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.SharedString")) as SharedVariable);
    }
}

