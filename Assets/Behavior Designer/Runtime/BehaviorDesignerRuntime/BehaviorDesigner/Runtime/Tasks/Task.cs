namespace BehaviorDesigner.Runtime.Tasks
{
    using BehaviorDesigner.Runtime;
    using System;
    using System.Collections;
    using UnityEngine;

    public abstract class Task
    {
        [SerializeField]
        private string friendlyName = string.Empty;
        protected UnityEngine.GameObject gameObject;
        [SerializeField]
        private int id = -1;
        [SerializeField]
        private bool instant = true;
        [SerializeField]
        private BehaviorDesigner.Runtime.NodeData nodeData;
        [SerializeField]
        private Behavior owner;
        private int referenceID = -1;
        protected UnityEngine.Transform transform;

        protected Task()
        {
        }

        protected T GetComponent<T>() where T: Component
        {
            return this.gameObject.GetComponent<T>();
        }

        protected Component GetComponent(System.Type type)
        {
            return this.gameObject.GetComponent(type);
        }

        protected UnityEngine.GameObject GetDefaultGameObject(UnityEngine.GameObject go)
        {
            if (go == null)
            {
                return this.gameObject;
            }
            return go;
        }

        public virtual float GetPriority()
        {
            return 0f;
        }

        public virtual void OnAwake()
        {
        }

        public virtual void OnBehaviorComplete()
        {
        }

        public virtual void OnBehaviorRestart()
        {
        }

        public virtual void OnCollisionEnter(Collision collision)
        {
        }

        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
        }

        public virtual void OnCollisionExit(Collision collision)
        {
        }

        public virtual void OnCollisionExit2D(Collision2D collision)
        {
        }

        public virtual void OnCollisionStay(Collision collision)
        {
        }

        public virtual void OnCollisionStay2D(Collision2D collision)
        {
        }

        public virtual void OnControllerColliderHit(ControllerColliderHit hit)
        {
        }

        public virtual void OnDrawGizmos()
        {
        }

        public virtual void OnEnd()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnLateUpdate()
        {
        }

        public virtual void OnPause(bool paused)
        {
        }

        public virtual void OnReset()
        {
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnTriggerEnter(Collider other)
        {
        }

        public virtual void OnTriggerEnter2D(Collider2D other)
        {
        }

        public virtual void OnTriggerExit(Collider other)
        {
        }

        public virtual void OnTriggerExit2D(Collider2D other)
        {
        }

        public virtual void OnTriggerStay(Collider other)
        {
        }

        public virtual void OnTriggerStay2D(Collider2D other)
        {
        }

        public virtual TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }

        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            return this.Owner.StartCoroutine(routine);
        }

        protected void StartCoroutine(string methodName)
        {
            this.Owner.StartTaskCoroutine(this, methodName);
        }

        protected Coroutine StartCoroutine(string methodName, object value)
        {
            return this.Owner.StartTaskCoroutine(this, methodName, value);
        }

        protected void StopAllCoroutines()
        {
            this.Owner.StopAllTaskCoroutines();
        }

        protected void StopCoroutine(string methodName)
        {
            this.Owner.StopTaskCoroutine(methodName);
        }

        public string FriendlyName
        {
            get
            {
                return this.friendlyName;
            }
            set
            {
                this.friendlyName = value;
            }
        }

        public UnityEngine.GameObject GameObject
        {
            set
            {
                this.gameObject = value;
            }
        }

        public int ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        public bool IsInstant
        {
            get
            {
                return this.instant;
            }
            set
            {
                this.instant = value;
            }
        }

        public BehaviorDesigner.Runtime.NodeData NodeData
        {
            get
            {
                return this.nodeData;
            }
            set
            {
                this.nodeData = value;
            }
        }

        public Behavior Owner
        {
            get
            {
                return this.owner;
            }
            set
            {
                this.owner = value;
            }
        }

        public int ReferenceID
        {
            get
            {
                return this.referenceID;
            }
            set
            {
                this.referenceID = value;
            }
        }

        public UnityEngine.Transform Transform
        {
            set
            {
                this.transform = value;
            }
        }
    }
}

