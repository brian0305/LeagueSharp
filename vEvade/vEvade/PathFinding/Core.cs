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
                var innerPolygonList = new List<Geometry.Polygon>();
                var outerPolygonList = new List<Geometry.Polygon>();

                foreach (var spell in Evade.SpellsDetected.Values.Where(i => i.Enable))
                {
                    innerPolygonList.Add(spell.PathFindingInnerPolygon);
                    outerPolygonList.Add(spell.PathFindingPolygon);
                }

                var innerPolygons = Geometry.ClipPolygons(innerPolygonList).ToPolygons();
                var outerPolygons = Geometry.ClipPolygons(outerPolygonList).ToPolygons();

                if (outerPolygons.Aggregate(false, (cur, poly) => cur || !poly.IsOutside(end)))
                {
                    end = end.GetClosestOutsidePoint(outerPolygons);
                }

                if (outerPolygons.Aggregate(false, (cur, poly) => cur || !poly.IsOutside(start)))
                {
                    start = start.GetClosestOutsidePoint(outerPolygons);
                }

                if (start.CanReach(end, innerPolygons))
                {
                    return new List<Vector2> { start, end };
                }

                outerPolygons.Add(new Geometry.Polygon { Points = new List<Vector2> { start, end } });
                var nodes = new List<Node>();

                foreach (var poly in outerPolygons)
                {
                    for (var i = 0; i < poly.Points.Count; i++)
                    {
                        if (poly.Points.Count != 2 && poly.Points.IsConcave(i))
                        {
                            continue;
                        }

                        var node = nodes.FirstOrDefault(a => a.Point == poly.Points[i]) ?? new Node(poly.Points[i]);
                        nodes.Add(node);

                        foreach (var poly2 in outerPolygons)
                        {
                            foreach (var point in poly2.Points)
                            {
                                if (!poly.Points[i].CanReach(point, innerPolygons))
                                {
                                    continue;
                                }

                                var nodeToAdd = nodes.FirstOrDefault(a => a.Point == point) ?? new Node(point);
                                nodes.Add(nodeToAdd);
                                node.Neightbours.Add(nodeToAdd);
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

        private static bool CanReach(this Vector2 start, Vector2 end, List<Geometry.Polygon> polygons)
        {
            if (start == end)
            {
                return false;
            }

            var step = start.Distance(end) / 2;
            var dir = (end - start).Normalized();

            for (var i = 0; i <= 2; i++)
            {
                if ((start + i * step * dir).IsWall())
                {
                    return false;
                }
            }

            foreach (var polygon in polygons)
            {
                for (var i = 0; i < polygon.Points.Count; i++)
                {
                    var a = polygon.Points[i];
                    var b = polygon.Points[i == polygon.Points.Count - 1 ? 0 : i + 1];

                    if (start.LineSegmentsCross(end, a, b))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Path<Node> FindPath(
            this Node start,
            Node destination,
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

                if (path.LastStep.Point.Equals(destination.Point))
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

        private static Vector2 GetClosestOutsidePoint(this Vector2 from, List<Geometry.Polygon> polygons)
        {
            var result = new List<Vector2>();

            foreach (var poly in polygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var sideStart = poly.Points[i];
                    var sideEnd = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    result.Add(from.ProjectOn(sideStart, sideEnd).SegmentPoint);
                }
            }

            return result.MinOrDefault(i => i.Distance(from));
        }

        private static bool IsConcave(this List<Vector2> vertices, int vertex)
        {
            var current = vertices[vertex];
            var next = vertices[(vertex + 1) % vertices.Count];
            var previous = vertices[vertex == 0 ? vertices.Count - 1 : vertex - 1];
            var left = new Vector2(current.X - previous.X, current.Y - previous.Y);
            var right = new Vector2(next.X - current.X, next.Y - current.Y);

            return left.X * right.Y - left.Y * right.X < 0;
        }

        private static bool LineSegmentsCross(this Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            if (denominator.Equals(0))
            {
                return false;
            }

            var num1 = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            var num2 = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);

            if (num1.Equals(0) || num2.Equals(0))
            {
                return false;
            }

            var r = num1 / denominator;
            var s = num2 / denominator;

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

            public void Enqueue(TP priority, TV value)
            {
                Queue<TV> q;

                if (!this.list.TryGetValue(priority, out q))
                {
                    q = new Queue<TV>();
                    this.list.Add(priority, q);
                }

                q.Enqueue(value);
            }

            #endregion
        }
    }
}