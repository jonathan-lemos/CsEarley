using System;

namespace CsEarley.Functional
{
    /// <summary>
    /// A class that optionally holds a value of the given type.
    /// </summary>
    /// <typeparam name="T">The type to optionally hold.</typeparam>
    public class Optional<T>
    {
        private readonly T _val;

        /// <summary>
        /// The value if this optional contains one, or throws InvalidOperationException if not.
        /// </summary>
        public T Value => IsSet ? _val : throw new InvalidOperationException("This Optional does not contain a value.");

        /// <summary>
        /// The value if this optional contains one, or default(T) if not.
        /// </summary>
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

        /// <summary>
        /// Returns the value if it exists, or the given parameter if not.
        /// </summary>
        /// <param name="val">The value to return if this optional does not contain a value.</param>
        /// <returns>The value if it exists, or the given parameter if not.</returns>
        public T ValueOr(T val) => IsSet ? Value : val;

        /// <summary>
        /// Returns the value if it exists, or returns the given function's result if not.
        /// </summary>
        /// <param name="func">The function to call if this optional does not contain a value.</param>
        /// <returns>The value if it exists, or the return of the given function if not.</returns>
        public T ValueOr(Func<T> func) => IsSet ? Value : func();

        public void Match(Action<T> ifSet, Action ifUnset)
        {
            if (IsSet)
            {
                ifSet(Value);
            }
            else
            {
                ifUnset();
            }
        }

        public TRet Match<TRet>(Func<T, TRet> ifSet, Func<TRet> ifUnset) => IsSet ? ifSet(Value) : ifUnset();

        public static implicit operator Optional<T>(T val) => new Optional<T>(val);
        public static explicit operator T(Optional<T> option) => option.Value;
        public static implicit operator bool(Optional<T> option) => option.IsSet;

        public override string ToString() => IsSet ? Value.ToString() : "Optional Empty";
    }
}