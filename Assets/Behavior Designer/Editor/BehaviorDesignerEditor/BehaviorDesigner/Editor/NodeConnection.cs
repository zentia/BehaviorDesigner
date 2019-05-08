namespace BehaviorDesigner.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class NodeConnection : ScriptableObject
    {
        [SerializeField]
        private NodeDesigner destinationNodeDesigner;
        private Vector2 endHorizontalBreak;
        private bool horizontalDirty = true;
        [SerializeField]
        private float horizontalHeight;
        private Vector3[] linePoints = new Vector3[4];
        [SerializeField]
        private BehaviorDesigner.Editor.NodeConnectionType nodeConnectionType;
        [SerializeField]
        private NodeDesigner originatingNodeDesigner;
        [SerializeField]
        private bool selected;
        private readonly Color selectedDisabledProColor = new Color(0.1316f, 0.3212f, 0.4803f);
        private readonly Color selectedDisabledStandardColor = new Color(0.1701f, 0.3982f, 0.5873f);
        private readonly Color selectedEnabledProColor = new Color(0.188f, 0.4588f, 0.6862f);
        private readonly Color selectedEnabledStandardColor = new Color(0.243f, 0.5686f, 0.839f);
        private Vector2 startHorizontalBreak;
        private readonly Color taskRunningProColor = new Color(0f, 0.698f, 0.4f);
        private readonly Color taskRunningStandardColor = new Color(0f, 1f, 0.2784f);

        public bool Contains(Vector2 point, Vector2 offset)
        {
            Vector2 center = this.originatingNodeDesigner.OutgoingConnectionRect(offset).center;
            Vector2 vector2 = new Vector2(center.x, this.horizontalHeight);
            if ((Mathf.Abs((float) (point.x - center.x)) >= 7f) || (((point.y < center.y) || (point.y > vector2.y)) && ((point.y > center.y) || (point.y < vector2.y))))
            {
                Rect rect = this.destinationNodeDesigner.IncomingConnectionRect(offset);
                Vector2 vector3 = new Vector2(rect.center.x, rect.y);
                Vector2 vector4 = new Vector2(vector3.x, this.horizontalHeight);
                if ((Mathf.Abs((float) (point.y - this.horizontalHeight)) < 7f) && (((point.x <= center.x) && (point.x >= vector4.x)) || ((point.x >= center.x) && (point.x <= vector4.x))))
                {
                    return true;
                }
                if ((Mathf.Abs((float) (point.x - vector3.x)) >= 7f) || (((point.y < vector3.y) || (point.y > vector4.y)) && ((point.y > vector3.y) || (point.y < vector4.y))))
                {
                    return false;
                }
            }
            return true;
        }

        public void deselect()
        {
            this.selected = false;
        }

        public void DrawConnection(Vector2 offset, bool disabled)
        {
            this.DrawConnection(this.OriginatingNodeDesigner.GetConnectionPosition(offset, BehaviorDesigner.Editor.NodeConnectionType.Outgoing), this.DestinationNodeDesigner.GetConnectionPosition(offset, BehaviorDesigner.Editor.NodeConnectionType.Incoming), disabled);
        }

        public void DrawConnection(Vector2 source, Vector2 destination, bool disabled)
        {
            Color selectedDisabledProColor = !disabled ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            bool flag = (((this.destinationNodeDesigner != null) && (this.destinationNodeDesigner.Task != null)) && (this.destinationNodeDesigner.Task.NodeData.PushTime != -1f)) && (this.destinationNodeDesigner.Task.NodeData.PushTime >= this.destinationNodeDesigner.Task.NodeData.PopTime);
            float num = !BehaviorDesignerPreferences.GetBool(BDPreferences.FadeNodes) ? 0.01f : 0.5f;
            if (this.selected)
            {
                if (disabled)
                {
                    if (EditorGUIUtility.isProSkin)
                    {
                        selectedDisabledProColor = this.selectedDisabledProColor;
                    }
                    else
                    {
                        selectedDisabledProColor = this.selectedDisabledStandardColor;
                    }
                }
                else if (EditorGUIUtility.isProSkin)
                {
                    selectedDisabledProColor = this.selectedEnabledProColor;
                }
                else
                {
                    selectedDisabledProColor = this.selectedEnabledStandardColor;
                }
            }
            else if (flag)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    selectedDisabledProColor = this.taskRunningProColor;
                }
                else
                {
                    selectedDisabledProColor = this.taskRunningStandardColor;
                }
            }
            else if ((((num != 0f) && (this.destinationNodeDesigner != null)) && ((this.destinationNodeDesigner.Task != null) && (this.destinationNodeDesigner.Task.NodeData.PopTime != -1f))) && ((Time.realtimeSinceStartup - this.destinationNodeDesigner.Task.NodeData.PopTime) < num))
            {
                float t = 1f - ((Time.realtimeSinceStartup - this.destinationNodeDesigner.Task.NodeData.PopTime) / num);
                Color white = Color.white;
                if (EditorGUIUtility.isProSkin)
                {
                    white = this.taskRunningProColor;
                }
                else
                {
                    white = this.taskRunningStandardColor;
                }
                selectedDisabledProColor = Color.Lerp(Color.white, white, t);
            }
            Handles.color = selectedDisabledProColor;
            if (this.horizontalDirty)
            {
                this.startHorizontalBreak = new Vector2(source.x, this.horizontalHeight);
                this.endHorizontalBreak = new Vector2(destination.x, this.horizontalHeight);
                this.horizontalDirty = false;
            }
            this.linePoints[0] = (Vector3) source;
            this.linePoints[1] = (Vector3) this.startHorizontalBreak;
            this.linePoints[2] = (Vector3) this.endHorizontalBreak;
            this.linePoints[3] = (Vector3) destination;
            Handles.DrawPolyLine(this.linePoints);
            for (int i = 0; i < this.linePoints.Length; i++)
            {
                this.linePoints[i].x++;
                this.linePoints[i].y++;
            }
            Handles.DrawPolyLine(this.linePoints);
        }

        public void LoadConnection(NodeDesigner nodeDesigner, BehaviorDesigner.Editor.NodeConnectionType nodeConnectionType)
        {
            this.originatingNodeDesigner = nodeDesigner;
            this.nodeConnectionType = nodeConnectionType;
            this.selected = false;
        }

        public void OnEnable()
        {
            base.hideFlags = HideFlags.HideAndDontSave;
        }

        public void select()
        {
            this.selected = true;
        }

        public NodeDesigner DestinationNodeDesigner
        {
            get
            {
                return this.destinationNodeDesigner;
            }
            set
            {
                this.destinationNodeDesigner = value;
            }
        }

        public float HorizontalHeight
        {
            set
            {
                this.horizontalHeight = value;
                this.horizontalDirty = true;
            }
        }

        public BehaviorDesigner.Editor.NodeConnectionType NodeConnectionType
        {
            get
            {
                return this.nodeConnectionType;
            }
            set
            {
                this.nodeConnectionType = value;
            }
        }

        public NodeDesigner OriginatingNodeDesigner
        {
            get
            {
                return this.originatingNodeDesigner;
            }
            set
            {
                this.originatingNodeDesigner = value;
            }
        }
    }
}

