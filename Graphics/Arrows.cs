using System;

namespace Graphics
{
    public interface IArrow
    {
        Sign this[Axis axis] { get; }
    }

    public struct Arrow : IArrow, IEquatable<Arrow>
    {
        public readonly Sign X;
        public readonly Sign Y;
        public readonly Sign Z;

        public Arrow(Sign x, Sign y, Sign z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", this.X.Show(), this.Y.Show(), this.Z.Show());
        }

        public bool Equals(Arrow other)
        {
            return this.X.Equals(other.X) && this.Y.Equals(other.Y) && this.Z.Equals(other.Z);
        }

        public Sign this[Axis axis]
        {
            get
            {
                switch (axis)
                {
                    case Axis.X:
                        return this.X;
                    case Axis.Y:
                        return this.Y;
                    case Axis.Z:
                        return this.Z;
                    default:
                        throw new ArgumentOutOfRangeException("axis", axis, null);
                }
            }
        }
    }

    public static class Arrows
    {
        public static Arrow AsArrow(this Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return new Arrow(Sign.Positive, Sign.Zero, Sign.Zero);
                case Axis.Y:
                    return new Arrow(Sign.Zero, Sign.Positive, Sign.Zero);
                case Axis.Z:
                    return new Arrow(Sign.Zero, Sign.Zero, Sign.Positive);
                default:
                    throw new ArgumentOutOfRangeException("axis", axis, null);
            }
        }

        public static Arrow Negate(this Arrow arrow)
        {
            return new Arrow(arrow.X.Negate(), arrow.Y.Negate(), arrow.Z.Negate());
        }
        
        public static Arrow Scale(this Arrow arrow, Sign sign)
        {
            return new Arrow(arrow.X.Multiply(sign), arrow.Y.Multiply(sign), arrow.Z.Multiply(sign));
        }

        public static Arrow Add(this Arrow left, Arrow right)
        {
            return new Arrow(left.X.Add(right.X), left.Y.Add(right.Y), left.Z.Add(right.Z));
        }

        public static int Magnitude(this Arrow arrow)
        {
            return arrow.Dot(arrow);
        }

        public static int Dot(this Arrow left, Arrow right)
        {
            return (int)left.X.Multiply(right.X) + (int)left.Y.Multiply(right.Y) + (int)left.Z.Multiply(right.Z);
        }

        public static Arrow Cross(this Arrow left, Arrow right)
        {
            var x = Determinant(new Sign[2, 2] { 
              { left.Y, left.Z },
              { right.Y, right.Z },
            });

            var y = Determinant(new Sign[2, 2] { 
              { left.Z, left.X },
              { right.Z, right.X },
            });
            
            var z = Determinant(new Sign[2, 2] { 
              { left.X, left.Y },
              { right.X, right.Y },
            });

            return new Arrow(x, y, z);
        }

        public static Sign Determinant(this Sign[,] matrix)
        {
            var diagonal = matrix[0, 0].Multiply(matrix[1, 1]);
            var antidiagonal = matrix[1, 0].Multiply(matrix[0, 1]);

            return diagonal.Add(antidiagonal.Negate());
        }
        
        public static IAuto<Arrow> Rotate(this Arrow axis)
        {
            axis.CheckUnit();

            return new Auto<Arrow>(
                morphF: arrow =>
                {
                    var projection = axis.Scale((Sign)axis.Dot(arrow));

                    return axis.Cross(arrow).Add(projection);
                });
        }

        public static void CheckParallel(this Arrow first, Arrow second)
        {
            if (first.Dot(second) <= 0)
            {
                throw new ArgumentException("Arrows must point in the same general direction.");
            }
        }

        public static void CheckPerpendicular(this Arrow first, Arrow second)
        {
            if (first.Dot(second) != 0)
            {
                throw new ArgumentException("Arrows must be perpendicular.");
            }
        }

        public static void CheckNonZero(this Arrow arrow)
        {
            if (arrow.Magnitude() == 0)
            {
                throw new ArgumentException("Arrow must be non-zero.");
            }
        }

        public static void CheckUnit(this Arrow arrow)
        {
            if (arrow.Magnitude() != 1)
            {
                throw new ArgumentException("Arrow must be a unit vector.");
            }
        }
    }
}
