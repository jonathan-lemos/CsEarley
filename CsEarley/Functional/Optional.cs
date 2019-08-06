using System;

namespace CsEarley.Functional
{
    public class Optional<T>
    {
        private readonly T _val;
        public T Value => IsSet ? _val : throw new InvalidOperationException("This Optional does not contain a value.");
        public T ValueOrDefault => IsSet ? Value : default(T);
        
        public readonly bool IsSet;

        public Optional(T val)
        {
            if (val == null)
            {
                throw new ArgumentException("Optionals cannot contain null values.");
            }

            (_val, IsSet) = (val, true);
        }

        public Optional()
        {
            (_val, IsSet) = (default(T), false);
        }
        
        public TR Match<TR>(Func<T, TR> ifSet, Func<TR> ifUnset) => IsSet ? ifSet(Value) : ifUnset();

        public static implicit operator Optional<T>(T val) => new Optional<T>(val);
        public static explicit operator T(Optional<T> option) => option.Value;
        public static implicit operator bool(Optional<T> option) => option.IsSet;

        public override string ToString() => IsSet ? $"Optional({Value})" : "Optional Empty";
    }
}