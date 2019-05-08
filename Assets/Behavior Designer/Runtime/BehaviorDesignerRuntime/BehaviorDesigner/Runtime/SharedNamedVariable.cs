namespace BehaviorDesigner.Runtime
{
    using System;

    [Serializable]
    public class SharedNamedVariable : SharedVariable<NamedVariable>
    {
        public SharedNamedVariable()
        {
            base.mValue = new NamedVariable();
        }

        public static implicit operator SharedNamedVariable(NamedVariable value)
        {
            return new SharedNamedVariable { mValue = value };
        }
    }
}

