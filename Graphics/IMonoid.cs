using System;
using System.Collections.Generic;
using System.Linq;

namespace Graphics
{
    public interface IMonoid<T>
    {
        T Null { get; }
        T Join(T first, T second);
    }

    public sealed class Monoid<T> : IMonoid<T>
    {
        private readonly Func<T, T, T> JoinF;

        public Monoid(T @null, Func<T, T, T> joinF)
        {
            Null = @null;
            JoinF = joinF;
        }

        public T Null { get; private set; }

        public T Join(T first, T second)
        {
            return this.JoinF(first, second);
        }
    }

    public static class Monoids
    {
        public static T Fold<T>(this IEnumerable<T> sequence, IMonoid<T> monoid)
        {
            return sequence.Aggregate(monoid.Null, monoid.Join);
        }

        public static T Power<T>(this IMonoid<T> monoid, T element, uint exponent)
        {
            return monoid.Join(exponent.Repeat(element));
        }

        public static T Join<T>(this IMonoid<T> monoid, IArray<T> array)
        {
            switch (array.Size)
            {
                case 0: return monoid.Null;
                case 1: return array[0];
                default:
                    var midpoint = array.Size / 2;

                    var left = array.Range(0, midpoint);
                    var right = array.Range(midpoint, array.Size);

                    return monoid.Join(monoid.Join(left), monoid.Join(right));
            }
        }

        public static IMonoid<T> Nullable<T>(this IMonoid<T> monoid)
            where T : class 
        {
            return new Monoid<T>(
                @null: null,
                joinF: (left, right) =>
                {
                    if (left == null)
                    {
                        return right;
                    }

                    if (right == null)
                    {
                        return left;
                    }

                    return monoid.Join(left, right);
                });
        }
    }
}