namespace BehaviorDesigner.Editor
{
    using BehaviorDesigner.Runtime;
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    [Serializable]
    public class NodeDesigner : ScriptableObject
    {
        private Rect breakpointTextureRect;
        private Rect collapseButtonTextureRect;
        private Rect commentLabelRect;
        private Rect commentRect;
        private Rect conditionalAbortLowerPriorityTextureRect;
        private Rect conditionalAbortTextureRect;
        private Rect disabledButtonTextureRect;
        private Rect errorTextureRect;
        private Rect failureExecutionStatusTextureRect;
        private readonly Color grayColor = new Color(0.7f, 0.7f, 0.7f);
        private bool hasError;
        private Rect iconBorderTextureRect;
        private Rect iconTextureRect;
        private Rect incomingConnectionTextureRect;
        [SerializeField]
        private bool isEntryDisplay;
        [SerializeField]
        private bool isParent;
        private bool mCacheIsDirty = true;
        [SerializeField]
        private bool mConnectionIsDirty;
        private int mIdentifyUpdateCount = -1;
        private Rect mIncomingRectangle;
        private bool mIncomingRectIsDirty = true;
        private Rect mOutgoingRectangle;
        private bool mOutgoingRectIsDirty = true;
        private Rect mRectangle;
        private bool mRectIsDirty = true;
        [SerializeField]
        private bool mSelected;
        [SerializeField]
        private BehaviorDesigner.Runtime.Tasks.Task mTask;
        private Rect nodeCollapsedTextureRect;
        private Rect outgoingConnectionTextureRect;
        [SerializeField]
        private List<NodeConnection> outgoingNodeConnections;
        [SerializeField]
        private NodeDesigner parentNodeDesigner;
        private int prevCommentLength = -1;
        private int prevFriendlyNameLength = -1;
        private bool prevRunningState;
        private int prevWatchedFieldsLength = -1;
        private Rect referenceTextureRect;
        private bool showHoverBar;
        [SerializeField]
        private bool showReferenceIcon;
        private Rect successExecutionStatusTextureRect;
        private Rect successReevaluatingExecutionStatusTextureRect;
        [SerializeField]
        private string taskName = string.Empty;
        private Rect titleRect;
        private Rect watchedFieldNamesRect;
        private Rect watchedFieldRect;
        private Rect watchedFieldValuesRect;

        public void AddChildNode(NodeDesigner childNodeDesigner, NodeConnection nodeConnection, bool adjustOffset, bool replaceNode)
        {
            this.AddChildNode(childNodeDesigner, nodeConnection, adjustOffset, replaceNode, -1);
        }

        public void AddChildNode(NodeDesigner childNodeDesigner, NodeConnection nodeConnection, bool adjustOffset, bool replaceNode, int replaceNodeIndex)
        {
            if (replaceNode)
            {
                ParentTask mTask = this.mTask as ParentTask;
                mTask.Children[replaceNodeIndex] = childNodeDesigner.Task;
            }
            else
            {
                if (!this.isEntryDisplay)
                {
                    ParentTask task2 = this.mTask as ParentTask;
                    int index = 0;
                    if (task2.Children != null)
                    {
                        index = 0;
                        while (index < task2.Children.Count)
                        {
                            if (childNodeDesigner.GetAbsolutePosition().x < (task2.Children[index].NodeData.NodeDesigner as NodeDesigner).GetAbsolutePosition().x)
                            {
                                break;
                            }
                            index++;
                        }
                    }
                    task2.AddChild(childNodeDesigner.Task, index);
                }
                if (adjustOffset)
                {
                    NodeData nodeData = childNodeDesigner.Task.NodeData;
                    nodeData.Offset -= this.GetAbsolutePosition();
                }
            }
            childNodeDesigner.ParentNodeDesigner = this;
            nodeConnection.DestinationNodeDesigner = childNodeDesigner;
            nodeConnection.NodeConnectionType = NodeConnectionType.Fixed;
            if (!nodeConnection.OriginatingNodeDesigner.Equals(this))
            {
                nodeConnection.OriginatingNodeDesigner = this;
            }
            this.outgoingNodeConnections.Add(nodeConnection);
            this.mConnectionIsDirty = true;
        }

        private void BringConnectionToFront(NodeDesigner nodeDesigner)
        {
            for (int i = 0; i < this.outgoingNodeConnections.Count; i++)
            {
                if (this.outgoingNodeConnections[i].DestinationNodeDesigner.Equals(nodeDesigner))
                {
                    NodeConnection connection = this.outgoingNodeConnections[i];
                    this.outgoingNodeConnections[i] = this.outgoingNodeConnections[this.outgoingNodeConnections.Count - 1];
                    this.outgoingNodeConnections[this.outgoingNodeConnections.Count - 1] = connection;
                    break;
                }
            }
        }

        private void CalculateNodeCommentRect(Rect nodeRect)
        {
            bool flag = false;
            if ((this.mTask.NodeData.WatchedFields != null) && (this.mTask.NodeData.WatchedFields.Count > 0))
            {
                float num2;
                float num3;
                float num4;
                string text = string.Empty;
                string str2 = string.Empty;
                for (int i = 0; i < this.mTask.NodeData.WatchedFields.Count; i++)
                {
                    FieldInfo info = this.mTask.NodeData.WatchedFields[i];
                    text = text + BehaviorDesignerUtility.SplitCamelCase(info.Name) + ": \n";
                    str2 = str2 + ((info.GetValue(this.mTask) == null) ? "null" : info.GetValue(this.mTask).ToString()) + "\n";
                }
                BehaviorDesignerUtility.TaskCommentGUIStyle.CalcMinMaxWidth(new GUIContent(text), out num2, out num3);
                BehaviorDesignerUtility.TaskCommentGUIStyle.CalcMinMaxWidth(new GUIContent(str2), out num2, out num4);
                float width = num3;
                float num6 = num4;
                float num7 = Mathf.Min((float) 220f, (float) ((num3 + num4) + 20f));
                if (num7 == 220f)
                {
                    width = (num3 / (num3 + num4)) * 220f;
                    num6 = (num4 / (num3 + num4)) * 220f;
                }
                this.watchedFieldRect = new Rect(nodeRect.xMax + 4f, nodeRect.y, num7 + 8f, nodeRect.height);
                this.watchedFieldNamesRect = new Rect(nodeRect.xMax + 6f, nodeRect.y + 4f, width, nodeRect.height - 8f);
                this.watchedFieldValuesRect = new Rect((nodeRect.xMax + 6f) + width, nodeRect.y + 4f, num6, nodeRect.height - 8f);
                flag = true;
            }
            if (!this.mTask.NodeData.Comment.Equals(string.Empty))
            {
                if (this.isParent)
                {
                    float num8;
                    float num9;
                    BehaviorDesignerUtility.TaskCommentGUIStyle.CalcMinMaxWidth(new GUIContent(this.mTask.NodeData.Comment), out num8, out num9);
                    float num10 = Mathf.Min((float) 220f, (float) (num9 + 20f));
                    if (flag)
                    {
                        this.commentRect = new Rect((nodeRect.xMin - 12f) - num10, nodeRect.y, num10 + 8f, nodeRect.height);
                        this.commentLabelRect = new Rect((nodeRect.xMin - 6f) - num10, nodeRect.y + 4f, num10, nodeRect.height - 8f);
                    }
                    else
                    {
                        this.commentRect = new Rect(nodeRect.xMax + 4f, nodeRect.y, num10 + 8f, nodeRect.height);
                        this.commentLabelRect = new Rect(nodeRect.xMax + 6f, nodeRect.y + 4f, num10, nodeRect.height - 8f);
                    }
                }
                else
                {
                    float height = Mathf.Min(100f, BehaviorDesignerUtility.TaskCommentGUIStyle.CalcHeight(new GUIContent(this.mTask.NodeData.Comment), nodeRect.width - 4f));
                    this.commentRect = new Rect(nodeRect.x, nodeRect.yMax + 4f, nodeRect.width, height + 4f);
                    this.commentLabelRect = new Rect(nodeRect.x, nodeRect.yMax + 4f, nodeRect.width - 4f, height);
                }
            }
        }

        public void ChangeOffset(Vector2 delta)
        {
            Vector2 vector = this.mTask.NodeData.Offset + delta;
            this.mTask.NodeData.Offset = vector;
            this.MarkDirty();
            if (this.parentNodeDesigner != null)
            {
                this.parentNodeDesigner.MarkDirty();
            }
        }

        public int ChildIndexForTask(BehaviorDesigner.Runtime.Tasks.Task childTask)
        {
            if (this.isParent)
            {
                ParentTask mTask = this.mTask as ParentTask;
                if (mTask.Children != null)
                {
                    for (int i = 0; i < mTask.Children.Count; i++)
                    {
                        if (mTask.Children[i].Equals(childTask))
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public void ConnectionContains(Vector2 point, Vector2 offset, ref List<NodeConnection> nodeConnections)
        {
            if ((this.outgoingNodeConnections != null) && !this.isEntryDisplay)
            {
                for (int i = 0; i < this.outgoingNodeConnections.Count; i++)
                {
                    if (this.outgoingNodeConnections[i].Contains(point, offset))
                    {
                        nodeConnections.Add(this.outgoingNodeConnections[i]);
                    }
                }
            }
        }

        public bool Contains(Vector2 point, Vector2 offset, bool includeConnections)
        {
            return this.Rectangle(offset, includeConnections, false).Contains(point);
        }

        public NodeConnection CreateNodeConnection(bool incomingNodeConnection)
        {
            NodeConnection connection = ScriptableObject.CreateInstance<NodeConnection>();
            connection.LoadConnection(this, !incomingNodeConnection ? NodeConnectionType.Outgoing : NodeConnectionType.Incoming);
            return connection;
        }

        public void Deselect()
        {
            this.mSelected = false;
        }

        public void DestroyConnections()
        {
            if (this.outgoingNodeConnections != null)
            {
                for (int i = this.outgoingNodeConnections.Count - 1; i > -1; i--)
                {
                    UnityEngine.Object.DestroyImmediate(this.outgoingNodeConnections[i], true);
                }
            }
        }

        private void DetermineConnectionHorizontalHeight(Rect nodeRect, Vector2 offset)
        {
            if (this.isParent)
            {
                float maxValue = float.MaxValue;
                float y = maxValue;
                for (int i = 0; i < this.outgoingNodeConnections.Count; i++)
                {
                    Rect rect = this.outgoingNodeConnections[i].DestinationNodeDesigner.Rectangle(offset, false, false);
                    if (rect.y < maxValue)
                    {
                        maxValue = rect.y;
                        y = rect.y;
                    }
                }
                maxValue = (maxValue * 0.75f) + (nodeRect.yMax * 0.25f);
                if (maxValue < (nodeRect.yMax + 15f))
                {
                    maxValue = nodeRect.yMax + 15f;
                }
                else if (maxValue > (y - 15f))
                {
                    maxValue = y - 15f;
                }
                for (int j = 0; j < this.outgoingNodeConnections.Count; j++)
                {
                    this.outgoingNodeConnections[j].HorizontalHeight = maxValue;
                }
            }
        }

        public bool DrawNode(Vector2 offset, bool drawSelected, bool disabled)
        {
            if (drawSelected != this.mSelected)
            {
                return false;
            }
            if (this.ToString().Length != this.prevFriendlyNameLength)
            {
                this.prevFriendlyNameLength = this.ToString().Length;
                this.mRectIsDirty = true;
            }
            Rect nodeRect = this.Rectangle(offset, false, false);
            this.UpdateCache(nodeRect);
            bool flag = ((this.mTask.NodeData.PushTime != -1f) && (this.mTask.NodeData.PushTime >= this.mTask.NodeData.PopTime)) || ((this.isEntryDisplay && (this.outgoingNodeConnections.Count > 0)) && !(this.outgoingNodeConnections[0].DestinationNodeDesigner.Task.NodeData.PushTime == -1f));
            bool flag2 = this.mIdentifyUpdateCount != -1;
            bool flag3 = this.prevRunningState != flag;
            float num = !BehaviorDesignerPreferences.GetBool(BDPreferences.FadeNodes) ? 0.01f : 0.5f;
            float num2 = 0f;
            if (flag2)
            {
                if ((0x7d0 - this.mIdentifyUpdateCount) < 500)
                {
                    num2 = ((float) (0x7d0 - this.mIdentifyUpdateCount)) / 500f;
                }
                else
                {
                    num2 = 1f;
                }
                if (this.mIdentifyUpdateCount != -1)
                {
                    this.mIdentifyUpdateCount++;
                    if (this.mIdentifyUpdateCount > 0x7d0)
                    {
                        this.mIdentifyUpdateCount = -1;
                    }
                }
                flag3 = true;
            }
            else if (flag)
            {
                num2 = 1f;
            }
            else if ((((this.mTask.NodeData.PopTime != -1f) && (num != 0f)) && ((Time.realtimeSinceStartup - this.mTask.NodeData.PopTime) < num)) || ((this.isEntryDisplay && (this.outgoingNodeConnections.Count > 0)) && ((this.outgoingNodeConnections[0].DestinationNodeDesigner.Task.NodeData.PopTime != -1f) && ((Time.realtimeSinceStartup - this.outgoingNodeConnections[0].DestinationNodeDesigner.Task.NodeData.PopTime) < num))))
            {
                if (this.isEntryDisplay)
                {
                    num2 = 1f - ((Time.realtimeSinceStartup - this.outgoingNodeConnections[0].DestinationNodeDesigner.Task.NodeData.PopTime) / num);
                }
                else
                {
                    num2 = 1f - ((Time.realtimeSinceStartup - this.mTask.NodeData.PopTime) / num);
                }
                flag3 = true;
            }
            if ((!this.isEntryDisplay && !this.prevRunningState) && (this.parentNodeDesigner != null))
            {
                this.parentNodeDesigner.BringConnectionToFront(this);
            }
            this.prevRunningState = flag;
            if (num2 != 1f)
            {
                GUI.color = (!disabled && !this.mTask.NodeData.Disabled) ? Color.white : this.grayColor;
                GUIStyle backgroundGUIStyle = null;
                if (BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode))
                {
                    backgroundGUIStyle = !this.mSelected ? BehaviorDesignerUtility.GetTaskCompactGUIStyle(this.mTask.NodeData.ColorIndex) : BehaviorDesignerUtility.GetTaskSelectedCompactGUIStyle(this.mTask.NodeData.ColorIndex);
                }
                else
                {
                    backgroundGUIStyle = !this.mSelected ? BehaviorDesignerUtility.GetTaskGUIStyle(this.mTask.NodeData.ColorIndex) : BehaviorDesignerUtility.GetTaskSelectedGUIStyle(this.mTask.NodeData.ColorIndex);
                }
                this.DrawNodeTexture(nodeRect, BehaviorDesignerUtility.GetTaskConnectionTopTexture(this.mTask.NodeData.ColorIndex), BehaviorDesignerUtility.GetTaskConnectionBottomTexture(this.mTask.NodeData.ColorIndex), backgroundGUIStyle, BehaviorDesignerUtility.GetTaskBorderTexture(this.mTask.NodeData.ColorIndex));
            }
            if (num2 > 0f)
            {
                GUIStyle taskIdentifySelectedCompactGUIStyle = null;
                Texture2D iconBorderTexture = null;
                if (flag2)
                {
                    if (BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode))
                    {
                        if (this.mSelected)
                        {
                            taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskIdentifySelectedCompactGUIStyle;
                        }
                        else
                        {
                            taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskIdentifyCompactGUIStyle;
                        }
                    }
                    else if (this.mSelected)
                    {
                        taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskIdentifySelectedGUIStyle;
                    }
                    else
                    {
                        taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskIdentifyGUIStyle;
                    }
                    iconBorderTexture = BehaviorDesignerUtility.TaskBorderIdentifyTexture;
                }
                else
                {
                    if (BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode))
                    {
                        if (this.mSelected)
                        {
                            taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskRunningSelectedCompactGUIStyle;
                        }
                        else
                        {
                            taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskRunningCompactGUIStyle;
                        }
                    }
                    else if (this.mSelected)
                    {
                        taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskRunningSelectedGUIStyle;
                    }
                    else
                    {
                        taskIdentifySelectedCompactGUIStyle = BehaviorDesignerUtility.TaskRunningGUIStyle;
                    }
                    iconBorderTexture = BehaviorDesignerUtility.TaskBorderRunningTexture;
                }
                Color color = (!disabled && !this.mTask.NodeData.Disabled) ? Color.white : this.grayColor;
                color.a = num2;
                GUI.color = color;
                Texture2D connectionTopTexture = null;
                Texture2D connectionBottomTexture = null;
                if (!this.isEntryDisplay)
                {
                    if (flag2)
                    {
                        connectionTopTexture = BehaviorDesignerUtility.TaskConnectionIdentifyTopTexture;
                    }
                    else
                    {
                        connectionTopTexture = BehaviorDesignerUtility.TaskConnectionRunningTopTexture;
                    }
                }
                if (this.isParent)
                {
                    if (flag2)
                    {
                        connectionBottomTexture = BehaviorDesignerUtility.TaskConnectionIdentifyBottomTexture;
                    }
                    else
                    {
                        connectionBottomTexture = BehaviorDesignerUtility.TaskConnectionRunningBottomTexture;
                    }
                }
                this.DrawNodeTexture(nodeRect, connectionTopTexture, connectionBottomTexture, taskIdentifySelectedCompactGUIStyle, iconBorderTexture);
                GUI.color = Color.white;
            }
            if (this.mTask.NodeData.Collapsed)
            {
                GUI.DrawTexture(this.nodeCollapsedTextureRect, BehaviorDesignerUtility.TaskConnectionCollapsedTexture);
            }
            if (!BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode))
            {
                GUI.DrawTexture(this.iconTextureRect, this.mTask.NodeData.Icon);
            }
            if ((this.mTask.NodeData.InterruptTime != -1f) && ((Time.realtimeSinceStartup - this.mTask.NodeData.InterruptTime) < (0.75f + num)))
            {
                float num3;
                if ((Time.realtimeSinceStartup - this.mTask.NodeData.InterruptTime) < 0.75f)
                {
                    num3 = 1f;
                }
                else
                {
                    num3 = 1f - ((Time.realtimeSinceStartup - (this.mTask.NodeData.InterruptTime + 0.75f)) / num);
                }
                Color white = Color.white;
                white.a = num3;
                GUI.color = white;
                GUI.Label(nodeRect, string.Empty, BehaviorDesignerUtility.TaskHighlightGUIStyle);
                GUI.color = Color.white;
            }
            GUI.Label(this.titleRect, this.ToString(), BehaviorDesignerUtility.TaskTitleGUIStyle);
            if (this.mTask.NodeData.IsBreakpoint)
            {
                GUI.DrawTexture(this.breakpointTextureRect, BehaviorDesignerUtility.BreakpointTexture);
            }
            if (this.showReferenceIcon)
            {
                GUI.DrawTexture(this.referenceTextureRect, BehaviorDesignerUtility.ReferencedTexture);
            }
            if (this.hasError)
            {
                GUI.DrawTexture(this.errorTextureRect, BehaviorDesignerUtility.ErrorIconTexture);
            }
            if ((this.mTask is Composite) && ((this.mTask as Composite).AbortType != AbortType.None))
            {
                switch ((this.mTask as Composite).AbortType)
                {
                    case AbortType.Self:
                        GUI.DrawTexture(this.conditionalAbortTextureRect, BehaviorDesignerUtility.ConditionalAbortSelfTexture);
                        break;

                    case AbortType.LowerPriority:
                        GUI.DrawTexture(this.conditionalAbortLowerPriorityTextureRect, BehaviorDesignerUtility.ConditionalAbortLowerPriorityTexture);
                        break;

                    case AbortType.Both:
                        GUI.DrawTexture(this.conditionalAbortTextureRect, BehaviorDesignerUtility.ConditionalAbortBothTexture);
                        break;
                }
            }
            GUI.color = Color.white;
            if (this.showHoverBar)
            {
                GUI.DrawTexture(this.disabledButtonTextureRect, !this.mTask.NodeData.Disabled ? BehaviorDesignerUtility.DisableTaskTexture : BehaviorDesignerUtility.EnableTaskTexture, ScaleMode.ScaleToFit);
                if (!this.isParent && !(this.mTask is BehaviorReference))
                {
                    return flag3;
                }
                bool collapsed = this.mTask.NodeData.Collapsed;
                if (this.mTask is BehaviorReference)
                {
                    collapsed = (this.mTask as BehaviorReference).collapsed;
                }
                GUI.DrawTexture(this.collapseButtonTextureRect, !collapsed ? BehaviorDesignerUtility.CollapseTaskTexture : BehaviorDesignerUtility.ExpandTaskTexture, ScaleMode.ScaleToFit);
            }
            return flag3;
        }

        public void DrawNodeComment(Vector2 offset)
        {
            if (this.mTask.NodeData.Comment.Length != this.prevCommentLength)
            {
                this.prevCommentLength = this.mTask.NodeData.Comment.Length;
                this.mRectIsDirty = true;
            }
            if ((this.mTask.NodeData.WatchedFields != null) && (this.mTask.NodeData.WatchedFields.Count != this.prevWatchedFieldsLength))
            {
                this.prevWatchedFieldsLength = this.mTask.NodeData.WatchedFields.Count;
                this.mRectIsDirty = true;
            }
            if (!this.mTask.NodeData.Comment.Equals(string.Empty) || ((this.mTask.NodeData.WatchedFields != null) && (this.mTask.NodeData.WatchedFields.Count != 0)))
            {
                if ((this.mTask.NodeData.WatchedFields != null) && (this.mTask.NodeData.WatchedFields.Count > 0))
                {
                    string text = string.Empty;
                    string str2 = string.Empty;
                    for (int i = 0; i < this.mTask.NodeData.WatchedFields.Count; i++)
                    {
                        FieldInfo info = this.mTask.NodeData.WatchedFields[i];
                        text = text + BehaviorDesignerUtility.SplitCamelCase(info.Name) + ": \n";
                        str2 = str2 + ((info.GetValue(this.mTask) == null) ? "null" : info.GetValue(this.mTask).ToString()) + "\n";
                    }
                    GUI.Box(this.watchedFieldRect, string.Empty, BehaviorDesignerUtility.TaskDescriptionGUIStyle);
                    GUI.Label(this.watchedFieldNamesRect, text, BehaviorDesignerUtility.TaskCommentRightAlignGUIStyle);
                    GUI.Label(this.watchedFieldValuesRect, str2, BehaviorDesignerUtility.TaskCommentLeftAlignGUIStyle);
                }
                if (!this.mTask.NodeData.Comment.Equals(string.Empty))
                {
                    GUI.Box(this.commentRect, string.Empty, BehaviorDesignerUtility.TaskDescriptionGUIStyle);
                    GUI.Label(this.commentLabelRect, this.mTask.NodeData.Comment, BehaviorDesignerUtility.TaskCommentGUIStyle);
                }
            }
        }

        public void DrawNodeConnection(Vector2 offset, bool disabled)
        {
            if (this.mConnectionIsDirty)
            {
                this.DetermineConnectionHorizontalHeight(this.Rectangle(offset, false, false), offset);
                this.mConnectionIsDirty = false;
            }
            if (this.isParent)
            {
                for (int i = 0; i < this.outgoingNodeConnections.Count; i++)
                {
                    this.outgoingNodeConnections[i].DrawConnection(offset, disabled);
                }
            }
        }

        private void DrawNodeTexture(Rect nodeRect, Texture2D connectionTopTexture, Texture2D connectionBottomTexture, GUIStyle backgroundGUIStyle, Texture2D iconBorderTexture)
        {
            if (!this.isEntryDisplay)
            {
                GUI.DrawTexture(this.incomingConnectionTextureRect, connectionTopTexture, ScaleMode.ScaleToFit);
            }
            if (this.isParent)
            {
                GUI.DrawTexture(this.outgoingConnectionTextureRect, connectionBottomTexture, ScaleMode.ScaleToFit);
            }
            GUI.Label(nodeRect, string.Empty, backgroundGUIStyle);
            if (this.mTask.NodeData.ExecutionStatus == TaskStatus.Success)
            {
                if (this.mTask.NodeData.IsReevaluating)
                {
                    GUI.DrawTexture(this.successReevaluatingExecutionStatusTextureRect, BehaviorDesignerUtility.ExecutionSuccessRepeatTexture);
                }
                else
                {
                    GUI.DrawTexture(this.successExecutionStatusTextureRect, BehaviorDesignerUtility.ExecutionSuccessTexture);
                }
            }
            else if (this.mTask.NodeData.ExecutionStatus == TaskStatus.Failure)
            {
                GUI.DrawTexture(this.failureExecutionStatusTextureRect, !this.mTask.NodeData.IsReevaluating ? BehaviorDesignerUtility.ExecutionFailureTexture : BehaviorDesignerUtility.ExecutionFailureRepeatTexture);
            }
            if (!BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode))
            {
                GUI.DrawTexture(this.iconBorderTextureRect, iconBorderTexture);
            }
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }

        public Vector2 GetAbsolutePosition()
        {
            Vector2 offset = this.mTask.NodeData.Offset;
            if (this.parentNodeDesigner != null)
            {
                offset += this.parentNodeDesigner.GetAbsolutePosition();
            }
            if (BehaviorDesignerPreferences.GetBool(BDPreferences.SnapToGrid))
            {
                float newX = BehaviorDesignerUtility.RoundToNearest(offset.x, 10f);
                offset.Set(newX, BehaviorDesignerUtility.RoundToNearest(offset.y, 10f));
            }
            return offset;
        }

        public Vector2 GetConnectionPosition(Vector2 offset, NodeConnectionType connectionType)
        {
            if (connectionType == NodeConnectionType.Incoming)
            {
                Rect rect = this.IncomingConnectionRect(offset);
                return new Vector2(rect.center.x, rect.y + 7f);
            }
            Rect rect2 = this.OutgoingConnectionRect(offset);
            return new Vector2(rect2.center.x, rect2.yMax - 8f);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool HasParent(NodeDesigner nodeDesigner)
        {
            if (this.parentNodeDesigner == null)
            {
                return false;
            }
            return (this.parentNodeDesigner.Equals(nodeDesigner) || this.parentNodeDesigner.HasParent(nodeDesigner));
        }

        public bool HoverBarAreaContains(Vector2 point, Vector2 offset)
        {
            Rect rect = this.Rectangle(offset, false, false);
            rect.y -= 24f;
            return rect.Contains(point);
        }

        public bool HoverBarButtonClick(Vector2 point, Vector2 offset, ref bool collapsedButtonClicked)
        {
            Rect rect = this.Rectangle(offset, false, false);
            Rect rect2 = new Rect(rect.x - 1f, rect.y - 17f, 14f, 14f);
            Rect rect3 = rect2;
            bool flag = false;
            if (rect2.Contains(point))
            {
                this.mTask.NodeData.Disabled = !this.mTask.NodeData.Disabled;
                flag = true;
            }
            if (!flag && (this.isParent || (this.mTask is BehaviorReference)))
            {
                Rect rect4 = new Rect(rect.x + 15f, rect.y - 17f, 14f, 14f);
                rect3.xMax = rect4.xMax;
                if (rect4.Contains(point))
                {
                    if (this.mTask is BehaviorReference)
                    {
                        (this.mTask as BehaviorReference).collapsed = !(this.mTask as BehaviorReference).collapsed;
                    }
                    else
                    {
                        this.mTask.NodeData.Collapsed = !this.mTask.NodeData.Collapsed;
                    }
                    collapsedButtonClicked = true;
                    flag = true;
                }
            }
            if (!flag && rect3.Contains(point))
            {
                flag = true;
            }
            return flag;
        }

        public void IdentifyNode()
        {
            this.mIdentifyUpdateCount = 0;
        }

        public Rect IncomingConnectionRect(Vector2 offset)
        {
            if (this.mIncomingRectIsDirty)
            {
                Rect rect = this.Rectangle(offset, false, false);
                this.mIncomingRectangle = new Rect(rect.x + ((rect.width - 42f) / 2f), rect.y - 14f, 42f, 14f);
                this.mIncomingRectIsDirty = false;
            }
            return this.mIncomingRectangle;
        }

        private void Init()
        {
            this.taskName = BehaviorDesignerUtility.SplitCamelCase(this.mTask.GetType().Name.ToString());
            this.isParent = this.mTask.GetType().IsSubclassOf(typeof(ParentTask));
            if (this.isParent)
            {
                this.outgoingNodeConnections = new List<NodeConnection>();
            }
            this.mRectIsDirty = this.mCacheIsDirty = true;
            this.mIncomingRectIsDirty = true;
            this.mOutgoingRectIsDirty = true;
        }

        public bool Intersects(Rect rect, Vector2 offset)
        {
            Rect rect2 = this.Rectangle(offset, false, false);
            return ((((rect2.xMin < rect.xMax) && (rect2.xMax > rect.xMin)) && (rect2.yMin < rect.yMax)) && (rect2.yMax > rect.yMin));
        }

        public bool IsDisabled()
        {
            return (this.mTask.NodeData.Disabled || ((this.parentNodeDesigner != null) && this.parentNodeDesigner.IsDisabled()));
        }

        public void LoadNode(BehaviorDesigner.Runtime.Tasks.Task task, BehaviorSource behaviorSource, Vector2 offset, ref int id)
        {
            RequiredComponentAttribute[] attributeArray;
            this.mTask = task;
            this.mTask.Owner = behaviorSource.Owner as Behavior;
            this.mTask.ID = id++;
            this.mTask.NodeData = new NodeData();
            this.mTask.NodeData.Offset = offset;
            this.mTask.NodeData.NodeDesigner = this;
            this.LoadTaskIcon();
            this.Init();
            this.mTask.FriendlyName = this.taskName;
            if ((this.mTask.Owner != null) && ((attributeArray = this.mTask.GetType().GetCustomAttributes(typeof(RequiredComponentAttribute), true) as RequiredComponentAttribute[]).Length > 0))
            {
                System.Type componentType = attributeArray[0].ComponentType;
                if (typeof(Component).IsAssignableFrom(componentType) && (this.mTask.Owner.gameObject.GetComponent(componentType) == null))
                {
                    this.mTask.Owner.gameObject.AddComponent(componentType);
                }
            }
            List<System.Type> baseClasses = FieldInspector.GetBaseClasses(this.mTask.GetType());
            BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (int i = baseClasses.Count - 1; i > -1; i--)
            {
                FieldInfo[] fields = baseClasses[i].GetFields(bindingAttr);
                for (int j = 0; j < fields.Length; j++)
                {
                    if (typeof(SharedVariable).IsAssignableFrom(fields[j].FieldType) && !fields[j].FieldType.IsAbstract)
                    {
                        SharedVariable variable = fields[j].GetValue(this.mTask) as SharedVariable;
                        if (variable == null)
                        {
                            variable = Activator.CreateInstance(fields[j].FieldType) as SharedVariable;
                        }
                        if (TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) || TaskUtility.HasAttribute(fields[j], typeof(SharedRequiredAttribute)))
                        {
                            variable.IsShared = true;
                        }
                        fields[j].SetValue(this.mTask, variable);
                    }
                }
            }
        }

        public void LoadTask(BehaviorDesigner.Runtime.Tasks.Task task, Behavior owner, ref int id)
        {
            if (task != null)
            {
                RequiredComponentAttribute[] attributeArray;
                this.mTask = task;
                this.mTask.Owner = owner;
                this.mTask.ID = id++;
                this.mTask.NodeData.NodeDesigner = this;
                this.mTask.NodeData.InitWatchedFields(this.mTask);
                if (!this.mTask.NodeData.FriendlyName.Equals(string.Empty))
                {
                    this.mTask.FriendlyName = this.mTask.NodeData.FriendlyName;
                    this.mTask.NodeData.FriendlyName = string.Empty;
                }
                this.LoadTaskIcon();
                this.Init();
                if ((this.mTask.Owner != null) && ((attributeArray = this.mTask.GetType().GetCustomAttributes(typeof(RequiredComponentAttribute), true) as RequiredComponentAttribute[]).Length > 0))
                {
                    System.Type componentType = attributeArray[0].ComponentType;
                    if (typeof(Component).IsAssignableFrom(componentType) && (this.mTask.Owner.gameObject.GetComponent(componentType) == null))
                    {
                        this.mTask.Owner.gameObject.AddComponent(componentType);
                    }
                }
                List<System.Type> baseClasses = FieldInspector.GetBaseClasses(this.mTask.GetType());
                BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                for (int i = baseClasses.Count - 1; i > -1; i--)
                {
                    FieldInfo[] fields = baseClasses[i].GetFields(bindingAttr);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        if (typeof(SharedVariable).IsAssignableFrom(fields[j].FieldType) && !fields[j].FieldType.IsAbstract)
                        {
                            SharedVariable variable = fields[j].GetValue(this.mTask) as SharedVariable;
                            if (variable == null)
                            {
                                variable = Activator.CreateInstance(fields[j].FieldType) as SharedVariable;
                            }
                            if (TaskUtility.HasAttribute(fields[j], typeof(RequiredFieldAttribute)) || TaskUtility.HasAttribute(fields[j], typeof(SharedRequiredAttribute)))
                            {
                                variable.IsShared = true;
                            }
                            fields[j].SetValue(this.mTask, variable);
                        }
                    }
                }
                if (this.isParent)
                {
                    ParentTask mTask = this.mTask as ParentTask;
                    if (mTask.Children != null)
                    {
                        for (int k = 0; k < mTask.Children.Count; k++)
                        {
                            NodeDesigner childNodeDesigner = ScriptableObject.CreateInstance<NodeDesigner>();
                            childNodeDesigner.LoadTask(mTask.Children[k], owner, ref id);
                            NodeConnection nodeConnection = ScriptableObject.CreateInstance<NodeConnection>();
                            nodeConnection.LoadConnection(this, NodeConnectionType.Fixed);
                            this.AddChildNode(childNodeDesigner, nodeConnection, true, true, k);
                        }
                    }
                    this.mConnectionIsDirty = true;
                }
            }
        }

        private void LoadTaskIcon()
        {
            TaskIconAttribute[] attributeArray = null;
            this.mTask.NodeData.Icon = null;
            if ((attributeArray = this.mTask.GetType().GetCustomAttributes(typeof(TaskIconAttribute), false) as TaskIconAttribute[]).Length > 0)
            {
                this.mTask.NodeData.Icon = BehaviorDesignerUtility.LoadIcon(attributeArray[0].IconPath, null);
            }
            if (this.mTask.NodeData.Icon == null)
            {
                string iconName = string.Empty;
                if (this.mTask.GetType().IsSubclassOf(typeof(BehaviorDesigner.Runtime.Tasks.Action)))
                {
                    iconName = "{SkinColor}ActionIcon.png";
                }
                else if (this.mTask.GetType().IsSubclassOf(typeof(Conditional)))
                {
                    iconName = "{SkinColor}ConditionalIcon.png";
                }
                else if (this.mTask.GetType().IsSubclassOf(typeof(Composite)))
                {
                    iconName = "{SkinColor}CompositeIcon.png";
                }
                else if (this.mTask.GetType().IsSubclassOf(typeof(Decorator)))
                {
                    iconName = "{SkinColor}DecoratorIcon.png";
                }
                else
                {
                    iconName = "{SkinColor}EntryIcon.png";
                }
                this.mTask.NodeData.Icon = BehaviorDesignerUtility.LoadIcon(iconName, null);
            }
        }

        public void MakeEntryDisplay()
        {
            this.isEntryDisplay = this.isParent = true;
            this.mTask.FriendlyName = this.taskName = "Entry";
            this.outgoingNodeConnections = new List<NodeConnection>();
        }

        public void MarkDirty()
        {
            this.mConnectionIsDirty = true;
            this.mRectIsDirty = true;
            this.mIncomingRectIsDirty = true;
            this.mOutgoingRectIsDirty = true;
        }

        public void MoveChildNode(int index, bool decreaseIndex)
        {
            int num = index + (!decreaseIndex ? 1 : -1);
            ParentTask mTask = this.mTask as ParentTask;
            BehaviorDesigner.Runtime.Tasks.Task task2 = mTask.Children[index];
            mTask.Children[index] = mTask.Children[num];
            mTask.Children[num] = task2;
        }

        public NodeConnection NodeConnectionRectContains(Vector2 point, Vector2 offset)
        {
            bool incomingNodeConnection = false;
            if (!(incomingNodeConnection = this.IncomingConnectionRect(offset).Contains(point)) && (!this.isParent || !this.OutgoingConnectionRect(offset).Contains(point)))
            {
                return null;
            }
            return this.CreateNodeConnection(incomingNodeConnection);
        }

        public NodeDesigner NodeDesignerForChildIndex(int index)
        {
            if ((index >= 0) && this.isParent)
            {
                ParentTask mTask = this.mTask as ParentTask;
                if (mTask.Children != null)
                {
                    if ((index < mTask.Children.Count) && (mTask.Children[index] != null))
                    {
                        return (mTask.Children[index].NodeData.NodeDesigner as NodeDesigner);
                    }
                    return null;
                }
            }
            return null;
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        public Rect OutgoingConnectionRect(Vector2 offset)
        {
            if (this.mOutgoingRectIsDirty)
            {
                Rect rect = this.Rectangle(offset, false, false);
                this.mOutgoingRectangle = new Rect(rect.x + ((rect.width - 42f) / 2f), rect.yMax, 42f, 16f);
                this.mOutgoingRectIsDirty = false;
            }
            return this.mOutgoingRectangle;
        }

        private Rect Rectangle(Vector2 offset)
        {
            if (this.mRectIsDirty)
            {
                this.mCacheIsDirty = true;
                if (this.mTask == null)
                {
                    return new Rect();
                }
                float b = BehaviorDesignerUtility.TaskTitleGUIStyle.CalcSize(new GUIContent(this.ToString())).x + 20f;
                if (!this.isParent)
                {
                    float num2;
                    float num3;
                    BehaviorDesignerUtility.TaskCommentGUIStyle.CalcMinMaxWidth(new GUIContent(this.mTask.NodeData.Comment), out num2, out num3);
                    num3 += 20f;
                    b = (b <= num3) ? num3 : b;
                }
                b = Mathf.Min(220f, Mathf.Max(100f, b));
                Vector2 absolutePosition = this.GetAbsolutePosition();
                float height = 20 + (!BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode) ? 0x34 : 0x16);
                this.mRectangle = new Rect((absolutePosition.x + offset.x) - (b / 2f), absolutePosition.y + offset.y, b, height);
                this.mRectIsDirty = false;
            }
            return this.mRectangle;
        }

        public Rect Rectangle(Vector2 offset, bool includeConnections, bool includeComments)
        {
            Rect rect = this.Rectangle(offset);
            if (includeConnections)
            {
                if (!this.isEntryDisplay)
                {
                    rect.yMin -= 14f;
                }
                if (this.isParent)
                {
                    rect.yMax += 16f;
                }
            }
            if (includeComments && (this.mTask != null))
            {
                if (((this.mTask.NodeData.WatchedFields != null) && (this.mTask.NodeData.WatchedFields.Count > 0)) && (rect.xMax < this.watchedFieldRect.xMax))
                {
                    rect.xMax = this.watchedFieldRect.xMax;
                }
                if (this.mTask.NodeData.Comment.Equals(string.Empty))
                {
                    return rect;
                }
                if (rect.xMax < this.commentRect.xMax)
                {
                    rect.xMax = this.commentRect.xMax;
                }
                if (rect.yMax < this.commentRect.yMax)
                {
                    rect.yMax = this.commentRect.yMax;
                }
            }
            return rect;
        }

        public void RemoveChildNode(NodeDesigner childNodeDesigner)
        {
            if (!this.isEntryDisplay)
            {
                ParentTask mTask = this.mTask as ParentTask;
                mTask.Children.Remove(childNodeDesigner.Task);
            }
            for (int i = 0; i < this.outgoingNodeConnections.Count; i++)
            {
                NodeConnection connection = this.outgoingNodeConnections[i];
                if (connection.DestinationNodeDesigner.Equals(childNodeDesigner) || connection.OriginatingNodeDesigner.Equals(childNodeDesigner))
                {
                    this.outgoingNodeConnections.RemoveAt(i);
                    break;
                }
            }
            childNodeDesigner.ParentNodeDesigner = null;
            this.mConnectionIsDirty = true;
        }

        public void Select()
        {
            if (!this.isEntryDisplay)
            {
                this.mSelected = true;
            }
        }

        public void SetID(ref int id)
        {
            this.mTask.ID = id++;
            if (this.isParent)
            {
                ParentTask mTask = this.mTask as ParentTask;
                if (mTask.Children != null)
                {
                    for (int i = 0; i < mTask.Children.Count; i++)
                    {
                        (mTask.Children[i].NodeData.NodeDesigner as NodeDesigner).SetID(ref id);
                    }
                }
            }
        }

        public void ToggleBreakpoint()
        {
            this.mTask.NodeData.IsBreakpoint = !this.Task.NodeData.IsBreakpoint;
        }

        public bool ToggleCollapseState()
        {
            this.mTask.NodeData.Collapsed = !this.Task.NodeData.Collapsed;
            return this.mTask.NodeData.Collapsed;
        }

        public void ToggleEnableState()
        {
            this.mTask.NodeData.Disabled = !this.Task.NodeData.Disabled;
        }

        public override string ToString()
        {
            return ((this.mTask != null) ? (!this.mTask.FriendlyName.Equals(string.Empty) ? this.mTask.FriendlyName : this.taskName) : string.Empty);
        }

        private void UpdateCache(Rect nodeRect)
        {
            if (this.mCacheIsDirty)
            {
                this.nodeCollapsedTextureRect = new Rect((nodeRect.x + ((nodeRect.width - 26f) / 2f)) + 1f, nodeRect.yMax + 2f, 26f, 6f);
                this.iconTextureRect = new Rect(nodeRect.x + ((nodeRect.width - 44f) / 2f), (nodeRect.y + 4f) + 2f, 44f, 44f);
                this.titleRect = new Rect(nodeRect.x, (nodeRect.yMax - (!BehaviorDesignerPreferences.GetBool(BDPreferences.CompactMode) ? ((float) 20) : ((float) 0x1c))) - 1f, nodeRect.width, 20f);
                this.breakpointTextureRect = new Rect(nodeRect.xMax - 16f, nodeRect.y + 3f, 14f, 14f);
                this.errorTextureRect = new Rect(nodeRect.xMax - 12f, nodeRect.y - 8f, 20f, 20f);
                this.referenceTextureRect = new Rect(nodeRect.x + 2f, nodeRect.y + 3f, 14f, 14f);
                this.conditionalAbortTextureRect = new Rect(nodeRect.x + 3f, nodeRect.y + 3f, 16f, 16f);
                this.conditionalAbortLowerPriorityTextureRect = new Rect(nodeRect.x + 3f, nodeRect.y, 16f, 16f);
                this.disabledButtonTextureRect = new Rect(nodeRect.x - 1f, nodeRect.y - 17f, 14f, 14f);
                this.collapseButtonTextureRect = new Rect(nodeRect.x + 15f, nodeRect.y - 17f, 14f, 14f);
                this.incomingConnectionTextureRect = new Rect(nodeRect.x + ((nodeRect.width - 42f) / 2f), ((nodeRect.y - 14f) - 3f) + 3f, 42f, 17f);
                this.outgoingConnectionTextureRect = new Rect(nodeRect.x + ((nodeRect.width - 42f) / 2f), nodeRect.yMax - 3f, 42f, 19f);
                this.successReevaluatingExecutionStatusTextureRect = new Rect(nodeRect.xMax - 37f, nodeRect.yMax - 38f, 35f, 36f);
                this.successExecutionStatusTextureRect = new Rect(nodeRect.xMax - 37f, nodeRect.yMax - 33f, 35f, 31f);
                this.failureExecutionStatusTextureRect = new Rect(nodeRect.xMax - 37f, nodeRect.yMax - 38f, 35f, 36f);
                this.iconBorderTextureRect = new Rect(nodeRect.x + ((nodeRect.width - 46f) / 2f), (nodeRect.y + 3f) + 2f, 46f, 46f);
                this.CalculateNodeCommentRect(nodeRect);
                this.mCacheIsDirty = false;
            }
        }

        public bool HasError
        {
            set
            {
                this.hasError = value;
            }
        }

        public bool IsEntryDisplay
        {
            get
            {
                return this.isEntryDisplay;
            }
        }

        public bool IsParent
        {
            get
            {
                return this.isParent;
            }
        }

        public List<NodeConnection> OutgoingNodeConnections
        {
            get
            {
                return this.outgoingNodeConnections;
            }
        }

        public NodeDesigner ParentNodeDesigner
        {
            get
            {
                return this.parentNodeDesigner;
            }
            set
            {
                this.parentNodeDesigner = value;
            }
        }

        public bool ShowHoverBar
        {
            get
            {
                return this.showHoverBar;
            }
            set
            {
                this.showHoverBar = value;
            }
        }

        public bool ShowReferenceIcon
        {
            set
            {
                this.showReferenceIcon = value;
            }
        }

        public BehaviorDesigner.Runtime.Tasks.Task Task
        {
            get
            {
                return this.mTask;
            }
            set
            {
                this.mTask = value;
                this.Init();
            }
        }
    }
}

