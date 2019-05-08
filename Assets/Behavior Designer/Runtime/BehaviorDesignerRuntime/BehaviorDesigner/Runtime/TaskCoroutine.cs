namespace BehaviorDesigner.Runtime
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public class TaskCoroutine
    {
        private UnityEngine.Coroutine mCoroutine;
        private IEnumerator mCoroutineEnumerator;
        private string mCoroutineName;
        private Behavior mParent;
        private bool mStop;

        public TaskCoroutine(Behavior parent, IEnumerator coroutine, string coroutineName)
        {
            this.mParent = parent;
            this.mCoroutineEnumerator = coroutine;
            this.mCoroutineName = coroutineName;
            this.mCoroutine = parent.StartCoroutine(this.RunCoroutine());
        }

        [DebuggerHidden]
        public IEnumerator RunCoroutine()
        {
            return new <RunCoroutine>c__Iterator1 { <>f__this = this };
        }

        public void Stop()
        {
            this.mStop = true;
        }

        public UnityEngine.Coroutine Coroutine
        {
            get
            {
                return this.mCoroutine;
            }
        }

        [CompilerGenerated]
        private sealed class <RunCoroutine>c__Iterator1 : IEnumerator, IDisposable, IEnumerator<object>
        {
            internal object $current;
            internal int $PC;
            internal TaskCoroutine <>f__this;

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
                        if (!this.<>f__this.mStop)
                        {
                            if ((this.<>f__this.mCoroutineEnumerator == null) || !this.<>f__this.mCoroutineEnumerator.MoveNext())
                            {
                                break;
                            }
                            this.$current = this.<>f__this.mCoroutineEnumerator.Current;
                            this.$PC = 1;
                            return true;
                        }
                        break;

                    default:
                        goto Label_00AF;
                }
                this.<>f__this.mParent.TaskCoroutineEnded(this.<>f__this, this.<>f__this.mCoroutineName);
                this.$PC = -1;
            Label_00AF:
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
    }
}

