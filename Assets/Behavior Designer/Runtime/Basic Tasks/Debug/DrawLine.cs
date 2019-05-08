using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks.Basic.UnityUnityEngine.Debug
{
    [TaskCategory("Basic/UnityEngine.Debug")]
    [TaskDescription("Draws a UnityEngine.Debug line")]
    public class DrawLine : Action
    {
        [Tooltip("The start position")]
        public SharedVector3 start;
        [Tooltip("The end position")]
        public SharedVector3 end;
        [Tooltip("The color")]
        public SharedColor color = Color.white;

        public override TaskStatus OnUpdate()
        {
            UnityEngine.Debug.DrawLine(start.Value, end.Value, color.Value);

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            start = Vector3.zero;
            end = Vector3.zero;
            color = Color.white;
        }
    }
}