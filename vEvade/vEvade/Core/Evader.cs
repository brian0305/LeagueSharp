namespace vEvade.Core
{
    #region

    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.EvadeSpells;
    using vEvade.Helpers;
    using vEvade.Managers;

    #endregion

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

            foreach (var spell in Evade.SpellsDetected.Values.Where(i => i.Enable))
            {
                if (spell.Data.TakeClosestPath && spell.IsDanger(Evade.PlayerPosition))
                {
                    takeClosestPath = true;
                }

                polygonList.Add(spell.EvadePolygon);
            }

            var dangerPolygons = Geometry.ClipPolygons(polygonList).ToPolygons();
            var myPosition = Evade.PlayerPosition;

            foreach (var poly in dangerPolygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var sideStart = poly.Points[i];
                    var sideEnd = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    var dir = (sideEnd - sideStart).Normalized();
                    var originalCandidate = myPosition.ProjectOn(sideStart, sideEnd).SegmentPoint;
                    var distToEvadePoint = originalCandidate.Distance(myPosition, true);

                    if (distToEvadePoint >= 600 * 600)
                    {
                        continue;
                    }

                    var s = distToEvadePoint < 200 * 200 && sideEnd.Distance(sideStart, true) > 90 * 90
                                ? Configs.DiagonalEvadePointsCount
                                : 0;

                    for (var j = -s; j <= s; j++)
                    {
                        var candidate = originalCandidate + j * Configs.DiagonalEvadePointsStep * dir;
                        var pathToPoint = ObjectManager.Player.GetPath(candidate.To3D()).ToList().To2D();

                        if (!isBlink)
                        {
                            if (Evade.IsSafePath(pathToPoint, Configs.EvadingFirstTimeOffset, speed, delay).IsSafe)
                            {
                                goodCandidates.Add(candidate);
                            }

                            if (Evade.IsSafePath(pathToPoint, Configs.EvadingSecondTimeOffset, speed, delay).IsSafe
                                && j == 0)
                            {
                                badCandidates.Add(candidate);
                            }
                        }
                        else
                        {
                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Configs.EvadingFirstTimeOffset,
                                delay))
                            {
                                goodCandidates.Add(candidate);
                            }

                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Configs.EvadingSecondTimeOffset,
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
                                         { goodCandidates.MinOrDefault(i => myPosition.Distance(i, true)) };
                }

                if (badCandidates.Count > 0)
                {
                    badCandidates = new List<Vector2> { badCandidates.MinOrDefault(i => myPosition.Distance(i, true)) };
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

            foreach (var type in validTargets)
            {
                switch (type)
                {
                    case SpellValidTargets.AllyChampions:
                        allTargets.AddRange(HeroManager.Allies.Where(i => !i.IsMe && i.IsValidTarget(range, false)));
                        break;
                    case SpellValidTargets.AllyMinions:
                        allTargets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, false) && i.IsAlly && (i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.AllyWards:
                        allTargets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, false) && i.IsAlly && i.IsWard()));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        allTargets.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(range)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        allTargets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range) && (i.IsJungle() || i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.EnemyWards:
                        allTargets.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget(range) && i.IsWard()));
                        break;
                }
            }

            foreach (var target in allTargets)
            {
                if (!dontCheckForSafety && !Evade.IsSafePos(target.ServerPosition.To2D()).IsSafe)
                {
                    continue;
                }

                if (isBlink)
                {
                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(target.ServerPosition.To2D(), Configs.EvadingFirstTimeOffset, delay))
                    {
                        goodTargets.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(target.ServerPosition.To2D(), Configs.EvadingSecondTimeOffset, delay))
                    {
                        badTargets.Add(target);
                    }
                }
                else
                {
                    var pathToTarget = new List<Vector2> { Evade.PlayerPosition, target.ServerPosition.To2D() };

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(pathToTarget, Configs.EvadingFirstTimeOffset, speed, delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(pathToTarget, Configs.EvadingSecondTimeOffset, speed, delay).IsSafe)
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