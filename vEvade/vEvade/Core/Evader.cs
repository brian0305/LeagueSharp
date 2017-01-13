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

        public static Vector2 GetBestPoint(Vector2 pos, int speed = -1, int delay = 0)
        {
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var posChecked = 0;
            var radiusIndex = 0;
            var myPos = Evade.PlayerPosition;
            var points = myPos.GetFastestPoints().Select(CheckClose).ToList();

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
                    .Select(i => i.GetExtendedSafePoint(myPos))
                    .Where(
                        i =>
                        (i.IsPathSafe(Configs.EvadingFirstTime, speed, delay).IsSafe
                         || i.IsPathSafe(Configs.EvadingSecondTime, speed, delay).IsSafe) && !i.IsWallBetween(myPos))
                    .OrderPoint(pos);
        }

        public static Vector2 GetBestPointBlink(Vector2 pos, int delay, float range)
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

            return
                points.Where(
                    i =>
                    !CheckPoint(i)
                    && (i.IsPointBlinkSafe(Configs.EvadingFirstTime, delay)
                        || i.IsPointBlinkSafe(Configs.EvadingSecondTime, delay)) && !i.IsWall()).OrderPoint(pos);
        }

        public static Vector2 GetBestPointBlock(Vector2 pos)
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
                points.Where(
                    i => !CheckPath(i) && i.IsPathSafe(Configs.EvadingFirstTime).IsSafe && !i.IsWallBetween(myPos))
                    .OrderPoint(pos);
        }

        public static Vector2 GetBestPointDash(Vector2 pos, int speed, int delay, float range, bool fixedRange = false)
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

            return
                points.Where(
                    i =>
                    !CheckPath(i)
                    && (i.IsPathSafe(Configs.EvadingFirstTime, speed, delay).IsSafe
                        || i.IsPathSafe(Configs.EvadingSecondTime, speed, delay).IsSafe) && !i.IsWall()).OrderPoint(pos);
        }

        public static Obj_AI_Base GetBestTarget(Vector2 pos, EvadeSpellData evadeSpell)
        {
            var myPos = Evade.PlayerPosition.To3D();
            var targets = new List<Obj_AI_Base>();
            var result = new List<Tuple<Obj_AI_Base, Vector2>>();
            var delay = evadeSpell.IsInvulnerability ? 0 : evadeSpell.Delay;

            foreach (var type in evadeSpell.ValidTargets)
            {
                switch (type)
                {
                    case SpellValidTargets.AllyChampions:
                        targets.AddRange(
                            HeroManager.Allies.Where(i => !i.IsMe && i.IsValidTarget(evadeSpell.MaxRange, false, myPos)));
                        break;
                    case SpellValidTargets.AllyMinions:
                        targets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i =>
                                    i.IsValidTarget(evadeSpell.MaxRange, false, myPos) && i.IsAlly
                                    && (i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.AllyWards:
                        targets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i => i.IsValidTarget(evadeSpell.MaxRange, false, myPos) && i.IsAlly && i.IsWard()));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        targets.AddRange(
                            HeroManager.Enemies.Where(i => i.IsValidTarget(evadeSpell.MaxRange, true, myPos)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        targets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i =>
                                    i.IsValidTarget(evadeSpell.MaxRange, true, myPos)
                                    && (i.IsJungle() || i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.EnemyWards:
                        targets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(evadeSpell.MaxRange, true, myPos) && i.IsWard()));
                        break;
                }
            }

            foreach (var target in targets)
            {
                var end = target.ServerPosition.To2D();

                if (evadeSpell.MenuName == "YasuoE" && target.HasBuff("YasuoDashWrapper"))
                {
                    continue;
                }

                if (evadeSpell.FixedRange)
                {
                    end = myPos.To2D().Extend(end, evadeSpell.MaxRange);
                }

                if (!evadeSpell.IsInvulnerability && !end.IsPointSafe().IsSafe)
                {
                    continue;
                }

                var canAdd = Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                             || (evadeSpell.IsDash
                                     ? end.IsPathSafe(Configs.EvadingFirstTime, evadeSpell.Speed, delay).IsSafe
                                       || end.IsPathSafe(Configs.EvadingSecondTime, evadeSpell.Speed, delay).IsSafe
                                     : end.IsPointBlinkSafe(Configs.EvadingFirstTime, delay)
                                       || end.IsPointBlinkSafe(Configs.EvadingSecondTime, delay));

                if (canAdd)
                {
                    result.Add(new Tuple<Obj_AI_Base, Vector2>(target, end));
                }
            }

            return result.OrderTarget(pos);
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

        private static Vector2 GetExtendedSafePoint(this Vector2 to, Vector2 from)
        {
            var dir = (to - from).Normalized();
            var pos = to;

            for (float i = 50; i <= 100; i += 50)
            {
                var newPos = to + dir * i;

                if (CheckPath(newPos))
                {
                    return pos;
                }

                pos = newPos;
            }

            return pos;
        }

        private static Vector2 GetFastestPoint(this Vector2 pos, SpellInstance spell)
        {
            switch (spell.Type)
            {
                case SpellType.Line:
                case SpellType.MissileLine:
                    var segment = pos.ProjectOn(spell.Line.Start, spell.Line.End);

                    if (segment.IsOnSegment)
                    {
                        return segment.SegmentPoint.Extend(pos, spell.Line.Radius + 10);
                    }
                    break;
                case SpellType.Circle:
                    return spell.Circle.Center.Extend(pos, spell.Circle.Radius + 10);
            }

            return Vector2.Zero;
        }

        private static List<Vector2> GetFastestPoints(this Vector2 pos)
        {
            return Evade.Spells.Select(i => pos.GetFastestPoint(i)).Where(i => i.IsValid()).ToList();
        }

        private static bool IsWallBetween(this Vector2 to, Vector2 from)
        {
            var dir = (to - from).Normalized();

            for (var i = 0f; i <= 1; i += 0.1f)
            {
                if ((from + i * dir).IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 OrderPoint(this IEnumerable<Vector2> points, Vector2 pos)
        {
            return
                points.OrderBy(i => !i.To3D().UnderTurret(true))
                    .ThenBy(i => i.To3D().CountEnemiesInRange(200))
                    .ThenBy(i => i.Distance(pos))
                    .FirstOrDefault();
        }

        private static Obj_AI_Base OrderTarget(this IEnumerable<Tuple<Obj_AI_Base, Vector2>> targets, Vector2 pos)
        {
            return
                targets.OrderBy(i => !i.Item2.To3D().UnderTurret(true))
                    .ThenBy(i => i.Item2.To3D().CountEnemiesInRange(200))
                    .ThenBy(i => i.Item2.Distance(pos))
                    .Select(i => i.Item1)
                    .FirstOrDefault();
        }

        #endregion
    }
}