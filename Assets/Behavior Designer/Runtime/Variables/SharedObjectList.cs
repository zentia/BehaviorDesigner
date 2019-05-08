using UnityEngine;
using System.Collections.Generic;

namespace BehaviorDesigner.Runtime
{
    [System.Serializable]
    public class SharedObjectList : SharedVariable<List<UnityEngine.Object>>
    {
        public static implicit operator SharedObjectList(List<UnityEngine.Object> value) { return new SharedObjectList { mValue = value }; }
    }
}