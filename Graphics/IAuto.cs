using System;

namespace Graphics
{
    public interface IAuto<T>
    {
        T Morph(T element);
    }

    public sealed class Auto<T> : IAuto<T>
    {
        private readonly Func<T, T> MorphF;

        public Auto(Func<T, T> morphF)
        {
            this.MorphF = morphF;
        }

        public T Morph(T element)
        {
            return this.MorphF(element);
        }
    }

    public static class Automorph
    {
        public static IAuto<T> Power<T>(this IAuto<T> auto, uint exponent)
        {
            return Automorphism<T>().Power(auto, exponent);
        }

        public static IMonoid<IAuto<T>> Automorphism<T>()
        {
            return new Monoid<IAuto<T>>(
                @null: new Auto<T>(x => x),
                joinF: (f, g) =>
                {
                    return new Auto<T>(morphF: x => g.Morph(f.Morph(x)));
                });
        }
    }
}
