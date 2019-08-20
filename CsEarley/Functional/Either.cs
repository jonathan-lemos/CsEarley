using System;

namespace CsEarley.Functional
{
    /// <summary>
    /// A class that holds one of two types.
    /// </summary>
    /// <typeparam name="TLeft">The first type.</typeparam>
    /// <typeparam name="TRight">The second type.</typeparam>
    public class Either<TLeft, TRight>
    {
        private readonly TLeft _left;

        /// <summary>
        /// The left value if present, or InvalidOperationException if not. Use <see cref="Either.IsLeft"/> to see if the left value is present.
        /// </summary>
        public TLeft Left =>
            IsLeft ? _left : throw new InvalidOperationException("This Either does not contain a Left value.");


        private readonly TRight _right;

        /// <summary>
        /// The right value if present, or InvalidOperationException if not. Use <see cref="Either.IsLeft"/> to see if the right value is present.
        /// </summary>
        public TRight Right =>
            IsRight ? _right : throw new InvalidOperationException("This Either does not contain a Right value.");

        /// <summary>
        /// True if the left value is present, false if not.
        /// </summary>
        public readonly bool IsLeft;

        /// <summary>
        /// True if the right value is present, false if not.
        /// </summary>
        public readonly bool IsRight;

        public Either(TLeft left)
        {
            (_left, IsLeft, IsRight) = (left, true, false);
        }

        public Either(TRight right)
        {
            (_right, IsLeft, IsRight) = (right, false, true);
        }

        /// <summary>
        /// Returns the left value if it exists, or the given parameter if not.
        /// </summary>
        /// <param name="val">The value to return if the left value is not present.</param>
        /// <returns>The left value or the given parameter.</returns>
        public TLeft LeftOr(TLeft val) => IsLeft ? Left : val;

        /// <summary>
        /// Returns the left value if it exists, or calls the given function if not.
        /// </summary>
        /// <param name="func">The function to call if the left value is not present.</param>
        /// <returns>The left value or the return of the given function.</returns>       
        public TLeft LeftOr(Func<TLeft> func) => IsLeft ? Left : func();

        /// <summary>
        /// Returns the right value if it exists, or the given parameter if not.
        /// </summary>
        /// <param name="val">The value to return if the right value is not present.</param>
        /// <returns>The right value or the given parameter.</returns>
        public TRight RightOr(TRight val) => IsRight ? Right : val;

        /// <summary>
        /// Returns the right value if it exists, or calls the given function if not.
        /// </summary>
        /// <param name="func">The function to call if the right value is not present.</param>
        /// <returns>The right value or the return of the given function.</returns>       
        public TRight RightOr(Func<TRight> func) => IsRight ? Right : func();

        /// <summary>
        /// Calls one of two functions depending on the value this <see cref="Either"/> holds.
        /// </summary>
        /// <param name="ifLeft">The function called if the left value is present.</param>
        /// <param name="ifRight">The function called if the right value is present.</param>
        public void Match(Action<TLeft> ifLeft, Action<TRight> ifRight)
        {
            if (IsLeft)
            {
                ifLeft(Left);
            }
            else
            {
                ifRight(Right);
            }
        }

        /// <summary>
        /// Calls one of two functions depending on the value this <see cref="Either"/> holds and returns the result.
        /// </summary>
        /// <param name="ifLeft">The function called if the left value is present.</param>
        /// <param name="ifRight">The function called if the right value is present.</param>
        public T Match<T>(Func<TLeft, T> ifLeft, Func<TRight, T> ifRight) => IsLeft ? ifLeft(Left) : ifRight(Right);

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);
        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

        public static explicit operator TLeft(Either<TLeft, TRight> entry) => entry.Left;
        public static explicit operator TRight(Either<TLeft, TRight> entry) => entry.Right;

        public override string ToString() => IsLeft ? $"Either<Left>({Left})" : $"Either<Right>({Right})";
    }
}