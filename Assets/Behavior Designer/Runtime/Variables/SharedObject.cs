using UnityEngine;
using System.Collections;

namespace BehaviorDesigner.Runtime
{
    [System.Serializable]
    public class SharedObject : SharedVariable<UnityEngine.Object>
    {
        public static explicit operator SharedObject(UnityEngine.Object value) { return new SharedObject { mValue = value }; }
    }
}