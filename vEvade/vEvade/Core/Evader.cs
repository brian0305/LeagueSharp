namespace vEvade.Core
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.EvadeSpells;
    using vEvade.Helpers;
    using vEvade.Managers;
    using vEvade.Spells;

    #endregion

    public static class Evader
    {
        #region Public Methods and Operators

        public static Vector2 GetBestPoint(int speed = -1, int delay = 0)
        {
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var posChecked = 0;
            var radiusIndex = 0;
            var myPos = Evade.PlayerPosition;
            var points = GetFastestPoints(myPos).Select(CheckClose).ToList();

            while (posChecked < 150)
            {
                radiusIndex++;
                var curRadius = radiusIndex * 50;
                var curCircleChecks = (int)Math.Ceiling(2 * Math.PI * curRadius / 50);

                for (var i = 1; i < curCircleChecks; i++)
                {
                    posChecked++;
                    var cRadians = 2 * Math.PI / (curCircleChecks - 1) * i;
                    points.Add(
                        CheckClose(
                            new Vector2(
                                (float)Math.Floor(myPos.X + curRadius * Math.Cos(cRadians)),
                                (float)Math.Floor(myPos.Y + curRadius * Math.Sin(cRadians)))));
                }
            }

            return
                points.Where(i => !CheckPath(i))
                    .Select(i => GetExtendedSafePoint(myPos, i))
                    .Where(
                        i =>
                        (i.IsPathSafe(Configs.EvadingFirstTime, speed, delay).IsSafe
                         || i.IsPathSafe(Configs.EvadingSecondTime, speed, delay).IsSafe) && !i.IsWallBetween())
                    .OrderBy(i => !i.To3D().UnderTurret(true))
                    .ThenBy(i => i.Distance(Game.CursorPos))
                    .FirstOrDefault();
        }

        public static Vector2 GetBestPointBlink(int delay, float range)
        {
            var posChecked = 0;
            var radiusIndex = 0;
            var myPos = Evade.PlayerPosition;
            var points = new List<Vector2>();

            while (posChecked < 100)
            {
                radiusIndex++;
                var curRadius = radiusIndex * 100;
                var curCircleChecks = (int)Math.Ceiling(2 * Math.PI * curRadius / 100);

                for (var i = 1; i < curCircleChecks; i++)
                {
                    posChecked++;
                    var cRadians = 2 * Math.PI / (curCircleChecks - 1) * i;
                    points.Add(
                        CheckClose(
                            new Vector2(
                                (float)Math.Floor(myPos.X + curRadius * Math.Cos(cRadians)),
                                (float)Math.Floor(myPos.Y + curRadius * Math.Sin(cRadians)))));
                }

                if (curRadius >= range)
                {
                    break;
                }
            }

            for (var i = 0; i < points.Count; i++)
            {
                var k = (int)(range - myPos.Distance(points[i]));
                k -= Util.Random.Next(k);
                var extend = points[i] + k * (points[i] - myPos).Normalized();

                if (extend.IsPointSafe().IsSafe)
                {
                    points[i] = extend;
                }
            }

            return
                points.Where(
                    i =>
                    !CheckPoint(i)
                    && (i.IsPointBlinkSafe(Configs.EvadingFirstTime, delay)
                        || i.IsPointBlinkSafe(Configs.EvadingSecondTime, delay)) && !i.IsWall())
                    .OrderBy(i => !i.To3D().UnderTurret(true))
                    .ThenBy(i => i.Distance(Game.CursorPos))
                    .FirstOrDefault();
        }

        public static Vector2 GetBestPointBlock(Vector3 pos)
        {
            var posChecked = 0;
            var radiusIndex = 0;
            var myPos = Evade.PlayerPosition;
            var points = new List<Vector2>();

            while (posChecked < 50)
            {
                radiusIndex++;
                var curRadius = radiusIndex * 100;
                var curCircleChecks = (int)Math.Ceiling(2 * Math.PI * curRadius / 100);

                for (var i = 1; i < curCircleChecks; i++)
                {
                    posChecked++;
                    var cRadians = 2 * Math.PI / (curCircleChecks - 1) * i;
                    points.Add(
                        CheckClose(
                            new Vector2(
                                (float)Math.Floor(myPos.X + curRadius * Math.Cos(cRadians)),
                                (float)Math.Floor(myPos.Y + curRadius * Math.Sin(cRadians)))));
                }
            }

            return
                points.Where(i => !CheckPath(i) && i.IsPathSafe(Configs.EvadingFirstTime).IsSafe && !i.IsWallBetween())
                    .OrderBy(i => !i.To3D().UnderTurret(true))
                    .ThenBy(i => i.Distance(pos))
                    .FirstOrDefault();
        }

        public static Vector2 GetBestPointDash(int speed, int delay, float range, bool fixedRange = false)
        {
            var posChecked = 0;
            var radiusIndex = 0;
            var myPos = Evade.PlayerPosition;
            var points = new List<Vector2>();
            var minDist = fixedRange ? range : 50;

            while (posChecked < 100)
            {
                radiusIndex++;
                var curRadius = radiusIndex * 100 + (minDist - 100);
                var curCircleChecks = (int)Math.Ceiling(2 * Math.PI * curRadius / 100);

                for (var i = 1; i < curCircleChecks; i++)
                {
                    posChecked++;
                    var cRadians = 2 * Math.PI / (curCircleChecks - 1) * i;
                    points.Add(
                        CheckClose(
                            new Vector2(
                                (float)Math.Floor(myPos.X + curRadius * Math.Cos(cRadians)),
                                (float)Math.Floor(myPos.Y + curRadius * Math.Sin(cRadians)))));
                }

                if (curRadius >= range)
                {
                    break;
                }
            }

            if (fixedRange)
            {
                for (var i = 0; i < points.Count; i++)
                {
                    points[i] = myPos.Extend(points[i], range);
                }

                for (var i = points.Count - 1; i > 0; i--)
                {
                    if (!points[i].IsPointSafe().IsSafe)
                    {
                        points.RemoveAt(i);
                    }
                }
            }
            else
            {
                for (var i = 0; i < points.Count; i++)
                {
                    var k = (int)(range - myPos.Distance(points[i]));
                    k -= Math.Max(Util.Random.Next(k) - 100, 0);
                    var extend = points[i] + k * (points[i] - myPos).Normalized();

                    if (extend.IsPointSafe().IsSafe)
                    {
                        points[i] = extend;
                    }
                }
            }

            return
                points.Where(
                    i =>
                    !CheckPath(i)
                    && (i.IsPathSafe(Configs.EvadingFirstTime, speed, delay).IsSafe
                        || i.IsPathSafe(Configs.EvadingSecondTime, speed, delay).IsSafe) && !i.IsWall())
                    .OrderBy(i => !i.To3D().UnderTurret(true))
                    .ThenBy(i => i.Distance(Game.CursorPos))
                    .FirstOrDefault();
        }

        public static List<Vector2> GetEvadePoints(
            int speed = -1,
            int delay = 0,
            bool isBlink = false,
            bool onlyGood = false)
        {
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var goods = new List<Vector2>();
            var bads = new List<Vector2>();
            var myPos = Evade.PlayerPosition;
            var polygons = new List<Geometry.Polygon>();
            var closestPath = false;

            foreach (var spell in Evade.Spells)
            {
                if (spell.Data.TakeClosestPath && !spell.IsSafePoint(myPos))
                {
                    closestPath = true;
                }

                polygons.Add(spell.EvadePolygon);
            }

            var dangerPolygons = Geometry.ClipPolygons(polygons).ToPolygons();

            foreach (var poly in dangerPolygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var start = poly.Points[i];
                    var end = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    var segment = myPos.ProjectOn(start, end).SegmentPoint;
                    var dist = segment.Distance(myPos);

                    if (dist >= 600)
                    {
                        continue;
                    }

                    var count = dist < 200 && end.Distance(start) > 90 ? Configs.EvadePointsCount : 0;
                    var step = Configs.EvadePointsStep * (end - start).Normalized();

                    for (var j = -count; j <= count; j++)
                    {
                        var pos = segment + j * step;
                        var paths = ObjectManager.Player.GetPath(pos.To3D()).Select(a => a.To2D()).ToList();

                        if (!isBlink)
                        {
                            if (Evade.IsSafePath(paths, Configs.EvadingFirstTime, speed, delay).IsSafe)
                            {
                                goods.Add(pos);
                            }

                            if (Evade.IsSafePath(paths, Configs.EvadingSecondTime, speed, delay).IsSafe && j == 0)
                            {
                                bads.Add(pos);
                            }
                        }
                        else
                        {
                            if (Evade.IsSafeToBlink(paths[paths.Count - 1], Configs.EvadingFirstTime, delay))
                            {
                                goods.Add(pos);
                            }

                            if (Evade.IsSafeToBlink(paths[paths.Count - 1], Configs.EvadingSecondTime, delay))
                            {
                                bads.Add(pos);
                            }
                        }
                    }
                }
            }

            if (closestPath)
            {
                if (goods.Count > 0)
                {
                    goods = new List<Vector2> { goods.OrderBy(i => myPos.Distance(i)).First() };
                }

                if (bads.Count > 0)
                {
                    bads = new List<Vector2> { bads.OrderBy(i => myPos.Distance(i)).First() };
                }
            }

            return goods.Count > 0 ? goods : (onlyGood ? new List<Vector2>() : bads);
        }

        public static List<Obj_AI_Base> GetEvadeTargets(
            SpellValidTargets[] validTargets,
            int speed,
            int delay,
            float range,
            bool isBlink = false,
            bool onlyGood = false,
            bool dontCheckForSafety = false)
        {
            var bads = new List<Obj_AI_Base>();
            var goods = new List<Obj_AI_Base>();
            var alls = new List<Obj_AI_Base>();
            var myPos = Evade.PlayerPosition.To3D();

            foreach (var type in validTargets)
            {
                switch (type)
                {
                    case SpellValidTargets.AllyChampions:
                        alls.AddRange(HeroManager.Allies.Where(i => !i.IsMe && i.IsValidTarget(range, false, myPos)));
                        break;
                    case SpellValidTargets.AllyMinions:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i => i.IsValidTarget(range, false, myPos) && i.IsAlly && (i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.AllyWards:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, false, myPos) && i.IsAlly && i.IsWard()));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        alls.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(range, true, myPos)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i =>
                                    i.IsValidTarget(range, true, myPos) && (i.IsJungle() || i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.EnemyWards:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, true, myPos) && i.IsWard()));
                        break;
                }
            }

            foreach (var target in alls)
            {
                var pos = target.ServerPosition.To2D();

                if (!dontCheckForSafety && !Evade.IsSafePoint(pos).IsSafe)
                {
                    continue;
                }

                if (!isBlink)
                {
                    var paths = new List<Vector2> { myPos.To2D(), pos };

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(paths, Configs.EvadingFirstTime, speed, delay).IsSafe)
                    {
                        goods.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(paths, Configs.EvadingSecondTime, speed, delay).IsSafe)
                    {
                        bads.Add(target);
                    }
                }
                else
                {
                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(pos, Configs.EvadingFirstTime, delay))
                    {
                        goods.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(pos, Configs.EvadingSecondTime, delay))
                    {
                        bads.Add(target);
                    }
                }
            }

            return goods.Count > 0 ? goods : (onlyGood ? new List<Obj_AI_Base>() : bads);
        }

        public static SafePath IsPathSafe(this List<Vector2> paths, int time, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersects = new List<Intersects>();

            foreach (var spell in Evade.Spells)
            {
                var checkPath = spell.IsSafePath(paths, time, speed, delay);
                isSafe = isSafe && checkPath.IsSafe;

                if (checkPath.Intersect.Valid)
                {
                    intersects.Add(checkPath.Intersect);
                }
            }

            return new SafePath(isSafe, isSafe ? new Intersects() : intersects.MinOrDefault(i => i.Distance));
        }

        public static SafePath IsPathSafe(this Vector2 pos, int time, int speed = -1, int delay = 0)
        {
            return ObjectManager.Player.GetPath(pos.To3D()).ToList().To2D().IsPathSafe(time, speed, delay);
        }

        public static bool IsPointBlinkSafe(this Vector2 pos, int time, int delay)
        {
            return Evade.Spells.All(i => i.IsSafeToBlink(pos, time, delay));
        }

        public static SafePoint IsPointSafe(this Vector2 pos)
        {
            return new SafePoint(Evade.Spells.Where(i => !i.IsSafePoint(pos)).ToList());
        }

        #endregion

        #region Methods

        private static Vector2 CheckClose(Vector2 pos)
        {
            return !ObjectManager.Player.IsMoving && Evade.PlayerPosition.Distance(pos) <= 75
                       ? Evade.PlayerPosition
                       : pos;
        }

        private static bool CheckPath(Vector2 pos)
        {
            var paths = ObjectManager.Player.GetPath(pos.To3D());

            return paths.Length > 0 && (pos.Distance(paths[paths.Length - 1]) > 5 || paths.Length > 2);
        }

        private static bool CheckPoint(Vector2 pos)
        {
            var paths = ObjectManager.Player.GetPath(pos.To3D());

            return paths.Length > 0 && pos.Distance(paths[paths.Length - 1]) > 5;
        }

        private static Vector2 GetExtendedSafePoint(Vector2 from, Vector2 to)
        {
            var dir = (to - from).Normalized();
            var lastPos = to;

            for (float i = 50; i <= 100; i += 50)
            {
                var pos = to + dir * i;

                if (CheckPath(pos))
                {
                    return lastPos;
                }

                lastPos = pos;
            }

            return lastPos;
        }

        private static Vector2 GetFastestPoint(Vector2 from, SpellInstance spell)
        {
            switch (spell.Type)
            {
                case SpellType.Line:
                case SpellType.MissileLine:
                    var segment = from.ProjectOn(spell.Line.Start, spell.Line.End);

                    if (segment.IsOnSegment)
                    {
                        return segment.SegmentPoint.Extend(from, spell.Line.Radius + 10);
                    }
                    break;
                case SpellType.Circle:
                    return spell.Circle.Center.Extend(from, spell.Circle.Radius + 10);
            }

            return Vector2.Zero;
        }

        private static List<Vector2> GetFastestPoints(Vector2 from)
        {
            return Evade.Spells.Select(i => GetFastestPoint(from, i)).Where(i => i.IsValid()).ToList();
        }

        private static bool IsWallBetween(this Vector2 point)
        {
            var myPos = Evade.PlayerPosition;
            var dir = point - myPos;

            for (var i = 0f; i <= 1; i += 0.1f)
            {
                if ((myPos + i * dir).IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}