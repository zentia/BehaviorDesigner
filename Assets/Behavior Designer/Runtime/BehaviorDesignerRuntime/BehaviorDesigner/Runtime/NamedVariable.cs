namespace BehaviorDesigner.Runtime
{
    using System;
    using UnityEngine;

    [Serializable]
    public class NamedVariable : GenericVariable
    {
        [SerializeField]
        public string name = string.Empty;
    }
}

