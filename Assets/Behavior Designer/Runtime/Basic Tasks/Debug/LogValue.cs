using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks.Basic.UnityUnityEngine.Debug
{
    [TaskCategory("Basic/UnityEngine.Debug")]
    [TaskDescription("Log a variable value.")]
    public class LogValue : Action
    {
        [Tooltip("The variable to output")]
        public SharedGenericVariable variable;

        public override TaskStatus OnUpdate()
        {
            UnityEngine.Debug.Log(variable.Value.value.GetValue());

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            variable = null;
        }
    }
}