
using System.IO;

namespace Ratcow.Muscle.Support
{
    using static TypeConstants;

    public class Point : Flattenable
    {
        /// Our data members, exposed for convenience
        public float X { get; set; }
        public float Y { get; set; }

        /// Default constructor, sets the point to be (0, 0) */
        public Point()
        {
            X = 0.0f;
            Y = 0.0f;
        }

        public Point(float x, float y)
        {
            Set(x, y);
        }

        public override Flattenable Clone()
        {
            return new Point(X, Y);
        }

        public override void SetEqualTo(Flattenable setFromMe)
        {
            if (setFromMe is Point p)
            {
                Set(p.X, p.Y);
            }
        }

        public void Set(float X, float Y)
        {
            this.X = X; this.Y = Y;
        }

        /// <summary>
        /// If the point is outside the rectangle specified by the two arguments,
        /// it will be moved horizontally and/or vertically until it falls 
        /// inside the rectangle.
        /// </summary>
        /// <param name="topLeft">Minimum values acceptable for X and Y</param>
        /// <param name="bottomRight">Maximum values acceptable for X and Y</param>
        ///
        public void ConstrainTo(Point topLeft, Point bottomRight)
        {
            if (X < topLeft.X)
                X = topLeft.X;
            if (Y < topLeft.Y)
                Y = topLeft.Y;
            if (X > bottomRight.X)
                X = bottomRight.X;
            if (Y > bottomRight.Y)
                Y = bottomRight.Y;
        }

        public override string ToString()
        {
            return "Point: " + X + " " + Y;
        }

        public Point Add(Point rhs)
        {
            return new Point(X + rhs.X, Y + rhs.Y);
        }

        public Point Subtract(Point rhs)
        {
            return new Point(X - rhs.X, Y - rhs.Y);
        }

        public void AddToThis(Point p)
        {
            X += p.X;
            Y += p.Y;
        }

        public void SubtractFromThis(Point p)
        {
            X -= p.X;
            Y -= p.Y;
        }

        public override bool Equals(object o)
        {
            if (o is Point p)
            {
                return ((X == p.X) && (Y == p.Y));
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Returns true.
        /// </summary>
        public override bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns B_POINT_TYPE.
        /// </summary>
        public override int TypeCode
        {
            get { return B_POINT_TYPE; }
        }

        /// <summary>
        /// Returns 8 (2*sizeof(float))
        /// </summary>
        public override int FlattenedSize
        {
            get { return 8; }
        }

        public override void Flatten(BinaryWriter writer)
        {
            writer.Write((float)X);
            writer.Write((float)Y);
        }

        /// <summary> 
        /// Returns true iff (code) is B_POINT_TYPE.
        /// </summary>
        public override bool AllowsTypeCode(int code)
        {
            return (code == B_POINT_TYPE);
        }

        public override void Unflatten(BinaryReader reader, int numBytes)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
        }
    }
}
