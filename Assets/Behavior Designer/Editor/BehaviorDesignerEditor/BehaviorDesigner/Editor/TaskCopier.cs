namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class TaskCopier : Editor
    {
        public static TaskSerializer CopySerialized(Task task)
        {
            TaskSerializer serializer;
            return new TaskSerializer { offset = (task.NodeData.NodeDesigner as NodeDesigner).GetAbsolutePosition() + new Vector2(10f, 10f), unityObjects = new List<Object>(), serialization = MiniJSON.Serialize(SerializeJSON.SerializeTask(task, false, ref serializer.unityObjects)) };
        }

        public static Task PasteTask(BehaviorSource behaviorSource, TaskSerializer serializer)
        {
            Dictionary<int, Task> iDtoTask = new Dictionary<int, Task>();
            return DeserializeJSON.DeserializeTask(behaviorSource, MiniJSON.Deserialize(serializer.serialization) as Dictionary<string, object>, ref iDtoTask, serializer.unityObjects);
        }
    }
}

