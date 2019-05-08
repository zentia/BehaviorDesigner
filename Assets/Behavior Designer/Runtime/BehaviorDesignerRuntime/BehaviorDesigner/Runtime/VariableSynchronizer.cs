namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    [AddComponentMenu("Behavior Designer/Variable Synchronizer")]
    public class VariableSynchronizer : MonoBehaviour
    {
        [SerializeField]
        private List<SynchronizedVariable> synchronizedVariables = new List<SynchronizedVariable>();
        [SerializeField]
        private UpdateIntervalType updateInterval;
        [SerializeField]
        private float updateIntervalSeconds;
        private WaitForSeconds updateWait;

        public void Awake()
        {
            for (int i = this.synchronizedVariables.Count - 1; i > -1; i--)
            {
                Behavior targetComponent;
                PropertyInfo property;
                System.Type typeWithinAssembly;
                int num2;
                System.Type type3;
                int num3;
                SynchronizedVariable variable = this.synchronizedVariables[i];
                if (variable.global)
                {
                    variable.sharedVariable = GlobalVariables.Instance.GetVariable(variable.variableName);
                }
                else
                {
                    variable.sharedVariable = variable.behavior.GetVariable(variable.variableName);
                }
                string str = string.Empty;
                if (variable.sharedVariable == null)
                {
                    str = "the SharedVariable can't be found";
                }
                else
                {
                    switch (variable.synchronizationType)
                    {
                        case SynchronizationType.BehaviorDesigner:
                            targetComponent = variable.targetComponent as Behavior;
                            if (targetComponent != null)
                            {
                                goto Label_00C4;
                            }
                            str = "the target component is not of type Behavior Tree";
                            break;

                        case SynchronizationType.Property:
                            property = variable.targetComponent.GetType().GetProperty(variable.targetName);
                            if (property != null)
                            {
                                goto Label_014C;
                            }
                            str = "the property " + variable.targetName + " doesn't exist";
                            break;

                        case SynchronizationType.Animator:
                            variable.animator = variable.targetComponent as Animator;
                            if (variable.animator != null)
                            {
                                goto Label_01EA;
                            }
                            str = "the component is not of type Animator";
                            break;

                        case SynchronizationType.PlayMaker:
                        {
                            typeWithinAssembly = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.VariableSynchronizer_PlayMaker");
                            if (typeWithinAssembly == null)
                            {
                                goto Label_032C;
                            }
                            MethodInfo method = typeWithinAssembly.GetMethod("Start");
                            if (method != null)
                            {
                                object[] parameters = new object[] { variable };
                                num2 = (int) method.Invoke(null, parameters);
                                if (num2 != 1)
                                {
                                    goto Label_02E3;
                                }
                                str = "the PlayMaker NamedVariable cannot be found";
                            }
                            break;
                        }
                        case SynchronizationType.uFrame:
                        {
                            type3 = TaskUtility.GetTypeWithinAssembly("BehaviorDesigner.Runtime.VariableSynchronizer_uFrame");
                            if (type3 == null)
                            {
                                goto Label_03D4;
                            }
                            MethodInfo info6 = type3.GetMethod("Start");
                            if (info6 != null)
                            {
                                object[] objArray2 = new object[] { variable };
                                num3 = (int) info6.Invoke(null, objArray2);
                                if (num3 != 1)
                                {
                                    goto Label_038B;
                                }
                                str = "the uFrame property cannot be found";
                            }
                            break;
                        }
                    }
                }
                goto Label_03DF;
            Label_00C4:
                if (variable.targetGlobal)
                {
                    variable.targetSharedVariable = GlobalVariables.Instance.GetVariable(variable.targetName);
                }
                else
                {
                    variable.targetSharedVariable = targetComponent.GetVariable(variable.targetName);
                }
                if (variable.targetSharedVariable == null)
                {
                    str = "the target SharedVariable cannot be found";
                }
                goto Label_03DF;
            Label_014C:
                if (variable.setVariable)
                {
                    MethodInfo getMethod = property.GetGetMethod();
                    if (getMethod == null)
                    {
                        str = "the property has no get method";
                    }
                    else
                    {
                        variable.getDelegate = CreateGetDelegate(variable.targetComponent, getMethod);
                    }
                }
                else
                {
                    MethodInfo setMethod = property.GetSetMethod();
                    if (setMethod == null)
                    {
                        str = "the property has no set method";
                    }
                    else
                    {
                        variable.setDelegate = CreateSetDelegate(variable.targetComponent, setMethod);
                    }
                }
                goto Label_03DF;
            Label_01EA:
                variable.targetID = Animator.StringToHash(variable.targetName);
                System.Type propertyType = variable.sharedVariable.GetType().GetProperty("Value").PropertyType;
                if (propertyType.Equals(typeof(bool)))
                {
                    variable.animatorParameterType = AnimatorParameterType.Bool;
                }
                else if (propertyType.Equals(typeof(float)))
                {
                    variable.animatorParameterType = AnimatorParameterType.Float;
                }
                else if (propertyType.Equals(typeof(int)))
                {
                    variable.animatorParameterType = AnimatorParameterType.Integer;
                }
                else
                {
                    str = "there is no animator parameter type that can synchronize with " + propertyType;
                }
                goto Label_03DF;
            Label_02E3:
                if (num2 == 2)
                {
                    str = "the Behavior Designer SharedVariable is not the same type as the PlayMaker NamedVariable";
                }
                else
                {
                    MethodInfo info5 = typeWithinAssembly.GetMethod("Tick");
                    if (info5 != null)
                    {
                        variable.thirdPartyTick = (Action<SynchronizedVariable>) Delegate.CreateDelegate(typeof(Action<SynchronizedVariable>), info5);
                    }
                }
                goto Label_03DF;
            Label_032C:
                str = "has the PlayMaker classes been imported?";
                goto Label_03DF;
            Label_038B:
                if (num3 == 2)
                {
                    str = "the Behavior Designer SharedVariable is not the same type as the uFrame property";
                }
                else
                {
                    MethodInfo info7 = type3.GetMethod("Tick");
                    if (info7 != null)
                    {
                        variable.thirdPartyTick = (Action<SynchronizedVariable>) Delegate.CreateDelegate(typeof(Action<SynchronizedVariable>), info7);
                    }
                }
                goto Label_03DF;
            Label_03D4:
                str = "has the uFrame classes been imported?";
            Label_03DF:
                if (!string.IsNullOrEmpty(str))
                {
                    UnityEngine.Debug.LogError(string.Format("Unable to synchronize {0}: {1}", variable.sharedVariable.Name, str));
                    this.synchronizedVariables.RemoveAt(i);
                }
            }
            if (this.synchronizedVariables.Count == 0)
            {
                base.enabled = false;
            }
            else
            {
                this.UpdateIntervalChanged();
            }
        }

        [DebuggerHidden]
        private IEnumerator CoroutineUpdate()
        {
            return new <CoroutineUpdate>c__Iterator2 { <>f__this = this };
        }

        private static Func<object> CreateGetDelegate(object instance, MethodInfo method)
        {
            return Expression.Lambda<Func<object>>(Expression.TypeAs(Expression.Call(Expression.Constant(instance), method), typeof(object)), new ParameterExpression[0]).Compile();
        }

        private static Action<object> CreateSetDelegate(object instance, MethodInfo method)
        {
            ParameterExpression expression2;
            ConstantExpression expression = Expression.Constant(instance);
            UnaryExpression expression3 = Expression.Convert(expression2 = Expression.Parameter(typeof(object), "p"), method.GetParameters()[0].ParameterType);
            Expression[] arguments = new Expression[] { expression3 };
            ParameterExpression[] parameters = new ParameterExpression[] { expression2 };
            return Expression.Lambda<Action<object>>(Expression.Call(expression, method, arguments), parameters).Compile();
        }

        public void Tick()
        {
            for (int i = 0; i < this.synchronizedVariables.Count; i++)
            {
                SynchronizedVariable variable = this.synchronizedVariables[i];
                switch (variable.synchronizationType)
                {
                    case SynchronizationType.BehaviorDesigner:
                    {
                        if (!variable.setVariable)
                        {
                            break;
                        }
                        variable.sharedVariable.SetValue(variable.targetSharedVariable.GetValue());
                        continue;
                    }
                    case SynchronizationType.Property:
                    {
                        if (!variable.setVariable)
                        {
                            goto Label_00A1;
                        }
                        variable.sharedVariable.SetValue(variable.getDelegate());
                        continue;
                    }
                    case SynchronizationType.Animator:
                    {
                        if (!variable.setVariable)
                        {
                            goto Label_015C;
                        }
                        switch (variable.animatorParameterType)
                        {
                            case AnimatorParameterType.Bool:
                                goto Label_00E5;

                            case AnimatorParameterType.Float:
                                goto Label_010B;

                            case AnimatorParameterType.Integer:
                                goto Label_0131;
                        }
                        continue;
                    }
                    case SynchronizationType.PlayMaker:
                    case SynchronizationType.uFrame:
                    {
                        variable.thirdPartyTick(variable);
                        continue;
                    }
                    default:
                    {
                        continue;
                    }
                }
                variable.targetSharedVariable.SetValue(variable.sharedVariable.GetValue());
                continue;
            Label_00A1:
                variable.setDelegate(variable.sharedVariable.GetValue());
                continue;
            Label_00E5:
                variable.sharedVariable.SetValue(variable.animator.GetBool(variable.targetID));
                continue;
            Label_010B:
                variable.sharedVariable.SetValue(variable.animator.GetFloat(variable.targetID));
                continue;
            Label_0131:
                variable.sharedVariable.SetValue(variable.animator.GetInteger(variable.targetID));
                continue;
            Label_015C:
                switch (variable.animatorParameterType)
                {
                    case AnimatorParameterType.Bool:
                        variable.animator.SetBool(variable.targetID, (bool) variable.sharedVariable.GetValue());
                        break;

                    case AnimatorParameterType.Float:
                        variable.animator.SetFloat(variable.targetID, (float) variable.sharedVariable.GetValue());
                        break;

                    case AnimatorParameterType.Integer:
                        variable.animator.SetInteger(variable.targetID, (int) variable.sharedVariable.GetValue());
                        break;
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

        public List<SynchronizedVariable> SynchronizedVariables
        {
            get
            {
                return this.synchronizedVariables;
            }
            set
            {
                this.synchronizedVariables = value;
                base.enabled = true;
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

        [CompilerGenerated]
        private sealed class <CoroutineUpdate>c__Iterator2 : IEnumerator, IDisposable, IEnumerator<object>
        {
            internal object $current;
            internal int $PC;
            internal VariableSynchronizer <>f__this;

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

        public enum AnimatorParameterType
        {
            Bool,
            Float,
            Integer
        }

        public enum SynchronizationType
        {
            BehaviorDesigner,
            Property,
            Animator,
            PlayMaker,
            uFrame
        }

        [Serializable]
        public class SynchronizedVariable
        {
            public Animator animator;
            public VariableSynchronizer.AnimatorParameterType animatorParameterType;
            public Behavior behavior;
            public Func<object> getDelegate;
            public bool global;
            public Action<object> setDelegate;
            public bool setVariable;
            public SharedVariable sharedVariable;
            public VariableSynchronizer.SynchronizationType synchronizationType;
            public Component targetComponent;
            public bool targetGlobal;
            public int targetID;
            public string targetName;
            public SharedVariable targetSharedVariable;
            public Action<VariableSynchronizer.SynchronizedVariable> thirdPartyTick;
            public object thirdPartyVariable;
            public string variableName;
            public Enum variableType;

            public SynchronizedVariable(VariableSynchronizer.SynchronizationType synchronizationType, bool setVariable, Behavior behavior, string variableName, bool global, Component targetComponent, string targetName, bool targetGlobal)
            {
                this.synchronizationType = synchronizationType;
                this.setVariable = setVariable;
                this.behavior = behavior;
                this.variableName = variableName;
                this.global = global;
                this.targetComponent = targetComponent;
                this.targetName = targetName;
                this.targetGlobal = targetGlobal;
            }
        }
    }
}

