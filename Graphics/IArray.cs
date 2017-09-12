using System;
using System.Collections.Generic;

namespace Graphics
{
    public interface IArray<out T>
    {
        uint Size { get; }
        T this[uint index] { get; }
    }

    public sealed class Array<T> : IArray<T>
    {
        private readonly Func<uint, T> AtF;

        public Array(uint size, Func<uint, T> atF)
        {
            Size = size;
            AtF = atF;
        }

        public uint Size { get; private set; }

        public T this[uint index]
        {
            get { return this.AtF(index); }
        }
    }

    public static class Arrays
    {
        public static IArray<B> Fmap<A, B>(this IArray<A> array, Func<A, B> convert)
        {
            return new Array<B>(
                size: array.Size,
                atF: index => convert(array[index]));
        }

        public static IArray<uint> Identity(this uint size)
        {
            return new Array<uint>(
                size: size, 
                atF: index => index);
        }

        public static IEnumerable<T> Each<T>(this IArray<T> array)
        {
            for (uint index = 0U; index < array.Size; index++)
            {
                yield return array[index];
            }
        }

        public static IEnumerable<uint> Indices<T>(this IArray<T> array)
        {
            return array.Size.Identity().Each();
        }

        public static T[] Concrete<T>(this IArray<T> array)
        {
            var elements = new T[array.Size];

            foreach (var index in array.Indices())
            {
                elements[index] = array[index];
            }

            return elements;
        }

        public static IArray<T> Abstract<T>(this T[] elements)
        {
            return new Array<T>(
                size: (uint) elements.Length,
                atF: index => elements[index]);
        }

        public static IArray<T> Repeat<T>(this uint size, T constant)
        {
            return new Array<T>(
                size: size,
                atF: index => constant);
        }

        public static IArray<T> Reverse<T>(this IArray<T> array)
        {
            return new Array<T>(
                size: array.Size,
                atF: index => array[index.Complement(array.Size)]);
        }

        public static uint Complement(this uint index, uint size)
        {
            if (index < size)
            {
                return size - (index + 1);
            }

            throw new ArgumentException("Index is not within the bounds [0, " + size + "[");
        }

        public static IArray<T> Range<T>(this IArray<T> array, uint start, uint end)
        {
            if (end < start)
            {
                throw new ArgumentException("Invalid range!");
            }

            if (array.Size < end)
            {
                throw new ArgumentException("Range out of bounds!");
            }

            return new Array<T>(
                size: end - start,
                atF: index => array[start + index]);
        }
    }
}