namespace vEvade.PathFinding
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Core;

    #endregion

    public static class Core
    {
        #region Public Methods and Operators

        public static List<Vector2> FindPaths(Vector2 start, Vector2 end)
        {
            var result = new List<Vector2>();

            try
            {
                var outerPolys = new List<Geometry.Polygon>();
                var innerPolys = new List<Geometry.Polygon>();

                foreach (var spell in Evade.Spells)
                {
                    outerPolys.Add(spell.PathFindingOuterPolygon);
                    innerPolys.Add(spell.PathFindingInnerPolygon);
                }

                var outerPolygons = Geometry.ClipPolygons(outerPolys).ToPolygons();
                var innerPolygons = Geometry.ClipPolygons(innerPolys).ToPolygons();

                if (outerPolygons.Aggregate(false, (cur, poly) => cur || poly.IsInside(end)))
                {
                    end = end.GetClosestOutsidePoint(outerPolygons);
                }

                if (outerPolygons.Aggregate(false, (cur, poly) => cur || poly.IsInside(start)))
                {
                    start = start.GetClosestOutsidePoint(outerPolygons);
                }

                if (start.CanReach(end, innerPolygons))
                {
                    return new List<Vector2> { start, end };
                }

                outerPolygons.Add(new Geometry.Polygon { Points = new List<Vector2> { start, end } });
                var nodes = new List<Node>();

                foreach (var poly1 in outerPolygons)
                {
                    for (var i = 0; i < poly1.Points.Count; i++)
                    {
                        if (poly1.Points.Count != 2 && poly1.Points.IsConcave(i))
                        {
                            continue;
                        }

                        var node1 = nodes.FirstOrDefault(a => a.Point == poly1.Points[i]) ?? new Node(poly1.Points[i]);
                        nodes.Add(node1);

                        foreach (var poly2 in outerPolygons)
                        {
                            foreach (var point in poly2.Points)
                            {
                                if (!poly1.Points[i].CanReach(point, innerPolygons))
                                {
                                    continue;
                                }

                                var node2 = nodes.FirstOrDefault(a => a.Point == point) ?? new Node(point);
                                nodes.Add(node2);
                                node1.Neightbours.Add(node2);
                            }
                        }
                    }
                }

                var startNode = nodes.FirstOrDefault(i => i.Point == start);
                var endNode = nodes.FirstOrDefault(i => i.Point == end);

                if (endNode == null)
                {
                    return result;
                }

                Func<Node, Node, double> dist = (node1, node2) => node1.Point.Distance(node2.Point);
                Func<Node, double> eta = t => t.Point.Distance(endNode.Point);
                var path = startNode.FindPath(endNode, dist, eta);

                if (path == null)
                {
                    return result;
                }

                result.AddRange(path.Select(i => i.Point));
                result.Reverse();
            }
            catch (Exception ex)
            {
                Console.WriteLine("=> vEvade [Find Paths]: " + ex);
            }

            return result;
        }

        #endregion

        #region Methods

        private static bool CanReach(this Vector2 start, Vector2 end, List<Geometry.Polygon> polys)
        {
            if (start == end)
            {
                return false;
            }

            var step = start.Distance(end) / 2 * (end - start).Normalized();

            for (var i = 0; i <= 2; i++)
            {
                if ((start + i * step).IsWall())
                {
                    return false;
                }
            }

            foreach (var poly in polys)
            {
                for (var i = 0; i < poly.Points.Count; i++)
                {
                    if (start.IsCross(end, poly.Points[i], poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Path<Node> FindPath(
            this Node start,
            Node dest,
            Func<Node, Node, double> dist,
            Func<Node, double> eta)
        {
            var closed = new HashSet<Vector2>();
            var queue = new PriorityQueue<double, Path<Node>>();
            queue.Enqueue(0, new Path<Node>(start));

            while (!queue.IsEmpty)
            {
                var path = queue.Dequeue();

                if (closed.Contains(path.LastStep.Point))
                {
                    continue;
                }

                if (path.LastStep.Point.Equals(dest.Point))
                {
                    return path;
                }

                closed.Add(path.LastStep.Point);

                foreach (var node in path.LastStep.Neightbours)
                {
                    var newPath = path.AddStep(node, dist(path.LastStep, node));
                    queue.Enqueue(newPath.TotalCost + eta(node), newPath);
                }
            }

            return null;
        }

        private static Vector2 GetClosestOutsidePoint(this Vector2 from, List<Geometry.Polygon> polys)
        {
            var result = new List<Vector2>();

            foreach (var poly in polys)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    result.Add(
                        from.ProjectOn(poly.Points[i], poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1]).SegmentPoint);
                }
            }

            return result.Count > 0 ? result.OrderBy(i => i.Distance(from)).First() : Vector2.Zero;
        }

        private static bool IsConcave(this List<Vector2> v, int id)
        {
            var cur = v[id];
            var next = v[(id + 1) % v.Count];
            var prev = v[id == 0 ? v.Count - 1 : id - 1];
            var left = new Vector2(cur.X - prev.X, cur.Y - prev.Y);
            var right = new Vector2(next.X - cur.X, next.Y - cur.Y);

            return left.X * right.Y - left.Y * right.X < 0;
        }

        private static bool IsCross(this Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var f = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (f.Equals(0f))
            {
                return false;
            }

            var num1 = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            var num2 = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            if (num1.Equals(0f) || num2.Equals(0f))
            {
                return false;
            }

            var r = num1 / f;
            var s = num2 / f;

            return r > 0 && r < 1 && s > 0 && s < 1;
        }

        #endregion

        public class Node
        {
            #region Fields

            public List<Node> Neightbours;

            public Vector2 Point;

            #endregion

            #region Constructors and Destructors

            public Node(Vector2 point)
            {
                this.Point = point;
                this.Neightbours = new List<Node>();
            }

            #endregion
        }

        public class PriorityQueue<TP, TV>
        {
            #region Fields

            private readonly SortedDictionary<TP, Queue<TV>> list = new SortedDictionary<TP, Queue<TV>>();

            #endregion

            #region Public Properties

            public bool IsEmpty => !this.list.Any();

            #endregion

            #region Public Methods and Operators

            public TV Dequeue()
            {
                var pair = this.list.First();
                var deq = pair.Value.Dequeue();

                if (pair.Value.Count == 0)
                {
                    this.list.Remove(pair.Key);
                }

                return deq;
            }

            public void Enqueue(TP p, TV v)
            {
                Queue<TV> q;

                if (!this.list.TryGetValue(p, out q))
                {
                    q = new Queue<TV>();
                    this.list.Add(p, q);
                }

                q.Enqueue(v);
            }

            #endregion
        }
    }
}