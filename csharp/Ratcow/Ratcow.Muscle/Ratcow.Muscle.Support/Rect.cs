
using System.IO;

namespace Ratcow.Muscle.Support
{
    using Constants;
    using static Constants.TypeConstants;

    public class Rect : Flattenable
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        /// <summary>
        /// Creates a rectangle with upper left point (0,0), and 
        /// lower right point (-1,-1).
        /// Note that this rectangle has a negative area!
        /// (that is, it's imaginary)
        /// </summary>
        public Rect()
        {
            Left = 0.0f;
            Top = 0.0f;
            Right = -1.0f;
            Bottom = -1.0f;
        }

        public Rect(float l, float t, float r, float b)
        {
            Set(l, t, r, b);
        }

        public Rect(Rect rhs)
        {
            SetEqualTo(rhs);
        }

        public Rect(Point leftTop, Point rightBottom)
        {
            Set(leftTop.X, leftTop.Y, rightBottom.X, rightBottom.Y);
        }

        public override Flattenable Clone()
        {
            return new Rect(Left, Top, Right, Bottom);
        }

        public override void SetEqualTo(Flattenable rhs)
        {
            Rect copyMe = (Rect)rhs;
            Set(copyMe.Left, copyMe.Top, copyMe.Right, copyMe.Bottom);
        }

        public void Set(float l, float t, float r, float b)
        {
            Left = l;
            Top = t;
            Right = r;
            Bottom = b;
        }

        public override string ToString()
        {
            return "Rect: leftTop=(" + Left + "," + Top + ") rightBottom=(" + Right + "," + Bottom + ")";
        }


        public Point LeftTop()
        {
            return new Point(Left, Top);
        }

        public Point RightBottom()
        {
            return new Point(Right, Bottom);
        }

        public Point LeftBottom()
        {
            return new Point(Left, Bottom);
        }

        public Point RightTop()
        {
            return new Point(Right, Top);
        }

        public void SetLeftTop(Point p)
        {
            Left = p.X;
            Top = p.Y;
        }

        public void SetRightBottom(Point p)
        {
            Right = p.X;
            Bottom = p.Y;
        }

        public void SetLeftBottom(Point p)
        {
            Left = p.X;
            Bottom = p.Y;
        }

        public void SetRightTop(Point p)
        {
            Right = p.X;
            Top = p.Y;
        }

        /// <summary>
        /// Makes the rectangle smaller by the amount specified in both 
        /// the x and y dimensions
        /// </summary>
        public void InsetBy(Point p)
        {
            InsetBy(p.X, p.Y);
        }

        /// <summary>
        /// Makes the rectangle smaller by the amount specified in 
        /// both the x and y dimensions
        /// </summary>
        public void InsetBy(float dx, float dy)
        {
            Left += dx;
            Top += dy;
            Right -= dx;
            Bottom -= dy;
        }

        /// <summary>
        /// Translates the rectangle by the amount specified in both the x and y 
        /// dimensions
        /// </summary>
        public void OffsetBy(Point p)
        {
            OffsetBy(p.X, p.Y);
        }

        /// <summary>
        /// Translates the rectangle by the amount specified in both 
        /// the x and y dimensions
        /// </summary>
        public void OffsetBy(float dx, float dy)
        {
            Left += dx;
            Top += dy;
            Right += dx;
            Bottom += dy;
        }

        /// <summary>
        /// Translates the rectangle so that its top left corner is at the 
        /// point specified.
        /// </summary>
        public void OffsetTo(Point p)
        {
            OffsetTo(p.X, p.Y);
        }

        /// <summary>
        /// Translates the rectangle so that its top left corner is at 
        /// the point specified.
        /// </summary>
        public void OffsetTo(float x, float y)
        {
            Right = x + Width();
            Bottom = y + Height();
            Left = x;
            Top = y;
        }

        /// <summary>
        /// Comparison Operator.  Returns true iff (r)'s dimensions are 
        /// exactly the same as this rectangle's.
        /// </summary>
        public override bool Equals(object o)
        {
            if (o is Rect r)
            {
                return (Left == r.Left) && (Top == r.Top) && (Right == r.Right) && (Bottom == r.Bottom);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Returns a rectangle whose area is the intersecting subset of 
        /// this rectangle's and (r)'s
        /// </summary>
        public Rect Intersect(Rect r)
        {
            var result = new Rect(this);

            if (result.Left < r.Left)
            {
                result.Left = r.Left;
            }

            if (result.Right > r.Right)
            {
                result.Right = r.Right;
            }

            if (result.Top < r.Top)
            {
                result.Top = r.Top;
            }

            if (result.Bottom > r.Bottom)
            {
                result.Bottom = r.Bottom;
            }

            return result;
        }

        /// <summary> 
        /// Returns a rectangle whose area is a superset of the union of 
        /// this rectangle's and (r)'s
        /// </summary>
        public Rect Unify(Rect r)
        {
            var result = new Rect(this);

            if (r.Left < result.Left)
            {
                result.Left = r.Left;
            }

            if (r.Right > result.Right)
            {
                result.Right = r.Right;
            }

            if (r.Top < result.Top)
            {
                result.Top = r.Top;
            }

            if (r.Bottom > result.Bottom)
            {
                result.Bottom = r.Bottom;
            }

            return result;
        }

        public bool Intersects(Rect r)
        {
            return (r.Intersect(this).IsValid());
        }

        public bool IsValid()
        {
            return ((Width() >= 0.0f) && (Height() >= 0.0f));
        }

        public float Width()
        {
            return Right - Left;
        }

        public float Height()
        {
            return Bottom - Top;
        }

        public bool Contains(Point p)
        {
            return ((p.X >= Left) && (p.X <= Right) && (p.Y >= Top) && (p.Y <= Bottom));
        }

        public bool Contains(Rect p)
        {
            return ((Contains(p.LeftTop())) && (Contains(p.RightTop())) &&
                (Contains(p.LeftBottom())) && (Contains(p.RightBottom())));
        }

        public override bool IsFixedSize
        {
           get { return true; }
        }

        public override TypeConstants TypeCode
        {
           get { return B_RECT_TYPE; }
        }

        public override int FlattenedSize
        {
            get { return 16; }
        }

        /// <summary>
        /// Returns true only if code is B_RECT_TYPE.
        /// </summary>
        public override bool AllowsTypeCode(TypeConstants code)
        {
            return (code == B_RECT_TYPE);
        }

        public override void Flatten(BinaryWriter writer)
        {
            writer.Write(Left);
            writer.Write(Top);
            writer.Write(Right);
            writer.Write(Bottom);
        }

        public override void Unflatten(BinaryReader reader, int numBytes)
        {
            Left = reader.ReadSingle();
            Top = reader.ReadSingle();
            Right = reader.ReadSingle();
            Bottom = reader.ReadSingle();
        }
    }
}
