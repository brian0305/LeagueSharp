namespace Valvrave_Sharp.Evade
{
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;

    using SharpDX;

    using Valvrave_Sharp.Core;

    public static class Evader
    {
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
            var polygonList = new List<Geometry.Polygon>();
            var takeClosestPath = false;
            foreach (var skillshot in Evade.DetectedSkillshots.Where(i => i.Enable))
            {
                if (skillshot.SpellData.TakeClosestPath && skillshot.IsDanger(Evade.PlayerPosition))
                {
                    takeClosestPath = true;
                }
                polygonList.Add(skillshot.EvadePolygon);
            }
            var dangerPolygons = Geometry.ClipPolygons(polygonList).ToPolygons();
            var myPosition = Evade.PlayerPosition;
            foreach (var poly in dangerPolygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var sideStart = poly.Points[i];
                    var sideEnd = poly.Points[(i == poly.Points.Count - 1) ? 0 : i + 1];
                    var originalCandidate = myPosition.ProjectOn(sideStart, sideEnd).SegmentPoint;
                    var distanceToEvadePoint = Vector2.DistanceSquared(originalCandidate, myPosition);
                    if (!(distanceToEvadePoint < 600 * 600))
                    {
                        continue;
                    }
                    var sideDistance = Vector2.DistanceSquared(sideEnd, sideStart);
                    var direction = (sideEnd - sideStart).Normalized();
                    var s = (distanceToEvadePoint < 200 * 200 && sideDistance > 90 * 90)
                                ? Config.DiagonalEvadePointsCount
                                : 0;
                    for (var j = -s; j <= s; j++)
                    {
                        var candidate = originalCandidate + j * Config.DiagonalEvadePointsStep * direction;
                        var pathToPoint = ObjectManager.Player.GetPath(candidate.ToVector3()).ToList().ToVector2();
                        if (!isBlink)
                        {
                            if (Evade.IsSafePath(pathToPoint, Config.EvadingFirstTimeOffset, speed, delay).IsSafe)
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (Evade.IsSafePath(pathToPoint, Config.EvadingSecondTimeOffset, speed, delay).IsSafe
                                && j == 0)
                            {
                                badCandidates.Add(candidate);
                            }
                        }
                        else
                        {
                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Config.EvadingFirstTimeOffset,
                                delay))
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Config.EvadingSecondTimeOffset,
                                delay))
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
            SpellValidTargets[] validTargets,
            int speed,
            int delay,
            float range,
            bool isBlink = false,
            bool onlyGood = false,
            bool dontCheckForSafety = false)
        {
            var badTargets = new List<Obj_AI_Base>();
            var goodTargets = new List<Obj_AI_Base>();
            var allTargets = new List<Obj_AI_Base>();
            foreach (var targetType in validTargets)
            {
                switch (targetType)
                {
                    case SpellValidTargets.AllyChampions:
                        allTargets.AddRange(GameObjects.AllyHeroes.Where(i => i.IsValidTarget(range, false) && !i.IsMe));
                        break;
                    case SpellValidTargets.AllyMinions:
                        allTargets.AddRange(
                            GameObjects.AllyMinions.Where(
                                i => i.IsValidTarget(range, false, ObjectManager.Player.Position) && i.IsMinion()));
                        break;
                    case SpellValidTargets.AllyWards:
                        allTargets.AddRange(GameObjects.AllyWards.Where(i => i.IsValidTarget(range, false)));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        allTargets.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(range)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        allTargets.AddRange(
                            GameObjects.EnemyMinions.Where(
                                i => i.IsValidTarget(range, true, ObjectManager.Player.Position) && i.IsMinion()));
                        allTargets.AddRange(
                            GameObjects.Jungle.Where(i => i.IsValidTarget(range, true, ObjectManager.Player.Position)));
                        break;
                    case SpellValidTargets.EnemyWards:
                        allTargets.AddRange(GameObjects.EnemyWards.Where(i => i.IsValidTarget(range)));
                        break;
                }
            }
            foreach (var target in
                allTargets.Where(
                    i =>
                    (dontCheckForSafety || Evade.IsSafePoint(i.ServerPosition.ToVector2()).IsSafe)
                    && (ObjectManager.Player.ChampionName != "Yasuo" || !i.HasBuff("YasuoDashWrapper"))))
            {
                if (isBlink)
                {
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafeToBlink(target.ServerPosition.ToVector2(), Config.EvadingFirstTimeOffset, delay))
                    {
                        goodTargets.Add(target);
                    }
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafeToBlink(target.ServerPosition.ToVector2(), Config.EvadingSecondTimeOffset, delay))
                    {
                        badTargets.Add(target);
                    }
                }
                else
                {
                    var pathToTarget = new List<Vector2> { Evade.PlayerPosition, target.ServerPosition.ToVector2() };
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafePath(pathToTarget, Config.EvadingFirstTimeOffset, speed, delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafePath(pathToTarget, Config.EvadingSecondTimeOffset, speed, delay).IsSafe)
                    {
                        badTargets.Add(target);
                    }
                }
            }
            return goodTargets.Count > 0 ? goodTargets : (onlyGood ? new List<Obj_AI_Base>() : badTargets);
        }

        #endregion
    }
}