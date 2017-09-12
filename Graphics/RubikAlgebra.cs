using System;
using System.Collections.Generic;

namespace Graphics
{
    public struct CubieFace2
    {
        public readonly Arrow Cubie;
        public readonly Axis Face;

        public CubieFace2(Arrow cubie, Axis face)
        {
            Cubie = cubie;
            Face = face;

            if (this.Cubie[Face] == Sign.Zero)
            {
                throw new ArgumentException("Face must be on one of the outer edges of the cube.");
            }
        }
    }

    public sealed class RubikCube2
    {
        private readonly Colors[,,,] Table = new Colors[3, 3, 3, 3];

        public Colors this[CubieFace2 face]
        {
            get
            {
                return this.Table[(int)face.Face, (int)face.Cubie.X + 1, (int)face.Cubie.Y + 1, (int)face.Cubie.Z + 1];
            }
        }
    }

    public struct CubieFace
    {
        public readonly Arrow Cubie;
        public readonly Arrow Face;

        public CubieFace(Arrow cubie, Arrow face)
        {
            this.Cubie = cubie;
            this.Face = face;

            this.Check();
        }

        public override string ToString()
        {
            return this.Cubie + ":" + this.Face;
        }
    }

    public struct FaceTurn
    {
        public readonly Arrow Axis;

        public FaceTurn(Arrow axis)
        {
            axis.CheckUnit();

            this.Axis = axis;
        }
    }

    public interface IRubik
    {
        Colors this[CubieFace face] { get; }
    }

    public sealed class RubikCube : IRubik
    {
        private readonly Func<CubieFace, Colors> GetF;

        public RubikCube(Func<CubieFace, Colors> getF)
        {
            this.GetF = getF;
        }

        public Colors this[CubieFace face]
        {
            get
            {
                return this.GetF(face);
            }
        }
    }
    
    public static class RubikAlgebra
    {
        public static IRubik InitialCube
        {
            get
            {
                return new RubikCube(
                    getF: face =>
                    {
                        return InitialColor(face.Face);
                    });
            }
        }

        public static Colors InitialColor(this Arrow face)
        {
            if (face.Z == Sign.Negative)
            {
                return Colors.Green;
            }

            if (face.Z == Sign.Positive)
            {
                return Colors.Blue;
            }

            if (face.X == Sign.Negative)
            {
                return Colors.Orange;
            }

            if (face.X == Sign.Positive)
            {
                return Colors.Red;
            }

            if (face.Y == Sign.Negative)
            {
                return Colors.Yellow;
            }

            if (face.Y == Sign.Positive)
            {
                return Colors.White;
            }

            throw new ArgumentException("Incorrect cube face.");
        }

        public static IEnumerable<Arrow> Inverse(Arrow axis)
        {
            yield return axis;
            yield return axis;
            yield return axis;
        }

        public static IAuto<CubieFace> Turn(this FaceTurn turn)
        {
            var rotation = turn.Axis.Rotate();

            return new Auto<CubieFace>(
                morphF: arrows =>
                {
                    if (arrows.Cubie.Dot(turn.Axis) > 0)
                    {
                        return new CubieFace(
                            cubie: rotation.Morph(arrows.Cubie),
                            face: rotation.Morph(arrows.Face));
                    }

                    return arrows;
                });
        }

        public static IAuto<IRubik> TurnCube(this FaceTurn turn)
        {
            return new Auto<IRubik>(
                morphF: cube =>
                {
                    return new RubikCube(
                        getF: face =>
                        {
                            return cube[turn.Turn().Morph(face)];
                        });
                });
        }

        public static CubieFace Neighbour(this CubieFace face, Arrow direction)
        {
            face.Check(direction);

            if (face.Cubie.Dot(direction) > 0)
            {
                return new CubieFace(face.Cubie, direction);
            }

            var cubie = face.Cubie.Add(direction);

            return new CubieFace(cubie, face.Face);
        }
        
        public static Arrow ReOrient(this CubieFace face, Arrow direction)
        {
            face.Check(direction);

            if (face.Cubie.Dot(direction) > 0)
            {
                return face.Face.Negate();
            }

            return direction;
        }

        public static Arrow ReOrient(this CubieFace face, Arrow direction, uint turn)
        {
            return face.Face.Rotate().Power(turn).Morph(direction);
        }

        public static CubieFace Follow(this CubieFace face, Arrow direction, params uint[] turns)
        {
            foreach (var turn in turns)
            {
                var neighbour = face.Neighbour(direction);
                var next = face.ReOrient(direction);
                var turned = neighbour.ReOrient(next, turn);

                face = neighbour;
                direction = turned;
            }

            return face;
        }

        public static CubieFace Loop(this CubieFace face, Arrow direction)
        {
            return face.Follow(direction, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public static void Check(this CubieFace face)
        {
            face.Cubie.CheckNonZero();

            face.Face.CheckUnit();

            face.Cubie.CheckParallel(face.Face);
        }

        public static void Check(this CubieFace face, Arrow direction)
        {
            direction.CheckUnit();

            face.Face.CheckPerpendicular(direction);
        }
    }
}
