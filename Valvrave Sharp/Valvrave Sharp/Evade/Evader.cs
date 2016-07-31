namespace Valvrave_Sharp.Evade
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    #endregion

    internal static class Evader
    {
        #region Methods

        internal static Vector3 GetEvadePoint(this EvadeSpellData spellData, float overideRange = -1)
        {
            var goodCandidates = new List<Vector2>();
            var badCandidates = new List<Vector2>();
            var polygonList = new List<Geometry.Polygon>();
            var takeClosestPath = false;
            foreach (var skillshot in Evade.DetectedSkillshots.Where(i => i.Enable))
            {
                if (skillshot.SpellData.TakeClosestPath && !skillshot.IsSafe(Evade.PlayerPosition))
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
                    var sideEnd = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    var originalCandidate = myPosition.ProjectOn(sideStart, sideEnd).SegmentPoint;
                    var distanceToEvadePoint = originalCandidate.DistanceSquared(myPosition);
                    if (!(distanceToEvadePoint < 600 * 600))
                    {
                        continue;
                    }
                    var sideDistance = sideEnd.DistanceSquared(sideStart);
                    var direction = (sideEnd - sideStart).Normalized();
                    var s = distanceToEvadePoint < 200 * 200 && sideDistance > 90 * 90
                                ? Config.DiagonalEvadePointsCount
                                : 0;
                    for (var j = -s; j <= s; j++)
                    {
                        var candidate = originalCandidate + j * Config.DiagonalEvadePointsStep * direction;
                        var pathToPoint = Program.Player.GetPath(candidate.ToVector3()).ToList().ToVector2();
                        if (spellData.IsBlink)
                        {
                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Config.EvadingFirstTimeOffset,
                                spellData.Delay))
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (Evade.IsSafeToBlink(
                                pathToPoint[pathToPoint.Count - 1],
                                Config.EvadingSecondTimeOffset,
                                spellData.Delay))
                            {
                                badCandidates.Add(candidate);
                            }
                        }
                        else
                        {
                            if (
                                Evade.IsSafePath(
                                    pathToPoint,
                                    Config.EvadingFirstTimeOffset,
                                    spellData.Speed,
                                    spellData.Delay).IsSafe)
                            {
                                goodCandidates.Add(candidate);
                            }
                            if (
                                Evade.IsSafePath(
                                    pathToPoint,
                                    Config.EvadingSecondTimeOffset,
                                    spellData.Speed,
                                    spellData.Delay).IsSafe && j == 0)
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
                                         { goodCandidates.MinOrDefault(i => Program.Player.DistanceSquared(i)) };
                }
                if (badCandidates.Count > 0)
                {
                    badCandidates = new List<Vector2>
                                        { badCandidates.MinOrDefault(i => Program.Player.DistanceSquared(i)) };
                }
            }
            var result = goodCandidates.Count > 0 ? goodCandidates : badCandidates;
            result.RemoveAll(i => i.Distance(myPosition) > (overideRange > -1 ? overideRange : spellData.Range));
            if (overideRange > -1 && spellData.IsTargetted)
            {
                for (var i = 0; i < result.Count; i++)
                {
                    var k = (int)(overideRange - result[i].Distance(myPosition));
                    k -= Evade.Rand.Next(k);
                    var posExtend = result[i] + k * (result[i] - myPosition).Normalized();
                    if (Evade.IsSafePoint(posExtend).IsSafe)
                    {
                        result[i] = posExtend;
                    }
                }
            }
            else if (spellData.IsDash)
            {
                if (spellData.IsFixedRange)
                {
                    for (var i = 0; i < result.Count; i++)
                    {
                        result[i] = myPosition.Extend(result[i], spellData.Range);
                    }
                    for (var i = result.Count - 1; i > 0; i--)
                    {
                        if (!Evade.IsSafePoint(result[i]).IsSafe)
                        {
                            result.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < result.Count; i++)
                    {
                        var k = (int)(spellData.Range - result[i].Distance(myPosition));
                        k -= Math.Max(Evade.Rand.Next(k) - 100, 0);
                        var posExtend = result[i] + k * (result[i] - myPosition).Normalized();
                        if (Evade.IsSafePoint(posExtend).IsSafe)
                        {
                            result[i] = posExtend;
                        }
                    }
                }
            }
            else if (spellData.IsBlink)
            {
                for (var i = 0; i < result.Count; i++)
                {
                    var k = (int)(spellData.Range - result[i].Distance(myPosition));
                    k -= Evade.Rand.Next(k);
                    var posExtend = result[i] + k * (result[i] - myPosition).Normalized();
                    if (Evade.IsSafePoint(posExtend).IsSafe)
                    {
                        result[i] = posExtend;
                    }
                }
            }
            return result.Count > 0 ? result.MinOrDefault(i => i.Distance(Game.CursorPos)).ToVector3() : new Vector3();
        }

        internal static Obj_AI_Base GetEvadeTarget(
            this EvadeSpellData spellData,
            bool isBlink = false,
            int overideDelay = -1,
            bool dontCheckForSafety = false)
        {
            var badTargets = new List<Tuple<Obj_AI_Base, Vector2>>();
            var goodTargets = new List<Tuple<Obj_AI_Base, Vector2>>();
            var allTargets = new List<Obj_AI_Base>();
            var delay = overideDelay > -1 ? overideDelay : spellData.Delay;
            foreach (var targetType in spellData.ValidTargets)
            {
                switch (targetType)
                {
                    case SpellValidTargets.AllyChampions:
                        allTargets.AddRange(
                            GameObjects.AllyHeroes.Where(i => i.IsValidTarget(spellData.Range, false) && !i.IsMe));
                        break;
                    case SpellValidTargets.AllyMinions:
                        allTargets.AddRange(
                            GameObjects.AllyMinions.Where(
                                i => i.IsValidTarget(spellData.Range, false) && (i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.AllyWards:
                        allTargets.AddRange(
                            GameObjects.AllyWards.Where(i => i.IsValidTarget(spellData.Range, false) && i.IsWard()));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        allTargets.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(spellData.Range)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        allTargets.AddRange(
                            GameObjects.EnemyMinions.Where(
                                i => i.IsValidTarget(spellData.Range) && (i.IsMinion() || i.IsPet(false))));
                        allTargets.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(spellData.Range)));
                        break;
                    case SpellValidTargets.EnemyWards:
                        allTargets.AddRange(GameObjects.EnemyWards.Where(i => i.IsValidTarget(spellData.Range)));
                        break;
                    case SpellValidTargets.AllyObjects:
                        allTargets.AddRange(
                            GameObjects.AllyMinions.Where(i => i.IsValid() && i.HasBuff(spellData.RequireBuff)));
                        break;
                }
            }
            var underTower = spellData.UnderTower
                                 ? Program.MainMenu["Evade"]["Spells"][spellData.Name][spellData.Slot + "Tower"]
                                 : true;
            foreach (var target in allTargets)
            {
                if (spellData.CheckBuffName != "" && target.HasBuff(spellData.CheckBuffName))
                {
                    continue;
                }
                var pos = spellData.IsFixedRange
                              ? Evade.PlayerPosition.Extend(target.ServerPosition, spellData.Range)
                              : target.ServerPosition.ToVector2();
                if (!dontCheckForSafety && !Evade.IsSafePoint(pos).IsSafe)
                {
                    continue;
                }
                if (pos.IsUnderEnemyTurret() && !underTower)
                {
                    continue;
                }
                if (spellData.IsBlink || isBlink)
                {
                    if (Evade.IsSafeToBlink(pos, Config.EvadingFirstTimeOffset, delay))
                    {
                        goodTargets.Add(new Tuple<Obj_AI_Base, Vector2>(target, pos));
                    }
                    if (Evade.IsSafeToBlink(pos, Config.EvadingSecondTimeOffset, delay))
                    {
                        badTargets.Add(new Tuple<Obj_AI_Base, Vector2>(target, pos));
                    }
                }
                else if (spellData.IsDash)
                {
                    var pathToTarget = new List<Vector2> { Evade.PlayerPosition, pos };
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafePath(pathToTarget, Config.EvadingFirstTimeOffset, spellData.Speed, delay).IsSafe)
                    {
                        goodTargets.Add(new Tuple<Obj_AI_Base, Vector2>(target, pos));
                    }
                    if (Variables.TickCount - Evade.LastWardJumpAttempt < 250
                        || Evade.IsSafePath(pathToTarget, Config.EvadingSecondTimeOffset, spellData.Speed, delay).IsSafe)
                    {
                        badTargets.Add(new Tuple<Obj_AI_Base, Vector2>(target, pos));
                    }
                }
            }
            var goodTarget = goodTargets.MinOrDefault(i => i.Item2.Distance(Game.CursorPos));
            var badTarget = badTargets.MinOrDefault(i => i.Item2.Distance(Game.CursorPos));
            return goodTarget != null ? goodTarget.Item1 : badTarget?.Item1;
        }

        #endregion
    }
}