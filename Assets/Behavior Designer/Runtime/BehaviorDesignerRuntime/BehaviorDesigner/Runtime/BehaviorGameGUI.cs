namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("Behavior Designer/Behavior Game GUI")]
    public class BehaviorGameGUI : MonoBehaviour
    {
        private BehaviorManager behaviorManager;
        private Camera mainCamera;

        public void OnGUI()
        {
            if (this.behaviorManager == null)
            {
                this.behaviorManager = BehaviorManager.instance;
            }
            if ((this.behaviorManager != null) && (this.mainCamera != null))
            {
                List<BehaviorManager.BehaviorTree> behaviorTrees = this.behaviorManager.BehaviorTrees;
                for (int i = 0; i < behaviorTrees.Count; i++)
                {
                    BehaviorManager.BehaviorTree tree = behaviorTrees[i];
                    string text = string.Empty;
                    for (int j = 0; j < tree.activeStack.Count; j++)
                    {
                        Task task = tree.taskList[tree.activeStack[j].Peek()];
                        if (task is BehaviorDesigner.Runtime.Tasks.Action)
                        {
                            text = text + tree.taskList[tree.activeStack[j].Peek()].FriendlyName + ((j >= (tree.activeStack.Count - 1)) ? string.Empty : "\n");
                        }
                    }
                    Transform transform = tree.behavior.transform;
                    Vector2 vector2 = GUIUtility.ScreenToGUIPoint(Camera.main.WorldToScreenPoint(transform.position));
                    GUIContent content = new GUIContent(text);
                    Vector2 vector3 = GUI.skin.label.CalcSize(content);
                    vector3.x += 14f;
                    vector3.y += 5f;
                    GUI.Box(new Rect(vector2.x - (vector3.x / 2f), vector2.y + (vector3.y / 2f), vector3.x, vector3.y), content);
                }
            }
        }

        public void Start()
        {
            this.mainCamera = Camera.main;
        }
    }
}

