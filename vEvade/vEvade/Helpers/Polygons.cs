namespace vEvade.Helpers
{
    #region

    using System;

    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    #endregion

    public static class Polygons
    {
        #region Constants

        private const int Segment = 28;

        #endregion

        public class Arc
        {
            #region Fields

            public Vector2 End;

            public int Radius;

            public Vector2 Start;

            #endregion

            #region Constructors and Destructors

            public Arc(Vector2 start, Vector2 end, int radius)
            {
                this.Start = start;
                this.End = end;
                this.Radius = radius;
            }

            #endregion

            #region Properties

            private float Distance => this.Start.Distance(this.End);

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int offset = 0)
            {
                var result = new Geometry.Polygon();
                var innerRadius = -0.1562f * this.Distance + 687.31f;
                var outerRadius = 0.35256f * this.Distance + 133f;
                outerRadius = outerRadius / (float)Math.Cos(2 * Math.PI / Segment);
                var innerCenter = Geometry.CircleCircleIntersection(this.Start, this.End, innerRadius, innerRadius)[0];
                var outerCenter = Geometry.CircleCircleIntersection(this.Start, this.End, outerRadius, outerRadius)[0];

                Render.Circle.DrawCircle(innerCenter.To3D(), this.Radius, Color.White);

                var direction = (this.End - outerCenter).Normalized();
                var step = -(float)(direction.AngleBetween((this.Start - outerCenter).Normalized()) * Math.PI / 180)
                           / Segment;

                for (var i = 0; i < Segment; i++)
                {
                    result.Add(outerCenter + (outerRadius + 15 + offset + this.Radius) * direction.Rotated(step * i));
                }

                direction = (this.Start - innerCenter).Normalized();
                step = (float)(direction.AngleBetween((this.End - innerCenter).Normalized()) * Math.PI / 180) / Segment;

                for (var i = 0; i < Segment; i++)
                {
                    result.Add(
                        innerCenter
                        + Math.Max(0, innerRadius - (offset + this.Radius) - 100) * direction.Rotated(step * i));
                }

                return result;
            }

            #endregion
        }

        public class Circle
        {
            #region Fields

            public Vector2 Center;

            public int Radius;

            #endregion

            #region Constructors and Destructors

            public Circle(Vector2 center, int radius)
            {
                this.Center = center;
                this.Radius = radius;
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int offset = 0, float overrideWidth = -1)
            {
                var result = new Geometry.Polygon();
                var outRadius = overrideWidth > 0
                                    ? overrideWidth
                                    : (offset + this.Radius) / (float)Math.Cos(2 * Math.PI / Segment);
                const double Step = 2 * Math.PI / Segment;
                var angle = (double)this.Radius;

                for (var i = 0; i <= Segment; i++)
                {
                    angle += Step;
                    result.Add(
                        new Vector2(
                            this.Center.X + outRadius * (float)Math.Cos(angle),
                            this.Center.Y + outRadius * (float)Math.Sin(angle)));
                }

                return result;
            }

            #endregion
        }

        public class Cone
        {
            #region Fields

            public Vector2 Center;

            public Vector2 Direction;

            public float Radius;

            public int Range;

            #endregion

            #region Constructors and Destructors

            public Cone(Vector2 center, Vector2 direction, int radius, int range)
            {
                this.Center = center;
                this.Direction = direction;
                this.Radius = radius * (float)Math.PI / 180;
                this.Range = range;
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int offset = 0)
            {
                var result = new Geometry.Polygon();
                result.Add(this.Center);
                var outRadius = (this.Range + offset) / (float)Math.Cos(2 * Math.PI / Segment);
                var side = this.Direction.Rotated(-this.Radius * 0.5f);

                for (var i = 0; i <= Segment; i++)
                {
                    var dir = side.Rotated(i * this.Radius / Segment).Normalized();
                    result.Add(new Vector2(this.Center.X + outRadius * dir.X, this.Center.Y + outRadius * dir.Y));
                }

                return result;
            }

            #endregion
        }

        public class Line
        {
            #region Fields

            public Vector2 End;

            public int Radius;

            public Vector2 Start;

            #endregion

            #region Constructors and Destructors

            public Line(Vector2 start, Vector2 end, int radius)
            {
                this.Start = start;
                this.End = end;
                this.Radius = radius;
            }

            #endregion

            #region Properties

            private Vector2 Direction => (this.End - this.Start).Normalized();

            private Vector2 Perpendicular => this.Direction.Perpendicular();

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int offset = 0, float overrideWidth = -1)
            {
                var result = new Geometry.Polygon();
                result.Add(
                    this.Start + (overrideWidth > 0 ? overrideWidth : this.Radius + offset) * this.Perpendicular
                    - offset * this.Direction);
                result.Add(
                    this.Start - (overrideWidth > 0 ? overrideWidth : this.Radius + offset) * this.Perpendicular
                    - offset * this.Direction);
                result.Add(
                    this.End - (overrideWidth > 0 ? overrideWidth : this.Radius + offset) * this.Perpendicular
                    + offset * this.Direction);
                result.Add(
                    this.End + (overrideWidth > 0 ? overrideWidth : this.Radius + offset) * this.Perpendicular
                    + offset * this.Direction);

                return result;
            }

            #endregion
        }

        public class Ring
        {
            #region Fields

            public Vector2 Center;

            public int Radius;

            public int RingRadius;

            #endregion

            #region Constructors and Destructors

            public Ring(Vector2 center, int radius, int ringRadius)
            {
                this.Center = center;
                this.Radius = radius;
                this.RingRadius = ringRadius;
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int offset = 0)
            {
                var result = new Geometry.Polygon();
                var outRadius = (offset + this.Radius + this.RingRadius) / (float)Math.Cos(2 * Math.PI / Segment);
                var innerRadius = this.Radius - this.RingRadius - offset;

                for (var i = 0; i <= Segment; i++)
                {
                    var angle = i * 2 * Math.PI / Segment;
                    result.Add(
                        new Vector2(
                            this.Center.X - outRadius * (float)Math.Cos(angle),
                            this.Center.Y - outRadius * (float)Math.Sin(angle)));
                }

                for (var i = 0; i <= Segment; i++)
                {
                    var angle = i * 2 * Math.PI / Segment;
                    result.Add(
                        new Vector2(
                            this.Center.X + innerRadius * (float)Math.Cos(angle),
                            this.Center.Y - innerRadius * (float)Math.Sin(angle)));
                }

                return result;
            }

            #endregion
        }
    }
}