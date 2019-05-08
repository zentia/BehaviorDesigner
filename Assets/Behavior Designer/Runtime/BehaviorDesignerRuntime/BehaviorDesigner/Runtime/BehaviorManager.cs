namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    [AddComponentMenu("Behavior Designer/Behavior Manager")]
    public class BehaviorManager : MonoBehaviour
    {
        private bool atBreakpoint;
        private Dictionary<Behavior, BehaviorTree> behaviorTreeMap = new Dictionary<Behavior, BehaviorTree>();
        private List<BehaviorTree> behaviorTrees = new List<BehaviorTree>();
        private List<int> conditionalParentIndexes = new List<int>();
        private static MethodInfo dialogueSystemStopMethod;
        private bool dirty;
        [SerializeField]
        private ExecutionsPerTickType executionsPerTick;
        private static MethodInfo iCodeStopMethod;
        public static BehaviorManager instance;
        private static object[] invokeParameters;
        [SerializeField]
        private int maxTaskExecutionsPerTick = 100;
        private Dictionary<object, ThirdPartyTask> objectTaskMap = new Dictionary<object, ThirdPartyTask>();
        public BehaviorManagerHandler onEnableBehavior;
        public BehaviorManagerHandler onTaskBreakpoint;
        private Dictionary<Behavior, BehaviorTree> pausedBehaviorTrees = new Dictionary<Behavior, BehaviorTree>();
        private static MethodInfo playMakerStopMethod;
        private Dictionary<ThirdPartyTask, object> taskObjectMap = new Dictionary<ThirdPartyTask, object>(new ThirdPartyTaskComparer());
        private ThirdPartyTask thirdPartyTaskCompare = new ThirdPartyTask();
        [SerializeField]
        private UpdateIntervalType updateInterval;
        [SerializeField]
        private float updateIntervalSeconds;
        private WaitForSeconds updateWait;
        private static MethodInfo uScriptStopMethod;
        private static MethodInfo uSequencerStopMethod;

        private int AddToTaskList(BehaviorTree behaviorTree, Task task, ref bool hasExternalBehavior, TaskAddData data)
        {
            if (task == null)
            {
                return -3;
            }
            task.GameObject = behaviorTree.behavior.gameObject;
            task.Transform = behaviorTree.behavior.transform;
            task.Owner = behaviorTree.behavior;
            if (task is BehaviorReference)
            {
                BehaviorSource[] sourceArray = null;
                BehaviorReference reference = task as BehaviorReference;
                if (reference == null)
                {
                    return -2;
                }
                ExternalBehavior[] externalBehaviors = null;
                externalBehaviors = reference.GetExternalBehaviors();
                if (externalBehaviors == null)
                {
                    return -2;
                }
                sourceArray = new BehaviorSource[externalBehaviors.Length];
                for (int i = 0; i < externalBehaviors.Length; i++)
                {
                    if (externalBehaviors[i] == null)
                    {
                        data.errorTask = behaviorTree.taskList.Count;
                        data.errorTaskName = string.IsNullOrEmpty(task.FriendlyName) ? task.GetType().ToString() : task.FriendlyName;
                        return -5;
                    }
                    sourceArray[i] = externalBehaviors[i].BehaviorSource;
                    sourceArray[i].Owner = externalBehaviors[i];
                }
                if (sourceArray == null)
                {
                    return -2;
                }
                ParentTask parentTask = data.parentTask;
                int parentIndex = data.parentIndex;
                int compositeParentIndex = data.compositeParentIndex;
                Vector2 vector = data.offset = task.NodeData.Offset;
                data.depth++;
                for (int j = 0; j < sourceArray.Length; j++)
                {
                    BehaviorSource behaviorSource = ObjectPool.Get<BehaviorSource>();
                    behaviorSource.Initialize(sourceArray[j].Owner);
                    sourceArray[j].CheckForSerialization(true, behaviorSource);
                    Task rootTask = behaviorSource.RootTask;
                    if (rootTask != null)
                    {
                        if (rootTask is ParentTask)
                        {
                            rootTask.NodeData.Collapsed = (task as BehaviorReference).collapsed;
                        }
                        if (reference.variables != null)
                        {
                            for (int k = 0; k < reference.variables.Length; k++)
                            {
                                if (data.overrideFields == null)
                                {
                                    data.overrideFields = ObjectPool.Get<Dictionary<string, TaskAddData.OverrideFieldValue>>();
                                    data.overrideFields.Clear();
                                }
                                if (!data.overrideFields.ContainsKey(reference.variables[k].Value.name))
                                {
                                    TaskAddData.OverrideFieldValue value2 = ObjectPool.Get<TaskAddData.OverrideFieldValue>();
                                    value2.Initialize(reference.variables[k].Value, data.depth);
                                    data.overrideFields.Add(reference.variables[k].Value.name, value2);
                                }
                            }
                        }
                        if (behaviorSource.Variables != null)
                        {
                            for (int m = 0; m < behaviorSource.Variables.Count; m++)
                            {
                                SharedVariable item = null;
                                item = behaviorTree.behavior.GetVariable(behaviorSource.Variables[m].Name);
                                if (item == null)
                                {
                                    item = behaviorSource.Variables[m];
                                    behaviorTree.behavior.SetVariable(item.Name, item);
                                }
                                if (data.overrideFields == null)
                                {
                                    data.overrideFields = ObjectPool.Get<Dictionary<string, TaskAddData.OverrideFieldValue>>();
                                    data.overrideFields.Clear();
                                }
                                if (!data.overrideFields.ContainsKey(item.Name))
                                {
                                    TaskAddData.OverrideFieldValue value3 = ObjectPool.Get<TaskAddData.OverrideFieldValue>();
                                    value3.Initialize(item, data.depth);
                                    data.overrideFields.Add(item.Name, value3);
                                }
                            }
                        }
                        ObjectPool.Return<BehaviorSource>(behaviorSource);
                        if (j > 0)
                        {
                            data.parentTask = parentTask;
                            data.parentIndex = parentIndex;
                            data.compositeParentIndex = compositeParentIndex;
                            data.offset = vector;
                            if ((data.parentTask == null) || (j >= data.parentTask.MaxChildren()))
                            {
                                return -4;
                            }
                            behaviorTree.parentIndex.Add(data.parentIndex);
                            behaviorTree.relativeChildIndex.Add(data.parentTask.Children.Count);
                            behaviorTree.parentCompositeIndex.Add(data.compositeParentIndex);
                            behaviorTree.childrenIndex[data.parentIndex].Add(behaviorTree.taskList.Count);
                            data.parentTask.AddChild(rootTask, data.parentTask.Children.Count);
                        }
                        hasExternalBehavior = true;
                        bool fromExternalTask = data.fromExternalTask;
                        data.fromExternalTask = true;
                        int num7 = 0;
                        num7 = this.AddToTaskList(behaviorTree, rootTask, ref hasExternalBehavior, data);
                        if (num7 < 0)
                        {
                            return num7;
                        }
                        data.fromExternalTask = fromExternalTask;
                    }
                    else
                    {
                        ObjectPool.Return<BehaviorSource>(behaviorSource);
                        return -2;
                    }
                }
                if (data.overrideFields != null)
                {
                    Dictionary<string, TaskAddData.OverrideFieldValue> dictionary = ObjectPool.Get<Dictionary<string, TaskAddData.OverrideFieldValue>>();
                    dictionary.Clear();
                    foreach (KeyValuePair<string, TaskAddData.OverrideFieldValue> pair in data.overrideFields)
                    {
                        if (pair.Value.Depth != data.depth)
                        {
                            dictionary.Add(pair.Key, pair.Value);
                        }
                    }
                    ObjectPool.Return<Dictionary<string, TaskAddData.OverrideFieldValue>>(data.overrideFields);
                    data.overrideFields = dictionary;
                }
                data.depth--;
            }
            else
            {
                if ((behaviorTree.taskList.Count == 0) && task.NodeData.Disabled)
                {
                    return -6;
                }
                task.ReferenceID = behaviorTree.taskList.Count;
                behaviorTree.taskList.Add(task);
                if (data.overrideFields != null)
                {
                    this.OverrideFields(data, task);
                }
                if (data.fromExternalTask)
                {
                    if (data.parentTask == null)
                    {
                        task.NodeData.Offset = behaviorTree.behavior.GetBehaviorSource().RootTask.NodeData.Offset;
                    }
                    else
                    {
                        int index = behaviorTree.relativeChildIndex[behaviorTree.relativeChildIndex.Count - 1];
                        data.parentTask.ReplaceAddChild(task, index);
                        if (data.offset != Vector2.zero)
                        {
                            task.NodeData.Offset = data.offset;
                            data.offset = Vector2.zero;
                        }
                    }
                }
                if (task is ParentTask)
                {
                    ParentTask task4 = task as ParentTask;
                    if ((task4.Children == null) || (task4.Children.Count == 0))
                    {
                        data.errorTask = behaviorTree.taskList.Count - 1;
                        data.errorTaskName = string.IsNullOrEmpty(behaviorTree.taskList[data.errorTask].FriendlyName) ? behaviorTree.taskList[data.errorTask].GetType().ToString() : behaviorTree.taskList[data.errorTask].FriendlyName;
                        return -1;
                    }
                    int num10 = behaviorTree.taskList.Count - 1;
                    List<int> list = ObjectPool.Get<List<int>>();
                    list.Clear();
                    behaviorTree.childrenIndex.Add(list);
                    list = ObjectPool.Get<List<int>>();
                    list.Clear();
                    behaviorTree.childConditionalIndex.Add(list);
                    int count = task4.Children.Count;
                    for (int n = 0; n < count; n++)
                    {
                        behaviorTree.parentIndex.Add(num10);
                        behaviorTree.relativeChildIndex.Add(n);
                        behaviorTree.childrenIndex[num10].Add(behaviorTree.taskList.Count);
                        data.parentTask = task as ParentTask;
                        data.parentIndex = num10;
                        if (task is Composite)
                        {
                            data.compositeParentIndex = num10;
                        }
                        behaviorTree.parentCompositeIndex.Add(data.compositeParentIndex);
                        int num9 = this.AddToTaskList(behaviorTree, task4.Children[n], ref hasExternalBehavior, data);
                        if (num9 < 0)
                        {
                            if (num9 == -3)
                            {
                                data.errorTask = num10;
                                data.errorTaskName = string.IsNullOrEmpty(behaviorTree.taskList[data.errorTask].FriendlyName) ? behaviorTree.taskList[data.errorTask].GetType().ToString() : behaviorTree.taskList[data.errorTask].FriendlyName;
                            }
                            return num9;
                        }
                    }
                }
                else
                {
                    behaviorTree.childrenIndex.Add(null);
                    behaviorTree.childConditionalIndex.Add(null);
                    if (task is Conditional)
                    {
                        int num13 = behaviorTree.taskList.Count - 1;
                        int num14 = behaviorTree.parentCompositeIndex[num13];
                        if (num14 != -1)
                        {
                            behaviorTree.childConditionalIndex[num14].Add(num13);
                        }
                    }
                }
            }
            return 0;
        }

        public void Awake()
        {
            instance = this;
            this.UpdateIntervalChanged();
        }

        public void BehaviorOnCollisionEnter(Collision collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionEnter(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionEnter(collision);
                    }
                }
            }
        }

        public void BehaviorOnCollisionEnter2D(Collision2D collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionEnter2D(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionEnter2D(collision);
                    }
                }
            }
        }

        public void BehaviorOnCollisionExit(Collision collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionExit(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionExit(collision);
                    }
                }
            }
        }

        public void BehaviorOnCollisionExit2D(Collision2D collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionExit2D(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionExit2D(collision);
                    }
                }
            }
        }

        public void BehaviorOnCollisionStay(Collision collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionStay(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionStay(collision);
                    }
                }
            }
        }

        public void BehaviorOnCollisionStay2D(Collision2D collision, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnCollisionStay2D(collision);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnCollisionStay2D(collision);
                    }
                }
            }
        }

        public void BehaviorOnControllerColliderHit(ControllerColliderHit hit, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnControllerColliderHit(hit);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnControllerColliderHit(hit);
                    }
                }
            }
        }

        public void BehaviorOnTriggerEnter(Collider other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerEnter(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerEnter(other);
                    }
                }
            }
        }

        public void BehaviorOnTriggerEnter2D(Collider2D other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerEnter2D(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerEnter2D(other);
                    }
                }
            }
        }

        public void BehaviorOnTriggerExit(Collider other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerExit(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerExit(other);
                    }
                }
            }
        }

        public void BehaviorOnTriggerExit2D(Collider2D other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerExit2D(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerExit2D(other);
                    }
                }
            }
        }

        public void BehaviorOnTriggerStay(Collider other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerStay(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerStay(other);
                    }
                }
            }
        }

        public void BehaviorOnTriggerStay2D(Collider2D other, Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int index;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.activeStack.Count; i++)
                {
                    if (tree.activeStack[i].Count != 0)
                    {
                        index = tree.activeStack[i].Peek();
                        while (index != -1)
                        {
                            if (tree.taskList[index].NodeData.Disabled)
                            {
                                break;
                            }
                            tree.taskList[index].OnTriggerStay2D(other);
                            index = tree.parentIndex[index];
                        }
                    }
                }
                for (int j = 0; j < tree.conditionalReevaluate.Count; j++)
                {
                    index = tree.conditionalReevaluate[j].index;
                    if (!tree.taskList[index].NodeData.Disabled && (tree.conditionalReevaluate[j].compositeIndex != -1))
                    {
                        tree.taskList[index].OnTriggerStay2D(other);
                    }
                }
            }
        }

        [DebuggerHidden]
        private IEnumerator CoroutineUpdate()
        {
            return new <CoroutineUpdate>c__Iterator0 { <>f__this = this };
        }

        public void DisableBehavior(Behavior behavior)
        {
            this.DisableBehavior(behavior, false);
        }

        public void DisableBehavior(Behavior behavior, bool paused)
        {
            if (!this.IsBehaviorEnabled(behavior) || !this.behaviorTreeMap.ContainsKey(behavior))
            {
                if (!this.pausedBehaviorTrees.ContainsKey(behavior) || paused)
                {
                    return;
                }
                this.EnableBehavior(behavior);
            }
            if (behavior.LogTaskChanges)
            {
                UnityEngine.Debug.Log(string.Format("{0}: {1} {2}", this.RoundedTime(), !paused ? "Disabling" : "Pausing", behavior.ToString()));
            }
            BehaviorTree tree = this.behaviorTreeMap[behavior];
            if (paused)
            {
                if (!this.pausedBehaviorTrees.ContainsKey(behavior))
                {
                    this.pausedBehaviorTrees.Add(behavior, tree);
                    behavior.ExecutionStatus = TaskStatus.Inactive;
                    for (int i = 0; i < tree.taskList.Count; i++)
                    {
                        tree.taskList[i].OnPause(true);
                    }
                    this.behaviorTrees.Remove(tree);
                }
            }
            else
            {
                TaskStatus success = TaskStatus.Success;
                for (int j = tree.activeStack.Count - 1; j > -1; j--)
                {
                    while (tree.activeStack[j].Count > 0)
                    {
                        int count = tree.activeStack[j].Count;
                        this.PopTask(tree, tree.activeStack[j].Peek(), j, ref success, true, false);
                        if (count == 1)
                        {
                            break;
                        }
                    }
                }
                this.RemoveChildConditionalReevaluate(tree, -1);
                for (int k = 0; k < tree.taskList.Count; k++)
                {
                    tree.taskList[k].OnBehaviorComplete();
                }
                behavior.ExecutionStatus = success;
                behavior.OnBehaviorEnded();
                this.behaviorTreeMap.Remove(behavior);
                this.behaviorTrees.Remove(tree);
                ObjectPool.Return<BehaviorTree>(tree);
            }
        }

        public void EnableBehavior(Behavior behavior)
        {
            if (!this.IsBehaviorEnabled(behavior))
            {
                BehaviorTree tree;
                if (this.pausedBehaviorTrees.TryGetValue(behavior, out tree))
                {
                    this.behaviorTrees.Add(tree);
                    this.pausedBehaviorTrees.Remove(behavior);
                    behavior.ExecutionStatus = TaskStatus.Running;
                    for (int i = 0; i < tree.taskList.Count; i++)
                    {
                        tree.taskList[i].OnPause(false);
                    }
                }
                else
                {
                    TaskAddData data = ObjectPool.Get<TaskAddData>();
                    data.Initialize();
                    behavior.CheckForSerialization();
                    Task rootTask = behavior.GetBehaviorSource().RootTask;
                    if (rootTask == null)
                    {
                        UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains no root task. This behavior will be disabled.", behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name));
                    }
                    else
                    {
                        tree = ObjectPool.Get<BehaviorTree>();
                        tree.Initialize(behavior);
                        tree.parentIndex.Add(-1);
                        tree.relativeChildIndex.Add(-1);
                        tree.parentCompositeIndex.Add(-1);
                        bool hasExternalBehavior = behavior.ExternalBehavior != null;
                        int num2 = this.AddToTaskList(tree, rootTask, ref hasExternalBehavior, data);
                        if (num2 >= 0)
                        {
                            this.dirty = true;
                            if (behavior.ExternalBehavior != null)
                            {
                                behavior.GetBehaviorSource().EntryTask = behavior.ExternalBehavior.BehaviorSource.EntryTask;
                            }
                            behavior.GetBehaviorSource().RootTask = tree.taskList[0];
                            if (behavior.ResetValuesOnRestart)
                            {
                                behavior.SaveResetValues();
                            }
                            Stack<int> item = ObjectPool.Get<Stack<int>>();
                            item.Clear();
                            tree.activeStack.Add(item);
                            tree.interruptionIndex.Add(-1);
                            tree.nonInstantTaskStatus.Add(TaskStatus.Inactive);
                            if (tree.behavior.LogTaskChanges)
                            {
                                for (int k = 0; k < tree.taskList.Count; k++)
                                {
                                    object[] args = new object[] { this.RoundedTime(), tree.taskList[k].FriendlyName, tree.taskList[k].GetType(), k, tree.taskList[k].GetHashCode() };
                                    UnityEngine.Debug.Log(string.Format("{0}: Task {1} ({2}, index {3}) {4}", args));
                                }
                            }
                            for (int j = 0; j < tree.taskList.Count; j++)
                            {
                                tree.taskList[j].OnAwake();
                            }
                            this.behaviorTrees.Add(tree);
                            this.behaviorTreeMap.Add(behavior, tree);
                            if (this.onEnableBehavior != null)
                            {
                                this.onEnableBehavior();
                            }
                            if (!tree.taskList[0].NodeData.Disabled)
                            {
                                tree.behavior.OnBehaviorStarted();
                                behavior.ExecutionStatus = TaskStatus.Running;
                                this.PushTask(tree, 0, 0);
                            }
                        }
                        else
                        {
                            tree = null;
                            int num5 = num2;
                            switch ((num5 + 6))
                            {
                                case 0:
                                {
                                    object[] objArray4 = new object[] { behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name, data.errorTaskName, data.errorTask };
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains a root task which is disabled. This behavior will be disabled.", objArray4));
                                    break;
                                }
                                case 1:
                                {
                                    object[] objArray3 = new object[] { behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name, data.errorTaskName, data.errorTask };
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains a Behavior Tree Reference task ({2} (index {3})) that which has an element with a null value in the externalBehaviors array. This behavior will be disabled.", objArray3));
                                    break;
                                }
                                case 2:
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains multiple external behavior trees at the root task or as a child of a parent task which cannot contain so many children (such as a decorator task). This behavior will be disabled.", behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name));
                                    break;

                                case 3:
                                {
                                    object[] objArray2 = new object[] { behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name, data.errorTaskName, data.errorTask };
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains a null task (referenced from parent task {2} (index {3})). This behavior will be disabled.", objArray2));
                                    break;
                                }
                                case 4:
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" cannot find the referenced external task. This behavior will be disabled.", behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name));
                                    break;

                                case 5:
                                {
                                    object[] objArray1 = new object[] { behavior.GetBehaviorSource().behaviorName, behavior.gameObject.name, data.errorTaskName, data.errorTask };
                                    UnityEngine.Debug.LogError(string.Format("The behavior \"{0}\" on GameObject \"{1}\" contains a parent task ({2} (index {3})) with no children. This behavior will be disabled.", objArray1));
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private int FindLCA(BehaviorTree behaviorTree, int taskIndex1, int taskIndex2)
        {
            int num;
            HashSet<int> set = ObjectPool.Get<HashSet<int>>();
            set.Clear();
            for (num = taskIndex1; num != -1; num = behaviorTree.parentIndex[num])
            {
                set.Add(num);
            }
            num = taskIndex2;
            while (!set.Contains(num))
            {
                num = behaviorTree.parentIndex[num];
            }
            return num;
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < this.behaviorTrees.Count; i++)
            {
                if (this.behaviorTrees[i].behavior.HasEvent[14])
                {
                    for (int j = this.behaviorTrees[i].activeStack.Count - 1; j > -1; j--)
                    {
                        int num3 = this.behaviorTrees[i].activeStack[j].Peek();
                        this.behaviorTrees[i].taskList[num3].OnFixedUpdate();
                    }
                }
            }
        }

        public List<Task> GetActiveTasks(Behavior behavior)
        {
            if (!this.IsBehaviorEnabled(behavior))
            {
                return null;
            }
            List<Task> list = new List<Task>();
            BehaviorTree tree = this.behaviorTreeMap[behavior];
            for (int i = 0; i < tree.activeStack.Count; i++)
            {
                Task item = tree.taskList[tree.activeStack[i].Peek()];
                if (item is BehaviorDesigner.Runtime.Tasks.Action)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public List<Task> GetTaskList(Behavior behavior)
        {
            if (!this.IsBehaviorEnabled(behavior))
            {
                return null;
            }
            BehaviorTree tree = this.behaviorTreeMap[behavior];
            return tree.taskList;
        }

        public void Interrupt(Behavior behavior, Task task)
        {
            this.Interrupt(behavior, task, task);
        }

        public void Interrupt(Behavior behavior, Task task, Task interruptionTask)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                int num = -1;
                BehaviorTree tree = this.behaviorTreeMap[behavior];
                for (int i = 0; i < tree.taskList.Count; i++)
                {
                    if (tree.taskList[i].ReferenceID == task.ReferenceID)
                    {
                        num = i;
                        break;
                    }
                }
                if (num > -1)
                {
                    for (int j = 0; j < tree.activeStack.Count; j++)
                    {
                        if (tree.activeStack[j].Count > 0)
                        {
                            for (int k = tree.activeStack[j].Peek(); k != -1; k = tree.parentIndex[k])
                            {
                                if (k == num)
                                {
                                    tree.interruptionIndex[j] = num;
                                    if (behavior.LogTaskChanges)
                                    {
                                        object[] args = new object[] { this.RoundedTime(), tree.behavior.ToString(), task.FriendlyName, task.GetType().ToString(), num, j };
                                        UnityEngine.Debug.Log(string.Format("{0}: {1}: Interrupt task {2} ({3}) with index {4} at stack index {5}", args));
                                    }
                                    interruptionTask.NodeData.InterruptTime = Time.realtimeSinceStartup;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool IsBehaviorEnabled(Behavior behavior)
        {
            return ((((this.behaviorTreeMap != null) && (this.behaviorTreeMap.Count > 0)) && (behavior != null)) && (behavior.ExecutionStatus == TaskStatus.Running));
        }

        private bool IsChild(BehaviorTree behaviorTree, int taskIndex1, int taskIndex2)
        {
            for (int i = taskIndex1; i != -1; i = behaviorTree.parentIndex[i])
            {
                if (i == taskIndex2)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsParentTask(BehaviorTree behaviorTree, int possibleParent, int possibleChild)
        {
            int num = 0;
            for (int i = possibleChild; i != -1; i = num)
            {
                num = behaviorTree.parentIndex[i];
                if (num == possibleParent)
                {
                    return true;
                }
            }
            return false;
        }

        public void LateUpdate()
        {
            for (int i = 0; i < this.behaviorTrees.Count; i++)
            {
                if (this.behaviorTrees[i].behavior.HasEvent[13])
                {
                    for (int j = this.behaviorTrees[i].activeStack.Count - 1; j > -1; j--)
                    {
                        int num3 = this.behaviorTrees[i].activeStack[j].Peek();
                        this.behaviorTrees[i].taskList[num3].OnLateUpdate();
                    }
                }
            }
        }

        public bool MapObjectToTask(object objectKey, Task task, ThirdPartyObjectType objectType)
        {
            if (!this.objectTaskMap.ContainsKey(objectKey))
            {
                ThirdPartyTask task2 = ObjectPool.Get<ThirdPartyTask>();
                task2.Initialize(task, objectType);
                this.objectTaskMap.Add(objectKey, task2);
                this.taskObjectMap.Add(task2, objectKey);
                return true;
            }
            string str = string.Empty;
            switch (objectType)
            {
                case ThirdPartyObjectType.PlayMaker:
                    str = "PlayMaker FSM";
                    break;

                case ThirdPartyObjectType.uScript:
                    str = "uScript Graph";
                    break;

                case ThirdPartyObjectType.DialogueSystem:
                    str = "Dialogue System";
                    break;

                case ThirdPartyObjectType.uSequencer:
                    str = "uSequencer sequence";
                    break;

                case ThirdPartyObjectType.ICode:
                    str = "ICode state machine";
                    break;
            }
            UnityEngine.Debug.LogError(string.Format("Only one behavior can be mapped to the same instance of the {0}.", str));
            return false;
        }

        public void OnApplicationQuit()
        {
            for (int i = this.behaviorTrees.Count - 1; i > -1; i--)
            {
                this.DisableBehavior(this.behaviorTrees[i].behavior);
            }
        }

        public void OnDestroy()
        {
            for (int i = this.behaviorTrees.Count - 1; i > -1; i--)
            {
                this.DisableBehavior(this.behaviorTrees[i].behavior);
            }
        }

        private void OverrideFields(TaskAddData data, object obj)
        {
            if ((obj != null) && !object.Equals(obj, null))
            {
                FieldInfo[] allFields = TaskUtility.GetAllFields(obj.GetType());
                for (int i = 0; i < allFields.Length; i++)
                {
                    object item = allFields[i].GetValue(obj);
                    if (item != null)
                    {
                        if (typeof(SharedVariable).IsAssignableFrom(allFields[i].FieldType))
                        {
                            SharedVariable variable = this.OverrideSharedVariable(data, allFields[i].FieldType, item as SharedVariable);
                            if (variable != null)
                            {
                                allFields[i].SetValue(obj, variable);
                            }
                        }
                        else
                        {
                            System.Type type;
                            if (typeof(IList).IsAssignableFrom(allFields[i].FieldType) && (typeof(SharedVariable).IsAssignableFrom(type = allFields[i].FieldType.GetElementType()) || (allFields[i].FieldType.IsGenericType && typeof(SharedVariable).IsAssignableFrom(type = allFields[i].FieldType.GetGenericArguments()[0]))))
                            {
                                IList<SharedVariable> list = item as IList<SharedVariable>;
                                for (int j = 0; j < list.Count; j++)
                                {
                                    SharedVariable variable2 = this.OverrideSharedVariable(data, type, list[j]);
                                    if (variable2 != null)
                                    {
                                        list[j] = variable2;
                                    }
                                }
                            }
                        }
                        if ((allFields[i].FieldType.IsClass && !allFields[i].FieldType.Equals(typeof(System.Type))) && (!typeof(Delegate).IsAssignableFrom(allFields[i].FieldType) && !data.overiddenFields.Contains(item)))
                        {
                            data.overiddenFields.Add(item);
                            this.OverrideFields(data, item);
                            data.overiddenFields.Remove(item);
                        }
                    }
                }
            }
        }

        private SharedVariable OverrideSharedVariable(TaskAddData data, System.Type fieldType, SharedVariable sharedVariable)
        {
            TaskAddData.OverrideFieldValue value2;
            SharedVariable variable = sharedVariable;
            if (sharedVariable is SharedGenericVariable)
            {
                sharedVariable = ((sharedVariable as SharedGenericVariable).GetValue() as GenericVariable).value;
            }
            if ((sharedVariable != null) && (!string.IsNullOrEmpty(sharedVariable.Name) && data.overrideFields.TryGetValue(sharedVariable.Name, out value2)))
            {
                object obj2 = value2.Value;
                if (obj2 is NamedVariable)
                {
                    NamedVariable variable2 = obj2 as NamedVariable;
                    if (variable2.name.Equals(sharedVariable.Name) && (fieldType.Equals(typeof(SharedVariable)) || variable2.type.Equals(fieldType.Name)))
                    {
                        if (variable2.value.IsShared)
                        {
                            return variable2.value;
                        }
                        sharedVariable.SetValue(variable2.value.GetValue());
                        sharedVariable.IsShared = false;
                    }
                }
                else if (fieldType.Equals(typeof(SharedGenericVariable)))
                {
                    (variable as SharedGenericVariable).Value.value.SetValue((obj2 as SharedVariable).GetValue());
                }
                else if (fieldType.Equals(typeof(SharedVariable)) || obj2.GetType().Equals(fieldType))
                {
                    return (obj2 as SharedVariable);
                }
            }
            return null;
        }

        private void PopTask(BehaviorTree behaviorTree, int taskIndex, int stackIndex, ref TaskStatus status, bool popChildren)
        {
            this.PopTask(behaviorTree, taskIndex, stackIndex, ref status, popChildren, true);
        }

        private void PopTask(BehaviorTree behaviorTree, int taskIndex, int stackIndex, ref TaskStatus status, bool popChildren, bool notifyOnEmptyStack)
        {
            if ((this.IsBehaviorEnabled(behaviorTree.behavior) && (stackIndex < behaviorTree.activeStack.Count)) && ((behaviorTree.activeStack[stackIndex].Count != 0) && (taskIndex == behaviorTree.activeStack[stackIndex].Peek())))
            {
                behaviorTree.activeStack[stackIndex].Pop();
                behaviorTree.nonInstantTaskStatus[stackIndex] = TaskStatus.Inactive;
                this.StopThirdPartyTask(behaviorTree, taskIndex);
                Task task = behaviorTree.taskList[taskIndex];
                task.OnEnd();
                int num = behaviorTree.parentIndex[taskIndex];
                task.NodeData.PushTime = -1f;
                task.NodeData.PopTime = Time.realtimeSinceStartup;
                task.NodeData.ExecutionStatus = status;
                if (behaviorTree.behavior.LogTaskChanges)
                {
                    object[] args = new object[] { this.RoundedTime(), behaviorTree.behavior.ToString(), task.FriendlyName, task.GetType(), taskIndex, stackIndex, (TaskStatus) status };
                    MonoBehaviour.print(string.Format("{0}: {1}: Pop task {2} ({3}, index {4}) at stack index {5} with status {6}", args));
                }
                if (num != -1)
                {
                    if (task is Conditional)
                    {
                        int num2 = behaviorTree.parentCompositeIndex[taskIndex];
                        if (num2 != -1)
                        {
                            Composite composite = behaviorTree.taskList[num2] as Composite;
                            if (composite.AbortType != AbortType.None)
                            {
                                BehaviorTree.ConditionalReevaluate reevaluate;
                                if (behaviorTree.conditionalReevaluateMap.TryGetValue(taskIndex, out reevaluate))
                                {
                                    reevaluate.compositeIndex = -1;
                                    reevaluate.taskStatus = status;
                                    task.NodeData.IsReevaluating = false;
                                }
                                else
                                {
                                    BehaviorTree.ConditionalReevaluate item = ObjectPool.Get<BehaviorTree.ConditionalReevaluate>();
                                    item.Initialize(taskIndex, status, stackIndex, (composite.AbortType == AbortType.LowerPriority) ? -1 : num2);
                                    behaviorTree.conditionalReevaluate.Add(item);
                                    behaviorTree.conditionalReevaluateMap.Add(taskIndex, item);
                                    task.NodeData.IsReevaluating = (composite.AbortType == AbortType.Self) || (composite.AbortType == AbortType.Both);
                                }
                            }
                        }
                    }
                    ParentTask task2 = behaviorTree.taskList[num] as ParentTask;
                    if (!task2.CanRunParallelChildren())
                    {
                        task2.OnChildExecuted(status);
                        status = task2.Decorate(status);
                    }
                    else
                    {
                        task2.OnChildExecuted(behaviorTree.relativeChildIndex[taskIndex], status);
                    }
                }
                if (task is ParentTask)
                {
                    ParentTask task3 = task as ParentTask;
                    if (task3.CanReevaluate())
                    {
                        for (int i = behaviorTree.parentReevaluate.Count - 1; i > -1; i--)
                        {
                            if (behaviorTree.parentReevaluate[i] == taskIndex)
                            {
                                behaviorTree.parentReevaluate.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    if (task3 is Composite)
                    {
                        Composite composite2 = task3 as Composite;
                        if (((composite2.AbortType == AbortType.Self) || (composite2.AbortType == AbortType.None)) || (behaviorTree.activeStack[stackIndex].Count == 0))
                        {
                            this.RemoveChildConditionalReevaluate(behaviorTree, taskIndex);
                        }
                        else if ((composite2.AbortType == AbortType.LowerPriority) || (composite2.AbortType == AbortType.Both))
                        {
                            for (int j = 0; j < behaviorTree.childConditionalIndex[taskIndex].Count; j++)
                            {
                                BehaviorTree.ConditionalReevaluate reevaluate3;
                                int key = behaviorTree.childConditionalIndex[taskIndex][j];
                                if (behaviorTree.conditionalReevaluateMap.TryGetValue(key, out reevaluate3))
                                {
                                    reevaluate3.compositeIndex = behaviorTree.parentCompositeIndex[taskIndex];
                                    behaviorTree.taskList[key].NodeData.IsReevaluating = true;
                                }
                            }
                            for (int k = 0; k < behaviorTree.conditionalReevaluate.Count; k++)
                            {
                                if (behaviorTree.conditionalReevaluate[k].compositeIndex == taskIndex)
                                {
                                    behaviorTree.conditionalReevaluate[k].compositeIndex = behaviorTree.parentCompositeIndex[taskIndex];
                                }
                            }
                        }
                    }
                }
                if (popChildren)
                {
                    for (int m = behaviorTree.activeStack.Count - 1; m > stackIndex; m--)
                    {
                        if ((behaviorTree.activeStack[m].Count > 0) && this.IsParentTask(behaviorTree, taskIndex, behaviorTree.activeStack[m].Peek()))
                        {
                            TaskStatus failure = TaskStatus.Failure;
                            for (int n = behaviorTree.activeStack[m].Count; n > 0; n--)
                            {
                                this.PopTask(behaviorTree, behaviorTree.activeStack[m].Peek(), m, ref failure, false, notifyOnEmptyStack);
                            }
                        }
                    }
                }
                if (behaviorTree.activeStack[stackIndex].Count == 0)
                {
                    if (stackIndex == 0)
                    {
                        if (notifyOnEmptyStack)
                        {
                            if (behaviorTree.behavior.RestartWhenComplete)
                            {
                                this.Restart(behaviorTree);
                            }
                            else
                            {
                                this.DisableBehavior(behaviorTree.behavior);
                                behaviorTree.behavior.ExecutionStatus = status;
                            }
                        }
                        status = TaskStatus.Inactive;
                    }
                    else
                    {
                        this.RemoveStack(behaviorTree, stackIndex);
                        status = TaskStatus.Running;
                    }
                }
            }
        }

        private void PushTask(BehaviorTree behaviorTree, int taskIndex, int stackIndex)
        {
            if (this.IsBehaviorEnabled(behaviorTree.behavior) && (stackIndex < behaviorTree.activeStack.Count))
            {
                Stack<int> stack = behaviorTree.activeStack[stackIndex];
                if ((stack.Count == 0) || (stack.Peek() != taskIndex))
                {
                    stack.Push(taskIndex);
                    behaviorTree.nonInstantTaskStatus[stackIndex] = TaskStatus.Running;
                    behaviorTree.executionCount++;
                    Task task = behaviorTree.taskList[taskIndex];
                    task.NodeData.PushTime = Time.realtimeSinceStartup;
                    task.NodeData.ExecutionStatus = TaskStatus.Running;
                    if (task.NodeData.IsBreakpoint)
                    {
                        this.atBreakpoint = true;
                        if (this.onTaskBreakpoint != null)
                        {
                            this.onTaskBreakpoint();
                        }
                    }
                    if (behaviorTree.behavior.LogTaskChanges)
                    {
                        object[] args = new object[] { this.RoundedTime(), behaviorTree.behavior.ToString(), task.FriendlyName, task.GetType(), taskIndex, stackIndex };
                        MonoBehaviour.print(string.Format("{0}: {1}: Push task {2} ({3}, index {4}) at stack index {5}", args));
                    }
                    task.OnStart();
                    if (task is ParentTask)
                    {
                        ParentTask task2 = task as ParentTask;
                        if (task2.CanReevaluate())
                        {
                            behaviorTree.parentReevaluate.Add(taskIndex);
                        }
                    }
                }
            }
        }

        private void ReevaluateConditionalTasks(BehaviorTree behaviorTree)
        {
            for (int i = behaviorTree.conditionalReevaluate.Count - 1; i > -1; i--)
            {
                if (behaviorTree.conditionalReevaluate[i].compositeIndex != -1)
                {
                    int index = behaviorTree.conditionalReevaluate[i].index;
                    TaskStatus status = behaviorTree.taskList[index].OnUpdate();
                    if (status != behaviorTree.conditionalReevaluate[i].taskStatus)
                    {
                        if (behaviorTree.behavior.LogTaskChanges)
                        {
                            int num3 = behaviorTree.parentCompositeIndex[index];
                            object[] args = new object[] { this.RoundedTime(), behaviorTree.behavior.ToString(), behaviorTree.taskList[num3].FriendlyName, behaviorTree.taskList[num3].GetType(), num3, behaviorTree.taskList[index].FriendlyName, behaviorTree.taskList[index].GetType(), index, status };
                            MonoBehaviour.print(string.Format("{0}: {1}: Conditional abort with task {2} ({3}, index {4}) because of conditional task {5} ({6}, index {7}) with status {8}", args));
                        }
                        int compositeIndex = behaviorTree.conditionalReevaluate[i].compositeIndex;
                        for (int j = behaviorTree.activeStack.Count - 1; j > -1; j--)
                        {
                            if (behaviorTree.activeStack[j].Count > 0)
                            {
                                int num6 = behaviorTree.activeStack[j].Peek();
                                int num7 = this.FindLCA(behaviorTree, index, num6);
                                if (this.IsChild(behaviorTree, num7, compositeIndex))
                                {
                                    int count = behaviorTree.activeStack.Count;
                                    while (((num6 != -1) && (num6 != num7)) && (behaviorTree.activeStack.Count == count))
                                    {
                                        TaskStatus failure = TaskStatus.Failure;
                                        this.PopTask(behaviorTree, num6, j, ref failure, false);
                                        num6 = behaviorTree.parentIndex[num6];
                                    }
                                }
                            }
                        }
                        for (int k = behaviorTree.conditionalReevaluate.Count - 1; k > (i - 1); k--)
                        {
                            BehaviorTree.ConditionalReevaluate reevaluate = behaviorTree.conditionalReevaluate[k];
                            if (this.FindLCA(behaviorTree, compositeIndex, reevaluate.index) == compositeIndex)
                            {
                                behaviorTree.taskList[behaviorTree.conditionalReevaluate[k].index].NodeData.IsReevaluating = false;
                                ObjectPool.Return<BehaviorTree.ConditionalReevaluate>(behaviorTree.conditionalReevaluate[k]);
                                behaviorTree.conditionalReevaluateMap.Remove(behaviorTree.conditionalReevaluate[k].index);
                                behaviorTree.conditionalReevaluate.RemoveAt(k);
                            }
                        }
                        Composite composite = behaviorTree.taskList[behaviorTree.parentCompositeIndex[index]] as Composite;
                        for (int m = i - 1; m > -1; m--)
                        {
                            BehaviorTree.ConditionalReevaluate reevaluate2 = behaviorTree.conditionalReevaluate[m];
                            if ((composite.AbortType == AbortType.LowerPriority) && (behaviorTree.parentCompositeIndex[reevaluate2.index] == behaviorTree.parentCompositeIndex[index]))
                            {
                                behaviorTree.taskList[behaviorTree.conditionalReevaluate[m].index].NodeData.IsReevaluating = false;
                                ObjectPool.Return<BehaviorTree.ConditionalReevaluate>(behaviorTree.conditionalReevaluate[m]);
                                behaviorTree.conditionalReevaluateMap.Remove(behaviorTree.conditionalReevaluate[m].index);
                                behaviorTree.conditionalReevaluate.RemoveAt(m);
                                i--;
                            }
                            else if (behaviorTree.parentCompositeIndex[reevaluate2.index] == behaviorTree.parentCompositeIndex[index])
                            {
                                for (int num11 = 0; num11 < behaviorTree.childrenIndex[compositeIndex].Count; num11++)
                                {
                                    if (!this.IsParentTask(behaviorTree, behaviorTree.childrenIndex[compositeIndex][num11], reevaluate2.index))
                                    {
                                        continue;
                                    }
                                    int num12 = behaviorTree.childrenIndex[compositeIndex][num11];
                                    while (!(behaviorTree.taskList[num12] is Composite))
                                    {
                                        if (behaviorTree.childrenIndex[num12] == null)
                                        {
                                            break;
                                        }
                                        num12 = behaviorTree.childrenIndex[num12][0];
                                    }
                                    if (behaviorTree.taskList[num12] is Composite)
                                    {
                                        reevaluate2.compositeIndex = num12;
                                    }
                                    break;
                                }
                            }
                        }
                        this.conditionalParentIndexes.Clear();
                        for (int n = behaviorTree.parentIndex[index]; n != compositeIndex; n = behaviorTree.parentIndex[n])
                        {
                            this.conditionalParentIndexes.Add(n);
                        }
                        if (this.conditionalParentIndexes.Count == 0)
                        {
                            this.conditionalParentIndexes.Add(behaviorTree.parentIndex[index]);
                        }
                        ParentTask task = behaviorTree.taskList[compositeIndex] as ParentTask;
                        task.OnConditionalAbort(behaviorTree.relativeChildIndex[this.conditionalParentIndexes[this.conditionalParentIndexes.Count - 1]]);
                        for (int num14 = this.conditionalParentIndexes.Count - 1; num14 > -1; num14--)
                        {
                            task = behaviorTree.taskList[this.conditionalParentIndexes[num14]] as ParentTask;
                            if (num14 == 0)
                            {
                                task.OnConditionalAbort(behaviorTree.relativeChildIndex[index]);
                            }
                            else
                            {
                                task.OnConditionalAbort(behaviorTree.relativeChildIndex[this.conditionalParentIndexes[num14 - 1]]);
                            }
                        }
                        behaviorTree.taskList[index].NodeData.InterruptTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private void ReevaluateParentTasks(BehaviorTree behaviorTree)
        {
            for (int i = behaviorTree.parentReevaluate.Count - 1; i > -1; i--)
            {
                int taskIndex = behaviorTree.parentReevaluate[i];
                if (behaviorTree.taskList[taskIndex] is Decorator)
                {
                    if (behaviorTree.taskList[taskIndex].OnUpdate() == TaskStatus.Failure)
                    {
                        this.Interrupt(behaviorTree.behavior, behaviorTree.taskList[taskIndex]);
                    }
                }
                else if (behaviorTree.taskList[taskIndex] is Composite)
                {
                    ParentTask task = behaviorTree.taskList[taskIndex] as ParentTask;
                    if (task.OnReevaluationStarted())
                    {
                        int stackIndex = 0;
                        TaskStatus status = this.RunParentTask(behaviorTree, taskIndex, ref stackIndex, TaskStatus.Inactive);
                        task.OnReevaluationEnded(status);
                    }
                }
            }
        }

        public void RemoveActiveThirdPartyTask(Task task)
        {
            object obj2;
            this.thirdPartyTaskCompare.Task = task;
            if (this.taskObjectMap.TryGetValue(this.thirdPartyTaskCompare, out obj2))
            {
                ObjectPool.Return<object>(obj2);
                this.taskObjectMap.Remove(this.thirdPartyTaskCompare);
                this.objectTaskMap.Remove(obj2);
            }
        }

        private void RemoveChildConditionalReevaluate(BehaviorTree behaviorTree, int compositeIndex)
        {
            for (int i = behaviorTree.conditionalReevaluate.Count - 1; i > -1; i--)
            {
                if (behaviorTree.conditionalReevaluate[i].compositeIndex == compositeIndex)
                {
                    ObjectPool.Return<BehaviorTree.ConditionalReevaluate>(behaviorTree.conditionalReevaluate[i]);
                    int index = behaviorTree.conditionalReevaluate[i].index;
                    behaviorTree.conditionalReevaluateMap.Remove(index);
                    behaviorTree.conditionalReevaluate.RemoveAt(i);
                    behaviorTree.taskList[index].NodeData.IsReevaluating = false;
                }
            }
        }

        private void RemoveStack(BehaviorTree behaviorTree, int stackIndex)
        {
            Stack<int> stack = behaviorTree.activeStack[stackIndex];
            stack.Clear();
            ObjectPool.Return<Stack<int>>(stack);
            behaviorTree.activeStack.RemoveAt(stackIndex);
            behaviorTree.interruptionIndex.RemoveAt(stackIndex);
            behaviorTree.nonInstantTaskStatus.RemoveAt(stackIndex);
        }

        private void Restart(BehaviorTree behaviorTree)
        {
            if (behaviorTree.behavior.LogTaskChanges)
            {
                UnityEngine.Debug.Log(string.Format("{0}: Restarting {1}", this.RoundedTime(), behaviorTree.behavior.ToString()));
            }
            this.RemoveChildConditionalReevaluate(behaviorTree, -1);
            if (behaviorTree.behavior.ResetValuesOnRestart)
            {
                behaviorTree.behavior.SaveResetValues();
            }
            for (int i = 0; i < behaviorTree.taskList.Count; i++)
            {
                behaviorTree.taskList[i].OnBehaviorRestart();
            }
            behaviorTree.behavior.OnBehaviorRestarted();
            this.PushTask(behaviorTree, 0, 0);
        }

        public void RestartBehavior(Behavior behavior)
        {
            if (this.IsBehaviorEnabled(behavior))
            {
                BehaviorTree behaviorTree = this.behaviorTreeMap[behavior];
                TaskStatus success = TaskStatus.Success;
                for (int i = behaviorTree.activeStack.Count - 1; i > -1; i--)
                {
                    while (behaviorTree.activeStack[i].Count > 0)
                    {
                        int count = behaviorTree.activeStack[i].Count;
                        this.PopTask(behaviorTree, behaviorTree.activeStack[i].Peek(), i, ref success, true, false);
                        if (count == 1)
                        {
                            break;
                        }
                    }
                }
                this.Restart(behaviorTree);
            }
        }

        private decimal RoundedTime()
        {
            return Math.Round((decimal) Time.time, 5, MidpointRounding.AwayFromZero);
        }

        private TaskStatus RunParentTask(BehaviorTree behaviorTree, int taskIndex, ref int stackIndex, TaskStatus status)
        {
            ParentTask task = behaviorTree.taskList[taskIndex] as ParentTask;
            if (!task.CanRunParallelChildren() || (task.OverrideStatus(TaskStatus.Running) != TaskStatus.Running))
            {
                TaskStatus inactive = TaskStatus.Inactive;
                int num = stackIndex;
                int num2 = -1;
                while ((task.CanExecute() && ((inactive != TaskStatus.Running) || task.CanRunParallelChildren())) && this.IsBehaviorEnabled(behaviorTree.behavior))
                {
                    List<int> list = behaviorTree.childrenIndex[taskIndex];
                    int childIndex = task.CurrentChildIndex();
                    if (((this.executionsPerTick == ExecutionsPerTickType.NoDuplicates) && (childIndex == num2)) || ((this.executionsPerTick == ExecutionsPerTickType.Count) && (behaviorTree.executionCount >= this.maxTaskExecutionsPerTick)))
                    {
                        if (this.executionsPerTick == ExecutionsPerTickType.Count)
                        {
                            UnityEngine.Debug.LogWarning(string.Format("{0}: {1}: More than the specified number of task executions per tick ({2}) have executed, returning early.", this.RoundedTime(), behaviorTree.behavior.ToString(), this.maxTaskExecutionsPerTick));
                        }
                        status = TaskStatus.Running;
                        break;
                    }
                    num2 = childIndex;
                    if (task.CanRunParallelChildren())
                    {
                        behaviorTree.activeStack.Add(ObjectPool.Get<Stack<int>>());
                        behaviorTree.interruptionIndex.Add(-1);
                        behaviorTree.nonInstantTaskStatus.Add(TaskStatus.Inactive);
                        stackIndex = behaviorTree.activeStack.Count - 1;
                        task.OnChildStarted(childIndex);
                    }
                    else
                    {
                        task.OnChildStarted();
                    }
                    status = inactive = this.RunTask(behaviorTree, list[childIndex], stackIndex, status);
                }
                stackIndex = num;
            }
            return status;
        }

        private TaskStatus RunTask(BehaviorTree behaviorTree, int taskIndex, int stackIndex, TaskStatus previousStatus)
        {
            Task task = behaviorTree.taskList[taskIndex];
            if (task == null)
            {
                return previousStatus;
            }
            if (task.NodeData.Disabled)
            {
                if (behaviorTree.behavior.LogTaskChanges)
                {
                    object[] args = new object[] { this.RoundedTime(), behaviorTree.behavior.ToString(), behaviorTree.taskList[taskIndex].FriendlyName, behaviorTree.taskList[taskIndex].GetType(), taskIndex, stackIndex };
                    MonoBehaviour.print(string.Format("{0}: {1}: Skip task {2} ({3}, index {4}) at stack index {5} (task disabled)", args));
                }
                if (behaviorTree.parentIndex[taskIndex] != -1)
                {
                    ParentTask task2 = behaviorTree.taskList[behaviorTree.parentIndex[taskIndex]] as ParentTask;
                    if (!task2.CanRunParallelChildren())
                    {
                        task2.OnChildExecuted(TaskStatus.Inactive);
                        return previousStatus;
                    }
                    task2.OnChildExecuted(behaviorTree.relativeChildIndex[taskIndex], TaskStatus.Inactive);
                }
                return previousStatus;
            }
            TaskStatus status = previousStatus;
            if (!task.IsInstant && ((((TaskStatus) behaviorTree.nonInstantTaskStatus[stackIndex]) == TaskStatus.Failure) || (((TaskStatus) behaviorTree.nonInstantTaskStatus[stackIndex]) == TaskStatus.Success)))
            {
                status = behaviorTree.nonInstantTaskStatus[stackIndex];
                this.PopTask(behaviorTree, taskIndex, stackIndex, ref status, true);
                return status;
            }
            this.PushTask(behaviorTree, taskIndex, stackIndex);
            if (this.atBreakpoint)
            {
                return TaskStatus.Running;
            }
            if (task is ParentTask)
            {
                ParentTask task3 = task as ParentTask;
                status = this.RunParentTask(behaviorTree, taskIndex, ref stackIndex, status);
                status = task3.OverrideStatus(status);
            }
            else
            {
                status = task.OnUpdate();
            }
            if (status != TaskStatus.Running)
            {
                if (task.IsInstant)
                {
                    this.PopTask(behaviorTree, taskIndex, stackIndex, ref status, true);
                    return status;
                }
                behaviorTree.nonInstantTaskStatus[stackIndex] = status;
            }
            return status;
        }

        public void StopThirdPartyTask(BehaviorTree behaviorTree, int taskIndex)
        {
            object obj2;
            this.thirdPartyTaskCompare.Task = behaviorTree.taskList[taskIndex];
            if (this.taskObjectMap.TryGetValue(this.thirdPartyTaskCompare, out obj2))
            {
                ThirdPartyObjectType thirdPartyObjectType = this.objectTaskMap[obj2].ThirdPartyObjectType;
                if (invokeParameters == null)
                {
                    invokeParameters = new object[1];
                }
                invokeParameters[0] = behaviorTree.taskList[taskIndex];
                switch (thirdPartyObjectType)
                {
                    case ThirdPartyObjectType.PlayMaker:
                        PlayMakerStopMethod.Invoke(null, invokeParameters);
                        break;

                    case ThirdPartyObjectType.uScript:
                        UScriptStopMethod.Invoke(null, invokeParameters);
                        break;

                    case ThirdPartyObjectType.DialogueSystem:
                        DialogueSystemStopMethod.Invoke(null, invokeParameters);
                        break;

                    case ThirdPartyObjectType.uSequencer:
                        USequencerStopMethod.Invoke(null, invokeParameters);
                        break;

                    case ThirdPartyObjectType.ICode:
                        ICodeStopMethod.Invoke(null, invokeParameters);
                        break;
                }
                this.RemoveActiveThirdPartyTask(behaviorTree.taskList[taskIndex]);
            }
        }

        public Task TaskForObject(object objectKey)
        {
            ThirdPartyTask task;
            if (!this.objectTaskMap.TryGetValue(objectKey, out task))
            {
                return null;
            }
            return task.Task;
        }

        public void Tick()
        {
            for (int i = 0; i < this.behaviorTrees.Count; i++)
            {
                this.Tick(this.behaviorTrees[i]);
            }
        }

        public void Tick(Behavior behavior)
        {
            if ((behavior != null) && this.IsBehaviorEnabled(behavior))
            {
                this.Tick(this.behaviorTreeMap[behavior]);
            }
        }

        private void Tick(BehaviorTree behaviorTree)
        {
            behaviorTree.executionCount = 0;
            this.ReevaluateParentTasks(behaviorTree);
            this.ReevaluateConditionalTasks(behaviorTree);
            for (int i = behaviorTree.activeStack.Count - 1; i > -1; i--)
            {
                int num2;
                TaskStatus inactive = TaskStatus.Inactive;
                if ((i < behaviorTree.interruptionIndex.Count) && ((num2 = behaviorTree.interruptionIndex[i]) != -1))
                {
                    behaviorTree.interruptionIndex[i] = -1;
                    while (behaviorTree.activeStack[i].Peek() != num2)
                    {
                        int count = behaviorTree.activeStack[i].Count;
                        this.PopTask(behaviorTree, behaviorTree.activeStack[i].Peek(), i, ref inactive, true);
                        if (count == 1)
                        {
                            break;
                        }
                    }
                    if (((i < behaviorTree.activeStack.Count) && (behaviorTree.activeStack[i].Count > 0)) && (behaviorTree.taskList[num2] == behaviorTree.taskList[behaviorTree.activeStack[i].Peek()]))
                    {
                        if (behaviorTree.taskList[num2] is ParentTask)
                        {
                            inactive = (behaviorTree.taskList[num2] as ParentTask).OverrideStatus();
                        }
                        this.PopTask(behaviorTree, num2, i, ref inactive, true);
                    }
                }
                int num4 = -1;
                while (((inactive != TaskStatus.Running) && (i < behaviorTree.activeStack.Count)) && (behaviorTree.activeStack[i].Count > 0))
                {
                    int taskIndex = behaviorTree.activeStack[i].Peek();
                    if ((((i < behaviorTree.activeStack.Count) && (behaviorTree.activeStack[i].Count > 0)) && (num4 == behaviorTree.activeStack[i].Peek())) || !this.IsBehaviorEnabled(behaviorTree.behavior))
                    {
                        break;
                    }
                    num4 = taskIndex;
                    inactive = this.RunTask(behaviorTree, taskIndex, i, inactive);
                }
            }
        }

        public void Update()
        {
            this.Tick();
        }

        private void UpdateIntervalChanged()
        {
            base.StopCoroutine("CoroutineUpdate");
            if (this.updateInterval == UpdateIntervalType.EveryFrame)
            {
                base.enabled = true;
            }
            else if (this.updateInterval == UpdateIntervalType.SpecifySeconds)
            {
                if (Application.isPlaying)
                {
                    this.updateWait = new WaitForSeconds(this.updateIntervalSeconds);
                    base.StartCoroutine("CoroutineUpdate");
                }
                base.enabled = false;
            }
            else
            {
                base.enabled = false;
            }
        }

        public bool AtBreakpoint
        {
            get
            {
                return this.atBreakpoint;
            }
            set
            {
                this.atBreakpoint = value;
            }
        }

        public List<BehaviorTree> BehaviorTrees
        {
            get
            {
                return this.behaviorTrees;
            }
        }

        private static MethodInfo DialogueSystemStopMethod
        {
            get
            {
                if (dialogueSystemStopMethod == null)
                {
                    dialogueSystemStopMethod = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorManager_DialogueSystem").GetMethod("StopDialogueSystem");
                }
                return dialogueSystemStopMethod;
            }
        }

        public bool Dirty
        {
            get
            {
                return this.dirty;
            }
            set
            {
                this.dirty = value;
            }
        }

        public ExecutionsPerTickType ExecutionsPerTick
        {
            get
            {
                return this.executionsPerTick;
            }
            set
            {
                this.executionsPerTick = value;
            }
        }

        private static MethodInfo ICodeStopMethod
        {
            get
            {
                if (iCodeStopMethod == null)
                {
                    iCodeStopMethod = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorManager_ICode").GetMethod("StopICode");
                }
                return iCodeStopMethod;
            }
        }

        public int MaxTaskExecutionsPerTick
        {
            get
            {
                return this.maxTaskExecutionsPerTick;
            }
            set
            {
                this.maxTaskExecutionsPerTick = value;
            }
        }

        public BehaviorManagerHandler OnEnableBehavior
        {
            set
            {
                this.onEnableBehavior = value;
            }
        }

        public BehaviorManagerHandler OnTaskBreakpoint
        {
            get
            {
                return this.onTaskBreakpoint;
            }
            set
            {
                this.onTaskBreakpoint = (BehaviorManagerHandler) Delegate.Combine(this.onTaskBreakpoint, value);
            }
        }

        private static MethodInfo PlayMakerStopMethod
        {
            get
            {
                if (playMakerStopMethod == null)
                {
                    playMakerStopMethod = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorManager_PlayMaker").GetMethod("StopPlayMaker");
                }
                return playMakerStopMethod;
            }
        }

        public UpdateIntervalType UpdateInterval
        {
            get
            {
                return this.updateInterval;
            }
            set
            {
                this.updateInterval = value;
                this.UpdateIntervalChanged();
            }
        }

        public float UpdateIntervalSeconds
        {
            get
            {
                return this.updateIntervalSeconds;
            }
            set
            {
                this.updateIntervalSeconds = value;
                this.UpdateIntervalChanged();
            }
        }

        private static MethodInfo UScriptStopMethod
        {
            get
            {
                if (uScriptStopMethod == null)
                {
                    uScriptStopMethod = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorManager_uScript").GetMethod("StopuScript");
                }
                return uScriptStopMethod;
            }
        }

        private static MethodInfo USequencerStopMethod
        {
            get
            {
                if (uSequencerStopMethod == null)
                {
                    uSequencerStopMethod = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.BehaviorManager_uSequencer").GetMethod("StopuSequencer");
                }
                return uSequencerStopMethod;
            }
        }

        [CompilerGenerated]
        private sealed class <CoroutineUpdate>c__Iterator0 : IEnumerator, IDisposable, IEnumerator<object>
        {
            internal object $current;
            internal int $PC;
            internal BehaviorManager <>f__this;

            [DebuggerHidden]
            public void Dispose()
            {
                this.$PC = -1;
            }

            public bool MoveNext()
            {
                uint num = (uint) this.$PC;
                this.$PC = -1;
                switch (num)
                {
                    case 0:
                    case 1:
                        this.<>f__this.Tick();
                        this.$current = this.<>f__this.updateWait;
                        this.$PC = 1;
                        return true;

                    default:
                        break;
                        this.$PC = -1;
                        break;
                }
                return false;
            }

            [DebuggerHidden]
            public void Reset()
            {
                throw new NotSupportedException();
            }

            object IEnumerator<object>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.$current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.$current;
                }
            }
        }

        public delegate void BehaviorManagerHandler();

        public class BehaviorTree
        {
            public List<Stack<int>> activeStack = new List<Stack<int>>();
            public Behavior behavior;
            public List<List<int>> childConditionalIndex = new List<List<int>>();
            public List<List<int>> childrenIndex = new List<List<int>>();
            public List<ConditionalReevaluate> conditionalReevaluate = new List<ConditionalReevaluate>();
            public Dictionary<int, ConditionalReevaluate> conditionalReevaluateMap = new Dictionary<int, ConditionalReevaluate>();
            public int executionCount;
            public List<int> interruptionIndex = new List<int>();
            public List<TaskStatus> nonInstantTaskStatus = new List<TaskStatus>();
            public List<int> parentCompositeIndex = new List<int>();
            public List<int> parentIndex = new List<int>();
            public List<int> parentReevaluate = new List<int>();
            public List<int> relativeChildIndex = new List<int>();
            public List<Task> taskList = new List<Task>();

            public void Initialize(Behavior b)
            {
                this.behavior = b;
                for (int i = this.childrenIndex.Count - 1; i > -1; i--)
                {
                    ObjectPool.Return<List<int>>(this.childrenIndex[i]);
                }
                for (int j = this.activeStack.Count - 1; j > -1; j--)
                {
                    ObjectPool.Return<Stack<int>>(this.activeStack[j]);
                }
                for (int k = this.childConditionalIndex.Count - 1; k > -1; k--)
                {
                    ObjectPool.Return<List<int>>(this.childConditionalIndex[k]);
                }
                this.taskList.Clear();
                this.parentIndex.Clear();
                this.childrenIndex.Clear();
                this.relativeChildIndex.Clear();
                this.activeStack.Clear();
                this.nonInstantTaskStatus.Clear();
                this.interruptionIndex.Clear();
                this.conditionalReevaluate.Clear();
                this.conditionalReevaluateMap.Clear();
                this.parentReevaluate.Clear();
                this.parentCompositeIndex.Clear();
                this.childConditionalIndex.Clear();
            }

            public class ConditionalReevaluate
            {
                public int compositeIndex = -1;
                public int index;
                public int stackIndex = -1;
                public TaskStatus taskStatus;

                public void Initialize(int i, TaskStatus status, int stack, int composite)
                {
                    this.index = i;
                    this.taskStatus = status;
                    this.stackIndex = stack;
                    this.compositeIndex = composite;
                }
            }
        }

        public enum ExecutionsPerTickType
        {
            NoDuplicates,
            Count
        }

        public class TaskAddData
        {
            public int compositeParentIndex = -1;
            public int depth;
            public int errorTask = -1;
            public string errorTaskName = string.Empty;
            public bool fromExternalTask;
            public Vector2 offset;
            public HashSet<object> overiddenFields = new HashSet<object>();
            public Dictionary<string, OverrideFieldValue> overrideFields;
            public int parentIndex = -1;
            public ParentTask parentTask;

            public void Initialize()
            {
                if (this.overrideFields != null)
                {
                    foreach (KeyValuePair<string, OverrideFieldValue> pair in this.overrideFields)
                    {
                        ObjectPool.Return<KeyValuePair<string, OverrideFieldValue>>(pair);
                    }
                }
                ObjectPool.Return<Dictionary<string, OverrideFieldValue>>(this.overrideFields);
                this.fromExternalTask = false;
                this.parentTask = null;
                this.parentIndex = -1;
                this.depth = 0;
                this.compositeParentIndex = -1;
                this.overrideFields = null;
            }

            public class OverrideFieldValue
            {
                private int depth;
                private object value;

                public void Initialize(object v, int d)
                {
                    this.value = v;
                    this.depth = d;
                }

                public int Depth
                {
                    get
                    {
                        return this.depth;
                    }
                }

                public object Value
                {
                    get
                    {
                        return this.value;
                    }
                }
            }
        }

        public enum ThirdPartyObjectType
        {
            PlayMaker,
            uScript,
            DialogueSystem,
            uSequencer,
            ICode
        }

        public class ThirdPartyTask
        {
            private BehaviorDesigner.Runtime.Tasks.Task task;
            private BehaviorDesigner.Runtime.BehaviorManager.ThirdPartyObjectType thirdPartyObjectType;

            public void Initialize(BehaviorDesigner.Runtime.Tasks.Task t, BehaviorDesigner.Runtime.BehaviorManager.ThirdPartyObjectType objectType)
            {
                this.task = t;
                this.thirdPartyObjectType = objectType;
            }

            public BehaviorDesigner.Runtime.Tasks.Task Task
            {
                get
                {
                    return this.task;
                }
                set
                {
                    this.task = value;
                }
            }

            public BehaviorDesigner.Runtime.BehaviorManager.ThirdPartyObjectType ThirdPartyObjectType
            {
                get
                {
                    return this.thirdPartyObjectType;
                }
            }
        }

        public class ThirdPartyTaskComparer : IEqualityComparer<BehaviorManager.ThirdPartyTask>
        {
            public bool Equals(BehaviorManager.ThirdPartyTask a, BehaviorManager.ThirdPartyTask b)
            {
                if (object.ReferenceEquals(a, null))
                {
                    return false;
                }
                if (object.ReferenceEquals(b, null))
                {
                    return false;
                }
                return a.Task.Equals(b.Task);
            }

            public int GetHashCode(BehaviorManager.ThirdPartyTask obj)
            {
                return ((obj == null) ? 0 : obj.Task.GetHashCode());
            }
        }
    }
}

