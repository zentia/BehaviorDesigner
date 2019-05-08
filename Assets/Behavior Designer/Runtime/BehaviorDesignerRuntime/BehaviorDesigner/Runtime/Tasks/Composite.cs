namespace BehaviorDesigner.Runtime.Tasks
{
    using System;
    using UnityEngine;

    public abstract class Composite : ParentTask
    {
        [SerializeField, BehaviorDesigner.Runtime.Tasks.Tooltip("Specifies the type of conditional abort. More information is located at http://www.opsive.com/assets/BehaviorDesigner/documentation.php?id=89.")]
        protected BehaviorDesigner.Runtime.Tasks.AbortType abortType;

        protected Composite()
        {
        }

        public BehaviorDesigner.Runtime.Tasks.AbortType AbortType
        {
            get
            {
                return this.abortType;
            }
        }
    }
}

