using System;

namespace CsEarley.Functional
{
    public class Either<TLeft, TRight>
    {
        private readonly TLeft _left;

        public TLeft Left =>
            IsLeft ? _left : throw new InvalidOperationException("This Either does not contain a Left value.");


        private readonly TRight _right;

        public TRight Right =>
            IsRight ? _right : throw new InvalidOperationException("This Either does not contain a Right value.");

        public readonly bool IsLeft;
        public readonly bool IsRight;

        public Either(TLeft left)
        {
            _left = left;
            _right = default(TRight);
            IsLeft = true;
            IsRight = false;
        }

        public Either(TRight right)
        {
            _left = default(TLeft);
            _right = right;
            IsLeft = false;
            IsRight = true;
        }

        public T Match<T>(Func<TLeft, T> ifLeft, Func<TRight, T> ifRight) => IsLeft ? ifLeft(Left) : ifRight(Right);
        public TLeft LeftOrDefault(TLeft def = default(TLeft)) => IsLeft ? Left : def;
        public TRight RightOrDefault(TRight def = default(TRight)) => IsRight ? Right : def;

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);
        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public static explicit operator TLeft(Either<TLeft, TRight> entry) => entry.Left;
        public static explicit operator TRight(Either<TLeft, TRight> entry) => entry.Right;

        public override string ToString() => IsLeft ? $"Either<Left>({Left})" : $"Either<Right>({Right})";
    }
}