using System;

namespace CsEarley.Functional
{
    public struct Optional<T>
    {
        private readonly T _val;
        public T Get => IsSet ? _val : throw new InvalidOperationException("This Optional does not contain a value.");
        public readonly bool IsSet;

        public Optional(T val)
        {
            _val = val;
            IsSet = true;
        }

        public TR Match<TR>(Func<T, TR> ifSet, Func<TR> ifUnset) => IsSet ? ifSet(Get) : ifUnset();
        public TR Match<TR>(Func<T, TR> ifSet, TR ifUnset) => IsSet ? ifSet(Get) : ifUnset;
        public T GetOrDefault(T def = default(T)) => IsSet ? Get : def;

        public static implicit operator Optional<T>(T val) => new Optional<T>(val);
        public static explicit operator T(Optional<T> option) => option.Get;
        public static implicit operator bool(Optional<T> option) => option.IsSet;

        public override string ToString() => IsSet ? $"Optional({Get})" : "Optional Empty";
    }
}