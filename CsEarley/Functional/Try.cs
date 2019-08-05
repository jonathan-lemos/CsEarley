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
            _val = value;
            IsSuccess = true;
            IsFailure = false;
        }

        public Try(TException ex)
        {
            _ex = ex;
            IsSuccess = false;
            IsFailure = true;
        }

        public T Value => 
            IsSuccess ? _val : throw new InvalidOperationException("This Try does not contain a value.");
        public TException Exception =>
            IsFailure ? _ex : throw new InvalidOperationException("This Try does not contain an exception.");

        public TRes Match<TRes>(Func<T, TRes> onSuccess, Func<TException, TRes> onFailure) =>
            IsSuccess ? onSuccess(Value) : onFailure(Exception);
        
        public static implicit operator Try<T, TException>(T value) => new Try<T, TException>(value);
        public static implicit operator Try<T, TException>(TException exception) => new Try<T, TException>(exception);

        public static explicit operator T(Try<T, TException> tryObj) => tryObj.Value;
        public static explicit operator TException(Try<T, TException> tryObj) => tryObj.Exception;

        public override string ToString() =>
            IsSuccess ? $"Success({Value})" : $"Failure({Exception})";
    }
}