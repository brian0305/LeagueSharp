namespace vEvade.Helpers
{
    #region

    using System;

    using LeagueSharp.Common;

    using SharpDX;

    #endregion

    public static class Polygons
    {
        #region Constants

        private const int Quality = 22;

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