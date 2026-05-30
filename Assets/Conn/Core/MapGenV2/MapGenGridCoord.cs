using System;

namespace Conn.MapGenV2.Core
{
    public readonly struct MapGenGridCoord : IEquatable<MapGenGridCoord>
    {
        public MapGenGridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public static MapGenGridCoord Zero => new MapGenGridCoord(0, 0);

        public static bool IsValidSize(int width, int height)
        {
            return width > 0 && height > 0;
        }

        public bool IsInBounds(int width, int height)
        {
            return X >= 0 && Y >= 0 && X < width && Y < height;
        }

        public int ToIndex(int width)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            }

            return (Y * width) + X;
        }

        public static MapGenGridCoord FromIndex(int index, int width)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index cannot be negative.");
            }

            return new MapGenGridCoord(index % width, index / width);
        }

        public MapGenGridCoord Offset(MapGenGridDirection direction)
        {
            return direction switch
            {
                MapGenGridDirection.North => new MapGenGridCoord(X, Y + 1),
                MapGenGridDirection.East => new MapGenGridCoord(X + 1, Y),
                MapGenGridDirection.South => new MapGenGridCoord(X, Y - 1),
                MapGenGridDirection.West => new MapGenGridCoord(X - 1, Y),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public bool Equals(MapGenGridCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is MapGenGridCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(MapGenGridCoord left, MapGenGridCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MapGenGridCoord left, MapGenGridCoord right)
        {
            return !left.Equals(right);
        }
    }
}
