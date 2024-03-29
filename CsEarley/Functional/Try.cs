using System;

namespace CsEarley.Functional
{
    public class Try<T, TException> where TException : Exception
    {
        private readonly T _val;
        private readonly TException _ex;
        public readonly bool IsSuccess;
        public readonly bool IsFailure;

        public Try(T value)
        {
            (_val, IsSuccess, IsFailure) = (value, true, false);
        }

        public Try(TException ex)
        {
            (_ex, IsSuccess, IsFailure) = (ex, false, true);
        }

        public T Value => 
            IsSuccess ? _val : throw new InvalidOperationException("This Try does not contain a value.");
        public TException Exception =>
            IsFailure ? _ex : throw new InvalidOperationException("This Try does not contain an exception.");

        
        public T ValueOr(T val) => IsSuccess ? Value : val;
        public T ValueOr(Func<T> func) => IsSuccess ? Value : func();

        // C# is shit and does not allow void in {T}
        // as such we had to make this
        // for some reason void returns cannot be used in ternaries either
        public void Match(Action<T> onSuccess, Action<TException> onFailure)
        {
            if (IsSuccess)
            {
                onSuccess(Value);
            }
            else
            {
                onFailure(Exception);
            }
        }
        public TRes Match<TRes>(Func<T, TRes> onSuccess, Func<TException, TRes> onFailure) =>
            IsSuccess ? onSuccess(Value) : onFailure(Exception);
        
        public static implicit operator Try<T, TException>(T value) => new Try<T, TException>(value);
        public static implicit operator Try<T, TException>(TException exception) => new Try<T, TException>(exception);

        public static explicit operator T(Try<T, TException> tryObj) => tryObj.Value;
        public static explicit operator TException(Try<T, TException> tryObj) => tryObj.Exception;

        public override string ToString() =>
            IsSuccess ? $"Success({Value})" : $"Failure({Exception})";
    }

    public class ValuedException<T> : Exception
    {
        public readonly T Value;

        public ValuedException(string msg, T value) : base(msg)
        {
            Value = value;
        }
    }
}