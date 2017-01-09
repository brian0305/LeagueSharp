namespace vEvade.Helpers
{
    #region

    using System;
    using System.Collections.Generic;

    using LeagueSharp.Common;

    using SharpDX;

    #endregion

    public static class Polygons
    {
        #region Constants

        private const int Quality = 22;

        #endregion

        #region Public Methods and Operators

        public static List<Vector2> GetIntersectPointsLine(this Geometry.Polygon poly, Vector2 start, Vector2 end)
        {
            var points = new List<Vector2>();

            for (var i = 0; i < poly.Points.Count; i++)
            {
                var inter = poly.Points[i].Intersection(poly.Points[i != poly.Points.Count - 1 ? i + 1 : 0], start, end);

                if (inter.Intersects)
                {
                    points.Add(inter.Point);
                }
            }

            return points;
        }

        public static Vector2[] GetIntersectPointsLineCircle(this Vector2 pos, float radius, Vector2 start, Vector2 end)
        {
            float t;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var a = dx * dx + dy * dy;
            var b = 2 * (dx * (start.X - pos.X) + dy * (start.Y - pos.Y));
            var c = (start.X - pos.X) * (start.X - pos.X) + (start.Y - pos.Y) * (start.Y - pos.Y) - radius * radius;
            var det = b * b - 4 * a * c;

            if (a <= 0.0000001 || det < 0)
            {
                return new Vector2[] { };
            }

            if (det.Equals(0))
            {
                t = -b / (2 * a);

                return new[] { new Vector2(start.X + t * dx, start.Y + t * dy) };
            }

            t = (float)((-b + Math.Sqrt(det)) / (2 * a));
            var t2 = (float)((-b - Math.Sqrt(det)) / (2 * a));

            return new[]
                       {
                           new Vector2(start.X + t * dx, start.Y + t * dy),
                           new Vector2(start.X + t2 * dx, start.Y + t2 * dy)
                       };
        }

        #endregion

        public class Arc
        {
            #region Fields

            public readonly float Distance;

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
                this.Distance = this.Start.Distance(this.End);
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int extraRadius = 0)
            {
                extraRadius += this.Radius;
                var result = new Geometry.Polygon();
                var outerRadius = (0.35256f * this.Distance + 133f) / (float)Math.Cos(2 * Math.PI / Quality);
                var innerRadius = -0.1562f * this.Distance + 687.31f;
                var outerCenter = Geometry.CircleCircleIntersection(this.Start, this.End, outerRadius, outerRadius)[0];
                var innerCenter = Geometry.CircleCircleIntersection(this.Start, this.End, innerRadius, innerRadius)[0];

                var dir = (this.End - outerCenter).Normalized();
                var step = -(float)(dir.AngleBetween((this.Start - outerCenter).Normalized()) * Math.PI / 180) / Quality;

                for (var i = 0; i < Quality; i++)
                {
                    result.Add(outerCenter + (outerRadius + 15 + extraRadius) * dir.Rotated(step * i));
                }

                dir = (this.Start - innerCenter).Normalized();
                step = (float)(dir.AngleBetween((this.End - innerCenter).Normalized()) * Math.PI / 180) / Quality;

                for (var i = 0; i < Quality; i++)
                {
                    result.Add(innerCenter + Math.Max(0, innerRadius - extraRadius - 100) * dir.Rotated(step * i));
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

            public Geometry.Polygon ToPolygon(int extraRadius = 0, float overrideRadius = -1)
            {
                var result = new Geometry.Polygon();
                const double Step = 2 * Math.PI / Quality;
                var radius = overrideRadius > 0 ? overrideRadius : (extraRadius + this.Radius) / (float)Math.Cos(Step);
                var angle = (double)this.Radius;

                for (var i = 0; i <= Quality; i++)
                {
                    angle += Step;
                    result.Add(
                        new Vector2(
                            this.Center.X + radius * (float)Math.Cos(angle),
                            this.Center.Y + radius * (float)Math.Sin(angle)));
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

            public Geometry.Polygon ToPolygon(int extraRadius = 0)
            {
                var result = new Geometry.Polygon();
                result.Add(this.Center);
                var radius = (this.Range + extraRadius) / (float)Math.Cos(2 * Math.PI / Quality);
                var side = this.Direction.Rotated(-this.Radius * 0.5f);

                for (var i = 0; i <= Quality; i++)
                {
                    var dir = side.Rotated(i * this.Radius / Quality).Normalized();
                    result.Add(new Vector2(this.Center.X + radius * dir.X, this.Center.Y + radius * dir.Y));
                }

                return result;
            }

            #endregion
        }

        public class Line
        {
            #region Fields

            public readonly Vector2 Direction;

            public readonly Vector2 Perpendicular;

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
                this.Direction = (this.End - this.Start).Normalized();
                this.Perpendicular = this.Direction.Perpendicular();
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int extraRadius = 0, float overrideRadius = -1)
            {
                var result = new Geometry.Polygon();
                var radius = (overrideRadius > 0 ? overrideRadius : this.Radius + extraRadius) * this.Perpendicular;
                var dir = extraRadius * this.Direction;
                result.Add(this.Start + radius - dir);
                result.Add(this.Start - radius - dir);
                result.Add(this.End - radius + dir);
                result.Add(this.End + radius + dir);

                return result;
            }

            #endregion
        }

        public class Ring
        {
            #region Fields

            public Vector2 Center;

            public int InnerRadius;

            public int OuterRadius;

            #endregion

            #region Constructors and Destructors

            public Ring(Vector2 center, int innerRadius, int outerRadius)
            {
                this.Center = center;
                this.InnerRadius = innerRadius;
                this.OuterRadius = outerRadius;
            }

            #endregion

            #region Public Methods and Operators

            public Geometry.Polygon ToPolygon(int extraRadius = 0)
            {
                var result = new Geometry.Polygon();
                const double Step = 2 * Math.PI / Quality;
                var outerRadius = (extraRadius + this.OuterRadius) / (float)Math.Cos(Step);
                var innerRadius = (this.InnerRadius - extraRadius) / (float)Math.Cos(Step);

                var outerAngle = (double)this.OuterRadius;

                for (var i = 0; i <= Quality; i++)
                {
                    outerAngle += Step;
                    result.Add(
                        new Vector2(
                            this.Center.X + outerRadius * (float)Math.Cos(outerAngle),
                            this.Center.Y + outerRadius * (float)Math.Sin(outerAngle)));
                }

                var innerAngle = (double)this.InnerRadius;

                for (var i = 0; i <= Quality; i++)
                {
                    innerAngle += Step;
                    result.Add(
                        new Vector2(
                            this.Center.X + innerRadius * (float)Math.Cos(innerAngle),
                            this.Center.Y + innerRadius * (float)Math.Sin(innerAngle)));
                }

                return result;
            }

            #endregion
        }
    }
}