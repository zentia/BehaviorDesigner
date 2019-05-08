namespace BehaviorDesigner.Runtime
{
    using BehaviorDesigner.Runtime.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    [Serializable]
    public abstract class Behavior : MonoBehaviour, IBehavior
    {
        private Dictionary<string, List<TaskCoroutine>> activeTaskCoroutines;
        private List<Dictionary<string, object>> defaultValues;
        private Dictionary<string, object> defaultVariableValues;
        private Dictionary<System.Type, Dictionary<string, Delegate>> eventTable;
        private TaskStatus executionStatus;
        [SerializeField]
        private BehaviorDesigner.Runtime.ExternalBehavior externalBehavior;
        [NonSerialized]
        public GizmoViewMode gizmoViewMode;
        [SerializeField]
        private int group;
        private bool[] hasEvent = new bool[15];
        private bool hasInheritedVariables;
        private bool initialized;
        private bool isPaused;
        [SerializeField]
        private bool logTaskChanges;
        [SerializeField]
        private BehaviorSource mBehaviorSource;
        [SerializeField]
        private bool pauseWhenDisabled;
        [SerializeField]
        private bool resetValuesOnRestart;
        [SerializeField]
        private bool restartWhenComplete;
        [NonSerialized]
        public bool showBehaviorDesignerGizmo = true;
        [SerializeField]
        private bool startWhenEnabled = true;

        public event BehaviorHandler OnBehaviorEnd;

        public event BehaviorHandler OnBehaviorRestart;

        public event BehaviorHandler OnBehaviorStart;

        public Behavior()
        {
            this.mBehaviorSource = new BehaviorSource(this);
        }

        int IBehavior.GetInstanceID()
        {
            return base.GetInstanceID();
        }

        public void CheckForSerialization()
        {
            if (this.externalBehavior != null)
            {
                List<SharedVariable> allVariables = null;
                bool force = false;
                if (!this.hasInheritedVariables)
                {
                    this.mBehaviorSource.CheckForSerialization(false, null);
                    allVariables = this.mBehaviorSource.GetAllVariables();
                    this.hasInheritedVariables = true;
                    force = true;
                }
                this.externalBehavior.BehaviorSource.Owner = this.ExternalBehavior;
                this.externalBehavior.BehaviorSource.CheckForSerialization(force, this.GetBehaviorSource());
                if (allVariables != null)
                {
                    for (int i = 0; i < allVariables.Count; i++)
                    {
                        if (allVariables[i] != null)
                        {
                            this.mBehaviorSource.SetVariable(allVariables[i].Name, allVariables[i]);
                        }
                    }
                }
            }
            else
            {
                this.mBehaviorSource.CheckForSerialization(false, null);
            }
        }

        public static BehaviorManager CreateBehaviorManager()
        {
            if ((BehaviorManager.instance == null) && Application.isPlaying)
            {
                GameObject obj2 = new GameObject {
                    name = "Behavior Manager"
                };
                return obj2.AddComponent<BehaviorManager>();
            }
            return null;
        }

        public void DisableBehavior()
        {
            if (BehaviorManager.instance != null)
            {
                BehaviorManager.instance.DisableBehavior(this, this.pauseWhenDisabled);
                this.isPaused = this.pauseWhenDisabled;
            }
        }

        public void DisableBehavior(bool pause)
        {
            if (BehaviorManager.instance != null)
            {
                BehaviorManager.instance.DisableBehavior(this, pause);
                this.isPaused = pause;
            }
        }

        private void DrawTaskGizmos(Task task)
        {
            if ((task != null) && (((this.gizmoViewMode != GizmoViewMode.Running) || task.NodeData.IsReevaluating) || (!task.NodeData.IsReevaluating && (task.NodeData.ExecutionStatus == TaskStatus.Running))))
            {
                task.OnDrawGizmos();
                if (task is ParentTask)
                {
                    ParentTask task2 = task as ParentTask;
                    if (task2.Children != null)
                    {
                        for (int i = 0; i < task2.Children.Count; i++)
                        {
                            this.DrawTaskGizmos(task2.Children[i]);
                        }
                    }
                }
            }
        }

        private void DrawTaskGizmos(bool selected)
        {
            if (((this.gizmoViewMode != GizmoViewMode.Never) && ((this.gizmoViewMode != GizmoViewMode.Selected) || selected)) && ((((this.gizmoViewMode == GizmoViewMode.Running) || (this.gizmoViewMode == GizmoViewMode.Always)) || (Application.isPlaying && (this.ExecutionStatus == TaskStatus.Running))) || !Application.isPlaying))
            {
                this.CheckForSerialization();
                this.DrawTaskGizmos(this.mBehaviorSource.RootTask);
                List<Task> detachedTasks = this.mBehaviorSource.DetachedTasks;
                if (detachedTasks != null)
                {
                    for (int i = 0; i < detachedTasks.Count; i++)
                    {
                        this.DrawTaskGizmos(detachedTasks[i]);
                    }
                }
            }
        }

        public void EnableBehavior()
        {
            CreateBehaviorManager();
            if (BehaviorManager.instance != null)
            {
                BehaviorManager.instance.EnableBehavior(this);
            }
        }

        public T FindTask<T>() where T: Task
        {
            return this.FindTask<T>(this.mBehaviorSource.RootTask);
        }

        private T FindTask<T>(Task task) where T: Task
        {
            ParentTask task2;
            if (task.GetType().Equals(typeof(T)))
            {
                return (T) task;
            }
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    T local = null;
                    local = this.FindTask<T>(task2.Children[i]);
                    if (local != null)
                    {
                        return local;
                    }
                }
            }
            return null;
        }

        public List<T> FindTasks<T>() where T: Task
        {
            this.CheckForSerialization();
            List<T> taskList = new List<T>();
            this.FindTasks<T>(this.mBehaviorSource.RootTask, ref taskList);
            return taskList;
        }

        private void FindTasks<T>(Task task, ref List<T> taskList) where T: Task
        {
            ParentTask task2;
            if (typeof(T).IsAssignableFrom(task.GetType()))
            {
                taskList.Add((T) task);
            }
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    this.FindTasks<T>(task2.Children[i], ref taskList);
                }
            }
        }

        public List<Task> FindTasksWithName(string taskName)
        {
            List<Task> taskList = new List<Task>();
            this.FindTasksWithName(taskName, this.mBehaviorSource.RootTask, ref taskList);
            return taskList;
        }

        private void FindTasksWithName(string taskName, Task task, ref List<Task> taskList)
        {
            ParentTask task2;
            if (task.FriendlyName.Equals(taskName))
            {
                taskList.Add(task);
            }
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    this.FindTasksWithName(taskName, task2.Children[i], ref taskList);
                }
            }
        }

        public Task FindTaskWithName(string taskName)
        {
            return this.FindTaskWithName(taskName, this.mBehaviorSource.RootTask);
        }

        private Task FindTaskWithName(string taskName, Task task)
        {
            ParentTask task2;
            if (task.FriendlyName.Equals(taskName))
            {
                return task;
            }
            if (((task2 = task as ParentTask) != null) && (task2.Children != null))
            {
                for (int i = 0; i < task2.Children.Count; i++)
                {
                    Task task3 = null;
                    task3 = this.FindTaskWithName(taskName, task2.Children[i]);
                    if (task3 != null)
                    {
                        return task3;
                    }
                }
            }
            return null;
        }

        public List<Task> GetActiveTasks()
        {
            if (BehaviorManager.instance == null)
            {
                return null;
            }
            return BehaviorManager.instance.GetActiveTasks(this);
        }

        public List<SharedVariable> GetAllVariables()
        {
            this.CheckForSerialization();
            return this.mBehaviorSource.GetAllVariables();
        }

        public BehaviorSource GetBehaviorSource()
        {
            return this.mBehaviorSource;
        }

        private Delegate GetDelegate(string name, System.Type type)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate delegate2;
            if (((this.eventTable != null) && this.eventTable.TryGetValue(type, out dictionary)) && dictionary.TryGetValue(name, out delegate2))
            {
                return delegate2;
            }
            return null;
        }

        public UnityEngine.Object GetObject()
        {
            return this;
        }

        public string GetOwnerName()
        {
            return base.gameObject.name;
        }

        public SharedVariable GetVariable(string name)
        {
            this.CheckForSerialization();
            return this.mBehaviorSource.GetVariable(name);
        }

        public void OnBehaviorEnded()
        {
            if (this.OnBehaviorEnd != null)
            {
                this.OnBehaviorEnd();
            }
        }

        public void OnBehaviorRestarted()
        {
            if (this.OnBehaviorRestart != null)
            {
                this.OnBehaviorRestart();
            }
        }

        public void OnBehaviorStarted()
        {
            if (this.OnBehaviorStart != null)
            {
                this.OnBehaviorStart();
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (this.hasEvent[0] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionEnter(collision, this);
            }
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            if (this.hasEvent[6] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionEnter2D(collision, this);
            }
        }

        public void OnCollisionExit(Collision collision)
        {
            if (this.hasEvent[2] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionExit(collision, this);
            }
        }

        public void OnCollisionExit2D(Collision2D collision)
        {
            if (this.hasEvent[8] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionExit2D(collision, this);
            }
        }

        public void OnCollisionStay(Collision collision)
        {
            if (this.hasEvent[1] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionStay(collision, this);
            }
        }

        public void OnCollisionStay2D(Collision2D collision)
        {
            if (this.hasEvent[7] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnCollisionStay2D(collision, this);
            }
        }

        public void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (this.hasEvent[12] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnControllerColliderHit(hit, this);
            }
        }

        public void OnDisable()
        {
            this.DisableBehavior();
        }

        public void OnDrawGizmos()
        {
            this.DrawTaskGizmos(false);
        }

        public void OnDrawGizmosSelected()
        {
            if (this.showBehaviorDesignerGizmo)
            {
                Gizmos.DrawIcon(base.transform.position, "Behavior Designer Scene Icon.png");
            }
            this.DrawTaskGizmos(true);
        }

        public void OnEnable()
        {
            if ((BehaviorManager.instance != null) && this.isPaused)
            {
                BehaviorManager.instance.EnableBehavior(this);
                this.isPaused = false;
            }
            else if (this.startWhenEnabled && this.initialized)
            {
                this.EnableBehavior();
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (this.hasEvent[3] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerEnter(other, this);
            }
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (this.hasEvent[9] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerEnter2D(other, this);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (this.hasEvent[5] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerExit(other, this);
            }
        }

        public void OnTriggerExit2D(Collider2D other)
        {
            if (this.hasEvent[11] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerExit2D(other, this);
            }
        }

        public void OnTriggerStay(Collider other)
        {
            if (this.hasEvent[4] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerStay(other, this);
            }
        }

        public void OnTriggerStay2D(Collider2D other)
        {
            if (this.hasEvent[10] && (BehaviorManager.instance != null))
            {
                BehaviorManager.instance.BehaviorOnTriggerStay2D(other, this);
            }
        }

        public void RegisterEvent<T>(string name, Action<T> handler)
        {
            this.RegisterEvent(name, handler);
        }

        public void RegisterEvent(string name, System.Action handler)
        {
            this.RegisterEvent(name, (Delegate) handler);
        }

        public void RegisterEvent<T, U>(string name, Action<T, U> handler)
        {
            this.RegisterEvent(name, handler);
        }

        public void RegisterEvent<T, U, V>(string name, Action<T, U, V> handler)
        {
            this.RegisterEvent(name, handler);
        }

        private void RegisterEvent(string name, Delegate handler)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate delegate2;
            if (this.eventTable == null)
            {
                this.eventTable = new Dictionary<System.Type, Dictionary<string, Delegate>>();
            }
            if (!this.eventTable.TryGetValue(handler.GetType(), out dictionary))
            {
                dictionary = new Dictionary<string, Delegate>();
                this.eventTable.Add(handler.GetType(), dictionary);
            }
            if (dictionary.TryGetValue(name, out delegate2))
            {
                dictionary[name] = Delegate.Combine(delegate2, handler);
            }
            else
            {
                dictionary.Add(name, handler);
            }
        }

        private void ResetValue(Task task, ref int index)
        {
            if (task != null)
            {
                Dictionary<string, object> dictionary = this.defaultValues[index];
                index++;
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    FieldInfo field = task.GetType().GetField(pair.Key);
                    if (field != null)
                    {
                        field.SetValue(task, pair.Value);
                    }
                }
                if (task is ParentTask)
                {
                    ParentTask task2 = task as ParentTask;
                    if (task2.Children != null)
                    {
                        for (int i = 0; i < task2.Children.Count; i++)
                        {
                            this.ResetValue(task2.Children[i], ref index);
                        }
                    }
                }
            }
        }

        private void ResetValues()
        {
            foreach (KeyValuePair<string, object> pair in this.defaultVariableValues)
            {
                this.SetVariableValue(pair.Key, pair.Value);
            }
            int index = 0;
            this.ResetValue(this.mBehaviorSource.RootTask, ref index);
        }

        public void SaveResetValues()
        {
            if (this.defaultValues == null)
            {
                this.defaultValues = new List<Dictionary<string, object>>();
                this.defaultVariableValues = new Dictionary<string, object>();
                this.SaveValues();
            }
            else
            {
                this.ResetValues();
            }
        }

        private void SaveValue(Task task)
        {
            if (task != null)
            {
                FieldInfo[] publicFields = TaskUtility.GetPublicFields(task.GetType());
                Dictionary<string, object> item = new Dictionary<string, object>();
                for (int i = 0; i < publicFields.Length; i++)
                {
                    object obj2 = publicFields[i].GetValue(task);
                    if (obj2 is SharedVariable)
                    {
                        SharedVariable variable = obj2 as SharedVariable;
                        if (variable.IsGlobal || variable.IsShared)
                        {
                            continue;
                        }
                    }
                    item.Add(publicFields[i].Name, publicFields[i].GetValue(task));
                }
                this.defaultValues.Add(item);
                if (task is ParentTask)
                {
                    ParentTask task2 = task as ParentTask;
                    if (task2.Children != null)
                    {
                        for (int j = 0; j < task2.Children.Count; j++)
                        {
                            this.SaveValue(task2.Children[j]);
                        }
                    }
                }
            }
        }

        private void SaveValues()
        {
            List<SharedVariable> allVariables = this.mBehaviorSource.GetAllVariables();
            for (int i = 0; i < allVariables.Count; i++)
            {
                this.defaultVariableValues.Add(allVariables[i].Name, allVariables[i].GetValue());
            }
            this.SaveValue(this.mBehaviorSource.RootTask);
        }

        public void SendEvent(string name)
        {
            System.Action action = this.GetDelegate(name, typeof(System.Action)) as System.Action;
            if (action != null)
            {
                action();
            }
        }

        public void SendEvent<T>(string name, T arg1)
        {
            Action<T> action = this.GetDelegate(name, typeof(Action<T>)) as Action<T>;
            if (action != null)
            {
                action(arg1);
            }
        }

        public void SendEvent<T, U>(string name, T arg1, U arg2)
        {
            Action<T, U> action = this.GetDelegate(name, typeof(Action<T, U>)) as Action<T, U>;
            if (action != null)
            {
                action(arg1, arg2);
            }
        }

        public void SendEvent<T, U, V>(string name, T arg1, U arg2, V arg3)
        {
            Action<T, U, V> action = this.GetDelegate(name, typeof(Action<T, U, V>)) as Action<T, U, V>;
            if (action != null)
            {
                action(arg1, arg2, arg3);
            }
        }

        public void SetBehaviorSource(BehaviorSource behaviorSource)
        {
            this.mBehaviorSource = behaviorSource;
        }

        public void SetVariable(string name, SharedVariable item)
        {
            this.CheckForSerialization();
            this.mBehaviorSource.SetVariable(name, item);
        }

        public void SetVariableValue(string name, object value)
        {
            SharedVariable variable = this.GetVariable(name);
            if (variable != null)
            {
                variable.SetValue(value);
                variable.ValueChanged();
            }
            else
            {
                Debug.LogError("Error: No variable exists with name " + name);
            }
        }

        public void Start()
        {
            if (this.startWhenEnabled)
            {
                this.EnableBehavior();
            }
            this.initialized = true;
            for (int i = 0; i < 15; i++)
            {
                this.hasEvent[i] = this.TaskContainsMethod(((EventTypes) i).ToString(), this.mBehaviorSource.RootTask);
            }
        }

        public Coroutine StartTaskCoroutine(Task task, string methodName)
        {
            MethodInfo method = task.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError("Unable to start coroutine " + methodName + ": method not found");
                return null;
            }
            if (this.activeTaskCoroutines == null)
            {
                this.activeTaskCoroutines = new Dictionary<string, List<TaskCoroutine>>();
            }
            TaskCoroutine item = new TaskCoroutine(this, (IEnumerator) method.Invoke(task, new object[0]), methodName);
            if (this.activeTaskCoroutines.ContainsKey(methodName))
            {
                List<TaskCoroutine> list = this.activeTaskCoroutines[methodName];
                list.Add(item);
                this.activeTaskCoroutines[methodName] = list;
            }
            else
            {
                List<TaskCoroutine> list2 = new List<TaskCoroutine> {
                    item
                };
                this.activeTaskCoroutines.Add(methodName, list2);
            }
            return item.Coroutine;
        }

        public Coroutine StartTaskCoroutine(Task task, string methodName, object value)
        {
            MethodInfo method = task.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError("Unable to start coroutine " + methodName + ": method not found");
                return null;
            }
            if (this.activeTaskCoroutines == null)
            {
                this.activeTaskCoroutines = new Dictionary<string, List<TaskCoroutine>>();
            }
            object[] parameters = new object[] { value };
            TaskCoroutine item = new TaskCoroutine(this, (IEnumerator) method.Invoke(task, parameters), methodName);
            if (this.activeTaskCoroutines.ContainsKey(methodName))
            {
                List<TaskCoroutine> list = this.activeTaskCoroutines[methodName];
                list.Add(item);
                this.activeTaskCoroutines[methodName] = list;
            }
            else
            {
                List<TaskCoroutine> list2 = new List<TaskCoroutine> {
                    item
                };
                this.activeTaskCoroutines.Add(methodName, list2);
            }
            return item.Coroutine;
        }

        public void StopAllTaskCoroutines()
        {
            base.StopAllCoroutines();
            foreach (KeyValuePair<string, List<TaskCoroutine>> pair in this.activeTaskCoroutines)
            {
                List<TaskCoroutine> list = pair.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Stop();
                }
            }
        }

        public void StopTaskCoroutine(string methodName)
        {
            if (this.activeTaskCoroutines.ContainsKey(methodName))
            {
                List<TaskCoroutine> list = this.activeTaskCoroutines[methodName];
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Stop();
                }
            }
        }

        private bool TaskContainsMethod(string methodName, Task task)
        {
            if (task != null)
            {
                MethodInfo method = task.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if ((method != null) && task.GetType().Equals(method.DeclaringType))
                {
                    return true;
                }
                if (task is ParentTask)
                {
                    ParentTask task2 = task as ParentTask;
                    if (task2.Children != null)
                    {
                        for (int i = 0; i < task2.Children.Count; i++)
                        {
                            if (this.TaskContainsMethod(methodName, task2.Children[i]))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public void TaskCoroutineEnded(TaskCoroutine taskCoroutine, string coroutineName)
        {
            if (this.activeTaskCoroutines.ContainsKey(coroutineName))
            {
                List<TaskCoroutine> list = this.activeTaskCoroutines[coroutineName];
                if (list.Count == 1)
                {
                    this.activeTaskCoroutines.Remove(coroutineName);
                }
                else
                {
                    list.Remove(taskCoroutine);
                    this.activeTaskCoroutines[coroutineName] = list;
                }
            }
        }

        public override string ToString()
        {
            return this.mBehaviorSource.ToString();
        }

        public void UnregisterEvent<T>(string name, Action<T> handler)
        {
            this.UnregisterEvent(name, handler);
        }

        public void UnregisterEvent(string name, System.Action handler)
        {
            this.UnregisterEvent(name, (Delegate) handler);
        }

        public void UnregisterEvent<T, U>(string name, Action<T, U> handler)
        {
            this.UnregisterEvent(name, handler);
        }

        public void UnregisterEvent<T, U, V>(string name, Action<T, U, V> handler)
        {
            this.UnregisterEvent(name, handler);
        }

        private void UnregisterEvent(string name, Delegate handler)
        {
            Dictionary<string, Delegate> dictionary;
            Delegate delegate2;
            if ((this.eventTable != null) && (this.eventTable.TryGetValue(handler.GetType(), out dictionary) && dictionary.TryGetValue(name, out delegate2)))
            {
                dictionary[name] = Delegate.Remove(delegate2, handler);
            }
        }

        public string BehaviorDescription
        {
            get
            {
                return this.mBehaviorSource.behaviorDescription;
            }
            set
            {
                this.mBehaviorSource.behaviorDescription = value;
            }
        }

        public string BehaviorName
        {
            get
            {
                return this.mBehaviorSource.behaviorName;
            }
            set
            {
                this.mBehaviorSource.behaviorName = value;
            }
        }

        public TaskStatus ExecutionStatus
        {
            get
            {
                return this.executionStatus;
            }
            set
            {
                this.executionStatus = value;
            }
        }

        public BehaviorDesigner.Runtime.ExternalBehavior ExternalBehavior
        {
            get
            {
                return this.externalBehavior;
            }
            set
            {
                if (this.externalBehavior != value)
                {
                    if (BehaviorManager.instance != null)
                    {
                        BehaviorManager.instance.DisableBehavior(this);
                    }
                    this.mBehaviorSource.HasSerialized = false;
                }
                this.externalBehavior = value;
                if (this.startWhenEnabled)
                {
                    this.EnableBehavior();
                }
            }
        }

        public int Group
        {
            get
            {
                return this.group;
            }
            set
            {
                this.group = value;
            }
        }

        public bool[] HasEvent
        {
            get
            {
                return this.hasEvent;
            }
        }

        public bool HasInheritedVariables
        {
            get
            {
                return this.hasInheritedVariables;
            }
            set
            {
                this.hasInheritedVariables = value;
            }
        }

        public bool LogTaskChanges
        {
            get
            {
                return this.logTaskChanges;
            }
            set
            {
                this.logTaskChanges = value;
            }
        }

        public bool PauseWhenDisabled
        {
            get
            {
                return this.pauseWhenDisabled;
            }
            set
            {
                this.pauseWhenDisabled = value;
            }
        }

        public bool ResetValuesOnRestart
        {
            get
            {
                return this.resetValuesOnRestart;
            }
            set
            {
                this.resetValuesOnRestart = value;
            }
        }

        public bool RestartWhenComplete
        {
            get
            {
                return this.restartWhenComplete;
            }
            set
            {
                this.restartWhenComplete = value;
            }
        }

        public bool StartWhenEnabled
        {
            get
            {
                return this.startWhenEnabled;
            }
            set
            {
                this.startWhenEnabled = value;
            }
        }

        public delegate void BehaviorHandler();

        public enum EventTypes
        {
            OnCollisionEnter,
            OnCollisionStay,
            OnCollisionExit,
            OnTriggerEnter,
            OnTriggerStay,
            OnTriggerExit,
            OnCollisionEnter2D,
            OnCollisionStay2D,
            OnCollisionExit2D,
            OnTriggerEnter2D,
            OnTriggerStay2D,
            OnTriggerExit2D,
            OnControllerColliderHit,
            OnLateUpdate,
            OnFixedUpdate,
            None
        }

        public enum GizmoViewMode
        {
            Running,
            Always,
            Selected,
            Never
        }
    }
}

