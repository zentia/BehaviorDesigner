namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class VariableSerializationData
    {
        [SerializeField]
        public FieldSerializationData fieldSerializationData = new FieldSerializationData();
        [SerializeField]
        public string JSONSerialization = string.Empty;
        [SerializeField]
        public List<int> variableStartIndex = new List<int>();
    }
}

