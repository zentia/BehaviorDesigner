namespace BehaviorDesigner.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class TaskSerializer
    {
        public List<int> childrenIndex;
        public Vector2 offset;
        public string serialization;
        public List<UnityEngine.Object> unityObjects;
    }
}

