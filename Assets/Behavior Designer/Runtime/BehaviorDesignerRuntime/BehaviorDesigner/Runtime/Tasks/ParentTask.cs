namespace BehaviorDesigner.Runtime.Tasks
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class ParentTask : Task
    {
        [SerializeField]
        protected List<Task> children;

        protected ParentTask()
        {
        }

        public void AddChild(Task child, int index)
        {
            if (this.children == null)
            {
                this.children = new List<Task>();
            }
            this.children.Insert(index, child);
        }

        public virtual bool CanExecute()
        {
            return true;
        }

        public virtual bool CanReevaluate()
        {
            return false;
        }

        public virtual bool CanRunParallelChildren()
        {
            return false;
        }

        public virtual int CurrentChildIndex()
        {
            return 0;
        }

        public virtual TaskStatus Decorate(TaskStatus status)
        {
            return status;
        }

        public virtual int MaxChildren()
        {
            return 0x7fffffff;
        }

        public virtual void OnChildExecuted(TaskStatus childStatus)
        {
        }

        public virtual void OnChildExecuted(int childIndex, TaskStatus childStatus)
        {
        }

        public virtual void OnChildStarted()
        {
        }

        public virtual void OnChildStarted(int childIndex)
        {
        }

        public virtual void OnConditionalAbort(int childIndex)
        {
        }

        public override void OnDrawGizmos()
        {
            if (this.children != null)
            {
                for (int i = 0; i < this.children.Count; i++)
                {
                    if (this.children[i] != null)
                    {
                        this.children[i].OnDrawGizmos();
                    }
                }
            }
        }

        public virtual void OnReevaluationEnded(TaskStatus status)
        {
        }

        public virtual bool OnReevaluationStarted()
        {
            return false;
        }

        public virtual TaskStatus OverrideStatus()
        {
            return TaskStatus.Running;
        }

        public virtual TaskStatus OverrideStatus(TaskStatus status)
        {
            return status;
        }

        public void ReplaceAddChild(Task child, int index)
        {
            if ((this.children != null) && (index < this.children.Count))
            {
                this.children[index] = child;
            }
            else
            {
                this.AddChild(child, index);
            }
        }

        public List<Task> Children
        {
            get
            {
                return this.children;
            }
            private set
            {
                this.children = value;
            }
        }
    }
}

