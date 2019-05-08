namespace BehaviorDesigner.Runtime.Tasks
{
    using BehaviorDesigner.Runtime;
    using System;
    using UnityEngine;

    [TaskDescription("Behavior Reference allows you to run another behavior tree within the current behavior tree."), BehaviorDesigner.Runtime.Tasks.HelpURL("http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=53"), TaskIcon("BehaviorTreeReferenceIcon.png")]
    public abstract class BehaviorReference : BehaviorDesigner.Runtime.Tasks.Action
    {
        [HideInInspector]
        public bool collapsed;
        [RequiredField, BehaviorDesigner.Runtime.Tasks.Tooltip("External behavior array that this task should reference")]
        public ExternalBehavior[] externalBehaviors;
        [BehaviorDesigner.Runtime.Tasks.Tooltip("Any variables that should be set for the specific tree")]
        public SharedNamedVariable[] variables;

        protected BehaviorReference()
        {
        }

        public virtual ExternalBehavior[] GetExternalBehaviors()
        {
            return this.externalBehaviors;
        }

        public override void OnReset()
        {
            this.externalBehaviors = null;
        }
    }
}

