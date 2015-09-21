namespace Valvrave_Sharp.Evade
{
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Math.Polygons;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    internal class Evader
    {
        #region Static Fields

        public static int LastWardJumpAttempt = 0;

        #endregion

        #region Public Methods and Operators

        public static List<Vector2> GetEvadePoints(
            int speed = -1,
            int delay = 0,
            bool isBlink = false,
            bool onlyGood = false)
        {
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var goodCandidates = new List<Vector2>();
            var badCandidates = new List<Vector2>();
            var polygonList = new List<Polygon>();
            var takeClosestPath = false;
            foreach (var skillshot in Evade.DetectedSkillshots.Where(i => i.Enabled))
            {
                if (skillshot.SpellData.TakeClosestPath && !skillshot.IsSafePoint(Evade.PlayerPosition))
                {
                    takeClosestPath = true;
                }
                polygonList.Add(skillshot.EvadePolygon);
            }
            var dangerPolygons = MathUtils.ClipPolygons(polygonList).ToPolygons();
            var myPosition = Evade.PlayerPosition;
            foreach (var poly in dangerPolygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var sideStart = poly.Points[i];
                    var sideEnd = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    var originalCandidate = myPosition.ProjectOn(sideStart, sideEnd).SegmentPoint;
                    var distanceToEvadePoint = myPosition.Distance(originalCandidate);
                    if (distanceToEvadePoint >= 600)
                    {
                        continue;
                    }
                    var s = distanceToEvadePoint < 200 && sideEnd.Distance(sideStart) > 90
                                ? Config.DiagonalEvadePointsCount
                                : 0;
                    for (var j = -s; j <= s; j++)
                    {
                        var candidate = originalCandidate
                                        + j * Config.DiagonalEvadePointsStep * (sideEnd - sideStart).Normalized();
                        var pathToPoint =
                            ObjectManager.Player.GetPath(candidate.ToVector3()).Select(a => a.ToVector2()).ToList();
                        if (!isBlink)
                        {
                            if (IsSafePath(pathToPoint, Config.EvadingFirstTimeOffset, speed, delay).IsSafe)
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (IsSafePath(pathToPoint, Config.EvadingSecondTimeOffset, speed, delay).IsSafe && j == 0)
                            {
                                badCandidates.Add(candidate);
                            }
                        }
                        else
                        {
                            if (IsSafeToBlink(pathToPoint[pathToPoint.Count - 1], Config.EvadingFirstTimeOffset, delay))
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (IsSafeToBlink(pathToPoint[pathToPoint.Count - 1], Config.EvadingSecondTimeOffset, delay))
                            {
                                badCandidates.Add(candidate);
                            }
                        }
                    }
                }
            }
            if (takeClosestPath)
            {
                if (goodCandidates.Count > 0)
                {
                    goodCandidates = new List<Vector2>
                                         { goodCandidates.MinOrDefault(i => ObjectManager.Player.DistanceSquared(i)) };
                }
                if (badCandidates.Count > 0)
                {
                    badCandidates = new List<Vector2>
                                        { badCandidates.MinOrDefault(i => ObjectManager.Player.DistanceSquared(i)) };
                }
            }
            return goodCandidates.Count > 0 ? goodCandidates : (onlyGood ? new List<Vector2>() : badCandidates);
        }

        public static List<Obj_AI_Base> GetEvadeTargets(
            SpellTargets[] validTargets,
            int speed,
            int delay,
            float range,
            bool isBlink = false,
            bool onlyGood = false,
            bool dontCheckForSafety = false,
            string buffName = "")
        {
            var badTargets = new List<Obj_AI_Base>();
            var goodTargets = new List<Obj_AI_Base>();
            var allTargets = new List<Obj_AI_Base>();
            foreach (var targetType in validTargets)
            {
                switch (targetType)
                {
                    case SpellTargets.AllyChampions:
                        allTargets.AddRange(GameObjects.AllyHeroes.Where(i => i.IsValidTarget(range, false) && !i.IsMe));
                        break;
                    case SpellTargets.AllyMinions:
                        allTargets.AddRange(
                            GameObjects.AllyMinions.Where(i => i.IsValidTarget(range, false) && i.IsMinion()));
                        break;
                    case SpellTargets.AllyWards:
                        allTargets.AddRange(GameObjects.AllyWards.Where(i => i.IsValidTarget(range, false)));
                        break;
                    case SpellTargets.EnemyChampions:
                        allTargets.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(range)));
                        break;
                    case SpellTargets.EnemyMinions:
                        allTargets.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget(range) && i.IsMinion()));
                        allTargets.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(range)));
                        break;
                    case SpellTargets.EnemyWards:
                        allTargets.AddRange(GameObjects.EnemyWards.Where(i => i.IsValidTarget(range)));
                        break;
                }
            }
            foreach (var target in
                allTargets.Where(
                    i =>
                    (dontCheckForSafety || IsSafePoint(i.ServerPosition.ToVector2()).IsSafe)
                    && (buffName == "" || !i.HasBuff(buffName))))
            {
                if (isBlink)
                {
                    if (Variables.TickCount - LastWardJumpAttempt < 250
                        || IsSafeToBlink(target.ServerPosition.ToVector2(), Config.EvadingFirstTimeOffset, delay))
                    {
                        goodTargets.Add(target);
                    }
                    if (Variables.TickCount - LastWardJumpAttempt < 250
                        || IsSafeToBlink(target.ServerPosition.ToVector2(), Config.EvadingSecondTimeOffset, delay))
                    {
                        badTargets.Add(target);
                    }
                }
                else
                {
                    var pathToTarget = new List<Vector2> { Evade.PlayerPosition, target.ServerPosition.ToVector2() };
                    if (Variables.TickCount - LastWardJumpAttempt < 250
                        || IsSafePath(pathToTarget, Config.EvadingFirstTimeOffset, speed, delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }
                    if (Variables.TickCount - LastWardJumpAttempt < 250
                        || IsSafePath(pathToTarget, Config.EvadingSecondTimeOffset, speed, delay).IsSafe)
                    {
                        badTargets.Add(target);
                    }
                }
            }
            return goodTargets.Count > 0 ? goodTargets : (onlyGood ? new List<Obj_AI_Base>() : badTargets);
        }

        public static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersections = new List<FoundIntersection>();
            var intersection = new FoundIntersection();
            foreach (var sResult in
                Evade.DetectedSkillshots.Where(i => i.Enabled).Select(i => i.IsSafePath(path, timeOffset, speed, delay))
                )
            {
                isSafe = isSafe && sResult.IsSafe;
                if (sResult.Intersection.Valid)
                {
                    intersections.Add(sResult.Intersection);
                }
            }
            if (isSafe)
            {
                return new SafePathResult(true, intersection);
            }
            var inter = intersections.MinOrDefault(i => i.Distance);
            return new SafePathResult(false, inter.Valid ? inter : intersection);
        }

        public static IsSafeResult IsSafePoint(Vector2 point)
        {
            var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
            foreach (var skillshot in Evade.DetectedSkillshots.Where(i => i.Enabled && !i.IsSafePoint(point)))
            {
                result.SkillshotList.Add(skillshot);
            }
            result.IsSafe = result.SkillshotList.Count == 0;
            return result;
        }

        #endregion

        #region Methods

        private static bool IsSafeToBlink(Vector2 point, int timeOffset, int delay)
        {
            return Evade.DetectedSkillshots.Where(i => i.Enabled).All(i => i.IsSafeToBlink(point, timeOffset, delay));
        }

        #endregion

        internal struct IsSafeResult
        {
            #region Fields

            public bool IsSafe;

            public List<Skillshot> SkillshotList;

            #endregion
        }
    }
}