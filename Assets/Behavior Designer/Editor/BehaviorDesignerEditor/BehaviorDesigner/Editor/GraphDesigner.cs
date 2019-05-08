namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class GraphDesigner : ScriptableObject
    {
        private NodeConnection mActiveNodeConnection;
        private List<NodeDesigner> mDetachedNodes = new List<NodeDesigner>();
        private NodeDesigner mEntryNode;
        private NodeDesigner mHoverNode;
        [SerializeField]
        private int mNextTaskID;
        private List<int> mNodeSelectedID = new List<int>();
        [SerializeField]
        private int[] mPrevNodeSelectedID;
        private NodeDesigner mRootNode;
        [SerializeField]
        private List<NodeConnection> mSelectedNodeConnections = new List<NodeConnection>();
        [SerializeField]
        private List<NodeDesigner> mSelectedNodes = new List<NodeDesigner>();

        private NodeDesigner AddNode(BehaviorSource behaviorSource, Task task, Vector2 position)
        {
            if (this.mEntryNode == null)
            {
                Task task2 = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.Tasks.EntryTask")) as Task;
                this.mEntryNode = ScriptableObject.CreateInstance<NodeDesigner>();
                this.mEntryNode.LoadNode(task2, behaviorSource, new Vector2(position.x, position.y - 120f), ref this.mNextTaskID);
                this.mEntryNode.MakeEntryDisplay();
            }
            NodeDesigner nodeDesigner = ScriptableObject.CreateInstance<NodeDesigner>();
            nodeDesigner.LoadNode(task, behaviorSource, position, ref this.mNextTaskID);
            TaskNameAttribute[] attributeArray = null;
            if ((attributeArray = task.GetType().GetCustomAttributes(typeof(TaskNameAttribute), false) as TaskNameAttribute[]).Length > 0)
            {
                task.FriendlyName = attributeArray[0].Name;
            }
            if (this.mEntryNode.OutgoingNodeConnections.Count == 0)
            {
                this.mActiveNodeConnection = ScriptableObject.CreateInstance<NodeConnection>();
                this.mActiveNodeConnection.LoadConnection(this.mEntryNode, NodeConnectionType.Outgoing);
                this.ConnectNodes(behaviorSource, nodeDesigner);
                return nodeDesigner;
            }
            this.mDetachedNodes.Add(nodeDesigner);
            return nodeDesigner;
        }

        public NodeDesigner AddNode(BehaviorSource behaviorSource, System.Type type, Vector2 position)
        {
            Task task = Activator.CreateInstance(type, true) as Task;
            if (task == null)
            {
                EditorUtility.DisplayDialog("Unable to Add Task", string.Format("Unable to create task of type {0}. Is the class name the same as the file name?", type), "OK");
                return null;
            }
            return this.AddNode(behaviorSource, task, position);
        }

        private void CheckForLastConnectionRemoval(NodeDesigner nodeDesigner)
        {
            if (nodeDesigner.IsEntryDisplay)
            {
                if (nodeDesigner.OutgoingNodeConnections.Count == 1)
                {
                    this.RemoveConnection(nodeDesigner.OutgoingNodeConnections[0]);
                }
            }
            else
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if ((task.Children != null) && ((task.Children.Count + 1) > task.MaxChildren()))
                {
                    NodeConnection nodeConnection = null;
                    for (int i = 0; i < nodeDesigner.OutgoingNodeConnections.Count; i++)
                    {
                        if (nodeDesigner.OutgoingNodeConnections[i].DestinationNodeDesigner.Equals(task.Children[task.Children.Count - 1].NodeData.NodeDesigner as NodeDesigner))
                        {
                            nodeConnection = nodeDesigner.OutgoingNodeConnections[i];
                            break;
                        }
                    }
                    if (nodeConnection != null)
                    {
                        this.RemoveConnection(nodeConnection);
                    }
                }
            }
        }

        private void Clear(NodeDesigner nodeDesigner)
        {
            if (nodeDesigner != null)
            {
                if (nodeDesigner.IsParent)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if ((task != null) && (task.Children != null))
                    {
                        for (int i = task.Children.Count - 1; i > -1; i--)
                        {
                            if (task.Children[i] != null)
                            {
                                this.Clear(task.Children[i].NodeData.NodeDesigner as NodeDesigner);
                            }
                        }
                    }
                }
                nodeDesigner.DestroyConnections();
                UnityEngine.Object.DestroyImmediate(nodeDesigner, true);
            }
        }

        public void Clear(bool saveSelectedNodes)
        {
            if (saveSelectedNodes)
            {
                this.mPrevNodeSelectedID = this.mNodeSelectedID.ToArray();
            }
            else
            {
                this.mPrevNodeSelectedID = null;
            }
            this.mNodeSelectedID.Clear();
            this.mSelectedNodes.Clear();
            this.mSelectedNodeConnections.Clear();
            this.DestroyNodeDesigners();
        }

        public void ClearConnectionSelection()
        {
            for (int i = 0; i < this.mSelectedNodeConnections.Count; i++)
            {
                this.mSelectedNodeConnections[i].deselect();
            }
            this.mSelectedNodeConnections.Clear();
        }

        public void ClearHover()
        {
            if (this.HoverNode != null)
            {
                this.HoverNode.ShowHoverBar = false;
                this.HoverNode = null;
            }
        }

        public void ClearNodeSelection()
        {
            if (this.mSelectedNodes.Count == 1)
            {
                this.IndicateReferencedTasks(this.mSelectedNodes[0].Task, false);
            }
            for (int i = 0; i < this.mSelectedNodes.Count; i++)
            {
                this.mSelectedNodes[i].Deselect();
            }
            this.mSelectedNodes.Clear();
            this.mNodeSelectedID.Clear();
        }

        public void ConnectNodes(BehaviorSource behaviorSource, NodeDesigner nodeDesigner)
        {
            NodeConnection mActiveNodeConnection = this.mActiveNodeConnection;
            this.mActiveNodeConnection = null;
            if ((mActiveNodeConnection != null) && !mActiveNodeConnection.OriginatingNodeDesigner.Equals(nodeDesigner))
            {
                NodeDesigner originatingNodeDesigner = mActiveNodeConnection.OriginatingNodeDesigner;
                if (mActiveNodeConnection.NodeConnectionType == NodeConnectionType.Outgoing)
                {
                    this.RemoveParentConnection(nodeDesigner);
                    this.CheckForLastConnectionRemoval(originatingNodeDesigner);
                    originatingNodeDesigner.AddChildNode(nodeDesigner, mActiveNodeConnection, true, false);
                }
                else
                {
                    this.RemoveParentConnection(originatingNodeDesigner);
                    this.CheckForLastConnectionRemoval(nodeDesigner);
                    nodeDesigner.AddChildNode(originatingNodeDesigner, mActiveNodeConnection, true, false);
                }
                if (mActiveNodeConnection.OriginatingNodeDesigner.IsEntryDisplay)
                {
                    this.mRootNode = mActiveNodeConnection.DestinationNodeDesigner;
                }
                this.mDetachedNodes.Remove(mActiveNodeConnection.DestinationNodeDesigner);
            }
        }

        public List<TaskSerializer> Copy(Vector2 graphOffset, float graphZoom)
        {
            List<TaskSerializer> list = new List<TaskSerializer>();
            for (int i = 0; i < this.mSelectedNodes.Count; i++)
            {
                TaskSerializer item = TaskCopier.CopySerialized(this.mSelectedNodes[i].Task);
                if (item != null)
                {
                    if (this.mSelectedNodes[i].IsParent)
                    {
                        ParentTask task = this.mSelectedNodes[i].Task as ParentTask;
                        if (task.Children != null)
                        {
                            List<int> list2 = new List<int>();
                            int index = -1;
                            for (int j = 0; j < task.Children.Count; j++)
                            {
                                index = this.mSelectedNodes.IndexOf(task.Children[j].NodeData.NodeDesigner as NodeDesigner);
                                if (index != -1)
                                {
                                    list2.Add(index);
                                }
                            }
                            item.childrenIndex = list2;
                        }
                    }
                    item.offset = (Vector2) ((item.offset + graphOffset) * graphZoom);
                    list.Add(item);
                }
            }
            return ((list.Count <= 0) ? null : list);
        }

        private bool CycleExists(NodeDesigner nodeDesigner, ref HashSet<NodeDesigner> set)
        {
            if (set.Contains(nodeDesigner))
            {
                return true;
            }
            set.Add(nodeDesigner);
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children != null)
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        if (this.CycleExists(task.Children[i].NodeData.NodeDesigner as NodeDesigner, ref set))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool Delete(BehaviorSource behaviorSource)
        {
            bool flag = false;
            if (this.mSelectedNodeConnections != null)
            {
                for (int i = 0; i < this.mSelectedNodeConnections.Count; i++)
                {
                    this.RemoveConnection(this.mSelectedNodeConnections[i]);
                }
                this.mSelectedNodeConnections.Clear();
                flag = true;
            }
            if (this.mSelectedNodes != null)
            {
                for (int j = 0; j < this.mSelectedNodes.Count; j++)
                {
                    this.RemoveNode(this.mSelectedNodes[j]);
                }
                this.mSelectedNodes.Clear();
                flag = true;
            }
            if (flag)
            {
                BehaviorUndo.RegisterUndo("Delete", behaviorSource.Owner.GetObject());
                TaskReferences.CheckReferences(behaviorSource);
                this.Save(behaviorSource);
            }
            return flag;
        }

        public void Deselect(NodeConnection nodeConnection)
        {
            this.mSelectedNodeConnections.Remove(nodeConnection);
            nodeConnection.deselect();
        }

        public void Deselect(NodeDesigner nodeDesigner)
        {
            this.mSelectedNodes.Remove(nodeDesigner);
            this.mNodeSelectedID.Remove(nodeDesigner.Task.ID);
            nodeDesigner.Deselect();
            this.IndicateReferencedTasks(nodeDesigner.Task, false);
        }

        public void DeselectAllExcept(NodeDesigner nodeDesigner)
        {
            for (int i = this.mSelectedNodes.Count - 1; i >= 0; i--)
            {
                if (!this.mSelectedNodes[i].Equals(nodeDesigner))
                {
                    this.mSelectedNodes[i].Deselect();
                    this.mSelectedNodes.RemoveAt(i);
                    this.mNodeSelectedID.RemoveAt(i);
                }
            }
            this.IndicateReferencedTasks(nodeDesigner.Task, false);
        }

        public void DeselectWithParent(NodeDesigner nodeDesigner)
        {
            for (int i = this.mSelectedNodes.Count - 1; i >= 0; i--)
            {
                if (this.mSelectedNodes[i].HasParent(nodeDesigner))
                {
                    this.Deselect(this.mSelectedNodes[i]);
                }
            }
        }

        public void DestroyNodeDesigners()
        {
            if (this.mEntryNode != null)
            {
                this.Clear(this.mEntryNode);
            }
            if (this.mRootNode != null)
            {
                this.Clear(this.mRootNode);
            }
            for (int i = this.mDetachedNodes.Count - 1; i > -1; i--)
            {
                this.Clear(this.mDetachedNodes[i]);
            }
            this.mEntryNode = null;
            this.mRootNode = null;
            this.mDetachedNodes = new List<NodeDesigner>();
        }

        private void DragNode(NodeDesigner nodeDesigner, Vector2 delta, bool dragChildren)
        {
            if (!this.IsParentSelected(nodeDesigner) || !dragChildren)
            {
                nodeDesigner.ChangeOffset(delta);
                if (nodeDesigner.ParentNodeDesigner != null)
                {
                    int index = nodeDesigner.ParentNodeDesigner.ChildIndexForTask(nodeDesigner.Task);
                    if (index != -1)
                    {
                        int num2 = index - 1;
                        bool flag = false;
                        NodeDesigner designer = nodeDesigner.ParentNodeDesigner.NodeDesignerForChildIndex(num2);
                        if ((designer != null) && (nodeDesigner.Task.NodeData.Offset.x < designer.Task.NodeData.Offset.x))
                        {
                            nodeDesigner.ParentNodeDesigner.MoveChildNode(index, true);
                            flag = true;
                        }
                        if (!flag)
                        {
                            num2 = index + 1;
                            designer = nodeDesigner.ParentNodeDesigner.NodeDesignerForChildIndex(num2);
                            if ((designer != null) && (nodeDesigner.Task.NodeData.Offset.x > designer.Task.NodeData.Offset.x))
                            {
                                nodeDesigner.ParentNodeDesigner.MoveChildNode(index, false);
                            }
                        }
                    }
                }
                if (nodeDesigner.IsParent && !dragChildren)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if (task.Children != null)
                    {
                        for (int i = 0; i < task.Children.Count; i++)
                        {
                            (task.Children[i].NodeData.NodeDesigner as NodeDesigner).ChangeOffset(-delta);
                        }
                    }
                }
                this.MarkNodeDirty(nodeDesigner);
            }
        }

        public bool DragSelectedNodes(Vector2 delta, bool dragChildren)
        {
            if (this.mSelectedNodes.Count == 0)
            {
                return false;
            }
            bool flag = this.mSelectedNodes.Count == 1;
            for (int i = 0; i < this.mSelectedNodes.Count; i++)
            {
                this.DragNode(this.mSelectedNodes[i], delta, dragChildren);
            }
            if ((flag && dragChildren) && (this.mSelectedNodes[0].IsEntryDisplay && (this.mRootNode != null)))
            {
                this.DragNode(this.mRootNode, delta, dragChildren);
            }
            return true;
        }

        private bool DrawNodeChildren(NodeDesigner nodeDesigner, Vector2 offset, bool disabledNode)
        {
            if (nodeDesigner == null)
            {
                return false;
            }
            bool flag = false;
            if (nodeDesigner.DrawNode(offset, false, disabledNode))
            {
                flag = true;
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.NodeData.Collapsed || (task.Children == null))
                {
                    return flag;
                }
                for (int i = task.Children.Count - 1; i > -1; i--)
                {
                    if ((task.Children[i] != null) && this.DrawNodeChildren(task.Children[i].NodeData.NodeDesigner as NodeDesigner, offset, task.NodeData.Disabled || disabledNode))
                    {
                        flag = true;
                    }
                }
            }
            return flag;
        }

        private void DrawNodeCommentChildren(NodeDesigner nodeDesigner, Vector2 offset)
        {
            if (nodeDesigner != null)
            {
                nodeDesigner.DrawNodeComment(offset);
                if (nodeDesigner.IsParent)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if (!task.NodeData.Collapsed && (task.Children != null))
                    {
                        for (int i = 0; i < task.Children.Count; i++)
                        {
                            if (task.Children[i] != null)
                            {
                                this.DrawNodeCommentChildren(task.Children[i].NodeData.NodeDesigner as NodeDesigner, offset);
                            }
                        }
                    }
                }
            }
        }

        private void DrawNodeConnectionChildren(NodeDesigner nodeDesigner, Vector2 offset, bool disabledNode)
        {
            if ((nodeDesigner != null) && !nodeDesigner.Task.NodeData.Collapsed)
            {
                nodeDesigner.DrawNodeConnection(offset, nodeDesigner.Task.NodeData.Disabled || disabledNode);
                if (nodeDesigner.IsParent)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if (task.Children != null)
                    {
                        for (int i = 0; i < task.Children.Count; i++)
                        {
                            if (task.Children[i] != null)
                            {
                                this.DrawNodeConnectionChildren(task.Children[i].NodeData.NodeDesigner as NodeDesigner, offset, task.NodeData.Disabled || disabledNode);
                            }
                        }
                    }
                }
            }
        }

        public bool DrawNodes(Vector2 mousePosition, Vector2 offset)
        {
            if (this.mEntryNode == null)
            {
                return false;
            }
            this.mEntryNode.DrawNodeConnection(offset, false);
            if (this.mRootNode != null)
            {
                this.DrawNodeConnectionChildren(this.mRootNode, offset, this.mRootNode.Task.NodeData.Disabled);
            }
            for (int i = 0; i < this.mDetachedNodes.Count; i++)
            {
                this.DrawNodeConnectionChildren(this.mDetachedNodes[i], offset, this.mDetachedNodes[i].Task.NodeData.Disabled);
            }
            for (int j = 0; j < this.mSelectedNodeConnections.Count; j++)
            {
                this.mSelectedNodeConnections[j].DrawConnection(offset, this.mSelectedNodeConnections[j].OriginatingNodeDesigner.IsDisabled());
            }
            if ((mousePosition != new Vector2(-1f, -1f)) && (this.mActiveNodeConnection != null))
            {
                this.mActiveNodeConnection.HorizontalHeight = (this.mActiveNodeConnection.OriginatingNodeDesigner.GetConnectionPosition(offset, this.mActiveNodeConnection.NodeConnectionType).y + mousePosition.y) / 2f;
                this.mActiveNodeConnection.DrawConnection(this.mActiveNodeConnection.OriginatingNodeDesigner.GetConnectionPosition(offset, this.mActiveNodeConnection.NodeConnectionType), mousePosition, (this.mActiveNodeConnection.NodeConnectionType == NodeConnectionType.Outgoing) && this.mActiveNodeConnection.OriginatingNodeDesigner.IsDisabled());
            }
            this.mEntryNode.DrawNode(offset, false, false);
            bool flag = false;
            if ((this.mRootNode != null) && this.DrawNodeChildren(this.mRootNode, offset, this.mRootNode.Task.NodeData.Disabled))
            {
                flag = true;
            }
            for (int k = 0; k < this.mDetachedNodes.Count; k++)
            {
                if (this.DrawNodeChildren(this.mDetachedNodes[k], offset, this.mDetachedNodes[k].Task.NodeData.Disabled))
                {
                    flag = true;
                }
            }
            for (int m = 0; m < this.mSelectedNodes.Count; m++)
            {
                if (this.mSelectedNodes[m].DrawNode(offset, true, this.mSelectedNodes[m].IsDisabled()))
                {
                    flag = true;
                }
            }
            if (this.mRootNode != null)
            {
                this.DrawNodeCommentChildren(this.mRootNode, offset);
            }
            for (int n = 0; n < this.mDetachedNodes.Count; n++)
            {
                this.DrawNodeCommentChildren(this.mDetachedNodes[n], offset);
            }
            return flag;
        }

        public Vector2 EntryNodePosition()
        {
            return this.mEntryNode.GetAbsolutePosition();
        }

        public List<BehaviorSource> FindReferencedBehaviors()
        {
            List<BehaviorSource> behaviors = new List<BehaviorSource>();
            if (this.mRootNode != null)
            {
                this.FindReferencedBehaviors(this.mRootNode, ref behaviors);
            }
            for (int i = 0; i < this.mDetachedNodes.Count; i++)
            {
                this.FindReferencedBehaviors(this.mDetachedNodes[i], ref behaviors);
            }
            return behaviors;
        }

        public void FindReferencedBehaviors(NodeDesigner nodeDesigner, ref List<BehaviorSource> behaviors)
        {
            System.Reflection.FieldInfo[] publicFields = TaskUtility.GetPublicFields(nodeDesigner.Task.GetType());
            for (int i = 0; i < publicFields.Length; i++)
            {
                System.Type fieldType = publicFields[i].FieldType;
                if (typeof(IList).IsAssignableFrom(fieldType))
                {
                    System.Type c = fieldType;
                    if (fieldType.IsGenericType)
                    {
                        while (!c.IsGenericType)
                        {
                            c = c.BaseType;
                        }
                        c = fieldType.GetGenericArguments()[0];
                    }
                    else
                    {
                        c = fieldType.GetElementType();
                    }
                    if (c != null)
                    {
                        if (typeof(ExternalBehavior).IsAssignableFrom(c) || typeof(Behavior).IsAssignableFrom(c))
                        {
                            IList list = publicFields[i].GetValue(nodeDesigner.Task) as IList;
                            if (list != null)
                            {
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (list[j] != null)
                                    {
                                        BehaviorSource item = null;
                                        if (list[j] is ExternalBehavior)
                                        {
                                            item = (list[j] as ExternalBehavior).BehaviorSource;
                                            if (item.Owner == null)
                                            {
                                                item.Owner = list[j] as ExternalBehavior;
                                            }
                                        }
                                        else
                                        {
                                            item = (list[j] as Behavior).GetBehaviorSource();
                                            if (item.Owner == null)
                                            {
                                                item.Owner = list[j] as Behavior;
                                            }
                                        }
                                        behaviors.Add(item);
                                    }
                                }
                            }
                        }
                        else if (typeof(Behavior).IsAssignableFrom(c))
                        {
                        }
                    }
                }
                else if (typeof(ExternalBehavior).IsAssignableFrom(fieldType) || typeof(Behavior).IsAssignableFrom(fieldType))
                {
                    object obj2 = publicFields[i].GetValue(nodeDesigner.Task);
                    if (obj2 != null)
                    {
                        BehaviorSource behaviorSource = null;
                        if (obj2 is ExternalBehavior)
                        {
                            behaviorSource = (obj2 as ExternalBehavior).BehaviorSource;
                            if (behaviorSource.Owner == null)
                            {
                                behaviorSource.Owner = obj2 as ExternalBehavior;
                            }
                            behaviors.Add(behaviorSource);
                        }
                        else
                        {
                            behaviorSource = (obj2 as Behavior).GetBehaviorSource();
                            if (behaviorSource.Owner == null)
                            {
                                behaviorSource.Owner = obj2 as Behavior;
                            }
                        }
                        behaviors.Add(behaviorSource);
                    }
                }
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children != null)
                {
                    for (int k = 0; k < task.Children.Count; k++)
                    {
                        if (task.Children[k] != null)
                        {
                            this.FindReferencedBehaviors(task.Children[k].NodeData.NodeDesigner as NodeDesigner, ref behaviors);
                        }
                    }
                }
            }
        }

        private void GetNodeMinMax(NodeDesigner nodeDesigner, ref Rect minMaxRect)
        {
            Rect rect = nodeDesigner.Rectangle(Vector2.zero, true, true);
            if (rect.xMin < minMaxRect.xMin)
            {
                minMaxRect.xMin = rect.xMin;
            }
            if (rect.yMin < minMaxRect.yMin)
            {
                minMaxRect.yMin = rect.yMin;
            }
            if (rect.xMax > minMaxRect.xMax)
            {
                minMaxRect.xMax = rect.xMax;
            }
            if (rect.yMax > minMaxRect.yMax)
            {
                minMaxRect.yMax = rect.yMax;
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children != null)
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        this.GetNodeMinMax(task.Children[i].NodeData.NodeDesigner as NodeDesigner, ref minMaxRect);
                    }
                }
            }
        }

        public void GraphDirty()
        {
            if (this.mEntryNode != null)
            {
                this.mEntryNode.MarkDirty();
                if (this.mRootNode != null)
                {
                    this.MarkNodeDirty(this.mRootNode);
                }
                for (int i = this.mDetachedNodes.Count - 1; i > -1; i--)
                {
                    this.MarkNodeDirty(this.mDetachedNodes[i]);
                }
            }
        }

        public Rect GraphSize()
        {
            if (this.mEntryNode == null)
            {
                return new Rect();
            }
            Rect minMaxRect = new Rect {
                xMin = float.MaxValue,
                xMax = float.MinValue,
                yMin = float.MaxValue,
                yMax = float.MinValue
            };
            this.GetNodeMinMax(this.mEntryNode, ref minMaxRect);
            if (this.mRootNode != null)
            {
                this.GetNodeMinMax(this.mRootNode, ref minMaxRect);
            }
            for (int i = 0; i < this.mDetachedNodes.Count; i++)
            {
                this.GetNodeMinMax(this.mDetachedNodes[i], ref minMaxRect);
            }
            return minMaxRect;
        }

        public bool HasEntryNode()
        {
            return ((this.mEntryNode != null) && (this.mEntryNode.Task != null));
        }

        public void Hover(NodeDesigner nodeDesigner)
        {
            if (!nodeDesigner.ShowHoverBar)
            {
                nodeDesigner.ShowHoverBar = true;
                this.HoverNode = nodeDesigner;
            }
        }

        public void IdentifyNode(NodeDesigner nodeDesigner)
        {
            nodeDesigner.IdentifyNode();
        }

        private void IndicateReferencedTasks(Task task, bool indicate)
        {
            List<Task> referencedTasks = TaskInspector.GetReferencedTasks(task);
            NodeDesigner nodeDesigner = null;
            if ((referencedTasks != null) && (referencedTasks.Count > 0))
            {
                for (int i = 0; i < referencedTasks.Count; i++)
                {
                    if ((referencedTasks[i] != null) && (referencedTasks[i].NodeData != null))
                    {
                        nodeDesigner = referencedTasks[i].NodeData.NodeDesigner as NodeDesigner;
                        if (nodeDesigner != null)
                        {
                            nodeDesigner.ShowReferenceIcon = indicate;
                        }
                    }
                }
            }
        }

        public bool IsParentSelected(NodeDesigner nodeDesigner)
        {
            if (nodeDesigner.ParentNodeDesigner == null)
            {
                return false;
            }
            return (this.IsSelected(nodeDesigner.ParentNodeDesigner) || this.IsParentSelected(nodeDesigner.ParentNodeDesigner));
        }

        public bool IsSelected(NodeConnection nodeConnection)
        {
            for (int i = 0; i < this.mSelectedNodeConnections.Count; i++)
            {
                if (this.mSelectedNodeConnections[i].Equals(nodeConnection))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSelected(NodeDesigner nodeDesigner)
        {
            return this.mSelectedNodes.Contains(nodeDesigner);
        }

        public bool Load(BehaviorSource behaviorSource, bool loadPrevBehavior, Vector2 nodePosition)
        {
            Task task;
            Task task2;
            List<Task> list2;
            if (behaviorSource == null)
            {
                this.Clear(false);
                return false;
            }
            this.DestroyNodeDesigners();
            if (((behaviorSource.Owner != null) && (behaviorSource.Owner is Behavior)) && ((behaviorSource.Owner as Behavior).ExternalBehavior != null))
            {
                List<SharedVariable> allVariables = null;
                bool force = !Application.isPlaying;
                if (Application.isPlaying && !(behaviorSource.Owner as Behavior).HasInheritedVariables)
                {
                    behaviorSource.CheckForSerialization(true, null);
                    allVariables = behaviorSource.GetAllVariables();
                    (behaviorSource.Owner as Behavior).HasInheritedVariables = true;
                    force = true;
                }
                ExternalBehavior externalBehavior = (behaviorSource.Owner as Behavior).ExternalBehavior;
                externalBehavior.BehaviorSource.Owner = externalBehavior;
                externalBehavior.BehaviorSource.CheckForSerialization(force, behaviorSource);
                if (allVariables != null)
                {
                    for (int i = 0; i < allVariables.Count; i++)
                    {
                        behaviorSource.SetVariable(allVariables[i].Name, allVariables[i]);
                    }
                }
            }
            else
            {
                behaviorSource.CheckForSerialization(!Application.isPlaying, null);
            }
            if (((behaviorSource.EntryTask == null) && (behaviorSource.RootTask == null)) && (behaviorSource.DetachedTasks == null))
            {
                this.Clear(false);
                return false;
            }
            if (loadPrevBehavior)
            {
                this.mSelectedNodes.Clear();
                this.mSelectedNodeConnections.Clear();
                if (this.mPrevNodeSelectedID != null)
                {
                    for (int j = 0; j < this.mPrevNodeSelectedID.Length; j++)
                    {
                        this.mNodeSelectedID.Add(this.mPrevNodeSelectedID[j]);
                    }
                    this.mPrevNodeSelectedID = null;
                }
            }
            else
            {
                this.Clear(false);
            }
            this.mNextTaskID = 0;
            this.mEntryNode = null;
            this.mRootNode = null;
            this.mDetachedNodes.Clear();
            behaviorSource.Load(out task, out task2, out list2);
            if (BehaviorDesignerUtility.AnyNullTasks(behaviorSource) || (((behaviorSource.TaskData != null) && BehaviorDesignerUtility.HasRootTask(behaviorSource.TaskData.JSONSerialization)) && (behaviorSource.RootTask == null)))
            {
                behaviorSource.CheckForSerialization(true, null);
                behaviorSource.Load(out task, out task2, out list2);
            }
            if (task == null)
            {
                if ((task2 != null) || ((list2 != null) && (list2.Count > 0)))
                {
                    behaviorSource.EntryTask = task = Activator.CreateInstance(TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.Tasks.EntryTask"), true) as Task;
                    this.mEntryNode = ScriptableObject.CreateInstance<NodeDesigner>();
                    if (task2 != null)
                    {
                        this.mEntryNode.LoadNode(task, behaviorSource, new Vector2(task2.NodeData.Offset.x, task2.NodeData.Offset.y - 120f), ref this.mNextTaskID);
                    }
                    else
                    {
                        this.mEntryNode.LoadNode(task, behaviorSource, new Vector2(nodePosition.x, nodePosition.y - 120f), ref this.mNextTaskID);
                    }
                    this.mEntryNode.MakeEntryDisplay();
                    EditorUtility.SetDirty(behaviorSource.Owner.GetObject());
                }
            }
            else
            {
                this.mEntryNode = ScriptableObject.CreateInstance<NodeDesigner>();
                this.mEntryNode.LoadTask(task, (behaviorSource.Owner == null) ? null : (behaviorSource.Owner.GetObject() as Behavior), ref this.mNextTaskID);
                this.mEntryNode.MakeEntryDisplay();
            }
            if (task2 != null)
            {
                this.mRootNode = ScriptableObject.CreateInstance<NodeDesigner>();
                this.mRootNode.LoadTask(task2, (behaviorSource.Owner == null) ? null : (behaviorSource.Owner.GetObject() as Behavior), ref this.mNextTaskID);
                NodeConnection nodeConnection = ScriptableObject.CreateInstance<NodeConnection>();
                nodeConnection.LoadConnection(this.mEntryNode, NodeConnectionType.Fixed);
                this.mEntryNode.AddChildNode(this.mRootNode, nodeConnection, false, false);
                this.LoadNodeSelection(this.mRootNode);
                if (this.mEntryNode.OutgoingNodeConnections.Count == 0)
                {
                    this.mActiveNodeConnection = ScriptableObject.CreateInstance<NodeConnection>();
                    this.mActiveNodeConnection.LoadConnection(this.mEntryNode, NodeConnectionType.Outgoing);
                    this.ConnectNodes(behaviorSource, this.mRootNode);
                }
            }
            if (list2 != null)
            {
                for (int k = 0; k < list2.Count; k++)
                {
                    if (list2[k] != null)
                    {
                        NodeDesigner item = ScriptableObject.CreateInstance<NodeDesigner>();
                        item.LoadTask(list2[k], (behaviorSource.Owner == null) ? null : (behaviorSource.Owner.GetObject() as Behavior), ref this.mNextTaskID);
                        this.mDetachedNodes.Add(item);
                        this.LoadNodeSelection(item);
                    }
                }
            }
            return true;
        }

        private void LoadNodeSelection(NodeDesigner nodeDesigner)
        {
            if (nodeDesigner != null)
            {
                if ((this.mNodeSelectedID != null) && this.mNodeSelectedID.Contains(nodeDesigner.Task.ID))
                {
                    this.Select(nodeDesigner, false);
                }
                if (nodeDesigner.IsParent)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if (task.Children != null)
                    {
                        for (int i = 0; i < task.Children.Count; i++)
                        {
                            if ((task.Children[i] != null) && (task.Children[i].NodeData != null))
                            {
                                this.LoadNodeSelection(task.Children[i].NodeData.NodeDesigner as NodeDesigner);
                            }
                        }
                    }
                }
            }
        }

        private void MarkNodeDirty(NodeDesigner nodeDesigner)
        {
            nodeDesigner.MarkDirty();
            if (nodeDesigner.IsEntryDisplay)
            {
                if ((nodeDesigner.OutgoingNodeConnections.Count > 0) && (nodeDesigner.OutgoingNodeConnections[0].DestinationNodeDesigner != null))
                {
                    this.MarkNodeDirty(nodeDesigner.OutgoingNodeConnections[0].DestinationNodeDesigner);
                }
            }
            else if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children != null)
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        if (task.Children[i] != null)
                        {
                            this.MarkNodeDirty(task.Children[i].NodeData.NodeDesigner as NodeDesigner);
                        }
                    }
                }
            }
        }

        public NodeDesigner NodeAt(Vector2 point, Vector2 offset)
        {
            if (this.mEntryNode != null)
            {
                for (int i = 0; i < this.mSelectedNodes.Count; i++)
                {
                    if (this.mSelectedNodes[i].Contains(point, offset, false))
                    {
                        return this.mSelectedNodes[i];
                    }
                }
                NodeDesigner designer = null;
                for (int j = this.mDetachedNodes.Count - 1; j > -1; j--)
                {
                    if ((this.mDetachedNodes[j] != null) && ((designer = this.NodeChildrenAt(this.mDetachedNodes[j], point, offset)) != null))
                    {
                        return designer;
                    }
                }
                if ((this.mRootNode != null) && ((designer = this.NodeChildrenAt(this.mRootNode, point, offset)) != null))
                {
                    return designer;
                }
                if (this.mEntryNode.Contains(point, offset, true))
                {
                    return this.mEntryNode;
                }
            }
            return null;
        }

        public bool NodeCanAcceptConnection(NodeDesigner nodeDesigner, NodeConnection connection)
        {
            if ((!nodeDesigner.IsEntryDisplay || (connection.NodeConnectionType != NodeConnectionType.Incoming)) && (nodeDesigner.IsEntryDisplay || (!nodeDesigner.IsParent && (nodeDesigner.IsParent || (connection.NodeConnectionType != NodeConnectionType.Outgoing)))))
            {
                return false;
            }
            if (!nodeDesigner.IsEntryDisplay && !connection.OriginatingNodeDesigner.IsEntryDisplay)
            {
                HashSet<NodeDesigner> set = new HashSet<NodeDesigner>();
                NodeDesigner designer = (connection.NodeConnectionType != NodeConnectionType.Outgoing) ? connection.OriginatingNodeDesigner : nodeDesigner;
                NodeDesigner item = (connection.NodeConnectionType != NodeConnectionType.Outgoing) ? nodeDesigner : connection.OriginatingNodeDesigner;
                if (this.CycleExists(designer, ref set))
                {
                    return false;
                }
                if (set.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        public bool NodeCanOriginateConnection(NodeDesigner nodeDesigner, NodeConnection connection)
        {
            return (!nodeDesigner.IsEntryDisplay || (nodeDesigner.IsEntryDisplay && (connection.NodeConnectionType == NodeConnectionType.Outgoing)));
        }

        private NodeDesigner NodeChildrenAt(NodeDesigner nodeDesigner, Vector2 point, Vector2 offset)
        {
            if (nodeDesigner.Contains(point, offset, true))
            {
                return nodeDesigner;
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                NodeDesigner designer = null;
                if (!task.NodeData.Collapsed && (task.Children != null))
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        if ((task.Children[i] != null) && ((designer = this.NodeChildrenAt(task.Children[i].NodeData.NodeDesigner as NodeDesigner, point, offset)) != null))
                        {
                            return designer;
                        }
                    }
                }
            }
            return null;
        }

        private void NodeChildrenConnectionsAt(NodeDesigner nodeDesigner, Vector2 point, Vector2 offset, ref List<NodeConnection> nodeConnections)
        {
            if (!nodeDesigner.Task.NodeData.Collapsed)
            {
                nodeDesigner.ConnectionContains(point, offset, ref nodeConnections);
                if (nodeDesigner.IsParent)
                {
                    ParentTask task = nodeDesigner.Task as ParentTask;
                    if ((task != null) && (task.Children != null))
                    {
                        for (int i = 0; i < task.Children.Count; i++)
                        {
                            if (task.Children[i] != null)
                            {
                                this.NodeChildrenConnectionsAt(task.Children[i].NodeData.NodeDesigner as NodeDesigner, point, offset, ref nodeConnections);
                            }
                        }
                    }
                }
            }
        }

        public void NodeConnectionsAt(Vector2 point, Vector2 offset, ref List<NodeConnection> nodeConnections)
        {
            if (this.mEntryNode != null)
            {
                this.NodeChildrenConnectionsAt(this.mEntryNode, point, offset, ref nodeConnections);
                if (this.mRootNode != null)
                {
                    this.NodeChildrenConnectionsAt(this.mRootNode, point, offset, ref nodeConnections);
                }
                for (int i = 0; i < this.mDetachedNodes.Count; i++)
                {
                    this.NodeChildrenConnectionsAt(this.mDetachedNodes[i], point, offset, ref nodeConnections);
                }
            }
        }

        public List<NodeDesigner> NodesAt(Rect rect, Vector2 offset)
        {
            List<NodeDesigner> nodes = new List<NodeDesigner>();
            if (this.mRootNode != null)
            {
                this.NodesChildrenAt(this.mRootNode, rect, offset, ref nodes);
            }
            for (int i = 0; i < this.mDetachedNodes.Count; i++)
            {
                this.NodesChildrenAt(this.mDetachedNodes[i], rect, offset, ref nodes);
            }
            return ((nodes.Count <= 0) ? null : nodes);
        }

        private void NodesChildrenAt(NodeDesigner nodeDesigner, Rect rect, Vector2 offset, ref List<NodeDesigner> nodes)
        {
            if (nodeDesigner.Intersects(rect, offset))
            {
                nodes.Add(nodeDesigner);
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (!task.NodeData.Collapsed && (task.Children != null))
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        if (task.Children[i] != null)
                        {
                            this.NodesChildrenAt(task.Children[i].NodeData.NodeDesigner as NodeDesigner, rect, offset, ref nodes);
                        }
                    }
                }
            }
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        public bool Paste(BehaviorSource behaviorSource, List<TaskSerializer> copiedTasks, Vector2 graphOffset, float graphZoom)
        {
            if ((copiedTasks == null) || (copiedTasks.Count == 0))
            {
                return false;
            }
            this.ClearNodeSelection();
            this.ClearConnectionSelection();
            this.RemapIDs();
            List<NodeDesigner> list = new List<NodeDesigner>();
            for (int i = 0; i < copiedTasks.Count; i++)
            {
                TaskSerializer serializer = copiedTasks[i];
                Task task = TaskCopier.PasteTask(behaviorSource, serializer);
                NodeDesigner item = ScriptableObject.CreateInstance<NodeDesigner>();
                item.LoadTask(task, (behaviorSource.Owner == null) ? null : (behaviorSource.Owner.GetObject() as Behavior), ref this.mNextTaskID);
                item.Task.NodeData.Offset = ((Vector2) (serializer.offset / graphZoom)) - graphOffset;
                list.Add(item);
                this.mDetachedNodes.Add(item);
                this.Select(item);
            }
            for (int j = 0; j < copiedTasks.Count; j++)
            {
                TaskSerializer serializer2 = copiedTasks[j];
                if (serializer2.childrenIndex != null)
                {
                    for (int k = 0; k < serializer2.childrenIndex.Count; k++)
                    {
                        NodeDesigner nodeDesigner = list[j];
                        NodeConnection nodeConnection = ScriptableObject.CreateInstance<NodeConnection>();
                        nodeConnection.LoadConnection(nodeDesigner, NodeConnectionType.Outgoing);
                        nodeDesigner.AddChildNode(list[serializer2.childrenIndex[k]], nodeConnection, true, false);
                        this.mDetachedNodes.Remove(list[serializer2.childrenIndex[k]]);
                    }
                }
            }
            this.Save(behaviorSource);
            return true;
        }

        private void RemapIDs()
        {
            if (this.mEntryNode != null)
            {
                this.mNextTaskID = 0;
                this.mEntryNode.SetID(ref this.mNextTaskID);
                if (this.mRootNode != null)
                {
                    this.mRootNode.SetID(ref this.mNextTaskID);
                }
                for (int i = 0; i < this.mDetachedNodes.Count; i++)
                {
                    this.mDetachedNodes[i].SetID(ref this.mNextTaskID);
                }
                this.mNodeSelectedID.Clear();
                for (int j = 0; j < this.mSelectedNodes.Count; j++)
                {
                    this.mNodeSelectedID.Add(this.mSelectedNodes[j].Task.ID);
                }
            }
        }

        public void RemoveConnection(NodeConnection nodeConnection)
        {
            nodeConnection.DestinationNodeDesigner.Task.NodeData.Offset = nodeConnection.DestinationNodeDesigner.GetAbsolutePosition();
            this.mDetachedNodes.Add(nodeConnection.DestinationNodeDesigner);
            nodeConnection.OriginatingNodeDesigner.RemoveChildNode(nodeConnection.DestinationNodeDesigner);
            if (nodeConnection.OriginatingNodeDesigner.IsEntryDisplay)
            {
                this.mRootNode = null;
            }
        }

        private void RemoveNode(NodeDesigner nodeDesigner)
        {
            if (!nodeDesigner.IsEntryDisplay)
            {
                if (nodeDesigner.IsParent)
                {
                    for (int i = 0; i < nodeDesigner.OutgoingNodeConnections.Count; i++)
                    {
                        NodeDesigner destinationNodeDesigner = nodeDesigner.OutgoingNodeConnections[i].DestinationNodeDesigner;
                        this.mDetachedNodes.Add(destinationNodeDesigner);
                        destinationNodeDesigner.Task.NodeData.Offset = destinationNodeDesigner.GetAbsolutePosition();
                        destinationNodeDesigner.ParentNodeDesigner = null;
                    }
                }
                if (nodeDesigner.ParentNodeDesigner != null)
                {
                    nodeDesigner.ParentNodeDesigner.RemoveChildNode(nodeDesigner);
                }
                if ((this.mRootNode != null) && this.mRootNode.Equals(nodeDesigner))
                {
                    this.mEntryNode.RemoveChildNode(nodeDesigner);
                    this.mRootNode = null;
                }
                if (this.mRootNode != null)
                {
                    this.RemoveReferencedTasks(this.mRootNode, nodeDesigner.Task);
                }
                if (this.mDetachedNodes != null)
                {
                    for (int j = 0; j < this.mDetachedNodes.Count; j++)
                    {
                        this.RemoveReferencedTasks(this.mDetachedNodes[j], nodeDesigner.Task);
                    }
                }
                this.mDetachedNodes.Remove(nodeDesigner);
                BehaviorUndo.DestroyObject(nodeDesigner, false);
            }
        }

        private void RemoveParentConnection(NodeDesigner nodeDesigner)
        {
            if (nodeDesigner.ParentNodeDesigner != null)
            {
                NodeDesigner parentNodeDesigner = nodeDesigner.ParentNodeDesigner;
                NodeConnection nodeConnection = null;
                for (int i = 0; i < parentNodeDesigner.OutgoingNodeConnections.Count; i++)
                {
                    if (parentNodeDesigner.OutgoingNodeConnections[i].DestinationNodeDesigner.Equals(nodeDesigner))
                    {
                        nodeConnection = parentNodeDesigner.OutgoingNodeConnections[i];
                        break;
                    }
                }
                if (nodeConnection != null)
                {
                    this.RemoveConnection(nodeConnection);
                }
            }
        }

        private void RemoveReferencedTasks(NodeDesigner nodeDesigner, Task task)
        {
            bool fullSync = false;
            bool doReference = false;
            System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(nodeDesigner.Task.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if ((!allFields[i].IsPrivate && !allFields[i].IsFamily) || BehaviorDesignerUtility.HasAttribute(allFields[i], typeof(SerializeField)))
                {
                    if (typeof(IList).IsAssignableFrom(allFields[i].FieldType))
                    {
                        if (typeof(Task).IsAssignableFrom(allFields[i].FieldType.GetElementType()) || (allFields[i].FieldType.IsGenericType && typeof(Task).IsAssignableFrom(allFields[i].FieldType.GetGenericArguments()[0])))
                        {
                            Task[] taskArray = allFields[i].GetValue(nodeDesigner.Task) as Task[];
                            if (taskArray != null)
                            {
                                for (int j = taskArray.Length - 1; j > -1; j--)
                                {
                                    if (nodeDesigner.Task.Equals(task) || taskArray[i].Equals(task))
                                    {
                                        TaskInspector.ReferenceTasks(nodeDesigner.Task, task, allFields[i], ref fullSync, ref doReference, false, false);
                                    }
                                }
                            }
                        }
                    }
                    else if (typeof(Task).IsAssignableFrom(allFields[i].FieldType))
                    {
                        Task task2 = allFields[i].GetValue(nodeDesigner.Task) as Task;
                        if ((task2 != null) && (nodeDesigner.Task.Equals(task) || task2.Equals(task)))
                        {
                            TaskInspector.ReferenceTasks(nodeDesigner.Task, task, allFields[i], ref fullSync, ref doReference, false, false);
                        }
                    }
                }
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task3 = nodeDesigner.Task as ParentTask;
                if (task3.Children != null)
                {
                    for (int k = 0; k < task3.Children.Count; k++)
                    {
                        if (task3.Children[k] != null)
                        {
                            this.RemoveReferencedTasks(task3.Children[k].NodeData.NodeDesigner as NodeDesigner, task);
                        }
                    }
                }
            }
        }

        private bool RemoveSharedVariableReference(NodeDesigner nodeDesigner, SharedVariable sharedVariable)
        {
            bool flag = false;
            System.Reflection.FieldInfo[] allFields = TaskUtility.GetAllFields(nodeDesigner.Task.GetType());
            for (int i = 0; i < allFields.Length; i++)
            {
                if (typeof(SharedVariable).IsAssignableFrom(allFields[i].FieldType))
                {
                    SharedVariable variable = allFields[i].GetValue(nodeDesigner.Task) as SharedVariable;
                    if (((variable != null) && !string.IsNullOrEmpty(variable.Name)) && ((variable.IsGlobal == sharedVariable.IsGlobal) && variable.Name.Equals(sharedVariable.Name)))
                    {
                        if (!allFields[i].FieldType.IsAbstract)
                        {
                            variable = Activator.CreateInstance(allFields[i].FieldType) as SharedVariable;
                            variable.IsShared = true;
                            allFields[i].SetValue(nodeDesigner.Task, variable);
                        }
                        flag = true;
                    }
                }
            }
            if (nodeDesigner.IsParent)
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children == null)
                {
                    return flag;
                }
                for (int j = 0; j < task.Children.Count; j++)
                {
                    if ((task.Children[j] != null) && this.RemoveSharedVariableReference(task.Children[j].NodeData.NodeDesigner as NodeDesigner, sharedVariable))
                    {
                        flag = true;
                    }
                }
            }
            return flag;
        }

        public bool RemoveSharedVariableReferences(SharedVariable sharedVariable)
        {
            if (this.mEntryNode == null)
            {
                return false;
            }
            bool flag = false;
            if ((this.mRootNode != null) && this.RemoveSharedVariableReference(this.mRootNode, sharedVariable))
            {
                flag = true;
            }
            if (this.mDetachedNodes != null)
            {
                for (int i = 0; i < this.mDetachedNodes.Count; i++)
                {
                    if (this.RemoveSharedVariableReference(this.mDetachedNodes[i], sharedVariable))
                    {
                        flag = true;
                    }
                }
            }
            return flag;
        }

        public bool ReplaceSelectedNode(BehaviorSource behaviorSource, System.Type taskType)
        {
            BehaviorUndo.RegisterUndo("Replace", behaviorSource.Owner.GetObject());
            Vector2 absolutePosition = this.SelectedNodes[0].GetAbsolutePosition();
            NodeDesigner parentNodeDesigner = this.SelectedNodes[0].ParentNodeDesigner;
            List<Task> list = !this.SelectedNodes[0].IsParent ? null : (this.SelectedNodes[0].Task as ParentTask).Children;
            this.RemoveNode(this.SelectedNodes[0]);
            this.mSelectedNodes.Clear();
            TaskReferences.CheckReferences(behaviorSource);
            NodeDesigner nodeDesigner = this.AddNode(behaviorSource, taskType, absolutePosition);
            if (nodeDesigner == null)
            {
                return false;
            }
            if (parentNodeDesigner != null)
            {
                this.ActiveNodeConnection = parentNodeDesigner.CreateNodeConnection(false);
                this.ConnectNodes(behaviorSource, nodeDesigner);
            }
            if (nodeDesigner.IsParent && (list != null))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    this.ActiveNodeConnection = nodeDesigner.CreateNodeConnection(false);
                    this.ConnectNodes(behaviorSource, list[i].NodeData.NodeDesigner as NodeDesigner);
                    if (i >= (nodeDesigner.Task as ParentTask).MaxChildren())
                    {
                        break;
                    }
                }
            }
            this.Select(nodeDesigner);
            return true;
        }

        public void Save(BehaviorSource behaviorSource)
        {
            if (!object.ReferenceEquals(behaviorSource.Owner.GetObject(), null))
            {
                this.RemapIDs();
                List<Task> detachedTasks = new List<Task>();
                for (int i = 0; i < this.mDetachedNodes.Count; i++)
                {
                    detachedTasks.Add(this.mDetachedNodes[i].Task);
                }
                behaviorSource.Save((this.mEntryNode == null) ? null : this.mEntryNode.Task, (this.mRootNode == null) ? null : this.mRootNode.Task, detachedTasks);
                if (BehaviorDesignerPreferences.GetBool(BDPreferences.BinarySerialization))
                {
                    BinarySerialization.Save(behaviorSource);
                }
                else
                {
                    SerializeJSON.Save(behaviorSource);
                }
            }
        }

        public void Select(NodeConnection nodeConnection)
        {
            this.mSelectedNodeConnections.Add(nodeConnection);
            nodeConnection.select();
        }

        public void Select(NodeDesigner nodeDesigner)
        {
            this.Select(nodeDesigner, true);
        }

        public void Select(NodeDesigner nodeDesigner, bool addHash)
        {
            if (this.mSelectedNodes.Count == 1)
            {
                this.IndicateReferencedTasks(this.mSelectedNodes[0].Task, false);
            }
            this.mSelectedNodes.Add(nodeDesigner);
            if (addHash)
            {
                this.mNodeSelectedID.Add(nodeDesigner.Task.ID);
            }
            nodeDesigner.Select();
            if (this.mSelectedNodes.Count == 1)
            {
                this.IndicateReferencedTasks(this.mSelectedNodes[0].Task, true);
            }
        }

        public void SelectAll()
        {
            for (int i = this.mSelectedNodes.Count - 1; i > -1; i--)
            {
                this.Deselect(this.mSelectedNodes[i]);
            }
            if (this.mRootNode != null)
            {
                this.SelectAll(this.mRootNode);
            }
            for (int j = this.mDetachedNodes.Count - 1; j > -1; j--)
            {
                this.SelectAll(this.mDetachedNodes[j]);
            }
        }

        private void SelectAll(NodeDesigner nodeDesigner)
        {
            this.Select(nodeDesigner);
            if (nodeDesigner.Task.GetType().IsSubclassOf(typeof(ParentTask)))
            {
                ParentTask task = nodeDesigner.Task as ParentTask;
                if (task.Children != null)
                {
                    for (int i = 0; i < task.Children.Count; i++)
                    {
                        this.SelectAll(task.Children[i].NodeData.NodeDesigner as NodeDesigner);
                    }
                }
            }
        }

        public void SetRootNodesOffset(Vector2 offset)
        {
            Vector2 vector = this.mEntryNode.Task.NodeData.Offset - offset;
            this.mEntryNode.Task.NodeData.Offset = offset;
            for (int i = 0; i < this.mDetachedNodes.Count; i++)
            {
                NodeData nodeData = this.mDetachedNodes[i].Task.NodeData;
                nodeData.Offset -= vector;
            }
        }

        public NodeConnection ActiveNodeConnection
        {
            get
            {
                return this.mActiveNodeConnection;
            }
            set
            {
                this.mActiveNodeConnection = value;
            }
        }

        public List<NodeDesigner> DetachedNodes
        {
            get
            {
                return this.mDetachedNodes;
            }
        }

        public NodeDesigner HoverNode
        {
            get
            {
                return this.mHoverNode;
            }
            set
            {
                this.mHoverNode = value;
            }
        }

        public NodeDesigner RootNode
        {
            get
            {
                return this.mRootNode;
            }
        }

        public List<NodeConnection> SelectedNodeConnections
        {
            get
            {
                return this.mSelectedNodeConnections;
            }
        }

        public List<NodeDesigner> SelectedNodes
        {
            get
            {
                return this.mSelectedNodes;
            }
        }
    }
}

